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

namespace QuantConnect.Lean.Engine.Results.Analysis.Messages.InteractiveBrokersBrokerageModel
{

    public class UnsupportedExerciseForIndexAndCashSettledOptionsAnalysis : MessageAnalysis
    {
        protected override string[] ExpectedMessageText { get; } =
        [
            "InteractiveBrokers does not support exercising index and cash-settled options.",
        ];


        protected override List<string> PotentialSolutions(Language _) =>
        [
            "Interactive Brokers does not support manually exercising index and cash-settled options. " +
            "These options are automatically exercised at expiry. Remove the manual exercise call.",
        ];
    }
}
