﻿using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Unicord.Universal.Models.Channels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace Unicord.Universal.Controls.Channels
{
    public sealed class ChannelListControl : Control
    {
        public ChannelListViewModel Channel
        {
            get { return (ChannelListViewModel)GetValue(ChannelProperty); }
            set { SetValue(ChannelProperty, value); }
        }

        public static readonly DependencyProperty ChannelProperty =
            DependencyProperty.Register("Channel", typeof(ChannelListViewModel), typeof(ChannelListControl), new PropertyMetadata(null, OnChannelChanged));

        private static void OnChannelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ChannelListControl)d).OnChannelChanged(e);
        }

        public ChannelListControl()
        {
            this.DefaultStyleKey = typeof(ChannelListControl);
        }

        private void OnChannelChanged(DependencyPropertyChangedEventArgs e)
        {
            //if (e.NewValue is DmChannelListViewModel)
            //    this.GoToElementStateCore("DM", false);
            //else
            //    this.GoToElementStateCore("Guild", false);
        }
    }
}