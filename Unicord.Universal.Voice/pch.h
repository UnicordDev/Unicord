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

#include <wrl/client.h>

#define WEBRTC_WIN
#define ABSL_USES_STD_STRING_VIEW
#undef min
#undef max