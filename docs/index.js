$.getJSON("https://api.github.com/repos/ffMathy/Shapeshifter/releases/latest").done(function (json) {
  var version = json.name.split("-")[1];
  var asset = json.assets.filter(x => x.name === "Shapeshifter.exe")[0];
  var sizeInMegabytes = (asset.size / 1024 / 1024).toFixed(1);

  $("#download-button")
    .attr("href", asset.browser_download_url)
    .removeClass("disabled"); 
    
  $("#download-size").text(sizeInMegabytes + " MB");
    
  $("#download-version").text(version);
});

$.getJSON("https://shapeshifter.azurewebsites.net/api/patreon/supporters").done(function(json) {
  var supporters = json.sort((a, b) => b.amount - a.amount);
  var amountSum = 0;
  for(var supporter of supporters) {
    amountSum += supporter.amount;

    var profilePictureElement = $("<img />");
    profilePictureElement.addClass("float-right");
    profilePictureElement.prop("src", supporter.imageUrl);

    var nameElement = $("<span />");
    nameElement.text(supporter.fullName);

    var pledgeAmountElement = $("<span />");
    pledgeAmountElement.addClass("amount");
    pledgeAmountElement.text("$" + supporter.amount + "/mo");

    var nameContainerElement = $("<span />");
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

  
});

(function() {
  var currentColorIndex = 0;
  var changeColorCallback = function() {
    var colors = ["red", "blue", "green", "purple"];
    var currentColor = colors[currentColorIndex++ % colors.length];
    $("#screenshot").css("background-image", "url(https://github.com/ffMathy/Shapeshifter/raw/master/assets/screenshots/" + currentColor + ".PNG)");
  };
  setInterval(changeColorCallback, 3000);
  changeColorCallback();
})();