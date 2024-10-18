using Unicord.Universal.Services;
using Windows.UI.Xaml;

namespace Unicord.Universal
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            // we wanna start connecting to Discord as early in the process as possible to
            // optimise for TTI
            if (args.Length > 0 && !args[0].Contains("Background"))
                DiscordManager.KickoffConnectionAsync();

            Application.Start((c) => new App());
        }
    }
}
