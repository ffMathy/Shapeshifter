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