#pragma once

#include <stdint.h>
#include "winrt/base.h"

using namespace winrt;
namespace winrt::Unicord::Universal::Voice::Interop
{
    struct ConnectionEndpoint
    {
        hstring hostname;
        uint16_t port;
    };
}