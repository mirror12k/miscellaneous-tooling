#!/usr/bin/env python3

import sys
import struct



# deflate a bytestream to utf-8 compliant text
	# relies on a trick of never setting the MSB of a byte
	# it works because \x00-\x7f are always allowed in utf8 no matter what
# this compressor is useful when certain decompressors are stricter in interpretation
	# ex: C#'s builtin deflate will reject an hclen of 0, making other ascii/utf8 compressors fail



# utility functions for dealing with bitstrings
def pad_binary_string(s):
	return s + '0' * ((8 - len(s) % 8) % 8)
def split_binary_string(s):
	return [ s[i*8:(i+1)*8] for i in range(int(len(s) / 8)) ]
def convert_to_bytes(s):
	return b''.join([ int(n[::-1], 2).to_bytes(1,byteorder='big') for n in split_binary_string(pad_binary_string(s)) ])

# an empty fixed-huffman block used for re-aligning the block structure
def empty_block():
	return '0' + '10' + '0000000'




# compiles a dynamic-huffman block from a bytestring of no more than 7 unique characters
# a better solution would be able to encode more unique characters into a single block, decreasing the overhead
def compile_dynamic_huffman_block(data, not_first=False, is_last=False):
	# used to determine alignment
	is_odd = len(data) % 2

	# prep our block stats
	bitstring = ('1' if is_last else '0') + '01'
	hlit = '01001'[::-1]
	hdist = ('01111' if not_first else ('10000' if is_odd else '10010'))[::-1]
	hclen = ('1001' if not_first else '1000')[::-1]

	bitstring += hlit + hdist + hclen

	# get the unique characters from our block
	chars = sorted(set(data))

	# these are the codes we use for encoding
	data_byte_indexes = [
		'0000',
		'0001',
		'0010',
		'0011',
		'0100',
		'0101',
		'0110',
	]

	# keep adding characters to the table until we have 7 exactly
	i = 0
	while len(chars) < 7:
		if i not in chars:
			chars.append(i)
		i += 1
	chars = sorted(chars)

	# 
	mapping_table = {}
	for i in range(len(data_byte_indexes)):
		mapping_table[chars[i]] = data_byte_indexes[i]

	# 16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15
	if not_first:
		hclen_table = ''.join( code[::-1] for code in ['000', '010', '010', '010', '000', '000', '000', '000', '000', '000', '000', '010', '000'])
	else:
		hclen_table = ''.join( code[::-1] for code in ['000', '010', '010', '010', '000', '000', '000', '000', '000', '000', '000', '010'])
	# literals table
	hlit_table = [ '00' for i in range(256 + 10) ]
	# encode our characters
	for c in chars:
		hlit_table[c] = '01'
	# encode some junk
	hlit_table[256] = '01'
	hlit_table[(256 + 1):(256 + 9)] = [ '01' for i in range(8) ]
	hlit_table = ''.join(hlit_table)

	# more junk
	if not_first:
		hdist_table = [ '01' for i in range(16) ]
	else:
		hdist_table = [ '00' for i in range(17 + (0 if is_odd else 2)) ]
		hdist_table[0:16] = [ '01' for i in range(16) ]
	hdist_table = ''.join(hdist_table)

	bitstring += hclen_table
	bitstring += hlit_table
	bitstring += hdist_table

	# encode the bytestring
	for c in data:
		bitstring += mapping_table[c]
	# block termination sequence
	bitstring += '0111'


	# re-align if we are odd
	if not_first and is_odd and not is_last:
		bitstring += empty_block() + empty_block()

	return bitstring


# this function breaks up a bytestring into blocks of no more than 7 unique characters
def deflate_dynamic_huffman(s):
	data_blocks = []
	current_block = b''
	chars = set()
	for c in s:
		if c in chars or len(chars) < 7:
			current_block += c.to_bytes(1, byteorder='big')
			chars.add(c)
		else:
			data_blocks.append(current_block)
			current_block = c.to_bytes(1, byteorder='big')
			chars = set([c])

	if current_block != b'':
		data_blocks.append(current_block)
	# print('blocks:', data_blocks)

	return ''.join( compile_dynamic_huffman_block(data_blocks[i], not_first=i != 0, is_last=i == len(data_blocks)-1) for i in range(len(data_blocks)) )



# read stdin as bytes
text = sys.stdin.buffer.read()
# compress and output 
sys.stdout.buffer.write(convert_to_bytes(deflate_dynamic_huffman(text)))







