using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DSharpPlus.Abstractions
{
    public interface IUIThreadDispatcher
    {
        void RunOnUIThread(Action action);
        T RunOnUIThread<T>(Func<T> func);

        Task RunOnUIThreadAsync(Action action);
    }
}
