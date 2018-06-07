using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Securities;
using QuantConnect.Securities.Forex;

namespace QuantConnect.Lean.Engine.MeanPricing
{
    public interface IConversionRateProvider
    {
        /// <summary>
        /// Will be CashBook.AccountCurrency most of the times
        /// </summary>
        Cash TargetCurrency { get; }

        IReadOnlyList<Cash> SourceCurrencies { get; }

        IReadOnlyList<Security> AvailableSecurities { get; }

        bool SetTargetCurrency(Cash targetCurrency);

        bool AddSourceCurrency(Cash cash);
        bool RemoveSourceCurrency(Cash cash);

        // should market be optional?
        bool AddPair(string pair, string market = null);
        
        // if brokerage == null, then remove whole pair
        bool RemovePair(string pair, string market = null);
        
        // ensure if all needed securities are contained
        bool ContainsAllNeededSecurities();

        // update the price for one currency per brokerage
        decimal Update(Brokerages.Brokerage Brokerage, Security currency, decimal LastPrice, decimal Volume24);
        
        // Get price in target currency
        decimal GetPrice(Cash cash);

        ConversionRatePath GetPath(Cash from, Cash to);
    }
    
    //! TODO cache everything, so it doesn't rebuilds the graph for every calculation
    /*public class MeanConversionRateProvider : IConversionRateProvider
    {
        private Graph CurrencyNetwork;

        // needs rebuilding graph
        private bool dirtyStructure = true;

        // needs rebuilding price
        private bool dirtyPrice = true;
        // private List<Path> Paths;
    
        public IReadOnlyList<Edge> Subscriptions { get { return SubscriptionsList; } }
        
        private List<Edge> SubscriptionsList;

        private Data.SubscriptionManager subscriptionManager;

        public MeanConversionRateProvider(Data.SubscriptionManager subscriptionManager)
        {
            this.subscriptionManager = subscriptionManager;

            CurrencyNetwork = new Graph();
            //Paths = new List<Path>();
            SubscriptionsList = new List<Edge>();
        }

        /// <summary>
        /// Add pair to currency network
        /// </summary>
        /// <param name="PairSymbol"></param>
        public void AddPair(string PairSymbol)
        {
            string baseCode = null, quoteCode = null;
            Forex.DecomposeCurrencyPair(PairSymbol, out baseCode, out quoteCode);

            Data.SubscriptionDataConfig cfg = this.subscriptionManager.Add(typeof(Data.BaseData), TickType.Trade, symbol, Resolution.Minute, NodaTime.DateTimeZone.Utc, NodaTime.DateTimeZone.Utc, false, false, false, true, true);
        }
    }*/
}