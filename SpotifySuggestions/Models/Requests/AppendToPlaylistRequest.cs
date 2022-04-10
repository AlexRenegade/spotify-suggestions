using System.Collections.Generic;

namespace SpotifySuggestions.Models.Requests
{
	public class AppendToPlaylistRequest
	{
		public List<string> Ids { get; set; }
		public string PlaylistId { get; set; }
	}
}