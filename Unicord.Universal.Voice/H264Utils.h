#pragma once
#include "Rtp.h"

// heavily based on the webrtc h264 impl

namespace winrt::Unicord::Universal::Voice::Utilities {

    using namespace winrt::Unicord::Universal::Voice::Transport;

    // The size of a full NAL start sequence {0 0 0 1}, used for the first NALU
    // of an access unit, and for SPS and PPS blocks.
    const size_t NAL_LONG_START_SEQUENCE_SIZE = 4;
    // The size of a shortened NAL start sequence {0 0 1}, that may be used if
    // not the first NAL of an access unit or an SPS or PPS block.
    const size_t NAL_SHORT_START_SEQUENCE_SIZE = 3;
    // The size of the NAL type byte (1).
    const size_t NAL_TYPE_SIZE = 1;

    enum H264NalType : uint8_t {
        Slice = 1,
        Idr = 5,
        Sei = 6,
        Sps = 7,
        Pps = 8,
        Aud = 9,
        EndOfSequence = 10,
        EndOfStream = 11,
        Filler = 12,
        StapA = 24,
        FuA = 28
    };

    enum H264SliceType : uint8_t { 
        P = 0,
        B = 1,
        I = 2, 
        Sp = 3, 
        Si = 4 
    };

    struct H264Packet {
        RtpPacket header;
        std::vector<uint8_t> data;
    };

    struct H264Frame {
        std::vector<uint8_t> sps;
        std::vector<uint8_t> pps;
        std::vector<std::vector<uint8_t>> data;
    };

    struct H264NalIndex {
        // Start index of NALU, including start sequence.
        size_t start_offset;
        // Start index of NALU payload, typically type header.
        size_t payload_start_offset;
        // Length of NALU payload, in bytes, counting from payload_start_offset.
        size_t payload_size;
    };

    class H264Utils {
    public:
        
        static std::vector<H264NalIndex> FindNalIndices(array_view<uint8_t>& view);
        static H264NalType ParseNalType(uint8_t nal_type);
        static void DecodeEscapedSequence();
        static void EncodeEscapedSequence();

    };
}