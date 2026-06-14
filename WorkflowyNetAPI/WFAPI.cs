using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WorkflowyNetAPI.DTOs;
using WorkflowyNetAPI.Utilities;

namespace WorkflowyNetAPI
{
	public class NodeIdentifier
	{
		public readonly static NodeIdentifier HOME = TargetNode("None");

		// your Inbox node, the built-in destination for quickly captured items at the top of your outline.
		public readonly static NodeIdentifier INBOX = TargetNode("inbox");

		// the root of your calendar.
		public readonly static NodeIdentifier CALENDAR = TargetNode("calendar");

		// the calendar node for today's date.
		public readonly static NodeIdentifier TODAY = TargetNode("today");

		// the calendar node for tomorrow's date.
		public readonly static NodeIdentifier TOMORROW = TargetNode("tomorrow");

		// the calendar node for the first day of next week, based on your week-start-day setting.
		public readonly static NodeIdentifier NEXT_WEEK = TargetNode("next_week");

		public readonly static NodeIdentifier[] AllIdentifiers = [HOME, INBOX, CALENDAR, TODAY, TOMORROW, NEXT_WEEK];

		private NodeIdentifier()
		{
		}

		public string Identifier { get; private set; } = null!;

		public static NodeIdentifier Guid(Guid guid)
		{
			return new NodeIdentifier()
			{
				Identifier = guid.ToString()
			};
		}

		public static NodeIdentifier TargetNode(string target)
		{
			return new() { Identifier = target };
		}

		public static NodeIdentifier DateNode(DateTime date)
		{
			return new() { Identifier = date.ToString("yyyy-MM-dd") };
		}

		public static NodeIdentifier YearNode(int year)
		{
			return new() { Identifier = year.ToString() };
		}

		public static NodeIdentifier MonthNode(int year, int month)
		{
			return new() { Identifier = $"{year}-{month:D2}" };
		}
	}

	public class WFAPIException : Exception
	{
		public object? Error { get; }
		public int StatusCode { get; }

		public WFAPIException(string message, object? error = null, int statusCode = 500)
			: base(message)
		{
			Debug.Assert(statusCode >= 0);

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
		private object? TryParseJson(string? content)
		{
			if(string.IsNullOrWhiteSpace(content))
				return null;

			try
			{
				return JsonSerializer.Deserialize<object?>(content, _jsonOptions);
			}
			catch
			{
				Debug.Assert(false);
				return content;
			}
		}

		private void EnsureHttpSuccessOrThrow(HttpResponseMessage? response, string content, string operation_desc)
		{
			if(response == null)
				throw new WFAPIException($"{operation_desc}: empty response", null);

			if(!response.IsSuccessStatusCode)
			{
				var errorObj = TryParseJson(content);
				throw new WFAPIException($"{operation_desc}: HTTP {(int)response.StatusCode} - {response.ReasonPhrase}", errorObj, (int)response.StatusCode);
			}
		}

		private void EnsureStatusOkOrThrow(HttpResponseMessage response, string content, string operation_desc)
		{
			EnsureHttpSuccessOrThrow(response, content, operation_desc);

			try
			{
				using var doc = JsonDocument.Parse(content);
				if(!doc.RootElement.TryGetProperty("status", out var statusProp))
					throw new WFAPIException($"{operation_desc}: missing 'status'", TryParseJson(content), (int)response.StatusCode);

				if(!string.Equals(statusProp.GetString(), "ok", StringComparison.OrdinalIgnoreCase))
					throw new WFAPIException($"{operation_desc}: status != ok", TryParseJson(content), (int)response.StatusCode);
			}
			catch(JsonException je)
			{
				throw new WFAPIException($"{operation_desc}: invalid JSON: {je.Message}", TryParseJson(content), (int)response.StatusCode);
			}
		}

		/// Generic request helper
		/// If checkOkStatus is true, also validates that the response JSON contains "status":"ok".
		private async Task<(HttpResponseMessage response, string content)> TryRequestAsync(
			Func<Task<HttpResponseMessage>> send,
			string operation,
			bool checkOkStatus = false)
		{
			HttpResponseMessage response;

			try { response = await send().ConfigureAwait(false); }
			catch(Exception ex)
			{
				throw new WFAPIException($"{operation}: network error: {ex.Message}", ex.Message, 0);
			}

			var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

			if (checkOkStatus)
				EnsureStatusOkOrThrow(response, content, operation);
			else
				EnsureHttpSuccessOrThrow(response, content, operation);

			return (response, content);
		}


		/*-------------------------------------------------------
			ENDPOINTS
		-------------------------------------------------------*/
		public async Task<Guid> CreateAsync(NodeIdentifier parent, string name, string? note = null, string? layoutMode = null, EPosition position = EPosition.TOP)
		{
			var body = new
			{
				parent_id = parent.Identifier,
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
				return Guid.Parse(parsed["item_id"]);
			}
			catch
			{
				throw new WFAPIException("Error deserializing response", content);
			}
		}

		public async Task UpdateNodeAsync(WFNodeUpdate node)
		{
			var json = JsonSerializer.Serialize(node, _jsonOptions);

			await TryRequestAsync(
				() => _client.PostAsync($"nodes/{node.Id}", new StringContent(json, Encoding.UTF8, "application/json")),
				"Update node",
				checkOkStatus: true
			);
		}

		public async Task<WFNode> GetNodeAsync(Guid nodeId)
		{
			var (_, content) = await TryRequestAsync(
				() => _client.GetAsync($"nodes/{nodeId}"),
				"Fetch node"
			);

			try
			{
				var node = JsonSerializer.Deserialize<WFNodeResponse>(content, _jsonOptions)!.Node;
				return node;
			}
			catch(JsonException je)
			{
				throw new WFAPIException($"Error deserializing response: {je.Message}", content);
			}
		}

		public async Task<WFNode[]> GetChildNodesAsync(NodeIdentifier parent)
		{
			string url = $"nodes?parent_id={Uri.EscapeDataString(parent.Identifier)}";

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
				throw new WFAPIException($"Error deserializing response: {je.Message}", content);
			}
		}

