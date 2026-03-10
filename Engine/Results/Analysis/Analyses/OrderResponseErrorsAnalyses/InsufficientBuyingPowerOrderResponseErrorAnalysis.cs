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
using System.Collections.Generic;
using QuantConnect.Lean.Engine.Results.Analysis.Utils;

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    /// <summary>
    /// Detects insufficient-buying-power order rejections.
    /// Error code: OrderResponseErrorCode.INSUFFICIENT_BUYING_POWER (-3)
    /// </summary>
    public class InsufficientBuyingPowerOrderResponseErrorAnalysis : OrderResponseErrorAnalysis
    {
        protected override string[] ExpectedMessageText { get; } =
        [
            "Insufficient buying power to complete order",
        ];

        protected override List<string> PotentialSolutions(Language language) =>
        [
            "This error occurs when you place an order but the buying power model determines you can't afford it.\n" +
            "To avoid this order response error, ensure you have enough margin remaining to cover the initial margin requirements of the order before placing it.\n" +
            "Example for regular orders:\n" +
            (language == Language.Python
                ? "```\ndef on_data(self, slice: Slice) -> None:\n    security = self.securities[\"SPY\"]\n    quantity = 100\n    parameter = InitialMarginParameters(security, quantity)\n    initial_margin = security.buying_power_model.get_initial_margin_requirement(parameter)\n    if self.portfolio.margin_remaining >= initial_margin.value:\n        self.market_order(security.symbol, quantity)\n    else:\n        self.debug(\"You don't have sufficient margin for this order.\")\n```"
                : "```\npublic override void OnData(Slice slice)\n{\n    var security = Securities[\"SPY\"];\n    var quantity = 100m;\n    var parameter = new InitialMarginParameters(security, quantity);\n    var initialMargin = security.BuyingPowerModel.GetInitialMarginRequirement(parameter);\n    if (Portfolio.MarginRemaining >= initialMargin.Value)\n    {\n        MarketOrder(security.Symbol, quantity);\n    }\n    else\n    {\n        Debug(\"You don't have sufficient margin for this order.\");\n    }\n}\n```"),

            "This error also commonly occurs when you place a market on open order with daily data. " +
            $"If you place the order with `{CodeByLanguage.SetHoldings[language]}` or use `{CodeByLanguage.CalculateOrderQuantity[language]}` to determine the order quantity, LEAN calculates the order quantity based on the market close price. " +
            "If the open price on the following day makes your order more expensive, then you may have insufficient buying power. " +
            "To avoid the order response error in this case, either use intraday data and place trades when the market is open or adjust your buying power buffer.\n" +
            (language == Language.Python
                ? "```\n# Set the cash buffer to 5%\nself.settings.free_portfolio_value_percentage = 0.05\n\n# Set the cash buffer to $10,000\nself.settings.free_portfolio_value = 10000\n```"
                : "```\n// Set the cash buffer to 5%\nSettings.FreePortfolioValuePercentage = 0.05m;\n\n// Set the cash buffer to $10,000\nSettings.FreePortfolioValue = 10000m;\n```") + "\n" +
            $"If you use `{CodeByLanguage.FreePortfolioValuePercentage[language]}`, you must set it in the `{CodeByLanguage.Initialize[language]}` or `{CodeByLanguage.PostInitialize[language]}` event handler. " +
            $"If you use `{CodeByLanguage.FreePortfolioValue[language]}`, you must set it after the `{CodeByLanguage.PostInitialize[language]}` event handler.",
        ];
    }
}
