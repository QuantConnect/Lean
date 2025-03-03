using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Algorithm.CSharp
{
    public class StochasticIndicatorAndSubIndicatorsWarmUpRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private bool _dataPointsReceived;
        private Symbol _spy;
        private Stochastic _sto;
        private Stochastic _stoHistory;

        public override void Initialize()
        {
            SetStartDate(2020, 1, 1);
            SetEndDate(2020, 2, 1);

            _spy = AddEquity("SPY", Resolution.Hour).Symbol;

            var dailyConsolidator = new TradeBarConsolidator(TimeSpan.FromDays(1));
            _sto = new Stochastic("FIRST", 14, 3, 3);
            RegisterIndicator(_spy, _sto, dailyConsolidator);

            WarmUpIndicator(_spy, _sto, TimeSpan.FromDays(1));

            _stoHistory = new Stochastic("SECOND", 14, 3, 3);
            RegisterIndicator(_spy, _stoHistory, dailyConsolidator);

            var history = History(_spy, _stoHistory.WarmUpPeriod, Resolution.Daily);

            // Warm up STO indicator
            foreach (var bar in history.TakeLast(_stoHistory.WarmUpPeriod))
            {
                _stoHistory.Update(bar);
            }

            var indicators = new List<IIndicator>() { _sto, _stoHistory };

            foreach (var indicator in indicators)
            {
                if (!indicator.IsReady)
                {
                    throw new RegressionTestException($"{indicator.Name} should be ready, but it is not. Number of samples: {indicator.Samples}");
                }
            }


        }

        public override void OnData(Slice slice)
        {
            if (IsWarmingUp) return;

            if (slice.ContainsKey(_spy))
            {
                _dataPointsReceived = true;

                if (_sto.StochK.Current.Value != _stoHistory.StochK.Current.Value)
                {
                    throw new RegressionTestException($"Stoch K values of indicators differ: {_sto.Name}.StochK: {_sto.StochK.Current.Value} | {_stoHistory.Name}.StochK: {_stoHistory.StochK.Current.Value}");
                }

                if (_sto.StochD.Current.Value != _stoHistory.StochD.Current.Value)
                {
                    throw new RegressionTestException($"Stoch D values of indicators differ: {_sto.Name}.StochD: {_sto.StochD.Current.Value} | {_stoHistory.Name}.StochD: {_stoHistory.StochD.Current.Value}");
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_dataPointsReceived)
            {
                throw new RegressionTestException("No data points received");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 302;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 36;

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
            {"Information Ratio", "-0.016"},
            {"Tracking Error", "0.101"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
