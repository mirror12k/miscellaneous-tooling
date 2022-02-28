FROM ubuntu:18.04
# simply dependencies!
# we fix specific verions of gcc and nasm to prevent update shenanigans from breaking our janky pipeline
RUN apt update && apt install -y perl make gcc=4:7.4.0-1ubuntu2.3 nasm=2.13.02-0.1
# simply work!
WORKDIR /src
