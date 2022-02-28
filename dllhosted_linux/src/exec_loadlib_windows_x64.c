


#define WCHAR const char
#define NULL 0

int main(WCHAR* cmdline, void** api_functions) {

	void* (*get_module_api)(char* module_name) = api_functions[0];
	void* (*get_function_api)(void* module, char* function_name) = api_functions[1];
	void* (*translator_wrapper_api)(void* function) = api_functions[2];

	char module_name1[] = "kernel32.dll";
	char function_name1[] = "LoadLibraryA";
	char function_name2[] = "ExitProcess";

	void* kernel32_module = get_module_api(module_name1);
	void* loadlibrarya_api = get_function_api(kernel32_module, function_name1);
	void* exitprocess_api = get_function_api(kernel32_module, function_name2);

	void* (*system_wrapper)(void* function, const char *command) = translator_wrapper_api;
	void* (*loadlibrarya_wrapper)(void* function, const char *lpLibFileName) = translator_wrapper_api;
	void* (*exitprocess_wrapper)(void* function, int uExitCode) = translator_wrapper_api;

	loadlibrarya_wrapper(loadlibrarya_api, cmdline);
	exitprocess_wrapper(exitprocess_api, 0);
}
