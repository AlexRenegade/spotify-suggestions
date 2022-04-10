using System.Collections.Generic;

namespace SpotifySuggestions.Models.Requests
{
	public class CreatePlaylistRequest
	{
		public List<string> Ids { get; set; }
		public string Name { get; set; }
		public bool IsPrivate { get; set; }
	}
}