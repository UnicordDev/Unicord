#pragma once

#include <unknwn.h>

#include "IAudioDeviceWasapi.h"

#ifdef CPPWINRT_VERSION

#include <rtc_base/criticalsection.h>
#include <rtc_base/scoped_ref_ptr.h>
#include <system_wrappers/include/event_wrapper.h>
#include <modules/audio_device/audio_device_buffer.h>
#include <zsLib/types.h>
#include <zsLib/Proxy.h>
#include <zsLib/ProxySubscriptions.h>

#include <atomic>
#include <mfidl.h>

#include <functional>
#include <vector>
#include <queue>

#if (_MSC_VER >= 1400)  // only include for VS 2005 and higher

#include <wmcodecdsp.h>      // CLSID_CWMAudioAEC
// (must be before audioclient.h)
#include <Audioclient.h>     // WASAPI
#include <Audiopolicy.h>
#include <wrl\implements.h>
#include <Mmdeviceapi.h>     // MMDevice
#include <avrt.h>            // Avrt
#include <endpointvolume.h>
#include <mediaobj.h>        // IMediaObject
#include <mfapi.h>
#include <ppltasks.h>

using winrt::Windows::Devices::Enumeration::DeviceClass;
using winrt::Windows::Devices::Enumeration::DeviceInformation;
using winrt::Windows::Devices::Enumeration::DeviceInformationCollection;
using winrt::Windows::Media::Devices::DefaultAudioRenderDeviceChangedEventArgs;
using winrt::Windows::Media::Devices::DefaultAudioCaptureDeviceChangedEventArgs;
using winrt::Windows::Media::Devices::AudioDeviceRole;
using winrt::Windows::Media::Devices::MediaDevice;

// Use Multimedia Class Scheduler Service (MMCSS) to boost the thread priority
#pragma comment(lib, "avrt.lib")
#define WEBRTC_TRACE {}
enum TraceModule {
    kTraceUndefined = 0,
    // not a module, triggered from the engine code
    kTraceVoice = 0x0001,
    // not a module, triggered from the engine code
    kTraceVideo = 0x0002,
    // not a module, triggered from the utility code
    kTraceUtility = 0x0003,
    kTraceRtpRtcp = 0x0004,
    kTraceTransport = 0x0005,
    kTraceSrtp = 0x0006,
    kTraceAudioCoding = 0x0007,
    kTraceAudioMixerServer = 0x0008,
    kTraceAudioMixerClient = 0x0009,
    kTraceFile = 0x000a,
    kTraceAudioProcessing = 0x000b,
    kTraceVideoCoding = 0x0010,
    kTraceVideoMixer = 0x0011,
    kTraceAudioDevice = 0x0012,
    kTraceVideoRenderer = 0x0014,
    kTraceVideoCapture = 0x0015,
    kTraceRemoteBitrateEstimator = 0x0017,
};

enum TraceLevel {
    kTraceNone = 0x0000,  // no trace
    kTraceStateInfo = 0x0001,
    kTraceWarning = 0x0002,
    kTraceError = 0x0004,
    kTraceCritical = 0x0008,
    kTraceApiCall = 0x0010,
    kTraceDefault = 0x00ff,

    kTraceModuleCall = 0x0020,
    kTraceMemory = 0x0100,  // memory info
    kTraceTimer = 0x0200,   // timing info
    kTraceStream = 0x0400,  // "continuous" stream of data

    // used for debug purposes
    kTraceDebug = 0x0800,  // debug
    kTraceInfo = 0x1000,   // debug info

    // Non-verbose level used by LS_INFO of logging.h. Do not use directly.
    kTraceTerseInfo = 0x2000,

    kTraceAll = 0xffff
};

namespace webrtc {

    const float MAX_CORE_SPEAKER_VOLUME = 255.0f;
    const float MIN_CORE_SPEAKER_VOLUME = 0.0f;
    const float MAX_CORE_MICROPHONE_VOLUME = 255.0f;
    const float MIN_CORE_MICROPHONE_VOLUME = 0.0f;
    const uint16_t CORE_SPEAKER_VOLUME_STEP_SIZE = 1;
    const uint16_t CORE_MICROPHONE_VOLUME_STEP_SIZE = 1;

