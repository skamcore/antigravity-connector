from pathlib import Path
from PIL import Image, ImageDraw

root = Path(__file__).parent
size = 512
im = Image.new('RGBA', (size, size))
mask = Image.new('L', (size, size))
ImageDraw.Draw(mask).rounded_rectangle((12, 12, 500, 500), radius=112, fill=255)
gradient = Image.new('RGBA', (size, size))
pixels = gradient.load()
for y in range(size):
    for x in range(size):
        t = (x + y) / (size * 2)
        pixels[x, y] = (int(117 - t * 42), int(87 - t * 47), int(235 - t * 25), 255)
im.paste(gradient, (0, 0), mask)
links = Image.new('RGBA', (size, size))
d = ImageDraw.Draw(links)
d.rounded_rectangle((94, 189, 304, 307), radius=59, outline='white', width=29)
d.rounded_rectangle((208, 189, 418, 307), radius=59, outline='white', width=29)
links = links.rotate(36, resample=Image.Resampling.BICUBIC, center=(256, 248))
im.alpha_composite(links)
im.save(root / 'connector.png')
im.save(root / 'app.ico', sizes=[(n, n) for n in (16, 24, 32, 48, 64, 128, 256)])
