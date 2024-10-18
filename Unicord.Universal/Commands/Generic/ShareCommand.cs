using System;
using System.Windows.Input;
using Unicord.Universal.Utilities;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.Storage;

namespace Unicord.Universal.Commands.Generic
{
    public class ShareCommand : ICommand
    {
        private readonly string url;
        private readonly string fileName;
        private readonly ProgressInfo shareProgress;

        public ShareCommand(string url, string fileName, ProgressInfo shareProgress)
        {
            this.url = url;
            this.fileName = fileName;
            this.shareProgress = shareProgress;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public async void Execute(object parameter)
        {
            var strings = ResourceLoader.GetForCurrentView("Controls");
            shareProgress.GoToProgress();

            try
            {
                var transferManager = DataTransferManager.GetForCurrentView();
                var shareFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);
                await Tools.DownloadToFileAsync(new Uri(url), shareFile, shareProgress.GetProgress());

                void DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
                {
                    var request = args.Request;
                    request.Data.Properties.Title = string.Format(strings.GetString("SharingTitleFormat"), fileName);
                    request.Data.Properties.Description = fileName;

                    request.Data.SetWebLink(new Uri(url));
                    request.Data.SetStorageItems(new[] { shareFile });

                    sender.DataRequested -= DataRequested;
                }

                transferManager.DataRequested += DataRequested;
                DataTransferManager.ShowShareUI();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                await UIUtilities.ShowErrorDialogAsync(
                      strings.GetString("AttachmentDownloadFailedTitle"),
                      strings.GetString("AttachmentDownloadFailedText"));
            }

            shareProgress.Reset();

        }
    }
}
