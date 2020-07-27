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

using System;
using System.IO;
using NUnit.Framework;
using QuantConnect;
using QuantConnect.Configuration;
using QuantConnect.Logging;
using QuantConnect.Python;

[SetUpFixture]
public class AssemblyInitialize
{
    [OneTimeSetUp]
    public void SetLogHandler()
    {
        AdjustCurrentDirectory();
        // save output to file as well
        Log.LogHandler = new ConsoleLogHandler();
    }

    public static void AdjustCurrentDirectory()
    {
        // nunit 3 sets the current folder to a temp folder we need it to be the test bin output folder
        var dir = TestContext.CurrentContext.TestDirectory;
        Environment.CurrentDirectory = dir;
        Directory.SetCurrentDirectory(dir);
        Config.Reset();
        Globals.Reset();
        PythonInitializer.SetPythonPathEnvironmentVariable(
            new[]
            {
                "./Alphas",
                "./Execution",
                "./Portfolio",
                "./Risk",
                "./Selection",
                "./RegressionAlgorithms",
                "./Research/RegressionScripts",
                "../../../Algorithm",
                "../../../Algorithm/Selection",
                "../../../Algorithm.Framework",
                "../../../Algorithm.Framework/Selection",
                "../../../Algorithm.Python"
            });
    }
}
