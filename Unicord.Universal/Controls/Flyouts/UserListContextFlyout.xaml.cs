using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using DSharpPlus;
using DSharpPlus.Entities;
using Unicord.Universal.Models.User;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using static Microsoft.Toolkit.Uwp.UI.Animations.Expressions.ExpressionValues;

namespace Unicord.Universal.Controls.Flyouts
{
    public sealed partial class UserListContextFlyout : MenuFlyout
    {
        public UserListContextFlyout()
        {
            InitializeComponent();
        }
    }
}
