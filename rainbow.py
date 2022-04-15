#!/usr/bin/env python3

import sys



# Rainbow Utility
# this python script generates rainbow buffers useful for developing vulnerabilities
# first generate a rainbow payload
# then, when a program crashes with an invalid EIP/etc address, copy it into the program and read the resulting offset
# this gives you the buffer offset from which a given program is reading invalid data

# supports a maximum size of 3mb, after which you will start to get duplicates



char_table = list('abcdefghijklmnopqrstuvwxyz')

def rainbow_generate(size, offset=0):
	v1 = 0
	v2 = 0
	v3 = 0
	v4 = 0

	s = ''
	while len(s) < size:
		s += (char_table[v1] + char_table[v2] + char_table[v3] + char_table[v4]).upper()
		s += char_table[v1] + char_table[v2] + char_table[v3] + char_table[v4]

		v4 = (v4 + 1) % len(char_table)
		if v4 == 0:
			v3 = (v3 + 1) % len(char_table)
			if v3 == 0:
				v2 = (v2 + 1) % len(char_table)
				if v2 == 0:
					v1 = (v1 + 1) % len(char_table)

	return s[0:size]

def rainbow_find(key):
	v1 = 0
	v2 = 0
	v3 = 0
	v4 = 0

	offset = 0
	s = ''
	while True:
		s += (char_table[v1] + char_table[v2] + char_table[v3] + char_table[v4]).upper()
		s += char_table[v1] + char_table[v2] + char_table[v3] + char_table[v4]

		v4 = (v4 + 1) % len(char_table)
		if v4 == 0:
			v3 = (v3 + 1) % len(char_table)
			if v3 == 0:
				v2 = (v2 + 1) % len(char_table)
				if v2 == 0:
					v1 = (v1 + 1) % len(char_table)

		# print (s)
		if key in s:
			offset += s.find(key)
			return offset

		if len(s) > 32:
			s = s[8:]
			offset += 8


	return s[0:size]


# def rainbow_find(key):


def main(args):
	if len(args) == 2:
		print(rainbow_generate(int(args[1])), end='')
	elif len(args) == 3 and args[1] == '--find':
		print(rainbow_find(args[2]))
	else:
		print("usage:")
		print("\t generate a rainbow: ./rainbow.py <length>")
		print("\t find a value in the rainbow: ./rainbow.py --find <4-or-8-letter-key>")

	# print(rainbow_find('AAAa'))
	# print(rainbow_generate(1024))


if __name__ == '__main__':
	main(sys.argv)
