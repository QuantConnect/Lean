using System;

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    public sealed class UpdateMarketDepthLevel2EventArgs : EventArgs
    {
        public int TickerId { get; private set; }
        public int Position { get; private set; }
        public string MarketMaker { get; private set; }
        public int Operation { get; private set; }
        public int Side { get; private set; }
        public double Price { get; private set; }
        public int Size { get; private set; }
        public UpdateMarketDepthLevel2EventArgs(int tickerId, int position, string marketMaker, int operation, int side, double price, int size)
        {
            TickerId = tickerId;
            Position = position;
            MarketMaker = marketMaker;
            Operation = operation;
            Side = side;
            Price = price;
            Size = size;
        }
    }
}