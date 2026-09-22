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
using System.IO;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Api;
using System.Threading;
using QuantConnect.Orders;
using QuantConnect.Brokerages;
using QuantConnect.Configuration;
using System.Collections.Generic;
using Python.Runtime;
using QuantConnect.Interfaces;

namespace QuantConnect.Tests.API
{
    /// <summary>
    /// API Live endpoint tests
    /// </summary>
    [TestFixture, Explicit("Requires configured api access, a live node to run on, and brokerage configurations.")]
    public class LiveTradingTests : ApiTestBase
    {
        private const bool StopLiveAlgos = true;
        private readonly Dictionary<string, object> _defaultSettings = new()
        {
            { "id", "QuantConnectBrokerage" },
            { "environment", "paper" },
            { "user", "" },
            { "password", "" },
            { "account", "" }
        };
        private readonly ProjectFile _defaultFile = new ProjectFile
        {
            Name = "Main.cs",
            Code = File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateAlgorithm.cs")
        };

        /// <summary>
        /// Live paper trading via Interactive Brokers
        /// </summary>
        [Test]
        public void LiveIBTest()
        {
            var user = Config.Get("ib-user-name");
            var password = Config.Get("ib-password");
            var account = Config.Get("ib-account");
            var environment = account.Substring(0, 2) == "DU" ? "paper" : "live";
            var ib_weekly_restart_utc_time = Config.Get("ib-weekly-restart-utc-time");

            // Create default algorithm settings
            var settings = new Dictionary<string, object>() {
                { "id", "InteractiveBrokersBrokerage" },
                { "ib-trading-mode", environment },
                { "ib-user-name", user },
                { "ib-password", password },
                { "ib-account", account },
                { "ib-weekly-restart-utc-time", ib_weekly_restart_utc_time},
                { "holdings", new List<Holding>() },
                { "cash", new List<Dictionary<object, object>>()
                    {
                    {new Dictionary<object, object>
                        {
                            { "currency" , "USD"},
                            { "amount", 100000}
                        }
                    }
                    }
                },
            };

            var quantConnectDataProvider = new Dictionary<string, object>
            {
                { "id", "QuantConnectBrokerage" },
            };

            var dataProviders = new Dictionary<string, object>
            {
                { "QuantConnectBrokerage", quantConnectDataProvider },
                { "InteractiveBrokersBrokerage", settings }
            };

            RunLiveAlgorithm(settings, _defaultFile, StopLiveAlgos, dataProviders);
        }

        [Test]
        public void PolygonTest()
        {
            var apiKey = Config.Get("polygon-api-key");
            var polygonDataProvider = new Dictionary<string, object>()
            {
                { "id", "Polygon" },
                { "polygon-api-key", apiKey },
            };

            var dataProviders = new Dictionary<string, object>
            {
                { "Polygon", polygonDataProvider },
            };

            RunLiveAlgorithm(_defaultSettings, _defaultFile, StopLiveAlgos, dataProviders);
        }

        [Test]
        public void BinanceTest()
        {
            var apiSecret = Config.Get("binance-api-secret");
            var apiKey = Config.Get("binance-api-key");
            var apiUrl = Config.Get("binance-api-url");
            var websocketUrl = Config.Get("binance-websocket-url");
            var binanceSettings = new Dictionary<string, object>()
            {
                { "id", "BinanceBrokerage" },
                { "binance-use-testnet", "paper" },
                { "binance-exchange-name", "Binance" },
                { "binance-api-secret", apiSecret },
                { "binance-api-key", apiKey },
                { "binance-api-url", apiUrl },
                { "binance-websocket-url", websocketUrl },
                { "holdings", new List<Holding>() },
                { "cash", new List<Dictionary<object, object>>()
                    {
                    {new Dictionary<object, object>
                        {
                            { "currency" , "USD"},
                            { "amount", 100000}
                        }
                    }
                    }
                },
            };

            var dataProviders = new Dictionary<string, object>
            {
                { "BinanceBrokerage", binanceSettings },
            };

            RunLiveAlgorithm(binanceSettings, _defaultFile, StopLiveAlgos, dataProviders);
        }

