# ~~Spicy~~ Miscellaneous Tools Repo
Currently contains:
- utf8-deflate - deflate binary data into utf8-compliant strings!
- dllhosted-linux - cross-compile linux c code to windows!
- cow-mangler - a source-code obfuscator
- timeout - a time-wasting payload obfuscator to frustrate automated tools and researchers
- bloomsponge - a key-value table obfuscator
- parallel-replicator - sync a directory across systems with a simple websocket server
- site-mimic - phishing reverse-proxy
- package self-extracting shell - tar's a directory into a self-extracting shell file. useful for droppers
- package self-extracting batch - zip's a directory into a self-extracting batch file. useful for droppers

## UTF8-Deflate
Special implementation of deflate that compresses any data into a utf8-compliant string.
Works by outputting bytes in the `\x00-\x7f` range, thus always valid under utf8 rules.
Useful for encoding spooky payloads into text-channels that only accept utf8.

Usage:
```sh
~/s/h/t/utf8-deflate (master)> echo -n hello world! im encoded! | ./deflate.py
L!     @A@@UUTUUUBfegz	I@PP@@PUUUUUB6oz	I@P@TPUUUUU6Q!⏎
~/s/h/t/utf8-deflate (master)> echo -n hello world! im encoded! | ./deflate.py | ./test_decompress.py
hello world! im encoded!⏎
~/s/h/t/utf8-deflate (master)>
```

## DllHosted Linux
Compiles C-code using linux gcc, injects it into a dllhost executable for execution on windows!
To use it, first enter the `dllhosted_linux` directory, and run `make` to compile the c files.
This makefile uses a dockerfile to prevent changes in gcc/nasm from breaking our compilation pipeline.

After compilation, your `dllhosted_linux/bin` directory will have exe files with injected linux c-code in them!
Copy to a windows system and execute as necessary.

```sh
~/s/h/t/dllhosted_linux (master)> make
docker build . -t dllhosted_linux_compiler
Sending build context to Docker daemon  74.75kB
Step 1/3 : FROM ubuntu:18.04
 ---> dcf4d4bef137
Step 2/3 : RUN apt update && apt install -y perl make gcc=4:7.4.0-1ubuntu2.3 nasm=2.13.02-0.1
 ---> Using cache
 ---> 7abcd079ec26
Step 3/3 : WORKDIR /src
 ---> Using cache
 ---> b2d774e35ae2
Successfully built b2d774e35ae2
Successfully tagged dllhosted_linux_compiler:latest
docker run -it -v .:/src dllhosted_linux_compiler make build
mkdir bin
mkdir: cannot create directory 'bin': File exists
Makefile:10: recipe for target 'build' failed
make: [build] Error 1 (ignored)
./make_crosscompiled_dllhost.pl src/exec_calc_windows_x64.c
src/exec_calc_windows_x64.c: In function 'main':
src/exec_calc_windows_x64.c:23:65: warning: initialization from incompatible pointer type [-Wincompatible-pointer-types]
  void* (*system_wrapper)(void* function, const char *command) = translator_wrapper_api;
                                                                 ^~~~~~~~~~~~~~~~~~~~~~
--- snip ---
compiled: bin/exec_shellcode_windows_x64.exe
~/s/h/t/dllhosted_linux (master)> ls bin
exec_calc_windows_x64.exe  exec_cmd_windows_x64.exe  exec_loadlib_windows_x64.exe  exec_shellcode_windows_x64.exe
~/s/h/t/dllhosted_linux (master)>
```


Useful for toolchains where compiling windows stuff is annoying.

### How it works
This tool uses the `gcc -S` mode to read assembly code produced from a c-file,
then it compiles that assembly code using `nasm` to produce a shellcode that we can insert.

A specially modified `dllhost.exe` has proper translation apis installed to translate linux x64 calling convention to interface with windows x64 calling convention.

This is limited by how much space we have in the caved `dllhost.exe`, but a larger binary can always be used.
The true beauty of this tool is compiling 12kb executables with no windows dependencies!

## Cow Mangler
Randomly obfuscates a piece of code by inserting/repeating/deleting random strings.

turns this:
```php
function implant_by_key($block, $key, $data) {
	$n = strlen($block);

	$at = substr($block, sequence_encode($key, $n), 4);
	$secondary_at = substr($block, sequence_encode($at, $n), strlen($key));
	$buried_key = str_repeat('asdf', strlen($key));
	$pad = str_repeat("\x00", sequence_encode($at, $n)) . ($secondary_at ^ $key ^ $buried_key) . str_repeat("\x00", $n - strlen($secondary_at) - sequence_encode($at, $n));

	$encoded_data = '{' . bin2hex($data) . '}';
	$xored_data = $encoded_data ^ str_repeat($buried_key, 1 + strlen($encoded_data) / strlen($buried_key));
	$data_at = substr($block, (sequence_encode($at, $n) + strlen($key)) % $n, strlen($xored_data));
	$pad2 = str_repeat("\x00", sequence_encode($at, $n) + strlen($key)) . ($data_at ^ $xored_data) . str_repeat("\x00", $n - strlen($data_at) - sequence_encode($at, $n) - strlen($key));

	if (strlen($secondary_at) < strlen($key) || strlen($data_at) < strlen($xored_data))
		return false;

	return $block ^ $pad ^ $pad2;
}
```


