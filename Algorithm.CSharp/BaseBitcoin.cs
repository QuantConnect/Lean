using NodaTime;
using QuantConnect.Brokerages;
using QuantConnect.Data.Market;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Algorithm.CSharp
{
    public abstract class BaseBitcoin : QCAlgorithm
    {

        const string _bitcoinSymbol = "BTCUSD";

        protected string BitcoinSymbol { get { return _bitcoinSymbol; } }
        protected decimal StopLoss { get; set; }
        protected decimal TakeProfit { get; set; }

        public override void Initialize()
        {
            StopLoss = 0.05m;
            TakeProfit = 0.2m;

            SetStartDate(2015, 11, 10);
            SetEndDate(2016,2, 20);
            SetBrokerageModel(BrokerageName.BitfinexBrokerage, AccountType.Margin);
            SetTimeZone(DateTimeZone.Utc);
            Transactions.MarketOrderFillTimeout = new TimeSpan(0, 0, 20);
            AddSecurity(SecurityType.Forex, BitcoinSymbol, Resolution.Tick, Market.Bitcoin, false, 3.3m, false);
            SetCash("USD", 1000, 1m);
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

        /// <summary>
        /// Bitfinex margin call uses different behaviour to vanilla LEAN.
        /// </summary>
        /// <remarks>For the time being, margin call requests are ignored and algorithms are expected to provide their own.
        /// If the ticker price moves more than 15% against the base price of your position then you risk your entire position being liquidated.</remarks>
        /// <param name="requests"></param>
        /// todo: Implement BitfinexMarginCallModel and/or override SecurityPortfolioManager.ScanForMarginCall()
        public override void OnMarginCall(List<Orders.SubmitOrderRequest> requests)
        {
            requests.Clear();
        }

        public abstract void OnData(Tick data);

        protected virtual void Output(string title)
        {
            Log(title + ": " + this.UtcTime.ToString() + ": " + Portfolio.Securities[BitcoinSymbol].Price.ToString() + " Trade:" + Portfolio[BitcoinSymbol].LastTradeProfit
                + " Total:" + Portfolio.TotalPortfolioValue);
        }

        protected void TryStopLoss()
        {
            if (Portfolio.TotalUnrealisedProfit < -(Portfolio.TotalPortfolioValue * StopLoss))
            {
                Liquidate();
                Output("stop ");
            }
        }

        protected virtual void Long()
        {
            Liquidate();
            SetHoldings(BitcoinSymbol, 3.0m);
        }

        protected virtual void Short()
        {
            Liquidate();
            SetHoldings(BitcoinSymbol, -3.0m);
        }

    }
}
