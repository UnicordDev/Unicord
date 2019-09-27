#pragma once

#include <stdint.h>
#include "winrt/base.h"

namespace winrt::Unicord::Universal::Voice
{
    struct ConnectionEndpoint
    {
        hstring hostname;
        uint16_t port;
    };
}