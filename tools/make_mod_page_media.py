from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(r"F:\MOD\FireSaveRepair")
OUT = ROOT / "dist" / "mod-page-media"
OUT.mkdir(parents=True, exist_ok=True)

HEADER_SRC = Path(
    r"C:\Users\sputn\.codex\generated_images\01a06d9e-c792-72d1-a05d-d45f6b3fe44d\call_iGhPOWURJpCDIrpcYcjyGDHG.png"
)
GALLERY_SRC = Path(
    r"C:\Users\sputn\.codex\generated_images\01a06d9e-c792-72d1-a05d-d45f6b3fe44d\call_6n5CEzDqtOB27isjXz7tj05H.png"
)

PATH_TEXT = r"C:\Users\Player\AppData\Local\Stalker2\Saved\STEAM\SaveGames\Data"


def font(size: int) -> ImageFont.FreeTypeFont:
    for p in (
        Path(r"C:\Windows\Fonts\segoeui.ttf"),
        Path(r"C:\Windows\Fonts\arial.ttf"),
    ):
        if p.exists():
            return ImageFont.truetype(str(p), size)
    return ImageFont.load_default(size=size)


def fit_text(draw: ImageDraw.ImageDraw, text: str, max_width: int, size: int):
    while size >= 12:
        f = font(size)
        if draw.textbbox((0, 0), text, font=f)[2] <= max_width:
            return f
        size -= 1
    return font(12)


def patch_field(img: Image.Image, rect, text_xy, text_size, max_width):
    draw = ImageDraw.Draw(img)
    x1, y1, x2, y2 = rect
    # Dark rounded field, matching the app surface closely enough to hide the old username.
    draw.rounded_rectangle(rect, radius=10, fill=(18, 18, 24), outline=(58, 59, 70), width=2)
    draw.text(text_xy, PATH_TEXT, font=fit_text(draw, PATH_TEXT, max_width, text_size), fill=(255, 255, 255))


def make_header():
    img = Image.open(HEADER_SRC).convert("RGB")
    # Header source coordinates.
    patch_field(img, (106, 498, 1138, 589), (136, 532), 30, 970)
    # Crop to Nexus header aspect, keeping title, language toggle, and real scan controls.
    crop = img.crop((0, 65, 1952, 624))
    crop = crop.resize((1300, 372), Image.Resampling.LANCZOS)
    crop.save(OUT / "fire-save-repair-header-1300x372.png", optimize=True)


def make_gallery():
    img = Image.open(GALLERY_SRC).convert("RGB")
    # Full app screenshot coordinates.
    patch_field(img, (64, 232, 956, 281), (86, 250), 20, 842)
    img = img.resize((1920, 1080), Image.Resampling.LANCZOS)
    img.save(OUT / "fire-save-repair-gallery-1920x1080.png", optimize=True)


if __name__ == "__main__":
    make_header()
    make_gallery()
    print(OUT / "fire-save-repair-header-1300x372.png")
    print(OUT / "fire-save-repair-gallery-1920x1080.png")
