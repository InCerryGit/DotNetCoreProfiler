#pragma once

#include <vector>
#include <windows.h>
#include "types.h"

namespace util
{
	WCHAR operator"" _W(const char c);
	wstring operator"" _W(const char* arr, size_t size);
	wstring ToWSTRING(const char* str);
	wstring ToWSTRING(const std::string& str);
	std::string ToString(const wstring& wstr);

	std::string GetEnvironmentValue(const std::string& name);

	wstring Trim(const wstring& str);
	std::string Trim(const std::string& str);

	wstring ToString(const std::vector<WCHAR>& data, size_t length);
	std::vector<WCHAR> ToRaw(const wstring& str);
	std::vector<BYTE> ToRaw(PCCOR_SIGNATURE signature, ULONG length);
}