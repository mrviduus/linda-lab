"""Linda (by Lekha, AnimateDiff clips in ../Linda/<clip>/*.png) -> Assets/Linda/Art/<clip>/<clip>_NN.png

Each clip: the grey background keyed out (flood fill from the edges, so latex highlights inside the body stay),
noise speckles dropped, every frame cropped to one cell for the whole clip with the body of frame 0 centred
and the feet on the bottom row (Unity pivot = bottom centre), scaled 1/5. The source faces left, so every clip is mirrored to face right.

Run from linda-lab/:  /tmp/venv/bin/python Tools/make_linda_frames.py   (needs Pillow, numpy, scipy)
"""
import os
import sys

import numpy as np
from PIL import Image
from scipy import ndimage

SRC = os.path.join(os.path.dirname(__file__), "..", "..", "Linda")
OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "Linda", "Art")
SCALE = 5                      # source px per output px
FLIP = None                    # None = every clip (the source faces left); or a set of clip names
MAX_HOLE = 8000                # enclosed grey areas this big are background, not a latex highlight
MIN_BLOB = 1500                # smaller foreground islands are background noise (source px)
SEAL = 3                       # px the ink is fattened by before the fill
MARGIN = 8                     # output px around the clip's union box


def key_out(im):
    a = np.asarray(im.convert("RGB")).astype(int)
    sat = a.max(2) - a.min(2)
    bright = a.sum(2)
    bg_like = (sat < 40) & (bright > 60)          # grey, not the black latex / outlines
    # background = grey pixels reachable from the edges; enclosed highlights are never reached.
    # The ink is fattened first so the fill cannot leak into the body through a thin outline.
    sealed = ndimage.binary_dilation(~bg_like, iterations=SEAL)
    labels, _ = ndimage.label(~sealed)
    edge = np.unique(np.concatenate([labels[0], labels[-1], labels[:, 0], labels[:, -1]]))
    sizes = ndimage.sum(~sealed, labels, range(1, labels.max() + 1))
    big = np.flatnonzero(sizes >= MAX_HOLE) + 1              # e.g. the gap between the legs in a turn
    reached = np.isin(labels, np.union1d(edge[edge != 0], big))
    fg = ~(ndimage.binary_dilation(reached, iterations=SEAL) & bg_like)
    # drop small islands: noise speckles that were not grey enough
    labels, n = ndimage.label(fg)
    sizes = ndimage.sum(fg, labels, range(1, n + 1))
    fg = np.isin(labels, np.flatnonzero(sizes >= MIN_BLOB) + 1)
    out = np.dstack([a, fg * 255]).astype(np.uint8)
    return Image.fromarray(out, "RGBA")


def bbox(im):
    return im.getbbox()


def clip(name):
    files = sorted(f for f in os.listdir(os.path.join(SRC, name)) if f.endswith(".png"))
    frames = [key_out(Image.open(os.path.join(SRC, name, f))) for f in files]
    if FLIP is None or name in FLIP:
        frames = [f.transpose(Image.FLIP_LEFT_RIGHT) for f in frames]
    boxes = [bbox(f) for f in frames]
    x0, y0, x1, y1 = (min(b[0] for b in boxes), min(b[1] for b in boxes),
                      max(b[2] for b in boxes), max(b[3] for b in boxes))
    b0 = boxes[0]
    cx, feet = (b0[0] + b0[2]) / 2, b0[3]           # frame 0: body centre and feet line
    half = max(cx - x0, x1 - cx) + MARGIN * SCALE
    top = y0 - MARGIN * SCALE
    bottom = max(y1, feet) + MARGIN * SCALE
    cell = (round(cx - half), top, round(cx + half), bottom)
    w, h = (cell[2] - cell[0]) // SCALE, (cell[3] - cell[1]) // SCALE
    w += w % 2
    pivot_y = (bottom - feet) / SCALE / h           # feet from the bottom, 0..1 for the Unity importer
    out_dir = os.path.join(OUT, name.replace(" ", "_"))
    os.makedirs(out_dir, exist_ok=True)
    for i, f in enumerate(frames):
        small = f.crop(cell).convert("RGBa").resize((w, h), Image.LANCZOS).convert("RGBA")
        small.save(os.path.join(out_dir, "%s_%02d.png" % (name.replace(" ", "_"), i)))
    print("%-13s %2d frames  %3dx%-3d  pivot y %.3f" % (name, len(frames), w, h, pivot_y))
    return pivot_y


def main():
    names = sys.argv[1:] or sorted(d for d in os.listdir(SRC) if os.path.isdir(os.path.join(SRC, d)))
    pivots = {name.replace(" ", "_"): clip(name) for name in names}
    with open(os.path.join(OUT, "pivots.txt"), "w") as f:   # LindaLab.cs reads this: clip pivot_y
        f.writelines("%s %.4f\n" % kv for kv in sorted(pivots.items()))


if __name__ == "__main__":
    main()
