using System.Collections.Generic;

namespace SpotifySuggestions.Models.Requests
{
	public class CustomSuggestionsRequest
	{
		public List<string> Artists { get; set; }
		public List<string> Tracks { get; set; }
	}
}