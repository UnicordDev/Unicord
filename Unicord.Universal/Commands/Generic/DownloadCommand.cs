using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Input;
using Unicord.Universal.Models;
using Unicord.Universal.Utilities;
using Windows.ApplicationModel.Resources;
using Windows.Storage.Pickers;
using Windows.Web.Http;

namespace Unicord.Universal.Commands.Generic
{
    public class ProgressInfo : ViewModelBase
    {
        private double _progressOpacity = 0;
        private double _iconOpacity = 1;
        private bool _isIndeterminate = false;
        private double _progress = 0;

        public double ProgressOpacity { get => _progressOpacity; set => OnPropertySet(ref _progressOpacity, value); }
        public double IconOpacity { get => _iconOpacity; set => OnPropertySet(ref _iconOpacity, value); }
        public bool IsIndeterminate { get => _isIndeterminate; set => OnPropertySet(ref _isIndeterminate, value); }
        public double Progress { get => _progress; set => OnPropertySet(ref _progress, value); }

        public IProgress<HttpProgress> GetProgress()
        {
            return new Progress<HttpProgress>(p =>
            {
                IconOpacity = 0;
                ProgressOpacity = 1;
                if (p.TotalBytesToReceive.HasValue)
                {
                    IsIndeterminate = false;
                    Progress = p.BytesReceived / (double)p.TotalBytesToReceive.Value;
                }
                else
                    IsIndeterminate = true;
            });
        }

        public void GoToProgress()
        {
            IconOpacity = 0;
            ProgressOpacity = 1;
            IsIndeterminate = true;
        }

        public void Reset()
        {
            IconOpacity = 1;
            ProgressOpacity = 0;
            IsIndeterminate = false;
        }
    }

    public class DownloadCommand : ICommand
    {
        private readonly ProgressInfo _info;
        private readonly string _url;
        private bool _canExecute;

        public DownloadCommand(string url, ProgressInfo info)
        {
            _url = url;
            _info = info;
            _canExecute = true;
        }

        public event EventHandler CanExecuteChanged;


        public bool CanExecute(object parameter)
        {
            return _canExecute;
        }

        public async void Execute(object parameter)
        {
            _canExecute = false;
            _info.GoToProgress();

            var url = new Uri(_url);
            var strings = ResourceLoader.GetForCurrentView("Controls");

            try
            {
                var extension = Path.GetExtension(url.AbsolutePath);
                var extensionString = Tools.GetItemTypeFromExtension(extension, strings.GetString("AttachmentExtensionPlaceholder"));
                var picker = new FileSavePicker()
                {
                    SuggestedStartLocation = PickerLocationId.Downloads,
                    SuggestedFileName = Path.GetFileNameWithoutExtension(url.AbsolutePath),
                    DefaultFileExtension = extension
                };

                picker.FileTypeChoices.Add($"{extensionString} (*{extension})", new List<string>() { extension });

                var file = await picker.PickSaveFileAsync();
                if (file != null)
                {
                    await Tools.DownloadToFileAsync(url, file, _info.GetProgress());
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                await UIUtilities.ShowErrorDialogAsync(
                    strings.GetString("AttachmentDownloadFailedTitle"),
                    strings.GetString("AttachmentDownloadFailedText"));
            }

            _info.Reset();
            _canExecute = true;
        }
    }
}
