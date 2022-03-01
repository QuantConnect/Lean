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
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Tests.Research.RegressionDefinitions;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace QuantConnect.Tests
{
    [TestFixture, Category("ResearchRegressionTests")]
    public class ResearchRegressionTests
    {
        // Update in config.json when template expected result needs to be updated
        private static readonly bool UpdateResearchRegressionOutput = Config.GetBool("research-regression-update-output", false);

        [Test, TestCaseSource(nameof(GetResearchRegressionTestParameters))]
        public void ResearchRegression(ResearchRegressionTestParameters parameters)
        {
            var actualOutput = RunResearchNotebookAndGetOutput(parameters.NotebookPath, parameters.NotebookOutputPath, Directory.GetCurrentDirectory());
            var actualCells = JToken.Parse(actualOutput)["cells"];
            var expectedCells = JToken.Parse(parameters.ExpectedOutput)["cells"];
            var expectedAndActualCells = expectedCells.Zip(actualCells, (e, a) => new { Expected = e, Actual = a });
            foreach (var cell in expectedAndActualCells)
            {
                // Assert Notebook Cell Input
                Assert.AreEqual(cell.Expected["source"], cell.Actual["source"]);

                // Assert Notebook Cell Output
                var expectedCellOutputs = cell.Expected["outputs"];
                var actualCellOutputs = cell.Actual["outputs"];
                if (expectedCellOutputs != null)
                {
                    if (actualCellOutputs == null)
                    {
                        Assert.Fail("Shouldn't be null when expected is not null");
                    }
                    // Iterate over all outputs for the given notebook cell
                    var expectedCellOutputsAndActualCellOutputs = expectedCellOutputs.Zip(actualCellOutputs, (e, a) => new { Expected = e, Actual = a });
                    foreach (var cellOutputsItem in expectedCellOutputsAndActualCellOutputs)
                    {
                        if (cellOutputsItem.Expected["data"] != null && cellOutputsItem.Expected["data"]["text/html"] != null)
                        {
                            continue;
                        }
                        else if (cellOutputsItem.Expected["name"]?.ToString() == "stdout" && cellOutputsItem.Expected["text"] != null)
                        {
                            if (!IsDeterministic(cellOutputsItem.Expected["text"].ToString()))
                            {
                                continue;
                            }
                        }
                        Assert.AreEqual(cellOutputsItem.Expected, cellOutputsItem.Actual);
                    }
                }
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

        private static void UpdateResearchRegressionOutputInSourceFile(string templateName, string notebookPath, string notebookoutputPath, string workingDirectoryForNotebook)
        {
            var templatePath = Directory.EnumerateFiles("../../Research/RegressionTemplates", $"*{templateName}.cs", SearchOption.AllDirectories).Single();
            var file = File.ReadAllLines(templatePath).ToList().GetEnumerator();
            var lines = new List<string>();
            while (file.MoveNext())
            {
                var line = file.Current;
                if (line == null)
                {
                    continue;
                }

                if (line.Contains("public string ExpectedOutput =>"))
                {
                    lines.Add(line);
                    var expectedOutput = RunResearchNotebookAndGetOutput(notebookPath, notebookoutputPath, workingDirectoryForNotebook);
                    expectedOutput = expectedOutput
                        .Replace("\\\\", "\\\\\\\\")
                        .Replace("\\\"", "\\\\\"")
                        .Replace("\"", "\\\"")
                        .Replace("\\n", "\\\n")
                        .Replace("\n", "\\n")
                        .Replace("\t", "\\t")
                        .Replace("\r", "\\r");
                    lines.Add($"            \"{expectedOutput}\";");

                    // now we skip existing expected statistics in file
                    while (file.MoveNext())
                    {
                        line = file.Current;
                        if (line != null && line.StartsWith("    }"))
                        {
                            lines.Add(line);
                            break;
                        }
                    }
                }
                else
                {
                    lines.Add(line);
                }
            }

            file.DisposeSafely();
            File.WriteAllLines(templatePath, lines);
        }

        private static string RunResearchNotebookAndGetOutput(string notebookPath, string notebookoutputPath, string workingDirectoryForNotebook)
        {
            var args = $"\"{notebookPath}\" \"{notebookoutputPath}\" --log-output --cwd {workingDirectoryForNotebook}";

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
            return File.ReadAllText(notebookoutputPath);
        }

        private static bool IsDeterministic(string input)
        {
            Regex rgxDateTime = new(@"(\d{4})(\d{2})(\d{2}) (\d{2}):(\d{2}):(\d{2}).(\d{3}) TRACE::");
            if (input.Contains("Initialize.csx"))
            {
                return false;
            }
            else if (input.Contains("Runtime.Initialize():"))
            {
                return false;
            }
            else if (input.Contains("PythonEngine.Initialize():"))
            {
                return false;
            }
            else if (rgxDateTime.IsMatch(input))
            {
                return false;
            }
            return true;
        }

        private static ResearchRegressionTestParameters[] GetResearchTemplates()
        {
            var result =
            (
                from type in typeof(BasicTemplateResearchPython).Assembly.GetTypes()
                where typeof(IRegressionResearchDefinition).IsAssignableFrom(type)
                where !type.IsAbstract                          // non-abstract
                where type.GetConstructor(Array.Empty<Type>()) != null  // has default ctor
                let instance = (IRegressionResearchDefinition)Activator.CreateInstance(type)
                let path = Path.GetFullPath(Path.Combine(Path.Combine(type.Assembly.Location, @"../"), type.Name + ".ipynb"))
                select new ResearchRegressionTestParameters(type.Name, path, instance.ExpectedOutput)
            )
            .Select(x => x)
            .ToArray();
            return result;
        }

        private static TestCaseData[] GetResearchRegressionTestParameters()
        {
            if (UpdateResearchRegressionOutput)
            {
                var templates = GetResearchTemplates();
                foreach (var template in templates)
                {
                    UpdateResearchRegressionOutputInSourceFile(template.NotebookName, template.NotebookPath, template.NotebookOutputPath, Directory.GetCurrentDirectory());
                }
            }

            TestGlobals.Initialize();

            // since these are static test cases, they are executed before test setup
            AssemblyInitialize.AdjustCurrentDirectory();
            var result = GetResearchTemplates()
                .OrderBy(x => x.NotebookName)
                // generate test cases from test parameters
                .Select(x => new TestCaseData(x).SetName(x.NotebookName))
                .ToArray();
            return result; ;
        }
    }
}