        [Test]
        public void BinanceUSTest()
        {
            var apiSecret = Config.Get("binanceus-api-secret");
            var apiKey = Config.Get("binanceus-api-key");
            var apiUrl = Config.Get("binanceus-api-url");
            var websocketUrl = Config.Get("binanceus-websocket-url");
            var binanceUSSettings = new Dictionary<string, object>()
            {
                { "id", "BinanceBrokerage" },
                { "binance-exchange-name", "BinanceUS" },
                { "binanceus-api-secret", apiSecret },
                { "binanceus-api-key", apiKey },
                { "binanceus-api-url", apiUrl },
                { "binanceus-websocket-url", websocketUrl },
                { "holdings", new List<Holding>() },
                { "cash", new List<Dictionary<object, object>>()
                    {
                    {new Dictionary<object, object>
                        {
                            { "currency" , "USD"},
                            { "amount", 100000}
                        }
                    }
                    }
                },
            };

            var dataProviders = new Dictionary<string, object>
            {
                { "BinanceBrokerage", binanceUSSettings },
            };

            RunLiveAlgorithm(binanceUSSettings, _defaultFile, StopLiveAlgos, dataProviders);
        }

        [Test]
        public void BinanceFuturesUSDMTest()
        {
            var apiSecret = Config.Get("binance-api-secret");
            var apiKey = Config.Get("binance-api-key");
            var apiUrl = Config.Get("binance-fapi-url");
            var websocketUrl = Config.Get("binance-fwebsocket-url");
            var binanceSettings = new Dictionary<string, object>()
            {
                { "id", "BinanceBrokerage" },
                { "binance-exchange-name", "Binance-USDM-Futures" },
                { "binance-api-secret", apiSecret },
                { "binance-api-key", apiKey },
                { "binance-fapi-url", apiUrl },
                { "binance-fwebsocket-url", websocketUrl },
                { "holdings", new List<Holding>() },
                { "cash", new List<Dictionary<object, object>>()
                    {
                    {new Dictionary<object, object>
                        {
                            { "currency" , "USD"},
                            { "amount", 100000}
                        }
                    }
                    }
                },
            };

            var dataProviders = new Dictionary<string, object>
            {
                { "BinanceBrokerage", binanceSettings },
            };

            RunLiveAlgorithm(binanceSettings, _defaultFile, StopLiveAlgos, dataProviders);
        }

        [Test]
        public void BinanceFuturesCOINTest()
        {
            var apiSecret = Config.Get("binance-api-secret");
            var apiKey = Config.Get("binance-api-key");
            var apiUrl = Config.Get("binance-dapi-url");
            var websocketUrl = Config.Get("binance-dwebsocket-url");
            var binanceSettings = new Dictionary<string, object>()
            {
                { "id", "BinanceBrokerage" },
                { "binance-exchange-name", "Binance-COIN-Futures" },
                { "binance-api-secret", apiSecret },
                { "binance-api-key", apiKey },
                { "binance-dapi-url", apiUrl },
                { "binance-dwebsocket-url", websocketUrl },
                { "holdings", new List<Holding>() },
                { "cash", new List<Dictionary<object, object>>()
                    {
                    {new Dictionary<object, object>
                        {
                            { "currency" , "USD"},
                            { "amount", 100000}
                        }
                    }
                    }
                },
            };

            var dataProviders = new Dictionary<string, object>
            {
                { "BinanceBrokerage", binanceSettings },
            };

            RunLiveAlgorithm(binanceSettings, _defaultFile, StopLiveAlgos, dataProviders);
        }

        /// <summary>
        /// Live paper trading via FXCM
        /// </summary>
        [Test]
        public void LiveFXCMTest()
        {
            var user = Config.Get("fxcm-user-name");
            var password = Config.Get("fxcm-password");
            var account = Config.Get("fxcm-account-id");

            // Create default algorithm settings
            var settings = new Dictionary<string, object>() {
                { "id", "FxcmBrokerage" },
                { "environment", "paper" },
                { "user", user },
                { "password", password },
                { "account", account }
            };

            var file = new ProjectFile
            {
                Name = "Main.cs",
                Code = File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateForexAlgorithm.cs")
            };

            RunLiveAlgorithm(settings, file, StopLiveAlgos);
        }

        /// <summary>
        /// Live paper trading via Oanda
        /// </summary>
        [Test]
        public void LiveOandaTest()
        {
            var token = Config.Get("oanda-access-token");
            var account = Config.Get("oanda-account-id");
            var environment = Config.Get("oanda-environment");

            // Create default algorithm settings
            var oandaSettings = new Dictionary<string, object>()
            {
                { "id", "OandaBrokerage" },
                { "oanda-access-token", token },
                { "oanda-account-id", account },
                { "oanda-environment", environment }
            };

            var dataProvider = new Dictionary<string, object>
            {
                { "OandaBrokerage", oandaSettings }
            };

            RunLiveAlgorithm(_defaultSettings, _defaultFile, StopLiveAlgos, dataProvider);
        }

