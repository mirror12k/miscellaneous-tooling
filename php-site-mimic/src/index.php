<?php

function request_url($method, $url, $target) {
    // Initiate the curl session
    $ch = curl_init();
    curl_setopt($ch, CURLOPT_URL, $url);
    // set method
    curl_setopt($ch, CURLOPT_CUSTOMREQUEST, $method);
    // have headers
    curl_setopt($ch, CURLOPT_HEADER, 1);
    // get output
    curl_setopt($ch, CURLOPT_RETURNTRANSFER, 1);
    // Execute the curl session
    $output = curl_exec($ch);

    // get the code
    $http_code = curl_getinfo($ch, CURLINFO_HTTP_CODE);

    // get the header and body
    $header_size = curl_getinfo($ch, CURLINFO_HEADER_SIZE);
	$header = substr($output, 0, $header_size);
	$body = substr($output, $header_size);

	$headers = explode( "\r\n" , $header );

    // shift the status line out of the headers
    array_shift($headers);

    // send the status code and the headers
    http_response_code($http_code);
    foreach ($headers as $h) {
    	if (strlen($h) > 0) {
		    header($h, false);
    	}
    }
    // Close the curl session
    curl_close($ch);

    // some substitutions
    $body = str_replace($target, 'localhost', $body);
    $body = str_replace('https://localhost', 'http://localhost', $body);

    return $body;
}




$target = "login.wordpress.org";
// echo "your path: " . $_SERVER['REQUEST_URI'];

$req = $entityBody = file_get_contents('php://input');

if (strlen($req) > 0)
    error_log("captured message body: $req");

echo request_url($_SERVER['REQUEST_METHOD'], "https://$target$_SERVER[REQUEST_URI]", $target);
// echo file_get_contents("https://$target$_SERVER[REQUEST_URI]");
