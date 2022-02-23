#!/usr/bin/env perl
use strict;
use warnings;

use feature 'say';

use IO::File;

=pod

Cow Mangler utility!

Mangles source code by performing mutations, then verifying against known-good behavior of the code.
Use liberally.

=cut



# these fuctions are for configuring the harnessed application itself
# execute_file performs the test operations. Use as many flags and as many runs as necessary to test the full breadth of the application
sub execute_file { return `php -d error_reporting="E_ERROR | E_PARSE" -d max_execution_time=1 $_[0]`; }
# reduce_file performs cleaning operation, in this case removing all comments (modify as necessary to your language)
sub reduce_file { return $_[0] =~ s#/\*.*?\*/##rgs =~ s#//[^\n]*?(\n|\Z)##rgs }
# a strict size limit to prevent bloating (mutators have a habit of increasing dead weight)
sub limit_file { return length $_[0] > 5000 ? substr $_[0], 0, 5000 : $_[0] }

# utility functions
sub slurp_file { local $/; return IO::File->new(shift, 'r')->getline; }
sub dump_file { local $/; return IO::File->new(shift, 'w')->write(shift); }
sub rand_choice { return @_[int rand @_] }

# mutators
sub mutate_byte {
	my ($data) = @_;

	my $from_p = int rand -1 + length $data;
	my $to_p = int rand length $data;

	my $replace_length = int rand 3;

	substr($data, $to_p, $replace_length) = substr $data, $from_p, 1;

	return $data;
}

sub mutate_substitution {
	my ($data) = @_;

	my $from_p = int rand -1 + length $data;
	my $from_length = 1 + int rand -$from_p -1 + length $data;
	$from_length = $from_length > 64 ? 64 : $from_length;
	my $to_p = 1 + int rand length $data;
	my $to_length = int rand -$to_p + length $data;
	$to_length = $to_length > 16 ? 16 : $to_length;

	my $from = quotemeta substr $data, $from_p, $from_length;
	my $to = substr $data, $to_p, $to_length;

	$data =~ s/$from/$to/gs;

	return $data;
}

my %pair_map = (
	'(' => ')',
	'{' => '}',
	'[' => ']',
	'"' => '"',
	'\'' => '\'',
);

sub mutate_swap {
	my ($data) = @_;

	my @start_positions;
	push @start_positions, $-[0] while $data =~ /[\(\{\["']/g;

	my $from_p = rand_choice(@start_positions);
	my $c = substr($data, $from_p, 1);
	my $quoted_c = quotemeta $c;
	my $pair_c = quotemeta $pair_map{$c};

	my @end_positions;
	push @end_positions, $-[0] while $data =~ /$pair_c/g;
	@end_positions = grep $_ > $from_p, @end_positions;

	my $from_p_end = (shift @end_positions) // $from_p + 1;
	my $from_length = 1 + $from_p_end - $from_p;

	my @to_positions;
	push @to_positions, $-[0] while $data =~ /$quoted_c/g;

	my $to_p = rand_choice(@to_positions);

	my @to_end_positions;
	push @to_end_positions, $-[0] while $data =~ /$pair_c/g;
	@to_end_positions = grep $_ > $to_p, @to_end_positions;

	my $to_p_end = (shift @to_end_positions) // $to_p + 1;
	my $to_length = 1 + $to_p_end - $to_p;

	my $from = substr($data, $from_p, $from_length);
	my $to = substr($data, $to_p, $to_length);

	if ($from_p < $to_p) {
		substr($data, $to_p, $to_length) = $from;
		substr($data, $from_p, $from_length) = $to;
	} else {
		substr($data, $from_p, $from_length) = $to;
		substr($data, $to_p, $to_length) = $from;
	}


	# say "from: $from => to: $to";



	return $data;
}

sub mutate_multiply {
	my ($data) = @_;

	my $from_p = int rand -1 + length $data;
	my $from_length = 1 + int rand -$from_p -1 + length $data;

	my $count = int rand 3;

	substr($data, $from_p, $from_length) = substr ($data, $from_p, $from_length) x $count;

	return $data;
}

sub mutate {
	my ($data) = @_;

	my @funs = (\&mutate_byte, \&mutate_substitution, \&mutate_swap, \&mutate_multiply);

	return rand_choice(@funs)->($data);
}



# inputs
my $target_file = shift // die "target file required";
mkdir 'test';
my $test_file = "test/$target_file";
my $save_file = "test/$target_file.save";
my $data = slurp_file($target_file);
my $source_res = execute_file($target_file);

# mutate 100000 times or until user tired
foreach my $i (1 .. 100000) {
	my $mutated_data = limit_file(reduce_file(mutate($data)));
	dump_file($test_file, $mutated_data);

	my $res = execute_file($test_file);

	if ($source_res ne $res) {
		say "test failed";
	} else {
		say "test_succeeded";
		$data = $mutated_data;
		dump_file($save_file, $data);
		# dump_file("$test_file." . sprintf("%06d", $i), $data);
	}
}


# output result
say dump_file($test_file, $data);