        /// <summary>
        /// Live paper trading via Tradier
        /// </summary>
        [Test]
        public void LiveTradierTest()
        {
            var refreshToken = Config.Get("tradier-refresh-token");
            var account = Config.Get("tradier-account-id");
            var accessToken = Config.Get("tradier-access-token");
            var dateIssued = Config.Get("tradier-issued-at");

            // Create default algorithm settings
            var tradierSettings = new Dictionary<string, object>()
            {
                { "id", "TradierBrokerage" },
                { "tradier-account-id", account },
                { "tradier-access-token", accessToken },
                { "tradier-environment", "paper" },
                { "holdings", new List<Holding>() },
                { "cash", new List<Dictionary<object, object>>()
                    {
                    {new Dictionary<object, object>
                        {
                            { "currency" , "USD"},
                            { "amount", 100000}
                        }
                    }
                    }
                }
            };

            var dataProvider = new Dictionary<string, object>()
            {
                { "TradierBrokerage",  tradierSettings}
            };

            RunLiveAlgorithm(tradierSettings, _defaultFile, StopLiveAlgos, dataProvider);
        }

        /// <summary>
        /// Live trading via Bitfinex
        /// </summary>
        [Test]
        public void LiveBitfinexTest()
        {
            var key = Config.Get("bitfinex-api-key");
            var secretKey = Config.Get("bitfinex-api-secret");

            // Create default algorithm settings
            var bitfinexSettings = new Dictionary<string, object>()
            {
                { "id", "BitfinexBrokerage" },
                { "bitfinex-api-secret", secretKey },
                { "bitfinex-api-key", key },
                { "holdings", new List<Holding>() },
                { "cash", new List<Dictionary<object, object>>()
                    {
                    {new Dictionary<object, object>
                        {
                            { "currency" , "USD"},
                            { "amount", 100000}
                        }
                    }
                    }
                },
            };

            var dataProvider = new Dictionary<string, object>()
            {
                { "BitfinexBrokerage", bitfinexSettings }
            };

            RunLiveAlgorithm(bitfinexSettings, _defaultFile, StopLiveAlgos, dataProvider);
        }

        /// <summary>
        /// Live trading via Coinbase
        /// </summary>
        [Test]
        public void LiveCoinbaseTest()
        {
            var key = Config.Get("coinbase-api-key");
            var secretKey = Config.Get("coinbase-api-secret");
            var apiUrl = Config.Get("coinbase-rest-api");
            var wsUrl = Config.Get("coinbase-url");

            // Create default algorithm settings
            var coinbaseSettings = new Dictionary<string, object>()
            {
                { "id", "CoinbaseBrokerage" },
                { "coinbase-api-key", key },
                { "coinbase-api-secret", secretKey },
                { "coinbase-rest-api", apiUrl },
                { "coinbase-url", wsUrl },
                { "holdings", new List<Holding>() },
                { "cash", new List<Dictionary<object, object>>()
                    {
                    {new Dictionary<object, object>
                        {
                            { "currency" , "USD"},
                            { "amount", 100000}
                        }
                    }
                    }
                }
            };

            var dataProvider = new Dictionary<string, object>
            {
                { "CoinbaseBrokerage", coinbaseSettings }
            };

            RunLiveAlgorithm(coinbaseSettings, _defaultFile, StopLiveAlgos, dataProvider);
        }

        [Test]
        public void KrakenTest()
        {
            var krakenSettings = new Dictionary<string, object>()
            {
                { "id", "KrakenBrokerage" },
                { "kraken-api-key", Config.Get("kraken-api-key") },
                { "kraken-api-secret", Config.Get("kraken-api-secret") },
                { "kraken-verification-tier", Config.Get("kraken-verification-tier") },
                { "holdings", new List<Holding>() },
                { "cash", new List<Dictionary<object, object>>()
                    {
                    {new Dictionary<object, object>
                        {
                            { "currency" , "USD"},
                            { "amount", 100000}
                        }
                    }
                    }
                }
            };

            var dataProvider = new Dictionary<string, object>
            {
                { "KrakenBrokerage", krakenSettings }
            };

            RunLiveAlgorithm(krakenSettings, _defaultFile, StopLiveAlgos, dataProvider);
        }

        [Test]
        public void BybitTest()
        {
            var bybitSettings = new Dictionary<string, object>()
            {
                { "id", "BybitBrokerage" },
                { "bybit-api-key", Config.Get("bybit-api-key") },
                { "bybit-api-secret", Config.Get("bybit-api-secret") },
                { "bybit-api-url", Config.Get("bybit-api-url") },
                { "bybit-websocket-url", Config.Get("bybit-websocket-url") },
                { "bybit-use-testnet", "paper" },
                { "bybit-vip-level", "VIP0" },
                { "holdings", new List<Holding>() },
                { "cash", new List<Dictionary<object, object>>()
                    {
                    {new Dictionary<object, object>
                        {
                            { "currency" , "USD"},
                            { "amount", 100000}
                        }
                    }
                    }
                }
            };

            var dataProvider = new Dictionary<string, object>
            {
                { "BybitBrokerage", bybitSettings }
            };

            RunLiveAlgorithm(bybitSettings, _defaultFile, StopLiveAlgos, dataProvider);
        }

