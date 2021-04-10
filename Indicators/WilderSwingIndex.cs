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
using System.Linq;
using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// This indicator calculates the Swing Index (SI) as defined by Welles Wilder
    /// in his book 'New Concepts in Technical Trading Systems'.
    ///
    /// Formula: SI = 50 * ( N / R ) * ( K / T )
    /// 
    /// Where:
    /// N [<see cref="N"/>]:
    ///     N = C2 - C1 + 0.5 * (C2 - O2) + 0.25 * (C1 - O1)
    /// 
    /// R [<see cref="R"/>]:
    ///     Values is determined by which expression has the highest value.
    ///
    ///     Expression: 
    ///         (1): |H2 - C1|
    ///         (2): |L2 - C1|
    ///         (3): |H2 - L2|
    ///
    ///     Corresponding Formulas:
    ///         (1): R = (H2 - C1) - 0.5 * (L2 - C1) + 0.25 * (C1 - O1)
    ///         (2): R = (L2 - C1) - 0.5 * (H2 - C1) + 0.25 * (C1 - O1)
    ///         (3): R = (H2 - L2) + 0.25 * (C1 - O1)
    ///         
    /// K: [<see cref="K"/>]
    ///     The larger value of the two following expressions
    ///
    ///     Expressions:
    ///         (1): |H1 - C2|
    ///         (2): |L1 - C2|
    /// 
    /// T:
    ///     The limit move, or the maximum change in price for the trading session.
    ///     
    ///     Types:
    ///         Fixed: T = <see cref="_fixedLimitMove"/>
    ///         Relative: T = <see cref="_percentLimitMove"/> * O1
    ///
    /// Where:
    ///     <list type="bullet">
    ///     <item>
    ///         <term>O1</term>
    ///         <description>Current Open Price</description>
    ///     </item>
    ///     <item>
    ///         <term>H1</term>
    ///         <description>Current Highest Price</description>
    ///     </item>
    ///     <item>
    ///         <term>L1</term>
    ///         <description>Current Lowest Price</description>
    ///     </item>
    ///     <item>
    ///         <term>C1</term>
    ///         <description>Current Closing Price</description>
    ///     </item>
    ///     <item>
    ///         <term>O2</term>
    ///         <description>Previous Open Price</description>
    ///     </item>
    ///     <item>
    ///         <term>H2</term>
    ///         <description>Previous Current Highest Price</description>
    ///     </item>
    ///     <item>
    ///         <term>L2</term>
    ///         <description>Previous Current Lowest Price</description>
    ///     </item>
    ///     <item>
    ///         <term>C2</term>
    ///         <description>Previous Closing Price</description>
    ///     </item>
    ///     </list>
    /// </summary>
    public class WilderSwingIndex : TradeBarIndicator, IIndicatorWarmUpPeriodProvider
    {
        protected IBaseDataBar _currentInput;
        protected IBaseDataBar _previousInput;

        public readonly bool _isRelative;

        protected readonly decimal _fixedLimitMove;
        protected readonly decimal _percentLimitMove;

        public string LimitMove
        {
            get
            {
                if (_isRelative)
                    return $"Fixed {_fixedLimitMove}";
                else
                    return $"Relative {_percentLimitMove}%";
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WilderSwingIndex"/> class using the specified name.
        /// </summary>
        /// <param name="name">A string for the name of this indicator.</param>
        /// <param name="fixedLimitMove">A decimal representing the fixed limit move value.</param>
        public WilderSwingIndex(string name, decimal fixedLimitMove)
            : base(name)
        {
            _isRelative = false;
            _fixedLimitMove = fixedLimitMove;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WilderSwingIndex"/> class using the default name.
        /// </summary>
        /// <param name="fixedLimitMove">A decimal representing the fixed limit move value.</param>
        public WilderSwingIndex(decimal fixedLimitMove)
            : this("SI", fixedLimitMove)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WilderSwingIndex"/> class using the specified name.
        /// </summary>
        /// <param name="name">A string for the name of this indicator.</param>
        /// <param name="relativeLimitMove">The maximum change in price for the trading session in basis points of the open price.</param>
        public WilderSwingIndex(string name, int relativeLimitMove)
            : base(name)
        {
            _isRelative = true;
            _percentLimitMove = (decimal)(relativeLimitMove / 10000);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WilderSwingIndex"/> class using the default name.
        /// </summary>
        /// <param name="relativeLimitMove">The maximum change in price for the trading session in basis points of the open price.</param>
        public WilderSwingIndex(int relativeLimitMove)
            : this("SI", relativeLimitMove)
        {
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized.
        /// </summary>
        public override bool IsReady => _currentInput != null;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod => 2;

        /// <summary>
        /// Computes the next value of this indicator from the given state.
        /// </summary>
        /// <param name="input">The input given to the indicator.</param>
        /// <returns>A new value for this indicator.</returns>
        protected override decimal ComputeNextValue(TradeBar input)
        {
            if (!IsReady)
            {
                _currentInput = input;
                return 0m;
            }

            _previousInput = _currentInput;
            _currentInput = input;

            return 50m * (N / R) * (K / T);
        }

        /// <summary>
        ///  
        /// </summary>
        /// <returns>The value of <see cref="K"/>.</returns>
        protected virtual decimal K
        {
            get
            {
                return Math.Max(
                    Math.Abs(_currentInput.High - _previousInput.Close),
                    Math.Abs(_currentInput.Low - _previousInput.Close));
            }
        }

        /// <summary>
        ///  
        /// </summary>
        /// <returns>The value of <see cref="T"/>.</returns>
        protected virtual decimal T
        {
            get
            {
                if (_isRelative)
                    return _currentInput.Open * _percentLimitMove;
                else
                    return _fixedLimitMove;
            }
        }

        /// <summary>
        ///  
        /// </summary>
        /// <returns>The value of <see cref="N"/>.</returns>
        protected virtual decimal N
        {
            get
            {
                return _previousInput.Close
                    - _currentInput.Close
                    + 0.5m * (_previousInput.Close - _previousInput.Open)
                    + 0.25m * (_currentInput.Close - _currentInput.Open);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>The value of <see cref="R"/>.</returns>
        protected virtual decimal R
        {
            get
            {
                Dictionary<int, decimal> conditionMap = _scenarioMap
                    .Select(x =>
                    {
                        Func<IBaseDataBar, IBaseDataBar, decimal>
                            conditionFunction = x.Value[0];

                        decimal conditionValue =
                            conditionFunction(
                                _currentInput,
                                _previousInput);

                        return new KeyValuePair<int, decimal>(
                            x.Key,
                            conditionValue);
                    })
                    .ToDictionary(x => x.Key, x => x.Value);

                var scenarioKey = conditionMap
                    .FirstOrDefault(x => x.Value == conditionMap.Values.Max())
                    .Key;

                Func<IBaseDataBar, IBaseDataBar, decimal>[] scenario;

                if (_scenarioMap.TryGetValue(scenarioKey, out scenario))
                {
                    Func<IBaseDataBar, IBaseDataBar, decimal> resultFunction = scenario[1];

                    return resultFunction(_currentInput, _previousInput);
                }
                else
                {
                    return 0m;
                }
            }
        }

        /// <summary>
        /// Mapping between the number for each scenario when calculating <see cref="R"/>
        /// as well as the expressions for determining what scenario is used
        /// and the corresponding functions used to calculate <see cref="R"/>.
        /// </summary>
        protected Dictionary<int, Func<IBaseDataBar, IBaseDataBar, decimal>[]> _scenarioMap =
            new Dictionary<int, Func<IBaseDataBar, IBaseDataBar, decimal>[]>()
            {
                [0] = new[]
                {
                    _conditionFunctionForFirstScenario,
                    _resultFunctionForFirstScenario
                },
                [1] = new[]
                {
                    _conditionFunctionForSecondScenario,
                    _resultFunctionForSecondScenario
                },
                [2] = new[]
                {
                    _conditionFunctionForSecondScenario,
                    _resultFunctionForThirdScenario
                }
            };

        /// <summary>
        /// Calculates the value for the first expression used in determing the
        /// the function to be used when calculating <see cref="R"/>.
        /// </summary>
        /// <param name="currentInput">The current data bar input.</param>
        /// <param name="previousInput">The previous data bar input.</param>
        /// <returns>
        /// The result of the expression.
        /// </returns>
        private static Func<IBaseDataBar, IBaseDataBar, decimal> _conditionFunctionForFirstScenario =
            (currentInput, previousInput) => Math.Abs(currentInput.High - previousInput.Close);

        /// <summary>
        /// Used to calculate <see cref="R"/> when the first of
        /// the three expressions has the highest value.
        /// </summary>
        /// <param name="currentInput">The current data bar input.</param>
        /// <param name="previousInput">The previous data bar input.</param>
        /// <returns>
        /// The result for <see cref="R"/>.
        /// </returns>
        private static Func<IBaseDataBar, IBaseDataBar, decimal> _resultFunctionForFirstScenario =
            (currentInput, previousInput) =>
            {
                decimal result = currentInput.High
                    - previousInput.Close
                    - 0.5m * (currentInput.Low - previousInput.Close)
                    + 0.25m * (previousInput.Close - previousInput.Open);

                return Math.Abs(result);
            };

        /// <summary>
        /// Calculates the value for the second expression used in determing the
        /// the function to be used when calculating <see cref="R"/>.
        /// </summary>
        /// <param name="currentInput">The current data bar input.</param>
        /// <param name="previousInput">The previous data bar input.</param>
        /// <returns>
        /// The result of the expression.
        /// </returns>
        private static Func<IBaseDataBar, IBaseDataBar, decimal> _conditionFunctionForSecondScenario =
            (currentInput, previousInput) => Math.Abs(currentInput.Low - previousInput.Close);

        /// <summary>
        /// Used to calculate <see cref="R"/> when the second of
        /// the three expressions has the highest value.
        /// </summary>
        /// <param name="currentInput">The current data bar input.</param>
        /// <param name="previousInput">The previous data bar input.</param>
        /// <returns>
        /// The result for <see cref="R"/>.
        /// </returns>
        private static Func<IBaseDataBar, IBaseDataBar, decimal> _resultFunctionForSecondScenario =
            (currentInput, previousInput) =>
            {
                decimal result = currentInput.Low
                    - previousInput.Close
                    - 0.5m * (currentInput.High - previousInput.Close)
                    + 0.25m * (previousInput.Close - previousInput.Open);

                return Math.Abs(result);
            };

        /// <summary>
        /// Calculates the value for the third expression used in determing the
        /// the function to be used when calculating <see cref="R"/>.
        /// </summary>
        /// <param name="currentInput">The current data bar input.</param>
        /// <param name="previousInput">The previous data bar input.</param>
        /// <returns>
        /// The result of the expression.
        /// </returns>
        private static Func<IBaseDataBar, IBaseDataBar, decimal> _conditionFunctionForThirdScenario =
            (currentInput, previousInput) => Math.Abs(currentInput.High - currentInput.Low);

        /// <summary>
        /// Used to calculate <see cref="R"/> when the third of
        /// the three expressions has the highest value.
        /// </summary>
        /// <param name="currentInput">The current data bar input.</param>
        /// <param name="previousInput">The previous data bar input.</param>
        /// <returns>
        /// The result for <see cref="R"/>.
        /// </returns>
        private static Func<IBaseDataBar, IBaseDataBar, decimal> _resultFunctionForThirdScenario =
            (currentInput, previousInput) =>
            {
                decimal result = currentInput.High
                    - currentInput.Low
                    + 0.25m * (previousInput.Close - previousInput.Open);

                return Math.Abs(result);
            };
    }
}