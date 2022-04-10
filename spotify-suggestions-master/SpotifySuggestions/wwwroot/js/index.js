$(function () {

	let customTracks = [];
	let customArtists = [];
	let selectedTracks = [];
	let songPlaying = false;

	$('#first-select').change(function () {
		let type = $(this).val();

		switch (type) {
			case "top":
				$('#second-step').html(`
					<p>Select below the time period you want your suggested music to be based on. Some of your top tracks from this period will be used.</p>
					<select id="top-select" class="form-control my-3">
						<option selected disabled>Select...</option>
						<option value="long">Long Term (1+ years)</option>
						<option value="medium">Medium Term (~6 months)</option>
						<option value="short">Short Term (~4 weeks)</option>
					</select>
				`);
				break;
			case "public-playlist":
				$('#second-step').html(`
					<p>In the box below, enter the URL to a public playlist, or one you own. New music suggestions will be based off this.</p>
					<input class="form-control my-3" spellcheck="false" id="playlist-url" placeholder="Paste in a playlist URL..." />
				`);
				break;
			case "private-playlist":
				$('#second-step').html(`<p>From the dropdown below, select a playlist you want to use to produce new music suggestions.</p>`);
				$.ajax({
					url: `/api/playlists/me`,
					method: "GET",
					success: function (result) {
						let html = `<select id="playlist-select" class="form-control my-3">`;
						for (let playlist of result.playlists) {
							html += `<option value="${playlist.id}">${playlist.name} (${playlist.count} songs)</option>`;
						}
						html += `</select>`;
						$('#second-step').append(html);
					},
					error: function () {
						window.location = window.location;
					}
				});
				break;
			case "custom":
				$('#second-step').html(`
					<p>Use the box below to search for artists/songs that you enjoy. You can add up to 5, which is what your suggestions will be based on. Click a track/artist to add it to your seed list.</p>
					<input class="form-control my-3" spellcheck="false" id="spotify-search" placeholder="Search for artists/songs..." />
					<p id="custom-error" class="text-red"></p>
					<h4 class="mt-3">Seed List</h4><hr class="mb-3 mt-2"/>
					<ul id="seed-list" class="list-unstyled"></ul>
					<div id="custom-results"></div>
				`);

				let typingTimer;

				$('#spotify-search').keyup(async function () {
					if (customTracks.length + customArtists.length >= 5) {
						displayError("You already have the maximum number of seed tracks/artists.");
						$('#spotify-search').attr('disabled', true);
						return;
					}

					let input = $(this);
					clearTimeout(typingTimer);
					if (input.val())
						typingTimer = setTimeout(finishedTypingSpotifySearch, 500, input.val());
					else
						$('#custom-results').empty();

					async function finishedTypingSpotifySearch(text) {
						let result = await spotifySearch(text);

						$('#custom-results').empty();

						if (result.tracks.length === 0 && result.artists.length === 0) {
							$('#custom-results').html(`<p class="font-weight-700">No results</p>`);
							return;
						}

						$('#custom-results').append(`<h4 class="mt-3">Tracks</h4><hr class="mb-3 mt-2"/>`);
						if (result.tracks.length === 0) {
							$('#custom-results').append(`<p>No results</p>`);
						} else {
							let html = `<div class="row no-gutters">`
							for (const track of result.tracks) {
								html += `
									<div class="col-md-3 mb-3">
										<div class="spotify-card custom-card row no-gutters" data-type="track" data-id="${track.id}">
											<div class="col-3">
												<img class="w-100 spotify-image" src="${track.albumCover}"/>
											</div>
											<div class="col-9 pl-2 pr-2 my-auto">
												<h5 class="track-name">${track.trackName.replace(/ *\([^)]*\)*/g, "").replace(/ - [Rr]ecorded [Aa]t.*/g, "")}</h5>
												<p class="my-0">${track.artistNames.substring(0, 24)}${track.artistNames.length > 24 ? '...' : ''}</p>
											</div>
										</div>
									</div>
								`;
							}
							html += `</div>`;
							$('#custom-results').append(html);
						}

						$('#custom-results').append(`<h4 class="mt-3">Artists</h4><hr class="mb-3 mt-2"/>`);
						if (result.artists.length === 0) {
							$('#custom-results').append(`<p>No results</p>`);
						} else {
							let html = `<div class="row no-gutters">`
							for (const artist of result.artists) {
								html += `
									<div class="col-md-3 mb-3">
										<div class="spotify-card custom-card row no-gutters" data-type="artist" data-id="${artist.id}">
											<div class="col-3">
												<img class="w-100" src="${artist.image}"/>
											</div>
											<div class="col-9 pl-2 pr-2 my-auto">
												<h5 class="my-0 artist-name" class="my-0">${artist.name}</h5>
											</div>
										</div>
									</div>
								`;
							}
							html += `</div>`;
							$('#custom-results').append(html);
						}
					}
				});

				break;
		}
	});

	$(document).on("click", ".custom-card", function () {
		let type = $(this).attr('data-type');
		let id = $(this).attr('data-id');
		let name;
		$("#custom-error").html("");

		if (type === "track") {
			name = $(this).find('.track-name').html();

			if (customTracks.find(x => x.id === id)) {
				$("#custom-error").html("You've already added this track to your seed list.");
				return;
			}

			customTracks.push({
				id, name
			});
		} else {
			name = $(this).find('.artist-name').html();

			if (customArtists.find(x => x.id === id)) {
				$("#custom-error").html("You've already added this artist to your seed list.");
				return;
			}

			customArtists.push({
				id, name
			});
		}

		$("#seed-list").append(`<li class="mb-2" data-id="${id}" data-type="${type === "track" ? "track" : "artist"}"><strong>${name}</strong> - ${type === "track" ? "Track" : "Artist"} <button class="btn btn-red btn-icon btn-xs custom-option ml-1"><i class="fal fa-trash-alt"></i></button></li>`);

		$('#custom-results').empty();
		$('#spotify-search').val('');

		if (customTracks.length + customArtists.length >= 5) {
			$('#spotify-search').prop('placeholder', 'Max seed tracks/artists reached');
			$('#spotify-search').attr('disabled', true);
		}
	});

	$(document).on("click", ".custom-option", function () {
		let id = $(this).closest("li").attr("data-id");
		let type = $(this).closest("li").attr("data-type");

		$(this).closest("li").remove();

		if (type === "artist") {
			customArtists = customArtists.filter(x => x.id !== id);
		} else {
			customTracks = customTracks.filter(x => x.id !== id);
		}

		if (customTracks.length + customArtists.length < 5) {
			$('#spotify-search').prop('placeholder', 'Search for artists/songs...');
			$('#spotify-search').removeAttr('disabled');
		}
	});

	$(document).on("click", "#select-all-button", function () {
		let addButtons = $(".add-button");
		
		if (addButtons.toArray().every(function (addButton) { return $(addButton).attr('src') === '/img/check.svg'; })) {
			for (let addButton of addButtons) {
				let element = $(addButton);
				
				element.attr('src', '/img/plus.svg');

				let card = element.closest('.spotify-card');
				let id = card.attr('data-id');

				selectedTracks = selectedTracks.filter(x => x !== id);
			}

			$("#select-all-button").text("Select All");
		} else {
			for (let addButton of addButtons) {
				let element = $(addButton);

				let src = element.attr('src');

				if (src !== '/img/check.svg') {
					element.attr('src', '/img/check.svg');

					let card = element.closest('.spotify-card');
					let id = card.attr('data-id');

					selectedTracks.push(id);
				}
			}

			$("#select-all-button").text("Deselect All");
		}		
	});

	$(document).on("click", "#reset-button", function () {
		$('#post-search').empty();
		$('#post-search').attr('hidden', true);

		customArtists = [];
		customTracks = [];
		selectedTracks = [];

		$('#pre-search').removeAttr('hidden');
	});

	$('#music-button').click(function () {
		let button = $(this);
		disableButton(button);
		let type = $('#first-select').val();

		let playlistId;

		switch (type) {
			case "top":
				let term = $('#top-select').val();
				if (term !== "long" && term !== "medium" && term !== "short") {
					enableButton(button);
					displayError("You must select a valid term to generate suggestions.");
				} else {
					$.ajax({
						url: `/api/suggestions/top?term=${term}`,
						method: "GET",
						success: function (result) {
							enableButton(button);
							displayResults(result["suggestions"]);
						},
						error: function (error) {
							enableButton(button);
							if (error.responseJSON) {
								displayError(error.responseJSON.message);
							} else {
								displayError("Unknown error occured.");
							}
						}
					});
				}
				break;
			case "public-playlist":
				const playlistRegex = new RegExp("https:\/\/open.spotify.com\/playlist/([^\?]+)([?]*.*)");

				let playlistUrl = $('#playlist-url').val();
				if (isNullOrEmpty(playlistUrl)) {
					enableButton(button);
					displayError("You must enter a playlist URL.");
					return;
				} else if (!playlistRegex.test(playlistUrl)) {
					enableButton(button);
					displayError("You must enter a valid playlist URL.");
					return;
				}

				playlistId = playlistRegex[Symbol.match](playlistUrl)[1];

				$.ajax({
					url: `/api/suggestions/playlist?id=${playlistId}`,
					method: "GET",
					success: function (result) {
						enableButton(button);
						displayResults(result["suggestions"]);
					},
					error: function (error) {
						enableButton(button);
						if (error.responseJSON) {
							displayError(error.responseJSON.message);
						} else {
							displayError("Unknown error occured.");
						}
					}
				});

				break;
			case "private-playlist":
				playlistId = $('#playlist-select').val();
				
				$.ajax({
					url: `/api/suggestions/playlist?id=${playlistId}`,
					method: "GET",
					success: function (result) {
						enableButton(button);
						displayResults(result["suggestions"]);
					},
					error: function (error) {
						enableButton(button);
						if (error.responseJSON) {
							displayError(error.responseJSON.message);
						} else {
							displayError("Unknown error occured.");
						}
					}
				});
				
				break;
			case "custom":
				if (customTracks.length === 0 && customArtists.length === 0) {
					enableButton(button);
					displayError("You must enter some seed tracks/artists.");
					return;
				} else if (customTracks.length + customArtists.length > 5) {
					enableButton(button);
					displayError("You have too many seed tracks/artists, please refresh the page.");
					return;
				}

				$.ajax({
					url: `/api/suggestions/custom`,
					method: "POST",
					headers: {
						"Content-Type": "application/json"
					},
					data: JSON.stringify({
						artists: customArtists.map(function (a) {
							return a.id;
						}),
						tracks: customTracks.map(function (t) {
							return t.id;
						})
					}),
					success: function (result) {
						enableButton(button);
						displayResults(result["suggestions"]);
					},
					error: function (error) {
						enableButton(button);
						if (error.responseJSON) {
							displayError(error.responseJSON.message);
						} else {
							displayError("Unknown error occured.");
						}
					}
				});

				break;
			default:
				enableButton(button);
				displayError("You must select a valid option to generate suggestions.");
				break;
		}
	});

	$(document).on("click", ".add-button", function () {
		let element = $(this);

		let src = element.attr('src');

		if (src !== '/img/check.svg') {
			element.attr('src', '/img/check.svg');

			let card = element.closest('.spotify-card');
			let id = card.attr('data-id');

			selectedTracks.push(id);
		} else {
			element.attr('src', '/img/plus.svg');

			let card = element.closest('.spotify-card');
			let id = card.attr('data-id');

			selectedTracks = selectedTracks.filter(x => x !== id);
		}

		let addButtons = $(".add-button");
		
		if (addButtons.toArray().every(function (addButton) { return $(addButton).attr('src') === '/img/check.svg'; })) {
			$("#select-all-button").text("Deselect All");
		} else {
			$("#select-all-button").text("Select All");
		}
	});

	$(document).on("click", "#refresh-button", function () {
		let button = $(this);
		disableButton(button);
		let type = $('#first-select').val();
		
		let playlistId;
		
		switch (type) {
			case "top":
				let term = $('#top-select').val();
				if (term !== "long" && term !== "medium" && term !== "short") {
					enableButton(button);
					displayError("You must select a valid term to generate suggestions.");
				} else {
					$.ajax({
						url: `/api/suggestions/top?term=${term}`,
						method: "GET",
						success: function (result) {
							enableButton(button);
							displayResults(result["suggestions"]);
						},
						error: function (error) {
							enableButton(button);
							if (error.responseJSON) {
								displayError(error.responseJSON.message);
							} else {
								displayError("Unknown error occured.");
							}
						}
					});
				}
				break;
			case "public-playlist":
				const playlistRegex = new RegExp("https:\/\/open.spotify.com\/playlist/([^\?]+)([?]*.*)");

				let playlistUrl = $('#playlist-url').val();
				if (isNullOrEmpty(playlistUrl)) {
					enableButton(button);
					displayError("You must enter a playlist URL.");
					return;
				} else if (!playlistRegex.test(playlistUrl)) {
					enableButton(button);
					displayError("You must enter a valid playlist URL.");
					return;
				}

				playlistId = playlistRegex[Symbol.match](playlistUrl)[1];

				$.ajax({
					url: `/api/suggestions/playlist?id=${playlistId}`,
					method: "GET",
					success: function (result) {
						enableButton(button);
						displayResults(result["suggestions"]);
					},
					error: function (error) {
						enableButton(button);
						if (error.responseJSON) {
							displayError(error.responseJSON.message);
						} else {
							displayError("Unknown error occured.");
						}
					}
				});

				break;
			case "private-playlist":
				playlistId = $("#playlist-select").val();
					
				$.ajax({
					url: `/api/suggestions/playlist?id=${playlistId}`,
					method: "GET",
					success: function (result) {
						enableButton(button);
						displayResults(result["suggestions"]);
					},
					error: function (error) {
						enableButton(button);
						if (error.responseJSON) {
							displayError(error.responseJSON.message);
						} else {
							displayError("Unknown error occured.");
						}
					}
				});
				
				break;
			case "custom":
				if (customTracks.length === 0 && customArtists.length === 0) {
					enableButton(button);
					displayError("You must enter some seed tracks/artists.");
					return;
				} else if (customTracks.length + customArtists.length > 5) {
					enableButton(button);
					displayError("You have too many seed tracks/artists, please refresh the page.");
					return;
				}

				$.ajax({
					url: `/api/suggestions/custom`,
					method: "POST",
					headers: {
						"Content-Type": "application/json"
					},
					data: JSON.stringify({
						artists: customArtists.map(function (a) {
							return a.id;
						}),
						tracks: customTracks.map(function (t) {
							return t.id;
						})
					}),
					success: function (result) {
						enableButton(button);
						displayResults(result["suggestions"]);
					},
					error: function (error) {
						enableButton(button);
						if (error.responseJSON) {
							displayError(error.responseJSON.message);
						} else {
							displayError("Unknown error occured.");
						}
					}
				});

				break;
			default:
				enableButton(button);
				displayError("You must select a valid option to generate suggestions.");
				break;
		}
	});

	$(document).on("click", ".play-button", function () {
		let playElement = $(this);
		let src = playElement.attr('src');

		if (src === "/img/play.png") {
			if (songPlaying) {
				$('#song')[0].pause();
				let elements = $('.play-button');

				for (const element of elements) {
					if ($(element).attr('src') === "/img/pause.png") {
						$(element).attr('src', '/img/play.png');
					}
				}
			}

			let card = playElement.closest('.spotify-card');
			$('#song').html(`<source src="${card.attr('data-audio')}" type="audio/mpeg">`)
			$('#song')[0].volume = 0.2;
			$('#song')[0].load();
			$('#song')[0].play();

			playElement.attr('src', '/img/pause.png');
			songPlaying = true;
		} else {
			playElement.attr('src', '/img/play.png');
			$('#song')[0].pause();
			songPlaying = false;
		}
	});

	$(document).on("click", "#generate-button", function () {
		let button = $(this);
		let name = $('#playlist-name').val();
		let isPrivate = $('#playlist-private').prop("checked");

		disableButton(button);

		$.ajax({
			url: `/api/playlists/create`,
			method: "POST",
			headers: {
				"Content-Type": "application/json"
			},
			data: JSON.stringify({
				ids: selectedTracks,
				name,
				isPrivate
			}),
			success: function (result) {
				enableButton(button);
				$('#playlist-success').html(`<p class="text-green m-0">Successfully created playlist, click <a target="_blank" href="https://open.spotify.com/playlist/${result.playlist}">here</a> to view it.</p>`)
			},
			error: function (error) {
				enableButton(button);
				if (error.responseJSON.message) {
					displayPlaylistError(error.responseJSON.message)
				} else {
					displayPlaylistError("An unknown error occured, please try again or refresh the page.");
				}
			}
		})
	});

	$(document).on("click", "#add-existing-button", function () {
		let button = $(this);
		let playlist = $("#existing-playlist-select").val();

		disableButton(button);

		$.ajax({
			url: `/api/playlists/append`,
			method: "PATCH",
			headers: {
				"Content-Type": "application/json"
			},
			data: JSON.stringify({
				ids: selectedTracks,
				playlistId: playlist
			}),
			success: function (result) {
				enableButton(button);
				$('#playlist-success').html(`<p class="text-green m-0">Successfully added to playlist, click <a target="_blank" href="https://open.spotify.com/playlist/${result.playlist}">here</a> to view it.</p>`)
			},
			error: function (error) {
				enableButton(button);
				if (error.responseJSON.message) {
					displayPlaylistError(error.responseJSON.message)
				} else {
					displayPlaylistError("An unknown error occured, please try again or refresh the page.");
				}
			}
		})
	});
});

