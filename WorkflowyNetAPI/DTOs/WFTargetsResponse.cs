using System.Text.Json.Serialization;

namespace WorkflowyNetAPI.DTOs
{
	public class WFTargetsResponse
	{
		[JsonPropertyName("targets")]
		public WFTarget[] Nodes { get; set; } = null!;
	}
}
