$(function () {
	let apiKeyInput = $("#bugsnag-apikey");
	let appVersionInput = $("#app-version");

	let apiKey = apiKeyInput.val();
	let appVersion = apiKeyInput.val();

	apiKeyInput.remove();
	appVersionInput.remove();

	Bugsnag.start({
		apiKey: apiKey,
		appVersion: appVersion,
		autoTrackSessions: false
	});
});
