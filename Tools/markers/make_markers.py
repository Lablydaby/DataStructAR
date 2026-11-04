"""Generates the four AR marker cards (Array, Linked List, Stack, Queue), following the
"DataStructAR AR markers, v2" design.

Each card is white with a thick ink border. A ring of small high-contrast tiles (squares, triangles,
circles, rings, L-shapes and leaves in indigo tones) gives Vuforia plenty to track; a solid block of
tiles in the top-left corner shows which way is up. The middle is a large empty white panel where the
3D model appears, and a bottom bar carries the logo, the topic name and the marker number.

The design is 1000 x 720; the cards are drawn at 2x (2000 x 1440), the same size as before.
Usage: python make_markers.py <output folder>
Needs Pillow (pip install pillow).
"""
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

DESIGN_W, DESIGN_H = 1000, 720
OUT = 2          # Output pixels per design pixel (2000 x 1440)
SS = 2           # Supersampling for smooth edges
K = OUT * SS     # Drawing pixels per design pixel

BORDER = 24      # Ink frame around the card
TILE, PITCH = 36, 40
COLS, ROWS = 23, 15
DENSITY = 70     # Out of 120: how many tile positions get a tile

# The white panel for the 3D model, in the card's inner (inside-the-border) coordinates
PANEL = (56, 80, 840, 440)   # x, y, width, height
BAR_H = 64

INK = (0x1E, 0x1A, 0x3C)
TONES = [INK, INK, (0x48, 0x3A, 0x75), (0x5F, 0x57, 0x87), (0xA2, 0x99, 0xC4)]
WHITE = (255, 255, 255)
SOFT = (0xD7, 0xD5, 0xE3)

HERE = Path(__file__).resolve().parent
FONT_DIR = HERE.parents[1] / "Assets" / "Fonts" / "poppins"

CARDS = [
    ("array", "Array", 1103),
    ("linkedlist", "Linked List", 2207),
    ("stack", "Stack", 3301),
    ("queue", "Queue", 4409),
]


def mulberry32(seed):
    """The design's random generator, so each card gets the same mosaic as in the design."""
    a = seed & 0xFFFFFFFF

    def imul(x, y):
        return (x * y) & 0xFFFFFFFF

    def next_value():
        nonlocal a
        a = (a + 0x6D2B79F5) & 0xFFFFFFFF
        t = a
        t = imul(t ^ (t >> 15), t | 1)
        t ^= (t + imul(t ^ (t >> 7), t | 61)) & 0xFFFFFFFF
        return ((t ^ (t >> 14)) & 0xFFFFFFFF) / 4294967296

    return next_value


def tiles(seed):
    """Tile positions and looks, as in the design: a ring two tiles deep at the top and bottom and one
    tile deep at the sides, with a solid corner block at the top left."""
    r = mulberry32(seed)
    fill = DENSITY / 120
    out = []
    for row in range(ROWS):
        for col in range(COLS):
            if 1 <= col <= 21 and 2 <= row <= 12:
                continue
            x, y = col * PITCH + 18, row * PITCH + 6
            corner = (row == 0 and col <= 1) or (row == 1 and col == 0)
            if corner:
                out.append(dict(x=x, y=y, kind="square", colour=INK, rot=0, radius=0))
                continue
            if r() > fill:
                continue
            kind = int(r() * 6)
            colour = TONES[int(r() * len(TONES))]
            rot = [0, 90, 180, 270][int(r() * 4)]
            out.append(dict(x=x, y=y, colour=colour, rot=rot, radius=4,
                            kind=["square", "triangle", "circle", "ring", "corner", "leaf"][kind]))
    return out


