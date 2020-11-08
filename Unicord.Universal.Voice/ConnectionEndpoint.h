#pragma once

#include "winrt/base.h"
#include <stdint.h>

using namespace winrt;
namespace winrt::Unicord::Universal::Voice::Interop {
    struct ConnectionEndpoint {
        hstring hostname;
        uint16_t port;
    };
}