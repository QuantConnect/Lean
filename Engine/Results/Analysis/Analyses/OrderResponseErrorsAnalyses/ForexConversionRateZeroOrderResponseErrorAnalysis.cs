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
using QuantConnect.Lean.Engine.Results.Analysis.Analyses.Messages;

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    /// <summary>
    /// Detects zero Forex conversion rate errors.
    /// </summary>
    public class ForexConversionRateZeroOrderResponseErrorAnalysis : MessageAnalysis
    {
        public override string Issue { get; } = "The algorithm tried to place an order for a Forex or Crypto pair but LEAN could't convert the value of the base currency to your account currency.";

        public override int Weight { get; } = 88;

        protected override string[] ExpectedMessageText { get; } =
        [
            ": requires ",
            " and ",
            " to have non-zero conversion rates. This can be caused by lack of data.",
        ];

        protected override List<string> Solutions(Language language) =>
        [
            "This error usually indicates a lack of data. Check if there is some data missing.",
        ];
    }
}
