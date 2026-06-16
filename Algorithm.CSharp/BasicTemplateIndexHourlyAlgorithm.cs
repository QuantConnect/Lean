using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression for running an Index algorithm with Hourly data
    /// </summary>
    public class BasicTemplateIndexHourlyAlgorithm : BasicTemplateIndexDailyAlgorithm
    {
        protected override Resolution Resolution => Resolution.Hour;
        protected override int ExpectedBarCount => base.ExpectedBarCount * 8;

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public override bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public override List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 401;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public override int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "19"},
            {"Average Win", "1.26%"},
            {"Average Loss", "-0.12%"},
            {"Compounding Annual Return", "13.245%"},
            {"Drawdown", "0.600%"},
            {"Expectancy", "0.609"},
            {"Start Equity", "1000000"},
            {"End Equity", "1005025"},
            {"Net Profit", "0.502%"},
            {"Sharpe Ratio", "1.633"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "56.004%"},
            {"Loss Rate", "86%"},
            {"Win Rate", "14%"},
            {"Profit-Loss Ratio", "10.27"},
            {"Alpha", "0.087"},
            {"Beta", "0.07"},
            {"Annual Standard Deviation", "0.057"},
            {"Annual Variance", "0.003"},
            {"Information Ratio", "-0.013"},
            {"Tracking Error", "0.118"},
            {"Treynor Ratio", "1.342"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$1800000.00"},
            {"Lowest Capacity Asset", "SPX XL80P3GHIA9A|SPX 31"},
            {"Portfolio Turnover", "5.63%"},
            {"Drawdown Recovery", "10"},
            {"OrderListHash", "28ad69d262f8910ba550087b9af0bac8"}
        };
    }
}
