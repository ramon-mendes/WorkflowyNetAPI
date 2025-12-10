using System.Text.Json.Serialization;

namespace WorkflowyNetAPI
{
    public class WFNodeResponse
    {
        [JsonPropertyName("node")]
        public WFNode Node { get; set; }
    }

    public class WFNode
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("note")]
        public string Note { get; set; }

		[JsonPropertyName("parent_id")]
		public string ParentId { get; set; }

		[JsonPropertyName("priority")]
        public int Priority { get; set; }

        [JsonPropertyName("data")]
        public WFNodeData Data { get; set; }

        [JsonPropertyName("createdAt")]
        public long CreatedAt { get; set; }

        [JsonPropertyName("modifiedAt")]
        public long ModifiedAt { get; set; }

        [JsonPropertyName("completedAt")]
        public long? CompletedAt { get; set; }
    }

    public class WFNodeData
    {
        [JsonPropertyName("layoutMode")]
        public string LayoutMode { get; set; }
    }
}
