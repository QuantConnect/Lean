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
using QuantConnect.Interfaces;
using System.Collections.Generic;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Tests.Common.Data.Fundamental
{
    public class TestFundamentalDataProvider : IFundamentalDataProvider
    {
        private readonly CoarseFundamentalDataProvider _coarseFundamentalData = new();

        private readonly Dictionary<string, double> _pERatio = new()
        {
            { "AAPL R735QTJ8XC9X", 13.012856d },
            { "IBM R735QTJ8XC9X", 12.394244d },
            { "AIG R735QTJ8XC9X", 8.185855d },
        };

        private readonly Dictionary<string, string> _industryTemplateCode = new()
        {
            { "AAPL R735QTJ8XC9X", "N" },
            { "IBM R735QTJ8XC9X", "N" },
            { "GOOG T1AZ164W5VTX", "N" },
            { "GOOCV VP83T1ZUHROL", "N" },
            { "NB R735QTJ8XC9X", "B" },
            { "AIG R735QTJ8XC9X", "I" },
        };

        private readonly Dictionary<string, double> _equityPerShareGrowthOneYear = new()
        {
            { "AAPL R735QTJ8XC9X", 0.091652d },
            { "IBM R735QTJ8XC9X", 0.280664d },
            { "GOOCV VP83T1ZUHROL", 0.196226d },
            { "NB R735QTJ8XC9X", 0.022944d },
        };

        private readonly Dictionary<string, long> _marketCap = new()
        {
            { "AIG R735QTJ8XC9X", 72866646492 },
            { "AAPL R735QTJ8XC9X", 469400291359 },
            { "IBM R735QTJ8XC9X", 192825068158 },
            { "GOOCV VP83T1ZUHROL", 375779584963 },
            { "NB R735QTJ8XC9X", 181116782342 },
        };

        public T Get<T>(DateTime time, SecurityIdentifier securityIdentifier, string name)
        {
            if (securityIdentifier == SecurityIdentifier.Empty)
            {
                return default;
            }

            switch (name)
            {
                case nameof(CoarseFundamental.Price):
                case nameof(CoarseFundamental.Value):
                case nameof(CoarseFundamental.Market):
                case nameof(CoarseFundamental.Volume):
                case nameof(CoarseFundamental.PriceFactor):
                case nameof(CoarseFundamental.SplitFactor):
                case nameof(CoarseFundamental.DollarVolume):
                    return _coarseFundamentalData.Get<T>(time, securityIdentifier, name);
                default:
                    return Get(time, securityIdentifier, name);
            }
        }

        private dynamic Get(DateTime time, SecurityIdentifier securityIdentifier, string name)
        {
            switch (name)
            {
                case nameof(CoarseFundamental.HasFundamentalData):
                    return true;
                case "CompanyProfile.MarketCap":
                    if(_marketCap.TryGetValue(securityIdentifier.ToString(), out var marketCap))
                    {
                        return marketCap;
                    }
                    return 0L;
                case "CompanyProfile.HeadquarterCity":
                    if (securityIdentifier.Symbol == "AAPL")
                    {
                        return "Cupertino";
                    }
                    return string.Empty;
                case "CompanyReference.IndustryTemplateCode":
                    if(_industryTemplateCode.TryGetValue(securityIdentifier.ToString(), out var  industryTemplateCode))
                    {
                        return industryTemplateCode;
                    }
                    return string.Empty;
                case "EarningRatios.EquityPerShareGrowth.OneYear":
                    if(_equityPerShareGrowthOneYear.TryGetValue(securityIdentifier.ToString(), out var ePSG))
                    {
                        return ePSG;
                    }
                    return 0d;
                case "ValuationRatios.PERatio":
                    if (_pERatio.TryGetValue(securityIdentifier.ToString(), out var peRatio))
                    {
                        return peRatio;
                    }
                    return 0d;
            }
            return null;
        }

        public void Initialize(IDataProvider dataProvider)
        {
            _coarseFundamentalData.Initialize(dataProvider);
        }
    }
}
