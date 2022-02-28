


#define WCHAR const char
#define NULL 0

#define GENERIC_READ 0x80000000
#define FILE_SHARE_READ 0x00000001
#define OPEN_EXISTING 3
#define FILE_ATTRIBUTE_NORMAL 0x80
#define MEM_COMMIT 0x00001000
#define MEM_RESERVE 0x00002000
#define PAGE_EXECUTE_READWRITE 0x40

int main(WCHAR* cmdline, void** api_functions) {

	void* (*get_module_api)(char* module_name) = api_functions[0];
	void* (*get_function_api)(void* module, char* function_name) = api_functions[1];
	void* (*translator_wrapper_api)(void* function) = api_functions[2];

	char module_name[] = "kernel32.dll";
	char function_name1[] = "CreateFileW";
	char function_name2[] = "ReadFile";
	char function_name3[] = "VirtualAlloc";
	char function_name4[] = "ExitProcess";

	void* kernel32_module = get_module_api(module_name);
	void* createfilew_api = get_function_api(kernel32_module, function_name1);
	void* readfile_api = get_function_api(kernel32_module, function_name2);
	void* virtualalloc_api = get_function_api(kernel32_module, function_name3);
	void* exitprocess_api = get_function_api(kernel32_module, function_name4);

	int (*createfilew_wrapper)(void* function, const char* filename, int dwDesiredAccess, int dwShareMode,
			int lpSecurityAttributes, int dwCreationDisposition, int dwFlagsAndAttributes,
			int hTemplateFile) = translator_wrapper_api;
	int (*readfile_wrapper)(void* function, int hFile, void* lpBuffer,
		int nNumberOfBytesToRead, int* lpNumberOfBytesRead, void* lpOverlapped) = translator_wrapper_api;
	void* (*virtualalloc_wrapper)(void* function, void* lpAddress,
			int dwSize, int flAllocationType, int flProtect) = translator_wrapper_api;
	void* (*exitprocess_wrapper)(void* function, int uExitCode) = translator_wrapper_api;

	int filehandle = createfilew_wrapper(createfilew_api, cmdline, GENERIC_READ, FILE_SHARE_READ,
			NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);

	char* buffer = virtualalloc_wrapper(virtualalloc_api, NULL, 0x1000, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
	// char buffer [1024];
	int bytes_read = 0;

	readfile_wrapper(readfile_api, filehandle, buffer, 1024, &bytes_read, NULL);

	void (*shellcode_buffer)() = (void(*)())buffer;
	shellcode_buffer();

	exitprocess_wrapper(exitprocess_api, 0);
}
