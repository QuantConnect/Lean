using System;

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    public sealed class ErrorEventArgs : EventArgs
    {
        public int Id { get; private set; }
        public int Code { get; private set; }
        public string Message { get; private set; }

        public ErrorEventArgs(int id, int code, string message)
        {
            Id = id;
            Code = code;
            Message = message;
        }
    }
}