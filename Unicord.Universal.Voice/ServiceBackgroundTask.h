#pragma once
#include "ServiceBackgroundTask.g.h"

namespace winrt::Unicord::Universal::Voice::Background::implementation
{
    struct ServiceBackgroundTask : ServiceBackgroundTaskT<ServiceBackgroundTask>
    {
        ServiceBackgroundTask() = default;

        void Run(Windows::ApplicationModel::Background::IBackgroundTaskInstance const& taskInstance);
    };
}
namespace winrt::Unicord::Universal::Voice::Background::factory_implementation
{
    struct ServiceBackgroundTask : ServiceBackgroundTaskT<ServiceBackgroundTask, implementation::ServiceBackgroundTask>
    {
    };
}
