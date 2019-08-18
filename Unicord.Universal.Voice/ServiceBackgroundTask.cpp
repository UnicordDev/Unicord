#include "pch.h"
#include "ServiceBackgroundTask.h"
#include "ServiceBackgroundTask.g.cpp"
#include "VoiceClient.h"
#include "VoiceClientOptions.h"

using namespace winrt::Windows::Foundation::Collections;
using namespace winrt::Windows::ApplicationModel;
using namespace winrt::Windows::ApplicationModel::AppService;
using namespace winrt::Windows::ApplicationModel::Background;
using namespace winrt::Windows::ApplicationModel::Calls;

namespace winrt::Unicord::Universal::Voice::Background::implementation
{
	VoipCallCoordinator ServiceBackgroundTask::voipCoordinator = VoipCallCoordinator{ nullptr };
	VoipPhoneCall ServiceBackgroundTask::activeCall = VoipPhoneCall{ nullptr };
	VoiceClient ServiceBackgroundTask::voiceClient = VoiceClient{ nullptr };

	void ServiceBackgroundTask::Run(IBackgroundTaskInstance const& taskInstance)
	{
		auto stream = new dbg_stream_for_cout();
		std::cout.rdbuf(stream);
		std::cout << std::unitbuf;

		this->taskDeferral = taskInstance.GetDeferral();
		taskInstance.Canceled({ this, &ServiceBackgroundTask::OnCancelled });

		this->voipCoordinator = VoipCallCoordinator::GetDefault();

		auto details = taskInstance.TriggerDetails().try_as<AppServiceTriggerDetails>();
		this->appServiceConnection = details.AppServiceConnection();
		this->appServiceConnection.RequestReceived({ this, &ServiceBackgroundTask::OnServiceMessage });
	}

	void ServiceBackgroundTask::RaiseEvent(VoiceServiceEvent ev, ValueSet data)
	{
		data.Insert(L"ev", box_value((uint32_t)ev));
		this->appServiceConnection.SendMessageAsync(data);
	}

	void ServiceBackgroundTask::OnServiceMessage(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
	{
		auto def = args.GetDeferral();
		auto request = args.Request();
		auto data = request.Message();
		VoiceClientOptions options{ nullptr };

		// req signifies a request
		if (data.HasKey(L"req"))
		{
			VoiceServiceRequest ev = VoiceServiceRequest::RequestSucceeded;
			ValueSet values;
			ValueSet event_values;

			try
			{
				auto request_op = (VoiceServiceRequest)unbox_value<uint32_t>(data.Lookup(L"req"));
				switch (request_op)
				{
				case VoiceServiceRequest::GuildConnectRequest:
				{
					if (voiceClient == nullptr && activeCall == nullptr) {
						activeCall = voipCoordinator.RequestNewOutgoingCall(L"", unbox_value<hstring>(data.Lookup(L"contact_name")), Package::Current().DisplayName(), VoipPhoneCallMedia::Audio);
						if (activeCall != nullptr) {
							options = make<Voice::implementation::VoiceClientOptions>();
							options.Token(unbox_value<hstring>(data.Lookup(L"token")));
							options.SessionId(unbox_value<hstring>(data.Lookup(L"session_id")));
							options.Endpoint(unbox_value<hstring>(data.Lookup(L"endpoint")));
							options.GuildId(unbox_value<uint64_t>(data.Lookup(L"guild_id")));
							options.ChannelId(unbox_value<uint64_t>(data.Lookup(L"channel_id")));
							options.CurrentUserId(unbox_value<uint64_t>(data.Lookup(L"user_id")));

							voiceClient = make<Voice::implementation::VoiceClient>(options);
							voiceClient.ConnectAsync().get();
							activeCall.NotifyCallActive();

							RaiseEvent(VoiceServiceEvent::Connected, event_values);
						}
						else {
							throw std::exception("Unable to get call");
						}
					}
					else {
						// already connected, so raise the event again
						RaiseEvent(VoiceServiceEvent::Connected, event_values);
					}
				}
				break;
				case VoiceServiceRequest::MuteRequest:
					if (voiceClient != nullptr) {
						auto muted = unbox_value<bool>(data.Lookup(L"muted"));
						voiceClient.Muted(muted);

						event_values.Insert(L"muted", box_value(muted));
						RaiseEvent(VoiceServiceEvent::Muted, event_values);
					}
					break;
				case VoiceServiceRequest::DeafenRequest:
					if (voiceClient != nullptr) {
						auto deafened = unbox_value<bool>(data.Lookup(L"deafened"));

						voiceClient.Deafened(deafened);
						event_values.Insert(L"deafened", box_value(deafened));
						RaiseEvent(VoiceServiceEvent::Deafened, event_values);
					}
					break;
				case VoiceServiceRequest::DisconnectRequest:
					if (voiceClient != nullptr) {
						voiceClient.Close();
						activeCall.NotifyCallEnded();
						activeCall = nullptr;
						voiceClient = nullptr;
						RaiseEvent(VoiceServiceEvent::Disconnected, event_values);
					}
					break;
				default:
					break;
				}
			}
			catch (const std::exception& ex)
			{
				ev = VoiceServiceRequest::RequestFailed;
				values.Insert(L"msg", box_value(to_hstring(ex.what())));
			}

			values.Insert(L"req", box_value((uint32_t)ev));
			request.SendResponseAsync(values).get();
		}

		// ev signifies an event
		if (data.HasKey(L"ev"))
		{
			auto event = unbox_value<VoiceServiceEvent>(data.Lookup(L"i"));
		}

		def.Complete();
	}

	void ServiceBackgroundTask::OnCancelled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
	{
		if (this->taskDeferral != nullptr) {
			this->taskDeferral.Complete();
		}
	}
}