        /// <summary>
        /// Test creating the settings object that provide the necessary parameters for each broker
        /// </summary>
        [Test]
        public void LiveAlgorithmSettings_CanBeCreated_Successfully()
        {
            var user = "";
            var password = "";
            var environment = "paper";
            var account = "";
            var key = "";
            var secretKey = "";

            // Oanda Custom Variables
            var accessToken = "";

            // Tradier Custom Variables
            var dateIssued = "";
            var refreshToken = "";

            // Create and test settings for each brokerage
            foreach (BrokerageName brokerageName in Enum.GetValues(typeof(BrokerageName)))
            {
                Dictionary<string, string> settings = null;

                switch (brokerageName)
                {
                    case BrokerageName.Default:
                        user = Config.Get("default-username");
                        password = Config.Get("default-password");
                        settings = new Dictionary<string, string>()
                        {
                            { "id", BrokerageName.Default.ToString() },
                            { "environment", environment },
                            { "user", user },
                            { "password", password },
                            { "account", account }
                        };

                        Assert.IsTrue(settings["id"] == BrokerageName.Default.ToString());
                        break;
                    case BrokerageName.FxcmBrokerage:
                        user = Config.Get("fxcm-user-name");
                        password = Config.Get("fxcm-password");
                        settings = new Dictionary<string, string>()
                        {
                            { "id", BrokerageName.FxcmBrokerage.ToString() },
                            { "environment", environment },
                            { "user", user },
                            { "password", password },
                            { "account", account }
                        };

                        Assert.IsTrue(settings["id"] == BrokerageName.FxcmBrokerage.ToString());
                        break;
                    case BrokerageName.InteractiveBrokersBrokerage:
                        user = Config.Get("ib-user-name");
                        password = Config.Get("ib-password");
                        account = Config.Get("ib-account");
                        settings = new Dictionary<string, string>()
                        {
                            { "id", BrokerageName.InteractiveBrokersBrokerage.ToString() },
                            { "environment", account.Substring(0, 2) == "DU" ? "paper" : "live" },
                            { "user", user },
                            { "password", password },
                            { "acount", account }
                        };

                        Assert.IsTrue(settings["id"] == BrokerageName.InteractiveBrokersBrokerage.ToString());
                        break;
                    case BrokerageName.OandaBrokerage:
                        accessToken = Config.Get("oanda-access-token");
                        account = Config.Get("oanda-account-id");

                        settings = new Dictionary<string, string>()
                        {
                            { "id", BrokerageName.OandaBrokerage.ToStringInvariant() },
                            { "user", "" },
                            { "password", "" },
                            { "environment", environment },
                            { "account", account },
                            { "accessToken", accessToken },
                            { "dateIssued", "1" }
                        };
                        Assert.IsTrue(settings["id"] == BrokerageName.OandaBrokerage.ToString());
                        break;
                    case BrokerageName.TradierBrokerage:
                        dateIssued = Config.Get("tradier-issued-at");
                        refreshToken = Config.Get("tradier-refresh-token");
                        account = Config.Get("tradier-account-id");

                        settings = new Dictionary<string, string>()
                        {
                            { "id", BrokerageName.TradierBrokerage.ToString() },
                            { "user", "" },
                            { "password", "" },
                            { "environment", "live" },
                            { "accessToken", accessToken },
                            { "dateIssued", dateIssued },
                            { "refreshToken", refreshToken },
                            { "lifetime", "86399" },
                            { "account", account }
                        };
                        break;
                    case BrokerageName.Bitfinex:
                        key = Config.Get("bitfinex-api-key");
                        secretKey = Config.Get("bitfinex-api-secret");

                        settings = new Dictionary<string, string>()
                        {
                            { "id", "BitfinexBrokerage" },
                            { "user", "" },
                            { "password", "" },
                            { "environment", "live" },
                            { "key", key },
                            { "secret", secretKey },
                        };
                        break;
                    case BrokerageName.GDAX:
                    case BrokerageName.Coinbase:
                        key = Config.Get("coinbase-api-key");
                        secretKey = Config.Get("coinbase-api-secret");
                        var apiUrl = Config.Get("coinbase-rest-api");
                        var wsUrl = Config.Get("coinbase-url");

                        settings = new Dictionary<string, string>()
                        {
                            { "id",  "CoinbaseBrokerage"},
                            { "user", "" },
                            { "password", "" },
                            { "environment", "live" },
                            { "key", key },
                            { "secret", secretKey },
                            { "apiUrl", (new Uri(apiUrl)).AbsoluteUri },
                            { "wsUrl", (new Uri(wsUrl)).AbsoluteUri }
                        };
                        break;
                    case BrokerageName.AlphaStreams:
                        // No live algorithm settings
                        settings = new Dictionary<string, string>()
                        {
                            { "id", "" },
                            { "user", "" },
                            { "password", "" },
                            { "account", "" }
                        };
                        break;
                    case BrokerageName.Binance:
                        // No live algorithm settings
                        settings = new Dictionary<string, string>()
                        {
                            { "id", "" },
                            { "user", "" },
                            { "password", "" },
                            { "account", "" }
                        };
                        break;
                    default:
                        throw new Exception($"Settings have not been implemented for this brokerage: {brokerageName}");
                }

                // Tests common to all brokerage configuration classes
                Assert.IsTrue(settings != null);
                Assert.IsTrue(settings["password"] == password);
                Assert.IsTrue(settings["user"] == user);

                // Oanda specific settings
                if (brokerageName == BrokerageName.OandaBrokerage)
                {
                    var oandaSetting = settings;
                    Assert.IsTrue(oandaSetting["accessToken"] == accessToken);
                }

                // Tradier specific settings
                if (brokerageName == BrokerageName.TradierBrokerage)
                {
                    var tradierLiveAlogrithmSettings = settings;

                    Assert.IsTrue(tradierLiveAlogrithmSettings["dateIssued"] == dateIssued);
                    Assert.IsTrue(tradierLiveAlogrithmSettings["refreshToken"] == refreshToken);
                    Assert.IsTrue(settings["environment"] == "life");
                }

                // reset variables
                user = "";
                password = "";
                environment = "paper";
                account = "";
            }
        }

