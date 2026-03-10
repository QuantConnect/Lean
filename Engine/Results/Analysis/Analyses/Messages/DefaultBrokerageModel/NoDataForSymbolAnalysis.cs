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

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses.Messages.DefaultBrokerageModel
{

    public class NoDataForSymbolAnalysis : MessageAnalysis
    {
        protected override string[] ExpectedMessageText { get; } =
        [
            "There is no data for the provided symbol ",
        ];


        protected override List<string> PotentialSolutions(Language _) =>
        [
            "This error occurs when you try to place an order for a security but there is no data for the security. " +
            "Check if you've subscribed to the correct security and if the data is available.",
        ];
    }
}
