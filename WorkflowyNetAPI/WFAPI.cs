using RestSharp;
using System.Text.Json;

namespace WorkflowyNetAPI
{
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

		private readonly RestClient _client;

		public WFAPI(string api_key)
		{
			var options = new RestClientOptions(BaseUrl)
			{
				ThrowOnAnyError = true
			};
			_client = new RestClient(options);
			_client.AddDefaultHeader("User-Agent", UserAgent);
			_client.AddDefaultHeader("Authorization", $"Bearer {api_key}");
		}

		private static object? TryParseError(string? content)
		{
			if(string.IsNullOrWhiteSpace(content))
				return null;

			try
			{
				// Try to parse JSON into a generic object
				return JsonSerializer.Deserialize<object?>(content);
			}
			catch
			{
				// If not JSON, return the raw content string
				return content;
			}
		}

		public async Task<string?> CreateAsync(string? parentNodeId, string name, string? note, string? layoutMode, string? position)
		{
			var body = new
			{
				parent_id = string.IsNullOrWhiteSpace(parentNodeId) ? null : parentNodeId,
				name = name,
				note = note ?? "",
				layout_mode = layoutMode ?? "default",
				position = position ?? "last"
			};

			var request = new RestRequest("nodes", Method.Post)
				.AddJsonBody(body);

			RestResponse? response = await _client.ExecuteAsync(request);

			if(response == null || !response.IsSuccessful)
			{
				var errorObj = TryParseError(response?.Content);
				throw new WFAPIException($"Error creating node: {(response?.StatusCode.ToString() ?? "no-response")}", errorObj, (int?)(response?.StatusCode) ?? 500);
			}

			return response.Content;
		}

		public async Task<WFNode?> GetNodeAsync(string nodeId)
		{
			var request = new RestRequest($"nodes/{nodeId}", Method.Get);
			RestResponse? response = await _client.ExecuteAsync(request);

			if(response == null || !response.IsSuccessful)
			{
				var errorObj = TryParseError(response?.Content);
				throw new WFAPIException($"Error fetching node: {(response?.StatusCode.ToString() ?? "no-response")}", errorObj, (int?)(response?.StatusCode) ?? 500);
			}

			return JsonSerializer.Deserialize<WFNodeResponse?>(response.Content)?.Node;
		}

		public async Task<WFNode[]?> GetNodesAsync(string? parentId = null)
		{
			string url = parentId == null ? "nodes?parent_id=None" : $"nodes?parent_id={parentId}";
			var request = new RestRequest(url, Method.Get);
			RestResponse? response = await _client.ExecuteAsync(request);

			if(response == null || !response.IsSuccessful)
			{
				var errorObj = TryParseError(response?.Content);
				throw new WFAPIException($"Error fetching nodes: {(response?.StatusCode.ToString() ?? "no-response")}", errorObj, (int?)(response?.StatusCode) ?? 500);
			}

			return JsonSerializer.Deserialize<WFNode[]?>(response.Content);
		}

		public async Task<WFNode?> UpdateNodeNameAsync(string nodeId, string newName)
		{
			var request = new RestRequest($"nodes/{nodeId}", Method.Post)
				.AddJsonBody(new { id = nodeId, name = newName });

			RestResponse? response = await _client.ExecuteAsync(request);

			if(response == null || !response.IsSuccessful)
			{
				var errorObj = TryParseError(response?.Content);
				throw new WFAPIException($"Error updating node: {(response?.StatusCode.ToString() ?? "no-response")}", errorObj, (int?)(response?.StatusCode) ?? 500);
			}

			return JsonSerializer.Deserialize<WFNode?>(response.Content);
		}

		// Agnostic helper for responses that contain a top-level {"status":"ok"} shape.
		// Throws WFAPIException on any non-success condition (HTTP error, missing/invalid status, parsing error).
		private static void EnsureStatusOkOrThrow(RestResponse? response, string operationDescription)
		{
			if(response == null)
			{
				throw new WFAPIException($"{operationDescription}: empty response", null, 0);
			}

			if(!response.IsSuccessful)
			{
				var errorObj = TryParseError(response.Content);
				throw new WFAPIException($"{operationDescription}: HTTP {(int)response.StatusCode} - {response.StatusDescription}", errorObj, (int)response.StatusCode);
			}

			try
			{
				using var doc = JsonDocument.Parse(response.Content ?? "{}");
				if(doc.RootElement.TryGetProperty("status", out var statusProp))
				{
					var status = statusProp.GetString();
					if(!string.Equals(status, "ok", StringComparison.OrdinalIgnoreCase))
					{
						var err = TryParseError(response.Content);
						throw new WFAPIException($"{operationDescription}: status != ok ({status})", err, (int)response.StatusCode);
					}

					// status == ok -> success (return normally)
					return;
				}
				else
				{
					// No 'status' property -> unexpected shape
					var err = TryParseError(response.Content);
					throw new WFAPIException($"{operationDescription}: unexpected response shape (missing 'status')", err, (int)response.StatusCode);
				}
			}
			catch(JsonException je)
			{
				var err = TryParseError(response.Content);
				throw new WFAPIException($"{operationDescription}: error parsing response JSON: {je.Message}", err, (int)response.StatusCode);
			}
		}

		// NOTE: Changed from Task<bool> to Task. On failure this method throws WFAPIException.
		public async Task DeleteAsync(string nodeId)
		{
			var request = new RestRequest($"nodes/{nodeId}", Method.Delete);
			RestResponse? response = await _client.ExecuteAsync(request);

			EnsureStatusOkOrThrow(response, "Delete node");
		}

		// NOTE: Changed from Task<bool> to Task. On failure this method throws WFAPIException.
		public async Task CompleteAsync(string nodeId)
		{
			var request = new RestRequest($"nodes/{nodeId}/complete", Method.Post);
			RestResponse? response = await _client.ExecuteAsync(request);

			EnsureStatusOkOrThrow(response, "Complete node");
		}

		// NOTE: Changed from Task<bool> to Task. On failure this method throws WFAPIException.
		public async Task UncompleteAsync(string nodeId)
		{
			var request = new RestRequest($"nodes/{nodeId}/uncomplete", Method.Post);
			RestResponse? response = await _client.ExecuteAsync(request);

			EnsureStatusOkOrThrow(response, "Uncomplete node");
		}
	}
}