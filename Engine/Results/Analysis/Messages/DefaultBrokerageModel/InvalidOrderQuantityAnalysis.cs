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
using QuantConnect;
using QuantConnect.Lean.Engine.Results.Analysis.Analyses;
using QuantConnect.Lean.Engine.Results.Analysis.Utils;

namespace QuantConnect.Lean.Engine.Results.Analysis.Messages.DefaultBrokerageModel
{

    public class InvalidOrderQuantityAnalysis : MessageAnalysis
    {
        protected override string[] ExpectedMessageText { get; } =
        [
            "The minimum order size (in quote currency) for ",
            " is ",
            ". Order quantity was ",
        ];


        protected override List<string> PotentialSolutions(Language language) =>
        [
            "This message occurs when the absolute order size (in the quote currency) is less than the security's minimum order size in the Symbol Properties Database. " +
            "Before placing orders, ensure their size exceeds the minimum order size.",

            "Increase the starting cash so trades are larger.",

            $"Increase the {CodeByLanguage.MinimumOrderMarginPortfolioPercentage[language]} setting.",
        ];
    }
}
