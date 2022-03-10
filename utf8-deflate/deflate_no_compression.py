#!/usr/bin/env python3

import sys
import struct


# compresses a block with no compression, but still valid deflate!
# useful for testing things, but this is NOT utf8 compliant

def pad_binary_string(s):
	return s + '0' * ((8 - len(s) % 8) % 8)
def split_binary_string(s):
	return [ s[i*8:(i+1)*8] for i in range(int(len(s) / 8)) ]
def convert_to_bytes(s):
	return b''.join([ int(n[::-1], 2).to_bytes(1,byteorder='big') for n in split_binary_string(pad_binary_string(s)) ])

text = sys.stdin.buffer.read()

# no compression block
sys.stdout.buffer.write(convert_to_bytes('1' + '00') + struct.pack('<HH', len(text), (~len(text)) & 0xffff) + text)