        /// <summary>
        /// Reading live algorithm tests
        ///   - Get a list of live algorithms
        ///   - Get logs for the first algorithm returned
        /// Will there always be a live algorithm for the test user?
        /// </summary>
        [Test]
        public void LiveAlgorithmsAndLiveLogs_CanBeRead_Successfully()
        {
            // Read all currently running algorithms
            var liveAlgorithms = ApiClient.ListLiveAlgorithms(AlgorithmStatus.Running);
            var stringRepresentation = liveAlgorithms.ToString();
            Assert.IsTrue(ApiTestBase.IsValidJson(stringRepresentation));

            Assert.IsTrue(liveAlgorithms.Success);
            // There has to be at least one running algorithm
            Assert.IsTrue(liveAlgorithms.Algorithms.Any());

            // Read the logs of the first live algorithm
            var firstLiveAlgo = liveAlgorithms.Algorithms[0];
            var liveLogs = ApiClient.ReadLiveLogs(firstLiveAlgo.ProjectId, firstLiveAlgo.DeployId, 0, 20);
            stringRepresentation = liveLogs.ToString();
            Assert.IsTrue(ApiTestBase.IsValidJson(stringRepresentation));

            Assert.IsTrue(liveLogs.Success);
            Assert.IsTrue(liveLogs.Logs.Any());
            Assert.IsTrue(liveLogs.Length >= 0);
            Assert.IsTrue(liveLogs.DeploymentOffset >= 0);

            // Filter by the longest word of the first line, so only lines containing it come back
            var keyword = liveLogs.Logs[0].Split(' ').OrderByDescending(x => x.Length).First();
            var filteredLogs = ApiClient.ReadLiveLogs(firstLiveAlgo.ProjectId, firstLiveAlgo.DeployId, 0, 20, query: keyword);
            Assert.IsTrue(filteredLogs.Success);
            Assert.Greater(filteredLogs.Logs.Count, 0);
            Assert.IsTrue(filteredLogs.Logs.All(x => x.Contains(keyword, StringComparison.Ordinal)));

            Assert.Throws<ArgumentException>(() => ApiClient.ReadLiveLogs(firstLiveAlgo.ProjectId, firstLiveAlgo.DeployId, 0, 251));
        }

