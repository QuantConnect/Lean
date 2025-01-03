using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QuantConnect.Algorithm.Framework.Portfolio.SignalExports;
using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;
using QuantConnect.Securities.Option;

namespace QuantConnect.Algorithm.CSharp.RegressionTests
{
    public class Collective2IndexOptionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private const string _collective2ApiKey = "8923ABC5-F221-4458-AF97-7CECF5BE3106";
        private const int _collective2SystemId = 145772785;

        private ExponentialMovingAverage _fast;
        private Symbol _spxw;
        private Symbol _spxwOption;

        public override void Initialize()
        {
            SetStartDate(2021, 1, 4);
            SetEndDate(2021, 1, 18);
            SetCash(100000);

            _spxw = AddIndex("SPXW", Resolution.Minute).Symbol;

            _spxwOption = QuantConnect.Symbol.CreateOption(
                _spxw,
                Market.USA,
                OptionStyle.European,
                OptionRight.Call,
                3200m,
                new DateTime(2021, 1, 15));

            var foo = AddIndexOptionContract(_spxwOption, Resolution.Minute);

            _fast = EMA("SPXW", 10, Resolution.Minute);

            // Configurar Collective2
            var test = new Collective2SignalExport(_collective2ApiKey, _collective2SystemId);
            SignalExport.AddSignalExportProviders(test);
            SetWarmUp(100);
        }

        public override void OnData(Slice slice)
        {
            //SetHoldings("SPXW", 0.1);
            //var chain = slice.OptionChains[_spxwOption];
            var test = Portfolio;
            if (!Portfolio[_spxw].Invested)
            {
                MarketOrder(_spxw, -1);
                SignalExport.SetTargetPortfolioFromPortfolio();
            }
        }
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.RuntimeError;

        public bool CanRunLocally { get; } = true;

        public virtual List<Language> Languages { get; } = new() { Language.CSharp };

        public long DataPoints => 80;

        public int AlgorithmHistoryDataPoints => 0;

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
            {"Information Ratio", "-9.604"},
            {"Tracking Error", "0.097"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
