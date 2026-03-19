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

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses.Messages.BinanceBrokerageModel
{
    /// <summary>
    /// Detects Binance brokerage model rejections due to an unsupported order type.
    /// </summary>
    public class UnsupportedOrderTypeWithLinkToSupportedTypesAnalysis : MessageAnalysis
    {
        public override string Issue { get; } = "The algorithm tried to place an order type that is not supported by the Binance brokerage model.";

        public override int Weight { get; } = 0;

        protected override string[] ExpectedMessageText { get; } =
        [
            "The Binance brokerage does not support ",
            " order type. Supported order types are: ",
        ];

        protected override List<string> Solutions(Language _) =>
        [
            "Only submit order types that Binance supports. " +
            "See https://www.quantconnect.com/docs/v2/writing-algorithms/reality-modeling/brokerages/supported-models/binance for supported order types.",
        ];
    }
}
