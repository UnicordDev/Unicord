using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Controls
{
    public sealed class CustomMediaTransportControls : MediaTransportControls
    {
        public CustomMediaTransportControls()
        {
            this.DefaultStyleKey = typeof(CustomMediaTransportControls);
            //this.IsCompact = true;
        }

        public event EventHandler<EventArgs> FullWindowRequested;

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (GetTemplateChild("CustomFullWindowButton") is not Button fullWindowButton) return;
            fullWindowButton.Click += OnFullWindowButtonClicked;
        }

        private void OnFullWindowButtonClicked(object sender, RoutedEventArgs e)
        {
            FullWindowRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
