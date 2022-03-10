#!/usr/bin/env python3

import sys
import struct


# produces an empty deflate block
# useful for testing or breaking decompressors

def pad_binary_string(s):
	return s + '0' * ((8 - len(s) % 8) % 8)
def split_binary_string(s):
	return [ s[i*8:(i+1)*8] for i in range(int(len(s) / 8)) ]
def convert_to_bytes(s):
	return b''.join([ int(n[::-1], 2).to_bytes(1,byteorder='big') for n in split_binary_string(pad_binary_string(s)) ])

text = sys.stdin.buffer.read()

# compressed block with nothing in it
# this empty block can be repeated as many times as you want
sys.stdout.buffer.write(convert_to_bytes('1' + '10' + '0000000'))





