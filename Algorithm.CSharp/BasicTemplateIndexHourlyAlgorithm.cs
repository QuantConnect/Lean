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
            {"Average Win", "0%"},
            {"Average Loss", "-0.11%"},
            {"Compounding Annual Return", "-18.082%"},
            {"Drawdown", "0.800%"},
            {"Expectancy", "-1"},
            {"Start Equity", "1000000"},
            {"End Equity", "991995"},
            {"Net Profit", "-0.800%"},
            {"Sharpe Ratio", "-5.01"},
            {"Sortino Ratio", "-2.603"},
            {"Probabilistic Sharpe Ratio", "0.018%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.151"},
            {"Beta", "0.149"},
            {"Annual Standard Deviation", "0.027"},
            {"Annual Variance", "0.001"},
            {"Information Ratio", "-2.39"},
            {"Tracking Error", "0.097"},
            {"Treynor Ratio", "-0.917"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$1800000.00"},
            {"Lowest Capacity Asset", "SPX XL80P3GHIA9A|SPX 31"},
            {"Portfolio Turnover", "5.58%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "1c7d841e0280e91b2297410fe2dbbc89"}
        };
    }
}