    class AudioDeviceWasapi;
    class DefaultAudioDeviceWatcher;

    // Utility class which initializes COM in the constructor (STA or MTA),
    // and uninitializes COM in the destructor.
    class ScopedCOMInitializer {
    public:
        // Enum value provided to initialize the thread as an MTA instead of STA.
        enum SelectMTA { kMTA };

        // Constructor for STA initialization.
        ScopedCOMInitializer() {
            Initialize(COINIT_APARTMENTTHREADED);
        }

        // Constructor for MTA initialization.
        explicit ScopedCOMInitializer(SelectMTA /* mta */) {
            Initialize(COINIT_MULTITHREADED);
        }

        ~ScopedCOMInitializer() {
            if (SUCCEEDED(hr_))
                CoUninitialize();
        }

        bool succeeded() const { return SUCCEEDED(hr_); }

    private:
        void Initialize(COINIT init) {
            hr_ = CoInitializeEx(NULL, init);
        }

        HRESULT hr_;

        ScopedCOMInitializer(const ScopedCOMInitializer&);
        void operator=(const ScopedCOMInitializer&);
    };

    class AudioInterfaceActivator : public winrt::implements<AudioInterfaceActivator,
        IActivateAudioInterfaceCompletionHandler> {
    public:
        enum ActivatorDeviceType {
            eNone = 0,
            eInputDevice,
            eOutputDevice
        };

        static ActivatorDeviceType m_DeviceType;
        static AudioDeviceWasapi* m_AudioDevice;
        static HANDLE m_activateCompletedHandle;
        static HRESULT m_activateResult;

    public:
        STDMETHODIMP ActivateCompleted(
            IActivateAudioInterfaceAsyncOperation* pAsyncOp);
        static winrt::Windows::Foundation::IAsyncAction
            ActivateAudioClientAsync(LPCWCHAR deviceId,
                ActivatorDeviceType deviceType, winrt::com_ptr<AudioInterfaceActivator> pActivator,
                winrt::com_ptr<IActivateAudioInterfaceAsyncOperation> pAsyncOp);
        static void SetAudioDevice(AudioDeviceWasapi* device);

    private:
        concurrency::task_completion_event<HRESULT> m_ActivateCompleted;
    };

    class AudioDeviceWasapi : public IAudioDeviceWasapi, webrtc::AudioDeviceModule {

    private:
        struct make_private {};

        void init(const CreationProperties& props) noexcept;

    public:
        AudioDeviceWasapi(const make_private&);

    protected:
        ~AudioDeviceWasapi();

    public:
        static rtc::scoped_refptr<AudioDeviceModule> create(const CreationProperties& info) noexcept;

        IAudioDeviceWasapiSubscriptionPtr subscribe(IAudioDeviceWasapiDelegatePtr delegate) override;

        std::string id() const noexcept override { return id_; }

        friend class AudioInterfaceActivator;
        friend class DefaultAudioDeviceWatcher;

        // Retrieve the currently utilized audio layer
        virtual int32_t ActiveAudioLayer(AudioLayer* audioLayer) const override;

        // Full-duplex transportation of PCM audio
        virtual int32_t RegisterAudioCallback(webrtc::AudioTransport* audioCallback) override;

        // Main initializaton and termination
        virtual int32_t Init() override;
        virtual int32_t Terminate() override;
        virtual bool Initialized() const override;

        // Device enumeration
        virtual int16_t PlayoutDevices() override;
        virtual int16_t RecordingDevices() override;
        virtual int32_t PlayoutDeviceName(
            uint16_t index,
            char name[webrtc::kAdmMaxDeviceNameSize],
            char guid[webrtc::kAdmMaxGuidSize]) override;
        virtual int32_t RecordingDeviceName(
            uint16_t index,
            char name[webrtc::kAdmMaxDeviceNameSize],
            char guid[webrtc::kAdmMaxGuidSize]) override;

        // Device selection
        virtual int32_t SetPlayoutDevice(uint16_t index) override;
        virtual int32_t SetPlayoutDevice(
            AudioDeviceModule::WindowsDeviceType device) override;
        virtual int32_t SetRecordingDevice(uint16_t index) override;
        virtual int32_t SetRecordingDevice(
            AudioDeviceModule::WindowsDeviceType device) override;

