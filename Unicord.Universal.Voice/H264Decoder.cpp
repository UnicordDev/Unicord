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
        std::ostringstream stream;
        stream << std::internal << std::setfill('0') << std::setw(2) << std::hex;
        for (size_t i = 0; i < min(decrypted_view.size(), 64); i++)
        {
            stream << (uint32_t)decrypted_view[i] << " ";
        }
        std::cout << stream.str() << std::endl;

        H264Packet packet;
        packet.header = header;

        uint16_t header_bullshit_length;
        memcpy_s(&header_bullshit_length, sizeof header_bullshit_length, decrypted_view.data(), sizeof header_bullshit_length);

        while (((uint32_t)decrypted_view[header_bullshit_length] >> 7) & 0x01) {
            header_bullshit_length++;
        }

        packet.data.resize(decrypted_view.size() - header_bullshit_length);
        std::copy(decrypted_view.begin() + header_bullshit_length, decrypted_view.end(), packet.data.data());

        std::vector<H264Packet> data;
        auto stampPos = packetQueue.find(header.timestamp);
        if (stampPos != packetQueue.end()) {
            data = packetQueue[header.timestamp];
        }

        if (header.marker) {

            data.push_back(packet);
            std::sort(data.begin(), data.end(), [](const H264Packet& lhs, const H264Packet& rhs) -> bool { return lhs.header.seq > rhs.header.seq; });

            frame = H264Frame();
            for (size_t i = 0; i < data.size(); i++) {
                H264Packet toProcess = data.at(i);

                uint32_t nal_header_nri = ((uint32_t)toProcess.data[0] >> 5) & 0x03;
                uint32_t nal_ref_idc = ((uint32_t)toProcess.data[0] >> 5) & 0x03;
                uint32_t nal_header_type = ((uint32_t)toProcess.data[0] >> 0) & 0x1F;
                uint32_t nal_header_f_bit = ((uint32_t)packet.data[0] >> 7) & 0x01;

                std::cout <<
                    "idc:" << nal_ref_idc << 
                    " nri:" << nal_header_nri << 
                    " f:" << nal_header_f_bit <<
                    " type:" << nal_header_type <<
                    " header_size:" << toProcess.header.size() <<
                    " had_extension:" << toProcess.header.extension <<
                    std::endl;

                // TODO: different NALs
                if (nal_header_type >= 1 && nal_header_type <= 23) {
                    
                    if (nal_header_type == 7) {
                        frame.sps = toProcess.data;
                    }
                    else if (nal_header_type == 8) {
                        frame.pps = toProcess.data;
                    }
                    else {
                        frame.data.push_back(toProcess.data);
                    }
                }
            }

            packetQueue.unsafe_erase(header.timestamp);
            return true;
        }
        else {
            data.push_back(packet);
            packetQueue[header.timestamp] = data;
        }

        return false;
    }
}