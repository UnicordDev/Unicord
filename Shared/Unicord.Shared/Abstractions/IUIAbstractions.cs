using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Unicord.Abstractions
{
    public interface IUIAbstractions
    {
        Task ShowFailureDialogAsync(string title, string instruction, string content);
    }
}
