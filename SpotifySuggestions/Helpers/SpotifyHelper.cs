using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using SpotifySuggestions.Models;

namespace SpotifySuggestions.Helpers
{
	public class SpotifyHelper
	{
		public static async Task<List<Track>> GetTopSuggestionsAsync(string accessToken, string term)
		{
			var spotifyClient = new SpotifyClient(accessToken);
			var random = new Random();
			var seedTracks = new List<string>();

			var timeRange = term switch
			{
				"long" => PersonalizationTopRequest.TimeRange.LongTerm,
				"medium" => PersonalizationTopRequest.TimeRange.MediumTerm,
				"short" => PersonalizationTopRequest.TimeRange.ShortTerm,
				_ => PersonalizationTopRequest.TimeRange.MediumTerm
			};

			var topTracks = await spotifyClient.Personalization.GetTopTracks(new PersonalizationTopRequest
			{
				Limit = 50,
				TimeRangeParam = timeRange
			});

			var allTopTracks = await spotifyClient.PaginateAll(topTracks);

			for (var i = 0; i < 5; i++)
				seedTracks.Add(allTopTracks[random.Next(allTopTracks.Count)].Id);

			var recommendationsRequest = new RecommendationsRequest
			{
				Limit = 100
			};

			foreach (var seedTrack in seedTracks)
				recommendationsRequest.SeedTracks.Add(seedTrack);

			var recommendationsResponse = await spotifyClient.Browse.GetRecommendations(recommendationsRequest);

			return await MapSuggestionsAsync(spotifyClient, recommendationsResponse.Tracks);
		}

		public static async Task<List<Track>> GetPlaylistSuggestionsAsync(string accessToken, string playlistId)
		{
			var spotifyClient = new SpotifyClient(accessToken);

			var playlistTracks = await spotifyClient.Playlists.GetItems(playlistId);
			var allPlaylistTracks = await spotifyClient.PaginateAll(playlistTracks);
			var random = new Random();
			var seedTracks = new List<string>();

			for (var i = 0; i < 5; i++)
			{
				PlaylistTrack<IPlayableItem> playlistTrack;

				do
				{
					playlistTrack = allPlaylistTracks[random.Next(allPlaylistTracks.Count)];

					if (playlistTrack.Track is FullTrack track)
						seedTracks.Add(track.Id);
				} while (!(playlistTrack.Track is FullTrack));
			}

			var recommendationsRequest = new RecommendationsRequest
			{
				Limit = 100
			};

			foreach (var seedTrack in seedTracks)
				recommendationsRequest.SeedTracks.Add(seedTrack);

			var recommendationsResponse = await spotifyClient.Browse.GetRecommendations(recommendationsRequest);

			return await MapSuggestionsAsync(spotifyClient, recommendationsResponse.Tracks);
		}

		public static async Task<List<Track>> GetCustomSuggestionsAsync(string accessToken, List<string> artistIds,
			List<string> trackIds)
		{
			var spotifyClient = new SpotifyClient(accessToken);

			var recommendationsRequest = new RecommendationsRequest
			{
				Limit = 100
			};
			
			if (artistIds.Count != 0)
			{
				var artistsResponse = await spotifyClient.Artists.GetSeveral(new ArtistsRequest(artistIds));
				
				foreach (var artist in artistsResponse.Artists)
					recommendationsRequest.SeedArtists.Add(artist.Id);
			}

			if (trackIds.Count != 0)
			{
				var tracksResponse = await spotifyClient.Tracks.GetSeveral(new TracksRequest(trackIds));
				
				foreach (var track in tracksResponse.Tracks)
					recommendationsRequest.SeedTracks.Add(track.Id);
			}

			var recommendationsResponse = await spotifyClient.Browse.GetRecommendations(recommendationsRequest);

			return await MapSuggestionsAsync(spotifyClient, recommendationsResponse.Tracks);
		}

		public static async Task<Query> QueryAsync(string accessToken, string query)
		{
			var spotifyClient = new SpotifyClient(accessToken);

			var searchResponse = await spotifyClient.Search.Item(
				new SearchRequest(SearchRequest.Types.Artist | SearchRequest.Types.Track, query)
				{
					Limit = 4
				});

			return new Query
			{
				Tracks = searchResponse.Tracks.Items?.Select(MapTrack).ToList(),
				Artists = searchResponse.Artists.Items?.Select(MapArtist).ToList()
			};
		}

