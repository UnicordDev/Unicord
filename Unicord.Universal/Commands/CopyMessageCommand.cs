using System;
using System.Web;
using System.Windows.Input;
using DSharpPlus;
using DSharpPlus.Entities;
using Markdig;
using Windows.ApplicationModel.DataTransfer;

namespace Unicord.Universal.Commands
{
    class CopyMessageCommand : ICommand
    {
        private MarkdownPipeline _pipeline;

        public CopyMessageCommand()
        {
            _pipeline = new MarkdownPipelineBuilder()
                .DisableHeadings()
                .DisableHtml()
                .UseAdvancedExtensions()
                .UseAutoLinks()
                .Build();
        }

#pragma warning disable 67 // the event <event> is never used
        public event EventHandler CanExecuteChanged;
#pragma warning restore 67

        public bool CanExecute(object parameter)
        {
            return parameter is DiscordMessage;
        }

        public void Execute(object parameter)
        {

            if (parameter is DiscordMessage message)
            {
                var package = new DataPackage();
                package.RequestedOperation = DataPackageOperation.Copy | DataPackageOperation.Link;

                var serverText = message.Channel.Guild != null ? message.Channel.GuildId.ToString() : "@me";
                var uri = "https://" + $"discordapp.com/channels/{serverText}/{message.ChannelId}/{message.Id}/";
                var markdown = Formatter.MaskedUrl(message.Content, new Uri(uri));
                var html = Markdown.ToHtml(markdown, _pipeline);
                package.SetText(message.Content);
                package.SetWebLink(new Uri(uri));
                package.SetHtmlFormat(html);
                package.SetRtf($"{{\\field{{\\*\\fldinst HYPERLINK \"{uri}\"}}{{\fldrslt {message.Content}}}");

                Clipboard.SetContent(package);
            }

        }
    }
}
