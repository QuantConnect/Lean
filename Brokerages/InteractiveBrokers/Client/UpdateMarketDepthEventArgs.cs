using System;

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    public sealed class UpdateMarketDepthEventArgs : EventArgs
    {
        public int TickerId { get; private set; }
        public int Position { get; private set; }
        public int Operation { get; private set; }
        public int Side { get; private set; }
        public double Price { get; private set; }
        public int Size { get; private set; }
        public UpdateMarketDepthEventArgs(int tickerId, int position, int operation, int side, double price, int size)
        {
            TickerId = tickerId;
            Position = position;
            Operation = operation;
            Side = side;
            Price = price;
            Size = size;
        }
    }
}