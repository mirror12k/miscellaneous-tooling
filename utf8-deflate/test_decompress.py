#!/usr/bin/env python3

import sys
import zlib

# a quick decompression tool to test the compressors

def inflate(data):
    decompress = zlib.decompressobj(
            -zlib.MAX_WBITS
    )
    inflated = decompress.decompress(data)
    inflated += decompress.flush()
    return inflated

text = sys.stdin.buffer.read()

sys.stdout.buffer.write(inflate(text))
# print(inflate(text))

