#pragma once
#include "ServiceBackgroundTask.g.h"
#include <winrt/Windows.ApplicationModel.AppService.h>
#include <winrt/Windows.ApplicationModel.Calls.Background.h>
#include <winrt/Windows.ApplicationModel.Calls.h>

namespace winrt::Unicord::Universal::Voice::Background::implementation {
    struct ServiceBackgroundTask : ServiceBackgroundTaskT<ServiceBackgroundTask> {
    private:
        Windows::ApplicationModel::Background::BackgroundTaskDeferral taskDeferral{ nullptr };
        Windows::ApplicationModel::AppService::AppServiceConnection appServiceConnection{ nullptr };
        bool appServiceConnected = false;

        static Windows::ApplicationModel::Calls::VoipCallCoordinator voipCoordinator;
        static Windows::ApplicationModel::Calls::VoipPhoneCall activeCall;
        static Unicord::Universal::Voice::VoiceClient voiceClient;
        static Unicord::Universal::Voice::VoiceClientOptions voiceClientOptions;

        void OnUdpPing(Windows::Foundation::IInspectable sender, uint32_t ping);
        void OnWsPing(Windows::Foundation::IInspectable sender, uint32_t ping);
        void RaiseEvent(Unicord::Universal::Voice::Background::VoiceServiceEvent ev, Windows::Foundation::Collections::ValueSet data);
        void OnServiceMessage(Windows::ApplicationModel::AppService::AppServiceConnection sender, Windows::ApplicationModel::AppService::AppServiceRequestReceivedEventArgs args);
        void OnServiceClosed(Windows::ApplicationModel::AppService::AppServiceConnection sender, Windows::ApplicationModel::AppService::AppServiceClosedEventArgs args);
        void OnCancelled(Windows::ApplicationModel::Background::IBackgroundTaskInstance sender, Windows::ApplicationModel::Background::BackgroundTaskCancellationReason reason);

    public:
        ServiceBackgroundTask() = default;
        void Run(Windows::ApplicationModel::Background::IBackgroundTaskInstance const& taskInstance);
    };
}
namespace winrt::Unicord::Universal::Voice::Background::factory_implementation {
    struct ServiceBackgroundTask : ServiceBackgroundTaskT<ServiceBackgroundTask, implementation::ServiceBackgroundTask> {
    };
}
