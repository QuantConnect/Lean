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
    public class AlgorithmWarmingUpOrderResponseErrorAnalysisTests
    {
        // The engine formats the method names by algorithm language, so the same rejection
        // produces a different message for C# and Python algorithms
        private const string CSharpMessage = "This operation is not allowed in Initialize or during warm up: " +
            "OrderRequest.Submit. Please move this code to the OnWarmupFinished() method.";

        private const string PythonMessage = "This operation is not allowed in initialize or during warm up: " +
            "OrderRequest.submit. Please move this code to the on_warmup_finished() method.";

        [TestCase(CSharpMessage, Language.CSharp)]
        [TestCase(PythonMessage, Language.Python)]
        public void MatchesTheLanguageFormattedWarmUpRejectionMessage(string message, Language language)
        {
            var analysis = new AlgorithmWarmingUpOrderResponseErrorAnalysis();

            var finding = analysis.Run(new[] { "Some other log line", message }, language).Single();

            Assert.AreEqual(message, finding.Sample);
            Assert.IsNotEmpty(finding.Solutions);
        }

        [Test]
        public void DoesNotMatchUnrelatedMessages()
        {
            var analysis = new AlgorithmWarmingUpOrderResponseErrorAnalysis();

            var finding = analysis.Run(new[] { "Some other log line" }, Language.CSharp).Single();

            Assert.IsNull(finding.Sample);
            Assert.IsEmpty(finding.Solutions);
        }
    }
}
