using System;
using System.Text.Json.Serialization;

namespace WorkflowyNetAPI.DTOs
{
	// Response of 'Create a mirror' (POST nodes/{id}/mirror)
	public class WFMirrorRef
	{
		// The id of the newly created mirror node (mirror root)
		[JsonPropertyName("item_id")]
		public Guid MirrorId { get; set; } = Guid.Empty;

		// The id of the origin node the mirror reflects.
		// When the mirrored node was itself a mirror, the API follows it to the true origin.
		[JsonPropertyName("origin_id")]
		public Guid OriginId { get; set; } = Guid.Empty;
	}

	// The 'mirror' object inside WFNodeData
	public class WFNodeMirror
	{
		// Set when this node IS a mirror: the origin it reflects.
		[JsonPropertyName("origin_id")]
		public Guid? OriginId { get; set; } = null;

		// Set when this node IS an origin: the ids of every mirror pointing at it.
		[JsonPropertyName("mirror_ids")]
		public Guid[]? MirrorIds { get; set; } = null;
	}
}
