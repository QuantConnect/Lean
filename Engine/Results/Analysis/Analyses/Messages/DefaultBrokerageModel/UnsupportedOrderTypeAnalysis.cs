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
    /// <summary>
    /// Detects brokerage model rejections where the submitted or updated order type is not supported.
    /// </summary>
    public class UnsupportedOrderTypeAnalysis : MessageAnalysis
    {
        public override string Issue { get; } = "The algorithm tried to submit or update an order type that is not supported by the brokerage model.";

        public override int Weight { get; } = 0;

        protected override string[] ExpectedMessageText { get; } =
        [
            " does not support ",
            " order type. Only supports ",
        ];

        protected override List<string> Solutions(Language _) =>
        [
            "Only submit or update order types that the brokerage model supports.",
            "Change the brokerage model to one that supports the types of orders you're placing.",
        ];
    }
}
