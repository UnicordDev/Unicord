#pragma once
#include "ServiceBackgroundTask.g.h"
#include <winrt/Windows.ApplicationModel.AppService.h>

namespace winrt::Unicord::Universal::Voice::Background::implementation
{
	struct ServiceBackgroundTask : ServiceBackgroundTaskT<ServiceBackgroundTask>
	{
	private:
		Windows::ApplicationModel::Background::BackgroundTaskDeferral taskDeferral{ nullptr };
		Windows::ApplicationModel::AppService::AppServiceConnection appServiceConnection{ nullptr };
		Unicord::Universal::Voice::VoiceClient voiceClient{ nullptr };

		void RaiseEvent(Unicord::Universal::Voice::Background::VoiceServiceEvent ev, Windows::Foundation::Collections::ValueSet data);
		void OnServiceMessage(Windows::ApplicationModel::AppService::AppServiceConnection sender, Windows::ApplicationModel::AppService::AppServiceRequestReceivedEventArgs args);
		void OnCancelled(Windows::ApplicationModel::Background::IBackgroundTaskInstance sender, Windows::ApplicationModel::Background::BackgroundTaskCancellationReason reason);
	public:
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
