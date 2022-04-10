using System.Collections.Generic;

namespace SpotifySuggestions.Models.Configurations
{
	public class SpotifyConfiguration
	{
		public string ClientId { get; set; }
		public string ClientSecret { get; set; }
		public List<string> Scopes { get; set; }
	}
}