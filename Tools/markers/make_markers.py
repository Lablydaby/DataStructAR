"""Generates the four AR marker cards (Array, Linked List, Stack, Queue).

Each card is a "stage": a dense, irregular, high-contrast frame that gives Vuforia plenty to
track, around a calm centre where the 3D model appears. The topic name and icon sit in one
corner. Every card has its own accent colour and frame shapes so they never look alike.

Usage: python make_markers.py <output folder>
Needs Pillow (pip install pillow).
"""
import math
import random
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

# Same proportions as the current Vuforia targets (1 : 0.72)
WIDTH, HEIGHT = 2000, 1440
SCALE = 2  # Drawn at double size, then scaled down for smooth edges
# Frame thickness: thinner at the sides so the calm centre fits a full row of 5 items
BAND_X = 170
BAND_Y = 330
CENTRE_RADIUS = 64

# App branding (matches UiKit)
BRAND = (0x45, 0x37, 0x74)
INK = (0x1E, 0x1A, 0x3B)
LAVENDER = (0xDC, 0xD4, 0xF0)
CENTRE = (0xF6, 0xF4, 0xFB)
WHITE = (0xFF, 0xFF, 0xFF)

FONT_DIR = Path(__file__).resolve().parents[2] / "Assets" / "Fonts" / "poppins"

CARDS = [
    ("array", "Array", (0xE0, 0x52, 0x52), 11),
    ("linkedlist", "Linked List", (0x3D, 0xBE, 0x7A), 23),
    ("stack", "Stack", (0xF2, 0xA9, 0x3B), 37),
    ("queue", "Queue", (0x4F, 0xA3, 0xE0), 53),
]


def s(value):
    return int(round(value * SCALE))


def in_centre(x, y, margin=0):
    return BAND_X - margin < x < WIDTH - BAND_X + margin and BAND_Y - margin < y < HEIGHT - BAND_Y + margin


def random_band_point(rng):
    while True:
        x, y = rng.uniform(0, WIDTH), rng.uniform(0, HEIGHT)
        if not in_centre(x, y, margin=-30):
            return x, y


def rotated(points, cx, cy, angle):
    c, n = math.cos(angle), math.sin(angle)
    return [(s(cx + px * c - py * n), s(cy + px * n + py * c)) for px, py in points]


def shape_colour(rng, accent):
    # Mostly light shapes on the dark band for contrast, with accent and ink for variety
    return rng.choices([WHITE, LAVENDER, accent, INK], weights=[4, 3, 3, 2])[0]


def draw_shape(draw, rng, kind, accent):
    x, y = random_band_point(rng)
    colour = shape_colour(rng, accent)
    filled = rng.random() < 0.6
    stroke = s(rng.uniform(6, 13))

    def poly(points):
        if filled:
            draw.polygon(points, fill=colour)
        else:
            draw.line(points + [points[0]], fill=colour, width=stroke, joint="curve")

    if kind == "array":
        # Axis-aligned cells of mixed sizes, like scattered array slots
        w, h = rng.uniform(22, 150), rng.uniform(22, 150)
        poly([(s(x), s(y)), (s(x + w), s(y)), (s(x + w), s(y + h)), (s(x), s(y + h))])
    elif kind == "linkedlist":
        # Nodes, some joined to a neighbour by a link with an end dot
        r = rng.uniform(12, 72)
        box = [s(x - r), s(y - r), s(x + r), s(y + r)]
        if filled:
            draw.ellipse(box, fill=colour)
        else:
            draw.ellipse(box, outline=colour, width=stroke)
        if rng.random() < 0.45:
            angle = rng.uniform(0, 2 * math.pi)
            length = rng.uniform(70, 190)
            ex, ey = x + math.cos(angle) * length, y + math.sin(angle) * length
            draw.line([(s(x), s(y)), (s(ex), s(ey))], fill=colour, width=s(rng.uniform(6, 11)))
            d = rng.uniform(9, 18)
            draw.ellipse([s(ex - d), s(ey - d), s(ex + d), s(ey + d)], fill=colour)
    elif kind == "stack":
        # Slightly tilted bars of mixed length, like loose stacked blocks
        w, h = rng.uniform(60, 260), rng.uniform(16, 54)
        angle = math.radians(rng.uniform(-9, 9))
        poly(rotated([(-w / 2, -h / 2), (w / 2, -h / 2), (w / 2, h / 2), (-w / 2, h / 2)], x, y, angle))
    else:
        # Arrows and chevrons pointing every which way
        size = rng.uniform(26, 120)
        angle = rng.uniform(0, 2 * math.pi)
        if rng.random() < 0.5:
            points = [(-size / 2, -size / 2), (size / 2, 0), (-size / 2, size / 2)]
            poly(rotated(points, x, y, angle))
        else:
            t = size * 0.28
            points = [(-size / 2, -size / 2), (-size / 2 + t, -size / 2), (size / 2, 0),
                      (-size / 2 + t, size / 2), (-size / 2, size / 2), (size / 2 - t, 0)]
            draw.polygon(rotated(points, x, y, angle), fill=colour)


