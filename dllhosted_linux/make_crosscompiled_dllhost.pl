#!/usr/bin/env perl
use strict;
use warnings;

use feature 'say';

use IO::File;



my $filepath = shift // die "missing argument";
my $basename = $filepath =~ s/\.c\Z//sir =~ s#\A.*/##sir;

# compile our c code to a string of assembly
my $asm = `./gcc_c_to_nasm_assembly.pl $filepath`;
# read our special exe file for injection
my $exe = `cat dllhost_64.exe`;


die "assembly too large (>0x700 bytes)! won't fit :X" if 0x700 < length $asm;
# insert shellcode into exe
substr($exe, 0xd00, length $asm) = $asm;

# output
my $f = IO::File->new("bin/$basename.exe", 'w');
$f->print("$exe");
$f->close;

say "compiled: bin/$basename.exe";

