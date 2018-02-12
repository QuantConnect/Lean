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
using System;
using System.Threading;
using System.Threading.Tasks;
using Nancy;
using QuantConnect.Configuration;
using QuantConnect.Logging;
using Nancy.Hosting.Self;
using QuantConnect.Util;
using QuantConnect.View.Plugins;

namespace QuantConnect.View
{
    class Program
    {
        /// <summary>
        /// QuantConnect View Service
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Log.Trace("QuantConnect.Views.Main(): LEAN View Service Started.");

            var port = Config.GetInt("view-http-port", 8080);
            var hostname = Config.Get("view-http-hostname", "localhost");
            var protocol = Config.Get("view-http-protocol", "http");
            var url = new Uri($"{protocol}://{hostname}:{port}");
            var hostConfigs = new HostConfiguration {UrlReservations = {CreateAutomatically = true}};
            var cancel = new CancellationTokenSource();
            var bootstrapper = new LeanView();

            // For some reason composer based view plugins types take 60s to load.
            // var bootstrapper = Composer.Instance.GetExportedValueByTypeName<DefaultNancyBootstrapper>(Config.Get("view-plugin"));

            var view = Task.Run(() =>
            {
                using (var server = new NancyHost(bootstrapper, hostConfigs, url))
                {
                    Log.Trace("QuantConnect.Views.Main(): Starting \"{0}\" on {1}", bootstrapper.GetType(), url);
                    server.Start();
                    Log.Trace("QuantConnect.Views.Main(): View Running.");
                    cancel.Token.WaitHandle.WaitOne();
                }
            });

            Console.ReadKey();
            cancel.Cancel();
            Log.Trace("QuantConnect.Views.Main(): Stopping Service..");
            Task.WaitAny(view);
        }
    }
}
