using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unicord.Universal.Models.Messages;

namespace Unicord.Universal.Converters
{
    public class Static
    {
        public static bool NotNull(object obj)
            => obj != null;

        public static bool Not(bool b)
            => !b;

        public static bool Is(MessageViewModelState state, MessageViewModelState other)
            => state == other;

        public static bool IsNot(MessageViewModelState state, MessageViewModelState other)
            => state != other;
    }
}