		public static async Task<string> CreatePlaylistAsync(string accessToken, List<string> ids, string name, bool isPrivate)
		{
			var spotifyClient = new SpotifyClient(accessToken);
			var user = await spotifyClient.UserProfile.Current();

			var playlist = await spotifyClient.Playlists.Create(user.Id, new PlaylistCreateRequest(string.IsNullOrEmpty(name) ? "music you'll (probably) like" : name)
			{
				Public = !isPrivate
			});

			if (playlist.Id != null)
			{
				for (var i = 0; i < ids.Count; i += 100)
				{
					await spotifyClient.Playlists.AddItems(playlist.Id,
						new PlaylistAddItemsRequest(ids.Skip(i).Take(100).Select(MapSpotifyUri).ToList()));
				}
			}

			return playlist.Id;
		}

		public static async Task<string> AppendToPlaylistAsync(string accessToken, List<string> ids, string playlistId)
		{
			var spotifyClient = new SpotifyClient(accessToken);

			for (var i = 0; i < ids.Count; i += 100)
			{
				await spotifyClient.Playlists.AddItems(playlistId,
					new PlaylistAddItemsRequest(ids.Skip(i).Take(100).Select(MapSpotifyUri).ToList()));
			}

			return playlistId;
		}

		public static async Task<List<Playlist>> GetPlaylistsAsync(string accessToken)
		{
			var spotifyClient = new SpotifyClient(accessToken);

			var playlistPage = await spotifyClient.Playlists.CurrentUsers(new PlaylistCurrentUsersRequest
			{
				Limit = 50
			});

			var playlists = await spotifyClient.PaginateAll(playlistPage);

			return playlists.Select(MapPlaylist).ToList();
		}

		private static string MapSpotifyUri(string id)
		{
			return $"spotify:track:{id}";
		}
		
		private static async Task<List<Track>> MapSuggestionsAsync(ISpotifyClient spotifyClient,
			IReadOnlyCollection<SimpleTrack> tracks)
		{
			var fullTracks = new List<FullTrack>();

			for (var i = 0; i < tracks.Count; i += 50)
			{
				var ids = tracks.Select(track => track.Id).Skip(i).Take(50).ToList();
				var tracksRequest = new TracksRequest(ids);

				var tracksResponse = await spotifyClient.Tracks.GetSeveral(tracksRequest);
				fullTracks.AddRange(tracksResponse.Tracks);
			}

			return fullTracks.Select(fullTrack => new Track
				{
					Id = fullTrack.Id,
					TrackName = fullTrack.Name,
					AlbumName = fullTrack.Album.Name,
					ArtistNames = fullTrack.Artists
						.Aggregate(string.Empty, (current, artist) => current + $"{artist.Name}, ").TrimEnd(',', ' '),
					AlbumCover = fullTrack.Album.Images[0].Url,
					PreviewUrl = fullTrack.PreviewUrl
				})
				.ToList();
		}

		private static Playlist MapPlaylist(SimplePlaylist playlist)
		{
			return new Playlist
			{
				Id = playlist.Id,
				Name = playlist.Name,
				Count = playlist.Tracks.Total ?? 0
			};
		}
		
		private static Track MapTrack(FullTrack fullTrack)
		{
			return new Track
			{
				Id = fullTrack.Id,
				TrackName = fullTrack.Name,
				AlbumName = fullTrack.Album.Name,
				ArtistNames = fullTrack.Artists
					.Aggregate(string.Empty, (current, artist) => current + $"{artist.Name}, ").TrimEnd(',', ' '),
				AlbumCover = fullTrack.Album.Images[0].Url,
				PreviewUrl = fullTrack.PreviewUrl
			};
		}
		
		private static Artist MapArtist(FullArtist fullArtist)
		{
			return new Artist
			{
				Id = fullArtist.Id,
				Name = fullArtist.Name, 
				Image = fullArtist.Images.Count == 0 ? "/img/album.png" : fullArtist.Images[0].Url,
			};
		}
	}
}