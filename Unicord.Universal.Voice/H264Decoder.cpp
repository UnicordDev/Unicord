#include "pch.h"
#include <algorithm>
#include <iostream>
#include <sstream>
#include <iomanip>
#include "H264Decoder.h"
#include "Rtp.h"

namespace winrt::Unicord::Universal::Voice::Decode
{
    bool H264Decoder::ProcessPacket(const RtpPacket& header, array_view<uint8_t>& decrypted_view, H264Frame& frame)
    {      
        std::vector<H264NalIndex> nalIndices = H264Utils::FindNalIndices(decrypted_view);

        if (header.marker) {

            std::ostringstream str;
            for (uint32_t i = 0; i < decrypted_view.size(); i++)
            {
                str << std::hex << std::setfill('0') << std::setw(2) << (uint32_t)decrypted_view[i] << " ";
            }
            std::cout << str.str() << std::endl;

        }

        return false;
    }
}