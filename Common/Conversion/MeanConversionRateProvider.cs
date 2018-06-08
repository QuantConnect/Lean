using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Securities;
using QuantConnect.Securities.Forex;

namespace QuantConnect.Conversion
{
    //! TODO cache everything, so it doesn't rebuilds the graph for every calculation
    public class MeanConversionRateProviderFactory /*: IConversionRateProvider*/
    {
        public Cash TargetCurrency { get; private set; }

        private Graph.Graph currencyGraph;

        /// <summary>
        /// Flag for rebuilding structure (Security has been added / removed)
        /// </summary>
        private bool dirtyStructure = true;

        /// <summary>
        /// Flag for recalculating prices (new data came)
        /// </summary>
        private bool dirtyPrice = true;

        // private List<Edge> SubscriptionsList;

        // public IReadOnlyList<Edge> Subscriptions { get { return SubscriptionsList; } }

        IReadOnlyList<Cash> SourceCurrencies { get; }

        IReadOnlyList<Security> SubscribedPairs { get; }

        private Data.SubscriptionManager subscriptionManager;

        public MeanConversionRateProviderFactory(Data.SubscriptionManager subscriptionManager)
        {
            this.subscriptionManager = subscriptionManager;

            // SubscriptionsList = new List<Edge>();
        }

        bool SetTargetCurrency(Cash targetCurrency)
        {
            if (this.TargetCurrency != targetCurrency)
            {
                this.TargetCurrency = targetCurrency;
                dirtyStructure = true;
                dirtyPrice = true;
                return true;
            }

            return false;
        }

        /*public void AddPair(string PairSymbol)
        {
            string baseCode = null, quoteCode = null;
            Forex.DecomposeCurrencyPair(PairSymbol, out baseCode, out quoteCode);

            Data.SubscriptionDataConfig cfg = this.subscriptionManager.Add(typeof(Data.BaseData), TickType.Trade, symbol, Resolution.Minute, NodaTime.DateTimeZone.Utc, NodaTime.DateTimeZone.Utc, false, false, false, true, true);
        }
        */

        bool AddSourceCurrency(Cash newSourceCurrency)
        {
            return false;
        }

        bool RemoveSourceCurrency(Cash oldSourceCurrency)
        {
            return false;
        }

        bool AddPair(Security pair, string market = null)
        {
            return false;
        }

        bool RemovePair(Security pair, string market = null)
        {
            return false;
        }

        bool ContainsAllNeededSecurities()
        {
            return false;
        }

        decimal Update(string market, Security currency, decimal LastPrice, decimal Volume24)
        {
            return 0m;
        }

        decimal GetPrice(Cash cash)
        {
            return 0m;
        }

        ConversionRatePath GetPath(Cash from, Cash to)
        {

            return null;
        }

        public Graph BuildGraph()
        {
            Graph graph = new Graph();

            return graph;
        }

        public void Recalculate()
        {
            // Stage 1

            // Stage 2
        }
    }
}