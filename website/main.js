// Shapeshifter website: picks the download for the visitor's platform, shows the latest version and runs the
// screenshot tabs. Everything here is an enhancement: without JavaScript the page offers a Windows download in the
// hero, the full download list and every screenshot.
(function () {
  'use strict';

  var DOWNLOAD = 'https://github.com/ffMathy/Shapeshifter/releases/latest/download/';
  var LATEST_API = 'https://api.github.com/repos/ffMathy/Shapeshifter/releases/latest';
  var VERSION_CACHE_KEY = 'shapeshifter:latest-release';
  var VERSION_CACHE_MS = 60 * 60 * 1000;

  var BUILDS = {
    'win-x64': { os: 'windows', name: 'Windows', arch: 'x64', file: 'Shapeshifter-win-x64.zip' },
    'win-arm64': { os: 'windows', name: 'Windows', arch: 'Arm64', file: 'Shapeshifter-win-arm64.zip' },
    'osx-arm64': { os: 'macos', name: 'Mac', arch: 'Apple silicon', file: 'Shapeshifter-osx-arm64.zip' },
    'osx-x64': { os: 'macos', name: 'Mac', arch: 'Intel', file: 'Shapeshifter-osx-x64.zip' },
    'linux-x64': { os: 'linux', name: 'Linux', arch: 'x64', file: 'Shapeshifter-linux-x64.tar.gz' },
    'linux-arm64': { os: 'linux', name: 'Linux', arch: 'Arm64', file: 'Shapeshifter-linux-arm64.tar.gz' }
  };

  var LOOKS = {
    windows: {
      alt: 'The Shapeshifter picker on Windows: a list of five copied items on the left, including text, an image and files, and the actions for the selected item on the right, with keyboard hints along the bottom.'
    },
    macos: {
      alt: 'The Shapeshifter picker on macOS, in the Liquid Glass look: five copied items on the left and the actions for the selected item on the right, with keyboard hints along the bottom.'
    },
    linux: {
      alt: 'The Shapeshifter picker on Linux, in the libadwaita look: five copied items on the left and the actions for the selected item on the right, with keyboard hints along the bottom.'
    }
  };

  // ---- Platform detection ------------------------------------------------------------------------------------

  /** The operating system and CPU family from what the browser tells us synchronously. */
  function detectBasic() {
    var ua = navigator.userAgent || '';
    var uaPlatform = navigator.platform || '';
    var data = navigator.userAgentData;
    var os = null;

    if (data && typeof data.platform === 'string' && data.platform) {
      var p = data.platform.toLowerCase();
      if (data.mobile) os = 'mobile';
      else if (p === 'windows') os = 'windows';
      else if (p === 'macos') os = 'macos';
      else if (p === 'linux') os = 'linux';
      else if (p === 'android' || p === 'ios') os = 'mobile';
      else os = 'unknown'; // Chrome OS and anything else we have no build for
    }

    if (!os) {
      if (/Android|iPhone|iPad|iPod|Windows Phone|Mobi/i.test(ua)) os = 'mobile';
      // iPadOS asks for the desktop site and claims to be a Mac; a touch screen gives it away.
      else if (/Macintosh|Mac OS X/.test(ua) && navigator.maxTouchPoints > 1) os = 'mobile';
      else if (/Windows/.test(ua) || /^Win/.test(uaPlatform)) os = 'windows';
      else if (/Macintosh|Mac OS X/.test(ua) || /^Mac/.test(uaPlatform)) os = 'macos';
      else if (/CrOS/.test(ua)) os = 'unknown';
      else if (/Linux|X11/.test(ua) || /Linux/.test(uaPlatform)) os = 'linux';
      else os = 'unknown';
    }

    var arch = null;
    if (/aarch64|arm64|armv8/i.test(ua + ' ' + uaPlatform)) arch = 'arm';
    else if (/x86_64|x64|amd64|Win64|WOW64|i686/i.test(ua + ' ' + uaPlatform)) arch = 'x86';
    // Every Mac browser reports an Intel user agent, even on Apple silicon, so it says nothing there.
    if (os === 'macos') arch = null;

    return { os: os, arch: arch };
  }

  /** The CPU family from client hints (Chromium browsers), or null. */
  function hintedArchitecture() {
    var data = navigator.userAgentData;
    // An empty platform means the user agent was overridden (device emulation): the hints describe another machine.
    if (!data || !data.platform || typeof data.getHighEntropyValues !== 'function') return Promise.resolve(null);
    return data.getHighEntropyValues(['architecture', 'bitness']).then(function (values) {
      if (values.architecture === 'arm') return 'arm';
      if (values.architecture === 'x86') return 'x86';
      return null;
    }, function () { return null; });
  }

  /** On a Mac, the GPU reveals Apple silicon ("Apple M1") versus Intel ("Intel Iris", "AMD Radeon"). */
  function macArchitectureFromGpu() {
    try {
      var canvas = document.createElement('canvas');
      var gl = canvas.getContext('webgl') || canvas.getContext('experimental-webgl');
      if (!gl) return null;
      var info = gl.getExtension('WEBGL_debug_renderer_info');
      var renderer = String(gl.getParameter(info ? info.UNMASKED_RENDERER_WEBGL : gl.RENDERER) || '');
      if (/Apple M\d|Apple A\d/i.test(renderer)) return 'arm';
      if (/Intel|AMD|Radeon|NVIDIA|GeForce/i.test(renderer)) return 'x86';
    } catch (e) { /* no WebGL: keep the default */ }
    return null; // "Apple GPU" (Safari) and others do not tell
  }

  function detect() {
    var basic = detectBasic();
    if (basic.os !== 'windows' && basic.os !== 'macos' && basic.os !== 'linux') return Promise.resolve(basic);
    return hintedArchitecture().then(function (hinted) {
      var arch = hinted || basic.arch;
      if (!arch && basic.os === 'macos') arch = macArchitectureFromGpu();
      return { os: basic.os, arch: arch };
    });
  }

  function chooseBuild(platform) {
    switch (platform.os) {
      case 'windows': return platform.arch === 'arm' ? 'win-arm64' : 'win-x64';
      case 'macos': return platform.arch === 'x86' ? 'osx-x64' : 'osx-arm64'; // Apple silicon unless we know better
      case 'linux': return platform.arch === 'arm' ? 'linux-arm64' : 'linux-x64';
      default: return null;
    }
  }

  // ---- Hero download button ----------------------------------------------------------------------------------

  function el(tag, text, attributes) {
    var node = document.createElement(tag);
    if (text) node.textContent = text;
    for (var key in attributes || {}) node.setAttribute(key, attributes[key]);
    return node;
  }

  function link(text, href) { return el('a', text, { href: href }); }

  function setNote(parts) {
    var note = document.getElementById('cta-note');
    if (!note) return;
    note.textContent = '';
    parts.forEach(function (part) {
      note.appendChild(typeof part === 'string' ? document.createTextNode(part) : part);
    });
  }

  function extension(file) { return /\.tar\.gz$/.test(file) ? '.tar.gz' : '.zip'; }

  function showBuild(id) {
    var build = BUILDS[id];
    var button = document.getElementById('primary-download');
    button.href = DOWNLOAD + build.file;
    button.setAttribute('data-asset', build.file);
    document.getElementById('primary-label').textContent = 'Download for ' + build.name;
    document.getElementById('primary-detail').textContent = build.arch + ' · ' + extension(build.file);

    var alternative = {
      'win-x64': ['Windows on Arm? ', 'Get the Arm64 build', 'win-arm64'],
      'win-arm64': ['Not on Arm? ', 'Get the x64 build', 'win-x64'],
      'osx-arm64': ['Intel Mac? ', 'Get the Intel build', 'osx-x64'],
      'osx-x64': ['Apple silicon Mac? ', 'Get the Apple silicon build', 'osx-arm64'],
      'linux-x64': ['On Arm? ', 'Get the Arm64 build', 'linux-arm64'],
      'linux-arm64': ['Not on Arm? ', 'Get the x64 build', 'linux-x64']
    }[id];
    var parts = [alternative[0], link(alternative[1], DOWNLOAD + BUILDS[alternative[2]].file), '.'];

    if (build.os === 'macos') {
      parts.push(el('br'), 'macOS asks you to allow Shapeshifter in System Settings → Privacy & Security → Accessibility, which it needs to see and send the paste shortcut.');
    } else if (build.os === 'linux') {
      parts.push(el('br'), 'Full support on X11; ', link('limited on Wayland', '#platforms'), '.');
    }
    setNote(parts);

    var group = document.querySelector('.download-group[data-os="' + build.os + '"]');
    if (group) group.classList.add('is-current');
  }

  function showAllDownloads(platform) {
    var button = document.getElementById('primary-download');
    button.href = '#download';
    button.removeAttribute('data-asset');
    document.getElementById('primary-label').textContent = 'See all downloads';
    document.getElementById('primary-detail').textContent = 'Windows, macOS and Linux';
    var secondary = document.getElementById('secondary-download');
    if (secondary) secondary.hidden = true;
    setNote([platform.os === 'mobile'
      ? 'Shapeshifter is a desktop app for Windows, macOS and Linux. Open this page on your computer to download it.'
      : 'Shapeshifter is a desktop app for Windows, macOS and Linux.']);
  }

  function useModifierName(os) {
    if (os !== 'macos') return;
    var nodes = document.querySelectorAll('[data-mod]');
    for (var i = 0; i < nodes.length; i++) nodes[i].textContent = 'Cmd';
  }

  function showLook(look) {
    var image = document.getElementById('hero-image');
    if (image && LOOKS[look] && look !== 'windows') {
      image.src = 'screenshots/' + look + '.PNG';
      image.alt = LOOKS[look].alt;
    }
  }

  // ---- Screenshot tabs ---------------------------------------------------------------------------------------

  function setUpTabs(firstLook) {
    var container = document.querySelector('[data-tabs]');
    if (!container) return null;
    var list = container.querySelector('[role="tablist"]');
    var tabs = Array.prototype.slice.call(list.querySelectorAll('[role="tab"]'));

    // The visitor's own platform comes first.
    var own = tabs.filter(function (tab) { return tab.getAttribute('data-look') === firstLook; })[0];
    if (own && own !== tabs[0]) {
      list.insertBefore(own, tabs[0]);
      tabs = Array.prototype.slice.call(list.querySelectorAll('[role="tab"]'));
    }

    function select(tab, focus) {
      tabs.forEach(function (other) {
        var selected = other === tab;
        other.setAttribute('aria-selected', selected ? 'true' : 'false');
        other.tabIndex = selected ? 0 : -1;
        var panel = document.getElementById(other.getAttribute('aria-controls'));
        if (panel) panel.hidden = !selected;
      });
      if (focus) tab.focus();
    }

    tabs.forEach(function (tab, index) {
      tab.addEventListener('click', function () { select(tab, false); });
      tab.addEventListener('keydown', function (event) {
        var next = null;
        if (event.key === 'ArrowRight') next = tabs[(index + 1) % tabs.length];
        else if (event.key === 'ArrowLeft') next = tabs[(index - 1 + tabs.length) % tabs.length];
        else if (event.key === 'Home') next = tabs[0];
        else if (event.key === 'End') next = tabs[tabs.length - 1];
        if (next) { event.preventDefault(); select(next, true); }
      });
    });

    var panels = container.querySelectorAll('[role="tabpanel"]');
    for (var i = 0; i < panels.length; i++) panels[i].tabIndex = 0;
    list.hidden = false;
    select(tabs[0], false);
    return select;
  }

  // ---- Latest version ----------------------------------------------------------------------------------------

  function readCache() {
    try {
      var cached = JSON.parse(sessionStorage.getItem(VERSION_CACHE_KEY) || 'null');
      if (cached && typeof cached.tag === 'string' && Date.now() - cached.at < VERSION_CACHE_MS) return cached.tag;
    } catch (e) { /* storage unavailable */ }
    return null;
  }

  function writeCache(tag) {
    try { sessionStorage.setItem(VERSION_CACHE_KEY, JSON.stringify({ tag: tag, at: Date.now() })); } catch (e) { /* ignore */ }
  }

  function showVersion(tag) {
    if (!/^v?\d+\.\d+\.\d+[0-9A-Za-z.-]*$/.test(tag)) return;
    var version = tag.replace(/^v/, '');
    var latest = document.getElementById('latest-version');
    if (latest) {
      latest.textContent = 'Version ' + version;
      latest.className = 'version-pill';
      latest.hidden = false;
    }
    var download = document.getElementById('download-version');
    if (download) {
      download.textContent = 'The latest version is ' + version + '.';
      download.hidden = false;
    }
  }

  function loadVersion() {
    var cached = readCache();
    if (cached) { showVersion(cached); return; }
    if (!window.fetch) return;
    fetch(LATEST_API, { headers: { Accept: 'application/vnd.github+json' } })
      .then(function (response) { return response.ok ? response.json() : null; })
      .then(function (release) {
        if (!release || typeof release.tag_name !== 'string') return;
        writeCache(release.tag_name);
        showVersion(release.tag_name);
      })
      .catch(function () { /* the version is a nicety; the download links do not need it */ });
  }

  // ---- Start -------------------------------------------------------------------------------------------------

  function start() {
    var basic = detectBasic();
    var desktop = basic.os === 'windows' || basic.os === 'macos' || basic.os === 'linux';

    setUpTabs(desktop ? basic.os : 'windows');
    useModifierName(basic.os);
    if (desktop) {
      showLook(basic.os);
      showBuild(chooseBuild(basic));
    } else {
      showAllDownloads(basic);
    }

    // Client hints and the GPU can refine the CPU architecture after the first paint.
    if (desktop) {
      detect().then(function (platform) {
        var id = chooseBuild(platform);
        if (id && id !== chooseBuild(basic)) showBuild(id);
      }, function () { /* keep the first guess */ });
    }

    loadVersion();
  }

  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', start);
  else start();
})();
