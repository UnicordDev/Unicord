#include "pch.h"
#include <algorithm>
#include <iostream>
#include <sstream>
#include <iomanip>
#include "H264Decoder.h"
#include "Rtp.h"

namespace winrt::Unicord::Universal::Voice::Decode
{
    bool H264Decoder::ProcessPacket(const RtpHeader& header, array_view<uint8_t>& decrypted_view, H264Frame& frame)
    {      
        std::vector<H264NalIndex> nalIndices = H264Utils::FindNalIndices(decrypted_view);
        std::cout << nalIndices.size() << "\n";
        return false;
    }
}