using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    public class PersistentCustomDataUniverseRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _universeSymbol;
        private bool _dataReceived;

        public override void Initialize()
        {
            SetStartDate(2018, 1, 3);
            SetEndDate(2018, 1, 20);

            var universe = AddUniverse<StockDataSource>("my-stock-data-source", Resolution.Daily, UniverseSelector);
            _universeSymbol = universe.Symbol;
        }

        private IEnumerable<Symbol> UniverseSelector(IEnumerable<BaseData> data)
        {
            foreach (var item in data.OfType<StockDataSource>())
            {
                yield return item.Symbol;
            }
        }

        public override void OnData(Slice slice)
        {
            _dataReceived = true;
            if (!slice.ContainsKey(_universeSymbol))
            {
                throw new RegressionTestException("OnData did not receive data for the universe symbol.");
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_dataReceived)
            {
                throw new RegressionTestException("No data was received after the universe selection.");
            }
        }

        /// <summary>
        /// Our custom data type that defines where to get and how to read our backtest and live data.
        /// </summary>
        class StockDataSource : BaseData
        {
            private const string Url = @"https://www.dropbox.com/s/ae1couew5ir3z9y/daily-stock-picker-backtest.csv?dl=1";

            public List<string> Symbols { get; set; }

            public StockDataSource()
            {
                Symbols = new List<string>();
            }

            public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
            {
                return new SubscriptionDataSource(Url, SubscriptionTransportMedium.RemoteFile);
            }

            public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
            {
                try
                {
                    // create a new StockDataSource and set the symbol using config.Symbol
                    var stocks = new StockDataSource { Symbol = config.Symbol };
                    // break our line into csv pieces
                    var csv = line.ToCsv();
                    if (isLiveMode)
                    {
                        // our live mode format does not have a date in the first column, so use date parameter
                        stocks.Time = date;
                        stocks.Symbols.AddRange(csv);
                    }
                    else
                    {
                        // our backtest mode format has the first column as date, parse it
                        stocks.Time = DateTime.ParseExact(csv[0], "yyyyMMdd", null);
                        // any following comma separated values are symbols, save them off
                        stocks.Symbols.AddRange(csv.Skip(1));
                    }
                    return stocks;
                }
                // return null if we encounter any errors
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 26024;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "0"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100000"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-12.133"},
            {"Tracking Error", "0.059"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
