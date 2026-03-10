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

namespace QuantConnect.Lean.Engine.Results.Analysis.Messages.TradierBrokerageModel
{

    public class ExtendedMarketHoursTradingNotSupportedOutsideExtendedSessionAnalysis : MessageAnalysis
    {
        protected override string[] ExpectedMessageText { get; } =
        [
            "Tradier does not support extended market hours trading outside of the extended session.",
        ];


        protected override List<string> PotentialSolutions(Language language) =>
        [
            "Tradier only supports extended hours trading during the pre-market and after-hours sessions. " +
            $"Use the `{CodeByLanguage.OutsideRegularTradingHours[language]}` order property to place extended-hours orders, " +
            "and only submit them during the extended trading sessions.",
        ];
    }
}