def tile_mask(kind, radius):
    """A TILE x TILE mask (at drawing scale) for one tile shape, before rotation."""
    size = TILE * K
    mask = Image.new("L", (size, size), 0)
    d = ImageDraw.Draw(mask)
    w = size - 1
    if kind == "square":
        d.rounded_rectangle([0, 0, w, w], radius=radius * K, fill=255)
    elif kind == "triangle":
        d.polygon([(0, 0), (w, 0), (0, w)], fill=255)
    elif kind == "circle":
        d.ellipse([0, 0, w, w], fill=255)
    elif kind == "ring":
        d.ellipse([0, 0, w, w], fill=255)
        inset = 8 * K
        d.ellipse([inset, inset, w - inset, w - inset], fill=0)
    elif kind == "corner":
        h = 0.45 * w
        d.polygon([(0, 0), (w, 0), (w, h), (h, h), (h, w), (0, w)], fill=255)
    elif kind == "leaf":
        # Rounded top-right and bottom-left corners (border-radius: 0 18px 0 18px)
        d.rounded_rectangle([0, 0, w, w], radius=18 * K, fill=255)
        half = size // 2
        d.rectangle([0, 0, half, half], fill=255)
        d.rectangle([half, half, w, w], fill=255)
    return mask


def spaced_text(draw, xy, text, font, fill, spacing, anchor):
    """Draws text with extra letter spacing; xy is the left-middle or centre-middle point."""
    widths = [draw.textlength(c, font=font) for c in text]
    total = sum(widths) + spacing * (len(text) - 1)
    x, y = xy
    if anchor == "mm":
        x -= total / 2
    elif anchor == "rm":
        x -= total
    for c, w in zip(text, widths):
        draw.text((x, y), c, font=font, fill=fill, anchor="lm")
        x += w + spacing


def make_card(kind, title, number, seed):
    img = Image.new("RGB", (DESIGN_W * K, DESIGN_H * K), INK)
    draw = ImageDraw.Draw(img)

    def box(x, y, w, h):
        """Inner coordinates (inside the border) to drawing pixels."""
        return [(BORDER + x) * K, (BORDER + y) * K, (BORDER + x + w) * K, (BORDER + y + h) * K]

    inner_w, inner_h = DESIGN_W - 2 * BORDER, DESIGN_H - 2 * BORDER
    draw.rectangle(box(0, 0, inner_w, inner_h), fill=WHITE)

    for t in tiles(seed):
        mask = tile_mask(t["kind"], t["radius"])
        if t["rot"]:
            mask = mask.rotate(-t["rot"])  # CSS rotates clockwise
        img.paste(t["colour"], ((BORDER + t["x"]) * K, (BORDER + t["y"]) * K), mask)

    # The empty panel for the 3D model
    px, py, pw, ph = PANEL
    draw.rounded_rectangle(box(px, py, pw, ph), radius=22 * K, fill=WHITE, outline=INK, width=6 * K)

    # Bottom bar: logo and name, topic, marker number
    bar_top = inner_h - BAR_H
    draw.rectangle(box(0, bar_top, inner_w, BAR_H), fill=INK)
    mid = (BORDER + bar_top + BAR_H / 2) * K

    chip = box(16, bar_top + (BAR_H - 44) / 2, 44, 44)
    draw.rounded_rectangle(chip, radius=9 * K, fill=WHITE)
    logo = Image.open(HERE / "logo.png").convert("RGBA").resize((40 * K, 40 * K), Image.LANCZOS)
    img.paste(logo, (int(chip[0] + 2 * K), int(chip[1] + 2 * K)), logo)

    bold = lambda size: ImageFont.truetype(str(FONT_DIR / "Poppins-Bold.ttf"), size * K)
    draw.text(((BORDER + 16 + 44 + 10) * K, mid), "DataStructAR", font=bold(21), fill=WHITE, anchor="lm")
    spaced_text(draw, (DESIGN_W / 2 * K, mid), title.upper(), bold(34), WHITE, 0.04 * 34 * K, "mm")
    spaced_text(draw, ((DESIGN_W - BORDER - 22) * K, mid), f"MARKER 0{number}", bold(13), SOFT, 0.12 * 13 * K, "rm")

    return img.resize((DESIGN_W * OUT, DESIGN_H * OUT), Image.LANCZOS)


def main():
    out = Path(sys.argv[1] if len(sys.argv) > 1 else ".")
    out.mkdir(parents=True, exist_ok=True)
    for number, (kind, title, seed) in enumerate(CARDS, start=1):
        card = make_card(kind, title, number, seed)
        # Vuforia takes 8- or 24-bit PNG/JPG under 2.25 MB
        path = out / f"{kind}-marker.jpg"
        card.save(path, quality=92, subsampling=0)
        print(f"{path}  {path.stat().st_size / 1e6:.2f} MB")


if __name__ == "__main__":
    main()
