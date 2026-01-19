using System;
using System.Text.Json.Serialization;

namespace WorkflowyNetAPI
{
	public class WFNode
	{
		public string SiteId => Id.Split('-').Last();
		public string SiteUrl => "https://workflowy.com/#/" + SiteId;

		[JsonPropertyName("id")]
		public string Id { get; set; } = null!;

		[JsonPropertyName("name")]
		public string Name { get; set; } = null!;

		[JsonPropertyName("note")]
		public string? Note { get; set; } = null;

		// The public API only returns the parent node on 'Export all nodes' endpoint
		[JsonPropertyName("parent_id")]
		public string? ParentId { get; set; } = null;

		[JsonPropertyName("priority")]
		public int Priority { get; set; }

		[JsonPropertyName("completed")]
		public bool Completed { get; set; }

		[JsonPropertyName("data")]
		public WFNodeData Data { get; set; } = null!;

		// Unix timestamp -> DateTime (UTC)
		[JsonPropertyName("createdAt")]
		[JsonConverter(typeof(UnixEpochDateTimeConverter))]
		public DateTime CreatedAt { get; set; }

		// Unix timestamp -> DateTime (UTC)
		[JsonPropertyName("modifiedAt")]
		[JsonConverter(typeof(UnixEpochDateTimeConverter))]
		public DateTime ModifiedAt { get; set; }

		// Optional unix timestamp -> nullable DateTime (UTC)
		[JsonPropertyName("completedAt")]
		[JsonConverter(typeof(NullableUnixEpochDateTimeConverter))]
		public DateTime? CompletedAt { get; set; }
	}

	public class WFNodeUpdate
	{
		[JsonPropertyName("id")]
		public string Id { get; set; } = null!;

		[JsonPropertyName("name")]
		public string Name { get; set; } = null!;

		[JsonPropertyName("note")]
		public string? Note { get; set; } = null;

		[JsonPropertyName("layoutMode")]
		public string LayoutMode { get; set; } = null!;
	}

	public class WFNodeData
	{
		[JsonPropertyName("layoutMode")]
		public string LayoutMode { get; set; } = null!;
	}
}

