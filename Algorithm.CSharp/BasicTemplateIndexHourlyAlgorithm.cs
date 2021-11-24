using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression for running an Index algorithm with Hourly data
    /// </summary>
    public class BasicTemplateIndexHourlyAlgorithm : BasicTemplateIndexDailyAlgorithm
    {
        protected override Resolution Resolution => Resolution.Hour;
        protected override int ExpectedBarCount => 96;

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public override bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public override Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "70"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.23%"},
            {"Compounding Annual Return", "-82.001%"},
            {"Drawdown", "8.000%"},
            {"Expectancy", "-1"},
            {"Net Profit", "-7.983%"},
            {"Sharpe Ratio", "-3.028"},
            {"Probabilistic Sharpe Ratio", "0.000%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.71"},
            {"Beta", "0.115"},
            {"Annual Standard Deviation", "0.231"},
            {"Annual Variance", "0.054"},
            {"Information Ratio", "-3.152"},
            {"Tracking Error", "0.248"},
            {"Treynor Ratio", "-6.094"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$310000.00"},
            {"Lowest Capacity Asset", "SPX XL80P3GHDZXQ|SPX 31"},
            {"Fitness Score", "0.029"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "-2.73"},
            {"Return Over Maximum Drawdown", "-10.335"},
            {"Portfolio Turnover", "0.301"},
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
            {"OrderListHash", "37beb84dea22dc720bb08a9ab057b484"}
        };
    }
}
