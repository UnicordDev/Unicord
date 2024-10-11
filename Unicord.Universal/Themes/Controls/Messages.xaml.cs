using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unicord.Universal.Models.Messages;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Unicord.Universal.Resources.Controls
{
    public partial class Messages : ResourceDictionary
    {
        public Messages()
        {
            InitializeComponent();
        }

        public Uri ToUri(object obj) => (Uri)obj;
    }
}
