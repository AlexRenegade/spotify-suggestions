using System.Collections.Generic;

namespace SpotifySuggestions.Models
{
	public class Query
	{
		public List<Track> Tracks { get; set; }
		public List<Artist> Artists { get; set; }
	}
}