        /// <summary>
        /// Runs the algorithm with the given settings and file
        /// </summary>
        /// <param name="settings">Settings for Lean</param>
        /// <param name="file">File to run</param>
        /// <param name="stopLiveAlgos">If true the algorithm will be stopped at the end of the method.
        /// Otherwise, it will keep running</param>
        /// <param name="dataProviders">Dictionary with the data providers and their corresponding credentials</param>
        /// <returns>The id of the project created with the algorithm in</returns>
        private int RunLiveAlgorithm(Dictionary<string, object> settings, ProjectFile file, bool stopLiveAlgos, Dictionary<string, object> dataProviders = null)
        {
            return RunLiveAlgorithm(ApiClient, settings, file, stopLiveAlgos, dataProviders);
        }

        internal static int RunLiveAlgorithm(Api.Api apiClient, Dictionary<string, object> settings, ProjectFile file, bool stopLiveAlgos,
            Dictionary<string, object> dataProviders = null, Language language = Language.CSharp)
        {
            // Create a new project
            var project = apiClient.CreateProject($"Test project - {DateTime.Now.ToStringInvariant()}", language, Globals.OrganizationID);
            var projectId = project.Projects.First().ProjectId;

            // Update Project Files
            var updateProjectFileContent = apiClient.UpdateProjectFileContent(projectId, language == Language.CSharp ? "Main.cs" : "main.py", file.Code);
            Assert.IsTrue(updateProjectFileContent.Success);

            // Create compile
            var compile = apiClient.CreateCompile(projectId);
            Assert.IsTrue(compile.Success);

            // Wait at max 30 seconds for project to compile
            var compileCheck = WaitForCompilerResponse(apiClient, projectId, compile.CompileId, 30);
            Assert.IsTrue(compileCheck.Success);
            Assert.IsTrue(compileCheck.State == CompileState.BuildSuccess);

            // Get a live node to launch the algorithm on
            var nodesResponse = apiClient.ReadProjectNodes(projectId);
            Assert.IsTrue(nodesResponse.Success);
            var freeNode = nodesResponse.Nodes.LiveNodes.Where(x => x.Busy == false);
            Assert.IsNotEmpty(freeNode, "No free Live Nodes found");

            // Create live default algorithm
            var createLiveAlgorithm = apiClient.CreateLiveAlgorithm(projectId, compile.CompileId, freeNode.FirstOrDefault().Id, settings, dataProviders: dataProviders);
            Assert.IsTrue(createLiveAlgorithm.Success);

            if (stopLiveAlgos)
            {
                // Liquidate live algorithm; will also stop algorithm
                var liquidateLive = apiClient.LiquidateLiveAlgorithm(projectId);
                Assert.IsTrue(liquidateLive.Success);

                // Delete the project
                var deleteProject = apiClient.DeleteProject(projectId);
                Assert.IsTrue(deleteProject.Success);
            }

            return projectId;
        }

        [Test]
        public void ReadLiveOrders()
        {
            // Create default algorithm settings
            var settings = new Dictionary<string, object>()
            {
                { "id", "QuantConnectBrokerage" },
                { "environment", "paper" },
                { "user", "" },
                { "password", "" },
                { "account", "" }
            };

            var file = new ProjectFile
            {
                Name = "Main.cs",
                Code = File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateAlgorithm.cs")
            };

            // Run the live algorithm
            var projectId = RunLiveAlgorithm(settings, file, false);

            // Wait to receive the orders
            var readLiveOrders = WaitForReadLiveOrdersResponse(projectId, 60 * 5);
            Assert.GreaterOrEqual(readLiveOrders.Length, readLiveOrders.Orders.Count);
            Assert.IsTrue(readLiveOrders.Orders.Any());
            Assert.AreEqual(Symbols.SPY, readLiveOrders.Orders.First().Symbol);

            // Liquidate live algorithm; will also stop algorithm
            var liquidateLive = ApiClient.LiquidateLiveAlgorithm(projectId);
            Assert.IsTrue(liquidateLive.Success);

            // Delete the project
            var deleteProject = ApiClient.DeleteProject(projectId);
            Assert.IsTrue(deleteProject.Success);
        }

