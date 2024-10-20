using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unicord.Universal.Extensions;
using Unicord.Universal.Pages;
using Windows.UI.Xaml;

namespace Unicord.Universal.Models
{
    internal class RootViewModel : ViewModelBase
    {
        /// <summary>
        /// Gets the RootViewModel for the current View
        /// </summary>
        /// <returns></returns>
        public static RootViewModel GetForCurrentView()
        {
            var mainPage = Window.Current.Content.FindChild<MainPage>();
            return (RootViewModel)mainPage.DataContext;
        }

        public RootViewModel()
        {

        }

        /// <summary>
        /// A boolean determining if the current window is in a "full frame" view, as in
        /// there is no <see cref="DiscordPage"/>, and a <see cref="ChannelPage"> takes up the entire window.
        /// 
        /// <para>Notable use cases include overlay windows and My People.</para>
        /// </summary>
        public bool IsFullFrame { get; internal set; }
    }
}
