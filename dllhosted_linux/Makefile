

# run in a docker to prevent version differences in gcc/nasm from breaking everything
all:
	docker build . -t dllhosted_linux_compiler
	docker run -it -v ${PWD}:/src dllhosted_linux_compiler make build

# compile our c files
build:
	-mkdir bin
	./make_crosscompiled_dllhost.pl src/exec_calc_windows_x64.c
	./make_crosscompiled_dllhost.pl src/exec_cmd_windows_x64.c
	./make_crosscompiled_dllhost.pl src/exec_loadlib_windows_x64.c
	./make_crosscompiled_dllhost.pl src/exec_shellcode_windows_x64.c

clean:
	rm -rf bin/
	rmdir bin