        [Test]
        public void RunLiveAlgorithmsFromPython()
        {
            var file = new ProjectFile
            {
                Name = "Main.cs",
                Code = File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateAlgorithm.cs")
            };
            // Create a new project
            var project = ApiClient.CreateProject($"Test project - {DateTime.Now.ToStringInvariant()}", Language.CSharp, TestOrganization);
            var projectId = project.Projects.First().ProjectId;

            // Update Project Files
            var updateProjectFileContent = ApiClient.UpdateProjectFileContent(projectId, "Main.cs", file.Code);
            Assert.IsTrue(updateProjectFileContent.Success);

            // Create compile
            var compile = ApiClient.CreateCompile(projectId);
            Assert.IsTrue(compile.Success);

            // Wait at max 30 seconds for project to compile
            var compileCheck = WaitForCompilerResponse(ApiClient, projectId, compile.CompileId, 30);
            Assert.IsTrue(compileCheck.Success);
            Assert.IsTrue(compileCheck.State == CompileState.BuildSuccess);

            // Get a live node to launch the algorithm on
            var nodesResponse = ApiClient.ReadProjectNodes(projectId);
            Assert.IsTrue(nodesResponse.Success);
            var freeNode = nodesResponse.Nodes.LiveNodes.Where(x => x.Busy == false);
            Assert.IsNotEmpty(freeNode, "No free Live Nodes found");

            using (Py.GIL())
            {

                dynamic pythonCall = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

def CreateLiveAlgorithmFromPython(apiClient, projectId, compileId, nodeId):
    user = Config.Get('ib-user-name')
    password = Config.Get('ib-password')
    account = Config.Get('ib-account')
    environment = 'paper'
    if account[:2] == 'DU':
        environment = 'live'
    ib_weekly_restart_utc_time = '22:00:00'
    settings = {'id':'InteractiveBrokersBrokerage', 'environment': environment, 'ib-user-name': user, 'ib-password': password, 'ib-account': account, 'ib-weekly-restart-utc-time': ib_weekly_restart_utc_time, 'holdings':[], 'cash': [{'currency' : 'USD', 'amount' : 100000}]}
    dataProviders = {'QuantConnectBrokerage':{'id':'QuantConnectBrokerage'}}
    apiClient.CreateLiveAlgorithm(projectId, compileId, nodeId, settings, dataProviders = dataProviders)
");
                var createLiveAlgorithmModule = pythonCall.GetAttr("CreateLiveAlgorithmFromPython");
                var createLiveAlgorithm = createLiveAlgorithmModule(ApiClient, projectId, compile.CompileId, freeNode.FirstOrDefault().Id);
                Assert.IsTrue(createLiveAlgorithm.Success);

                // Liquidate live algorithm; will also stop algorithm
                var liquidateLive = ApiClient.LiquidateLiveAlgorithm(projectId);
                Assert.IsTrue(liquidateLive.Success);

                // Delete the project
                var deleteProject = ApiClient.DeleteProject(projectId);
                Assert.IsTrue(deleteProject.Success);
            }
        }

        /// <summary>
        /// live/create answers with the summary of the algorithm it just deployed
        /// </summary>
        [Test]
        public void CreateLiveAlgorithmReturnsTheDeployedAlgorithmSummary()
        {
            var created = DeployPaperAlgorithm(out var projectId);
            try
            {
                Assert.IsNotNull(created.Live, "live/create is documented to answer with the summary of the algorithm");
                Assert.AreEqual(created.DeployId, created.Live.DeployId);
                Assert.IsNotEmpty(created.Live.Status);
            }
            finally
            {
                StopAndDeleteProject(projectId);
            }
        }

        /// <summary>
        /// live/read takes an optional deploy id, and without one it reads the latest deployment of the project
        /// </summary>
        [Test]
        public void ReadLiveAlgorithmWithoutADeployId()
        {
            var description = $"Deployed by ReadLiveAlgorithmWithoutADeployId at {DateTime.UtcNow.ToStringInvariant("yyyy-MM-dd HH-mm-ss")}";
            var created = DeployPaperAlgorithm(out var projectId, description);
            try
            {
                var latest = ApiClient.ReadLiveAlgorithm(projectId);
                Assert.IsTrue(latest.Success, $"Error reading the live algorithm: {string.Join(", ", latest.Errors ?? [])}");
                Assert.AreEqual(created.DeployId, latest.DeployId, "Omitting the deploy id reads the latest deployment of the project");
                Assert.IsFalse(latest.IsPublicStreaming, "A new deployment does not stream publicly");

                var byDeployId = ApiClient.ReadLiveAlgorithm(projectId, created.DeployId);
                Assert.IsTrue(byDeployId.Success, $"Error reading the live algorithm: {string.Join(", ", byDeployId.Errors ?? [])}");
                Assert.AreEqual(created.DeployId, byDeployId.DeployId);
            }
            finally
            {
                StopAndDeleteProject(projectId);
            }
        }

