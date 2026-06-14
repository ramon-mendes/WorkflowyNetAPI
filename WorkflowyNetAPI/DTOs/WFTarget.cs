using System.Text.Json.Serialization;

namespace WorkflowyNetAPI.DTOs
{
	public class WFTarget
	{

		[JsonPropertyName("key")]
		public string? Key { get; set; } = null;

		[JsonPropertyName("type")]
		public string? Type { get; set; } = null;

		[JsonPropertyName("name")]
		public string? Name { get; set; } = null;
	}
}
