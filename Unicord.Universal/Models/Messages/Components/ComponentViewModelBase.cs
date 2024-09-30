﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace Unicord.Universal.Models.Messages.Components
{
    public class ComponentViewModelBase : ViewModelBase
    {
        protected readonly DiscordComponent component;
        protected readonly MessageViewModel messageViewModel;

        public ComponentViewModelBase(DiscordComponent component, MessageViewModel messageViewModel)
            : base(messageViewModel)
        {
            this.component = component;
            this.messageViewModel = messageViewModel;
        }
    }
}