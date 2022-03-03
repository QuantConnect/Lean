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
            
            // Update expected result if required.
            if (UpdateResearchRegressionOutput)
            {
                UpdateResearchRegressionOutputInSourceFile(parameters.NotebookName, actualOutput);
            }
            var actualCells = JToken.Parse(CleanDispensableEscapeCharacters(actualOutput))["cells"];
            var expectedCells = JToken.Parse(parameters.ExpectedOutput)["cells"];
            var expectedAndActualCells = expectedCells.Zip(actualCells, (e, a) => new { Expected = e, Actual = a });
            
            foreach (var cell in expectedAndActualCells)
            {
                // Assert Notebook Cell Input
                Assert.AreEqual(cell.Expected["source"].ToString(), cell.Actual["source"].ToString());

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
                        Assert.AreEqual(cellOutputsItem.Expected.ToString(), cellOutputsItem.Actual.ToString());
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

        private static void UpdateResearchRegressionOutputInSourceFile(string templateName, string expectedOutput)
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

                    // Escape the "escape" sequence for correct parse back
                    expectedOutput = expectedOutput
                        .Replace("\\\\", "\\\\\\\\")
                        .Replace("\\\"", "\\\\\"")
                        .Replace("\"", "\\\"");
                    expectedOutput = CleanDispensableEscapeCharacters(expectedOutput);
                    
                    lines.Add($"            \"{expectedOutput}\";");

                    // now we skip the old  expected ouptut in file
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

        private static string CleanDispensableEscapeCharacters(string json)
        {
            json = json
                .Replace("\\n", string.Empty)
                .Replace("\n", string.Empty)
                .Replace("\\r", string.Empty)
                .Replace("\r", string.Empty)
                .Replace("\\t", string.Empty)
                .Replace("\t", string.Empty);
            return json;
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
            return Composer.Instance.GetExportedTypes<IRegressionResearchDefinition>()
                .Where(type =>
                    !type.IsAbstract &&
                    type.GetConstructor(Array.Empty<Type>()) != null)
                .Select(type => new
                {
                    instance = (IRegressionResearchDefinition)Activator.CreateInstance(type),
                    name = type.Name,
                    path = Path.GetFullPath(Path.Combine(Path.Combine(type.Assembly.Location, @"../"), type.Name + ".ipynb"))
                })
                .Select(tempObj => new ResearchRegressionTestParameters(tempObj.name, tempObj.path, tempObj.instance.ExpectedOutput))
                .ToArray();
        }

        private static TestCaseData[] GetResearchRegressionTestParameters()
        {
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
