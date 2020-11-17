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

namespace winrt::Unicord::Universal::Voice::Background::implementation {
    VoipCallCoordinator ServiceBackgroundTask::voipCoordinator = VoipCallCoordinator{ nullptr };
    VoipPhoneCall ServiceBackgroundTask::activeCall = VoipPhoneCall{ nullptr };
    VoiceClient ServiceBackgroundTask::voiceClient = VoiceClient{ nullptr };
    VoiceClientOptions ServiceBackgroundTask::voiceClientOptions = VoiceClientOptions{ nullptr };

    void ServiceBackgroundTask::Run(IBackgroundTaskInstance const& taskInstance) {
        auto stream = new dbg_stream_for_cout();
        std::cout.rdbuf(stream);
        std::cout << std::unitbuf;

        this->taskDeferral = taskInstance.GetDeferral();
        taskInstance.Canceled({ this, &ServiceBackgroundTask::OnCancelled });

        auto details = taskInstance.TriggerDetails().try_as<AppServiceTriggerDetails>();
        this->appServiceConnection = details.AppServiceConnection();
        this->appServiceConnected = true;
        this->appServiceConnection.ServiceClosed({ this, &ServiceBackgroundTask::OnServiceClosed });
        this->appServiceConnection.RequestReceived({ this, &ServiceBackgroundTask::OnServiceMessage });
    }

    void ServiceBackgroundTask::OnUdpPing(Windows::Foundation::IInspectable sender, uint32_t ping) {
        ValueSet values;
        values.Insert(L"ping", box_value(ping));
        RaiseEvent(VoiceServiceEvent::UdpPing, values);
    }

    void ServiceBackgroundTask::OnWsPing(Windows::Foundation::IInspectable sender, uint32_t ping) {
        ValueSet values;
        values.Insert(L"ping", box_value(ping));
        RaiseEvent(VoiceServiceEvent::WebSocketPing, values);
    }

    void ServiceBackgroundTask::RaiseEvent(VoiceServiceEvent ev, ValueSet data) {
        if (appServiceConnected) {
            data.Insert(L"ev", box_value((uint32_t)ev));
            this->appServiceConnection.SendMessageAsync(data);
        }
    }

    void ServiceBackgroundTask::OnServiceMessage(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args) {
        auto def = args.GetDeferral();
        auto request = args.Request();
        auto data = request.Message();

        // req signifies a request
        if (data.HasKey(L"req")) {
            VoiceServiceRequest ev = VoiceServiceRequest::RequestSucceeded;
            ValueSet values;
            ValueSet event_values;

            try {
                auto request_op = (VoiceServiceRequest)unbox_value<uint32_t>(data.Lookup(L"req"));
                switch (request_op) {
                case VoiceServiceRequest::GuildConnectRequest: {
                    if (voiceClient == nullptr && activeCall == nullptr) {
                        voipCoordinator = VoipCallCoordinator::GetDefault();
                        activeCall = voipCoordinator.RequestNewOutgoingCall(
                            L"", unbox_value<hstring>(data.Lookup(L"contact_name")), Package::Current().DisplayName(), VoipPhoneCallMedia::Audio);
                        if (activeCall != nullptr) {
                            voiceClientOptions = make<Voice::implementation::VoiceClientOptions>();
                            voiceClientOptions.Token(unbox_value<hstring>(data.Lookup(L"token")));
                            voiceClientOptions.SessionId(unbox_value<hstring>(data.Lookup(L"session_id")));
                            voiceClientOptions.Endpoint(unbox_value<hstring>(data.Lookup(L"endpoint")));
                            voiceClientOptions.GuildId(unbox_value<uint64_t>(data.Lookup(L"guild_id")));
                            voiceClientOptions.ChannelId(unbox_value<uint64_t>(data.Lookup(L"channel_id")));
                            voiceClientOptions.CurrentUserId(unbox_value<uint64_t>(data.Lookup(L"user_id")));
                            voiceClientOptions.SuppressionLevel((NoiseSuppressionLevel)unbox_value<uint32_t>(data.TryLookup(L"noise_suppression")));
                            voiceClientOptions.EchoCancellation(unbox_value_or(data.TryLookup(L"echo_cancellation"), true));
                            voiceClientOptions.VoiceActivity(unbox_value_or(data.TryLookup(L"voice_activity"), true));
                            voiceClientOptions.AutomaticGainControl(unbox_value_or(data.TryLookup(L"auto_gain_control"), true));

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
                } break;
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

                        try {
                            if (muted || voiceClient.Deafened()) {
                                activeCall.NotifyCallHeld();
                            }
                            else {
                                activeCall.NotifyCallActive();
                            }	
						}
                        catch (const winrt::hresult_error&) {
						}

                        event_values.Insert(L"muted", box_value(muted));
                        RaiseEvent(VoiceServiceEvent::Muted, event_values);
                    }
                    break;
                case VoiceServiceRequest::DeafenRequest:
                    if (voiceClient != nullptr) {
                        auto deafened = unbox_value<bool>(data.Lookup(L"deafened"));
                        voiceClient.Deafened(deafened);

                        try {
                            if (deafened || voiceClient.Muted()) {
                                activeCall.NotifyCallHeld();
                            }
                            else {
                                activeCall.NotifyCallActive();
                            }
                        }
                        catch (const winrt::hresult_error&) {
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
            catch (const std::exception& ex) {
                ev = VoiceServiceRequest::RequestFailed;
                values.Insert(L"msg", box_value(to_hstring(ex.what())));
            }
            catch (const winrt::hresult_error& ex) {
                ev = VoiceServiceRequest::RequestFailed;
                values.Insert(L"msg", box_value(to_hstring(ex.message())));
            }

            values.Insert(L"req", box_value((uint32_t)ev));
            request.SendResponseAsync(values).get();
        }

        // ev signifies an event
        if (data.HasKey(L"ev")) {
            auto event = unbox_value<VoiceServiceEvent>(data.Lookup(L"ev"));
        }

        def.Complete();
    }

    void ServiceBackgroundTask::OnServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args) {
        this->appServiceConnected = false;
    }

    void ServiceBackgroundTask::OnCancelled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason) {
        if (this->taskDeferral != nullptr) {
            this->taskDeferral.Complete();
        }
    }
}
