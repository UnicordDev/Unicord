#pragma once
#define NOMINMAX
#define WEBRTC_WIN
#define ABSL_USES_STD_STRING_VIEW

#include <unknwn.h>
#include <ppltasks.h>
#include <pplawait.h>
#include <gsl/gsl>
#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Foundation.Diagnostics.h>
#include <winrt/Windows.Foundation.Collections.h>
#include <winrt/Windows.Data.Json.h>
#include <winrt/Windows.Networking.Sockets.h>
#include <winrt/Windows.System.Threading.h>
#include <winrt/Windows.Storage.Streams.h>
#include <wrl/client.h>
