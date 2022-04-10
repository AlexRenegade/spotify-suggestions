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
	[Route("api/suggestions")]
	[ApiController]
	public class SuggestionsController : Controller
	{
		private readonly SpotifyConfiguration _spotifyConfiguration;
		private readonly IClient _bugsnagClient;

		public SuggestionsController(SpotifyConfiguration spotifyConfiguration, IClient bugsnagClient)
		{
			_spotifyConfiguration = spotifyConfiguration;
			_bugsnagClient = bugsnagClient;
		}
		
		[HttpGet("top")]
		public async Task<IActionResult> GetTopSuggestionsAsync(string term)
		{
			var accessToken = HttpContext.Session.GetString("access_token");

			if (accessToken == null)
				return Unauthorized(new {error = true, message = "You have been logged out, please refresh the page."});

			if (string.IsNullOrEmpty(term))
				return BadRequest(new {error = true, message = "You must supply a valid term"});

			if (term != "long" && term != "medium" && term != "short")
				return BadRequest(new {error = true, message = "You must supply a valid term"});

			try
			{
				var suggestions = await SpotifyHelper.GetTopSuggestionsAsync(accessToken, term);

				return Ok(new {error = false, suggestions});
			}
			catch (APIUnauthorizedException)
			{
				var refreshToken = HttpContext.Session.GetString("refresh_token");

				var response = await new OAuthClient().RequestToken(
					new AuthorizationCodeRefreshRequest(_spotifyConfiguration.ClientId,
						_spotifyConfiguration.ClientSecret, refreshToken));
				
				HttpContext.Session.SetString("access_token", response.AccessToken);
				
				var suggestions = await SpotifyHelper.GetTopSuggestionsAsync(response.AccessToken, term);

				return Ok(new {error = false, suggestions});
			}
			catch (Exception exception)
			{
				_bugsnagClient.Notify(exception);
				return BadRequest(new {error = true, message = exception.Message});
			}
		}

		[HttpGet("playlist")]
		public async Task<IActionResult> GetPlaylistSuggestionsAsync(string id)
		{
			var accessToken = HttpContext.Session.GetString("access_token");

			if (accessToken == null)
				return Unauthorized(new {error = true, message = "You have been logged out, please refresh the page."});

			if (string.IsNullOrEmpty(id))
				return BadRequest(new {error = true, message = "You must supply a valid playlist ID"});
			
			try
			{
				var spotifyClient = new SpotifyClient(accessToken);
				
				await spotifyClient.Playlists.Get(id);

				var suggestions = await SpotifyHelper.GetPlaylistSuggestionsAsync(accessToken, id);

				return Ok(new {error = false, suggestions});
			}
			catch (APIUnauthorizedException)
			{
				var refreshToken = HttpContext.Session.GetString("refresh_token");

				var response = await new OAuthClient().RequestToken(
					new AuthorizationCodeRefreshRequest(_spotifyConfiguration.ClientId,
						_spotifyConfiguration.ClientSecret, refreshToken));
				
				HttpContext.Session.SetString("access_token", response.AccessToken);
				
				var spotifyClient = new SpotifyClient(response.AccessToken);
				
				await spotifyClient.Playlists.Get(id);

				var suggestions = await SpotifyHelper.GetPlaylistSuggestionsAsync(accessToken, id);

				return Ok(new {error = false, suggestions});
			}
			catch (Exception exception)
			{
				_bugsnagClient.Notify(exception);
				return BadRequest(new {error = true, message = exception.Message});
			}
		}

		[HttpPost("custom")]
		public async Task<IActionResult> GetCustomSuggestionsAsync(
			[FromBody] CustomSuggestionsRequest customSuggestionsRequest)
		{
			var accessToken = HttpContext.Session.GetString("access_token");

			if (accessToken == null)
				return Unauthorized(new {error = true, message = "You have been logged out, please refresh the page."});

			if (customSuggestionsRequest.Artists == null || customSuggestionsRequest.Tracks == null)
				return BadRequest(new {error = true, message = "You must provide a list of artists, tracks or both"});

			if (customSuggestionsRequest.Artists.Count == 0 && customSuggestionsRequest.Tracks.Count == 0)
				return BadRequest(new {error = true, message = "You must provide a list of artists, tracks or both"});

			if (customSuggestionsRequest.Artists.Count + customSuggestionsRequest.Tracks.Count > 5)
				return BadRequest(new {error = true, message = "You must only specify a maximum of 5 items"});

			try
			{
				var suggestions = await SpotifyHelper.GetCustomSuggestionsAsync(accessToken,
					customSuggestionsRequest.Artists, customSuggestionsRequest.Tracks);

				return Ok(new {error = false, suggestions});
			}
			catch (APIUnauthorizedException)
			{
				var refreshToken = HttpContext.Session.GetString("refresh_token");

				var response = await new OAuthClient().RequestToken(
					new AuthorizationCodeRefreshRequest(_spotifyConfiguration.ClientId,
						_spotifyConfiguration.ClientSecret, refreshToken));
				
				HttpContext.Session.SetString("access_token", response.AccessToken);
				
				var suggestions = await SpotifyHelper.GetCustomSuggestionsAsync(response.AccessToken,
					customSuggestionsRequest.Artists, customSuggestionsRequest.Tracks);

				return Ok(new {error = false, suggestions});
			}
			catch (Exception exception)
			{
				_bugsnagClient.Notify(exception);
				return BadRequest(new {error = true, message = exception.Message});
			}
		}
	}
}