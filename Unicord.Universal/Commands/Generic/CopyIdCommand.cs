using System;
using System.Globalization;
using System.Windows.Input;
using DSharpPlus.Entities;
using Microsoft.AppCenter.Analytics;
using Unicord.Universal.Models;
using Windows.ApplicationModel.DataTransfer;

namespace Unicord.Universal.Commands.Generic
{
    public class CopyIdCommand : ICommand
    {
        private readonly ISnowflake snowflake;

#pragma warning disable 67 // the event <event> is never used
        public event EventHandler CanExecuteChanged;
#pragma warning restore 67

        public CopyIdCommand(ISnowflake snowflake)
        {
            this.snowflake = snowflake;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            Analytics.TrackEvent("CopyIdCommand_Invoked");

            var package = new DataPackage();
            package.SetText(snowflake.Id.ToString(CultureInfo.InvariantCulture));
            Clipboard.SetContent(package);
        }
    }
}
