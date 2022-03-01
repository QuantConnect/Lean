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
*/

using Newtonsoft.Json.Linq;
using NUnit.Framework;
using QuantConnect.Algorithm.CSharp;
using QuantConnect.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace QuantConnect.Tests
{
    [TestFixture, Category("ResearchRegressionTests")]
    public class ResearchRegressionTests
    {
        [Test, TestCaseSource(nameof(GetResearchRegressionTestParameters))]
        public void ResearchRegression(ResearchRegressionTestParameters parameters)
        {
            var args = $"\"{parameters.NotebookPath}\" \"{parameters.NotebookOutputPath}\" --log-output --cwd {Directory.GetCurrentDirectory()}";

            // Use ProcessStartInfo class
            var startInfo = new ProcessStartInfo("papermill", args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                WorkingDirectory = Directory.GetCurrentDirectory()
            };

            var process = new Process
            {
                StartInfo = startInfo,
            };
            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            process.WaitForExit();
            process.Dispose();
            var actualOutput = File.ReadAllText(parameters.NotebookOutputPath);
            var actualCells = JToken.Parse(actualOutput)["cells"];
            var expeectedCells = JToken.Parse(parameters.ExpectedOutput)["cells"];
            var expectedAndActual = expeectedCells.Zip(actualCells, (e, a) => new { Expected = e, Actual = a });
            foreach (var item in expectedAndActual)
            {
                Assert.AreEqual(item.Expected["source"], item.Actual["source"]);
                Assert.AreEqual(item.Expected["outputs"], item.Actual["outputs"]);
            }
        }

        public class ResearchRegressionTestParameters
        {
            public readonly string NotebookName;
            public readonly string NotebookPath;
            public readonly string ExpectedOutput;
            public readonly string NotebookOutputPath;

            public ResearchRegressionTestParameters(string notebookName, string notebookPath, string expectedOutput)
            {
                NotebookName = notebookName;
                NotebookPath = notebookPath;
                ExpectedOutput = expectedOutput;
                NotebookOutputPath = notebookPath.Split(".")[0] + "-output" + ".ipynb";
            }
        }

        private static TestCaseData[] GetResearchRegressionTestParameters()
        {
            TestGlobals.Initialize();

            // since these are static test cases, they are executed before test setup
            AssemblyInitialize.AdjustCurrentDirectory();

            // find all research regression algorithms in Algorithm.CSharp
            var result =
            (
                from type in typeof(BasicTemplateResearch).Assembly.GetTypes()
                where typeof(IRegressionResearchDefinition).IsAssignableFrom(type)
                where !type.IsAbstract                          // non-abstract
                where type.GetConstructor(Array.Empty<Type>()) != null  // has default ctor
                let instance = (IRegressionResearchDefinition)Activator.CreateInstance(type)
                let path = Path.GetFullPath(Path.Combine(Path.Combine(type.Assembly.Location, @"../"), type.Name + ".ipynb"))
                select new ResearchRegressionTestParameters(type.Name, path, instance.ExpectedOutput)
            )
            .OrderBy(x => x.NotebookName)
            // generate test cases from test parameters
            .Select(x => new TestCaseData(x).SetName(x.NotebookName))
            .ToArray();
            return result;
        }
    }
}
