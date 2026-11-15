(function () {
	// Highlight the download that matches the visitor's operating system.
	var platform = ((navigator.userAgentData && navigator.userAgentData.platform) || navigator.platform || navigator.userAgent || "").toLowerCase();
	var os = platform.indexOf("win") !== -1 ? "windows" :
		(platform.indexOf("mac") !== -1 || platform.indexOf("iphone") !== -1 || platform.indexOf("ipad") !== -1) ? "mac" :
		platform.indexOf("linux") !== -1 ? "linux" :
		null;

	$(".download-asset").each(function () {
		var button = $(this);
		if (button.data("os") === os) {
			button.removeClass("btn-outline-primary").addClass("btn-primary");
		}
	});
})();

// Each button already links to the latest release page; point it straight at the asset when it exists.
$.getJSON("https://api.github.com/repos/ffMathy/Shapeshifter/releases/latest").done(function (json) {
	var assets = json.assets || [];

	$(".download-asset").each(function () {
		var button = $(this);
		var asset = assets.filter(x => x.name === button.data("asset"))[0];
		if (!asset) {
			return;
		}

		var sizeInMegabytes = (asset.size / 1024 / 1024).toFixed(1);
		button.attr("href", asset.browser_download_url);
		button.append($("<span />").addClass("size").text(sizeInMegabytes + " MB"));
	});

	$("#download-version").text(json.tag_name || json.name || "");
});

$.getJSON("/api/patreon/supporters").done(function (json) {
	var goal = 100;

	var supporters = json.sort((a, b) => b.amount - a.amount);
	var totalAmount = 0;
	for (var supporter of supporters) {
		totalAmount += supporter.amount;

		var profilePictureElement = $("<img />");
		profilePictureElement.addClass("float-right");
		profilePictureElement.prop("src", supporter.imageUrl);

		var nameElement = $("<span />");
		nameElement.text(supporter.fullName);

		var pledgeAmountElement = $("<span />");
		pledgeAmountElement.addClass("amount");
		pledgeAmountElement.text("$" + supporter.amount + "/mo");

		var nameContainerElement = $("<a />");
		nameContainerElement.attr("href", supporter.url);
		nameContainerElement.attr("target", "_blank");
		nameContainerElement.addClass("float-left");
		nameContainerElement.append(pledgeAmountElement);
		nameContainerElement.append(nameElement);

		var clearfix = $("<div />");
		clearfix.addClass("clearfix");

		var listElement = $("<li />");
		listElement.addClass("list-group-item");
		listElement.addClass("patron");
		listElement.append(nameContainerElement);
		listElement.append(profilePictureElement);
		listElement.append(clearfix);

		$("#patrons").append(listElement);
	}

	var percentageOfGoal = (100 / goal * totalAmount).toFixed(0);
	$("#funding-progress-bar")
		.css("width", percentageOfGoal + "%")
		.text(percentageOfGoal + "%");

	$("#total-amount").text("$" + totalAmount + "/mo");
	$("#goal").text("$" + goal + "/mo");

	$(".funding-progress").show();
	$(".patrons").show();
});

(function () {
	var currentColorIndex = 0;
	var changeColorCallback = function () {
		var colors = ["red", "blue", "green", "purple"];
		var currentColor = colors[currentColorIndex++ % colors.length];
		$("#screenshot").css("background-image", "url(https://raw.githubusercontent.com/ffMathy/Shapeshifter/master/assets/screenshots/" + currentColor + ".PNG)");
	};
	setInterval(changeColorCallback, 3000);
	changeColorCallback();
})();