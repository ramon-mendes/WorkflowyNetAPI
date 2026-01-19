using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WorkflowyNetAPI
{
	public struct ParentIdOrTarget
	{
		public readonly static ParentIdOrTarget TOP_LEVEL = new ParentIdOrTarget("None");
		public readonly static ParentIdOrTarget HOME = new ParentIdOrTarget("home");
		public readonly static ParentIdOrTarget INBOX = new ParentIdOrTarget("inbox");

		public string ParentId { get; private set; }

        public ParentIdOrTarget(string id_or_target)
        {
			ParentId = id_or_target;
			Validate();
		}

		public static implicit operator ParentIdOrTarget(string value)
		{
			return new ParentIdOrTarget(value);
		}

		public void Validate()
		{
			// TODO: very low-priority
		}
	}

	public class WFNodeResponse
	{
		[JsonPropertyName("node")]
		public WFNode Node { get; set; } = null!;
	}

	public class WFNodesResponse
	{
		[JsonPropertyName("nodes")]
		public WFNode[] Nodes { get; set; } = null!;
	}


	public class WFAPIException : Exception
	{
		public object? Error { get; }
		public int StatusCode { get; }

		public WFAPIException(string message, object? error = null, int statusCode = 500)
			: base(message)
		{
			Error = error;
			StatusCode = statusCode;
		}
	}

	public class WFAPI
	{
		private const string BaseUrl = "https://workflowy.com/api/v1/";
		private const string UserAgent = "insomnia/11.6.1";

		private readonly HttpClient _client;
		private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

		public WFAPI(string api_key)
		{
			if(string.IsNullOrWhiteSpace(api_key))
				throw new ArgumentException("API key must be provided", nameof(api_key));

			_client = new HttpClient
			{
				BaseAddress = new Uri(BaseUrl),
				Timeout = TimeSpan.FromSeconds(30)
			};

			_client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
			_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", api_key);
			_client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

			_jsonOptions.Converters.Add(new UnixEpochDateTimeConverter());
			_jsonOptions.Converters.Add(new NullableUnixEpochDateTimeConverter());
		}

		/*-------------------------------------------------------
			HELPERS
		-------------------------------------------------------*/

		private object? TryParseError(string? content)
		{
			if(string.IsNullOrWhiteSpace(content))
				return null;

			try
			{
				return JsonSerializer.Deserialize<object?>(content, _jsonOptions);
			}
			catch
			{
				return content;
			}
		}

		private void EnsureHttpSuccessOrThrow(HttpResponseMessage? response, string content, string op)
		{
			if(response == null)
				throw new WFAPIException($"{op}: empty response", null, 0);

			if(!response.IsSuccessStatusCode)
			{
				var errorObj = TryParseError(content);
				throw new WFAPIException($"{op}: HTTP {(int)response.StatusCode} - {response.ReasonPhrase}", errorObj, (int)response.StatusCode);
			}
		}

		private void EnsureStatusOkOrThrow(HttpResponseMessage response, string content, string op)
		{
			EnsureHttpSuccessOrThrow(response, content, op);

			try
			{
				using var doc = JsonDocument.Parse(content);
				if(!doc.RootElement.TryGetProperty("status", out var statusProp))
					throw new WFAPIException($"{op}: missing 'status'", TryParseError(content), (int)response.StatusCode);

				if(!string.Equals(statusProp.GetString(), "ok", StringComparison.OrdinalIgnoreCase))
					throw new WFAPIException($"{op}: status != ok", TryParseError(content), (int)response.StatusCode);
			}
			catch(JsonException je)
			{
				throw new WFAPIException($"{op}: invalid JSON: {je.Message}", TryParseError(content), (int)response.StatusCode);
			}
		}

		/// Generic request helper
		private async Task<(HttpResponseMessage response, string content)> TryRequestAsync(
			Func<Task<HttpResponseMessage>> send,
			string operation)
		{
			HttpResponseMessage response;

			try { response = await send().ConfigureAwait(false); }
			catch(Exception ex)
			{
				throw new WFAPIException($"{operation}: network error: {ex.Message}", ex.Message, 0);
			}

			var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

			EnsureHttpSuccessOrThrow(response, content, operation);
			return (response, content);
		}

		/// Request helper that requires "status": "ok"
		private async Task<(HttpResponseMessage response, string content)> TryRequestStatusOkAsync(
			Func<Task<HttpResponseMessage>> send,
			string operation)
		{
			var (response, content) = await TryRequestAsync(send, operation);
			EnsureStatusOkOrThrow(response, content, operation);
			return (response, content);
		}

		/*-------------------------------------------------------
			ENDPOINTS
		-------------------------------------------------------*/

		public async Task<string> CreateAsync(string? parentNodeId, string name, string? note, string? layoutMode, EPosition position = EPosition.TOP)
		{
			var body = new
			{
				parent_id = string.IsNullOrWhiteSpace(parentNodeId) ? null : parentNodeId,
				name,
				note,
				layoutMode,
				position = position.ToString().ToLower()
			};

			var json = JsonSerializer.Serialize(body, _jsonOptions);

			var (_, content) = await TryRequestAsync(
				() => _client.PostAsync("nodes", new StringContent(json, Encoding.UTF8, "application/json")),
				"Create node"
			);

			try
			{
				var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(content, _jsonOptions)!;
				return parsed["item_id"];
			}
			catch
			{
				return content;
			}
		}

		public async Task UpdateNodeAsync(WFNodeUpdate node)
		{
			var json = JsonSerializer.Serialize(node, _jsonOptions);

			await TryRequestStatusOkAsync(
				() => _client.PostAsync($"nodes/{node.Id}", new StringContent(json, Encoding.UTF8, "application/json")),
				"Update node"
			);
		}

		public async Task<WFNode> GetNodeAsync(string nodeId)
		{
			var (_, content) = await TryRequestAsync(
				() => _client.GetAsync($"nodes/{nodeId}"),
				"Fetch node"
			);

			try
			{
				return JsonSerializer.Deserialize<WFNodeResponse>(content, _jsonOptions)!.Node;
			}
			catch(JsonException je)
			{
				throw new WFAPIException($"Error deserializing node: {je.Message}", content);
			}
		}

		public async Task<WFNode[]> GetNodesAsync(string? parentId = null)
		{
			string url = parentId == null
				? "nodes?parent_id=None"
				: $"nodes?parent_id={Uri.EscapeDataString(parentId)}";

			var (_, content) = await TryRequestAsync(
				() => _client.GetAsync(url),
				"Fetch nodes"
			);

			try
			{
				return JsonSerializer.Deserialize<WFNodesResponse>(content, _jsonOptions)!.Nodes;
			}
			catch(JsonException je)
			{
				throw new WFAPIException($"Error deserializing nodes: {je.Message}", content);
			}
		}

		public async Task DeleteAsync(string nodeId)
		{
			await TryRequestStatusOkAsync(
				() => _client.DeleteAsync($"nodes/{nodeId}"),
				"Delete node"
			);
		}

		public enum EPosition
		{
			TOP,
			BOTTOM
		}

		public async Task MoveAsync(string nodeId, ParentIdOrTarget parentNode, EPosition position = EPosition.TOP)
		{
			var json = JsonSerializer.Serialize(new
			{
				parent_id = parentNode.ParentId,
				position = position.ToString().ToLower()
			}, _jsonOptions);

			await TryRequestStatusOkAsync(
				() => _client.PostAsync($"nodes/{nodeId}/move", new StringContent(json, Encoding.UTF8, "application/json")),
				"Move node"
			);
		}

		public async Task CompleteAsync(string nodeId)
		{
			await TryRequestStatusOkAsync(
				() => _client.PostAsync($"nodes/{nodeId}/complete", new StringContent("")),
				"Complete node"
			);
		}

		public async Task UncompleteAsync(string nodeId)
		{
			await TryRequestStatusOkAsync(
				() => _client.PostAsync($"nodes/{nodeId}/uncomplete", new StringContent("")),
				"Uncomplete node"
			);
		}

		public async Task<WFNodesResponse> ExportAllNodesAsync()
		{
			var (_, content) = await TryRequestAsync(
				() => _client.GetAsync("nodes-export"),
				"Export nodes"
			);

			try
			{
				return JsonSerializer.Deserialize<WFNodesResponse>(content, _jsonOptions)!;
			}
			catch
			{
				throw new WFAPIException("Error deserializing export", content);
			}
		}
	}
}
