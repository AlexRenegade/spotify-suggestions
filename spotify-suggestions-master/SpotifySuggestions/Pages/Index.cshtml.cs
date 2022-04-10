using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SpotifyAPI.Web;
using SpotifySuggestions.Models.Configurations;

namespace SpotifySuggestions.Pages
{
	public class IndexModel : PageModel
	{
		private readonly SpotifyConfiguration _spotifyConfiguration;
		private readonly BugsnagConfiguration _bugsnagConfiguration;
		
		public IndexModel(SpotifyConfiguration spotifyConfiguration, BugsnagConfiguration bugsnagConfiguration)
		{
			_spotifyConfiguration = spotifyConfiguration;
			_bugsnagConfiguration = bugsnagConfiguration;
		}
		
		public async Task OnGet()
		{
			var accessToken = HttpContext.Session.GetString("access_token");

			if (accessToken == null) return;

			ViewData["bugsnag-apikey"] = _bugsnagConfiguration.ClientApiKey;
			ViewData["app-version"] = GetType().Assembly.GetName().Version?.ToString();
			
			try
			{
				var spotifyClient = new SpotifyClient(accessToken);
				var user = await spotifyClient.UserProfile.Current();

				ViewData["username"] = user.DisplayName;
			}
			catch (APIUnauthorizedException)
			{
				var refreshToken = HttpContext.Session.GetString("refresh_token");

				var response = await new OAuthClient().RequestToken(
					new AuthorizationCodeRefreshRequest(_spotifyConfiguration.ClientId,
						_spotifyConfiguration.ClientSecret, refreshToken));
				
				HttpContext.Session.SetString("access_token", response.AccessToken);
				
				var spotifyClient = new SpotifyClient(response.AccessToken);
				var user = await spotifyClient.UserProfile.Current();

				ViewData["username"] = user.DisplayName;
			}
		}
	}
}