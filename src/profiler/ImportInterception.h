#pragma once

#include "cor.h"

typedef struct _ImportInterception
{
	char* CallerAssembly;
	char* TargetAssemblyName;
	char* TargetMethodName;
	char* TargetTypeName;
	int TargetMethodParametersCount;
	char* InterceptorTypeName;
	char* InterceptorAssemblyName;
} ImportInterception;