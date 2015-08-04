using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TakeAshUtility;

namespace p2pChat {

    public class MessageReceivedEventArgs :
        EventArgs {

        public MessageReceivedEventArgs(ChatMessage message) {
            Message = message;
        }

        public ChatMessage Message { get; private set; }
        public ChatMessage Response { get; set; }

        public override string ToString() {
            return "Message:{" + Message + "}, Response:{" + Response + "}";
        }
    }

    public delegate void MessageReceivedEventHandler(
        INotifyMessageReceived sender,
        MessageReceivedEventArgs e
    );

    public interface INotifyMessageReceived {
        event MessageReceivedEventHandler MessageReceived;
    }

    public static class INotifyMessageReceivedExtensionMethods {

        const string EventHandlerName = "MessageReceived";

        public static void NotifyMessageReceived(this INotifyMessageReceived sender, MessageReceivedEventArgs e) {
            MessageReceivedEventHandler handler;
            if (sender == null ||
                e == null ||
                (handler = sender.GetDelegate(EventHandlerName)
                    .GetHandler<MessageReceivedEventHandler>()) == null) {
                return;
            }
            handler(sender, e);
        }

        public static ChatMessage NotifyMessageReceived(this INotifyMessageReceived sender, ChatMessage message) {
            var e = new MessageReceivedEventArgs(message);
            sender.NotifyMessageReceived(e);
            return e.Response;
        }
    }
}
