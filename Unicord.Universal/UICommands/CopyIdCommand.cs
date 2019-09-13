using System;
using System.Windows.Input;
using DSharpPlus.Entities;
using Windows.ApplicationModel.DataTransfer;

namespace Unicord.Universal.UICommands
{
    class CopyIdCommand : ICommand
    {
#pragma warning disable 67 // the event <event> is never used
        public event EventHandler CanExecuteChanged;
#pragma warning restore 67

        public bool CanExecute(object parameter)
        {
            return parameter is SnowflakeObject;
        }

        public void Execute(object parameter)
        {
            if (parameter is SnowflakeObject snowflake)
            {
                var package = new DataPackage();
                package.SetText(snowflake.Id.ToString());
                Clipboard.SetContent(package);
            }
        }
    }
}
