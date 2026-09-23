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

using System.Linq;
using NUnit.Framework;
using QuantConnect.Lean.Engine.Results.Analysis.Analyses;

namespace QuantConnect.Tests.Engine.Results
{
    [TestFixture]
    public class MarketOnCloseOrderTooLateOrderResponseErrorAnalysisTests
    {
        private const string RejectionMessage = "MarketOnClose orders must be placed within 00:15:30 before market close." +
            " Override this TimeSpan buffer by setting Orders.MarketOnCloseOrder.SubmissionTimeBuffer in QCAlgorithm.Initialize().";

        [Test]
        public void MatchesTheTooLateRejectionMessage()
        {
            var analysis = new MarketOnCloseOrderTooLateOrderResponseErrorAnalysis();

            var finding = analysis.Run(new[] { "Some other log line", RejectionMessage }, Language.CSharp).Single();

            Assert.AreEqual(RejectionMessage, finding.Sample);
            Assert.IsNotEmpty(finding.Solutions);
        }

        [Test]
        public void DoesNotMatchUnrelatedMessages()
        {
            var analysis = new MarketOnCloseOrderTooLateOrderResponseErrorAnalysis();

            var finding = analysis.Run(new[] { "Some other log line" }, Language.CSharp).Single();

            Assert.IsNull(finding.Sample);
            Assert.IsEmpty(finding.Solutions);
        }
    }
}
