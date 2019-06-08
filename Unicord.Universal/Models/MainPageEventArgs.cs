namespace Unicord.Universal.Models
{
    internal class MainPageViewModel
    {
        public ulong ChannelId { get; internal set; }
        public bool IsUriActivation { get; internal set; }
        internal ulong UserId { get; set; }
        internal bool FullFrame { get; set; }
    }
}
