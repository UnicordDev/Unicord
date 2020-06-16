#include "pch.h"
#ifdef _WIN32
#include <WinSock2.h>
#include <Windows.h>
#endif //_WIN32

#include "AudioDeviceWasapi.h"

#ifdef CPPWINRT_VERSION

#pragma warning(disable: 4995)  // name was marked as #pragma deprecated

#if (_MSC_VER >= 1310) && (_MSC_VER < 1400)
// Reports the major and minor versions of the compiler.
// For example, 1310 for Microsoft Visual C++ .NET 2003. 1310 represents
// version 13 and a 1.0 point release.
// The Visual C++ 2005 compiler version is 1400.
// Type cl /? at the command line to see the major and minor versions of your
// compiler along with the build number.
#pragma message(">> INFO: Windows Core Audio is not supported in VS 2003")
#endif

#include <assert.h>
#include <string.h>

#include <windows.h>
#include <comdef.h>
#include <dmo.h>
#include <Functiondiscoverykeys_devpkey.h>
#include <mmsystem.h>
#include <endpointvolume.h>
#include <strsafe.h>
#include <uuids.h>
#include <ppltasks.h>


#include <rtc_base/scoped_ref_ptr.h>
#include <media/base/videocapturerfactory.h>


#include <modules/audio_device/audio_device_config.h>
#include <system_wrappers/include/sleep.h>
#include <rtc_base/logging.h>
#include <rtc_base/win32.h>
#include <rtc_base/refcountedobject.h>

#include <zsLib/String.h>
#include <zsLib/IMessageQueueThread.h>


#ifdef CPPWINRT_VERSION
ZS_DECLARE_PROXY_IMPLEMENT(webrtc::IAudioDeviceWasapiDelegate)
ZS_DECLARE_PROXY_SUBSCRIPTIONS_IMPLEMENT(webrtc::IAudioDeviceWasapiDelegate,
    webrtc::IAudioDeviceWasapiSubscription)
#endif  // CPPWINRT_VERSION

    using zsLib::String;
using zsLib::Time;
using zsLib::Seconds;
using zsLib::Milliseconds;
using zsLib::AutoRecursiveLock;

using winrt::Windows::Foundation::TypedEventHandler;


#define KSAUDIO_SPEAKER_MONO        (SPEAKER_FRONT_CENTER)
#define KSAUDIO_SPEAKER_STEREO      (SPEAKER_FRONT_LEFT | \
                                     SPEAKER_FRONT_RIGHT)
#define KSAUDIO_SPEAKER_QUAD        (SPEAKER_FRONT_LEFT  | \
                                     SPEAKER_FRONT_RIGHT | \
                                     SPEAKER_BACK_LEFT   | \
                                     SPEAKER_BACK_RIGHT)

#define KSAUDIO_SPEAKER_SURROUND    (SPEAKER_FRONT_LEFT   | \
                                     SPEAKER_FRONT_RIGHT  | \
                                     SPEAKER_FRONT_CENTER | \
                                     SPEAKER_BACK_CENTER)

#define KSAUDIO_SPEAKER_5POINT1     (SPEAKER_FRONT_LEFT    | \
                                     SPEAKER_FRONT_RIGHT   | \
                                     SPEAKER_FRONT_CENTER  | \
                                     SPEAKER_LOW_FREQUENCY | \
                                     SPEAKER_BACK_LEFT     | \
                                     SPEAKER_BACK_RIGHT)

#define KSAUDIO_SPEAKER_7POINT1     (SPEAKER_FRONT_LEFT           | \
                                     SPEAKER_FRONT_RIGHT          | \
                                     SPEAKER_FRONT_CENTER         | \
                                     SPEAKER_LOW_FREQUENCY        | \
                                     SPEAKER_BACK_LEFT            | \
                                     SPEAKER_BACK_RIGHT           | \
                                     SPEAKER_FRONT_LEFT_OF_CENTER | \
                                     SPEAKER_FRONT_RIGHT_OF_CENTER)

// These defines are not available for Windows Store applications.
// However, those flags are needed and acccepted by WASAPI in order to support
// multichannel devices
#define AUDCLNT_STREAMFLAGS_AUTOCONVERTPCM      0x80000000
#define AUDCLNT_STREAMFLAGS_SRC_DEFAULT_QUALITY 0x08000000

// Macro that calls a COM method returning HRESULT value.
#define EXIT_ON_ERROR(hres)         do { if (FAILED(hres)) goto Exit; } while (0)

// Macro that continues to a COM error.
#define CONTINUE_ON_ERROR(hres) do { if (FAILED(hres)) goto Next; } while (0)

// Macro that releases a COM object if not NULL.
#define SAFE_RELEASE(p) do { if ((p)) { (p)->Release(); (p) = NULL; } } \
  while (0)

#define ROUND(x) ((x) >=0 ? static_cast<int>((x) + 0.5) : \
  static_cast<int>((x) - 0.5))

// REFERENCE_TIME time units per millisecond
#define REFTIMES_PER_MILLISEC  10000

#define MAXERRORLENGTH 256

#pragma comment(lib, "Mmdevapi.lib")
#pragma comment(lib, "MFuuid.lib")
#pragma comment(lib, "MFReadWrite.lib")
#pragma comment(lib, "Mfplat.lib")
#pragma comment(lib, "uuid.lib")
#pragma comment(lib, "ole32.lib")




typedef struct tagTHREADNAME_INFO {
    DWORD dwType;        // must be 0x1000
    LPCSTR szName;       // pointer to name (in user addr space)
    DWORD dwThreadID;    // thread ID (-1=caller thread)
    DWORD dwFlags;       // reserved for future use, must be zero
} THREADNAME_INFO;

namespace {

    enum { COM_THREADING_MODEL = COINIT_MULTITHREADED };

    enum {
        kAecCaptureStreamIndex = 0,
        kAecRenderStreamIndex = 1
    };

}  // namespace

namespace webrtc
{

    class DefaultAudioDeviceWatcher {
    public:
        DefaultAudioDeviceWatcher(AudioDeviceWasapi* observer);
        virtual ~DefaultAudioDeviceWatcher();

    private:
        void DefaultAudioCaptureDeviceChanged(
            DefaultAudioCaptureDeviceChangedEventArgs const& args) noexcept;
        void DefaultAudioRenderDeviceChanged(
            DefaultAudioRenderDeviceChangedEventArgs const& args) noexcept;

    private:
        AudioDeviceWasapi* observer_;
        winrt::event_token defaultCaptureChangedToken_;
        winrt::event_token defaultRenderChangedToken_;
    };

    //-----------------------------------------------------------------------------
    DefaultAudioDeviceWatcher::DefaultAudioDeviceWatcher(
        AudioDeviceWasapi* observer) : observer_(observer) {
        defaultCaptureChangedToken_ = MediaDevice::DefaultAudioCaptureDeviceChanged([this](winrt::Windows::Foundation::IInspectable const& /* sender */, winrt::Windows::Media::Devices::DefaultAudioCaptureDeviceChangedEventArgs const& args) {
            this->DefaultAudioCaptureDeviceChanged(args);
        });

        defaultRenderChangedToken_ = MediaDevice::DefaultAudioRenderDeviceChanged([this](winrt::Windows::Foundation::IInspectable const& /* sender */, winrt::Windows::Media::Devices::DefaultAudioRenderDeviceChangedEventArgs const& args) {
            this->DefaultAudioRenderDeviceChanged(args);
        });
    }

    //-----------------------------------------------------------------------------
    DefaultAudioDeviceWatcher::~DefaultAudioDeviceWatcher() {
        MediaDevice::DefaultAudioCaptureDeviceChanged(defaultCaptureChangedToken_);
        MediaDevice::DefaultAudioRenderDeviceChanged(defaultRenderChangedToken_);
    }

    //-----------------------------------------------------------------------------
    void DefaultAudioDeviceWatcher::DefaultAudioCaptureDeviceChanged(
        DefaultAudioCaptureDeviceChangedEventArgs const& args
    ) noexcept
    {
        if (nullptr == observer_)
            return;
        observer_->DefaultAudioCaptureDeviceChanged(args);
    }

    //-----------------------------------------------------------------------------
    void DefaultAudioDeviceWatcher::DefaultAudioRenderDeviceChanged(
        DefaultAudioRenderDeviceChangedEventArgs const& args
    ) noexcept
    {
        if (nullptr == observer_)
            return;
        observer_->DefaultAudioRenderDeviceChanged(args);
    }

    AudioDeviceWasapi* AudioInterfaceActivator::m_AudioDevice = nullptr;
    AudioInterfaceActivator::ActivatorDeviceType
        AudioInterfaceActivator::m_DeviceType = eNone;
    HANDLE AudioInterfaceActivator::m_activateCompletedHandle = NULL;
    HRESULT AudioInterfaceActivator::m_activateResult;

