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
        public override Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 401;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public override int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "81"},
            {"Average Win", "1.28%"},
            {"Average Loss", "-0.06%"},
            {"Compounding Annual Return", "-20.546%"},
            {"Drawdown", "1.800%"},
            {"Expectancy", "-0.402"},
            {"Start Equity", "1000000"},
            {"End Equity", "990775"},
            {"Net Profit", "-0.922%"},
            {"Sharpe Ratio", "-2.903"},
            {"Sortino Ratio", "-6.081"},
            {"Probabilistic Sharpe Ratio", "22.230%"},
            {"Loss Rate", "97%"},
            {"Win Rate", "3%"},
            {"Profit-Loss Ratio", "19.95"},
            {"Alpha", "-0.157"},
            {"Beta", "0.025"},
            {"Annual Standard Deviation", "0.053"},
            {"Annual Variance", "0.003"},
            {"Information Ratio", "-2.07"},
            {"Tracking Error", "0.121"},
            {"Treynor Ratio", "-6.189"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$300000.00"},
            {"Lowest Capacity Asset", "SPX XL80P3GHDZXQ|SPX 31"},
            {"Portfolio Turnover", "24.63%"},
            {"OrderListHash", "44325fc1fdebb8e54f64a3f6e8a4bcd7"}
        };
    }
}
