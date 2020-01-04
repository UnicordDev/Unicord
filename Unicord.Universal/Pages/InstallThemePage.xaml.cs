using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Unicord.Universal.Utilities;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Unicord.Universal.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class InstallThemePage : Page
    {
        private List<StorageFile> _files;

        public InstallThemePage()
        {
            RequestedTheme = App.Current.RequestedTheme == ApplicationTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is FileActivatedEventArgs args)
            {
                _files = args.Files.OfType<StorageFile>().ToList();
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            WindowManager.HandleTitleBarForWindow(TitleBar, this);
            WindowManager.HandleTitleBarForControl(Title);

            var currentView = ApplicationView.GetForCurrentView();
            currentView.TryResizeView(new Size(480, 640));

            var resources = new ResourceDictionary();
            var themes = await ThemeManager.LoadFromArchivesAsync(_files, resources);

            if (!themes.Any())
            {
                // no valid theme files found
                return;
            }

            Tools.InvertTheme(ActualTheme, this);
            App.Current.Resources.MergedDictionaries.Add(resources);
            Tools.InvertTheme(ActualTheme, this);

            DataContext = themes.First().Value;
        }
    }
}
