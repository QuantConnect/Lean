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
using System.Collections.Generic;
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Portfolio;

namespace QuantConnect.DataLibrary.Tests
{
    /// <summary>
    /// Quiver Quantitative is a provider of alternative data.
    /// </summary>
    public class MeanReversionPortfolioConstructionModelRegressionAlgorithm : QCAlgorithm
    {
        public override void Initialize()
        {
            SetStartDate(2020, 9, 1);
            SetEndDate(2021, 2, 28);
            SetCash(100000);
            
            SetSecurityInitializer(security => security.SetMarketPrice(GetLastKnownPrice(security)));

            foreach (var ticker in new List<string>{"AAPL", "SPY"})
            {
                AddEquity(ticker, Resolution.Daily);
            }
            
            AddAlpha(new ConstantAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromDays(1)));
            SetPortfolioConstruction(new MeanReversionPortfolioConstructionModel());
        }
    }
}
