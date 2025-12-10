using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace WorkflowyNetAPI
{
	public class WFAPIException : System.Exception
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
		}

		private static object? TryParseError(string? content)
		{
			if(string.IsNullOrWhiteSpace(content))
				return null;

			try
			{
				return JsonSerializer.Deserialize<object?>(content);
			}
			catch
			{
				return content;
			}
		}

		// Agnostic helper for responses that contain a top-level {"status":"ok"} shape.
		// Throws WFAPIException on any non-success condition (HTTP error, missing/invalid status, parsing error).
		private static void EnsureStatusOkOrThrow(HttpResponseMessage? response, string content, string operationDescription)
		{
			if(response == null)
			{
				throw new WFAPIException($"{operationDescription}: empty response", null, 0);
			}

			if(!response.IsSuccessStatusCode)
			{
				var errorObj = TryParseError(content);
				throw new WFAPIException($"{operationDescription}: HTTP {(int)response.StatusCode} - {response.ReasonPhrase}", errorObj, (int)response.StatusCode);
			}

			try
			{
				using var doc = JsonDocument.Parse(content ?? "{}");
				if(doc.RootElement.TryGetProperty("status", out var statusProp))
				{
					var status = statusProp.GetString();
					if(!string.Equals(status, "ok", StringComparison.OrdinalIgnoreCase))
					{
						var err = TryParseError(content);
						throw new WFAPIException($"{operationDescription}: status != ok ({status})", err, (int)response.StatusCode);
					}

					// status == ok -> success
					return;
				}
				else
				{
					var err = TryParseError(content);
					throw new WFAPIException($"{operationDescription}: unexpected response shape (missing 'status')", err, (int)response.StatusCode);
				}
			}
			catch(JsonException je)
			{
				var err = TryParseError(content);
				throw new WFAPIException($"{operationDescription}: error parsing response JSON: {je.Message}", err, (int)response.StatusCode);
			}
		}

		// Changed: return deserialized object when possible so controller receives proper JSON value (not a serialized string)
		public async Task<object?> CreateAsync(string? parentNodeId, string name, string? note, string? layoutMode, string? position)
		{
			var body = new
			{
				parent_id = string.IsNullOrWhiteSpace(parentNodeId) ? null : parentNodeId,
				name = name,
				note = note ?? "",
				layout_mode = layoutMode ?? "default",
				position = position ?? "last"
			};

			var json = JsonSerializer.Serialize(body, _jsonOptions);
			using var content = new StringContent(json, Encoding.UTF8, "application/json");

			HttpResponseMessage? response;
			try
			{
				response = await _client.PostAsync("nodes", content).ConfigureAwait(false);
			}
			catch(Exception ex)
			{
				throw new WFAPIException($"Error creating node: {ex.Message}", ex.Message, 0);
			}

			var respContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

			if(!response.IsSuccessStatusCode)
			{
				var errorObj = TryParseError(respContent);
				throw new WFAPIException($"Error creating node: {(int)response.StatusCode} - {response.ReasonPhrase}", errorObj, (int)response.StatusCode);
			}

			// Try to deserialize into an object so we don't return a JSON string inside the 'data' envelope.
			try
			{
				// Deserialize to object (JsonElement / dictionary / primitive)
				var parsed = JsonSerializer.Deserialize<object?>(respContent, _jsonOptions);
				// If parsing yielded null but respContent is non-empty, return the raw string as a fallback
				return parsed ?? respContent;
			}
			catch(JsonException)
			{
				// Not valid JSON: return raw string content
				return respContent;
			}
		}

		public async Task<WFNode?> GetNodeAsync(string nodeId)
		{
			HttpResponseMessage? response;
			try
			{
				response = await _client.GetAsync($"nodes/{nodeId}").ConfigureAwait(false);
			}
			catch(Exception ex)
			{
				throw new WFAPIException($"Error fetching node: {ex.Message}", ex.Message, 0);
			}

			var respContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

			if(!response.IsSuccessStatusCode)
			{
				var errorObj = TryParseError(respContent);
				throw new WFAPIException($"Error fetching node: {(int)response.StatusCode} - {response.ReasonPhrase}", errorObj, (int)response.StatusCode);
			}

			try
			{
				return JsonSerializer.Deserialize<WFNodeResponse?>(respContent, _jsonOptions)?.Node;
			}
			catch(JsonException je)
			{
				throw new WFAPIException($"Error deserializing node response: {je.Message}", respContent, (int)response.StatusCode);
			}
		}

		public async Task<WFNode[]?> GetNodesAsync(string? parentId = null)
		{
			var url = parentId == null ? "nodes?parent_id=None" : $"nodes?parent_id={Uri.EscapeDataString(parentId)}";

			HttpResponseMessage? response;
			try
			{
				response = await _client.GetAsync(url).ConfigureAwait(false);
			}
			catch(Exception ex)
			{
				throw new WFAPIException($"Error fetching nodes: {ex.Message}", ex.Message, 0);
			}

			var respContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

			if(!response.IsSuccessStatusCode)
			{
				var errorObj = TryParseError(respContent);
				throw new WFAPIException($"Error fetching nodes: {(int)response.StatusCode} - {response.ReasonPhrase}", errorObj, (int)response.StatusCode);
			}

			try
			{
				return JsonSerializer.Deserialize<WFNode[]?>(respContent, _jsonOptions);
			}
			catch(JsonException je)
			{
				throw new WFAPIException($"Error deserializing nodes response: {je.Message}", respContent, (int)response.StatusCode);
			}
		}

		public async Task<WFNode?> UpdateNodeNameAsync(string nodeId, string newName)
		{
			var body = new { id = nodeId, name = newName };
			var json = JsonSerializer.Serialize(body, _jsonOptions);
			using var content = new StringContent(json, Encoding.UTF8, "application/json");

			HttpResponseMessage? response;
			try
			{
				response = await _client.PostAsync($"nodes/{nodeId}", content).ConfigureAwait(false);
			}
			catch(Exception ex)
			{
				throw new WFAPIException($"Error updating node: {ex.Message}", ex.Message, 0);
			}

			var respContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

			if(!response.IsSuccessStatusCode)
			{
				var errorObj = TryParseError(respContent);
				throw new WFAPIException($"Error updating node: {(int)response.StatusCode} - {response.ReasonPhrase}", errorObj, (int)response.StatusCode);
			}

			try
			{
				return JsonSerializer.Deserialize<WFNode?>(respContent, _jsonOptions);
			}
			catch(JsonException je)
			{
				throw new WFAPIException($"Error deserializing update response: {je.Message}", respContent, (int)response.StatusCode);
			}
		}

		// NOTE: Changed from Task<bool> to Task. On failure this method throws WFAPIException.
		public async Task DeleteAsync(string nodeId)
		{
			HttpResponseMessage? response;
			try
			{
				response = await _client.DeleteAsync($"nodes/{nodeId}").ConfigureAwait(false);
			}
			catch(Exception ex)
			{
				throw new WFAPIException($"Error deleting node: {ex.Message}", ex.Message, 0);
			}

			var respContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
			EnsureStatusOkOrThrow(response, respContent, "Delete node");
		}

		// NOTE: Changed from Task<bool> to Task. On failure this method throws WFAPIException.
		public async Task CompleteAsync(string nodeId)
		{
			HttpResponseMessage? response;
			try
			{
				response = await _client.PostAsync($"nodes/{nodeId}/complete", new StringContent(string.Empty)).ConfigureAwait(false);
			}
			catch(Exception ex)
			{
				throw new WFAPIException($"Error completing node: {ex.Message}", ex.Message, 0);
			}

			var respContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
			EnsureStatusOkOrThrow(response, respContent, "Complete node");
		}

		// NOTE: Changed from Task<bool> to Task. On failure this method throws WFAPIException.
		public async Task UncompleteAsync(string nodeId)
		{
			HttpResponseMessage? response;
			try
			{
				response = await _client.PostAsync($"nodes/{nodeId}/uncomplete", new StringContent(string.Empty)).ConfigureAwait(false);
			}
			catch(Exception ex)
			{
				throw new WFAPIException($"Error uncompleting node: {ex.Message}", ex.Message, 0);
			}

			var respContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
			EnsureStatusOkOrThrow(response, respContent, "Uncomplete node");
		}
	}
}