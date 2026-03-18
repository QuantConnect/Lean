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
    /// Detects MarketOnClose orders submitted too early in the day.
    /// </summary>
    public class MarketOnCloseOrderTooLateOrderResponseErrorAnalysis : MessageAnalysis
    {
        public override string Issue { get; } = "The algorithm tried to place a market on close (MOC) order too early in the trading day.";

        public override int Weight { get; } = 52;

        protected override string[] ExpectedMessageText { get; } =
        [
            "MarketOnClose orders must be placed within ",
            " before market close. Override this TimeSpan buffer by setting Orders.MarketOnCloseOrder.SubmissionTimeBuffer in QCAlgorithm.Initialize().",
        ];

        protected override List<string> Solutions(Language language)
        {
            var bufferProp = language == Language.Python ? "submission_time_buffer" : "SubmissionTimeBuffer";
            var exampleCode = language == Language.Python
                ? "```\nMarketOnCloseOrder.submission_time_buffer = timedelta(minutes=10)\n```"
                : "```\nOrders.MarketOnCloseOrder.SubmissionTimeBuffer = TimeSpan.FromMinutes(10);\n```";

            return
            [
                "Place the MOC order closer to the market close or adjust the submission time buffer. " +
                "By default, you must place MOC orders at least 15.5 minutes before the close, but some exchanges let you submit them closer to the market closing time. " +
                $"To adjust the buffer period that's required, set the `MarketOnCloseOrder.{bufferProp}` property.\n" +
                exampleCode,
            ];
        }
    }
}