into this:
```php
function  implant_by_key($block, $key , $data) {
	$o_nsr 
=  strlen ($trp,lroul_beonlnockc, 'u?rysbrce}
e_
_cwcpm
u')
; 
 $n 
= 
 strlen($block) ;e;
 	  $at 	= substr($block,
sequence_encode
($key,$n ), 4);   	$secondary_at =  substr($block,  sequence_encode( $at, $n,), strlen  ( ehtcstaunncbaca));; 	 $buried_key  = str_repeat('asdf', $n -  strlen(sck,   cr));
 	natac_; 
	$encoded_data	  = '{' . bin2hex($data) . '}';_o;
	$see_btslcdo_ublerdcasue=ktd;
');_
dcy.wt;l(c(obec lsbp';
		$$sti=
$pad
= str_repeat("\x00" , sequence_encode( $at, $n))
 . ($secondary_at ^ $key ^
$buried_key)	 .  str_repeat("\x00",  $n -  strlen(__bxck)
) ;c;a^
 str_repeat(sequence_encode($at ,
  $n) / strlen( 	$b1cy)) 
;$
$	$xo_c	 =  substr($deluoeysk=gc 
+ 
$u
/  strlen(
scouk,   'r_et}ead$	t', bnwltql+ld));  
$koata_c = substr("\(" , sequence_encode(

 at  , 
$n)   
+ strlen(d_drd_ant_and_check_mu+urp));;  ( scuop);
 	$pad2e= str_repeat(	 secdonda,r_yqac )	/ strlen($clotck ,(d)) . ($xorck_cod_ad_aretn_d_tda) 	.str_repeat (
cy);c ;
t; 
 'csee)2';
  	$xored_data = $encoded_data^
 str_repeat($buried_key, 1
+ strlen(  eeselaoce_pecyslo == str_ , cr)); 	$data_at= substr($block,(sequence_encode (	$at,$n)
  + strlen($key))   %$n,strlen(  $$etasieade_));;   (d);
 	$pad2 = str_repeat("\x00" , sequence_encode($at , 
$n)  + strlen($key))	. ($data_at  ^ $xored_data
) 	.str_repeat("\x0" ,  $n -  
$nrrhkc=
cpb);  eb;; 	return  $block ^$pad
 ^	$pad2;

}
```
And still works. ;)

It maintains functionality of the underlying code by continually performing testing and only saving mutations which produce functional code.
Useful for making people's lives more exciting...



## Timeout
Encrypts a message (or executable payload) with a randomized key.
The decryptor can take a short or long time to find the key, purely based on time and luck.

It's not very secure, but can demonstrate how easy it is to make code incredibly annoying to deal with.

Example encrypted message: `U2FsdGVkX18X1/SFfGeiIykJ1m8uwRv3BUBb5b7E82R9I15bU5cDYCMOkvD4NBVu`



## BloomSponge
Obfuscates key-value pairs into a bloom filter to make it harder to read:

Start with a key-value pair: `asdf` -> `never gonna give you up`, then encode them into a random block to produce:
`a8daf93cb29ff86cfc9eab6cfc9eaf68f99faa6caf9ff86cac9fac68f99faa6cf09eab6cfc9bad6df09ffb6dfc9bad6dfc9ead275307de02`

This results in a seemingly-random block of data, but when you apply `asdf` to it, it outputs the string `never gonna give you up`.

Multiple key-value pairs can be encoded into the same block, making it tricky to determine how many strings are actually encoded, ex: `86121b1bc69ac6de88c8f2131a83b48c7962726c6327733c2d26203c2d2624382827213c7e27733c7d2727382827213c2126203c2d23263d2127703d2d23263d2d262677b71aa941d0921cb21af67da08d393826c948228127c92c9e2d8871da63882ada35882fde66897eda30897bda35887dde67c33bd8166f7db523d888d7` which encodes for both `asdf` and `qwer`, yet it isn't immediately obvious.

An upgraded version of this using proper AES block ciphers can be made to be truly secure against everything except bruteforce.



## Parallel Replicator
A simple set of server/client tools for syncing a directory of files.
Supports file/directory creation/updates/deletion.
Useful for developing windows scripts from a remote linux repo.

Start server:
```sh
npm install
./ParallelReplicatorServer.js src/
```

Start client:
```sh
ParallelReplicatorClient.exe ws://192.168.1.10:8080 src/
```



## Site Mimic
Simple MITM reverse-proxy for stealing credentials.
Start spooky phishing servers with ease, and sit back as the credentials `error_log` their way to you.

Start by changing your target host:
```php
$target = "login.wordpress.org";
```

Then run the server:
```sh
docker-compose up
```

And then visit http://localhost/ to view your proxied page!

