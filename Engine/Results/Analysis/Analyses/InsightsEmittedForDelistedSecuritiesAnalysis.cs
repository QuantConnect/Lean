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
    /// Detects the QC warning about emitting insights for delisted securities.
    /// </summary>
    public class InsightsEmittedForDelistedSecuritiesAnalysis : MessageAnalysis
    {
        public override string Issue { get; } = "The algorithm emitted insights for delisted securities.";

        public override int Weight { get; } = 80;

        protected override string[] ExpectedMessageText { get; } =
        [
            "QCAlgorithm.EmitInsights(): Warning: cannot emit insights for delisted securities, these will be discarded",
        ];

        protected override List<string> Solutions(Language language) =>
        [
            "Before you emit an insight for a security, check if it's tradable.",
        ];
    }
}
