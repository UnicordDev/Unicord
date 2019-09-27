#pragma once
#include <unknwn.h>
#include <ppltasks.h>
#include <pplawait.h>
#include <gsl/gsl>
#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Foundation.Diagnostics.h>
#include <winrt/Windows.Foundation.Collections.h>
#include <winrt/Windows.Networking.Sockets.h>
#include <winrt/Windows.System.Threading.h>

#define CLEANUP_SAFE(x, methodName) if (x != nullptr) { x.methodName(); x = nullptr; }
#define SAFE_CLOSE(x) CLEANUP_SAFE(x, Close)
#define SAFE_CANCEL(x) CLEANUP_SAFE(x, Cancel)