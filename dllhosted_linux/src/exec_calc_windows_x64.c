


#define WCHAR const char
#define NULL 0

int main(WCHAR* cmdline, void** api_functions) {

	void* (*get_module_api)(char* module_name) = api_functions[0];
	void* (*get_function_api)(void* module, char* function_name) = api_functions[1];
	void* (*translator_wrapper_api)(void* function) = api_functions[2];

	char module_name1[] = "kernel32.dll";
	char module_name2[] = "msvcrt.dll";
	char function_name1[] = "system";
	char function_name2[] = "ExitProcess";

	void* kernel32_module = get_module_api(module_name1);
	void* msvcrt_module = get_module_api(module_name2);
	void* system_api = get_function_api(msvcrt_module, function_name1);
	void* exitprocess_api = get_function_api(kernel32_module, function_name2);

	void* (*system_wrapper)(void* function, const char *command) = translator_wrapper_api;
	void* (*exitprocess_wrapper)(void* function, int uExitCode) = translator_wrapper_api;

	char cmd[] = "calc";

	system_wrapper(system_api, cmd);

	exitprocess_wrapper(exitprocess_api, 0);
}
