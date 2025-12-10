using RestSharp;
using System.Text.Json;

namespace WorkflowyNetAPI
{
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

		public async Task<string> CreateAsync(string parentNodeId, string name, string note, string layoutMode, string position)
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

			RestResponse response = await _client.ExecuteAsync(request);

			if(!response.IsSuccessful)
				throw new Exception($"Error creating node: {response.StatusCode} - {response.Content}");

			return response.Content;
		}

		public async Task<WFNode> GetNodeAsync(string nodeId)
        {
            var request = new RestRequest($"nodes/{nodeId}", Method.Get);
            RestResponse response = await _client.ExecuteAsync(request);

            if (!response.IsSuccessful)
				throw new Exception($"Error fetching node: {response.StatusCode} - {response.Content}");

            return JsonSerializer.Deserialize<WFNodeResponse>(response.Content)?.Node;
        }

        public async Task<WFNode[]> GetNodesAsync(string parentId = null)
        {
            string url = parentId == null ? "nodes?parent_id=None" : $"nodes?parent_id={parentId}";
            var request = new RestRequest(url, Method.Get);
            RestResponse response = await _client.ExecuteAsync(request);

            if (!response.IsSuccessful)
                throw new System.Exception($"Error fetching nodes: {response.StatusCode} - {response.Content}");

            return JsonSerializer.Deserialize<WFNode[]>(response.Content);
        }

        public async Task<WFNode> UpdateNodeNameAsync(string nodeId, string newName)
        {
            var request = new RestRequest($"nodes/{nodeId}", Method.Post)
                .AddJsonBody(new { id = nodeId, name = newName });

            RestResponse response = await _client.ExecuteAsync(request);

            if (!response.IsSuccessful)
                throw new System.Exception($"Error updating node: {response.StatusCode} - {response.Content}");

            return JsonSerializer.Deserialize<WFNode>(response.Content);
        }

		public async Task<bool> DeleteAsync(string nodeId)
		{
			var request = new RestRequest($"nodes/{nodeId}", Method.Delete);
			RestResponse response = await _client.ExecuteAsync(request);

			if(!response.IsSuccessful)
				throw new Exception($"Error deleting node: {response.StatusCode} - {response.Content}");

			try
			{
				using var doc = JsonDocument.Parse(response.Content);
				string status = doc.RootElement.GetProperty("status").GetString();
				return status?.ToLower() == "ok";
			}
			catch
			{
				return false;
			}
		}

		public async Task<bool> CompleteAsync(string nodeId)
		{
			var request = new RestRequest($"nodes/{nodeId}/complete", Method.Post);
			RestResponse response = await _client.ExecuteAsync(request);

			if(!response.IsSuccessful)
				throw new Exception($"Error completing node: {response.StatusCode} - {response.Content}");

			try
			{
				using var doc = JsonDocument.Parse(response.Content);
				string status = doc.RootElement.GetProperty("status").GetString();
				return status?.ToLower() == "ok";
			}
			catch
			{
				return false;
			}
		}

		public async Task<bool> UncompleteAsync(string nodeId)
		{
			var request = new RestRequest($"nodes/{nodeId}/uncomplete", Method.Post);
			RestResponse response = await _client.ExecuteAsync(request);

			if(!response.IsSuccessful)
				throw new Exception($"Error uncompleting node: {response.StatusCode} - {response.Content}");

			try
			{
				using var doc = JsonDocument.Parse(response.Content);
				string status = doc.RootElement.GetProperty("status").GetString();
				return status?.ToLower() == "ok";
			}
			catch
			{
				return false;
			}
		}
	}
}
