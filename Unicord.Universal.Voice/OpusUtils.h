#pragma once
#include <opus.h>
#include <winrt/base.h>
#include <comdef.h>

namespace winrt::Unicord::Universal::Voice::Utilities
{
    class OpusUtils {
    public:
        static inline void OpusUtils::CheckOpusError(int32_t error, hstring message) {
            switch (error)
            {
            case OPUS_BAD_ARG:
            case OPUS_BUFFER_TOO_SMALL:
            case OPUS_INVALID_PACKET:
                throw winrt::hresult_invalid_argument(message);
            case OPUS_INTERNAL_ERROR:
            case OPUS_INVALID_STATE:
                throw winrt::hresult_error(E_UNEXPECTED, message);
            case OPUS_UNIMPLEMENTED:
                throw winrt::hresult_not_implemented(message);
            case OPUS_ALLOC_FAIL:
                throw winrt::hresult_error(E_OUTOFMEMORY, message);
            default:
                return;
            }
        }
    };
}