        /// <summary>
        /// live/list takes the documented projectId filter, and every summary carries the equity,
        /// environment, description and leagues of its deployment
        /// </summary>
        [Test]
        public void ListLiveAlgorithmsFiltersByProjectId()
        {
            var description = $"Deployed by ListLiveAlgorithmsFiltersByProjectId at {DateTime.UtcNow.ToStringInvariant("yyyy-MM-dd HH-mm-ss")}";
            var created = DeployPaperAlgorithm(out var projectId, description);
            try
            {
                // the equity is only reported once the deployment sends its first result packet
                var listed = ApiClient.ListLiveAlgorithms(projectId: projectId);
                var finish = DateTime.UtcNow.AddMinutes(3);
                while (listed.Success && listed.Algorithms.All(x => x.Equity == 0) && DateTime.UtcNow < finish)
                {
                    Thread.Sleep(10000);
                    listed = ApiClient.ListLiveAlgorithms(projectId: projectId);
                }

                Assert.IsTrue(listed.Success, $"Error listing the live algorithms: {string.Join(", ", listed.Errors)}");
                Assert.IsTrue(listed.Algorithms.All(x => x.ProjectId == projectId),
                    "The projectId filter must narrow the list to the deployments of one project");

                var summary = listed.Algorithms.Single(x => x.DeployId == created.DeployId);
                Assert.AreEqual("paper", summary.Environment);
                Assert.AreEqual(description, summary.Description);
                Assert.Greater(summary.Equity, 0);
                Assert.IsNotNull(summary.Leagues);
            }
            finally
            {
                StopAndDeleteProject(projectId);
            }
        }

        /// <summary>
        /// Deploys the default paper algorithm to a new project and hands back the api response, so a test
        /// can assert on it. The caller stops the deployment and deletes the project
        /// </summary>
        private CreateLiveAlgorithmResponse DeployPaperAlgorithm(out int projectId, string description = null)
        {
            var project = ApiClient.CreateProject($"Test project - {DateTime.Now.ToStringInvariant()}", Language.CSharp, TestOrganization);
            Assert.IsTrue(project.Success, $"Error creating project: {string.Join(", ", project.Errors)}");
            projectId = project.Projects.First().ProjectId;

            if (description != null)
            {
                var updateProject = ApiClient.UpdateProject(projectId, description: description);
                Assert.IsTrue(updateProject.Success, $"Error updating project: {string.Join(", ", updateProject.Errors)}");

                var readProject = ApiClient.ReadProject(projectId);
                Assert.IsTrue(readProject.Success, $"Error reading project: {string.Join(", ", readProject.Errors)}");
                Assert.AreEqual(description, readProject.Projects.Single().Description, "The project description was not set");
            }

            var updateProjectFileContent = ApiClient.UpdateProjectFileContent(projectId, _defaultFile.Name, _defaultFile.Code);
            Assert.IsTrue(updateProjectFileContent.Success, $"Error updating project file: {string.Join(", ", updateProjectFileContent.Errors)}");

            var compile = ApiClient.CreateCompile(projectId);
            Assert.IsTrue(compile.Success, $"Error compiling project: {string.Join(", ", compile.Errors)}");
            compile = WaitForCompilerResponse(ApiClient, projectId, compile.CompileId, 30);
            Assert.AreEqual(CompileState.BuildSuccess, compile.State, $"Error compiling project: {string.Join(", ", compile.Errors)}");

            var nodesResponse = ApiClient.ReadProjectNodes(projectId);
            Assert.IsTrue(nodesResponse.Success, $"Error reading project nodes: {string.Join(", ", nodesResponse.Errors)}");
            var freeNode = nodesResponse.Nodes.LiveNodes.FirstOrDefault(x => !x.Busy);
            Assert.IsNotNull(freeNode, "No free Live Nodes found");

            var created = ApiClient.CreateLiveAlgorithm(projectId, compile.CompileId, freeNode.Id, _defaultSettings);
            Assert.IsTrue(created.Success, $"Error deploying the live algorithm: {string.Join(", ", created.Errors)}");

            return created;
        }

        /// <summary>
        /// Liquidating the live algorithm of the project also stops it
        /// </summary>
        private void StopAndDeleteProject(int projectId)
        {
            ApiClient.LiquidateLiveAlgorithm(projectId);
            ApiClient.DeleteProject(projectId);
        }

        /// <summary>
        /// Wait to receive at least one order
        /// </summary>
        /// <param name="projectId">Id of the project</param>
        /// <param name="seconds">Seconds to allow for receive an order</param>
        /// <returns></returns>
        private OrdersResponseWrapper WaitForReadLiveOrdersResponse(int projectId, int seconds)
        {
            var readLiveOrders = new OrdersResponseWrapper();
            var finish = DateTime.UtcNow.AddSeconds(seconds);
            while (DateTime.UtcNow < finish && !readLiveOrders.Orders.Any())
            {
                Thread.Sleep(10000);
                readLiveOrders = ApiClient.ReadLiveOrders(projectId);
            }
            return readLiveOrders;
        }
    }
}
