using System;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using DSharpPlus;
using DSharpPlus.EventArgs;
using Microsoft.AppCenter.Analytics;
using Microsoft.Toolkit.Uwp.Helpers;
using TenMica;
using Unicord.Universal.Integration;
using Unicord.Universal.Models;
using Unicord.Universal.Models.Messaging;
using Unicord.Universal.Pages;
using Unicord.Universal.Services;
using Unicord.Universal.Utilities;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.Foundation.Metadata;
using Windows.Security.Credentials;
using Windows.UI.Core;
using Windows.UI.StartScreen;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace Unicord.Universal
{
    public sealed partial class MainPage : Page
    {
        public bool IsOverlayShown { get; internal set; }
        public Frame RootFrame => rootFrame;
        public Frame CustomFrame => CustomOverlayFrame;

        private MainPageArgs Arguments { get; set; }

        public MainPage()
        {
            InitializeComponent();

            if (ThemeService.GetForCurrentView().GetTheme() == AppTheme.SunValley)
            {
                Background = new TenMicaBrush();
            }

            if (!LoginService.GetForCurrentView().HasToken)
                RootFrame.SourcePageType = typeof(LoginPage);

            WeakReferenceMessenger.Default.Register<MainPage, ShowConnectingOverlayMessage>(this, (t, m) => t.ShowConnectingOverlay());
            WeakReferenceMessenger.Default.Register<MainPage, HideConnectingOverlayMessage>(this, (t, m) => t.HideConnectingOverlay());
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is not MainPageArgs args)
                return;

            DataContext = new RootViewModel() { IsFullFrame = args.FullFrame };
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "Connecting", false);
            VisualStateManager.GoToState(this, "OverlayHidden", false);

            var handle = WindowingService.Current.GetHandle(this);
            if (WindowingService.Current.IsMainWindow(handle))
                WindowingService.Current.HandleTitleBarForWindow(TitleBar, this);
        }

        private void OnSplashResize(object sender, WindowSizeChangedEventArgs e)
        {
            PositionSplash(Arguments.SplashScreen);
        }

        private void PositionSplash(SplashScreen splash)
        {
            var imageRect = splash.ImageLocation;
            ExtendedSplashImage.SetValue(Canvas.LeftProperty, imageRect.X);
            ExtendedSplashImage.SetValue(Canvas.TopProperty, imageRect.Y);
            ExtendedSplashImage.Height = imageRect.Height;
            ExtendedSplashImage.Width = imageRect.Width;

            ConnectingProgress.SetValue(Canvas.LeftProperty, imageRect.X + (imageRect.Width * 0.5) - (ConnectingProgress.Width * 0.5));
            ConnectingProgress.SetValue(Canvas.TopProperty, imageRect.Y + imageRect.Height + imageRect.Height * 0.1);
        }

        internal void ShowConnectingOverlay()
        {
            VisualStateManager.GoToState(this, "Connecting", true);
        }

        internal void HideConnectingOverlay()
        {
            VisualStateManager.GoToState(this, "Connected", true);
        }

        public void ShowCustomOverlay()
        {
            VisualStateManager.GoToState(this, "OverlayVisible", true);
        }

        public void HideCustomOverlay()
        {
            VisualStateManager.GoToState(this, "OverlayHidden", true);
        }

        private void OverlayBackdrop_Tapped(object sender, TappedRoutedEventArgs e)
        {
            OverlayService.GetForCurrentView()
                .CloseOverlay();
        }
    }
}
