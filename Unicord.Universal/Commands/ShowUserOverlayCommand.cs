using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using DSharpPlus.Entities;
using Windows.UI.Xaml;

namespace Unicord.Universal.Commands
{
    public class ShowUserOverlayCommand : ICommand
    {
#pragma warning disable 67 // event <event> is unused
        public event EventHandler CanExecuteChanged;
#pragma warning restore 67

        public bool CanExecute(object parameter) => parameter is DiscordUser;

        public void Execute(object parameter)
        {
            if (parameter is DiscordMember member)
            {
                var page = Window.Current.Content.FindChild<MainPage>();
                if (page != null)
                {
                    page.ShowUserOverlay(member, true);
                }
            }
        }
    }
}
