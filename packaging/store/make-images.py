#!/usr/bin/env python3
"""Generates the Microsoft Store tile images and the 1024 px Mac App Store icon from one square app icon.

    python3 packaging/store/make-images.py <windows-icon.png> <macos-icon.png>

Both sources should be at least 1024 px (smaller ones are scaled up and look soft). Writes packaging/store/windows/Images
(the names AppxManifest.xml refers to, with the scale and target-size qualifiers that resources.pri resolves) and
packaging/store/macos/AppIcon.png. Needs Pillow (pip install pillow).
"""
import os
import sys

from PIL import Image

here = os.path.dirname(os.path.abspath(__file__))
images = os.path.join(here, "windows", "Images")


def load(path):
    icon = Image.open(path).convert("RGBA")
    box = icon.getbbox()
    return icon.crop(box) if box else icon


def fit(icon, width, height, fill):
    """The icon centred on a transparent width x height canvas, as large as `fill` (0-1) of the shorter side allows."""
    canvas = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    side = max(1, round(min(width, height) * fill))
    scale = side / max(icon.size)
    resized = icon.resize((max(1, round(icon.width * scale)), max(1, round(icon.height * scale))), Image.LANCZOS)
    canvas.alpha_composite(resized, ((width - resized.width) // 2, (height - resized.height) // 2))
    return canvas


def main():
    if len(sys.argv) != 3:
        sys.exit(__doc__)
    windows, macos = load(sys.argv[1]), load(sys.argv[2])
    os.makedirs(images, exist_ok=True)
    for name in os.listdir(images):
        os.remove(os.path.join(images, name))

    def save(name, image):
        image.save(os.path.join(images, name), optimize=True)

    for scale in (100, 125, 150, 200, 400):
        f = scale / 100
        save(f"Square44x44Logo.scale-{scale}.png", fit(windows, round(44 * f), round(44 * f), 1.0))
        save(f"Square150x150Logo.scale-{scale}.png", fit(windows, round(150 * f), round(150 * f), 0.66))
        save(f"Wide310x150Logo.scale-{scale}.png", fit(windows, round(310 * f), round(150 * f), 0.66))
        save(f"StoreLogo.scale-{scale}.png", fit(windows, round(50 * f), round(50 * f), 1.0))
    # Taskbar, Start and Explorer pick these by pixel size; "unplated" ones are drawn without a tile behind them.
    for size in (16, 20, 24, 30, 32, 36, 40, 48, 60, 64, 72, 80, 96, 256):
        icon = fit(windows, size, size, 1.0)
        save(f"Square44x44Logo.targetsize-{size}.png", icon)
        save(f"Square44x44Logo.altform-unplated_targetsize-{size}.png", icon)
        save(f"Square44x44Logo.altform-lightunplated_targetsize-{size}.png", icon)

    # Apple's icon grid: the artwork fills 824 of 1024 px. An icon drawn for that grid (with a transparent margin) is
    # scaled as it is; one that fills its canvas gets the margin added.
    whole = Image.open(sys.argv[2]).convert("RGBA")
    has_margin = macos.size != whole.size
    mac_icon = whole.resize((1024, 1024), Image.LANCZOS) if has_margin else fit(macos, 1024, 1024, 824 / 1024)
    mac_icon.save(os.path.join(here, "macos", "AppIcon.png"), optimize=True)
    print(f"Wrote {len(os.listdir(images))} images to {images} and macos/AppIcon.png")


if __name__ == "__main__":
    main()
