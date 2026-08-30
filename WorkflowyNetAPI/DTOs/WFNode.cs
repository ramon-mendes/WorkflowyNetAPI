using System;
using System.Text.Json.Serialization;
using WorkflowyNetAPI.Utilities;

namespace WorkflowyNetAPI.DTOs
{
	public class WFNode
	{
		public string Hash => Id.ToString().Split('-').Last();
		public string URL => "https://workflowy.com/#/" + Hash;

		// True when this node is a mirror of another node
		[JsonIgnore]
		public bool IsMirror => Data?.Mirror?.OriginId != null;

		// True when this node is the origin of one or more mirrors
		[JsonIgnore]
		public bool IsMirrorOrigin => Data?.Mirror?.MirrorIds?.Length > 0;

		[JsonPropertyName("id")]
		public Guid Id { get; set; } = Guid.Empty;

		[JsonPropertyName("name")]
		public string Name { get; set; } = null!;

		[JsonPropertyName("note")]
		public string? Note { get; set; } = null;

		// The public API only returns the parent node on 'Export all nodes' endpoint
		[JsonPropertyName("parent_id")]
		public Guid? ParentId { get; set; } = Guid.Empty;

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

		// Only present when the node is a mirror or a mirror origin
		[JsonPropertyName("mirror")]
		public WFNodeMirror? Mirror { get; set; } = null;
	}
}