using System;

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    public sealed class UpdateNewsBulletinEventArgs : EventArgs
    {
        public int MessageId { get; private set; }
        public int MessageType { get; private set; }
        public string Message { get; private set; }
        public string OriginalExchange { get; private set; }
        public UpdateNewsBulletinEventArgs(int messageId, int messageType, string message, string originalExchange)
        {
            MessageId = messageId;
            MessageType = messageType;
            Message = message;
            OriginalExchange = originalExchange;
        }
    }
}