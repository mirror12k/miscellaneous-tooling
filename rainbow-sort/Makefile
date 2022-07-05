
all:
	# compile code
	javac RainbowSorter.java
	javac RainbowStripper.java
	# hash each element of the wordlist
	java RainbowSorter wordlist.txt > hashed.txt
	# sort the hashes
	sort --parallel=8 -uo hashed-sorted.txt hashed.txt
	# strip the hashes
	java RainbowStripper hashed-sorted.txt > rainbow-wordlist.txt
	# now we are left with rainbow-wordlist which is a rainbow table of passwords in order of their hash
	# rm hashed.txt hashed-sorted.txt


