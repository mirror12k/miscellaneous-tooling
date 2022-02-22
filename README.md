# Spicy Tools Repo
Currently contains:
- bloomsponge - a key-value table obfuscator
- timeout - a time-wasting payload obfuscator to frustrate automated tools and researchers

## BloomSponge
Obfuscates key-value pairs into a bloom filter to make it harder to read:

Start with a key-value pair: `asdf` -> `never gonna give you up`, then encode them into a random block to produce:
`a8daf93cb29ff86cfc9eab6cfc9eaf68f99faa6caf9ff86cac9fac68f99faa6cf09eab6cfc9bad6df09ffb6dfc9bad6dfc9ead275307de02`

This results in a seemingly-random block of data, but when you apply `asdf` to it, it outputs the string `never gonna give you up`.

Multiple key-value pairs can be encoded into the same block, making it tricky to determine how many strings are actually encoded, ex: `86121b1bc69ac6de88c8f2131a83b48c7962726c6327733c2d26203c2d2624382827213c7e27733c7d2727382827213c2126203c2d23263d2127703d2d23263d2d262677b71aa941d0921cb21af67da08d393826c948228127c92c9e2d8871da63882ada35882fde66897eda30897bda35887dde67c33bd8166f7db523d888d7` which encodes for both `asdf` and `qwer`, yet it isn't immediately obvious.

An upgraded version of this using proper AES block ciphers can be made to be truly secure against everything except bruteforce.

## Timeout
Encrypts a message (or executable payload) with a randomized key.
The decryptor can take a short or long time to find the key, purely based on time and luck.

It's not very secure, but can demonstrate how easy it is to make code incredibly annoying to deal with.

Example encrypted message: `U2FsdGVkX18X1/SFfGeiIykJ1m8uwRv3BUBb5b7E82R9I15bU5cDYCMOkvD4NBVu`





