namespace SpotifySuggestions.Models
{
	public class Track
	{
		public string Id { get; set; }
		public string TrackName { get; set; }
		public string ArtistNames { get; set; }
		public string AlbumName { get; set; }
		public string AlbumCover { get; set; }
		public string PreviewUrl { get; set; }
	}
}