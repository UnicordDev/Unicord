using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Unicord.Universal.Services;

namespace Unicord.Universal.Commands
{
    class SettingsCommand : ICommand
    {
#pragma warning disable 67 // the event <event> is never used
        public event EventHandler CanExecuteChanged;
#pragma warning restore 67

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public async void Execute(object parameter)
        {
            var settings = SettingsService.GetForCurrentView();
            await settings.OpenAsync(parameter is SettingsPageType t ? t : SettingsPageType.Accounts);
        }
    }
}
