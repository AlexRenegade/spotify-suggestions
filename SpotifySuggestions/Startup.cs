using System;
using Bugsnag.AspNet.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SpotifySuggestions.Helpers;
using SpotifySuggestions.Models.Configurations;

namespace SpotifySuggestions
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		private IConfiguration Configuration { get; }

		public void ConfigureServices(IServiceCollection services)
		{
			services.AddDistributedMemoryCache();

			services.AddSession(options =>
			{
				options.IdleTimeout = TimeSpan.FromHours(12);
				options.Cookie.Name = "Session";
				options.Cookie.HttpOnly = true;
				options.Cookie.IsEssential = true;
			});
			
			services.AddSingleton(Configuration.GetSection("SpotifyConfiguration").Get<SpotifyConfiguration>());
			services.AddSingleton(Configuration.GetSection("BugsnagConfiguration").Get<BugsnagConfiguration>());
			services.AddTransient<SpotifyHelper>();

			services.AddBugsnag(configuration =>
			{
				configuration.ApiKey = Configuration.GetSection("BugsnagConfiguration").Get<BugsnagConfiguration>()
					.ServerApiKey;
				configuration.AppVersion = GetType().Assembly.GetName().Version?.ToString();
			});

			services.AddRazorPages();
			services.AddControllers();
			services.AddRouting(options => options.LowercaseUrls = true);
		}

		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
				app.UseDeveloperExceptionPage();
			else
				app.UseExceptionHandler("/Errors/Status500");

			app.UseSession();
			app.UseStatusCodePagesWithReExecute("/Errors/Status{0}");
			app.UseHttpsRedirection();
			app.UseHsts();
			app.UseRouting();
			app.UseAuthorization();
			app.UseStaticFiles();
			app.UseStaticFiles();
			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
				endpoints.MapRazorPages();
			});
		}
	}
}