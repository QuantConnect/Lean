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

using QuantConnect.Util;
using QuantConnect.Logging;
using QuantConnect.Interfaces;
using QuantConnect.Configuration;

namespace QuantConnect.Lean.DownloaderDataProvider;
class Program
{
    static void Main(string[] args)
    {
        if (args.Length > 0)
        {
            ProcessCommand(args);
        }
        else
        {
            throw new ArgumentException($"{nameof(DownloaderDataProvider)}: The arguments array is empty. Please provide valid command line arguments.");
        }
    }
    static void ProcessCommand(string[] args)
    {
        var optionsObject = DownloaderDataProviderArgumentParser.ParseArguments(args);

        Log.Trace($"{nameof(ProcessCommand)}:Prompt Command: {string.Join(',', optionsObject)}");

        Log.DebuggingEnabled = Config.GetBool("debug-mode");
        Log.LogHandler = Composer.Instance.GetExportedValueByTypeName<ILogHandler>(Config.Get("log-handler", "CompositeLogHandler"));

        if (!optionsObject.TryGetValue("data-provider", out var parsedDataProvider))
        {
            parsedDataProvider = "DefaultDataProvider";
        }

        var dataProvider = Composer.Instance.GetExportedValueByTypeName<IDataProvider>(parsedDataProvider.ToString());

        Log.Trace($"{nameof(ProcessCommand)}: dataProvider: {dataProvider}, {dataProvider.GetType()}");
    }
}
