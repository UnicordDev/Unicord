using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Unicord.Abstractions;
using Unicord.Universal.Dialogs;

namespace Unicord.Universal.Abstractions
{
    class UwpUIAbstractions : IUIAbstractions
    {
        public async Task ShowFailureDialogAsync(string title, string instruction, string content)
        {
            var dialog = new ErrorDialog()
            {
                Title = title,
                Text = content
            };

            await dialog.ShowAsync();
        }
    }
}
