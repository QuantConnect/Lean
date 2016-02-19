using NodaTime;
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.CSharp;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.Custom;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Fills;
using QuantConnect.Orders.Slippage;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Algorithm.CSharp
{


    /// <summary>
    /// Sample Bitcoin Trading Algo.
    /// </summary>
    public partial class BitcoinRsiAlgorithm : QCAlgorithm
    {
        string symbol = "BTCUSD";
        RelativeStrengthIndex rsi;
        int period = 12;

        public override void Initialize()
        {

            SetBrokerageModel(BrokerageName.BitfinexBrokerage, AccountType.Margin);
            SetTimeZone(DateTimeZone.Utc);
            Transactions.MarketOrderFillTimeout = new TimeSpan(0, 0, 20);

            SetStartDate(2016, 1, 1);
            SetEndDate(2016, 2, 1);
            AddSecurity(SecurityType.Forex, symbol, Resolution.Tick, Market.Bitcoin, false, 3.3m, false);
            rsi = RSI(symbol, period, MovingAverageType.Exponential, Resolution.Hour);           

            SetCash("USD", 1000, 1m);

            var history = History<Tick>(symbol, this.StartDate.AddHours(-period), this.StartDate, Resolution.Tick);

            foreach (var item in history)
            {
                rsi.Update(item.Time, item.Price);
            }

        }

        private void Analyse(DateTime time, decimal price)
        {
            if (rsi.IsReady && !this.IsWarmingUp)
            {
                Long();
                Short();
            }
        }

        public void OnData(TradeBars data)
        {
            Analyse(data.Values.First().Time, data.Values.First().Price);
        }

        public void OnData(Tick data)
        {
            Analyse(data.Time, data.Price);
        }

        public void OnData(Ticks data)
        {

            foreach (var item in data)
            {
                foreach (var tick in item.Value)
                {
                    OnData(tick);
                }
            }

        }

        private void Long()
        {
            if (!Portfolio[symbol].IsLong && rsi.Current.Value > 5 && rsi.Current.Value < 30)
            {
                int quantity = CalculateOrderQuantity(symbol, 3m);
                if (quantity > 0)
                {
                    SetHoldings(symbol, 3.0m);
                    //maker fee
                    //LimitOrder(symbol, quantity, Portfolio[symbol].Price - 0.1m);
                }
                Output("Long");
            }

        }

        private void Short()
        {
            if (!Portfolio[symbol].IsShort && rsi.Current.Value > 70)
            {
                SetHoldings(symbol, -3.0m);
                Output("Short");
            }
        }

        private void Output(string title)
        {
            Log(title + ":" + Portfolio.Securities[symbol].Price.ToString() + " rsi:" + Math.Round(rsi.Current.Value, 0) + " Total:" + Portfolio.TotalPortfolioValue);
        }

    }
}