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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Algorithm used to generate daily data on disk for the different <see cref="ConstituentsUniverse"/>.
    /// How to use me? search for 'CHANGE ME'
    /// 1- change the universe name
    /// 2- change the desired start and end date to generate
    /// 3- define the universe selection to use
    /// Data will be generated in:
    /// 'Globals.DataFolder\SecurityType\Market\universes\Resolution\{UniverseName}\{dates:yyyyMMdd}.csv'
    /// </summary>
    /// <remarks>The data produced by this algorithm is the one used by the
    /// <see cref="ConstituentsUniverseRegressionAlgorithm"/></remarks>
    /// <remarks>In the cloud, users can implement their own <see cref="ConstituentsUniverseData"/>
    /// (not using <see cref="SubscriptionTransportMedium.LocalFile"/>) that can fetch the files
    /// generated from this algorithm</remarks>
    public class ConstituentsUniverseDataGeneratorAlgorithm : QCAlgorithm
    {
        private readonly HashSet<Symbol> _currentSelection = new HashSet<Symbol>();
        private DateTime _currentDateTime = DateTime.MinValue;
        private readonly string _rootDataPath = Globals.DataFolder;
        private string _dataPath;
        private bool _skippedFirst;

        // Configuration properties: Only these are supported for now (Don't change me)
        private readonly SecurityType _securityType = SecurityType.Equity;
        private readonly string _market = Market.USA;
        private readonly Resolution _resolution = Resolution.Daily;

        // CHANGE ME
        private readonly string _universeName = "qctest";

        public override void Initialize()
        {
            // CHANGE ME
            SetStartDate(2013, 10, 07);   // Set Start Date
            SetEndDate(2013, 10, 11);     // Set End Date
            SetCash(100000);            // Set Strategy Cash

            UniverseSettings.Resolution = Resolution.Daily;
            _dataPath = Path.Combine(_rootDataPath,
                _securityType.SecurityTypeToLower(),
                _market,
                "universes",
                _resolution.ResolutionToLower(),
                _universeName);
            Directory.CreateDirectory(_dataPath);

            // CHANGE ME
            int step = 0;
            AddUniverse(coarse =>
            {
                step++;
                switch (step)
                {
                    case 1:
                    case 2:
                        return new[]
                        {
                            QuantConnect.Symbol.Create("QQQ", SecurityType.Equity, Market.USA),
                            QuantConnect.Symbol.Create("AAPL", SecurityType.Equity, Market.USA)
                        };
                    case 3:
                        return Enumerable.Empty<Symbol>();
                    case 4:
                    case 5:
                        return new[]
                        {
                            QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA),
                            QuantConnect.Symbol.Create("FB", SecurityType.Equity, Market.USA)
                        };
                    default:
                        throw new Exception("Unexpected step count");
                }
            });

            Schedule.On(DateRules.EveryDay(), TimeRules.At(23, 0), SaveConstituentsUniverseDataToDisk);
        }

        private void SaveConstituentsUniverseDataToDisk()
        {
            if (_skippedFirst && Time > _currentDateTime)
            {
                if (Time.DayOfWeek == DayOfWeek.Sunday
                    || Time.DayOfWeek == DayOfWeek.Monday)
                {
                    // we generate files from Tue to Saturday using current selected securities
                    return;
                }
                // files are for Mon to Friday
                _currentDateTime = Time.Date.AddDays(-1);

                var path = Path.Combine(_dataPath, $"{_currentDateTime:yyyyMMdd}.csv");
                File.Delete(path);
                if (_currentSelection.Count == 0)
                {
                    using (StreamWriter constituentsUniverseFile = new StreamWriter(path, append:true))
                    {
                        constituentsUniverseFile.WriteLine(
                            $"{QuantConnect.Symbol.None.Value},{QuantConnect.Symbol.None.ID.ToString()}");
                    }
                }
                else
                {
                    foreach (var symbol in _currentSelection)
                    {
                        using (StreamWriter constituentsUniverseFile = new StreamWriter(path, append: true))
                        {
                            constituentsUniverseFile.WriteLine($"{symbol.Value},{symbol.ID.ToString()}");
                        }
                    }
                }
            }
            _skippedFirst = true;
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            foreach (var added in changes.AddedSecurities)
            {
                Log($"AddedSecurities {added}");
                if (_currentSelection.Contains(added.Symbol))
                {
                    throw new Exception("Added symbol already selected");
                }
                _currentSelection.Add(added.Symbol);
            }

            foreach (var removed in changes.RemovedSecurities)
            {
                Log($"RemovedSecurities {removed}");
                if (!_currentSelection.Contains(removed.Symbol))
                {
                    throw new Exception("Removing symbol already deselected");
                }
                _currentSelection.Remove(removed.Symbol);
            }
        }
    }
}