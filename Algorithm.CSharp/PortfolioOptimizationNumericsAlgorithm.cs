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

using QuantConnect.Data;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;
using System.Collections.Generic;
using System.Linq;
using System;
using QuantConnect.Indicators;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm uses Math.NET Numerics library, specifically Linear Algebra object (Vector and Matrix) and operations, in order to solve a portfolio optimization problem.
    /// </summary>
    /// <meta name="tag" content="strategy example" />
    /// <meta name="tag" content="portfolio optimization" />
    public class PortfolioOptimizationNumericsAlgorithm : QCAlgorithm
    {
        private const double _targetReturn = 0.1;
        private const double _riskFreeRate = 0.01;
        private double _lagrangeMultiplier;
        private double _portfolioRisk;
        private Matrix<double> Sigma;
        private List<SymbolData> SymbolDataList;

        public Vector<double> DiscountMeanVector
        {
            get
            {
                if (SymbolDataList == null)
                {
                    return null;
                }

                return
                    Vector<double>.Build.DenseOfArray(SymbolDataList.Select(x => (double)x.Return).ToArray()) -
                    Vector<double>.Build.Dense(SymbolDataList.Count, _riskFreeRate);
            }
        }


        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            SetCash(100000);             //Set Strategy Cash
            // Find more symbols here: http://quantconnect.com/data
            AddEquity("SPY", Resolution.Daily);
            AddEquity("AIG", Resolution.Daily);
            AddEquity("BAC", Resolution.Daily);
            AddEquity("IBM", Resolution.Daily);

            var allHistoryBars = new List<double[]>();
            SymbolDataList = new List<SymbolData>();

            foreach (var security in Securities)
            {
                var history = History(security.Key, TimeSpan.FromDays(365));
                allHistoryBars.Add(history.Select(x => (double)x.Value).ToArray());
                SymbolDataList.Add(new SymbolData(security.Key, history));
            }

            // Diagonal Matrix with each security risk (standard deviation)
            var S = Matrix<double>.Build.DenseOfDiagonalArray(SymbolDataList.Select(x => (double)x.Risk).ToArray());

            // Computes Correlation Matrix (using Math.NET Numerics Statistics)
            var R = MathNet.Numerics.Statistics.Correlation.PearsonMatrix(allHistoryBars);

            // Computes Covariance Matrix (using Math.NET Numerics Linear Algebra)
            Sigma = S * R * S;

            ComputeLagrangeMultiplier();
            ComputeWeights();
            ComputePortfolioRisk();

            Log($"Lagrange Multiplier: {_lagrangeMultiplier.ToStringInvariant("7:F4")}");
            Log($"Portfolio Risk:      {_portfolioRisk.ToStringInvariant("7:P2")} ");
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                foreach (var symbolData in SymbolDataList.OrderBy(x => x.Weight))
                {
                    SetHoldings(symbolData.Symbol, symbolData.Weight);
                    Debug("Purchased Stock: " + symbolData);
                }
            }
        }

        /// <summary>
        /// Computes Lagrange Multiplier
        /// </summary>
        private void ComputeLagrangeMultiplier()
        {
            var denominatorMatrix = DiscountMeanVector * Sigma.Inverse() * DiscountMeanVector.ToColumnMatrix();

            _lagrangeMultiplier = (_targetReturn - _riskFreeRate) / denominatorMatrix.ToArray().First();
        }

        /// <summary>
        /// Computes weight for each risky asset
        /// </summary>
        private void ComputeWeights()
        {
            var weights = _lagrangeMultiplier * Sigma.Inverse() * DiscountMeanVector.ToColumnMatrix();

            for (var i = 0; i < weights.RowCount; i++)
            {
                SymbolDataList[i].SetWeight(weights.ToArray()[i, 0]);
            }
        }

        /// <summary>
        /// Computes Portfolio Risk
        /// </summary>
        private void ComputePortfolioRisk()
        {
            var weights = Vector<double>.Build.DenseOfArray(SymbolDataList.Select(x => (double)x.Return).ToArray());
            var portfolioVarianceMatrix = weights * Sigma * weights.ToColumnMatrix();
            _portfolioRisk = Math.Sqrt(portfolioVarianceMatrix.ToArray().First());
        }

        /// <summary>
        /// Symbol Data class to store security data (Return, Risk, Weight)
        /// </summary>
        class SymbolData
        {
            private RateOfChange ROC = new RateOfChange(2);
            private SimpleMovingAverage SMA;
            private StandardDeviation STD;
            public Symbol Symbol { get; private set; }
            public decimal Return { get { return SMA.Current; }  }
            public decimal Risk { get { return STD.Current; } }
            public decimal Weight { get; private set; }

            public SymbolData(Symbol symbol, IEnumerable<BaseData> history)
            {
                Symbol = symbol;
                SMA = new SimpleMovingAverage(365).Of(ROC);
                STD = new StandardDeviation(365).Of(ROC);

                foreach (var data in history)
                {
                    Update(data);
                }
            }

            public void Update(BaseData data)
            {
                ROC.Update(data.Time, data.Value);
            }

            public void SetWeight(double value)
            {
                Weight = (decimal)value;
            }

            public override string ToString()
            {
                return Invariant($"{Symbol.Value}: {Weight,10:P2}\t{Return,10:P2}\t{Risk,10:P2}");
            }
        }
    }
}
