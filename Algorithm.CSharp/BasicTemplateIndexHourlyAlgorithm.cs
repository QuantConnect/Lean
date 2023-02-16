using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression for running an Index algorithm with Hourly data
    /// </summary>
    public class BasicTemplateIndexHourlyAlgorithm : BasicTemplateIndexDailyAlgorithm
    {
        protected override Resolution Resolution => Resolution.Hour;
        protected override int ExpectedBarCount => base.ExpectedBarCount * 7;

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
        public override long DataPoints => 391;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public override int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "71"},
            {"Average Win", "1.28%"},
            {"Average Loss", "-0.06%"},
            {"Compounding Annual Return", "-20.546%"},
            {"Drawdown", "1.800%"},
            {"Expectancy", "-0.402"},
            {"Net Profit", "-0.922%"},
            {"Sharpe Ratio", "-2.856"},
            {"Probabilistic Sharpe Ratio", "22.230%"},
            {"Loss Rate", "97%"},
            {"Win Rate", "3%"},
            {"Profit-Loss Ratio", "19.95"},
            {"Alpha", "-0.155"},
            {"Beta", "0.025"},
            {"Annual Standard Deviation", "0.053"},
            {"Annual Variance", "0.003"},
            {"Information Ratio", "-2.07"},
            {"Tracking Error", "0.121"},
            {"Treynor Ratio", "-6.089"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$200000.00"},
            {"Lowest Capacity Asset", "SPX XL80P3GHDZXQ|SPX 31"},
            {"Fitness Score", "0.01"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "-6.834"},
            {"Return Over Maximum Drawdown", "-10.862"},
            {"Portfolio Turnover", "0.327"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "9e974939d13fd3255c6291a65d2c1eb9"}
        };
    }
}
