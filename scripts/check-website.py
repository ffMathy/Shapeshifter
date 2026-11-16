#!/usr/bin/env python3
"""Checks a built copy of the website before it is deployed.

Every local link, image, script, stylesheet and icon (in HTML, CSS and the web manifest) must exist, every in-page
anchor must point at an element on that page, and every image must have alt text.

    scripts/check-website.py <site-folder>
"""

import json
import re
import sys
from html.parser import HTMLParser
from pathlib import Path
from urllib.parse import unquote, urlsplit

URL_ATTRIBUTES = {"href", "src", "srcset", "poster"}


class Page(HTMLParser):
    def __init__(self):
        super().__init__(convert_charrefs=True)
        self.urls = []
        self.ids = set()
        self.images_without_alt = []

    def handle_starttag(self, tag, attrs):
        attributes = dict(attrs)
        if "id" in attributes:
            self.ids.add(attributes["id"])
        if tag == "img" and "alt" not in attributes:
            self.images_without_alt.append(attributes.get("src", "?"))
        for name, value in attrs:
            if value is None:
                continue
            if name == "srcset":
                self.urls.extend(part.strip().split(" ")[0] for part in value.split(",") if part.strip())
            elif name in URL_ATTRIBUTES:
                self.urls.append(value)
        if tag == "meta" and attributes.get("property") == "og:image":
            self.urls.append(attributes.get("content", ""))


def is_external(url):
    return bool(re.match(r"^[a-z][a-z0-9+.-]*:", url, re.I)) or url.startswith("//")


def main(root):
    root = Path(root).resolve()
    problems = []

    def check(source, url):
        if not url or is_external(url):
            return
        parts = urlsplit(url)
        path = unquote(parts.path)
        if not path:
            return  # a fragment on the same page; checked separately
        target = (root / path.lstrip("/")) if path.startswith("/") else (source.parent / path)
        target = target.resolve()
        if target.is_dir():
            target = target / "index.html"
        if root not in target.parents and target != root:
            problems.append(f"{source.relative_to(root)}: {url} points outside the site")
        elif not target.is_file():
            problems.append(f"{source.relative_to(root)}: {url} does not exist")

    pages = sorted(root.rglob("*.html"))
    if not pages:
        problems.append("no HTML pages found")

    for page_path in pages:
        page = Page()
        page.feed(page_path.read_text(encoding="utf-8"))
        for url in page.urls:
            check(page_path, url)
            if url.startswith("#") and len(url) > 1 and unquote(url[1:]) not in page.ids:
                problems.append(f"{page_path.relative_to(root)}: anchor {url} has no matching id")
        for src in page.images_without_alt:
            problems.append(f"{page_path.relative_to(root)}: image {src} has no alt attribute")

    for css in sorted(root.rglob("*.css")):
        for url in re.findall(r"url\(\s*['\"]?([^'\")]+)['\"]?\s*\)", css.read_text(encoding="utf-8")):
            if not url.startswith("data:"):
                check(css, url)

    for manifest in sorted(root.rglob("*.webmanifest")):
        for icon in json.loads(manifest.read_text(encoding="utf-8")).get("icons", []):
            check(manifest, icon.get("src", ""))

    if problems:
        print("The website has problems:")
        for problem in problems:
            print("  " + problem)
        return 1
    print(f"Checked {len(pages)} page(s): every local link, image and icon exists.")
    return 0


if __name__ == "__main__":
    if len(sys.argv) != 2:
        print(__doc__.strip())
        sys.exit(2)
    sys.exit(main(sys.argv[1]))
