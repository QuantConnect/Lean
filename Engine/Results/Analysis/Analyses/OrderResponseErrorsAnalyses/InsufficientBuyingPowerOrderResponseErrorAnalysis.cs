/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
*/
using QuantConnect.Algorithm;
using System.Collections.Generic;

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    /// <summary>
    /// Detects insufficient-buying-power order rejections.
    /// </summary>
    public class InsufficientBuyingPowerOrderResponseErrorAnalysis : OrderResponseErrorAnalysis
    {
        /// <summary>
        /// Gets a description of the insufficient buying power issue.
        /// </summary>
        public override string Issue { get; } = "One of the following cases occurred:\n" +
            " - The algorithm tried to place an order but the buying power model determined you couldn't afford it\n" +
            " - The algorithm tried to place a market on open order with daily data";

        /// <summary>
        /// Gets the priority weight for this analysis.
        /// </summary>
        public override int Weight { get; } = 97;

        /// <summary>
        /// Gets the message fragment that identifies an insufficient buying power error.
        /// </summary>
        protected override string[] ExpectedMessageText { get; } =
        [
            "Insufficient buying power to complete order",
        ];

        /// <summary>
        /// Gets solutions for ensuring sufficient margin or adjusting the buying power buffer.
        /// </summary>
        protected override List<string> Solutions(Language language) =>
        [
            "This error occurs when you place an order but the buying power model determines you can't afford it.\n" +
            "To avoid this order response error, ensure you have enough margin remaining to cover the initial margin requirements of the order before placing it.\n" +
            "Example for regular orders:\n" +
            (language == Language.Python
                ? "```\ndef on_data(self, slice: Slice) -> None:\n    security = self.securities[\"SPY\"]\n    quantity = 100\n    parameter = InitialMarginParameters(security, quantity)\n    initial_margin = security.buying_power_model.get_initial_margin_requirement(parameter)\n    if self.portfolio.margin_remaining >= initial_margin.value:\n        self.market_order(security.symbol, quantity)\n    else:\n        self.debug(\"You don't have sufficient margin for this order.\")\n```"
                : "```\npublic override void OnData(Slice slice)\n{\n    var security = Securities[\"SPY\"];\n    var quantity = 100m;\n    var parameter = new InitialMarginParameters(security, quantity);\n    var initialMargin = security.BuyingPowerModel.GetInitialMarginRequirement(parameter);\n    if (Portfolio.MarginRemaining >= initialMargin.Value)\n    {\n        MarketOrder(security.Symbol, quantity);\n    }\n    else\n    {\n        Debug(\"You don't have sufficient margin for this order.\");\n    }\n}\n```"),

            "This error also commonly occurs when you place a market on open order with daily data. " +
            $"If you place the order with `{FormatCode(nameof(QCAlgorithm.SetHoldings), language)}` or use `{FormatCode(nameof(QCAlgorithm.CalculateOrderQuantity), language)}` to determine the order quantity, LEAN calculates the order quantity based on the market close price. " +
            "If the open price on the following day makes your order more expensive, then you may have insufficient buying power. " +
            "To avoid the order response error in this case, either use intraday data and place trades when the market is open or adjust your buying power buffer.\n" +
            (language == Language.Python
                ? "```\n# Set the cash buffer to 5%\nself.settings.free_portfolio_value_percentage = 0.05\n\n# Set the cash buffer to $10,000\nself.settings.free_portfolio_value = 10000\n```"
                : "```\n// Set the cash buffer to 5%\nSettings.FreePortfolioValuePercentage = 0.05m;\n\n// Set the cash buffer to $10,000\nSettings.FreePortfolioValue = 10000m;\n```") + "\n" +
            $"If you use `{FormatCode(nameof(AlgorithmSettings.FreePortfolioValuePercentage), language)}`, you must set it in the `{FormatCode(nameof(QCAlgorithm.Initialize), language)}` or `{FormatCode(nameof(QCAlgorithm.PostInitialize), language)}` event handler. " +
            $"If you use `{FormatCode(nameof(AlgorithmSettings.FreePortfolioValue), language)}`, you must set it after the `{FormatCode(nameof(QCAlgorithm.PostInitialize), language)}` event handler.",
        ];
    }
}