    //-----------------------------------------------------------------------------
    HRESULT AudioInterfaceActivator::ActivateCompleted(
        IActivateAudioInterfaceAsyncOperation* pAsyncOp) {
        HRESULT hr = S_OK;
        HRESULT hrActivateResult = S_OK;
        IUnknown* punkAudioInterface = nullptr;
        IAudioClient2* audioClient = nullptr;
        WAVEFORMATEX* mixFormat = nullptr;

        if (m_DeviceType == eInputDevice) {

            // Check for a successful activation result
            hr = pAsyncOp->GetActivateResult(&hrActivateResult, &punkAudioInterface);
            if (SUCCEEDED(hr) && SUCCEEDED(hrActivateResult)) {
                // Get the pointer for the Audio Client
                hr = punkAudioInterface->QueryInterface(IID_PPV_ARGS(&audioClient));
                if ((E_POINTER == hr) || (E_NOINTERFACE == hr)) {
                    hr = E_FAIL;
                    goto exit;
                }

                AudioClientProperties prop = { 0 };
                prop.cbSize = sizeof(AudioClientProperties);
                prop.bIsOffload = 0;
                prop.eCategory = AudioCategory_Communications;
                prop.Options = AUDCLNT_STREAMOPTIONS_NONE;
                hr = audioClient->SetClientProperties(&prop);

                if (FAILED(hr)) {
                    goto exit;
                }

                hr = audioClient->GetMixFormat(&mixFormat);
                if (FAILED(hr)) {
                    goto exit;
                }

                if (SUCCEEDED(hr)) {
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "Audio Engine's current capturing mix format:");
                    // format type
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "wFormatTag     : 0x%X (%u)", mixFormat->wFormatTag,
                        mixFormat->wFormatTag);
                    // number of channels (i.e. mono, stereo...)
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "nChannels      : %d", mixFormat->nChannels);
                    // sample rate
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "nSamplesPerSec : %d", mixFormat->nSamplesPerSec);
                    // for buffer estimation
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "nAvgBytesPerSec: %d", mixFormat->nAvgBytesPerSec);
                    // block size of data
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "nBlockAlign    : %d", mixFormat->nBlockAlign);
                    // number of bits per sample of mono data
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "wBitsPerSample : %d", mixFormat->wBitsPerSample);
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "cbSize         : %d", mixFormat->cbSize);
                }

                WAVEFORMATEX Wfx = WAVEFORMATEX();
                WAVEFORMATEX* pWfxClosestMatch = NULL;

                // Set wave format
                Wfx.wFormatTag = WAVE_FORMAT_PCM;
                Wfx.wBitsPerSample = 16;
                Wfx.cbSize = 0;

                const int freqs[6] = { 48000, 44100, 16000, 96000, 32000, 8000 };
                hr = S_FALSE;

                // Iterate over frequencies and channels, in order of priority
                for (int freq = 0; freq < sizeof(freqs) / sizeof(freqs[0]); freq++) {
                    for (int chan = 0; chan < sizeof(m_AudioDevice->recChannelsPrioList_) /
                        sizeof(m_AudioDevice->recChannelsPrioList_[0]); chan++) {
                        Wfx.nChannels = m_AudioDevice->recChannelsPrioList_[chan];
                        Wfx.nSamplesPerSec = freqs[freq];
                        Wfx.nBlockAlign = Wfx.nChannels * Wfx.wBitsPerSample / 8;
                        Wfx.nAvgBytesPerSec = Wfx.nSamplesPerSec * Wfx.nBlockAlign;
                        // If the method succeeds and the audio endpoint device supports the
                        // specified stream format, it returns S_OK. If the method succeeds
                        // and provides a closest match to the specified format, it returns
                        // S_FALSE.
                        hr = audioClient->IsFormatSupported(
                            AUDCLNT_SHAREMODE_SHARED,
                            &Wfx,
                            &pWfxClosestMatch);
                        if (hr == S_OK) {
                            break;
                        }
                        else {
                            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                                "nChannels=%d, nSamplesPerSec=%d is not supported",
                                Wfx.nChannels, Wfx.nSamplesPerSec);
                            // if number of channels is more than 2, keep mix format which is
                            // prefered by the engine. Wasapi will handle channel mixing.
                            if (mixFormat->nChannels > 2) {
                                hr = S_OK;
                                break;
                            }
                        }
                    }
                    if (hr == S_OK)
                        break;
                }

                if (hr == S_OK) {
                    m_AudioDevice->recAudioFrameSize_ = Wfx.nBlockAlign;
                    m_AudioDevice->recSampleRate_ = Wfx.nSamplesPerSec;
                    m_AudioDevice->recBlockSize_ = Wfx.nSamplesPerSec / 100;
                    m_AudioDevice->recChannels_ = Wfx.nChannels;

                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "VoE selected this capturing format:");
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "wFormatTag        : 0x%X (%u)", Wfx.wFormatTag, Wfx.wFormatTag);
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "nChannels         : %d", Wfx.nChannels);
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "nSamplesPerSec    : %d", Wfx.nSamplesPerSec);
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "nAvgBytesPerSec   : %d", Wfx.nAvgBytesPerSec);
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "nBlockAlign       : %d", Wfx.nBlockAlign);
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "wBitsPerSample    : %d", Wfx.wBitsPerSample);
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "cbSize            : %d", Wfx.cbSize);
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "Additional settings:");
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "recAudioFrameSize_: %d", m_AudioDevice->recAudioFrameSize_);
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "recBlockSize_     : %d", m_AudioDevice->recBlockSize_);
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "recChannels_      : %d", m_AudioDevice->recChannels_);
                }
                // Create a capturing stream.
                hr = audioClient->Initialize(
                    AUDCLNT_SHAREMODE_SHARED,                 // share Audio Engine with
                                                              // other applications
                    AUDCLNT_STREAMFLAGS_EVENTCALLBACK |       // processing of the audio
                                                              // buffer by the client will
                                                              // be event driven
                    AUDCLNT_STREAMFLAGS_NOPERSIST |           // volume and mute settings
                                                              // for an audio session will
                                                              // not persist across system
                                                              // restarts
                    AUDCLNT_STREAMFLAGS_AUTOCONVERTPCM |      // support for multichannel
                                                              // devices
                    AUDCLNT_STREAMFLAGS_SRC_DEFAULT_QUALITY,  // keep default quality
                    0,                                        // required for event-driven
                                                              // shared mode
                    0,                                        // periodicity
                    &Wfx,                                     // selected wave format
                    NULL);                                    // session GUID

                if (hr != S_OK) {
                    WEBRTC_TRACE(kTraceError, kTraceAudioDevice, m_AudioDevice->id_,
                        "IAudioClient::Initialize() failed:");
                    if (pWfxClosestMatch != NULL) {
                        WEBRTC_TRACE(kTraceError, kTraceAudioDevice, m_AudioDevice->id_,
                            "closest mix format: #channels=%d, samples/sec=%d, bits/sample=%d",
                            pWfxClosestMatch->nChannels, pWfxClosestMatch->nSamplesPerSec,
                            pWfxClosestMatch->wBitsPerSample);
                    }
                    else {
                        WEBRTC_TRACE(kTraceError, kTraceAudioDevice, m_AudioDevice->id_,
                            "no format suggested");
                    }
                }

                if (FAILED(hr)) {
                    goto exit;
                }

                // Get the capture client
                hr = audioClient->GetService(__uuidof(IAudioCaptureClient),
                    reinterpret_cast<void**>(&m_AudioDevice->ptrCaptureClient_));
                if (FAILED(hr)) {
                    goto exit;
                }
                m_AudioDevice->ptrClientIn_ = audioClient;
            }
        }
        else if (m_DeviceType == eOutputDevice) {
            // Check for a successful activation result
            hr = pAsyncOp->GetActivateResult(&hrActivateResult, &punkAudioInterface);
            if (SUCCEEDED(hr) && SUCCEEDED(hrActivateResult)) {
                // Get the pointer for the Audio Client
                hr = punkAudioInterface->QueryInterface(IID_PPV_ARGS(&audioClient));
                if ((E_POINTER == hr) || (E_NOINTERFACE == hr)) {
                    hr = E_FAIL;
                    goto exit;
                }

                AudioClientProperties prop = { 0 };
                prop.cbSize = sizeof(AudioClientProperties);
                prop.bIsOffload = 0;
                prop.eCategory = AudioCategory_Communications;
                prop.Options = AUDCLNT_STREAMOPTIONS_NONE;
                hr = audioClient->SetClientProperties(&prop);

                if (FAILED(hr)) {
                    goto exit;
                }

                hr = audioClient->GetMixFormat(&mixFormat);
                if (FAILED(hr)) {
                    goto exit;
                }

                if (SUCCEEDED(hr)) {
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "Audio Engine's current rendering mix format:");
                    // format type
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "wFormatTag     : 0x%X (%u)",
                        mixFormat->wFormatTag, mixFormat->wFormatTag);
                    // number of channels (i.e. mono, stereo...)
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "nChannels      : %d", mixFormat->nChannels);
                    // sample rate
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "nSamplesPerSec : %d", mixFormat->nSamplesPerSec);
                    // for buffer estimation
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "nAvgBytesPerSec: %d", mixFormat->nAvgBytesPerSec);
                    // block size of data
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "nBlockAlign    : %d", mixFormat->nBlockAlign);
                    // number of bits per sample of mono data
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "wBitsPerSample : %d", mixFormat->wBitsPerSample);
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "cbSize         : %d", mixFormat->cbSize);
                }

                WAVEFORMATEX Wfx = WAVEFORMATEX();
                WAVEFORMATEX* pWfxClosestMatch = NULL;

                // Set wave format
                Wfx.wFormatTag = WAVE_FORMAT_PCM;
                Wfx.wBitsPerSample = 16;
                Wfx.cbSize = 0;

                const int freqs[] = { 48000, 44100, 16000, 96000, 32000, 8000 };
                hr = S_FALSE;

                // Iterate over frequencies and channels, in order of priority
                for (int freq = 0; freq < sizeof(freqs) / sizeof(freqs[0]); freq++) {
                    for (int chan = 0; chan < sizeof(m_AudioDevice->playChannelsPrioList_)
                        / sizeof(m_AudioDevice->playChannelsPrioList_[0]); chan++) {
                        Wfx.nChannels = m_AudioDevice->playChannelsPrioList_[chan];
                        Wfx.nSamplesPerSec = freqs[freq];
                        Wfx.nBlockAlign = Wfx.nChannels * Wfx.wBitsPerSample / 8;
                        Wfx.nAvgBytesPerSec = Wfx.nSamplesPerSec * Wfx.nBlockAlign;
                        // If the method succeeds and the audio endpoint device supports the
                        // specified stream format, it returns S_OK. If the method succeeds
                        // and provides a closest match to the specified format, it returns
                        // S_FALSE.
                        hr = audioClient->IsFormatSupported(
                            AUDCLNT_SHAREMODE_SHARED,
                            &Wfx,
                            &pWfxClosestMatch);
                        if (hr == S_OK) {
                            break;
                        }
                        else {
                            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                                "nChannels=%d, nSamplesPerSec=%d is not supported",
                                Wfx.nChannels, Wfx.nSamplesPerSec);
                        }
                    }
                    if (hr == S_OK)
                        break;
                }


                if (hr == S_OK) {
                    m_AudioDevice->playAudioFrameSize_ = Wfx.nBlockAlign;
                    m_AudioDevice->playBlockSize_ = Wfx.nSamplesPerSec / 100;
                    m_AudioDevice->playSampleRate_ = Wfx.nSamplesPerSec;
                    // The device itself continues to run at 44.1 kHz.
                    m_AudioDevice->devicePlaySampleRate_ = Wfx.nSamplesPerSec;
                    m_AudioDevice->devicePlayBlockSize_ = Wfx.nSamplesPerSec / 100;
                    m_AudioDevice->playChannels_ = Wfx.nChannels;

                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "VoE selected this rendering format:");
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "wFormatTag         : 0x%X (%u)", Wfx.wFormatTag, Wfx.wFormatTag);
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "nChannels          : %d", Wfx.nChannels);
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "nSamplesPerSec     : %d", Wfx.nSamplesPerSec);
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "nAvgBytesPerSec    : %d", Wfx.nAvgBytesPerSec);
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "nBlockAlign        : %d", Wfx.nBlockAlign);
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "wBitsPerSample     : %d", Wfx.wBitsPerSample);
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "cbSize             : %d", Wfx.cbSize);
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "Additional settings:");
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "playAudioFrameSize_: %d", m_AudioDevice->playAudioFrameSize_);
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "playBlockSize_     : %d", m_AudioDevice->playBlockSize_);
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "playChannels_      : %d", m_AudioDevice->playChannels_);
                }
                else {
                    // IsFormatSupported failed, device is probably in surround mode.
                    // Firstly generate mix format to initialize media engine
                    WAVEFORMATEX* WfxMix = m_AudioDevice->GenerateMixFormatForMediaEngine(mixFormat);

                    // Secondly initialize media engine with "expected" values
                    m_AudioDevice->playAudioFrameSize_ = WfxMix->nBlockAlign;
                    m_AudioDevice->playBlockSize_ = WfxMix->nSamplesPerSec / 100;
                    m_AudioDevice->playSampleRate_ = WfxMix->nSamplesPerSec;
                    // The device itself continues to run at 44.1 kHz.
                    m_AudioDevice->devicePlaySampleRate_ = WfxMix->nSamplesPerSec;
                    m_AudioDevice->devicePlayBlockSize_ = WfxMix->nSamplesPerSec / 100;
                    m_AudioDevice->playChannels_ = WfxMix->nChannels;

                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "VoE has been forced to select this rendering format:");
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "wFormatTag         : 0x%X (%u)", WfxMix->wFormatTag, WfxMix->wFormatTag);
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "nChannels          : %d", WfxMix->nChannels);
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "nSamplesPerSec     : %d", WfxMix->nSamplesPerSec);
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "nAvgBytesPerSec    : %d", WfxMix->nAvgBytesPerSec);
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "nBlockAlign        : %d", WfxMix->nBlockAlign);
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "wBitsPerSample     : %d", WfxMix->wBitsPerSample);
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "cbSize             : %d", WfxMix->cbSize);
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "Additional settings:");
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "playAudioFrameSize_: %d", m_AudioDevice->playAudioFrameSize_);
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "playBlockSize_     : %d", m_AudioDevice->playBlockSize_);
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, m_AudioDevice->id_,
                        "playChannels_      : %d", m_AudioDevice->playChannels_);

                    // Now switch to the real supported mix format to initialize device
                    m_AudioDevice->mixFormatSurroundOut_ =
                        m_AudioDevice->GeneratePCMMixFormat(mixFormat);

                    // Set the flag to enable upmix
                    m_AudioDevice->enableUpmix_ = true;
                }

                // ask for minimum buffer size (default)
                REFERENCE_TIME hnsBufferDuration = 0;
                if (mixFormat->nSamplesPerSec == 44100) {
                    // Ask for a larger buffer size (30ms) when using 44.1kHz as render
                    // rate. There seems to be a larger risk of underruns for 44.1 compared
                    // with the default rate (48kHz). When using default, we set the
                    // requested buffer duration to 0, which sets the buffer to the minimum
                    // size required by the engine thread. The actual buffer size can then
                    // be read by GetBufferSize() and it is 20ms on most machines.
                    hnsBufferDuration = 30 * 10000;
                }

                if (m_AudioDevice->ShouldUpmix()) {
                    // Initialize the AudioClient in Shared Mode with the user specified
                    // buffer
                    hr = audioClient->Initialize(AUDCLNT_SHAREMODE_SHARED,
                        AUDCLNT_STREAMFLAGS_EVENTCALLBACK,
                        hnsBufferDuration,
                        0,
                        reinterpret_cast<WAVEFORMATEX*>(m_AudioDevice->mixFormatSurroundOut_),
                        nullptr);
                }
                else {
                    // Initialize the AudioClient in Shared Mode with the user specified
                    // buffer
                    hr = audioClient->Initialize(AUDCLNT_SHAREMODE_SHARED,
                        AUDCLNT_STREAMFLAGS_EVENTCALLBACK,
                        hnsBufferDuration,
                        0,
                        &Wfx,
                        nullptr);
                }

                if (FAILED(hr)) {
                    WEBRTC_TRACE(kTraceError, kTraceAudioDevice, m_AudioDevice->id_,
                        "IAudioClient::Initialize() failed:");
                    if (pWfxClosestMatch != NULL) {
                        WEBRTC_TRACE(kTraceError, kTraceAudioDevice, m_AudioDevice->id_,
                            "closest mix format: #channels=%d, samples/sec=%d, bits/sample=%d",
                            pWfxClosestMatch->nChannels, pWfxClosestMatch->nSamplesPerSec,
                            pWfxClosestMatch->wBitsPerSample);
                    }
                    else {
                        WEBRTC_TRACE(kTraceError, kTraceAudioDevice, m_AudioDevice->id_,
                            "no format suggested, hr = 0x%08X", hr);
                    }
                }

                if (FAILED(hr)) {
                    goto exit;
                }

                // Get the render client
                hr = audioClient->GetService(__uuidof(IAudioRenderClient),
                    reinterpret_cast<void**>(&m_AudioDevice->ptrRenderClient_));
                if (FAILED(hr)) {
                    goto exit;
                }
                m_AudioDevice->ptrClientOut_ = audioClient;
            }
        }

    exit:
        SAFE_RELEASE(punkAudioInterface);
        CoTaskMemFree(mixFormat);

        if (FAILED(hr)) {
            SAFE_RELEASE(audioClient);
            if (m_DeviceType == eInputDevice) {
                SAFE_RELEASE(m_AudioDevice->ptrCaptureClient_);
            }
            else if (m_DeviceType == eOutputDevice) {
                SAFE_RELEASE(m_AudioDevice->ptrRenderClient_);
            }
        }
        m_activateResult = hr;
        ::SetEvent(m_activateCompletedHandle);
        return S_OK;
    }

    void AudioInterfaceActivator::SetAudioDevice(
        AudioDeviceWasapi* device) {
        m_AudioDevice = device;
    }

    //-----------------------------------------------------------------------------
    winrt::Windows::Foundation::IAsyncAction
        AudioInterfaceActivator::ActivateAudioClientAsync(
            LPCWCHAR deviceId, ActivatorDeviceType deviceType, winrt::com_ptr<AudioInterfaceActivator> pActivator,
            winrt::com_ptr<IActivateAudioInterfaceAsyncOperation> pAsyncOp) {

        m_DeviceType = deviceType;

        auto queue = zsLib::IMessageQueueThread::singletonUsingCurrentGUIThreadsMessageQueue();
        ;
        ZS_ASSERT(queue);

        Concurrency::task<void> initialize_async_task;
        queue->postClosure([deviceId, &pAsyncOp, pActivator]() {

            winrt::com_ptr<IActivateAudioInterfaceCompletionHandler> pHandler = pActivator;

            HRESULT hr = ActivateAudioInterfaceAsync(
                deviceId,
                __uuidof(IAudioClient2),
                nullptr,
                pHandler.get(),
                pAsyncOp.put());

            if (FAILED(hr))
                throw winrt::hresult_error(hr);
        });

        m_activateCompletedHandle = ::CreateEventEx(NULL, NULL, 0, EVENT_ALL_ACCESS);

        co_await winrt::resume_on_signal(m_activateCompletedHandle);

        ::CloseHandle(m_activateCompletedHandle);
        m_activateCompletedHandle = NULL;

        HRESULT hr1 = S_OK, hr2 = S_OK;
        winrt::com_ptr<IUnknown> pUnk;
        // Get the audio activation result as IUnknown pointer
        hr2 = pAsyncOp->GetActivateResult(&hr1, pUnk.put());

        // Activation failure
        if (FAILED(hr1)) {
            RTC_LOG(LS_ERROR) << "Failed to activate " <<
                (m_DeviceType == eInputDevice ? "input" :
                    m_DeviceType == eOutputDevice ? "output" : "unknown") <<
                " audio device, hr=" << std::showbase << std::hex << m_activateResult;
            throw winrt::hresult_error(hr1);
        }
        // Failure to get activate result
        if (FAILED(hr2)) {
            RTC_LOG(LS_ERROR) << "Failed to get activation result for " <<
                (m_DeviceType == eInputDevice ? "input" :
                    m_DeviceType == eOutputDevice ? "output" : "unknown") <<
                " audio device, hr=" << std::showbase << std::hex << m_activateResult;
            throw winrt::hresult_error(hr2);
        }

        if (FAILED(m_activateResult)) {
            RTC_LOG(LS_ERROR) << "Failed to configure " <<
                (m_DeviceType == eInputDevice ? "input" :
                    m_DeviceType == eOutputDevice ? "output" : "unknown") <<
                " audio device after activate, hr=" << std::showbase << std::hex << m_activateResult;
            throw winrt::hresult_error(S_OK);
        }
    }

    // ============================================================================
    //                            Construction & Destruction
    // ============================================================================

    // ----------------------------------------------------------------------------
    //  AudioDeviceWasapi() - ctor
    // ----------------------------------------------------------------------------
    AudioDeviceWasapi::AudioDeviceWasapi(const make_private&) :
        comInit_(ScopedCOMInitializer::kMTA),
        captureDevice_(nullptr),
        renderDevice_(nullptr),
        ptrCaptureCollection_(nullptr),
        ptrRenderCollection_(nullptr),
        ptrCollection_(nullptr),
        ptrAudioBuffer_(NULL),
        ptrClientOut_(NULL),
        ptrClientIn_(NULL),
        ptrRenderClient_(NULL),
        ptrCaptureClient_(NULL),
        ptrCaptureVolume_(NULL),
        ptrRenderSimpleVolume_(NULL),
        builtInAecEnabled_(false),
        builtInNSEnabled_(false),
        builtInAGCEnabled_(false),
        playAudioFrameSize_(0),
        playSampleRate_(0),
        playBlockSize_(0),
        playChannels_(2),
        sndCardPlayDelay_(0),
        sndCardRecDelay_(0),
        sampleDriftAt48kHz_(0),
        mixFormatSurroundOut_(NULL),
        enableUpmix_(false),
        driftAccumulator_(0),
        writtenSamples_(0),
        readSamples_(0),
        playAcc_(0),
        recAudioFrameSize_(0),
        recSampleRate_(0),
        recBlockSize_(0),
        recChannels_(2),
        hRenderSamplesReadyEvent_(NULL),
        hPlayThread_(NULL),
        hCaptureSamplesReadyEvent_(NULL),
        hRecThread_(NULL),
        hShutdownRenderEvent_(NULL),
        hShutdownCaptureEvent_(NULL),
        hRestartRenderEvent_(NULL),
        hRestartCaptureEvent_(NULL),
        hRenderStartedEvent_(NULL),
        hCaptureStartedEvent_(NULL),
        hGetCaptureVolumeThread_(NULL),
        hSetCaptureVolumeThread_(NULL),
        hSetCaptureVolumeEvent_(NULL),
        hObserverThread_(NULL),
        hObserverStartedEvent_(NULL),
        hObserverShutdownEvent_(NULL),
        hMmTask_(NULL),
        initialized_(false),
        recording_(false),
        playing_(false),
        recIsInitialized_(false),
        playIsInitialized_(false),
        speakerIsInitialized_(false),
        microphoneIsInitialized_(false),
        playWarning_(0),
        playError_(0),
        playIsRecovering_(false),
        recWarning_(0),
        recError_(0),
        recIsRecovering_(false),
        playBufDelay_(80),
        playBufDelayFixed_(80),
        usingInputDeviceIndex_(false),
        usingOutputDeviceIndex_(false),
        inputDeviceRole_(AudioDeviceRole::Communications),
        outputDeviceRole_(AudioDeviceRole::Communications),
        inputDeviceIndex_(0),
        outputDeviceIndex_(0),
        newMicLevel_(0),
        recordingEnabled_(true),
        playoutEnabled_(true),
        subscriptions_(decltype(subscriptions_)::create()) {
        WEBRTC_TRACE(kTraceMemory, kTraceAudioDevice, id_, "%s created",
            __FUNCTION__);
        // Create our samples ready events - we want auto reset events that start
        // in the not-signaled state. The state of an auto-reset event object
        // remains signaled until a single waiting thread is released, at which
        // time the system automatically sets the state to nonsignaled. If no
        // threads are waiting, the event object's state remains signaled.
        // (Except for hShutdownCaptureEvent_, which is used to shutdown multiple
        // threads).

        hRenderSamplesReadyEvent_ = CreateEvent(NULL, FALSE, FALSE, NULL);
        hCaptureSamplesReadyEvent_ = CreateEvent(NULL, FALSE, FALSE, NULL);
        hShutdownRenderEvent_ = CreateEvent(NULL, FALSE, FALSE, NULL);
        hShutdownCaptureEvent_ = CreateEvent(NULL, TRUE, FALSE, NULL);
        hRestartRenderEvent_ = CreateEvent(NULL, TRUE, FALSE, NULL);
        hRestartCaptureEvent_ = CreateEvent(NULL, TRUE, FALSE, NULL);
        hRenderStartedEvent_ = CreateEvent(NULL, FALSE, FALSE, NULL);
        hCaptureStartedEvent_ = CreateEvent(NULL, FALSE, FALSE, NULL);
        hSetCaptureVolumeEvent_ = CreateEvent(NULL, FALSE, FALSE, NULL);
        hObserverStartedEvent_ = CreateEvent(NULL, FALSE, FALSE, NULL);
        hObserverShutdownEvent_ = CreateEvent(NULL, FALSE, FALSE, NULL);

        perfCounterFreq_.QuadPart = 1;
        perfCounterFactor_ = 0.0;
        avgCPULoad_ = 0.0;

        // list of number of channels to use on recording side
        recChannelsPrioList_[0] = 2;  // stereo is prio 1
        recChannelsPrioList_[1] = 1;  // mono is prio 2

        // list of number of channels to use on playout side
        playChannelsPrioList_[0] = 2;  // stereo is prio 1
        playChannelsPrioList_[1] = 1;  // mono is prio 2

        defaultDeviceWatcher_ = std::make_unique<DefaultAudioDeviceWatcher>(this);
    }

    // ----------------------------------------------------------------------------
    //  AudioDeviceWasapi() - dtor
    // ----------------------------------------------------------------------------
    AudioDeviceWasapi::~AudioDeviceWasapi() {
        WEBRTC_TRACE(kTraceMemory, kTraceAudioDevice, id_, "%s destroyed",
            __FUNCTION__);

        thisWeak_.reset();

        Terminate();

        ptrAudioBuffer_ = NULL;

        if (NULL != hRenderSamplesReadyEvent_) {
            CloseHandle(hRenderSamplesReadyEvent_);
            hRenderSamplesReadyEvent_ = NULL;
        }

        if (NULL != hCaptureSamplesReadyEvent_) {
            CloseHandle(hCaptureSamplesReadyEvent_);
            hCaptureSamplesReadyEvent_ = NULL;
        }

        if (NULL != hRenderStartedEvent_) {
            CloseHandle(hRenderStartedEvent_);
            hRenderStartedEvent_ = NULL;
        }

        if (NULL != hCaptureStartedEvent_) {
            CloseHandle(hCaptureStartedEvent_);
            hCaptureStartedEvent_ = NULL;
        }

        if (NULL != hShutdownRenderEvent_) {
            CloseHandle(hShutdownRenderEvent_);
            hShutdownRenderEvent_ = NULL;
        }

        if (NULL != hShutdownCaptureEvent_) {
            CloseHandle(hShutdownCaptureEvent_);
            hShutdownCaptureEvent_ = NULL;
        }

        if (NULL != hRestartRenderEvent_) {
            CloseHandle(hRestartRenderEvent_);
            hRestartRenderEvent_ = NULL;
        }

        if (NULL != hRestartCaptureEvent_) {
            CloseHandle(hRestartCaptureEvent_);
            hRestartCaptureEvent_ = NULL;
        }

        if (NULL != hObserverStartedEvent_) {
            CloseHandle(hObserverStartedEvent_);
            hObserverStartedEvent_ = NULL;
        }

        if (NULL != hObserverShutdownEvent_) {
            CloseHandle(hObserverShutdownEvent_);
            hObserverShutdownEvent_ = NULL;
        }

        if (NULL != hSetCaptureVolumeEvent_) {
            CloseHandle(hSetCaptureVolumeEvent_);
            hSetCaptureVolumeEvent_ = NULL;
        }

        if (NULL != mixFormatSurroundOut_)
            delete mixFormatSurroundOut_;
    }

    //-----------------------------------------------------------------------------
    rtc::scoped_refptr<webrtc::AudioDeviceModule> AudioDeviceWasapi::create(const CreationProperties& info) noexcept {
        auto result = new ::rtc::RefCountedObject<AudioDeviceWasapi>(make_private{});
        result->init(info);
        return result;
    }

    //-----------------------------------------------------------------------------
    void AudioDeviceWasapi::init(const CreationProperties& props) noexcept {
        id_ = String(props.id_);

        recordingEnabled_ = props.recordingEnabled_;
        playoutEnabled_ = props.playoutEnabled_;

        if (props.delegate_) {
            defaultSubscription_ = subscriptions_.subscribe(props.delegate_, zsLib::IMessageQueueThread::singletonUsingCurrentGUIThreadsMessageQueue());
        }

        auto thisWeak = thisWeak_;
    }

    //-----------------------------------------------------------------------------
    IAudioDeviceWasapiSubscriptionPtr AudioDeviceWasapi::subscribe(IAudioDeviceWasapiDelegatePtr originalDelegate) {
        AutoRecursiveLock lock(subscriptionLock_);
        if (!originalDelegate) return defaultSubscription_;

        auto subscription = subscriptions_.subscribe(originalDelegate, zsLib::IMessageQueueThread::singletonUsingCurrentGUIThreadsMessageQueue());

        auto delegate = subscriptions_.delegate(subscription, true);

        if (delegate) {
            auto pThis = thisWeak_.lock();
        }

        return subscription;
    }

    // ============================================================================
    //                                     API
    // ============================================================================

    // ----------------------------------------------------------------------------
    //  ActiveAudioLayer
    // ----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::ActiveAudioLayer(AudioLayer* audioLayer) const {
        *audioLayer = AudioDeviceModule::kWindowsCoreAudio;
        return 0;
    }

    //-----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::RegisterAudioCallback(webrtc::AudioTransport* audioCallback) {
        rtc::CritScope lock(&critSect_);
        if (ptrAudioBuffer_)
            return ptrAudioBuffer_->RegisterAudioCallback(audioCallback);
        else
            return -1;
    }

    // ----------------------------------------------------------------------------
    //  Init
    // ----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::Init() {
        rtc::CritScope lock(&critSect_);

        if (initialized_) {
            return 0;
        }

        playWarning_ = 0;
        playError_ = 0;
        recWarning_ = 0;
        recError_ = 0;

        ptrAudioBuffer_ = new webrtc::AudioDeviceBuffer();

        // Inform the AudioBuffer about default settings for this implementation.
        // Set all values to zero here since the actual settings will be done by
        // InitPlayout and InitRecording later.
        ptrAudioBuffer_->SetRecordingSampleRate(0);
        ptrAudioBuffer_->SetPlayoutSampleRate(0);
        ptrAudioBuffer_->SetRecordingChannels(0);
        ptrAudioBuffer_->SetPlayoutChannels(0);

        EnumerateEndpointDevicesAll();

        initialized_ = true;

        StartObserverThread();
        return 0;
    }

    // ----------------------------------------------------------------------------
    //  Terminate
    // ----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::Terminate() {
        rtc::CritScope lock(&critSect_);

        if (!initialized_) {
            return 0;
        }

        initialized_ = false;
        speakerIsInitialized_ = false;
        microphoneIsInitialized_ = false;
        playing_ = false;
        recording_ = false;

        SAFE_RELEASE(ptrClientOut_);
        SAFE_RELEASE(ptrClientIn_);
        SAFE_RELEASE(ptrRenderClient_);
        SAFE_RELEASE(ptrCaptureClient_);
        SAFE_RELEASE(ptrCaptureVolume_);
        SAFE_RELEASE(ptrRenderSimpleVolume_);
        if (ptrAudioBuffer_)
            delete ptrAudioBuffer_;

        StopObserverThread();

        return 0;
    }

    // ----------------------------------------------------------------------------
    //  Initialized
    // ----------------------------------------------------------------------------
    bool AudioDeviceWasapi::Initialized() const {
        return (initialized_);
    }

    // ----------------------------------------------------------------------------
    //  InitSpeaker
    // ----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::InitSpeaker() {
        rtc::CritScope lock(&critSect_);

        if (playing_) {
            return -1;
        }

        if (!playoutEnabled_) {
            return 0;
        }

        if (usingOutputDeviceIndex_) {
            int16_t nDevices = PlayoutDevices();
            if (outputDeviceIndex_ > (nDevices - 1)) {
                WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                    "current device selection is invalid => unable to initialize");
                return -1;
            }
        }

        int32_t ret(0);
        renderDevice_ = nullptr;

        if (usingOutputDeviceIndex_) {
            // Refresh the selected rendering endpoint device using selected device id
            renderDevice_ = GetListDevice(DeviceClass::AudioRender,
                deviceIdStringOut_);
            if (renderDevice_ == nullptr) {
                RTC_LOG(LS_WARNING) << "Selected audio playout device not found "
                    << rtc::ToUtf8(deviceIdStringOut_.c_str()).c_str()
                    << ", using default!";
                WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                    "selected audio playout device not found %s, using default.",
                    rtc::ToUtf8(deviceIdStringOut_.c_str()).c_str());
            }
            else {
                RTC_LOG(LS_INFO) << "Using selected audio playout device:"
                    << rtc::ToUtf8(renderDevice_.Name().c_str()).c_str();
                WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                    "using selected audio playout device %s.",
                    rtc::ToUtf8(renderDevice_.Name().c_str()).c_str());
            }
        }
        else if (!preferredOutputDeviceIdString_.empty()) {
            renderDevice_ = DeviceInformation::CreateFromIdAsync(preferredOutputDeviceIdString_).get();
        }
        else {
            renderDevice_ = GetDefaultDevice(DeviceClass::AudioRender,
                outputDeviceRole_);
            if (renderDevice_ == nullptr) {
                RTC_LOG(LS_ERROR) << "Failed to get:"
                    << (outputDeviceRole_ == AudioDeviceRole::Communications ? "communications" :
                        outputDeviceRole_ == AudioDeviceRole::Default ? "default" : "unknown")
                    << " audio playout device, using default.";
                WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                    "failed to get %s audio playout device, using default.",
                    outputDeviceRole_ == AudioDeviceRole::Communications ? "communications" :
                    outputDeviceRole_ == AudioDeviceRole::Default ? "default" : "unknown");
            }
            else {
                RTC_LOG(LS_INFO) << "Using "
                    << (outputDeviceRole_ == AudioDeviceRole::Communications ? "communications" :
                        outputDeviceRole_ == AudioDeviceRole::Default ? "default" : "unknown")
                    << " audio playout device: "
                    << rtc::ToUtf8(renderDevice_.Name().c_str()).c_str();
            }
        }

        if (renderDevice_ == nullptr) {
            renderDevice_ = GetDefaultDevice(DeviceClass::AudioRender,
                AudioDeviceRole::Communications);
            if (renderDevice_ != nullptr) {
                RTC_LOG(LS_ERROR) << "Using default audio playout device:"
                    << rtc::ToUtf8(renderDevice_.Name().c_str()).c_str();
                WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                    "using default audio playout device: %s",
                    rtc::ToUtf8(renderDevice_.Name().c_str()).c_str());
            }
        }

        if (ret != 0 || (renderDevice_ == nullptr)) {
            WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                "failed to initialize the audio playout device, no device found");
            return -1;
        }

        try {
            InitializeAudioDeviceOut(renderDevice_.Id());
            RTC_LOG(LS_INFO) << "Output audio device activated "
                << rtc::ToUtf8(renderDevice_.Name().c_str()).c_str();
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "output audio device activated: %s",
                rtc::ToUtf8(renderDevice_.Name().c_str()).c_str());
        }
        catch (winrt::hresult_error const& ex) {
            RTC_LOG(LS_ERROR) << "Failed to activate output audio device "
                << rtc::ToUtf8(renderDevice_.Name().c_str()).c_str()
                << ", ex="
                << rtc::ToUtf8(ex.message().c_str()).c_str();
            WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                "failed to activate output audio device %s, ex=%s",
                rtc::ToUtf8(renderDevice_.Name().c_str()).c_str(),
                rtc::ToUtf8(ex.message().c_str()).c_str());
        }

        if (ptrAudioBuffer_) {
            // Update the audio buffer with the selected parameters
            ptrAudioBuffer_->SetPlayoutSampleRate(playSampleRate_);
            ptrAudioBuffer_->SetPlayoutChannels((uint8_t)playChannels_);
        }

        if (ptrClientOut_ == nullptr) {  // Initialize audio output device failed
            RTC_LOG(LS_ERROR) << "Failed to initialize the audio playout enpoint device";
            WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                "failed to initialize audio playout device");
            return -1;
        }

        SAFE_RELEASE(ptrRenderSimpleVolume_);
        ret = ptrClientOut_->GetService(__uuidof(ISimpleAudioVolume),
            reinterpret_cast<void**>(&ptrRenderSimpleVolume_));
        if (ret != 0 || ptrRenderSimpleVolume_ == NULL) {
            WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                "  failed to initialize the render simple volume");
            SAFE_RELEASE(ptrRenderSimpleVolume_);
            return -1;
        }

        speakerIsInitialized_ = true;

        return 0;
    }

    // ----------------------------------------------------------------------------
    //  InitMicrophone
    // ----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::InitMicrophone() {
        rtc::CritScope lock(&critSect_);

        if (recording_) {
            return -1;
        }

        if (!recordingEnabled_) {
            return 0;
        }

        if (usingInputDeviceIndex_) {
            int16_t nDevices = RecordingDevices();
            if (inputDeviceIndex_ > (nDevices - 1)) {
                WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                    "current device selection is invalid => unable to initialize");
                return -1;
            }
        }

        int32_t ret(0);

        captureDevice_ = nullptr;
        if (usingInputDeviceIndex_) {
            // Refresh the selected capture endpoint device using selected device Id
            captureDevice_ = GetListDevice(DeviceClass::AudioCapture,
                deviceIdStringIn_);
            if (captureDevice_ == nullptr) {
                RTC_LOG(LS_WARNING) << "Selected audio capture device not found "
                    << rtc::ToUtf8(deviceIdStringIn_.c_str()).c_str()
                    << ", using default";
                WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                    "selected audio capture device not found %s, using default.",
                    rtc::ToUtf8(deviceIdStringIn_.c_str()).c_str());
            }
            else {
                RTC_LOG(LS_INFO) << "Using selected audio capture device:"
                    << rtc::ToUtf8(captureDevice_.Name().c_str()).c_str();
                WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                    "using selected audio capture device %s",
                    rtc::ToUtf8(deviceIdStringIn_.c_str()).c_str());
            }
        }
        else if (!preferredInputDeviceIdString_.empty()) {
            captureDevice_ = DeviceInformation::CreateFromIdAsync(preferredInputDeviceIdString_).get();
        }
        else {
            captureDevice_ = GetDefaultDevice(DeviceClass::AudioCapture,
                inputDeviceRole_);
            if (captureDevice_ == nullptr) {
                RTC_LOG(LS_ERROR) << "Failed to get:"
                    << (inputDeviceRole_ == AudioDeviceRole::Communications ? "communications" :
                        inputDeviceRole_ == AudioDeviceRole::Default ? "default" : "unknown")
                    << " audio capture device, using default.";
                WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                    "failed to get %s audio capture device, using default.",
                    inputDeviceRole_ == AudioDeviceRole::Communications ? "communications" :
                    inputDeviceRole_ == AudioDeviceRole::Default ? "default" : "unknown");
            }
            else {
                RTC_LOG(LS_INFO) << "Using "
                    << (inputDeviceRole_ == AudioDeviceRole::Communications ? "communications" :
                        inputDeviceRole_ == AudioDeviceRole::Default ? "default" : "unknown")
                    << " audio capture device: "
                    << rtc::ToUtf8(captureDevice_.Name().c_str()).c_str();
            }

        }
        if (captureDevice_ == nullptr) {
            captureDevice_ = GetDefaultDevice(DeviceClass::AudioCapture,
                AudioDeviceRole::Communications);
            if (captureDevice_ != nullptr) {
                RTC_LOG(LS_ERROR) << "Using default audio capture device:"
                    << rtc::ToUtf8(captureDevice_.Name().c_str()).c_str();
                WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                    "using default audio recording device %s",
                    rtc::ToUtf8(captureDevice_.Name().c_str()).c_str());
            }
        }

        if (ret != 0 || (captureDevice_ == nullptr)) {
            WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                "failed to initialize the capturing enpoint device, device not found");
            return -1;
        }

        try {
            InitializeAudioDeviceIn(captureDevice_.Id());
            RTC_LOG(LS_INFO) << "Input audio device activated "
                << rtc::ToUtf8(captureDevice_.Name().c_str()).c_str();
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "input audio device activated: %s",
                rtc::ToUtf8(captureDevice_.Name().c_str()).c_str());
        }
        catch (winrt::hresult_error const& ex) {
            RTC_LOG(LS_ERROR) << "Failed to activate input audio device "
                << rtc::ToUtf8(captureDevice_.Name().c_str()).c_str()
                << ", ex="
                << rtc::ToUtf8(ex.message().c_str()).c_str();
            WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                "failed to activate input audio device %s, ex=%s",
                rtc::ToUtf8(captureDevice_.Name().c_str()).c_str(),
                rtc::ToUtf8(ex.message().c_str()).c_str());
        }

        if (ptrAudioBuffer_) {
            // Update the audio buffer with the selected parameters
            ptrAudioBuffer_->SetRecordingSampleRate(recSampleRate_);
            ptrAudioBuffer_->SetRecordingChannels((uint8_t)recChannels_);
        }

        if (ptrClientIn_ == nullptr) {
            RTC_LOG(LS_ERROR) << "Failed to initialize the capturing enpoint device";
            WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                "failed to initialize the capturing enpoint device");
            return -1;
        }

        ret = ptrClientIn_->GetService(__uuidof(ISimpleAudioVolume),
            reinterpret_cast<void**>(&ptrCaptureVolume_));
        if (ret != 0 || ptrCaptureVolume_ == NULL) {
            WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                "failed to initialize the capture volume");
            SAFE_RELEASE(ptrCaptureVolume_);
            return -1;
        }

        microphoneIsInitialized_ = true;

        return 0;
    }

    // ----------------------------------------------------------------------------
    //  SpeakerIsInitialized
    // ----------------------------------------------------------------------------
    bool AudioDeviceWasapi::SpeakerIsInitialized() const {
        return (speakerIsInitialized_);
    }

    // ----------------------------------------------------------------------------
    //  MicrophoneIsInitialized
    // ----------------------------------------------------------------------------
    bool AudioDeviceWasapi::MicrophoneIsInitialized() const {
        return (microphoneIsInitialized_);
    }

    // ----------------------------------------------------------------------------
    //  SpeakerVolumeIsAvailable
    // ----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::SpeakerVolumeIsAvailable(bool* available) {
        HRESULT hr = S_OK;
        ISimpleAudioVolume* pVolume = NULL;
        float volume(0.0f);

        rtc::CritScope lock(&critSect_);

        if (ptrClientOut_ == nullptr) {
            return -1;
        }

        hr = ptrClientOut_->GetService(__uuidof(ISimpleAudioVolume),
            reinterpret_cast<void**>(&pVolume));
        EXIT_ON_ERROR(hr);

        hr = pVolume->GetMasterVolume(&volume);
        if (FAILED(hr)) {
            *available = false;
        }
        *available = true;

        SAFE_RELEASE(pVolume);

        return 0;

    Exit:
        TraceCOMError(hr);
        SAFE_RELEASE(pVolume);
        return -1;
    }

    // ----------------------------------------------------------------------------
    //  SetSpeakerVolume
    // ----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::SetSpeakerVolume(uint32_t volume) {
        {
            rtc::CritScope lock(&critSect_);

            if (!speakerIsInitialized_) {
                return -1;
            }

            if (ptrRenderSimpleVolume_ == NULL) {
                return -1;
            }
        }

        if (volume < (uint32_t)MIN_CORE_SPEAKER_VOLUME ||
            volume >(uint32_t)MAX_CORE_SPEAKER_VOLUME) {
            return -1;
        }

        HRESULT hr = S_OK;

        // scale input volume to valid range (0.0 to 1.0)
        const float fLevel = static_cast<float>(volume) / MAX_CORE_SPEAKER_VOLUME;
        volumeMutex_.Enter();
        hr = ptrRenderSimpleVolume_->SetMasterVolume(fLevel, NULL);
        volumeMutex_.Leave();
        EXIT_ON_ERROR(hr);

        return 0;

    Exit:
        TraceCOMError(hr);
        return -1;
    }

    // ----------------------------------------------------------------------------
    //  SpeakerVolume
    // ----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::SpeakerVolume(uint32_t* volume) const {
        {
            rtc::CritScope lock(&critSect_);

            if (!speakerIsInitialized_) {
                return -1;
            }

            if (ptrRenderSimpleVolume_ == NULL) {
                return -1;
            }
        }

        HRESULT hr = S_OK;
        float fLevel(0.0f);

        volumeMutex_.Enter();
        hr = ptrRenderSimpleVolume_->GetMasterVolume(&fLevel);
        volumeMutex_.Leave();
        EXIT_ON_ERROR(hr);

        // scale input volume range [0.0,1.0] to valid output range
        *volume = static_cast<uint32_t> (fLevel * MAX_CORE_SPEAKER_VOLUME);

        return 0;

    Exit:
        TraceCOMError(hr);
        return -1;
    }

    // ----------------------------------------------------------------------------
    //  MaxSpeakerVolume
    //
    //  The internal range for Core Audio is 0.0 to 1.0, where 0.0 indicates
    //  silence and 1.0 indicates full volume (no attenuation).
    //  We add our (webrtc-internal) own max level to match the Wave API and
    //  how it is used today in VoE.
    // ----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::MaxSpeakerVolume(uint32_t* maxVolume) const {
        if (!speakerIsInitialized_) {
            return -1;
        }

        *maxVolume = static_cast<uint32_t> (MAX_CORE_SPEAKER_VOLUME);

        return 0;
    }

    // ----------------------------------------------------------------------------
    //  MinSpeakerVolume
    // ----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::MinSpeakerVolume(uint32_t* minVolume) const {
        if (!speakerIsInitialized_) {
            return -1;
        }

        *minVolume = static_cast<uint32_t> (MIN_CORE_SPEAKER_VOLUME);

        return 0;
    }

    // ----------------------------------------------------------------------------
    //  SpeakerMuteIsAvailable
    // ----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::SpeakerMuteIsAvailable(bool* available) {
        rtc::CritScope lock(&critSect_);

        if (ptrClientOut_ == NULL) {
            return -1;
        }

        HRESULT hr = S_OK;
        ISimpleAudioVolume* pVolume = NULL;

        // Query the speaker system mute state.
        hr = ptrClientOut_->GetService(__uuidof(ISimpleAudioVolume),
            reinterpret_cast<void**>(&pVolume));
        EXIT_ON_ERROR(hr);

        BOOL mute;
        hr = pVolume->GetMute(&mute);
        if (FAILED(hr))
            *available = false;
        else
            *available = true;

        SAFE_RELEASE(pVolume);

        return 0;

    Exit:
        TraceCOMError(hr);
        SAFE_RELEASE(pVolume);
        return -1;
    }

    // ----------------------------------------------------------------------------
    //  SetSpeakerMute
    // ----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::SetSpeakerMute(bool enable) {

        HRESULT hr = S_OK;
        ISimpleAudioVolume* pVolume = NULL;
        const BOOL mute{ enable };

        rtc::CritScope lock(&critSect_);

        if (!speakerIsInitialized_) {
            return -1;
        }

        if (ptrClientOut_ == NULL) {
            return -1;
        }

        // Set the speaker system mute state.
        hr = ptrClientOut_->GetService(__uuidof(ISimpleAudioVolume),
            reinterpret_cast<void**>(&pVolume));
        EXIT_ON_ERROR(hr);

        hr = pVolume->SetMute(mute, NULL);
        EXIT_ON_ERROR(hr);

        SAFE_RELEASE(pVolume);

        return 0;

    Exit:
        TraceCOMError(hr);
        SAFE_RELEASE(pVolume);
        return -1;
    }

    // ----------------------------------------------------------------------------
    //  SpeakerMute
    // ----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::SpeakerMute(bool* enabled) const {
        if (!speakerIsInitialized_) {
            return -1;
        }

        if (ptrClientOut_ == NULL) {
            return -1;
        }

        HRESULT hr = S_OK;
        ISimpleAudioVolume* pVolume = NULL;

        // Query the speaker system mute state.
        hr = ptrClientOut_->GetService(__uuidof(ISimpleAudioVolume),
            reinterpret_cast<void**>(&pVolume));
        EXIT_ON_ERROR(hr);

        BOOL mute;
        hr = pVolume->GetMute(&mute);
        EXIT_ON_ERROR(hr);

        *enabled = (mute == TRUE) ? true : false;

        SAFE_RELEASE(pVolume);

        return 0;

    Exit:
        TraceCOMError(hr);
        return -1;
    }

    // ----------------------------------------------------------------------------
    //  MicrophoneMuteIsAvailable
    // ---------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::MicrophoneMuteIsAvailable(bool* available) {
        rtc::CritScope lock(&critSect_);

        if (ptrClientIn_ == NULL) {
            return -1;
        }

        HRESULT hr = S_OK;
        ISimpleAudioVolume* pVolume = NULL;

        // Query the microphone system mute state.
        hr = ptrClientIn_->GetService(__uuidof(ISimpleAudioVolume),
            reinterpret_cast<void**>(&pVolume));
        EXIT_ON_ERROR(hr);

        BOOL mute;
        hr = pVolume->GetMute(&mute);
        if (FAILED(hr))
            *available = false;
        else
            *available = true;

        SAFE_RELEASE(pVolume);
        return 0;

    Exit:
        TraceCOMError(hr);
        SAFE_RELEASE(pVolume);
        return -1;
    }

    // ----------------------------------------------------------------------------
    //  SetMicrophoneMute
    // ----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::SetMicrophoneMute(bool enable) {
        HRESULT hr = S_OK;
        ISimpleAudioVolume* pVolume = NULL;
        const BOOL mute{ enable };

        if (!microphoneIsInitialized_) {
            return -1;
        }

        if (ptrClientIn_ == NULL) {
            return -1;
        }

        // Set the microphone system mute state.
        hr = ptrClientIn_->GetService(__uuidof(ISimpleAudioVolume),
            reinterpret_cast<void**>(&pVolume));
        EXIT_ON_ERROR(hr);

        hr = pVolume->SetMute(mute, NULL);
        EXIT_ON_ERROR(hr);

        SAFE_RELEASE(pVolume);
        return 0;

    Exit:
        TraceCOMError(hr);
        return -1;
    }

    // ----------------------------------------------------------------------------
    //  MicrophoneMute
    // ----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::MicrophoneMute(bool* enabled) const {
        if (!microphoneIsInitialized_) {
            return -1;
        }

        HRESULT hr = S_OK;
        ISimpleAudioVolume* pVolume = NULL;

        // Query the microphone system mute state.
        hr = ptrClientIn_->GetService(__uuidof(ISimpleAudioVolume),
            reinterpret_cast<void**>(&pVolume));
        EXIT_ON_ERROR(hr);

        BOOL mute;
        hr = pVolume->GetMute(&mute);
        EXIT_ON_ERROR(hr);

        *enabled = (mute == TRUE) ? true : false;

        SAFE_RELEASE(pVolume);
        return 0;

    Exit:
        SAFE_RELEASE(pVolume);
        TraceCOMError(hr);
        return -1;
    }

    // ----------------------------------------------------------------------------
    //  StereoRecordingIsAvailable
    // ----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::StereoRecordingIsAvailable(bool* available) const {
        *available = true;
        return 0;
    }

    // ----------------------------------------------------------------------------
    //  SetStereoRecording
    // ----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::SetStereoRecording(bool enable) {
        rtc::CritScope lock(&critSect_);

        if (enable) {
            recChannelsPrioList_[0] = 2;    // try stereo first
            recChannelsPrioList_[1] = 1;
            recChannels_ = 2;
        }
        else {
            recChannelsPrioList_[0] = 1;    // try mono first
            recChannelsPrioList_[1] = 2;
            recChannels_ = 1;
        }
        if (ptrAudioBuffer_)
            ptrAudioBuffer_->SetRecordingChannels(recChannels_);

        return 0;
    }

    // ----------------------------------------------------------------------------
    //  StereoRecording
    // ----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::StereoRecording(bool* enabled) const {
        if (recChannels_ == 2)
            *enabled = true;
        else
            *enabled = false;

        return 0;
    }

    // ----------------------------------------------------------------------------
    //  StereoPlayoutIsAvailable
    // ----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::StereoPlayoutIsAvailable(bool* available) const {
        *available = true;
        return 0;
    }

    // ----------------------------------------------------------------------------
    //  SetStereoPlayout
    // ----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::SetStereoPlayout(bool enable) {
        rtc::CritScope lock(&critSect_);

        if (enable) {
            playChannelsPrioList_[0] = 2;    // try stereo first
            playChannelsPrioList_[1] = 1;
            playChannels_ = 2;
        }
        else {
            playChannelsPrioList_[0] = 1;    // try mono first
            playChannelsPrioList_[1] = 2;
            playChannels_ = 1;
        }
        if (ptrAudioBuffer_)
            ptrAudioBuffer_->SetPlayoutChannels(playChannels_);

        return 0;
    }

    // ----------------------------------------------------------------------------
    //  StereoPlayout
    // ----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::StereoPlayout(bool* enabled) const {
        if (playChannels_ == 2)
            *enabled = true;
        else
            *enabled = false;

        return 0;
    }

    // ----------------------------------------------------------------------------
    //  MicrophoneVolumeIsAvailable
    // ----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::MicrophoneVolumeIsAvailable(
        bool* available) {

        HRESULT hr = S_OK;
        ISimpleAudioVolume* pVolume = NULL;
        float volume{ 0.0f };

        rtc::CritScope lock(&critSect_);

        if (ptrClientIn_ == NULL) {
            return -1;
        }

        hr = ptrClientIn_->GetService(__uuidof(ISimpleAudioVolume),
            reinterpret_cast<void**>(&pVolume));
        EXIT_ON_ERROR(hr);

        hr = pVolume->GetMasterVolume(&volume);
        if (FAILED(hr)) {
            *available = false;
        }
        *available = true;

        SAFE_RELEASE(pVolume);
        return 0;

    Exit:
        SAFE_RELEASE(pVolume);
        TraceCOMError(hr);
        return -1;
    }

    // ----------------------------------------------------------------------------
    //  SetMicrophoneVolume
    // ----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::SetMicrophoneVolume(uint32_t volume) {
        WEBRTC_TRACE(kTraceStream, kTraceAudioDevice, id_,
            "AudioDeviceWasapi::SetMicrophoneVolume(volume=%u)", volume); {
            rtc::CritScope lock(&critSect_);

            if (!microphoneIsInitialized_) {
                return -1;
            }

            if (ptrCaptureVolume_ == NULL) {
                return -1;
            }
        }

        if (volume < static_cast<uint32_t>(MIN_CORE_MICROPHONE_VOLUME) ||
            volume > static_cast<uint32_t>(MAX_CORE_MICROPHONE_VOLUME)) {
            return -1;
        }

        HRESULT hr = S_OK;
        // scale input volume to valid range (0.0 to 1.0)
        const float fLevel = static_cast<float>(volume) / MAX_CORE_MICROPHONE_VOLUME;
        volumeMutex_.Enter();
        ptrCaptureVolume_->SetMasterVolume(fLevel, NULL);
        volumeMutex_.Leave();
        EXIT_ON_ERROR(hr);

        return 0;

    Exit:
        TraceCOMError(hr);
        return -1;
    }

    // ----------------------------------------------------------------------------
    //  MicrophoneVolume
    // ----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::MicrophoneVolume(uint32_t* volume) const {
        {
            rtc::CritScope lock(&critSect_);

            if (!microphoneIsInitialized_) {
                return -1;
            }

            if (ptrCaptureVolume_ == NULL) {
                return -1;
            }
        }

        HRESULT hr = S_OK;
        float fLevel(0.0f);
        *volume = 0;
        volumeMutex_.Enter();
        hr = ptrCaptureVolume_->GetMasterVolume(&fLevel);
        volumeMutex_.Leave();
        EXIT_ON_ERROR(hr);

        // scale input volume range [0.0,1.0] to valid output range
        *volume = static_cast<uint32_t> (fLevel * MAX_CORE_MICROPHONE_VOLUME);

        return 0;

    Exit:
        TraceCOMError(hr);
        return -1;
    }

    // ----------------------------------------------------------------------------
    //  MaxMicrophoneVolume
    //
    //  The internal range for Core Audio is 0.0 to 1.0, where 0.0 indicates
    //  silence and 1.0 indicates full volume (no attenuation).
    //  We add our (webrtc-internal) own max level to match the Wave API and
    //  how it is used today in VoE.
    // ----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::MaxMicrophoneVolume(
        uint32_t* maxVolume) const {
        WEBRTC_TRACE(kTraceStream, kTraceAudioDevice, id_, "%s", __FUNCTION__);

        if (!microphoneIsInitialized_) {
            return -1;
        }

        *maxVolume = static_cast<uint32_t> (MAX_CORE_MICROPHONE_VOLUME);

        return 0;
    }

    // ----------------------------------------------------------------------------
    //  MinMicrophoneVolume
    // ----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::MinMicrophoneVolume(
        uint32_t* minVolume) const {
        if (!microphoneIsInitialized_) {
            return -1;
        }

        *minVolume = static_cast<uint32_t> (MIN_CORE_MICROPHONE_VOLUME);

        return 0;
    }

    // ----------------------------------------------------------------------------
    //  PlayoutDevices
    // ----------------------------------------------------------------------------
    int16_t AudioDeviceWasapi::PlayoutDevices() {
        rtc::CritScope lock(&critSect_);

        // Refresh the list of rendering endpoint devices
        RefreshDeviceList(DeviceClass::AudioRender);

        return (DeviceListCount(DeviceClass::AudioRender));
    }

    // ----------------------------------------------------------------------------
    //  SetPlayoutDevice I (II)
    // ----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::SetPlayoutDevice(uint16_t index) {
        if (!playoutEnabled_) {
            return 0;
        }

        // Get current number of available rendering endpoint devices and refresh the
        // rendering collection.
        UINT nDevices = PlayoutDevices();

        if (index < 0 || index >(nDevices - 1)) {
            WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                "device index is out of range [0,%u]", (nDevices - 1));
            return -1;
        }

        rtc::CritScope lock(&critSect_);

        assert(ptrRenderCollection_ != nullptr);

        // Select an endpoint rendering device given the specified index
        renderDevice_ = nullptr;
        deviceIdStringOut_.clear();

        renderDevice_ = ptrRenderCollection_.GetAt(index);
        deviceIdStringOut_ = renderDevice_.Id();

        // Get the endpoint device's friendly-name
        if (!GetDeviceName(renderDevice_).empty()) {
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_, "friendly name: \"%S\"",
                GetDeviceName(renderDevice_));
        }

        usingOutputDeviceIndex_ = true;
        outputDeviceIndex_ = index;

        return 0;
    }

    // ----------------------------------------------------------------------------
    //  SetPlayoutDevice II (II)
    // ----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::SetPlayoutDevice(
        AudioDeviceModule::WindowsDeviceType device) {
        if (!playoutEnabled_) {
            return 0;
        }

        // If device has been selected once by index value it couldn't be changed afterwards
        // by setting the device type
        if (usingOutputDeviceIndex_) {
            return 0;
        }

        rtc::CritScope lock(&critSect_);

        // Refresh the list of rendering endpoint devices
        RefreshDeviceList(DeviceClass::AudioRender);

        //  Select an endpoint rendering device given the specified role
        renderDevice_ = nullptr;
        deviceIdStringOut_.clear();

        if (device == AudioDeviceModule::kDefaultDevice) {
            outputDeviceRole_ = AudioDeviceRole::Default;
        }
        else if (device == AudioDeviceModule::kDefaultCommunicationDevice) {
            outputDeviceRole_ = AudioDeviceRole::Communications;
        }

        renderDevice_ = GetDefaultDevice(DeviceClass::AudioRender, outputDeviceRole_);
        deviceIdStringOut_ = renderDevice_.Id();

        // Get the endpoint device's friendly-name
        std::wstring str = GetDeviceName(renderDevice_).c_str();
        if (str.length() > 0) {
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_, "friendly name: \"%S\"",
                str.c_str());
        }

        return 0;
    }

    // ----------------------------------------------------------------------------
    //  PlayoutDeviceName
    // ----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::PlayoutDeviceName(
        uint16_t index,
        char name[webrtc::kAdmMaxDeviceNameSize],
        char guid[webrtc::kAdmMaxGuidSize]) {
        bool defaultCommunicationDevice(false);
        const int16_t nDevices(PlayoutDevices());  // also updates the list of
                                                   // devices

        // Special fix for the case when the user selects '-1' as index (<=> Default
        // Communication Device)
        if (index == (uint16_t)(-1)) {
            defaultCommunicationDevice = true;
            index = 0;
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "Default Communication endpoint device will be used");
        }

        if ((index > (nDevices - 1)) || (name == NULL)) {
            return -1;
        }

        memset(name, 0, sizeof(name));

        if (guid != NULL) {
            memset(guid, 0, sizeof(guid));
        }

        rtc::CritScope lock(&critSect_);

        winrt::hstring deviceName;

        // Get the endpoint device's friendly-name
        if (defaultCommunicationDevice) {
            deviceName = GetDefaultDeviceName(DeviceClass::AudioRender);
        }
        else {
            deviceName = GetListDeviceName(DeviceClass::AudioRender, index);
        }

        if (!deviceName.empty()) {
            // Convert the endpoint device's friendly-name to UTF-8
            if (WideCharToMultiByte(CP_UTF8, 0, deviceName.c_str(), -1, name,
                webrtc::kAdmMaxDeviceNameSize, NULL, NULL) == 0) {
                WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                    "WideCharToMultiByte(CP_UTF8) failed with error code %d",
                    GetLastError());
            }
        }

        winrt::hstring deviceId;

        // Get the endpoint ID string (uniquely identifies the device among all audio
        // endpoint devices)
        if (defaultCommunicationDevice) {
            deviceId = GetDefaultDeviceID(DeviceClass::AudioRender);
        }
        else {
            deviceId = GetListDeviceID(DeviceClass::AudioRender, index);
        }

        if (guid != NULL && !deviceId.empty()) {
            // Convert the endpoint device's ID string to UTF-8
            if (WideCharToMultiByte(CP_UTF8, 0, deviceId.c_str(), -1, guid,
                webrtc::kAdmMaxGuidSize, NULL, NULL) == 0) {
                WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                    "WideCharToMultiByte(CP_UTF8) failed with error code %d",
                    GetLastError());
            }
        }

        int32_t ret = (!deviceName.empty() && !deviceId.empty()) ? 0 : -1;
        return ret;
    }

    // ----------------------------------------------------------------------------
    //  RecordingDeviceName
    // ----------------------------------------------------------------------------

    int32_t AudioDeviceWasapi::RecordingDeviceName(
        uint16_t index,
        char name[webrtc::kAdmMaxDeviceNameSize],
        char guid[webrtc::kAdmMaxGuidSize]) {
        bool defaultCommunicationDevice(false);
        const int16_t nDevices(RecordingDevices());  // also updates the list of
                                                     // devices

        // Special fix for the case when the user selects '-1' as index (<=> Default
        // Communication Device)
        if (index == (uint16_t)(-1)) {
            defaultCommunicationDevice = true;
            index = 0;
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "Default Communication endpoint device will be used");
        }

        if ((index > (nDevices - 1)) || (name == NULL)) {
            return -1;
        }

        memset(name, 0, sizeof(name));

        if (guid != NULL) {
            memset(guid, 0, sizeof(guid));
        }

        rtc::CritScope lock(&critSect_);

        winrt::hstring deviceName;

        // Get the endpoint device's friendly-name
        if (defaultCommunicationDevice) {
            deviceName = GetDefaultDeviceName(DeviceClass::AudioCapture);
        }
        else {
            deviceName = GetListDeviceName(DeviceClass::AudioCapture, index);
        }

        if (!deviceName.empty()) {
            // Convert the endpoint device's friendly-name to UTF-8
            if (WideCharToMultiByte(CP_UTF8, 0, deviceName.c_str(), -1, name,
                webrtc::kAdmMaxDeviceNameSize, NULL, NULL) == 0) {
                WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                    "WideCharToMultiByte(CP_UTF8) failed with error code %d",
                    GetLastError());
            }
        }

        winrt::hstring deviceId;

        // Get the endpoint ID string (uniquely identifies the device among all audio
        // endpoint devices)
        if (defaultCommunicationDevice) {
            deviceId = GetDefaultDeviceID(DeviceClass::AudioCapture);
        }
        else {
            deviceId = GetListDeviceID(DeviceClass::AudioCapture, index);
        }

        if (guid != NULL && !deviceId.empty()) {
            // Convert the endpoint device's ID string to UTF-8
            if (WideCharToMultiByte(CP_UTF8, 0, deviceId.c_str(), -1, guid,
                webrtc::kAdmMaxGuidSize, NULL, NULL) == 0) {
                WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                    "WideCharToMultiByte(CP_UTF8) failed with error code %d",
                    GetLastError());
            }
        }

        int32_t ret = (!deviceName.empty() && !deviceId.empty()) ? 0 : -1;
        return ret;
    }

    // ----------------------------------------------------------------------------
    //  RecordingDevices
    // ----------------------------------------------------------------------------
    int16_t AudioDeviceWasapi::RecordingDevices() {
        rtc::CritScope lock(&critSect_);

        // Refresh the list of capture endpoint devices
        RefreshDeviceList(DeviceClass::AudioCapture);

        return (DeviceListCount(DeviceClass::AudioCapture));
    }

    // ----------------------------------------------------------------------------
    //  SetRecordingDevice I (II)
    // ----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::SetRecordingDevice(uint16_t index) {
        if (!recordingEnabled_) {
            return 0;
        }

        // Get current number of available capture endpoint devices and refresh the
        // capture collection.
        UINT nDevices = RecordingDevices();

        if (index < 0 || index >(nDevices - 1)) {
            WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                "device index is out of range [0,%u]", (nDevices - 1));
            return -1;
        }

        rtc::CritScope lock(&critSect_);

        assert(ptrCaptureCollection_ != nullptr);

        // Select an endpoint capture device given the specified index
        captureDevice_ = nullptr;
        deviceIdStringIn_.clear();

        captureDevice_ = ptrCaptureCollection_.GetAt(index);
        deviceIdStringIn_ = captureDevice_.Id();


        // Get the endpoint device's friendly-name
        if (!GetDeviceName(captureDevice_).empty()) {
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_, "friendly name: \"%S\"",
                GetDeviceName(captureDevice_));
        }

        usingInputDeviceIndex_ = true;
        inputDeviceIndex_ = index;

        return 0;
    }

    // ----------------------------------------------------------------------------
    //  SetRecordingDevice II (II)
    // ----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::SetRecordingDevice(
        AudioDeviceModule::WindowsDeviceType device) {
        if (!recordingEnabled_) {
            return 0;
        }

        // If device has been selected once by index value it couldn't be changed
        // afterwards by setting the device type
        if (usingInputDeviceIndex_) {
            return 0;
        }

        rtc::CritScope lock(&critSect_);

        // Refresh the list of capture endpoint devices
        RefreshDeviceList(DeviceClass::AudioCapture);

        // Select an endpoint capture device given the specified role
        captureDevice_ = nullptr;
        deviceIdStringIn_.clear();

        if (device == AudioDeviceModule::kDefaultDevice) {
            inputDeviceRole_ = AudioDeviceRole::Default;
        }
        else if (device == AudioDeviceModule::kDefaultCommunicationDevice) {
            inputDeviceRole_ = AudioDeviceRole::Communications;
        }

        captureDevice_ = GetDefaultDevice(DeviceClass::AudioCapture, inputDeviceRole_);
        deviceIdStringIn_ = captureDevice_.Id();

        winrt::hstring deviceName;

        // Get the endpoint device's friendly-name
        if (!GetDeviceName(captureDevice_).empty()) {
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_, "friendly name: \"%S\"",
                GetDeviceName(captureDevice_));
        }

        return 0;
    }

    // ----------------------------------------------------------------------------
    //  PlayoutIsAvailable
    // ----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::PlayoutIsAvailable(bool* available) {
        *available = false;

        // Try to initialize the playout side
        int32_t res = InitPlayout();

        // Cancel effect of initialization
        StopPlayout();

        if (res != -1) {
            *available = true;
        }

        return 0;
    }

    // ----------------------------------------------------------------------------
    //  RecordingIsAvailable
    // ----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::RecordingIsAvailable(bool* available) {
        *available = false;

        // Try to initialize the recording side
        int32_t res = InitRecording();

        // Cancel effect of initialization
        StopRecording();

        if (res != -1) {
            *available = true;
        }

        return 0;
    }

    // ----------------------------------------------------------------------------
    //  InitPlayout
    // ----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::InitPlayout() {
        if (!playoutEnabled_) {
            return 0;
        }

        rtc::CritScope lock(&playoutControlMutex_);
        return InitPlayoutInternal();
    }

    //-----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::InitPlayoutInternal() {
        rtc::CritScope lock(&critSect_);

        if (playing_) {
            return -1;
        }

        if (playIsInitialized_) {
            return 0;
        }

        if (renderDevice_ == nullptr) {
            return -1;
        }

        // Initialize the speaker (devices might have been added or removed)
        if (InitSpeaker() == -1) {
            WEBRTC_TRACE(kTraceWarning, kTraceAudioDevice, id_,
                "InitSpeaker() failed");
        }

        // Ensure that the updated rendering endpoint device is valid
        if (renderDevice_ == nullptr) {
            return -1;
        }

        HRESULT hr = S_OK;
        WAVEFORMATEX* pWfxOut = NULL;
        WAVEFORMATEX Wfx = WAVEFORMATEX();
        WAVEFORMATEX* pWfxClosestMatch = NULL;

        // Create COM object with IAudioClient interface.
        if (ptrClientOut_ == nullptr) {
            return -1;
        }

        // Retrieve the stream format that the audio engine uses for its internal
        // processing (mixing) of shared-mode streams.
        hr = ptrClientOut_->GetMixFormat(&pWfxOut);
        if (SUCCEEDED(hr)) {
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "Audio Engine's current rendering mix format:");
            // format type
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "wFormatTag     : 0x%X (%u)", pWfxOut->wFormatTag, pWfxOut->wFormatTag);
            // number of channels (i.e. mono, stereo...)
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_, "nChannels      : %d",
                pWfxOut->nChannels);
            // sample rate
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_, "nSamplesPerSec : %d",
                pWfxOut->nSamplesPerSec);
            // for buffer estimation
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_, "nAvgBytesPerSec: %d",
                pWfxOut->nAvgBytesPerSec);
            // block size of data
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_, "nBlockAlign    : %d",
                pWfxOut->nBlockAlign);
            // number of bits per sample of mono data
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_, "wBitsPerSample : %d",
                pWfxOut->wBitsPerSample);
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_, "cbSize         : %d",
                pWfxOut->cbSize);
        }

        // Set wave format
        Wfx.wFormatTag = WAVE_FORMAT_PCM;
        Wfx.wBitsPerSample = 16;
        Wfx.cbSize = 0;

        const int freqs[] = { 48000, 44100, 16000, 96000, 32000, 8000 };
        hr = S_FALSE;

        // Iterate over frequencies and channels, in order of priority
        for (int freq = 0; freq < sizeof(freqs) / sizeof(freqs[0]); freq++) {
            for (int chan = 0; chan < sizeof(playChannelsPrioList_) /
                sizeof(playChannelsPrioList_[0]); chan++) {
                Wfx.nChannels = playChannelsPrioList_[chan];
                Wfx.nSamplesPerSec = freqs[freq];
                Wfx.nBlockAlign = Wfx.nChannels * Wfx.wBitsPerSample / 8;
                Wfx.nAvgBytesPerSec = Wfx.nSamplesPerSec * Wfx.nBlockAlign;
                // If the method succeeds and the audio endpoint device supports the
                // specified stream format, it returns S_OK. If the method succeeds and
                // provides a closest match to the specified format, it returns S_FALSE.
                hr = ptrClientOut_->IsFormatSupported(
                    AUDCLNT_SHAREMODE_SHARED,
                    &Wfx,
                    &pWfxClosestMatch);
                if (hr == S_OK) {
                    break;
                }
                else {
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                        "nChannels=%d, nSamplesPerSec=%d is not supported",
                        Wfx.nChannels, Wfx.nSamplesPerSec);
                }
            }
            if (hr == S_OK)
                break;
        }

        // TODO(andrew): what happens in the event of failure in the above loop?
        // Is ptrClientOut_->Initialize expected to fail?
        // Same in InitRecording().
        if (hr == S_OK) {
            playAudioFrameSize_ = Wfx.nBlockAlign;
            playBlockSize_ = Wfx.nSamplesPerSec / 100;
            playSampleRate_ = Wfx.nSamplesPerSec;
            // The device itself continues to run at 44.1 kHz.
            devicePlaySampleRate_ = Wfx.nSamplesPerSec;
            devicePlayBlockSize_ = Wfx.nSamplesPerSec / 100;
            playChannels_ = Wfx.nChannels;

            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "VoE selected this rendering format:");
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "wFormatTag         : 0x%X (%u)", Wfx.wFormatTag, Wfx.wFormatTag);
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "nChannels          : %d", Wfx.nChannels);
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "nSamplesPerSec     : %d", Wfx.nSamplesPerSec);
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "nAvgBytesPerSec    : %d", Wfx.nAvgBytesPerSec);
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "nBlockAlign        : %d", Wfx.nBlockAlign);
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "wBitsPerSample     : %d", Wfx.wBitsPerSample);
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "cbSize             : %d", Wfx.cbSize);
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "Additional settings:");
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "playAudioFrameSize_: %d", playAudioFrameSize_);
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "playBlockSize_     : %d", playBlockSize_);
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "playChannels_      : %d", playChannels_);
        }

        Get44kHzDrift();

        // Create a rendering stream.
        //
        // **************************************************************************
        // For a shared-mode stream that uses event-driven buffering, the caller must
        // set both hnsPeriodicity and hnsBufferDuration to 0. The Initialize method
        // determines how large a buffer to allocate based on the scheduling period
        // of the audio engine. Although the client's buffer processing thread is
        // event driven, the basic buffer management process, as described
        // previously, is unaltered.
        // Each time the thread awakens, it should call
        // IAudioClient::GetCurrentPadding to determine how much data to write to a
        // rendering buffer or read from a capture buffer. In contrast to the two
        // buffers that the Initialize method allocates for an exclusive-mode stream
        // that uses event-driven buffering, a shared-mode stream requires a single
        // buffer.
        // **************************************************************************

        // ask for minimum buffer size (default)
        REFERENCE_TIME hnsBufferDuration = 0;
        if (devicePlaySampleRate_ == 44100) {
            // Ask for a larger buffer size (30ms) when using 44.1kHz as render rate.
            // There seems to be a larger risk of underruns for 44.1 compared
            // with the default rate (48kHz). When using default, we set the requested
            // buffer duration to 0, which sets the buffer to the minimum size
            // required by the engine thread. The actual buffer size can then be
            // read by GetBufferSize() and it is 20ms on most machines.
            hnsBufferDuration = 30 * 10000;
        }

        if (ptrAudioBuffer_) {
            // Update the audio buffer with the selected parameters
            ptrAudioBuffer_->SetPlayoutSampleRate(playSampleRate_);
            ptrAudioBuffer_->SetPlayoutChannels((uint8_t)playChannels_);
        }
        else {
            // We can enter this state during CoreAudioIsSupported() when no
            // AudioDeviceImplementation has been created, hence the AudioDeviceBuffer
            // does not exist. It is OK to end up here since we don't initiate any
            // media in CoreAudioIsSupported().
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "AudioDeviceBuffer must be attached before streaming can start");
        }

        // Get the actual size of the shared (endpoint buffer).
        // Typical value is 960 audio frames <=> 20ms @ 48kHz sample rate.
        UINT bufferFrameCount(0);
        hr = ptrClientOut_->GetBufferSize(
            &bufferFrameCount);
        if (SUCCEEDED(hr)) {
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "IAudioClient::GetBufferSize() => %u (<=> %u bytes)",
                bufferFrameCount, bufferFrameCount * playAudioFrameSize_);
        }

        // Set the event handle that the system signals when an audio buffer is ready
        // to be processed by the client.
        hr = ptrClientOut_->SetEventHandle(
            hRenderSamplesReadyEvent_);
        // EXIT_ON_ERROR(hr);

        // Get an IAudioRenderClient interface.
        SAFE_RELEASE(ptrRenderClient_);
        hr = ptrClientOut_->GetService(
            __uuidof(IAudioRenderClient),
            reinterpret_cast<void**>(&ptrRenderClient_));
        EXIT_ON_ERROR(hr);

        // Mark playout side as initialized
        playIsInitialized_ = true;

        CoTaskMemFree(pWfxOut);
        CoTaskMemFree(pWfxClosestMatch);

        WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
            "render side is now initialized");
        return 0;

    Exit:
        TraceCOMError(hr);
        CoTaskMemFree(pWfxOut);
        CoTaskMemFree(pWfxClosestMatch);
        SAFE_RELEASE(ptrClientOut_);
        SAFE_RELEASE(ptrRenderClient_);
        return -1;
    }

    // ----------------------------------------------------------------------------
    //  InitRecording
    // ----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::InitRecording() {
        if (!recordingEnabled_) {
            return 0;
        }

        rtc::CritScope lock(&recordingControlMutex_);
        return InitRecordingInternal();
    }

    //-----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::InitRecordingInternal() {
        rtc::CritScope lock(&critSect_);

        if (recording_) {
            return -1;
        }

        if (recIsInitialized_) {
            return 0;
        }

        if (QueryPerformanceFrequency(&perfCounterFreq_) == 0) {
            return -1;
        }
        perfCounterFactor_ = 10000000.0 / static_cast<double>(
            perfCounterFreq_.QuadPart);

        if (captureDevice_ == nullptr) {
            return -1;
        }

        // Initialize the microphone (devices might have been added or removed)
        if (InitMicrophone() == -1) {
            WEBRTC_TRACE(kTraceWarning, kTraceAudioDevice, id_,
                "InitMicrophone() failed");
        }

        // Ensure that the updated capturing endpoint device is valid
        if (captureDevice_ == nullptr) {
            return -1;
        }

        HRESULT hr = S_OK;
        WAVEFORMATEX* pWfxIn = NULL;
        WAVEFORMATEX Wfx = WAVEFORMATEX();
        WAVEFORMATEX* pWfxClosestMatch = NULL;

        // Create COM object with IAudioClient interface.
        if (ptrClientIn_ == nullptr) {
            return -1;
        }

        // Retrieve the stream format that the audio engine uses for its internal
        // processing (mixing) of shared-mode streams.
        hr = ptrClientIn_->GetMixFormat(&pWfxIn);
        if (SUCCEEDED(hr)) {
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "Audio Engine's current capturing mix format:");
            // format type
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "wFormatTag     : 0x%X (%u)", pWfxIn->wFormatTag, pWfxIn->wFormatTag);
            // number of channels (i.e. mono, stereo...)
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_, "nChannels      : %d",
                pWfxIn->nChannels);
            // sample rate
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_, "nSamplesPerSec : %d",
                pWfxIn->nSamplesPerSec);
            // for buffer estimation
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_, "nAvgBytesPerSec: %d",
                pWfxIn->nAvgBytesPerSec);
            // block size of data
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_, "nBlockAlign    : %d",
                pWfxIn->nBlockAlign);
            // number of bits per sample of mono data
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_, "wBitsPerSample : %d",
                pWfxIn->wBitsPerSample);
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_, "cbSize         : %d",
                pWfxIn->cbSize);
        }

        // Set wave format
        Wfx.wFormatTag = WAVE_FORMAT_PCM;
        Wfx.wBitsPerSample = 16;
        Wfx.cbSize = 0;

        const int freqs[6] = { 48000, 44100, 16000, 96000, 32000, 8000 };
        hr = S_FALSE;

        // Iterate over frequencies and channels, in order of priority
        for (int freq = 0; freq < sizeof(freqs) / sizeof(freqs[0]); freq++) {
            for (int chan = 0; chan < sizeof(recChannelsPrioList_) /
                sizeof(recChannelsPrioList_[0]); chan++) {
                Wfx.nChannels = recChannelsPrioList_[chan];
                Wfx.nSamplesPerSec = freqs[freq];
                Wfx.nBlockAlign = Wfx.nChannels * Wfx.wBitsPerSample / 8;
                Wfx.nAvgBytesPerSec = Wfx.nSamplesPerSec * Wfx.nBlockAlign;
                // If the method succeeds and the audio endpoint device supports the
                // specified stream format, it returns S_OK. If the method succeeds and
                // provides a closest match to the specified format, it returns S_FALSE.
                hr = ptrClientIn_->IsFormatSupported(
                    AUDCLNT_SHAREMODE_SHARED,
                    &Wfx,
                    &pWfxClosestMatch);
                if (hr == S_OK) {
                    break;
                }
                else {
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                        "nChannels=%d, nSamplesPerSec=%d is not supported",
                        Wfx.nChannels, Wfx.nSamplesPerSec);
                }
            }
            if (hr == S_OK)
                break;
        }

        if (hr == S_OK) {
            recAudioFrameSize_ = Wfx.nBlockAlign;
            recSampleRate_ = Wfx.nSamplesPerSec;
            recBlockSize_ = Wfx.nSamplesPerSec / 100;
            recChannels_ = Wfx.nChannels;

            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "VoE selected this capturing format:");
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "wFormatTag        : 0x%X (%u)", Wfx.wFormatTag, Wfx.wFormatTag);
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_, "nChannels         : %d",
                Wfx.nChannels);
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_, "nSamplesPerSec    : %d",
                Wfx.nSamplesPerSec);
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_, "nAvgBytesPerSec   : %d",
                Wfx.nAvgBytesPerSec);
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_, "nBlockAlign       : %d",
                Wfx.nBlockAlign);
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_, "wBitsPerSample    : %d",
                Wfx.wBitsPerSample);
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_, "cbSize            : %d",
                Wfx.cbSize);
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_, "Additional settings:");
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_, "recAudioFrameSize_: %d",
                recAudioFrameSize_);
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_, "recBlockSize_     : %d",
                recBlockSize_);
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_, "recChannels_      : %d",
                recChannels_);
        }

        if (ptrAudioBuffer_) {
            // Update the audio buffer with the selected parameters
            ptrAudioBuffer_->SetRecordingSampleRate(recSampleRate_);
            ptrAudioBuffer_->SetRecordingChannels((uint8_t)recChannels_);
        }
        else {
            // We can enter this state during CoreAudioIsSupported() when no
            // AudioDeviceImplementation has been created, hence the AudioDeviceBuffer
            // does not exist. It is OK to end up here since we don't initiate any
            // media in CoreAudioIsSupported().
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "AudioDeviceBuffer must be attached before streaming can start");
        }

        // Get the actual size of the shared (endpoint buffer).
        // Typical value is 960 audio frames <=> 20ms @ 48kHz sample rate.
        UINT bufferFrameCount(0);
        hr = ptrClientIn_->GetBufferSize(&bufferFrameCount);
        if (SUCCEEDED(hr)) {
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "IAudioClient::GetBufferSize() => %u (<=> %u bytes)", bufferFrameCount,
                bufferFrameCount * recAudioFrameSize_);
        }

        // Set the event handle that the system signals when an audio buffer is ready
        // to be processed by the client.
        hr = ptrClientIn_->SetEventHandle(hCaptureSamplesReadyEvent_);
        // EXIT_ON_ERROR(hr);

        // Get an IAudioCaptureClient interface.
        SAFE_RELEASE(ptrCaptureClient_);
        hr = ptrClientIn_->GetService(__uuidof(IAudioCaptureClient),
            reinterpret_cast<void**>(&ptrCaptureClient_));
        EXIT_ON_ERROR(hr);

        // Mark capture side as initialized
        recIsInitialized_ = true;

        CoTaskMemFree(pWfxIn);
        CoTaskMemFree(pWfxClosestMatch);

        WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
            "capture side is now initialized");
        return 0;

    Exit:
        TraceCOMError(hr);
        CoTaskMemFree(pWfxIn);
        CoTaskMemFree(pWfxClosestMatch);
        SAFE_RELEASE(ptrClientIn_);
        SAFE_RELEASE(ptrCaptureClient_);
        return -1;
    }

    // ----------------------------------------------------------------------------
    //  StartRecording
    // ----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::StartRecording() {
        if (!recordingEnabled_) {
            return 0;
        }

        rtc::CritScope lock(&recordingControlMutex_);
        if (ptrAudioBuffer_)
            ptrAudioBuffer_->StartRecording();
        return StartRecordingInternal();
    }

    //-----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::StartRecordingInternal() {
        if (!recIsInitialized_) {
            return -1;
        }

        if (hRecThread_ != NULL) {
            return 0;
        }

        if (recording_) {
            return 0;
        }

        {
            rtc::CritScope lock(&critSect_);

            // Create thread which will drive the capturing
            LPTHREAD_START_ROUTINE lpStartAddress = WSAPICaptureThread;

            assert(hRecThread_ == NULL);
            hRecThread_ = CreateThread(NULL,
                0,
                lpStartAddress,
                this,
                0,
                NULL);
            if (hRecThread_ == NULL) {
                WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                    "failed to create the recording thread");
                return -1;
            }

            // Set thread priority to highest possible
            SetThreadPriority(hRecThread_, THREAD_PRIORITY_TIME_CRITICAL);

            assert(hGetCaptureVolumeThread_ == NULL);
            hGetCaptureVolumeThread_ = CreateThread(NULL,
                0,
                GetCaptureVolumeThread,
                this,
                0,
                NULL);
            if (hGetCaptureVolumeThread_ == NULL) {
                WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                    "  failed to create the volume getter thread");
                return -1;
            }

            assert(hSetCaptureVolumeThread_ == NULL);
            hSetCaptureVolumeThread_ = CreateThread(NULL,
                0,
                SetCaptureVolumeThread,
                this,
                0,
                NULL);
            if (hSetCaptureVolumeThread_ == NULL) {
                WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                    "  failed to create the volume setter thread");
                return -1;
            }
        }  // critScoped

        DWORD ret = WaitForSingleObject(hCaptureStartedEvent_, 1000);
        if (ret != WAIT_OBJECT_0) {
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "capturing did not start up properly");
            return -1;
        }
        WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
            "capture audio stream has now started...");

        avgCPULoad_ = 0.0f;
        playAcc_ = 0;
        recording_ = true;

        return 0;
    }

    // ----------------------------------------------------------------------------
    //  StopRecording
    // ----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::StopRecording() {
        if (!recordingEnabled_) {
            return 0;
        }

        rtc::CritScope lock(&recordingControlMutex_);
        int32_t result = StopRecordingInternal();
        if (ptrAudioBuffer_)
            ptrAudioBuffer_->StopRecording();
        return result;
    }

    int32_t AudioDeviceWasapi::StopRecordingInternal() {
        int32_t err = 0;

        if (!recIsInitialized_) {
            return 0;
        }

        Lock();

        if (hRecThread_ == NULL) {
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "no capturing stream is active => close down WASAPI only");
            SAFE_RELEASE(ptrClientIn_);
            SAFE_RELEASE(ptrCaptureClient_);
            recIsInitialized_ = false;
            recording_ = false;
            UnLock();
            return 0;
        }

        // Stop the driving thread...
        WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
            "closing down the webrtc_core_audio_capture_thread...");
        // Manual-reset event; it will remain signalled to stop all capture threads.
        SetEvent(hShutdownCaptureEvent_);

        UnLock();
        DWORD ret = WaitForSingleObject(hRecThread_, 2000);
        if (ret != WAIT_OBJECT_0) {
            WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                "failed to close down webrtc_core_audio_capture_thread (errCode=%d)",
                ret);
            err = -1;
        }
        else {
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "webrtc_core_audio_capture_thread is now closed");
        }

        ret = WaitForSingleObject(hGetCaptureVolumeThread_, 2000);
        if (ret != WAIT_OBJECT_0) {
            // the thread did not stop as it should
            WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                "  failed to close down volume getter thread");
            err = -1;
        }
        else {
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "  volume getter thread is now closed");
        }

        ret = WaitForSingleObject(hSetCaptureVolumeThread_, 2000);
        if (ret != WAIT_OBJECT_0) {
            // the thread did not stop as it should
            WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                "  failed to close down volume setter thread");
            err = -1;
        }
        else {
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "  volume setter thread is now closed");
        }
        Lock();

        ResetEvent(hShutdownCaptureEvent_);  // Must be manually reset.

        recIsInitialized_ = false;
        recording_ = false;

        // These will create thread leaks in the result of an error,
        // but we can at least resume the call.
        CloseHandle(hRecThread_);
        hRecThread_ = NULL;

        CloseHandle(hGetCaptureVolumeThread_);
        hGetCaptureVolumeThread_ = NULL;

        CloseHandle(hSetCaptureVolumeThread_);
        hSetCaptureVolumeThread_ = NULL;

        // Reset the recording delay value.
        sndCardRecDelay_ = 0;

        UnLock();

        return err;
    }

    // ----------------------------------------------------------------------------
    //  RecordingIsInitialized
    // ----------------------------------------------------------------------------
    bool AudioDeviceWasapi::RecordingIsInitialized() const {
        return (recIsInitialized_);
    }

    // ----------------------------------------------------------------------------
    //  Recording
    // ----------------------------------------------------------------------------
    bool AudioDeviceWasapi::Recording() const {
        return (recording_);
    }

    // ----------------------------------------------------------------------------
    //  PlayoutIsInitialized
    // ----------------------------------------------------------------------------
    bool AudioDeviceWasapi::PlayoutIsInitialized() const {
        return (playIsInitialized_);
    }

    // ----------------------------------------------------------------------------
    //  StartPlayout
    // ----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::StartPlayout() {
        if (!playoutEnabled_) {
            return 0;
        }

        rtc::CritScope lock(&playoutControlMutex_);
        if (ptrAudioBuffer_)
            ptrAudioBuffer_->StartPlayout();
        return StartPlayoutInternal();
    }

    //-----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::StartPlayoutInternal() {
        if (!playIsInitialized_) {
            return -1;
        }

        if (hPlayThread_ != NULL) {
            return 0;
        }

        if (playing_) {
            return 0;
        }

        {
            rtc::CritScope lock(&critSect_);

            // Create thread which will drive the rendering.
            assert(hPlayThread_ == NULL);
            hPlayThread_ = CreateThread(
                NULL,
                0,
                WSAPIRenderThread,
                this,
                0,
                NULL);
            if (hPlayThread_ == NULL) {
                WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                    "failed to create the playout thread");
                return -1;
            }

            // Set thread priority to highest possible.
            SetThreadPriority(hPlayThread_, THREAD_PRIORITY_TIME_CRITICAL);
        }  // critScoped

        DWORD ret = WaitForSingleObject(hRenderStartedEvent_, 1000);
        if (ret != WAIT_OBJECT_0) {
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "rendering did not start up properly");
            return -1;
        }

        playing_ = true;
        WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
            "rendering audio stream has now started...");

        return 0;
    }

    // ----------------------------------------------------------------------------
    //  StopPlayout
    // ----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::StopPlayout() {
        if (!playoutEnabled_) {
            return 0;
        }

        rtc::CritScope lock(&playoutControlMutex_);
        int32_t result = StopPlayoutInternal();
        if (ptrAudioBuffer_)
            ptrAudioBuffer_->StopPlayout();
        return result;
    }

    int32_t AudioDeviceWasapi::StopPlayoutInternal() {
        if (!playIsInitialized_) {
            return 0;
        }

        {
            rtc::CritScope lock(&critSect_);

            if (hPlayThread_ == NULL) {
                WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                    "no rendering stream is active => close down WASAPI only");
                SAFE_RELEASE(ptrClientOut_);
                SAFE_RELEASE(ptrRenderClient_);
                playIsInitialized_ = false;
                playing_ = false;
                return 0;
            }

            // stop the driving thread...
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "closing down the webrtc_core_audio_render_thread...");
            SetEvent(hShutdownRenderEvent_);
        }  // critScoped

        DWORD ret = WaitForSingleObject(hPlayThread_, 2000);
        if (ret != WAIT_OBJECT_0) {
            // the thread did not stop as it should
            WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                "failed to close down webrtc_core_audio_render_thread");
            CloseHandle(hPlayThread_);
            hPlayThread_ = NULL;
            playIsInitialized_ = false;
            playing_ = false;
            return -1;
        }

        {
            rtc::CritScope lock(&critSect_);
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "webrtc_core_audio_render_thread is now closed");

            // to reset this event manually at each time we finish with it, in case
            // that the render thread has exited before StopPlayout(), this event
            // might be caught by the new render thread within same VoE instance.
            ResetEvent(hShutdownRenderEvent_);

            SAFE_RELEASE(ptrClientOut_);
            SAFE_RELEASE(ptrRenderClient_);
            SAFE_RELEASE(ptrRenderSimpleVolume_);

            playIsInitialized_ = false;
            playing_ = false;

            CloseHandle(hPlayThread_);
            hPlayThread_ = NULL;

            if (builtInAecEnabled_ && recording_) {
                // The DMO won't provide us captured output data unless we
                // give it render data to process.
                //
                // We still permit the playout to shutdown, and trace a warning.
                // Otherwise, VoE can get into a state which will never permit
                // playout to stop properly.
                WEBRTC_TRACE(kTraceWarning, kTraceAudioDevice, id_,
                    "Recording should be stopped before playout when using the "
                    "built-in AEC");
            }

            // Reset the playout delay value.
            sndCardPlayDelay_ = 0;
        }  // critScoped

        return 0;
    }

    // ----------------------------------------------------------------------------
    //  PlayoutDelay
    // ----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::PlayoutDelay(uint16_t* delayMS) const {
        rtc::CritScope lock(&critSect_);
        *delayMS = static_cast<uint16_t>(sndCardPlayDelay_);
        return 0;
    }

    // ----------------------------------------------------------------------------
    //  Playing
    // ----------------------------------------------------------------------------
    bool AudioDeviceWasapi::Playing() const {
        return (playing_);
    }

    // ----------------------------------------------------------------------------
    //  CPULoad
    // ----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::CPULoad(uint16_t* load) const {
        *load = static_cast<uint16_t> (100 * avgCPULoad_);

        return 0;
    }

    // ----------------------------------------------------------------------------
    //  PlayoutWarning
    // ----------------------------------------------------------------------------
    bool AudioDeviceWasapi::PlayoutWarning() const {
        return (playWarning_ > 0);
    }

    // ----------------------------------------------------------------------------
    //  PlayoutError
    // ----------------------------------------------------------------------------
    bool AudioDeviceWasapi::PlayoutError() const {
        return (playError_ > 0);
    }

    // ----------------------------------------------------------------------------
    //  RecordingWarning
    // ----------------------------------------------------------------------------
    bool AudioDeviceWasapi::RecordingWarning() const {
        return (recWarning_ > 0);
    }

    // ----------------------------------------------------------------------------
    //  RecordingError
    // ----------------------------------------------------------------------------
    bool AudioDeviceWasapi::RecordingError() const {
        return (recError_ > 0);
    }

    // ----------------------------------------------------------------------------
    //  ClearPlayoutWarning
    // ----------------------------------------------------------------------------
    void AudioDeviceWasapi::ClearPlayoutWarning() {
        playWarning_ = 0;
    }

    // ----------------------------------------------------------------------------
    //  ClearPlayoutError
    // ----------------------------------------------------------------------------
    void AudioDeviceWasapi::ClearPlayoutError() {
        playError_ = 0;
    }

    // ----------------------------------------------------------------------------
    //  ClearRecordingWarning
    // ----------------------------------------------------------------------------
    void AudioDeviceWasapi::ClearRecordingWarning() {
        recWarning_ = 0;
    }

    // ----------------------------------------------------------------------------
    //  ClearRecordingError
    // ----------------------------------------------------------------------------
    void AudioDeviceWasapi::ClearRecordingError() {
        recError_ = 0;
    }

    // ============================================================================
    //                                 Private Methods
    // ============================================================================

    // ----------------------------------------------------------------------------
    //  [static] WSAPIRenderThread
    // ----------------------------------------------------------------------------
    DWORD WINAPI AudioDeviceWasapi::WSAPIRenderThread(LPVOID context) {
        return reinterpret_cast<AudioDeviceWasapi*>(context)->
            DoRenderThread();
    }

    // ----------------------------------------------------------------------------
    //  [static] WSAPICaptureThread
    // ----------------------------------------------------------------------------
    DWORD WINAPI AudioDeviceWasapi::WSAPICaptureThread(LPVOID context) {
        return reinterpret_cast<AudioDeviceWasapi*>(context)->
            DoCaptureThread();
    }

    //-----------------------------------------------------------------------------
    DWORD WINAPI AudioDeviceWasapi::GetCaptureVolumeThread(LPVOID context) {
        return reinterpret_cast<AudioDeviceWasapi*>(context)->
            DoGetCaptureVolumeThread();
    }

    //-----------------------------------------------------------------------------
    DWORD WINAPI AudioDeviceWasapi::SetCaptureVolumeThread(LPVOID context) {
        return reinterpret_cast<AudioDeviceWasapi*>(context)->
            DoSetCaptureVolumeThread();
    }

    //-----------------------------------------------------------------------------
    DWORD AudioDeviceWasapi::DoGetCaptureVolumeThread() {
        HANDLE waitObject = hShutdownCaptureEvent_;

        while (1) {
            DWORD waitResult = WaitForSingleObject(waitObject,
                GET_MIC_VOLUME_INTERVAL_MS);
            switch (waitResult) {
            case WAIT_OBJECT_0:  // hShutdownCaptureEvent_
                return 0;
            case WAIT_TIMEOUT:   // timeout notification
                break;
            default:             // unexpected error
                WEBRTC_TRACE(kTraceWarning, kTraceAudioDevice, id_,
                    "  unknown wait termination on get volume thread");
                return 1;
            }
        }
    }

    //-----------------------------------------------------------------------------
    DWORD AudioDeviceWasapi::DoSetCaptureVolumeThread() {
        HANDLE waitArray[2] = { hShutdownCaptureEvent_, hSetCaptureVolumeEvent_ };

        while (1) {
            DWORD waitResult = WaitForMultipleObjects(2, waitArray, FALSE, INFINITE);
            switch (waitResult) {
            case WAIT_OBJECT_0:      // hShutdownCaptureEvent_
                return 0;
            case WAIT_OBJECT_0 + 1:  // hSetCaptureVolumeEvent_
                break;
            default:                 // unexpected error
                WEBRTC_TRACE(kTraceWarning, kTraceAudioDevice, id_,
                    "  unknown wait termination on set volume thread");
                return 1;
            }

            Lock();
            uint32_t newMicLevel = newMicLevel_;
            UnLock();

            if (SetMicrophoneVolume(newMicLevel) == -1) {
                WEBRTC_TRACE(kTraceWarning, kTraceAudioDevice, id_,
                    "  the required modification of the microphone volume failed");
            }
        }
    }

    // ----------------------------------------------------------------------------
    //  DoRenderThread
    // ----------------------------------------------------------------------------
    DWORD AudioDeviceWasapi::DoRenderThread()
    {
        IAudioClock* clock = NULL;
        HRESULT hr = S_OK;
        bool keepPlaying = true;

        // scope: do thread
        {
            HANDLE waitArray[2] = { hShutdownRenderEvent_, hRenderSamplesReadyEvent_ };

            LARGE_INTEGER t1;
            LARGE_INTEGER t2;
            int32_t time(0);

            // Initialize COM as MTA in this thread.
            ScopedCOMInitializer comInit(ScopedCOMInitializer::kMTA);
            if (!comInit.succeeded()) {
                WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                    "failed to initialize COM in render thread");
                return 1;
            }

            SetThreadName(0, "webrtc_core_audio_render_thread");

            Lock();

            // Get size of rendering buffer (length is expressed as the number of audio
            // frames the buffer can hold).
            // This value is fixed during the rendering session.
            UINT32 bufferLength = 0;
            hr = ptrClientOut_->GetBufferSize(&bufferLength);
            EXIT_ON_ERROR(hr);
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "[REND] size of buffer       : %u", bufferLength);

            // Get maximum latency for the current stream (will not change for the
            // lifetime of the IAudioClient object).
            REFERENCE_TIME latency;
            ptrClientOut_->GetStreamLatency(&latency);
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "[REND] max stream latency   : %u (%3.2f ms)",
                (DWORD)latency, (double)(latency / 10000.0));

            // Get the length of the periodic interval separating successive processing
            // passes by the audio engine on the data in the endpoint buffer.
            //
            // The period between processing passes by the audio engine is fixed for a
            // particular audio endpoint device and represents the smallest processing
            // quantum for the audio engine. This period plus the stream latency between
            // the buffer and endpoint device represents the minimum possible latency
            // that an audio application can achieve. Typical value: 100000 <=> 0.01
            // sec = 10ms.
            REFERENCE_TIME devPeriod = 0;
            REFERENCE_TIME devPeriodMin = 0;
            ptrClientOut_->GetDevicePeriod(&devPeriod, &devPeriodMin);
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "[REND] device period        : %u (%3.2f ms)", (DWORD)devPeriod,
                static_cast<double>(devPeriod / 10000.0));

            // Derive initial rendering delay.
            // Example: 10*(960/480) + 15 = 20 + 15 = 35ms
            int playout_delay = 10 * (bufferLength / playBlockSize_) +
                static_cast<int>((latency + devPeriod) / 10000);
            sndCardPlayDelay_ = playout_delay;
            writtenSamples_ = 0;
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "[REND] initial delay        : %u", playout_delay);

            double endpointBufferSizeMS = 10.0 * (static_cast<double>(bufferLength) /
                static_cast<double>(devicePlayBlockSize_));
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "[REND] endpointBufferSizeMS : %3.2f", endpointBufferSizeMS);

            BYTE* pData = NULL;
            // Before starting the stream, fill the rendering buffer with silence.
            {
                UINT32 initialPadding = 0;
                hr = ptrClientOut_->GetCurrentPadding(&initialPadding);
                EXIT_ON_ERROR(hr);

                // Derive the amount of available space in the output buffer
                // Is it possible to silence the padding as well?
                uint32_t initialFramesAvailable = bufferLength - initialPadding;

                hr = ptrRenderClient_->GetBuffer(initialFramesAvailable, &pData);
                EXIT_ON_ERROR(hr);

                hr = ptrRenderClient_->ReleaseBuffer(initialFramesAvailable,
                    AUDCLNT_BUFFERFLAGS_SILENT);
                EXIT_ON_ERROR(hr);
            }

            writtenSamples_ += bufferLength;

            hr = ptrClientOut_->GetService(__uuidof(IAudioClock),
                reinterpret_cast<void**>(&clock));
            if (FAILED(hr)) {
                WEBRTC_TRACE(kTraceWarning, kTraceAudioDevice, id_,
                    "failed to get IAudioClock interface from the IAudioClient");
            }

            // Start up the rendering audio stream.
            hr = ptrClientOut_->Start();
            if (FAILED(hr)) {
                WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                    "failed to start rendering client, hr = 0x%08X", hr);
                goto Exit;
            }

            UnLock();

            // Set event which will ensure that the calling thread modifies the playing
            // state to true.
            SetEvent(hRenderStartedEvent_);
            // >> ------------------ THREAD LOOP ------------------

            while (keepPlaying) {
                // Wait for a render notification event or a shutdown event
                DWORD waitResult = WaitForMultipleObjects(2, waitArray, FALSE, 500);
                switch (waitResult) {
                case WAIT_OBJECT_0 + 0:     // hShutdownRenderEvent_
                    keepPlaying = false;
                    break;
                case WAIT_OBJECT_0 + 1:     // hRenderSamplesReadyEvent_
                    break;
                case WAIT_TIMEOUT:          // timeout notification
                    WEBRTC_TRACE(kTraceWarning, kTraceAudioDevice, id_,
                        "render event timed out after 0.5 seconds");
                    goto Exit;
                default:                    // unexpected error
                    WEBRTC_TRACE(kTraceWarning, kTraceAudioDevice, id_,
                        "unknown wait termination on render side");
                    goto Exit;
                }

                while (keepPlaying) {
                    Lock();

                    // Sanity check to ensure that essential states are not modified
                    // during the unlocked period.
                    if (ptrRenderClient_ == NULL || ptrClientOut_ == NULL) {
                        UnLock();
                        WEBRTC_TRACE(kTraceCritical, kTraceAudioDevice, id_,
                            "output state has been modified during unlocked period");
                        goto Exit;
                    }

                    // Get the number of frames of padding (queued up to play) in the
                    // endpoint buffer.
                    UINT32 padding = 0;
                    hr = ptrClientOut_->GetCurrentPadding(&padding);
                    if (FAILED(hr)) {
                        WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                            "rendering loop failed (GetCurrentPadding), hr = 0x%08X", hr);
                        goto Exit;
                    }

                    // Derive the amount of available space in the output buffer
                    uint32_t framesAvailable = bufferLength - padding;
                    // WEBRTC_TRACE(kTraceStream, kTraceAudioDevice, id_,
                    //   "#available audio frames = %u", framesAvailable);

                    // Do we have 10 ms available in the render buffer?
                    if (framesAvailable < playBlockSize_) {
                        // Not enough space in render buffer to store next render packet.
                        UnLock();
                        break;
                    }

                    // Write n*10ms buffers to the render buffer
                    const uint32_t n10msBuffers = (framesAvailable / playBlockSize_);
                    for (uint32_t n = 0; n < n10msBuffers; n++) {
                        // Get pointer (i.e., grab the buffer) to next space in the shared
                        // render buffer.
                        hr = ptrRenderClient_->GetBuffer(playBlockSize_, &pData);
                        if (FAILED(hr)) {
                            WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                                "rendering loop failed (GetBuffer), hr = 0x%08X", hr);
                            goto Exit;
                        }

                        QueryPerformanceCounter(&t1);    // measure time: START

                        if (ptrAudioBuffer_) {
                            // Request data to be played out (#bytes =
                            // playBlockSize_*_audioFrameSize)
                            UnLock();
                            int32_t nSamples =
                                ptrAudioBuffer_->RequestPlayoutData(playBlockSize_);
                            Lock();

                            if (nSamples == -1) {
                                UnLock();
                                WEBRTC_TRACE(kTraceCritical, kTraceAudioDevice, id_,
                                    "failed to read data from render client");
                                goto Exit;
                            }

                            // Sanity check to ensure that essential states are not modified
                            // during the unlocked period
                            if (ptrRenderClient_ == NULL || ptrClientOut_ == NULL) {
                                UnLock();
                                WEBRTC_TRACE(kTraceCritical, kTraceAudioDevice, id_,
                                    "output state has been modified during unlocked period");
                                goto Exit;
                            }
                            if (nSamples != static_cast<int32_t>(playChannels_ * playBlockSize_)) {
                                WEBRTC_TRACE(kTraceWarning, kTraceAudioDevice, id_,
                                    "nSamples(%d) != playBlockSize_(%d)", nSamples,
                                    playChannels_ * playBlockSize_);
                            }

                            if (ShouldUpmix()) {
                                int size = playBlockSize_ * playAudioFrameSize_;
                                // Create temporary array for up-mixing procedure
                                std::unique_ptr<BYTE> mediaEngineRenderData(new BYTE[size]);

                                // Get the actual (stored) data
                                nSamples = ptrAudioBuffer_->GetPlayoutData(
                                    reinterpret_cast<int8_t*>(mediaEngineRenderData.get()));

                                if (mixFormatSurroundOut_->SubFormat == KSDATAFORMAT_SUBTYPE_PCM) {
                                    // Do the up-mixing. We are using 16-bit samples only at this point
                                    Upmix(reinterpret_cast<int16_t*>(mediaEngineRenderData.get()),
                                        playBlockSize_,
                                        reinterpret_cast<int16_t*>(pData),
                                        playChannels_,
                                        mixFormatSurroundOut_->Format.nChannels);
                                }
                                else if (mixFormatSurroundOut_->SubFormat == KSDATAFORMAT_SUBTYPE_IEEE_FLOAT) {
                                    // Do the up-mixing. We are using 32-bit samples only at this point
                                    UpmixAndConvert(reinterpret_cast<int16_t*>(mediaEngineRenderData.get()),
                                        playBlockSize_,
                                        reinterpret_cast<float*>(pData),
                                        playChannels_,
                                        mixFormatSurroundOut_->Format.nChannels);
                                }
                                else {
                                    WEBRTC_TRACE(kTraceCritical, kTraceAudioDevice, id_,
                                        "audio data format type is not supported");
                                    goto Exit;
                                }
                            }
                            else {
                                // Get the actual (stored) data
                                nSamples = ptrAudioBuffer_->GetPlayoutData(
                                    reinterpret_cast<int8_t*>(pData));
                            }
                        }

                        QueryPerformanceCounter(&t2);    // measure time: STOP
                        time = static_cast<int>(t2.QuadPart - t1.QuadPart);
                        playAcc_ += time;

                        DWORD dwFlags(0);
                        hr = ptrRenderClient_->ReleaseBuffer(playBlockSize_, dwFlags);
                        // See http://msdn.microsoft.com/en-us/library/dd316605(VS.85).aspx
                        // for more details regarding AUDCLNT_E_DEVICE_INVALIDATED.
                        if (FAILED(hr)) {
                            WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                                "rendering loop failed (ReleaseBuffer), hr = 0x%08X", hr);
                            goto Exit;
                        }

                        writtenSamples_ += playBlockSize_;
                    }

                    // Check the current delay on the playout side.
                    if (clock) {
                        UINT64 pos = 0;
                        UINT64 freq = 1;
                        clock->GetPosition(&pos, NULL);
                        clock->GetFrequency(&freq);
                        playout_delay = ROUND((static_cast<double>(writtenSamples_) /
                            devicePlaySampleRate_ - static_cast<double>(pos) / freq) * 1000.0);
                        sndCardPlayDelay_ = playout_delay;
                    }

                    // Clear flag marking a successful recovery.
                    if (playIsRecovering_) {
                        playIsRecovering_ = false;
                    }
                    UnLock();
                }
            }

            // ------------------ THREAD LOOP ------------------ <<

            webrtc::SleepMs(static_cast<DWORD>(endpointBufferSizeMS + 0.5));
            hr = ptrClientOut_->Stop();
        }

    Exit:
        SAFE_RELEASE(clock);

        if (FAILED(hr)) {
            ptrClientOut_->Stop();
            UnLock();
            WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                "rendering terminated with error, hr = 0x%08X", hr);
            TraceCOMError(hr);
        }

        Lock();

        if (keepPlaying) {
            // In case of AUDCLNT_E_DEVICE_INVALIDATED, restart the rendering.
            // https://msdn.microsoft.com/en-us/library/windows/desktop/dd316605(v=vs.85).aspx
            bool isRecoverableError = hr == AUDCLNT_E_DEVICE_INVALIDATED;

            if (ptrClientOut_ != NULL) {
                hr = ptrClientOut_->Stop();
                if (FAILED(hr)) {
                    WEBRTC_TRACE(kTraceError, kTraceUtility, id_, "failed to stop rendering client, hr = 0x%08X", hr);
                    TraceCOMError(hr);
                }
                hr = ptrClientOut_->Reset();
                if (FAILED(hr)) {
                    WEBRTC_TRACE(kTraceError, kTraceUtility, id_, "failed to reset rendering client, hr = 0x%08X", hr);
                    TraceCOMError(hr);
                }
            }

            if (isRecoverableError) {
                if (playIsRecovering_) {
                    // If the AUDCLNT_E_DEVICE_INVALIDATED error is received right
                    // after a recovery, consider it as a failure and avoid another
                    // recovery.
                    WEBRTC_TRACE(kTraceError, kTraceUtility, id_, "kPlayoutError message "
                        "posted: rendering thread has ended prematurely after recovery");
                    playIsRecovering_ = false;
                    playError_ = 1;
                }
                else {
                    WEBRTC_TRACE(kTraceWarning, kTraceUtility, id_,
                        "audio rendering thread has ended prematurely, restarting renderer...");
                    SetEvent(hRestartRenderEvent_);
                }
            }
            else {
                // Trigger callback from module process thread
                WEBRTC_TRACE(kTraceError, kTraceUtility, id_, "kPlayoutError message "
                    "posted: rendering thread has ended prematurely");
                playError_ = 1;
            }
        }
        else {
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "_Rendering thread is now terminated properly");
        }

        UnLock();

        return (DWORD)hr;
    }

    //-----------------------------------------------------------------------------
    DWORD AudioDeviceWasapi::InitCaptureThreadPriority() {
        hMmTask_ = NULL;

        SetThreadName(0, "webrtc_core_audio_capture_thread");

        return S_OK;
    }

    //-----------------------------------------------------------------------------
    void AudioDeviceWasapi::RevertCaptureThreadPriority() {
        hMmTask_ = NULL;
    }

    // ----------------------------------------------------------------------------
    //  DoCaptureThread
    // ----------------------------------------------------------------------------
    DWORD AudioDeviceWasapi::DoCaptureThread() {

        HRESULT hr = S_OK;
        bool keepRecording = true;
        BYTE* syncBuffer = NULL;

        // scope: do capture thread
        {
            HANDLE waitArray[2] = { hShutdownCaptureEvent_, hCaptureSamplesReadyEvent_ };

            LARGE_INTEGER t1;
            LARGE_INTEGER t2;
            int32_t time(0);

            UINT32 syncBufIndex = 0;

            readSamples_ = 0;

            // Initialize COM as MTA in this thread.
            ScopedCOMInitializer comInit(ScopedCOMInitializer::kMTA);
            if (!comInit.succeeded()) {
                WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                    "failed to initialize COM in capture thread");
                return 1;
            }

            hr = InitCaptureThreadPriority();
            if (FAILED(hr)) {
                return hr;
            }

            Lock();

            // Get size of capturing buffer (length is expressed as the number of audio
            // frames the buffer can hold). This value is fixed during the capturing
            // session.
            UINT32 bufferLength = 0;
            if (ptrClientIn_ == NULL) {
                WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                    "input state has been modified before capture loop starts.");
                return 1;
            }
            hr = ptrClientIn_->GetBufferSize(&bufferLength);
            EXIT_ON_ERROR(hr);
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "[CAPT] size of buffer       : %u", bufferLength);

            // Allocate memory for sync buffer.
            // It is used for compensation between native 44.1 and internal 44.0 and
            // for cases when the capture buffer is larger than 10ms.
            const UINT32 syncBufferSize = 2 * (bufferLength * recAudioFrameSize_);
            syncBuffer = new BYTE[syncBufferSize];
            if (syncBuffer == NULL) {
                return (DWORD)E_POINTER;
            }
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "[CAPT] size of sync buffer  : %u [bytes]", syncBufferSize);

            // Get maximum latency for the current stream (will not change for the
            // lifetime of the IAudioClient object).
            REFERENCE_TIME latency;
            ptrClientIn_->GetStreamLatency(&latency);
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "[CAPT] max stream latency   : %u (%3.2f ms)", (DWORD)latency,
                static_cast<double>(latency / 10000.0));

            // Get the length of the periodic interval separating successive processing
            // passes by the audio engine on the data in the endpoint buffer.
            REFERENCE_TIME devPeriod = 0;
            REFERENCE_TIME devPeriodMin = 0;
            ptrClientIn_->GetDevicePeriod(&devPeriod, &devPeriodMin);
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "[CAPT] device period        : %u (%3.2f ms)", (DWORD)devPeriod,
                static_cast<double>(devPeriod / 10000.0));

            double extraDelayMS = static_cast<double>((latency + devPeriod) / 10000.0);
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "[CAPT] extraDelayMS         : %3.2f", extraDelayMS);

            double endpointBufferSizeMS = 10.0 * (static_cast<double>(bufferLength) /
                static_cast<double>(recBlockSize_));
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "[CAPT] endpointBufferSizeMS : %3.2f", endpointBufferSizeMS);

            // Start up the capturing stream.
            hr = ptrClientIn_->Start();
            if (FAILED(hr)) {
                WEBRTC_TRACE(kTraceCritical, kTraceAudioDevice, id_,
                    "failed to start capture hr = %d", hr);
                goto Exit;
            }

            UnLock();

            // Set event which will ensure that the calling thread modifies the recording
            // state to true.
            SetEvent(hCaptureStartedEvent_);

            // >> ---------------------------- THREAD LOOP ----------------------------

            while (keepRecording) {
                // Wait for a capture notification event or a shutdown event
                DWORD waitResult = WaitForMultipleObjects(2, waitArray, FALSE, 500);
                switch (waitResult) {
                case WAIT_OBJECT_0 + 0:        // hShutdownCaptureEvent_
                    keepRecording = false;
                    break;
                case WAIT_OBJECT_0 + 1:        // hCaptureSamplesReadyEvent_
                    break;
                case WAIT_TIMEOUT:            // timeout notification
                    WEBRTC_TRACE(kTraceWarning, kTraceAudioDevice, id_,
                        "capture event timed out after 0.5 seconds");
                    goto Exit;
                default:                    // unexpected error
                    WEBRTC_TRACE(kTraceWarning, kTraceAudioDevice, id_,
                        "unknown wait termination on capture side");
                    goto Exit;
                }

                while (keepRecording) {
                    BYTE* pData = 0;
                    UINT32 framesAvailable = 0;
                    DWORD flags = 0;
                    UINT64 recTime = 0;
                    UINT64 recPos = 0;

                    Lock();

                    // Sanity check to ensure that essential states are not modified
                    // during the unlocked period.
                    if (ptrCaptureClient_ == NULL || ptrClientIn_ == NULL) {
                        UnLock();
                        WEBRTC_TRACE(kTraceCritical, kTraceAudioDevice, id_,
                            "input state has been modified during unlocked period");
                        goto Exit;
                    }

                    //  Find out how much capture data is available
                    hr = ptrCaptureClient_->GetBuffer(
                        &pData,            // packet which is ready to be read by used
                        &framesAvailable,  // #frames in the captured packet (can be zero)
                        &flags,            // support flags (check)
                        &recPos,           // device position of first audio frame in data
                                           // packet
                        &recTime);         // value of performance counter at the time of
                                           // recording the first audio frame

                    if (SUCCEEDED(hr)) {
                        if (AUDCLNT_S_BUFFER_EMPTY == hr) {
                            // Buffer was empty => start waiting for a new capture notification
                            // event
                            UnLock();
                            break;
                        }

                        if (flags & AUDCLNT_BUFFERFLAGS_SILENT) {
                            // Treat all of the data in the packet as silence and ignore the
                            // actual data values.
                            WEBRTC_TRACE(kTraceWarning, kTraceAudioDevice, id_,
                                "AUDCLNT_BUFFERFLAGS_SILENT");
                            pData = NULL;
                        }

                        assert(framesAvailable != 0);

                        if (pData) {
                            CopyMemory(&syncBuffer[syncBufIndex * recAudioFrameSize_], pData,
                                framesAvailable * recAudioFrameSize_);
                        }
                        else {
                            ZeroMemory(&syncBuffer[syncBufIndex * recAudioFrameSize_],
                                framesAvailable * recAudioFrameSize_);
                        }
                        assert(syncBufferSize >= (syncBufIndex * recAudioFrameSize_) +
                            framesAvailable * recAudioFrameSize_);

                        // Release the capture buffer
                        hr = ptrCaptureClient_->ReleaseBuffer(framesAvailable);
                        if (FAILED(hr)) {
                            WEBRTC_TRACE(kTraceCritical, kTraceAudioDevice, id_,
                                "failed fo release capture buffer hr = %d", hr);
                            goto Exit;
                        }

                        readSamples_ += framesAvailable;
                        syncBufIndex += framesAvailable;

                        QueryPerformanceCounter(&t1);

                        // Get the current recording and playout delay.
                        uint32_t sndCardRecDelay = (uint32_t)
                            (((((UINT64)t1.QuadPart * perfCounterFactor_) - recTime) /
                                10000) + (10 * syncBufIndex) / recBlockSize_ - 10);
                        uint32_t sndCardPlayDelay =
                            static_cast<uint32_t>(sndCardPlayDelay_);

                        sndCardRecDelay_ = sndCardRecDelay;

                        while (syncBufIndex >= recBlockSize_) {
                            if (ptrAudioBuffer_) {
                                ptrAudioBuffer_->SetRecordedBuffer((const int8_t*)syncBuffer,
                                    recBlockSize_);

                                driftAccumulator_ += sampleDriftAt48kHz_;
                                const int32_t clockDrift =
                                    static_cast<int32_t>(driftAccumulator_);
                                driftAccumulator_ -= clockDrift;

                                ptrAudioBuffer_->SetVQEData(sndCardPlayDelay,
                                    sndCardRecDelay);

                                ptrAudioBuffer_->SetTypingStatus(KeyPressed());

                                QueryPerformanceCounter(&t1);    // measure time: START

                                UnLock();  // release lock while making the callback
                                ptrAudioBuffer_->DeliverRecordedData();
                                Lock();    // restore the lock

                                QueryPerformanceCounter(&t2);    // measure time: STOP

                                // Measure "average CPU load".
                                // Basically what we do here is to measure how many percent of
                                // our 10ms period is used for encoding and decoding. This
                                // value should be used as a warning indicator only and not seen
                                // as an absolute value. Running at ~100% will lead to bad QoS.
                                time = static_cast<int>(t2.QuadPart - t1.QuadPart);
                                avgCPULoad_ = static_cast<float>(avgCPULoad_ * .99 +
                                    (time + playAcc_) /
                                    static_cast<double>(perfCounterFreq_.QuadPart));
                                playAcc_ = 0;

                                // Sanity check to ensure that essential states are not
                                // modified during the unlocked period
                                if (ptrCaptureClient_ == NULL || ptrClientIn_ == NULL) {
                                    UnLock();
                                    WEBRTC_TRACE(kTraceCritical, kTraceAudioDevice, id_,
                                        "input state has been modified during unlocked period");
                                    goto Exit;
                                }
                            }

                            // store remaining data which was not able to deliver as 10ms segment
                            MoveMemory(&syncBuffer[0],
                                &syncBuffer[recBlockSize_ * recAudioFrameSize_],
                                (syncBufIndex - recBlockSize_) * recAudioFrameSize_);
                            syncBufIndex -= recBlockSize_;
                            sndCardRecDelay -= 10;
                        }
                    }
                    else {
                        // If GetBuffer returns AUDCLNT_E_BUFFER_ERROR, the thread consuming
                        // the audio samples must wait for the next processing pass. The client
                        // might benefit from keeping a count of the failed GetBuffer calls.
                        // If GetBuffer returns this error repeatedly, the client can start a
                        // new processing loop after shutting down the current client by
                        // calling IAudioClient::Stop, IAudioClient::Reset, and releasing the
                        // audio client.
                        WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                            R"(IAudioCaptureClient::GetBuffer returned hr = 0x%08X)",
                            hr);
                        goto Exit;
                    }
                    // Clear flag marking a successful recovery.
                    if (recIsRecovering_) {
                        recIsRecovering_ = false;
                    }
                    UnLock();
                }
            }

            // ---------------------------- THREAD LOOP ---------------------------- <<

            if (ptrClientIn_) {
                hr = ptrClientIn_->Stop();
            }
        }

    Exit:
        if (FAILED(hr)) {
            ptrClientIn_->Stop();
            UnLock();
            WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                "capturing terminated with error, hr = 0x%08X", hr);
            TraceCOMError(hr);
        }

        RevertCaptureThreadPriority();

        Lock();

        if (keepRecording) {
            // In case of AUDCLNT_E_DEVICE_INVALIDATED, restart the capturing.
            // https://msdn.microsoft.com/en-us/library/windows/desktop/dd316605(v=vs.85).aspx
            bool isRecoverableError = hr == AUDCLNT_E_DEVICE_INVALIDATED;
            if (ptrClientIn_ != NULL) {
                hr = ptrClientIn_->Stop();
                if (FAILED(hr)) {
                    WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                        "failed to stop audio capturing, hr = 0x%08X", hr);
                    TraceCOMError(hr);
                }
                hr = ptrClientIn_->Reset();
                if (FAILED(hr)) {
                    WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                        "failed to reset audio capturing, hr = 0x%08X", hr);
                    TraceCOMError(hr);
                }
            }
            if (isRecoverableError) {
                if (recIsRecovering_) {
                    // If the AUDCLNT_E_DEVICE_INVALIDATED error is received right
                    // after a recovery, consider it as a failure and avoid another
                    // recovery.
                    WEBRTC_TRACE(kTraceError, kTraceUtility, id_, "kRecordingError message "
                        "posted: capturing thread has ended prematurely after recovery");
                    recIsRecovering_ = false;
                    recError_ = 1;
                }
                else {
                    WEBRTC_TRACE(kTraceWarning, kTraceUtility, id_, "capturing thread has "
                        "ended prematurely, restarting capturer...");
                    SetEvent(hRestartCaptureEvent_);
                }
            }
            else {
                WEBRTC_TRACE(kTraceError, kTraceUtility, id_, "kRecordingError message "
                    "posted: capturing thread has ended prematurely");
                // Trigger callback from module process thread
                recError_ = 1;
            }
        }
        else {
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "_Capturing thread is now terminated properly");
        }

        SAFE_RELEASE(ptrClientIn_);
        SAFE_RELEASE(ptrCaptureClient_);
        SAFE_RELEASE(ptrCaptureVolume_);

        UnLock();

        if (syncBuffer) {
            delete[] syncBuffer;
        }

        return (DWORD)hr;
    }

    //-----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::StartObserverThread() {
        if (hObserverThread_ != NULL) {
            return 0;  // Already started.
        }

        // Create thread which will restart renderer or capturer if needed.
        LPTHREAD_START_ROUTINE lpStartAddress = WSAPIObserverThread;
        assert(hObserverThread_ == NULL);
        hObserverThread_ = CreateThread(NULL,
            0,
            lpStartAddress,
            this,
            0,
            NULL);
        if (hObserverThread_ == NULL) {
            WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                "failed to create the observer thread");
            return -1;
        }

        DWORD ret = WaitForSingleObject(hObserverStartedEvent_, 1000);
        if (ret != WAIT_OBJECT_0) {
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "observer did not start up properly");
            return -1;
        }
        WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
            "audio observer has now started");
        return 0;
    }

    //-----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::StopObserverThread() {
        if (hObserverThread_ == NULL) {
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "no observer thread was started");
            return 0;
        }
        WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
            "closing down the audio observer thead...");

        SetEvent(hObserverShutdownEvent_);

        DWORD ret = WaitForSingleObject(hObserverThread_, 2000);
        if (ret != WAIT_OBJECT_0) {
            WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                "failed to close down audio observer thread (errCode=%d)", ret);

            ResetEvent(hObserverShutdownEvent_);  // Must be manually reset.

            // These will create thread leaks in the result of an error,
            // but we can reinitialize this module.
            CloseHandle(hObserverThread_);
            hObserverThread_ = NULL;
            return -1;
        }
        else {
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "audio observer thead is now closed");
        }

        ResetEvent(hObserverShutdownEvent_);  // Must be manually reset.
        CloseHandle(hObserverThread_);
        hObserverThread_ = NULL;

        return 0;
    }

    //-----------------------------------------------------------------------------
    DWORD WINAPI AudioDeviceWasapi::WSAPIObserverThread(LPVOID context) {
        return reinterpret_cast<AudioDeviceWasapi*>(context)->
            DoObserverThread();
    }

    //-----------------------------------------------------------------------------
    DWORD AudioDeviceWasapi::DoObserverThread() {
        SetThreadName(0, "webrtc_core_audio_observer_thread");
        SetEvent(hObserverStartedEvent_);
        bool keepObserving = true;
        HANDLE waitArray[3] = { hObserverShutdownEvent_,
                                hRestartCaptureEvent_,
                                hRestartRenderEvent_ };
        while (keepObserving) {
            // Wait shutdown or restart capturer/renderer events
            DWORD waitResult = WaitForMultipleObjects(3, waitArray, FALSE, INFINITE);
            switch (waitResult) {
            case WAIT_OBJECT_0 + 0:        // hObserverShutdownEvent_
                keepObserving = false;
                break;
            case WAIT_OBJECT_0 + 1: {      // hRestartCaptureEvent_
                rtc::CritScope lock(&recordingControlMutex_);
                WEBRTC_TRACE(kTraceWarning, kTraceAudioDevice, id_,
                    "observer -> restart audio capture event detected");
                int32_t result = StopRecordingInternal();
                if (result == 0) {
                    recIsRecovering_ = true;
                    result = InitRecordingInternal();
                }
                if (result == 0) {
                    result = StartRecordingInternal();
                }
                if (result != 0) {
                    WEBRTC_TRACE(kTraceWarning, kTraceAudioDevice, id_,
                        "failed to restart audio capture");
                    if (recIsRecovering_) {
                        // Stop recording thread in case is running
                        StopRecordingInternal();
                        recIsRecovering_ = false;
                    }
                    // Trigger callback from module process thread
                    recError_ = 2;
                }
                else {
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                        "audio capture restarted");
                }
                ResetEvent(hRestartCaptureEvent_);
                break;
            }
            case WAIT_OBJECT_0 + 2: {      // hRestartRenderEvent_
                rtc::CritScope lock(&playoutControlMutex_);
                int32_t result = StopPlayoutInternal();
                if (result == 0) {
                    playIsRecovering_ = true;
                    result = InitPlayoutInternal();
                }
                if (result == 0) {
                    result = StartPlayoutInternal();
                }
                if (result != 0) {
                    WEBRTC_TRACE(kTraceWarning, kTraceAudioDevice, id_,
                        "failed to restart audio renderer");
                    if (playIsRecovering_) {
                        // Stop playout thread in case is running
                        StopPlayoutInternal();
                        playIsRecovering_ = false;
                    }
                    // Trigger callback from module process thread
                    playError_ = 2;
                }
                else {
                    WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                        "audio renderer restarted");
                }
                ResetEvent(hRestartRenderEvent_);
                break;
            }

            default:                       // unexpected error
                WEBRTC_TRACE(kTraceWarning, kTraceAudioDevice, id_,
                    "audio device observer unknown wait termination");
                break;
            }
        }
        WEBRTC_TRACE(kTraceWarning, kTraceAudioDevice, id_,
            "audio device observer thread terminated");
        return 0;
    }

    //-----------------------------------------------------------------------------
    bool AudioDeviceWasapi::BuiltInAECIsAvailable() const {
        // There is a bug in the OS preventing the Effects detection (Noise SUppression and AEC) to work for Win10 Phones.
        // The bug is severe enough that it's not only the detection that doesn't work but the activation of the effect.
        // For Windows phone (until the bug is solved at the OS level, it will return false, and the software AEC will be used
        if (!recordingEnabled_)
            return false;

        return CheckBuiltInCaptureCapability(winrt::Windows::Media::Effects::AudioEffectType::AcousticEchoCancellation);
    }

    //-----------------------------------------------------------------------------
    bool AudioDeviceWasapi::BuiltInAGCIsAvailable() const {
        if (!playoutEnabled_)
            return false;

        return CheckBuiltInRenderCapability(winrt::Windows::Media::Effects::AudioEffectType::AutomaticGainControl);
    }

    //-----------------------------------------------------------------------------
    bool AudioDeviceWasapi::BuiltInNSIsAvailable() const {
        if (!recordingEnabled_)
            return false;

        return CheckBuiltInCaptureCapability(winrt::Windows::Media::Effects::AudioEffectType::NoiseSuppression);
    }

    //-----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::EnableBuiltInAEC(bool enable) {
        if (recIsInitialized_) {
            WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                "Attempt to set Windows AEC with recording already initialized");
            return -1;
        }

        builtInAecEnabled_ = enable;
        return 0;
    }

    //-----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::EnableBuiltInAGC(bool enable) {
        if (playIsInitialized_) {
            WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                "Attempt to set Windows Automatic Gain Control with playout already initialized");
            return -1;
        }

        builtInAGCEnabled_ = enable;
        return 0;
    }

    //-----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::EnableBuiltInNS(bool enable) {
        if (recIsInitialized_) {
            WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                "Attempt to set Windows Noise Suppression with recording already initialized");
            return -1;
        }

        builtInNSEnabled_ = enable;
        return 0;
    }

    //-----------------------------------------------------------------------------
    bool AudioDeviceWasapi::CheckBuiltInCaptureCapability(winrt::Windows::Media::Effects::AudioEffectType effect) const {

        // Check to see if the current device supports the capability

        winrt::Windows::Media::Effects::AudioCaptureEffectsManager effManager = nullptr;

        winrt::Windows::Media::Capture::MediaCategory category;
        category = winrt::Windows::Media::Capture::MediaCategory::Communications;

        winrt::hstring deviceId;

        if (!deviceIdStringIn_.empty()) {
            deviceId = deviceIdStringIn_;
        }
        else {
            deviceId = captureDevice_.Id();
        }

        effManager = winrt::Windows::Media::Effects::AudioEffectsManager::CreateAudioCaptureEffectsManager(
            deviceId, category, winrt::Windows::Media::AudioProcessing::Default);

        winrt::Windows::Foundation::Collections::IVectorView<winrt::Windows::Media::Effects::AudioEffect> effectsList;

        effectsList = effManager.GetAudioCaptureEffects();

        unsigned int i;
        // Iterate through the supported effect to see if Echo Cancellation is supported
        for (i = 0; i < effectsList.Size(); i++) {
            if (effectsList.GetAt(i).AudioEffectType() == effect) {
                return true;
            }
        }
        return false;
    }

    //-----------------------------------------------------------------------------
    bool AudioDeviceWasapi::CheckBuiltInRenderCapability(winrt::Windows::Media::Effects::AudioEffectType effect) const {
        winrt::Windows::Media::Effects::AudioRenderEffectsManager effManager = nullptr;

        winrt::Windows::Media::Render::AudioRenderCategory category;
        category = winrt::Windows::Media::Render::AudioRenderCategory::Communications;

        winrt::hstring deviceId;

        if (!deviceIdStringOut_.empty()) {
            deviceId = deviceIdStringOut_;
        }
        else {
            deviceId = renderDevice_.Id();
        }

        try {
            effManager = winrt::Windows::Media::Effects::AudioEffectsManager::CreateAudioRenderEffectsManager(
                deviceId, category, winrt::Windows::Media::AudioProcessing::Default);
        }
        catch (winrt::hresult_error const& ex) {
            RTC_LOG(LS_ERROR) << "Failed to create audio render effects manager ("
                << rtc::ToUtf8(ex.message().c_str()).c_str() << ")";
            return false;
        }

        winrt::Windows::Foundation::Collections::IVectorView<winrt::Windows::Media::Effects::AudioEffect> effectsList;

        effectsList = effManager.GetAudioRenderEffects();

        unsigned int i;
        for (i = 0; i < effectsList.Size(); ++i) {
            if (effectsList.GetAt(i).AudioEffectType() == effect) {
                return true;
            }
        }
        return false;
    }

    //-----------------------------------------------------------------------------
    int AudioDeviceWasapi::SetBoolProperty(IPropertyStore* ptrPS,
        REFPROPERTYKEY key,
        VARIANT_BOOL value) {
        PROPVARIANT pv;
        PropVariantInit(&pv);
        pv.vt = VT_BOOL;
        pv.boolVal = value;
        HRESULT hr = ptrPS->SetValue(key, pv);
        PropVariantClear(&pv);
        if (FAILED(hr)) {
            TraceCOMError(hr);
            return -1;
        }
        return 0;
    }

    //-----------------------------------------------------------------------------
    int AudioDeviceWasapi::SetVtI4Property(IPropertyStore* ptrPS,
        REFPROPERTYKEY key,
        LONG value) {
        PROPVARIANT pv;
        PropVariantInit(&pv);
        pv.vt = VT_I4;
        pv.lVal = value;
        HRESULT hr = ptrPS->SetValue(key, pv);
        PropVariantClear(&pv);
        if (FAILED(hr)) {
            TraceCOMError(hr);
            return -1;
        }
        return 0;
    }

    // ----------------------------------------------------------------------------
    //  RefreshDeviceList
    //
    //  Creates a new list of endpoint rendering or capture devices after
    //  deleting any previously created (and possibly out-of-date) list of
    //  such devices.
    // ----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::RefreshDeviceList(DeviceClass cls) {
        WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_, "%s", __FUNCTION__);

        try {
            Concurrency::create_task([this, cls]() {
                return DeviceInformation::FindAllAsync(cls).get().as<winrt::Windows::Foundation::Collections::
                    IVectorView<winrt::Windows::Devices::Enumeration::DeviceInformation> >();
            }).then([this](winrt::Windows::Foundation::Collections::
                IVectorView<winrt::Windows::Devices::Enumeration::DeviceInformation> interfaces) {
                ptrCollection_ = interfaces.as<winrt::Windows::Devices::Enumeration::DeviceInformationCollection>();
            }).wait();
        }
        catch (winrt::hresult_invalid_argument) {
            // The InvalidArgumentException gets thrown by FindAllAsync when the GUID
            // isn't formatted properly. The only reason we're catching it here is
            // because the user is allowed to enter GUIDs without validation In normal
            // usage of the API, this exception handling probably wouldn't be necessary
            // when using known-good GUIDs
        }

        if (cls == DeviceClass::AudioCapture) {
            ptrCaptureCollection_ = ptrCollection_;
        }
        else if (cls == DeviceClass::AudioRender) {
            ptrRenderCollection_ = ptrCollection_;
        }
        else {
            return -1;
        }

        return 0;
    }

    // ----------------------------------------------------------------------------
    //  DeviceListCount
    //
    //  Gets a count of the endpoint rendering or capture devices in the
    //  current list of such devices.
    // ----------------------------------------------------------------------------
    int16_t AudioDeviceWasapi::DeviceListCount(DeviceClass cls) {
        WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_, "%s", __FUNCTION__);

        UINT count = 0;

        if (cls == DeviceClass::AudioCapture) {
            count = ptrCaptureCollection_.Size();
        }
        else if (cls == DeviceClass::AudioRender) {
            count = ptrRenderCollection_.Size();
        }
        else {
            return -1;
        }

        return static_cast<int16_t> (count);
    }

    // ----------------------------------------------------------------------------
    //  GetListDeviceName
    //
    //  Gets the friendly name of an endpoint rendering or capture device
    //  from the current list of such devices. The caller uses an index
    //  into the list to identify the device.
    //
    //  Uses: ptrRenderCollection_ or ptrCaptureCollection_ which is updated
    //  in RefreshDeviceList().
    // ----------------------------------------------------------------------------
    winrt::hstring AudioDeviceWasapi::GetListDeviceName(
        DeviceClass cls, int index) {
        WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_, "%s", __FUNCTION__);

        if (cls == DeviceClass::AudioRender) {
            return ptrRenderCollection_.GetAt(index).Name();
        }
        else if (cls == DeviceClass::AudioCapture) {
            return ptrCaptureCollection_.GetAt(index).Name();
        }

        return winrt::hstring();
    }

    // ----------------------------------------------------------------------------
    //  GetDefaultDeviceName
    //
    //  Gets the friendly name of an endpoint rendering or capture device
    //  given a specified device role.
    //
    //  Uses: _ptrEnumerator
    // ----------------------------------------------------------------------------
    winrt::hstring AudioDeviceWasapi::GetDefaultDeviceName(
        DeviceClass cls) {
        WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_, "%s", __FUNCTION__);

        if (cls == DeviceClass::AudioRender) {
            DeviceInformation defaultDevice = GetDefaultDevice(
                DeviceClass::AudioRender, AudioDeviceRole::Default);
            return defaultDevice != nullptr ? defaultDevice.Name() : winrt::hstring();
        }
        else if (cls == DeviceClass::AudioCapture) {
            DeviceInformation defaultDevice = GetDefaultDevice(
                DeviceClass::AudioCapture, AudioDeviceRole::Default);
            return defaultDevice != nullptr ? defaultDevice.Name() : winrt::hstring();
        }

        return winrt::hstring();
    }

    // ----------------------------------------------------------------------------
    //  GetListDeviceID
    //
    //  Gets the unique ID string of an endpoint rendering or capture device
    //  from the current list of such devices. The caller uses an index
    //  into the list to identify the device.
    //
    //  Uses: ptrRenderCollection_ or ptrCaptureCollection_ which is updated
    //  in RefreshDeviceList().
    // ----------------------------------------------------------------------------
    winrt::hstring AudioDeviceWasapi::GetListDeviceID(DeviceClass cls,
        int index) {
        WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_, "%s", __FUNCTION__);

        if (cls == DeviceClass::AudioRender) {
            return ptrRenderCollection_.GetAt(index).Id();
        }
        else if (cls == DeviceClass::AudioCapture) {
            return ptrCaptureCollection_.GetAt(index).Id();
        }

        return winrt::hstring();
    }

    // ----------------------------------------------------------------------------
    //  GetDefaultDeviceID
    //
    //  Gets the uniqe device ID of an endpoint rendering or capture device
    //  given a specified device role.
    //
    //  Uses: _ptrEnumerator
    // ----------------------------------------------------------------------------
    winrt::hstring AudioDeviceWasapi::GetDefaultDeviceID(
        DeviceClass cls) {
        WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_, "%s", __FUNCTION__);

        if (cls == DeviceClass::AudioRender) {
            DeviceInformation defaultDevice = GetDefaultDevice(
                DeviceClass::AudioRender, AudioDeviceRole::Default);
            return defaultDevice != nullptr ? defaultDevice.Id() : winrt::hstring();
        }
        else if (cls == DeviceClass::AudioCapture) {
            DeviceInformation defaultDevice = GetDefaultDevice(
                DeviceClass::AudioCapture, AudioDeviceRole::Default);
            return defaultDevice != nullptr ? defaultDevice.Id() : winrt::hstring();
        }

        return winrt::hstring();
    }

    // ----------------------------------------------------------------------------
    //  GetDeviceName
    // ----------------------------------------------------------------------------
    winrt::hstring AudioDeviceWasapi::GetDeviceName(
        DeviceInformation const& device) {
        WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_, "%s", __FUNCTION__);

        return device.Name();
    }

    // ----------------------------------------------------------------------------
    //  GetDeviceID
    // ----------------------------------------------------------------------------
    winrt::hstring AudioDeviceWasapi::GetDeviceID(
        DeviceInformation const& device) {
        WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_, "%s", __FUNCTION__);

        return device.Id();
    }

    // ----------------------------------------------------------------------------
    //  GetDefaultDevice
    // ----------------------------------------------------------------------------
    DeviceInformation AudioDeviceWasapi::GetDefaultDevice(
        DeviceClass cls, AudioDeviceRole role) {
        WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_, "%s", __FUNCTION__);
        if (cls == DeviceClass::AudioRender && playoutEnabled_) {
            DeviceInformation defaultRenderDevice = nullptr;
            Concurrency::create_task([this, role]() {
                return winrt::Windows::Devices::Enumeration::DeviceInformation::CreateFromIdAsync(
                    MediaDevice::GetDefaultAudioRenderId(role)).get().as<winrt::Windows::Devices::Enumeration::IDeviceInformation>();
            }).then(
                [this, &defaultRenderDevice](winrt::Windows::Devices::Enumeration::IDeviceInformation
                    const& deviceInformation) {
                defaultRenderDevice = deviceInformation.as<winrt::Windows::Devices::Enumeration::DeviceInformation>();
            }).wait();
            return defaultRenderDevice;
        }
        else if (cls == DeviceClass::AudioCapture && recordingEnabled_) {
            DeviceInformation defaultCaptureDevice = nullptr;
            Concurrency::create_task([this, role]() {
                return winrt::Windows::Devices::Enumeration::DeviceInformation::CreateFromIdAsync(
                    MediaDevice::GetDefaultAudioCaptureId(role)).get().as<winrt::Windows::Devices::Enumeration::IDeviceInformation>();
            }).then(
                [this, &defaultCaptureDevice](winrt::Windows::Devices::Enumeration::IDeviceInformation
                    const& deviceInformation) {
                defaultCaptureDevice = deviceInformation.as<winrt::Windows::Devices::Enumeration::DeviceInformation>();;
            }).wait();
            return defaultCaptureDevice;
        }
        return nullptr;
    }

    // ----------------------------------------------------------------------------
    //  GetListDevice
    // ----------------------------------------------------------------------------
    DeviceInformation AudioDeviceWasapi::GetListDevice(DeviceClass cls,
        int index) {
        if (cls == DeviceClass::AudioRender) {
            return ptrRenderCollection_.GetAt(index);
        }
        else if (cls == DeviceClass::AudioCapture) {
            return ptrCaptureCollection_.GetAt(index);
        }

        return nullptr;
    }

    // ----------------------------------------------------------------------------
    //  GetListDevice
    // ----------------------------------------------------------------------------
    DeviceInformation AudioDeviceWasapi::GetListDevice(DeviceClass cls,
        winrt::hstring const& deviceId) {
        if (deviceId.empty()) {
            return nullptr;
        }

        if (cls == DeviceClass::AudioRender) {
            for (unsigned int i = 0; i < ptrRenderCollection_.Size(); ++i) {
                DeviceInformation device = ptrRenderCollection_.GetAt(i);
                if (device.Id() == deviceId) {
                    return device;
                }
            }
        }
        else if (cls == DeviceClass::AudioCapture) {
            for (unsigned int i = 0; i < ptrCaptureCollection_.Size(); ++i) {
                DeviceInformation device = ptrCaptureCollection_.GetAt(i);
                if (device.Id() == deviceId) {
                    return device;
                }
            }
        }

        return nullptr;
    }

    // ----------------------------------------------------------------------------
    //  EnumerateEndpointDevicesAll
    // ----------------------------------------------------------------------------
    int32_t AudioDeviceWasapi::EnumerateEndpointDevicesAll() {
        WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_, "%s", __FUNCTION__);

        HRESULT hr = S_OK;

        // Generate a collection of audio endpoint devices in the system.
        // Get states for *AudioCapture* endpoint devices.
        try {
            Concurrency::create_task([this]() {
                return DeviceInformation::FindAllAsync(DeviceClass::AudioCapture).get().
                    as<winrt::Windows::Foundation::Collections::IVectorView<winrt::Windows::Devices::Enumeration::DeviceInformation> >();

            }).then(
                [this](concurrency::task<winrt::Windows::Foundation::Collections::
                    IVectorView<winrt::Windows::Devices::Enumeration::DeviceInformation> > getDevicesTask) {
                try {
                    ptrCaptureCollection_ = getDevicesTask.get().as<winrt::Windows::Devices::Enumeration::DeviceInformationCollection>();
                }
                catch (winrt::hresult_error const& ex) {
                    WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                        "Failed to enumerate audio capture devices, ex=%s", rtc::ToUtf8(ex.message().c_str()).c_str());
                }
            }).wait();
        }
        catch (winrt::hresult_invalid_argument) {
            // The InvalidArgumentException gets thrown by FindAllAsync when the GUID
            // isn't formatted properly. The only reason we're catching it here is
            // because the user is allowed to enter GUIDs without validation. In normal
            // usage of the API, this exception handling probably wouldn't be necessary
            // when using known-good GUIDs
        }

        // Generate a collection of audio endpoint devices in the system.
        // Get states for *AudioRender* endpoint devices.
        try {
            Concurrency::create_task([this]() {
                return DeviceInformation::FindAllAsync(DeviceClass::AudioRender).get().as<winrt::Windows::Foundation::Collections::
                    IVectorView<winrt::Windows::Devices::Enumeration::DeviceInformation> >();
            }).then(
                [this](concurrency::task<winrt::Windows::Foundation::Collections::
                    IVectorView<winrt::Windows::Devices::Enumeration::DeviceInformation> > getDevicesTask) {
                try {
                    ptrRenderCollection_ = getDevicesTask.get().as<winrt::Windows::Devices::Enumeration::
                        DeviceInformationCollection>();
                }
                catch (winrt::hresult_error const& ex) {
                    WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
                        "Failed to enumerate audio render devices, ex=%s", rtc::ToUtf8(ex.message().c_str()).c_str());
                }
            }).wait();
        }
        catch (winrt::hresult_invalid_argument) {
            // The InvalidArgumentException gets thrown by FindAllAsync when the GUID
            // isn't formatted properly. The only reason we're catching it here is
            // because the user is allowed to enter GUIDs without validation. In normal
            // usage of the API, this exception handling probably wouldn't be necessary
            // when using known-good GUIDs
        }

        EXIT_ON_ERROR(hr);
        return 0;

    Exit:
        TraceCOMError(hr);
        return -1;
    }

    //
    //  InitializeAudioDeviceIn()
    //
    //  Activates the default audio capture.
    //
    //-----------------------------------------------------------------------------
    void AudioDeviceWasapi::InitializeAudioDeviceIn(winrt::hstring const& deviceId) {
        try {
            AudioInterfaceActivator::SetAudioDevice(this);
            winrt::hresult_error ex;
            winrt::com_ptr<AudioInterfaceActivator> pActivator = winrt::make_self<AudioInterfaceActivator>();
            winrt::com_ptr<IActivateAudioInterfaceAsyncOperation> pAsyncOp;
            AudioInterfaceActivator::ActivateAudioClientAsync(
                deviceId.c_str(),
                AudioInterfaceActivator::ActivatorDeviceType::eInputDevice, pActivator, pAsyncOp).get();
        }
        catch (winrt::hresult_error const& e) {
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "failed to activate input audio device id=%s ex=%s",
                rtc::ToUtf8(deviceId.c_str()).c_str(),
                rtc::ToUtf8(e.message().c_str()).c_str());
            throw e;
        }
    }

    //
    //  InitializeAudioDeviceOut()
    //
    //  Activates the default audio render.
    //
    //-----------------------------------------------------------------------------
    void AudioDeviceWasapi::InitializeAudioDeviceOut(winrt::hstring const& deviceId) {
        try {
            AudioInterfaceActivator::SetAudioDevice(this);
            winrt::hresult_error ex;
            winrt::com_ptr<AudioInterfaceActivator> pActivator = winrt::make_self<AudioInterfaceActivator>();
            winrt::com_ptr<IActivateAudioInterfaceAsyncOperation> pAsyncOp;
            AudioInterfaceActivator::ActivateAudioClientAsync(
                deviceId.c_str(),
                AudioInterfaceActivator::ActivatorDeviceType::eOutputDevice, pActivator, pAsyncOp).get();
        }
        catch (winrt::hresult_error const& e) {
            WEBRTC_TRACE(kTraceInfo, kTraceAudioDevice, id_,
                "failed to activate output audio device id=%s ex=%s",
                rtc::ToUtf8(deviceId.c_str()).c_str(),
                rtc::ToUtf8(e.message().c_str()).c_str());
            throw e;
        }
    }

    // ----------------------------------------------------------------------------
    //  ShouldUpmix
    // ----------------------------------------------------------------------------
    bool AudioDeviceWasapi::ShouldUpmix() {
        return enableUpmix_;
    }

    // ----------------------------------------------------------------------------
    //  GenerateMixFormatForMediaEngine
    // ----------------------------------------------------------------------------
    WAVEFORMATEX* AudioDeviceWasapi::GenerateMixFormatForMediaEngine(
        WAVEFORMATEX* actualMixFormat) {
        WAVEFORMATEX* Wfx = new WAVEFORMATEX();

        bool isStereo = false;
        StereoPlayoutIsAvailable(&isStereo);

        // Set wave format
        Wfx->wFormatTag = WAVE_FORMAT_PCM;
        Wfx->wBitsPerSample = 16;
        Wfx->cbSize = 0;

        Wfx->nChannels = isStereo ? 2 : 1;
        Wfx->nSamplesPerSec = actualMixFormat->nSamplesPerSec;
        Wfx->nBlockAlign = Wfx->nChannels * Wfx->wBitsPerSample / 8;
        Wfx->nAvgBytesPerSec = Wfx->nSamplesPerSec * Wfx->nBlockAlign;

        return Wfx;
    }

    // ----------------------------------------------------------------------------
    //  GeneratePCMMixFormat
    //  Main principles used from
    //  https://msdn.microsoft.com/en-us/library/windows/hardware/dn653308%28v=vs.85%29.aspx
    // ----------------------------------------------------------------------------
    WAVEFORMATPCMEX* AudioDeviceWasapi::GeneratePCMMixFormat(
        WAVEFORMATEX* actualMixFormat) {
        WAVEFORMATPCMEX* waveFormatPCMEx = new WAVEFORMATPCMEX();

        waveFormatPCMEx->Format.wFormatTag = WAVE_FORMAT_EXTENSIBLE;
        waveFormatPCMEx->Format.nChannels = actualMixFormat->nChannels;
        waveFormatPCMEx->Format.wBitsPerSample = actualMixFormat->wBitsPerSample;
        waveFormatPCMEx->Format.nSamplesPerSec = actualMixFormat->nSamplesPerSec;

        waveFormatPCMEx->Format.nBlockAlign = waveFormatPCMEx->Format.nChannels *
            waveFormatPCMEx->Format.wBitsPerSample / 8;  /* Same as the usual */

        waveFormatPCMEx->Format.nAvgBytesPerSec =
            waveFormatPCMEx->Format.nSamplesPerSec * waveFormatPCMEx->Format.nBlockAlign;

        waveFormatPCMEx->Format.cbSize = 22;  /* After this to GUID */
        waveFormatPCMEx->Samples.wValidBitsPerSample =
            waveFormatPCMEx->Format.wBitsPerSample;  /* All bits have data */

        switch (waveFormatPCMEx->Format.nChannels) {
        case 1:
            waveFormatPCMEx->dwChannelMask = KSAUDIO_SPEAKER_MONO;
            break;
        case 2:
            waveFormatPCMEx->dwChannelMask = KSAUDIO_SPEAKER_STEREO;
            break;
        case 4:
            waveFormatPCMEx->dwChannelMask = KSAUDIO_SPEAKER_QUAD;
            break;
        case 6:
            waveFormatPCMEx->dwChannelMask = KSAUDIO_SPEAKER_5POINT1;
            break;
        case 8:
            waveFormatPCMEx->dwChannelMask = KSAUDIO_SPEAKER_7POINT1;
            break;
        default:
            waveFormatPCMEx->dwChannelMask = KSAUDIO_SPEAKER_STEREO;
            break;
        }

        if (waveFormatPCMEx->Format.wBitsPerSample == 16) {
            waveFormatPCMEx->SubFormat = KSDATAFORMAT_SUBTYPE_PCM;  // Specify PCM
        }
        else if (waveFormatPCMEx->Format.wBitsPerSample == 32) {
            waveFormatPCMEx->SubFormat = KSDATAFORMAT_SUBTYPE_IEEE_FLOAT;  // Specify FLOAT
        }
        else {
            WEBRTC_TRACE(kTraceCritical, kTraceAudioDevice, id_,
                "wrong value for number of bits per sample");
            return NULL;
        }

        return waveFormatPCMEx;
    }

    // ----------------------------------------------------------------------------
    //  Upmix
    //  Reference upmixer application found on
    //  https://hg.mozilla.org/releases/mozilla-aurora/file/tip/media/libcubeb/src/cubeb_wasapi.cpp
    // ----------------------------------------------------------------------------
    void AudioDeviceWasapi::Upmix(
        int16_t* inSamples,
        uint32_t numberOfFrames,
        int16_t* outSamplesReal,
        uint32_t inChannels,
        uint32_t outChannels) {
        // Create temporary array to do the upmix
        std::unique_ptr<int16_t> outSamples(new int16_t[numberOfFrames * outChannels]);

        // Copy over input channels
        for (uint32_t i = 0, o = 0; i < numberOfFrames * inChannels;
            i += inChannels, o += outChannels) {
            if (inChannels <= outChannels) {
                for (uint32_t j = 0; j < inChannels; ++j) {
                    outSamples.get()[o + j] = inSamples[i + j];
                }
            }
            else {
                for (uint32_t j = 0; j < outChannels; ++j) {
                    outSamples.get()[o + j] = (inSamples[i + j] + inSamples[i + j + 1]) / 2;
                }
            }
        }

        if (inChannels < outChannels) {
            // Add 0 to other channels
            for (uint32_t i = 0, o = 0; i < numberOfFrames; ++i, o += outChannels) {
                for (uint32_t j = inChannels; j < outChannels; ++j) {
                    outSamples.get()[o + j] = 0;
                }
            }
        }

        // Copy over memory to be delivered to the IAudioRenderClient
        memcpy(outSamplesReal, outSamples.get(),
            playBlockSize_ * outChannels * sizeof(int16_t));
    }

    //-----------------------------------------------------------------------------
    void AudioDeviceWasapi::UpmixAndConvert(
        int16_t* inSamples,
        uint32_t numberOfFrames,
        float* outSamplesReal,
        uint32_t inChannels,
        uint32_t outChannels) {
        // Create temporary array to do the upmix
        std::unique_ptr<float> outSamples(new float[numberOfFrames * outChannels]);

        // Copy over input channels
        for (uint32_t i = 0, o = 0; i < numberOfFrames * inChannels;
            i += inChannels, o += outChannels) {
            if (inChannels <= outChannels) {
                for (uint32_t j = 0; j < inChannels; ++j) {
                    outSamples.get()[o + j] = (float)inSamples[i + j] / (float)INT16_MAX;
                }
            }
            else {
                for (uint32_t j = 0; j < outChannels; ++j) {
                    outSamples.get()[o + j] = ((float)inSamples[i + j] + (float)inSamples[i + j + 1]) / (float)INT16_MAX / 2.0F;
                }
            }
        }

        if (inChannels < outChannels) {
            // Add 0 to other channels
            for (uint32_t i = 0, o = 0; i < numberOfFrames; ++i, o += outChannels) {
                for (uint32_t j = inChannels; j < outChannels; ++j) {
                    outSamples.get()[o + j] = 0.0f;
                }
            }
        }

        // Copy over memory to be delivered to the IAudioRenderClient
        memcpy(outSamplesReal, outSamples.get(),
            playBlockSize_ * outChannels * sizeof(float));
    }


    // ----------------------------------------------------------------------------
    //  TraceCOMError
    // ----------------------------------------------------------------------------
    void AudioDeviceWasapi::TraceCOMError(HRESULT hr) const {
        TCHAR errorText[MAXERRORLENGTH];

        const DWORD dwFlags = FORMAT_MESSAGE_FROM_SYSTEM |
            FORMAT_MESSAGE_IGNORE_INSERTS;
        const DWORD dwLangID = MAKELANGID(LANG_ENGLISH, SUBLANG_ENGLISH_US);

        // Gets the system's human readable message string for this HRESULT.
        // All error message in English by default.
        DWORD messageLength = ::FormatMessageW(dwFlags,
            0,
            hr,
            dwLangID,
            errorText,
            MAXERRORLENGTH,
            NULL);

        assert(messageLength <= MAXERRORLENGTH);

        // Trims tailing white space (FormatMessage() leaves a trailing cr-lf.).
        for (; messageLength && ::isspace(errorText[messageLength - 1]);
            --messageLength) {
            errorText[messageLength - 1] = '\0';
        }

        WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_,
            "Core Audio method failed (hr=0x%08X)", hr);
        if (messageLength) {
            WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_, "Error details (length=%d): %s", messageLength, rtc::ToUtf8(errorText, messageLength).c_str());
        }
        else {
            WEBRTC_TRACE(kTraceError, kTraceAudioDevice, id_, "Error details (length=%d): N/A", messageLength);
        }
    }


    // ----------------------------------------------------------------------------
    //  SetThreadName
    // ----------------------------------------------------------------------------
    void AudioDeviceWasapi::SetThreadName(DWORD dwThreadID,
        LPCSTR szThreadName) {
        // See http://msdn.microsoft.com/en-us/library/xcb2z8hs(VS.71).aspx for
        // details on the code in this function. Name of article is "Setting a Thread
        // Name (Unmanaged)".

        THREADNAME_INFO info;
        info.dwType = 0x1000;
        info.szName = szThreadName;
        info.dwThreadID = dwThreadID;
        info.dwFlags = 0;
    }

    // ----------------------------------------------------------------------------
    //  Get44kHzDrift
    // ----------------------------------------------------------------------------
    void AudioDeviceWasapi::Get44kHzDrift() {
        // We aren't able to resample at 44.1 kHz. Instead we run at 44 kHz and
        // push/pull from the engine faster to compensate. If only one direction is
        // set to 44.1 kHz the result is indistinguishable from clock drift to the
        // AEC. We can compensate internally if we inform the AEC about the drift.
        sampleDriftAt48kHz_ = 0;
        driftAccumulator_ = 0;

        if (playSampleRate_ == 44000 && recSampleRate_ != 44000) {
            sampleDriftAt48kHz_ = 480.0f / 440;
        }
        else if (playSampleRate_ != 44000 && recSampleRate_ == 44000) {
            sampleDriftAt48kHz_ = -480.0f / 441;
        }
    }

    //-----------------------------------------------------------------------------
    bool AudioDeviceWasapi::KeyPressed() const {
        int key_down = 0;
        return (key_down > 0);
    }

    //-----------------------------------------------------------------------------
    void AudioDeviceWasapi::DefaultAudioCaptureDeviceChanged(
        DefaultAudioCaptureDeviceChangedEventArgs const& args) {
        //if (usingInputDeviceIndex_) {
        //    // Not using default audio input device
        //    return;
        //}
        if (!Recording()) {
            return;
        }
        if (args && inputDeviceRole_ != args.Role()) {
            return;
        }
        RTC_LOG(LS_INFO) << "Default audio capture device changed, restarting capturer!";
        SetEvent(hRestartCaptureEvent_);
    }

    //-----------------------------------------------------------------------------
    void AudioDeviceWasapi::DefaultAudioRenderDeviceChanged(
        DefaultAudioRenderDeviceChangedEventArgs const& args) {
        //if (usingOutputDeviceIndex_) {
        //    // Not using default audio output device
        //    return;
        //}
        if (!Playing()) {
            return;
        }
        if (args && outputDeviceRole_ != args.Role()) {
            return;
        }
        RTC_LOG(LS_INFO) << "Default audio render device changed, restarting renderer!";
        SetEvent(hRestartRenderEvent_);
    }

    //-----------------------------------------------------------------------------
    rtc::scoped_refptr<webrtc::AudioDeviceModule> IAudioDeviceWasapi::create(const CreationProperties& info) noexcept
    {
        return AudioDeviceWasapi::create(info);
    }
} // namespace webrtc

#endif //CPPWINRT_VERSION