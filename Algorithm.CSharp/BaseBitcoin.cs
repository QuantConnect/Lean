using NodaTime;
using QuantConnect.Brokerages;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Algorithm.CSharp
{

    /// <summary>
    /// Base class for Bitcoin algorithms. This base class provides standard behaviour for trading BTCUSD on Bitfinex
    /// trading on Bitfinex
    /// </summary>
    public abstract class BaseBitcoin : QCAlgorithm
    {

        const string _bitcoinSymbol = "BTCUSD";

        protected string BitcoinSymbol { get { return _bitcoinSymbol; } }

        public BaseBitcoin()
        {
            Portfolio = new BitfinexSecurityPortfolioManager(Securities, Transactions);
            SetBrokerageModel(BrokerageName.BitfinexBrokerage, AccountType.Margin);
            SetTimeZone(DateTimeZone.Utc);
            Transactions.MarketOrderFillTimeout = new TimeSpan(0, 0, 20);
        }

        public override void Initialize()
        {
            SetStartDate(2015, 11, 10);
            SetEndDate(2016, 2, 20);
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

        public abstract void OnData(Tick data);

        protected virtual void Output(string title)
        {
            Log(title + ": " + this.UtcTime.ToString() + ": " + Portfolio.Securities[BitcoinSymbol].Price.ToString() + " Trade:" + Portfolio[BitcoinSymbol].LastTradeProfit
                + " Total:" + Portfolio.TotalPortfolioValue);
        }

        /// <summary>
        /// Must liquidate before reversing position
        /// </summary>
        //todo: implement transaction limits in brokerage model
        protected virtual void Long()
        {
            if (Portfolio[BitcoinSymbol].IsShort)
            {
                Liquidate();
            }
            SetHoldings(BitcoinSymbol, 3.0m);
        }

        /// <summary>
        /// Must liquidate before reversing position
        /// </summary>
        //todo: implement transaction limits in brokerage model
        protected virtual void Short()
        {
            if (Portfolio[BitcoinSymbol].IsLong)
            {
                Liquidate();
            }
            SetHoldings(BitcoinSymbol, -3.0m);
        }

    }
}
