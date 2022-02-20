<?php
/**
 * Bloom Sponge implementation
 * 
 * Generates a bloom-filter which works as a key-value table.
 * The entire point of this is to obfuscate a map table to keep prying eyes guessing.
 * This is not secure by any measure, it's only meant to make it it harder to decode information.
 * Mix and match with other obfuscations for best results. :)
 * 
 */



// a psuedo random algorithm which converts strings to integers in a simple manner
// a possible improvement would be to md5'ing the string and then modulo'ing the large number down to size
function sequence_encode($s, $n) {
	$p = 1;
	$i = 0;
	foreach (str_split($s) as $c) {
		$p = (3 + $p * ord($c) + ord($c) << $i) % $n;
		$i = ($i + 1) % 8;
	}
	return $p % $n;
}



// this function implants a piece of data with the given key into the randomized block
function implant_by_key($block, $key, $data) {
	$n = strlen($block);

	// find the 4 byte pointer from the data sequence
	$at = substr($block, sequence_encode($key, $n), 4);
	// secondary pointer is necessary so space out the blocks so that similar codes will still produce radically different locations
	// it also helps with keeping the data positions random
	$secondary_at = substr($block, sequence_encode($at, $n), strlen($key));
	// this is the randomized xor key that we bury to obfuscate the data
	$buried_key = random_bytes(strlen($key));
	// now we replace a segment of the block with the buried key
	$pad = str_repeat("\x00", sequence_encode($at, $n)) . ($secondary_at ^ $key ^ $buried_key) . str_repeat("\x00", $n - strlen($secondary_at) - sequence_encode($at, $n));

	// pack our data (a possible improvement here would be adding a simple MAC by md5'ing the data)
	$encoded_data = '{' . bin2hex($data) . '}';
	// encrypt it with a repeated xor key
	$xored_data = $encoded_data ^ str_repeat($buried_key, 1 + strlen($encoded_data) / strlen($buried_key));
	// cut out the existing data
	$data_at = substr($block, (sequence_encode($at, $n) + strlen($key)) % $n, strlen($xored_data));
	// now replace the existing data with our weakly encrypted data
	$pad2 = str_repeat("\x00", sequence_encode($at, $n) + strlen($key)) . ($data_at ^ $xored_data) . str_repeat("\x00", $n - strlen($data_at) - sequence_encode($at, $n) - strlen($key));

	// error_log("encoding::");
	// error_log("\t n: $n");
	// error_log("\t at: " . bin2hex($at));
	// error_log("\t secondary_at: " . bin2hex($secondary_at));
	// error_log("\t pad: " . bin2hex($pad));
	// error_log("\t buried_key: " . bin2hex($buried_key));
	// error_log("\t encoded_data: " . ($encoded_data));
	// error_log("\t xored_data: " . bin2hex($xored_data));

	// check that there was enough room for us to encode
	if (strlen($secondary_at) < strlen($key) || strlen($data_at) < strlen($xored_data))
		return false;

	// if there was enough room, we composite all of the deltas together and return the new block
	return $block ^ $pad ^ $pad2;
}


// this function extracts a piece of data for the given key and randomized block, returns false if there wasn't any data implanted under this key
function extract_by_key($block, $key) {
	$n = strlen($block);

	// find the 4 byte pointer from the data sequence
	$at = substr($block, sequence_encode($key, $n), 4);
	// grab the decode key (an improvement could be md5'ing it here to prevent combing)
	$decode_key = $key ^ substr($block, sequence_encode($at, $n), strlen($key));
	// decrypt the rest of the block with the decode key (an improvement here could be using a proper block cipher and proper cipher mode)
	$possible_block = substr($block, sequence_encode($at, $n) + strlen($key)) ^ str_repeat($decode_key, 1 + $n / strlen($decode_key));
	// verify that we have a block here
	$is_block = $possible_block[0] === '{';
	// find the length of the sequence
	$cutoff = strpos($possible_block, '}');
	// check that the block has a cutoff
	$is_complete = $cutoff !== false;
	// grab the decoded data
	$decoded_data = $is_block && $is_complete ? hex2bin(substr($possible_block, 1, $cutoff - 1)) : false;

	// error_log("trying to decode::");
	// error_log("\t n: $n");
	// error_log("\t at: " . bin2hex($at));
	// error_log("\t decode_key: " . bin2hex($decode_key));
	// error_log("\t possible_block: " . $possible_block);
	// error_log("\t is_block: " . $is_block);
	// error_log("\t is_complete: " . $is_complete);
	// error_log("\t decoded_data: " . $decoded_data);

	// check that there was a block there to decode
	if (!$is_block || !$is_complete)
		return false;

	// return the result of hex2bin
	return $decoded_data;
}

// this implants and checks the result for correctness
function implant_and_check($block, $key, $data) {
	// implant the data
	$block = implant_by_key($block, $key, $data);

	// check for errors
	if ($block === false || extract_by_key($block, $key) !== $data)
		return false;

	return $block;
}

// this implants multiple pieces of data and then checks for correctness
function implant_and_check_multiple($block, $key_data_pairs) {
	// implant data
	foreach ($key_data_pairs as $key => $data) {
		$block = implant_by_key($block, $key, $data);
		if ($block === false)
			return false;
	}

	// check for errors
	foreach ($key_data_pairs as $key => $data) {
		if (extract_by_key($block, $key) !== $data)
			return false;
	}

	return $block;
}


// generate a pure random 4096 byte block for us to use
// use larger size when encoding more data
$block = random_bytes(4096);

// this implants three pieces of data under three keys
$result_block = implant_and_check_multiple($block, [
	'Apl1' => 'who turned out the lights?',
	'Apl2' => 'hello world!!!',
	'Apl3' => 'goodbye_world!!!',
]);

// example usage of the block to extract data by key
$result2 = $result_block ? extract_by_key($result_block, 'Apl1') : false;
error_log("extract[Apl1]: " . ($result2));
$result2 = $result_block ? extract_by_key($result_block, 'Apl2') : false;
error_log("extract[Apl2]: " . ($result2));
$result2 = $result_block ? extract_by_key($result_block, 'Apl3') : false;
error_log("extract[Apl3]: " . ($result2));
$result2 = $result_block ? extract_by_key($result_block, 'Apl4') : false;
error_log("extract[Apl4]: " . ($result2));

// prints the resulting block
echo($result_block);
