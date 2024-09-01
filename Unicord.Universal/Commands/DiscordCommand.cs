using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using DSharpPlus;
using Unicord.Universal.Models;

namespace Unicord.Universal.Commands
{
    public abstract class DiscordCommand<T> : ICommand where T : ViewModelBase
    {
        protected T viewModel;
        protected DiscordClient discord;

        public event EventHandler CanExecuteChanged;

        public DiscordCommand(T viewModel)
        {
            this.viewModel = viewModel;
            this.discord = App.Discord;
        }

        public virtual bool CanExecute(object parameter)
        {
            return true;
        }

        public abstract void Execute(object parameter);
    }
}
