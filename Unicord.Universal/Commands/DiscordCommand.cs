using System;
using System.Windows.Input;
using DSharpPlus;
using Unicord.Universal.Models;
using Unicord.Universal.Services;

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
            this.discord = DiscordManager.Discord;
        }

        public virtual bool CanExecute(object parameter)
        {
            return true;
        }

        public abstract void Execute(object parameter);
    }
}
