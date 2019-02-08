#r "pythonnet/Python.Runtime.dll"
#r "QuantConnect.Algorithm.dll"
#r "QuantConnect.Algorithm.Framework.dll"
#r "QuantConnect.Common.dll"
#r "QuantConnect.Indicators.dll"
#r "QuantConnect.Jupyter.dll"
#r "NodaTime.dll"
#r "Accord.dll"
#r "Accord.Fuzzy.dll"
#r "Accord.Math.Core.dll"
#r "Accord.Math.dll"
#r "MathNet.Numerics.dll"
#r "Newtonsoft.Json.dll"
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
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.Framework;
using QuantConnect.Indicators;
using QuantConnect.Jupyter;