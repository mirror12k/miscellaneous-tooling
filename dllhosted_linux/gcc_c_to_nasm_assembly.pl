#!/usr/bin/env perl
use strict;
use warnings;

use feature 'say';

use IO::File;



my $data = `gcc $ARGV[0] -fno-stack-protector -S -masm=intel -o -`;

# janky hack to force gcc's output to be compatible with nasm assembly files
$data = join "\n",
	map s/\bmovabs\b/mov/gr, # translate movabs to nasm compatible instruction
	map s/\bPTR\b//gr, # remove the PTR prefix
	map s/(\d+)\[([^\[\]]+)\]/[$2+$1]/gr, # ???
	map s/([+\-]\d+)\[([^\[\]]+)\]/[$2$1]/gr, # ???
	grep /\A\s*[^\s\.]/, # strip any .labels
	split /\n/, $data; # parse lines

$data =~ s/(\w+:\s*)+\Z//sg; # strip more labels

# write to an assembly file
my $f = IO::File->new('tmpout.S', 'w');
$f->print("bits 64\n");
$f->print("$data");
$f->close;

# compile with nasm
`nasm -f bin tmpout.S -o tmpout.bin`;
my $compiled = `cat tmpout.bin`;
`rm tmpout.S tmpout.bin`;

# output the compiled assembly
print $compiled;