        // Audio transport initialization
        virtual int32_t PlayoutIsAvailable(bool* available) override;
        virtual int32_t InitPlayout() override;
        virtual bool PlayoutIsInitialized() const override;
        virtual int32_t RecordingIsAvailable(bool* available) override;
        virtual int32_t InitRecording() override;
        virtual bool RecordingIsInitialized() const override;

        // Audio transport control
        virtual int32_t StartPlayout() override;
        virtual int32_t StopPlayout() override;
        virtual bool Playing() const override;
        virtual int32_t StartRecording() override;
        virtual int32_t StopRecording() override;
        virtual bool Recording() const override;

        // Audio mixer initialization
        virtual int32_t InitSpeaker() override;
        virtual bool SpeakerIsInitialized() const override;
        virtual int32_t InitMicrophone() override;
        virtual bool MicrophoneIsInitialized() const override;

        // Speaker volume controls
        virtual int32_t SpeakerVolumeIsAvailable(bool* available) override;
        virtual int32_t SetSpeakerVolume(uint32_t volume) override;
        virtual int32_t SpeakerVolume(uint32_t* volume) const override;
        virtual int32_t MaxSpeakerVolume(uint32_t* maxVolume) const override;
        virtual int32_t MinSpeakerVolume(uint32_t* minVolume) const override;

        // Microphone volume controls
        virtual int32_t MicrophoneVolumeIsAvailable(bool* available) override;
        virtual int32_t SetMicrophoneVolume(uint32_t volume) override;
        virtual int32_t MicrophoneVolume(uint32_t* volume) const override;
        virtual int32_t MaxMicrophoneVolume(uint32_t* maxVolume) const override;
        virtual int32_t MinMicrophoneVolume(uint32_t* minVolume) const override;

        // Speaker mute control
        virtual int32_t SpeakerMuteIsAvailable(bool* available) override;
        virtual int32_t SetSpeakerMute(bool enable) override;
        virtual int32_t SpeakerMute(bool* enabled) const override;

        // Microphone mute control
        virtual int32_t MicrophoneMuteIsAvailable(bool* available) override;
        virtual int32_t SetMicrophoneMute(bool enable) override;
        virtual int32_t MicrophoneMute(bool* enabled) const override;

        // Stereo support
        virtual int32_t StereoPlayoutIsAvailable(bool* available) const override;
        virtual int32_t SetStereoPlayout(bool enable) override;
        virtual int32_t StereoPlayout(bool* enabled) const override;
        virtual int32_t StereoRecordingIsAvailable(bool* available) const override;
        virtual int32_t SetStereoRecording(bool enable) override;
        virtual int32_t StereoRecording(bool* enabled) const override;

        // Delay information and control
        virtual int32_t PlayoutDelay(uint16_t* delayMS) const override;

        // CPU load
        virtual int32_t CPULoad(uint16_t* load) const;

        virtual bool BuiltInAECIsAvailable() const override;
        virtual bool BuiltInAGCIsAvailable() const override;
        virtual bool BuiltInNSIsAvailable() const override;

        virtual int32_t EnableBuiltInAEC(bool enable) override;
        virtual int32_t EnableBuiltInAGC(bool enable) override;
        virtual int32_t EnableBuiltInNS(bool enable) override;

        virtual void SetPreferredDevices(winrt::hstring capture, winrt::hstring render) {
            this->preferredInputDeviceIdString_ = capture;
            this->preferredOutputDeviceIdString_ = render;
        }

    public:
        virtual bool PlayoutWarning() const;
        virtual bool PlayoutError() const;
        virtual bool RecordingWarning() const;
        virtual bool RecordingError() const;
        virtual void ClearPlayoutWarning();
        virtual void ClearPlayoutError();
        virtual void ClearRecordingWarning();
        virtual void ClearRecordingError();

        DeviceInformation GetListDevice(DeviceClass cls, int index);

    private:
        bool KeyPressed() const;

    private:  // thread functions
        DWORD InitCaptureThreadPriority();
        void RevertCaptureThreadPriority();
        static DWORD WINAPI WSAPICaptureThread(LPVOID context);
        DWORD DoCaptureThread();

