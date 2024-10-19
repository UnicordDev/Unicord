using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using DSharpPlus.Entities;
using Unicord.Universal.Pages.Subpages;
using Unicord.Universal.Services;

namespace Unicord.Universal.Models.Channels
{
    public class ChannelPageViewModelBase : ChannelViewModel
    {
        internal ChannelPageViewModelBase(DiscordChannel channel, bool isTransient = false, ViewModelBase parent = null) 
            : base(channel, isTransient, parent)
        {
            CreateCommands();
        }

        internal ChannelPageViewModelBase(ulong channelId, bool isTransient = false, ViewModelBase parent = null)
            : base(channelId, isTransient, parent)
        {
            CreateCommands();
        }

        private void CreateCommands()
        {
            LeftPaneCommand = new RelayCommand(() => SplitPaneService.GetForCurrentView().ToggleLeftPane());
            SearchCommand = new RelayCommand(() => SplitPaneService.GetForCurrentView().ToggleRightPane<SearchPage>(this));
            PinsCommand = new RelayCommand(() => SplitPaneService.GetForCurrentView().ToggleRightPane<PinsPage>(this));
            UserListCommand = new RelayCommand(() => SplitPaneService.GetForCurrentView().ToggleRightPane<UserListPage>(this));
        }

        public ICommand LeftPaneCommand { get; private set; }
        public ICommand SearchCommand { get; private set; }            
        public ICommand PinsCommand { get; private set; }
        public ICommand UserListCommand { get; private set; }
    }
}
