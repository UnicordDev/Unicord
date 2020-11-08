#pragma once
#include "VoiceBackgroundTask.g.h"

using namespace winrt::Windows::ApplicationModel::Background;

namespace winrt::Unicord::Universal::Voice::Background::implementation {
    struct VoiceBackgroundTask : VoiceBackgroundTaskT<VoiceBackgroundTask> {
    private:
        Windows::ApplicationModel::Background::BackgroundTaskDeferral taskDeferral{ nullptr };
        void OnCancelled(Windows::ApplicationModel::Background::IBackgroundTaskInstance sender, Windows::ApplicationModel::Background::BackgroundTaskCancellationReason reason);

    public:
        VoiceBackgroundTask() = default;
        void Run(IBackgroundTaskInstance const& taskInstance);
    };
}
namespace winrt::Unicord::Universal::Voice::Background::factory_implementation {
    struct VoiceBackgroundTask : VoiceBackgroundTaskT<VoiceBackgroundTask, implementation::VoiceBackgroundTask> {
    };
}