        static DWORD WINAPI WSAPIRenderThread(LPVOID context);
        DWORD DoRenderThread();

        static DWORD WINAPI GetCaptureVolumeThread(LPVOID context);
        DWORD DoGetCaptureVolumeThread();

        static DWORD WINAPI SetCaptureVolumeThread(LPVOID context);
        DWORD DoSetCaptureVolumeThread();

        int32_t StartObserverThread();
        int32_t StopObserverThread();
        static DWORD WINAPI WSAPIObserverThread(LPVOID context);
        DWORD DoObserverThread();

        bool CheckBuiltInCaptureCapability(winrt::Windows::Media::Effects::AudioEffectType) const;
        bool CheckBuiltInRenderCapability(winrt::Windows::Media::Effects::AudioEffectType) const;

        void SetThreadName(DWORD dwThreadID, LPCSTR szThreadName);
        void Lock() { critSect_.Enter(); }
        void UnLock() { critSect_.Leave(); }

    private:
        int32_t InitRecordingInternal();
        int32_t StartRecordingInternal();
        int32_t StopRecordingInternal();

        int32_t InitPlayoutInternal();
        int32_t StartPlayoutInternal();
        int32_t StopPlayoutInternal();

        int SetBoolProperty(IPropertyStore* ptrPS,
            REFPROPERTYKEY key,
            VARIANT_BOOL value);

        int SetVtI4Property(IPropertyStore* ptrPS,
            REFPROPERTYKEY key,
            LONG value);

        int32_t EnumerateEndpointDevicesAll();
        void TraceCOMError(HRESULT hr) const;
        void Get44kHzDrift();

        int32_t RefreshDeviceList(DeviceClass cls);
        int16_t DeviceListCount(DeviceClass cls);

        winrt::hstring  GetDefaultDeviceName(DeviceClass cls);
        winrt::hstring  GetListDeviceName(DeviceClass cls, int index);
        winrt::hstring  GetDeviceName(DeviceInformation const& device);

        winrt::hstring  GetListDeviceID(DeviceClass cls, int index);
        winrt::hstring  GetDefaultDeviceID(DeviceClass cls);
        winrt::hstring  GetDeviceID(DeviceInformation const& device);

        DeviceInformation GetListDevice(DeviceClass cls, winrt::hstring const& deviceId);
        DeviceInformation GetDefaultDevice(DeviceClass cls, AudioDeviceRole role);

        void InitializeAudioDeviceIn(winrt::hstring const& deviceId);
        void InitializeAudioDeviceOut(winrt::hstring const& deviceId);

        // Surround system support
        bool ShouldUpmix();
        WAVEFORMATEX* GenerateMixFormatForMediaEngine(
            WAVEFORMATEX* actualMixFormat);
        WAVEFORMATPCMEX* GeneratePCMMixFormat(WAVEFORMATEX* actualMixFormat);
        void Upmix(int16_t* inSamples, uint32_t numberOfFrames,
            int16_t* outSamples, uint32_t inChannels, uint32_t outChannels);
        void UpmixAndConvert(int16_t* inSamples, uint32_t numberOfFrames,
            float* outSamples, uint32_t inChannels, uint32_t outChannels);

    public:
        void DefaultAudioCaptureDeviceChanged(
            DefaultAudioCaptureDeviceChangedEventArgs const& args);
        void DefaultAudioRenderDeviceChanged(
            DefaultAudioRenderDeviceChangedEventArgs const& args);

    private:
        AudioDeviceWasapiWeakPtr thisWeak_;
        mutable zsLib::RecursiveLock subscriptionLock_;

        IAudioDeviceWasapiDelegateSubscriptions subscriptions_;
        IAudioDeviceWasapiSubscriptionPtr defaultSubscription_;

        std::string id_;

    private:
        ScopedCOMInitializer                    comInit_;
        webrtc::AudioDeviceBuffer* ptrAudioBuffer_;
        rtc::CriticalSection                    critSect_;
        rtc::CriticalSection                    volumeMutex_;
        rtc::CriticalSection                    recordingControlMutex_;
        rtc::CriticalSection                    playoutControlMutex_;

    private:  // MMDevice
        winrt::hstring      deviceIdStringIn_;
        winrt::hstring      deviceIdStringOut_;
        DeviceInformation   captureDevice_;
        DeviceInformation   renderDevice_;

