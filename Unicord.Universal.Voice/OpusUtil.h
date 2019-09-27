#pragma once
#include <opus.h>
#include <winrt/base.h>
#include <comdef.h>

namespace winrt::Unicord::Universal::Voice
{
    class OpusUtil {
    public:
        static inline void OpusUtil::CheckOpusError(int32_t error, hstring message) {
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