function displayError(message) {
	let element = $('#music-error');

	element.removeAttr('hidden');
	element.html(message);
	setTimeout(function () {
		element.html("");
		element.attr('hidden', true);
	}, 4000);
}

function displayPlaylistError(message) {
	let element = $('#playlist-error');

	element.removeAttr('hidden');
	element.html(message);
	setTimeout(function () {
		element.html("");
		element.attr('hidden', true);
	}, 4000);
}

function disableButton(element) {
	element.attr("disabled", "true");
}

function enableButton(element) {
	element.removeAttr("disabled");
}

function isNullOrEmpty(string) {
	return !string || string === "";
}

function spotifySearch(text) {
	return new Promise(async (resolve, reject) => {
		try {
			let request = $.ajax({
				url: `/api/query?search=${text}`
			});

			let response = await request;

			if (!response["query"] || response["error"] === true) {
				displayError("An error occured when searching. Please try again or refresh the page.");
			}

			resolve(response["query"]);
		} catch (err) {
			reject(err);
		}
	});
}

function displayResults(suggestions) {
	$('#pre-search').attr('hidden', true);

	$('#post-search').html(`<h4 id="suggestions">Suggestions</h4><hr class="mb-3 mt-2"/><p class="text-xs">Click the play icons to listen to a preview of the songs listed below. To add a song, click the '+' icon. Once you've finished looking through songs, either click the 'Refresh' button or generate your playlist using the button at the bottom of the page.</p><hr class="mb-3"/><button class="text-xs w-100 py-1 btn btn-green mb-3" id="select-all-button">Select All</button>`);

	let html = `<div class="row no-gutters">`

	for (const suggestion of suggestions) {
		
		if (!suggestion.previewUrl) {
			html += `
				<div class="col-md-3 mb-3">
					<div class="spotify-card row no-gutters" data-type="track" data-id="${suggestion.id}">
						<img src="/img/plus.svg" width="24px" height="24px" class="add-button">
						<div class="col-3">
							<img class="w-100 spotify-image" src="/img/nomusic.png" style="background-image: url('${suggestion.albumCover}'); background-size: contain"/>
						</div>
						<div class="col-9 pl-2 pr-2 my-auto">
							<h5 class="track-name">${suggestion.trackName.replace(/ *\([^)]*\)*/g, "").replace(/ - [Rr]ecorded [Aa]t.*/g, "")}</h5>
							<p class="text-xs my-0">${suggestion.artistNames.substring(0, 24)}${suggestion.artistNames.length > 24 ? '...' : ''}</p>
						</div>
					</div>
				</div>
			`;
		} else {
			html += `
				<div class="col-md-3 mb-3">
					<div class="spotify-card row no-gutters" data-type="track" data-id="${suggestion.id}" data-audio="${suggestion.previewUrl}">
						<img src="/img/plus.svg" width="24px" height="24px" class="add-button">
						<div class="col-3">
							<img class="w-100 spotify-image play-button" src="/img/play.png" style="background-image: url('${suggestion.albumCover}'); background-size: contain"/>
						</div>
						<div class="col-9 pl-2 pr-2 my-auto">
							<h5 class="track-name">${suggestion.trackName.replace(/ *\([^)]*\)*/g, "").replace(/ - [Rr]ecorded [Aa]t.*/g, "")}</h5>
							<p class="text-xs my-0">${suggestion.artistNames.substring(0, 24)}${suggestion.artistNames.length > 24 ? '...' : ''}</p>
						</div>
					</div>
				</div>
			`;
		}
	}

	html += `</div>`;

	$.ajax({
		url: `/api/playlists/me`,
		method: "GET",
		success: function (result) {
			let html = "";
			for (let playlist of result.playlists) {
				html += `<option value="${playlist.id}">${playlist.name} (${playlist.count} songs)</option>`;
			}
			html += "";
			$('#existing-playlist-select').append(html);
		}
	});
	
	$('#post-search').append(`${html}<button id="refresh-button" class="text-xs w-100 py-1 btn btn-green"><i class="far fa-sync-alt"></i> Refresh</button><hr class="my-3"/>
		<ul class="nav nav-tabs" role="tablist">
			<li class="nav-item"><a class="nav-link active" id="generate-new-tab" data-toggle="tab" href="#generate-new-pane" role="tab">Generate new playlist</a></li>
			<li class="nav-item"><a class="nav-link" id="add-existing-tab" data-toggle="tab" href="#add-existing-pane" role="tab">Add to existing playlist</a></li>
		</ul>
		<div class="tab-content pt-3">
			<div class="tab-pane fade show active" id="generate-new-pane" role="tabpanel">
				<input type="text" class="form-control mb-2" placeholder="Playlist Name..." id="playlist-name">
				<div class="custom-control custom-checkbox">
					<input class="custom-control-input" id="playlist-private" type="checkbox">
					<label class="custom-control-label" for="playlist-private">Make playlist private?</label>
				</div>
				<button class="btn mt-2 btn-green w-100" id="generate-button">Generate Playlist!</button>
			</div>
			<div class="tab-pane fade" id="add-existing-pane" role="tabpanel">
				<select id="existing-playlist-select" class="form-control mb-2"></select>
				<button class="btn mt-2 btn-green w-100" id="add-existing-button">Add to Playlist!</button>
			</div>
		</div>
		<div id="playlist-error" class="text-red mt-3"></div>
		<div id="playlist-success" class="mt-3"></div>
		<button class="btn mt-3 btn-warning w-100" id="reset-button">Start again</button>`);

	$('#post-search').removeAttr('hidden');

	document.getElementById('suggestions').scrollIntoView(true);
}