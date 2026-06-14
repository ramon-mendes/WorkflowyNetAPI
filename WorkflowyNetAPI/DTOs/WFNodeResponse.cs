using System.Text.Json.Serialization;

namespace WorkflowyNetAPI.DTOs
{
	public class WFNodeResponse
	{
		[JsonPropertyName("node")]
		public WFNode Node { get; set; } = null!;
	}
}
