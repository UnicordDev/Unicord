using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using DSharpPlus.Entities;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json.Linq;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using KWebView2;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Unicord.Universal.Dialogs
{
    public sealed partial class CaptchaRequestDialog : ContentDialog
    {
        private readonly DiscordCaptchaRequest captchaRequest;
        
        public DiscordCaptchaResponse CaptchaResponse;

        public CaptchaRequestDialog(DiscordCaptchaRequest captchaRequest)
        {
            this.InitializeComponent();

            this.captchaRequest = captchaRequest;
            this.CaptchaResponse = new DiscordCaptchaResponse();
        }

        private async void ContentDialog_Loaded(object sender, RoutedEventArgs e)
        {
            var theme = Application.Current.RequestedTheme == ApplicationTheme.Dark ? "dark" : "light";

            await CaptchaWebView.EnsureCoreWebView2Async(); // ensure the CoreWebView2 is created
            var coreWebView = CaptchaWebView.CoreWebView2;
            if (coreWebView != null)
            {
                coreWebView.SetVirtualHostNameToFolderMapping(
                    "appx", Package.Current.InstalledLocation.Path,
                    CoreWebView2HostResourceAccessKind.Allow);

                coreWebView.WebMessageReceived += OnMessageRecieved;

                CaptchaWebView.Source = new Uri($"https://appx/Assets/Captcha/hCaptcha.html?siteKey={captchaRequest.SiteKey}&theme=" + theme);
            }
        }

        private async void OnMessageRecieved(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            var jObject = JObject.Parse(args.WebMessageAsJson);
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                var op = jObject["op"].ToObject<string>();
                switch (op)
                {
                    case "captcha_complete":
                        CaptchaResponse = new DiscordCaptchaResponse(jObject["token"].ToObject<string>());
                        IsPrimaryButtonEnabled = true;
                        break;
                };
            });
        }
    }
}
