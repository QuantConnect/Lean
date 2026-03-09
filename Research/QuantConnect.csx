#r "Python.Runtime.dll"
#r "QuantConnect.Algorithm.dll"
#r "QuantConnect.Algorithm.Framework.dll"
#r "QuantConnect.Common.dll"
#r "QuantConnect.Indicators.dll"
#r "QuantConnect.Research.dll"
#r "NodaTime.dll"
#r "Accord.dll"
#r "Accord.Fuzzy.dll"
#r "Accord.Math.Core.dll"
#r "Accord.Math.dll"
#r "MathNet.Numerics.dll"
#r "Newtonsoft.Json.dll"
#r "QuantConnect.AlgorithmFactory.dll"
#r "QuantConnect.Logging.dll"
#r "QuantConnect.Messaging.dll"
#r "QuantConnect.Configuration.dll"
#r "QuantConnect.Lean.Engine.dll"
#r "QuantConnect.Algorithm.CSharp.dll"
#r "QuantConnect.Api.dll"
// Note: #r directives must be in the beggining of the file

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
/*
 * This C# Script File (.csx) can be loaded in a notebook (ipynb file)
 * in order to reference QuantConnect assemblies
 * https://github.com/scriptcs/scriptcs/wiki/Writing-a-script#referencing-assemblies
 *
 * Usage:
 * #load "QuantConnect.csx"
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.Framework;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Api;
using QuantConnect.Parameters;
using QuantConnect.Benchmarks;
using QuantConnect.Brokerages;
using QuantConnect.Util;
using QuantConnect.Interfaces;
using QuantConnect.Indicators;
using QuantConnect.Research;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Custom;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Notifications;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Fills;
using QuantConnect.Orders.Slippage;
using QuantConnect.Scheduling;
using QuantConnect.Securities;
using QuantConnect.Securities.Equity;
using QuantConnect.Securities.Forex;
using QuantConnect.Securities.Interfaces;
using QuantConnect.Configuration;
using QuantConnect.Lean.Engine;

Config.Reset();
Initializer.Start();
Api api = (Api)Initializer.GetSystemHandlers().Api;
var algorithmHandlers = Initializer.GetAlgorithmHandlers(researchMode: true);
