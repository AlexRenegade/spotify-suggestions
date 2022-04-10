using System;
using System.Threading.Tasks;
using Bugsnag;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SpotifyAPI.Web;
using SpotifySuggestions.Helpers;
using SpotifySuggestions.Models.Configurations;
using SpotifySuggestions.Models.Requests;

namespace SpotifySuggestions.Controllers
{
	[Route("api/playlists")]
	[ApiController]
	public class PlaylistController : Controller
	{
		private readonly SpotifyConfiguration _spotifyConfiguration;
		private readonly IClient _bugsnagClient;
		
		public PlaylistController(SpotifyConfiguration spotifyConfiguration, IClient bugsnagClient)
		{
			_spotifyConfiguration = spotifyConfiguration;
			_bugsnagClient = bugsnagClient;
		}

		[HttpPost("create")]
		public async Task<IActionResult> CreatePlaylistAsync([FromBody] CreatePlaylistRequest createPlaylistRequest)
		{
			var accessToken = HttpContext.Session.GetString("access_token");

			if (accessToken == null)
				return Unauthorized(new {error = true, message = "You have been logged out, please refresh the page."});

			if (createPlaylistRequest.Ids == null)
				return BadRequest(new {error = true, message = "Select some songs to add to the playlist!"});
			
			if (createPlaylistRequest.Ids.Count == 0)
				return BadRequest(new {error = true, message = "Select some songs to add to the playlist!"});
			
			try
			{
				var playlistId = await SpotifyHelper.CreatePlaylistAsync(accessToken, createPlaylistRequest.Ids, createPlaylistRequest.Name, createPlaylistRequest.IsPrivate);

				return Ok(new {error = false, playlist = playlistId});
			}
			catch (APIUnauthorizedException)
			{
				var refreshToken = HttpContext.Session.GetString("refresh_token");

				var response = await new OAuthClient().RequestToken(
					new AuthorizationCodeRefreshRequest(_spotifyConfiguration.ClientId,
						_spotifyConfiguration.ClientSecret, refreshToken));
				
				HttpContext.Session.SetString("access_token", response.AccessToken);
				
				var playlistId = await SpotifyHelper.CreatePlaylistAsync(accessToken, createPlaylistRequest.Ids, createPlaylistRequest.Name, createPlaylistRequest.IsPrivate);

				return Ok(new {error = false, playlist = playlistId});
			}
			catch (Exception exception)
			{
				_bugsnagClient.Notify(exception);
				return BadRequest(new {error = true, message = exception.Message});
			}
		}

		[HttpPatch("append")]
		public async Task<IActionResult> AppendToPlaylistAsync([FromBody] AppendToPlaylistRequest appendToPlaylistRequest)
		{
			var accessToken = HttpContext.Session.GetString("access_token");

			if (accessToken == null)
				return Unauthorized(new {error = true, message = "You have been logged out, please refresh the page."});

			if (appendToPlaylistRequest.Ids == null)
				return BadRequest(new {error = true, message = "Select some songs to add to the playlist!"});
			
			if (appendToPlaylistRequest.Ids.Count == 0)
				return BadRequest(new {error = true, message = "Select some songs to add to the playlist!"});
			
			if (appendToPlaylistRequest.PlaylistId == null)
				return BadRequest(new {error = true, message = "Choose a playlist to add songs to!"});
			
			try
			{
				var playlistId = await SpotifyHelper.AppendToPlaylistAsync(accessToken, appendToPlaylistRequest.Ids, appendToPlaylistRequest.PlaylistId);

				return Ok(new {error = false, playlist = playlistId});
			}
			catch (APIUnauthorizedException)
			{
				var refreshToken = HttpContext.Session.GetString("refresh_token");

				var response = await new OAuthClient().RequestToken(
					new AuthorizationCodeRefreshRequest(_spotifyConfiguration.ClientId,
						_spotifyConfiguration.ClientSecret, refreshToken));
				
				HttpContext.Session.SetString("access_token", response.AccessToken);
				
				var playlistId = await SpotifyHelper.AppendToPlaylistAsync(accessToken, appendToPlaylistRequest.Ids, appendToPlaylistRequest.PlaylistId);

				return Ok(new {error = false, playlist = playlistId});
			}
			catch (Exception exception)
			{
				_bugsnagClient.Notify(exception);
				return BadRequest(new {error = true, message = exception.Message});
			}
		}
		
		[HttpGet("me")]
		public async Task<IActionResult> GetPlaylistsAsync()
		{
			var accessToken = HttpContext.Session.GetString("access_token");

			if (accessToken == null)
				return Unauthorized(new {error = true, message = "You have been logged out, please refresh the page."});
			
			try
			{
				var playlists = await SpotifyHelper.GetPlaylistsAsync(accessToken);
				
				return Ok(new {error = false, playlists});
			}
			catch (APIUnauthorizedException)
			{
				var refreshToken = HttpContext.Session.GetString("refresh_token");

				var response = await new OAuthClient().RequestToken(
					new AuthorizationCodeRefreshRequest(_spotifyConfiguration.ClientId,
						_spotifyConfiguration.ClientSecret, refreshToken));
				
				HttpContext.Session.SetString("access_token", response.AccessToken);
				
				var playlists = await SpotifyHelper.GetPlaylistsAsync(accessToken);
				
				return Ok(new {error = false, playlists});
			}
			catch (Exception exception)
			{
				_bugsnagClient.Notify(exception);
				return BadRequest(new {error = true, message = exception.Message});
			}
		}
	}
}