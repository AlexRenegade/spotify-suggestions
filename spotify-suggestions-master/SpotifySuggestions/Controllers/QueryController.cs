using System;
using System.Threading.Tasks;
using Bugsnag;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SpotifyAPI.Web;
using SpotifySuggestions.Helpers;
using SpotifySuggestions.Models.Configurations;

namespace SpotifySuggestions.Controllers
{
	[Route("api/query")]
	[ApiController]
	public class QueryController : Controller
	{
		private readonly SpotifyConfiguration _spotifyConfiguration;
		private readonly IClient _bugsnagClient;
		
		public QueryController(SpotifyConfiguration spotifyConfiguration, IClient bugsnagClient)
		{
			_spotifyConfiguration = spotifyConfiguration;
			_bugsnagClient = bugsnagClient;
		}
		
		public async Task<IActionResult> QueryAsync(string search)
		{
			var accessToken = HttpContext.Session.GetString("access_token");

			if (accessToken == null)
				return Unauthorized(new {error = true, message = "You have been logged out, please refresh the page."});

			try
			{
				var query = await SpotifyHelper.QueryAsync(accessToken, search);

				return Ok(new {error = false, query});
			}
			catch (APIUnauthorizedException)
			{
				var refreshToken = HttpContext.Session.GetString("refresh_token");

				var response = await new OAuthClient().RequestToken(
					new AuthorizationCodeRefreshRequest(_spotifyConfiguration.ClientId,
						_spotifyConfiguration.ClientSecret, refreshToken));
				
				HttpContext.Session.SetString("access_token", response.AccessToken);
				
				var query = await SpotifyHelper.QueryAsync(response.AccessToken, search);

				return Ok(new {error = false, query});
			}
			catch (Exception exception)
			{
				_bugsnagClient.Notify(exception);
				return BadRequest(new {error = true, message = exception.Message});
			}
		}
	}
}