using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Unicord.Universal.Models.Messaging
{
    public class LoggedOutMessage : CollectionRequestMessage<Task>
    {
    }

    public class ShowConnectingOverlayMessage : CollectionRequestMessage<Task>
    {

    }
    public class HideConnectingOverlayMessage : CollectionRequestMessage<Task>
    {

    }
}
