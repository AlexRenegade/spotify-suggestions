using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SpotifySuggestions.Models.Configurations;

namespace SpotifySuggestions.Controllers
{
	[Route("api")]
	[ApiController]
	public class AuthenticationController : Controller
	{
		private readonly SpotifyConfiguration _spotifyConfiguration;

		public AuthenticationController(SpotifyConfiguration spotifyConfiguration)
		{
			_spotifyConfiguration = spotifyConfiguration;
		}
		
		[HttpGet("login")]
		public IActionResult Login()
		{
			var scopes =
				HttpUtility.UrlEncode(
					_spotifyConfiguration.Scopes.Aggregate(string.Empty, (current, scope) => current + $"{scope} "));

			return Redirect("https://accounts.spotify.com/authorize?" +
			                $"client_id={_spotifyConfiguration.ClientId}&" +
			                "response_type=code&" +
			                $"redirect_uri={Environment.GetEnvironmentVariable("baseUrl")}/api/callback&" +
			                $"scope={scopes}&prompt=consent");
		}
		
		[HttpGet("callback")]
		public async Task<IActionResult> CallbackAsync(string code, string error)
		{
			if (error == null)
			{
				var httpClient = new HttpClient();
				var request = new HttpRequestMessage
				{
					RequestUri = new Uri("https://accounts.spotify.com/api/token"),
					Method = HttpMethod.Post,
					Content = new FormUrlEncodedContent(new[]
					{
						new KeyValuePair<string, string>("grant_type", "authorization_code"),
						new KeyValuePair<string, string>("code", code),
						new KeyValuePair<string, string>("redirect_uri",
							Environment.GetEnvironmentVariable("baseUrl") + "/api/callback"),
						new KeyValuePair<string, string>("client_id", _spotifyConfiguration.ClientId),
						new KeyValuePair<string, string>("client_secret", _spotifyConfiguration.ClientSecret)
					})
				};

				var response = await httpClient.SendAsync(request);
				var responseObject = JsonConvert.DeserializeObject<JObject>(await response.Content.ReadAsStringAsync());
				var accessToken = responseObject.SelectToken("access_token");
				var refreshToken = responseObject.SelectToken("refresh_token");

				if (accessToken != null && refreshToken != null)
				{
					HttpContext.Session.SetString("access_token", accessToken.ToString());
					HttpContext.Session.SetString("refresh_token", refreshToken.ToString());
				}

				return Redirect("/");
			}

			return Redirect("/");
		}
		
		[HttpGet("logout")]
		public IActionResult Logout()
		{
			HttpContext.Session.Clear();

			return Redirect("/");
		}
	}
}