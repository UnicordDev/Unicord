#include "pch.h"
#include <chrono>
#include "ServiceBackgroundTask.h"
#include "ServiceBackgroundTask.g.cpp"
#include "VoiceClient.h"
#include "VoiceClientOptions.h"

using namespace std::chrono_literals;
using namespace winrt::Windows::Foundation;
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
    VoiceClientOptions ServiceBackgroundTask::voiceClientOptions = VoiceClientOptions{ nullptr };

    void ServiceBackgroundTask::Run(IBackgroundTaskInstance const& taskInstance)
    {
        auto stream = new dbg_stream_for_cout();
        std::cout.rdbuf(stream);
        std::cout << std::unitbuf;

        this->taskDeferral = taskInstance.GetDeferral();
        taskInstance.Canceled({ this, &ServiceBackgroundTask::OnCancelled });

        auto details = taskInstance.TriggerDetails().try_as<AppServiceTriggerDetails>();

        this->appServiceConnection = details.AppServiceConnection();
        this->appServiceConnection.ServiceClosed({ this, &ServiceBackgroundTask::OnServiceClosed });
        this->appServiceConnection.RequestReceived({ this, &ServiceBackgroundTask::OnServiceMessage });
        this->appServiceConnected = true;
    }

    void ServiceBackgroundTask::OnUdpPing(IInspectable sender, uint32_t ping)
    {
        if (appServiceConnected && appServiceConnection != nullptr) {
            ValueSet values;
            values.Insert(L"ping", box_value(ping));
            RaiseEvent(VoiceServiceEvent::UdpPing, values);
        }
    }

    void ServiceBackgroundTask::OnWsPing(IInspectable sender, uint32_t ping)
    {
        if (appServiceConnected && appServiceConnection != nullptr) {
            ValueSet values;
            values.Insert(L"ping", box_value(ping));
            RaiseEvent(VoiceServiceEvent::WebSocketPing, values);
        }
    }

    void ServiceBackgroundTask::OnDisconnected(IInspectable sender, bool args)
    {
        ValueSet valueSet;
       if (appServiceConnected && appServiceConnection != nullptr) {
            if (args) { // we're attempting to reconnect
                RaiseEvent(VoiceServiceEvent::Reconnecting, valueSet);
            }
            else {
                RaiseEvent(VoiceServiceEvent::Disconnected, valueSet);
            }
        }
    }

    void ServiceBackgroundTask::OnConnected(IInspectable sender, bool args)
    {
        ValueSet valueSet;
        if (appServiceConnected && appServiceConnection != nullptr) {
            RaiseEvent(VoiceServiceEvent::Connected, valueSet);
        }
    }

    void ServiceBackgroundTask::OnAnswerRequested(VoipPhoneCall call, CallAnswerEventArgs args)
    {
        ValueSet valueSet;
        if (appServiceConnected && appServiceConnection != nullptr) {
            RaiseEvent(VoiceServiceEvent::AnswerRequested, valueSet);
        }
    }

    void ServiceBackgroundTask::OnRejectRequested(VoipPhoneCall call, CallRejectEventArgs args)
    {
        ValueSet valueSet;
        if (appServiceConnected && appServiceConnection != nullptr) {
            RaiseEvent(VoiceServiceEvent::RejectRequested, valueSet);
            activeCall = nullptr;
        }
    }

    void ServiceBackgroundTask::RaiseEvent(VoiceServiceEvent ev, ValueSet data)
    {
        if (appServiceConnected && appServiceConnection != nullptr) {
            data.Insert(L"ev", box_value((uint32_t)ev));
            this->appServiceConnection.SendMessageAsync(data);
        }
    }

    void ServiceBackgroundTask::OnServiceMessage(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
    {
        auto def = args.GetDeferral();
        auto request = args.Request();
        auto data = request.Message();

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
                    if (voiceClient == nullptr) {
                        voipCoordinator = VoipCallCoordinator::GetDefault();
                        activeCall = voipCoordinator.RequestNewOutgoingCall(
                            L"", unbox_value<hstring>(data.Lookup(L"contact_name")), Package::Current().DisplayName(), VoipPhoneCallMedia::Audio);
                        if (activeCall != nullptr) {
                            SetupCall(data);
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
                case VoiceServiceRequest::CallConnectRequest:
                {
                    if(activeCall != nullptr){
                        SetupCall(data);
                    }
                }
                break;
                case VoiceServiceRequest::NotifyIncomingCallRequest:
                {
                    voipCoordinator = VoipCallCoordinator::GetDefault();
                    activeCall = voipCoordinator.RequestNewIncomingCall(L"",
                        unbox_value<hstring>(data.Lookup(L"contact_name")),
                        unbox_value<hstring>(data.Lookup(L"contact_number")),
                        Uri{ unbox_value<hstring>(data.Lookup(L"contact_image")) },
                        Package::Current().DisplayName(),
                        Uri{ unbox_value<hstring>(data.Lookup(L"branding_image")) },
                        unbox_value<hstring>(data.Lookup(L"call_details")),
                        Uri{ unbox_value<hstring>(data.Lookup(L"ringtone")) },
                        VoipPhoneCallMedia::Audio,
                        90s);

                    if (activeCall != nullptr) {
                        activeCall.AnswerRequested({ this, &ServiceBackgroundTask::OnAnswerRequested });
                        activeCall.RejectRequested({ this, &ServiceBackgroundTask::OnRejectRequested });
                    }
                }
                break;
                case VoiceServiceRequest::GuildMoveRequest:
                {
                    if (voiceClient != nullptr && activeCall != nullptr) {
                        activeCall.ContactName(unbox_value<hstring>(data.Lookup(L"contact_name")));
                        voiceClientOptions.ChannelId(unbox_value<uint64_t>(data.Lookup(L"channel_id")));

                        // already connected, so raise the event again
                        RaiseEvent(VoiceServiceEvent::Connected, event_values);
                    }
                }
                break;
                case VoiceServiceRequest::StateRequest:
                    if (voiceClient != nullptr) {
                        values.Insert(L"state", box_value((uint32_t)VoiceServiceState::Connected));
                        values.Insert(L"guild_id", box_value(voiceClientOptions.GuildId()));
                        values.Insert(L"channel_id", box_value(voiceClientOptions.ChannelId()));
                        values.Insert(L"muted", box_value(voiceClient.Muted()));
                        values.Insert(L"deafened", box_value(voiceClient.Deafened()));
                    }
                    else {
                        values.Insert(L"state", box_value((uint32_t)VoiceServiceState::ReadyToConnect));
                    }
                    break;
                case VoiceServiceRequest::MuteRequest:
                    if (voiceClient != nullptr) {
                        auto muted = unbox_value<bool>(data.Lookup(L"muted"));
                        voiceClient.Muted(muted);

                        if (muted || voiceClient.Deafened()) {
                            activeCall.NotifyCallHeld();
                        }
                        else {
                            activeCall.NotifyCallActive();
                        }

                        event_values.Insert(L"muted", box_value(muted));
                        RaiseEvent(VoiceServiceEvent::Muted, event_values);
                    }
                    break;
                case VoiceServiceRequest::DeafenRequest:
                    if (voiceClient != nullptr) {
                        auto deafened = unbox_value<bool>(data.Lookup(L"deafened"));
                        voiceClient.Deafened(deafened);

                        if (deafened || voiceClient.Muted()) {
                            activeCall.NotifyCallHeld();
                        }
                        else {
                            activeCall.NotifyCallActive();
                        }

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

                        event_values.Insert(L"is_reconnecting", box_value(false));
                        RaiseEvent(VoiceServiceEvent::Disconnected, event_values);
                    }
                    break;

                case VoiceServiceRequest::SettingsUpdate:
                    if (voiceClient != nullptr) {
                        if (data.HasKey(L"input_device")) {
                            voiceClientOptions.PreferredRecordingDevice(unbox_value_or<hstring>(data.Lookup(L"input_device"), L""));
                        }

                        if (data.HasKey(L"output_device")) {
                            voiceClientOptions.PreferredPlaybackDevice(unbox_value_or<hstring>(data.Lookup(L"output_device"), L""));
                        }

                        voiceClient.UpdateAudioDevices();
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
            auto event = unbox_value<VoiceServiceEvent>(data.Lookup(L"ev"));
        }

        def.Complete();
    }

    void ServiceBackgroundTask::SetupCall(winrt::Windows::Foundation::Collections::ValueSet &data)
    {
        voiceClientOptions = make<Voice::implementation::VoiceClientOptions>();
        voiceClientOptions.Token(unbox_value<hstring>(data.Lookup(L"token")));
        voiceClientOptions.SessionId(unbox_value<hstring>(data.Lookup(L"session_id")));
        voiceClientOptions.Endpoint(unbox_value<hstring>(data.Lookup(L"endpoint")));
        voiceClientOptions.GuildId(unbox_value_or<uint64_t>(data.Lookup(L"guild_id"), 0));
        voiceClientOptions.ChannelId(unbox_value<uint64_t>(data.Lookup(L"channel_id")));
        voiceClientOptions.CurrentUserId(unbox_value<uint64_t>(data.Lookup(L"user_id")));

        if (data.HasKey(L"input_device")) {
            voiceClientOptions.PreferredRecordingDevice(unbox_value_or<hstring>(data.Lookup(L"input_device"), L""));
        }

        if (data.HasKey(L"output_device")) {
            voiceClientOptions.PreferredPlaybackDevice(unbox_value_or<hstring>(data.Lookup(L"output_device"), L""));
        }

        voiceClient = make<Voice::implementation::VoiceClient>(voiceClientOptions);
        voiceClient.UdpSocketPingUpdated({ this, &ServiceBackgroundTask::OnUdpPing });
        voiceClient.WebSocketPingUpdated({ this, &ServiceBackgroundTask::OnWsPing });

        if (data.HasKey(L"muted")) {
            voiceClient.Muted(unbox_value<bool>(data.Lookup(L"muted")));
        }

        if (data.HasKey(L"deafened")) {
            voiceClient.Deafened(unbox_value<bool>(data.Lookup(L"deafened")));
        }

        voiceClient.Connected({ this, &ServiceBackgroundTask::OnConnected });
        voiceClient.Disconnected({ this, &ServiceBackgroundTask::OnDisconnected });
        voiceClient.ConnectAsync().get();
        activeCall.NotifyCallActive();
    }


    void ServiceBackgroundTask::OnServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
    {
        this->appServiceConnected = false;
        this->appServiceConnection = nullptr;
    }

    void ServiceBackgroundTask::OnCancelled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
    {
        if (this->taskDeferral != nullptr) {
            this->taskDeferral.Complete();
        }
    }
}
