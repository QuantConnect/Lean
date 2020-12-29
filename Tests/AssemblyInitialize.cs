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
using NUnit.Framework.Interfaces;
using QuantConnect;
using QuantConnect.Configuration;
using QuantConnect.Logging;
using QuantConnect.Python;
using QuantConnect.Util;

[assembly: MaintainLogHandler()]
[SetUpFixture]
public class AssemblyInitialize
{
    [OneTimeSetUp]
    public void InitializeTestEnvironment()
    {
        AdjustCurrentDirectory();
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

[AttributeUsage(AttributeTargets.Assembly)]
public class MaintainLogHandlerAttribute : Attribute, ITestAction
{
    private static ILogHandler logHandler;

    public MaintainLogHandlerAttribute()
    {
        logHandler = GetLogHandler();
    }

    /// <summary>
    /// Get the log handler defined by test context parameters. Defaults to ConsoleLogHandler if no
    /// "log-handler" parameter is found.
    /// </summary>
    /// <returns>A new LogHandler</returns>
    public static ILogHandler GetLogHandler()
    {
        if (TestContext.Parameters.Exists("log-handler"))
        {
            var logHandler = TestContext.Parameters["log-handler"];
            Log.Trace($"QuantConnect.Tests.AssemblyInitialize(): Log handler test parameter loaded {logHandler}");

            return Composer.Instance.GetExportedValueByTypeName<ILogHandler>(logHandler);
        }
        
        // If no parameter just use ConsoleLogHandler
        return new ConsoleLogHandler();
    }

    public void BeforeTest(ITest details)
    {
        Log.LogHandler = logHandler;
    }

    public void AfterTest(ITest details)
    {
        //NOP
    }

    public ActionTargets Targets
    {   // Set only to act on test fixture not individual tests
        get { return ActionTargets.Suite; }
    }
}


