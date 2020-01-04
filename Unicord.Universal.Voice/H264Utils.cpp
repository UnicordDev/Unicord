#include "pch.h"
#include "H264Utils.h"

namespace winrt::Unicord::Universal::Voice::Utilities {
    std::vector<H264NalIndex> H264Utils::FindNalIndices(array_view<uint8_t>& view) {
        std::vector<H264NalIndex> indices;

        if (view.size() < NAL_SHORT_START_SEQUENCE_SIZE) {
            return indices;
        }

        size_t end = view.size() - NAL_SHORT_START_SEQUENCE_SIZE;
        for (size_t i = 0; i < end;)
        {
            if (view[i + 2] > 1) {
                i += 3;
            }
            else if (view[i + 2] == 1) {
                if (view[i + 1] == 0 && view[i] == 0) {
                    // We found a start sequence, now check if it was a 3 of 4 byte one.
                    H264NalIndex index = { i, i + 3, 0 };
                    if (index.start_offset > 0 && view[index.start_offset - 1] == 0)
                        --index.start_offset;
                    // Update length of previous entry.
                    auto it = indices.rbegin();
                    if (it != indices.rend())
                        it->payload_size = index.start_offset - it->payload_start_offset;
                    indices.push_back(index);
                }
                i += 3;
            }
            else {
                ++i;
            }
        }

        auto it = indices.rbegin();
        if (it != indices.rend())
            it->payload_size = view.size() - it->payload_start_offset;

        return indices;
    }
}