def draw_sprinkles(draw, rng, accent):
    # Small crosses and dots everywhere in the band add sharp corners for tracking
    for _ in range(260):
        x, y = random_band_point(rng)
        colour = rng.choice([WHITE, LAVENDER, accent, INK])
        size = rng.uniform(6, 20)
        if rng.random() < 0.5:
            w = s(rng.uniform(3, 6))
            draw.line([(s(x - size), s(y)), (s(x + size), s(y))], fill=colour, width=w)
            draw.line([(s(x), s(y - size)), (s(x), s(y + size))], fill=colour, width=w)
        else:
            draw.ellipse([s(x - size / 2), s(y - size / 2), s(x + size / 2), s(y + size / 2)], fill=colour)


def draw_icon(draw, kind, x, y, size, colour):
    """A small line icon for the topic, inside a size x size box at (x, y)."""
    w = s(size * 0.09)
    if kind == "array":
        cell = size / 3.4
        for i in range(3):
            left = x + i * (cell + size * 0.06)
            draw.rounded_rectangle([s(left), s(y + size / 2 - cell / 2), s(left + cell), s(y + size / 2 + cell / 2)],
                                   radius=s(cell * 0.18), fill=colour)
    elif kind == "linkedlist":
        r = size * 0.17
        cy = y + size / 2
        draw.ellipse([s(x), s(cy - r), s(x + 2 * r), s(cy + r)], fill=colour)
        draw.ellipse([s(x + size - 2 * r), s(cy - r), s(x + size), s(cy + r)], fill=colour)
        draw.line([(s(x + 2 * r), s(cy)), (s(x + size - 2 * r - size * 0.08), s(cy))], fill=colour, width=w)
        tip = x + size - 2 * r - size * 0.02
        draw.polygon([(s(tip), s(cy)), (s(tip - size * 0.14), s(cy - size * 0.1)), (s(tip - size * 0.14), s(cy + size * 0.1))],
                     fill=colour)
    elif kind == "stack":
        bar = size * 0.22
        for i in range(3):
            top = y + size * 0.08 + i * (bar + size * 0.07)
            draw.rounded_rectangle([s(x + size * 0.1), s(top), s(x + size * 0.9), s(top + bar)],
                                   radius=s(bar * 0.3), fill=colour)
    else:
        cell = size * 0.24
        cy = y + size / 2
        for i in range(3):
            left = x + i * (cell + size * 0.05)
            draw.rounded_rectangle([s(left), s(cy - cell / 2), s(left + cell), s(cy + cell / 2)],
                                   radius=s(cell * 0.2), fill=colour)
        tip = x + size
        draw.polygon([(s(tip), s(cy)), (s(tip - size * 0.17), s(cy - size * 0.16)), (s(tip - size * 0.17), s(cy + size * 0.16))],
                     fill=colour)


def make_card(kind, title, accent, seed):
    rng = random.Random(seed)
    image = Image.new("RGB", (s(WIDTH), s(HEIGHT)), BRAND)
    draw = ImageDraw.Draw(image)

    for _ in range(330):
        draw_shape(draw, rng, kind, accent)
    draw_sprinkles(draw, rng, accent)

    # Calm stage in the middle, with a thin outline so its edge is crisp
    centre = [s(BAND_X), s(BAND_Y), s(WIDTH - BAND_X), s(HEIGHT - BAND_Y)]
    draw.rounded_rectangle([c + (-s(14) if i < 2 else s(14)) for i, c in enumerate(centre)],
                           radius=s(CENTRE_RADIUS + 14), fill=WHITE)
    draw.rounded_rectangle(centre, radius=s(CENTRE_RADIUS), fill=CENTRE)

    # Topic label in the top-left corner: icon + name on a white pill
    bold = ImageFont.truetype(str(FONT_DIR / "Poppins-Bold.ttf"), s(86))
    text_w = draw.textlength(title, font=bold) / SCALE
    icon = 92
    pad = 40
    pill = [52, 52, 52 + pad + icon + 28 + text_w + pad, 52 + 150]
    # Dark outline keeps the pill separate from light shapes behind it
    draw.rounded_rectangle([s(v) for v in pill], radius=s(75), fill=WHITE, outline=INK, width=s(8))
    draw_icon(draw, kind, pill[0] + pad, pill[1] + (150 - icon) / 2, icon, accent)
    draw.text((s(pill[0] + pad + icon + 28), s(pill[1] + 75)), title, font=bold, fill=BRAND, anchor="lm")

    # App name in the bottom-right corner
    small = ImageFont.truetype(str(FONT_DIR / "Poppins-SemiBold.ttf"), s(44))
    name = "DataStructAR"
    name_w = draw.textlength(name, font=small) / SCALE
    tag = [WIDTH - 52 - name_w - 64, HEIGHT - 52 - 92, WIDTH - 52, HEIGHT - 52]
    draw.rounded_rectangle([s(v) for v in tag], radius=s(46), fill=INK)
    draw.text((s((tag[0] + tag[2]) / 2), s((tag[1] + tag[3]) / 2)), name, font=small, fill=WHITE, anchor="mm")

    return image.resize((WIDTH, HEIGHT), Image.LANCZOS)


def main():
    out = Path(sys.argv[1] if len(sys.argv) > 1 else ".")
    out.mkdir(parents=True, exist_ok=True)
    for kind, title, accent, seed in CARDS:
        card = make_card(kind, title, accent, seed)
        # Vuforia takes 8- or 24-bit PNG/JPG under 2.25 MB
        path = out / f"{kind}-marker.jpg"
        card.save(path, quality=92, subsampling=0)
        print(f"{path}  {path.stat().st_size / 1e6:.2f} MB")


if __name__ == "__main__":
    main()