        WAVEFORMATPCMEX* mixFormatSurroundOut_;
        bool                   enableUpmix_;

        DeviceInformationCollection  ptrCaptureCollection_;
        DeviceInformationCollection  ptrRenderCollection_;
        DeviceInformationCollection  ptrCollection_;

    private:  // WASAPI

        uint16_t                                recChannelsPrioList_[2];
        uint16_t                                playChannelsPrioList_[2];

        IAudioClient* ptrClientOut_;
        IAudioClient* ptrClientIn_;
        IAudioRenderClient* ptrRenderClient_;
        IAudioCaptureClient* ptrCaptureClient_;
        ISimpleAudioVolume* ptrCaptureVolume_;
        ISimpleAudioVolume* ptrRenderSimpleVolume_;

        bool                                    builtInAecEnabled_;
        bool                                    builtInNSEnabled_;
        bool                                    builtInAGCEnabled_;

        HANDLE                                  hRenderSamplesReadyEvent_;
        HANDLE                                  hPlayThread_;
        HANDLE                                  hRenderStartedEvent_;
        HANDLE                                  hShutdownRenderEvent_;
        HANDLE                                  hRestartRenderEvent_;

        HANDLE                                  hCaptureSamplesReadyEvent_;
        HANDLE                                  hRecThread_;
        HANDLE                                  hCaptureStartedEvent_;
        HANDLE                                  hShutdownCaptureEvent_;
        HANDLE                                  hRestartCaptureEvent_;

        HANDLE                                  hObserverThread_;
        HANDLE                                  hObserverStartedEvent_;
        HANDLE                                  hObserverShutdownEvent_;

        HANDLE                                  hGetCaptureVolumeThread_;
        HANDLE                                  hSetCaptureVolumeThread_;
        HANDLE                                  hSetCaptureVolumeEvent_;

        HANDLE                                  hMmTask_;

        UINT                                    playAudioFrameSize_;
        uint32_t                                playSampleRate_;
        uint32_t                                devicePlaySampleRate_;
        uint32_t                                playBlockSize_;
        uint32_t                                devicePlayBlockSize_;
        uint32_t                                playChannels_;
        uint32_t                                sndCardPlayDelay_;

        float                                   sampleDriftAt48kHz_;
        float                                   driftAccumulator_;

        UINT64                                  writtenSamples_;
        LONGLONG                                playAcc_;

        UINT                                    recAudioFrameSize_;
        uint32_t                                recSampleRate_;
        uint32_t                                recBlockSize_;
        uint32_t                                recChannels_;
        UINT64                                  readSamples_;
        uint32_t                                sndCardRecDelay_;

        LARGE_INTEGER                           perfCounterFreq_;
        double                                  perfCounterFactor_;
        float                                   avgCPULoad_;

    private:
        bool                                    initialized_;
        bool                                    recording_;
        bool                                    playing_;
        bool                                    recIsInitialized_;
        bool                                    playIsInitialized_;
        bool                                    speakerIsInitialized_;
        bool                                    microphoneIsInitialized_;

        bool                                    usingInputDeviceIndex_;
        bool                                    usingOutputDeviceIndex_;
        winrt::hstring                          preferredInputDeviceIdString_;
        winrt::hstring                          preferredOutputDeviceIdString_;
        AudioDeviceRole                         outputDeviceRole_;
        AudioDeviceRole                         inputDeviceRole_;
        uint16_t                                inputDeviceIndex_;
        uint16_t                                outputDeviceIndex_;

        uint16_t                                playWarning_;
        uint16_t                                playError_;
        bool                                    playIsRecovering_;
        uint16_t                                recWarning_;
        uint16_t                                recError_;
        bool                                    recIsRecovering_;

        uint16_t                                playBufDelay_;
        uint16_t                                playBufDelayFixed_;

        uint16_t                                newMicLevel_;

        std::unique_ptr<DefaultAudioDeviceWatcher> defaultDeviceWatcher_;

        bool                                    recordingEnabled_;
        bool                                    playoutEnabled_;
    };

#endif    // #if (_MSC_VER >= 1400)

}  // namespace webrtc

#endif //CPPWINRT_VERSION