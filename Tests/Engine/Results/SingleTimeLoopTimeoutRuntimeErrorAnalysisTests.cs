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
using System.Linq;
using NUnit.Framework;
using QuantConnect.Lean.Engine.Results.Analysis.Analyses;

namespace QuantConnect.Tests.Engine.Results
{
    [TestFixture]
    public class SingleTimeLoopTimeoutRuntimeErrorAnalysisTests
    {
        private const string SingleTimeLoopError =
            "20240101 15:30:00.000 Runtime Error: Algorithm took longer than 10 minutes on a single time loop. CurrentTimeStepElapsed: 10.0 minutes";

        private const string SingleTimeLoopWithAdditionalTimeError =
            "Runtime Error: Algorithm took longer than 10 minutes on a single time loop. " +
            "An additional 30 minutes were also allocated and consumed. CurrentTimeStepElapsed: 40.1 minutes";

        private const string OperationCanceledError = "20240101 15:30:00.000 Runtime Error: Operation was canceled";

        private const string MaximumRuntimeError =
            "Runtime Error: Execution Security Error: Operation timed out - 1440 minutes max. Check for recursive loops.";

        private const string MaximumRuntimeEngineError =
            "Runtime Error: Failed to complete algorithm within 86400 seconds. Please make it run faster.";

        [TestCase(SingleTimeLoopError)]
        [TestCase(SingleTimeLoopWithAdditionalTimeError)]
        [TestCase(OperationCanceledError)]
        [TestCase(MaximumRuntimeError)]
        [TestCase(MaximumRuntimeEngineError)]
        public void DetectsEachTimeoutRuntimeErrorFromState(string runtimeError)
        {
            var state = new Dictionary<string, string> { ["RuntimeError"] = runtimeError };

            var finding = new SingleTimeLoopTimeoutRuntimeErrorAnalysis().Run(state, [], Language.CSharp).Single();

            Assert.AreEqual(nameof(SingleTimeLoopTimeoutRuntimeErrorAnalysis), finding.Name);
            Assert.AreEqual(runtimeError, finding.Sample);
            Assert.IsNotEmpty(finding.Solutions);
        }

        [Test]
        public void DetectsRuntimeErrorFromLogsWhenStateIsMissing()
        {
            var logs = new List<string>
            {
                "20240101 15:29:00.000 Launching analysis for the algorithm",
                SingleTimeLoopError,
            };

            var finding = new SingleTimeLoopTimeoutRuntimeErrorAnalysis().Run(null, logs, Language.CSharp).Single();

            Assert.AreEqual(SingleTimeLoopError, finding.Sample);
            Assert.IsNotEmpty(finding.Solutions);
        }

        [Test]
        public void IgnoresLogLinesThatAreNotRuntimeErrors()
        {
            // These mention cancellation and timeouts but are not the algorithm's runtime error
            var logs = new List<string>
            {
                "20240101 15:29:00.000 Isolator.ExecuteWithTimeLimit(): Operation was canceled",
                "20240101 15:29:00.000 The download operation timed out after 5 minutes max wait",
            };

            var finding = new SingleTimeLoopTimeoutRuntimeErrorAnalysis().Run(new Dictionary<string, string>(), logs, Language.CSharp).Single();

            Assert.IsNull(finding.Sample);
            Assert.IsEmpty(finding.Solutions);
        }

        [TestCase("Runtime Error: System.DivideByZeroException: Attempted to divide by zero.")]
        [TestCase("")]
        public void DoesNotFlagOtherRuntimeErrorsOrCleanRuns(string runtimeError)
        {
            var state = new Dictionary<string, string> { ["RuntimeError"] = runtimeError };

            var finding = new SingleTimeLoopTimeoutRuntimeErrorAnalysis().Run(state, [], Language.CSharp).Single();

            Assert.IsNull(finding.Sample);
            Assert.IsEmpty(finding.Solutions);
        }

        [TestCase(Language.CSharp, "Train", "OnData")]
        [TestCase(Language.Python, "train", "on_data")]
        public void FormatsCodeReferencesForTheAlgorithmLanguage(Language language, string trainMethod, string onDataMethod)
        {
            var state = new Dictionary<string, string> { ["RuntimeError"] = SingleTimeLoopError };

            var solutions = new SingleTimeLoopTimeoutRuntimeErrorAnalysis().Run(state, [], language).Single().Solutions;

            Assert.IsTrue(solutions.Any(solution => solution.Contains($"`{trainMethod}`")));
            Assert.IsTrue(solutions.Any(solution => solution.Contains($"`{onDataMethod}`")));
        }

        [Test]
        public void PythonSolutionsIncludeVectorizationAdvice()
        {
            var state = new Dictionary<string, string> { ["RuntimeError"] = SingleTimeLoopError };

            var csharpSolutions = new SingleTimeLoopTimeoutRuntimeErrorAnalysis().Run(state, [], Language.CSharp).Single().Solutions;
            var pythonSolutions = new SingleTimeLoopTimeoutRuntimeErrorAnalysis().Run(state, [], Language.Python).Single().Solutions;

            Assert.IsFalse(csharpSolutions.Any(solution => solution.Contains("numpy")));
            Assert.IsTrue(pythonSolutions.Any(solution => solution.Contains("numpy")));
        }
    }
}
