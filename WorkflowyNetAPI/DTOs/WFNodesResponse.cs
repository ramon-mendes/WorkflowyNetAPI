using System.Text.Json.Serialization;

namespace WorkflowyNetAPI.DTOs
{
	public class WFNodesResponse
	{
		[JsonPropertyName("nodes")]
		public WFNode[] Nodes { get; set; } = null!;
	}
}
