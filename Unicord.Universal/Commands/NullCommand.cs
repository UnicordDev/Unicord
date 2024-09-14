using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Unicord.Universal.Commands
{
    internal class NullCommand : ICommand
    {
        public static readonly ICommand Instance
            = new NullCommand();

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return false;
        }

        public void Execute(object parameter)
        {
            // do nothing
        }
    }
}