		public async Task DeleteAsync(Guid nodeId)
		{
			await TryRequestAsync(
				() => _client.DeleteAsync($"nodes/{nodeId}"),
				"Delete node",
				checkOkStatus: true
			);
		}

		public enum EPosition
		{
			TOP,
			BOTTOM
		}

		public async Task MoveAsync(Guid nodeId, NodeIdentifier parent, EPosition position = EPosition.TOP)
		{
			var json = JsonSerializer.Serialize(new
			{
				parent_id = parent.Identifier,
				position = position.ToString().ToLower()
			}, _jsonOptions);

			await TryRequestAsync(
				() => _client.PostAsync($"nodes/{nodeId}/move", new StringContent(json, Encoding.UTF8, "application/json")),
				"Move node",
				checkOkStatus: true
			);
		}

		public async Task CompleteAsync(Guid nodeId)
		{
			await TryRequestAsync(
				() => _client.PostAsync($"nodes/{nodeId}/complete", new StringContent(""))
				, "Complete node"
				, checkOkStatus: true
			);
		}

		public async Task UncompleteAsync(Guid nodeId)
		{
			await TryRequestAsync(
				() => _client.PostAsync($"nodes/{nodeId}/uncomplete", new StringContent(""))
				, "Uncomplete node"
				, checkOkStatus: true
			);
		}

		public virtual async Task<WFNodesResponse> ExportAllNodesAsync()
		{
			var (_, content) = await TryRequestAsync(
				() => _client.GetAsync("nodes-export"),
				"Export all nodes"
			);

			try
			{
				return JsonSerializer.Deserialize<WFNodesResponse>(content, _jsonOptions)!;
			}
			catch
			{
				throw new WFAPIException("Error deserializing response", content);
			}
		}

		public virtual async Task<WFTarget[]> ListTargetsAsync()
		{
			var (_, content) = await TryRequestAsync(
				() => _client.GetAsync("targets"),
				"List targets"
			);

			try
			{
				return JsonSerializer.Deserialize<WFTargetsResponse>(content, _jsonOptions)!.Nodes;
			}
			catch(JsonException je)
			{
				throw new WFAPIException($"Error deserializing response: {je.Message}", content);
			}
		}
	}
}
