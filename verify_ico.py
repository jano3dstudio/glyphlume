"""Independent binary/Pillow checks; tests only, not a runtime dependency."""
import struct
import sys
from pathlib import Path
from PIL import Image

path = Path(sys.argv[1])
data = path.read_bytes()
assert struct.unpack_from('<HHH', data) == (0, 1, 7)
expected = {(n, n) for n in (16, 24, 32, 48, 64, 128, 256)}
image = Image.open(path)
assert image.format == 'ICO'
assert image.ico.sizes() == expected
for i in range(7):
    w, h, colors, reserved, planes, bits, length, offset = struct.unpack_from('<BBBBHHII', data, 6 + i * 16)
    w, h = w or 256, h or 256
    assert planes == 1 and bits == 32 and reserved == 0
    assert offset + length <= len(data)
    assert struct.unpack_from('<IiiHH', data, offset) == (40, w, h * 2, 1, 32)
    pixels = image.ico.getimage((w, h))
    assert pixels.size == (w, h) and pixels.mode == 'RGBA'
    assert pixels.getchannel('A').getextrema()[1] > 0
print('PASS: genuine ICO; 7 complete independently decoded RGBA images, 16 through 256 px.')
