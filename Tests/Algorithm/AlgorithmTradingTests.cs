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
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using QuantConnect.Brokerages;
using Moq;

namespace QuantConnect.Tests.Algorithm
{
    [TestFixture]
    public class AlgorithmTradingTests
    {
        public TestCaseData[] TestParameters
        {
            get
            {
                return new[]
                {
                    new TestCaseData(1m),
                    new TestCaseData(2m),
                    new TestCaseData(100m),
                };
            }
        }

        public TestCaseData[] TestParametersDifferentMargins
        {
            get
            {
                return new[]
                {
                    new TestCaseData(0.5m, 0.25m),
                };
            }
        }

        /*****************************************************/
        //  Isostatic market conditions tests.
        /*****************************************************/

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_ZeroToLong(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 0);
            //Set price to $25 & Target 50%
            Update(msft, 25);
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 0.5m);
            Assert.AreEqual(2000, actual);
        }

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_ZeroToLong_SmallConstantFeeStructure(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 1);
            //Set price to $25 & Target 50%
            Update(msft, 25);
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 0.5m);
            // $1 in fees, so 1 share less than 2k from SetHoldings_ZeroToLong
            Assert.AreEqual(1999, actual);
        }

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_ZeroToLong_HighConstantFeeStructure(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 10000);
            //Set price to $25 & Target 50%
            Update(msft, 25);
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 0.5m);
            // 10k in fees = 400 shares (400*25), so 400 less than 2k from SetHoldings_ZeroToLong
            Assert.AreEqual(1600, actual);
        }

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_ZeroToShort(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 0);
            //Set price to $25 & Target 50%
            Update(msft, 25);
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -0.5m);
            Assert.AreEqual(-2000, actual);
        }

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_ZeroToShort_SmallConstantFeeStructure(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 1);
            //Set price to $25 & Target 50%
            Update(msft, 25);
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -0.5m);
            Assert.AreEqual(-1999, actual);
        }

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_ZeroToShort_HighConstantFeeStructure(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 10000);
            //Set price to $25 & Target 50%
            Update(msft, 25);
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -0.5m);
            Assert.AreEqual(-1600, actual);
        }

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_LongToLonger(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 0);
            //Set price to $25
            Update(msft, 25);
            //Half cash spent on 2000 MSFT shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, 2000);
            //Calculate the new holdings:
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 0.75m);
            Assert.AreEqual(1000, actual);
        }

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_LongToLonger_SmallConstantFeeStructure(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 1);
            //Set price to $25
            Update(msft, 25);
            //Half cash spent on 2000 MSFT shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, 2000);
            //Calculate the new holdings:
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 0.75m);
            Assert.AreEqual(999, actual);
        }

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_LongToLonger_HighConstantFeeStructure(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 10000);
            //Set price to $25
            Update(msft, 25);
            //Half cash spent on 2000 MSFT shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, 2000);
            //Calculate the new holdings:
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 0.75m);
            Assert.AreEqual(600, actual);
        }

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_LongerToLong(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 0);
            //Set price to $25
            Update(msft, 25);
            //75% cash spent on 3000 MSFT shares.
            algo.Portfolio.SetCash(25000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, 3000);
            //Sell all 2000 held:
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 0.5m);
            Assert.AreEqual(-1000, actual);
        }

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_LongerToLong_SmallConstantFeeStructure(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 1);
            //Set price to $25
            Update(msft, 25);
            //75% cash spent on 3000 MSFT shares.
            algo.Portfolio.SetCash(25000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, 3000);
            //Sell all 2000 held:
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 0.5m);
            Assert.AreEqual(-999, actual);
        }

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_LongerToLong_HighConstantFeeStructure(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 10000);
            //Set price to $25
            Update(msft, 25);
            //75% cash spent on 3000 MSFT shares.
            algo.Portfolio.SetCash(25000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, 3000);
            //Sell all 2000 held:
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 0.5m);
            Assert.AreEqual(-600, actual);
        }

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_LongToZero(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 0);
            //Set price to $25
            Update(msft, 25);
            //Half cash spent on 2000 MSFT shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, 2000);
            //Sell all 2000 held:
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 0m);
            Assert.AreEqual(-2000, actual);
        }

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_LongToZero_SmallConstantFeeStructure(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 1);
            //Set price to $25
            Update(msft, 25);
            //Half cash spent on 2000 MSFT shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, 2000);
            //Sell all 2000 held:
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 0m);
            Assert.AreEqual(-2000, actual);
        }

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_LongToZero_HighConstantFeeStructure(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 10000);
            //Set price to $25
            Update(msft, 25);
            //Half cash spent on 2000 MSFT shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, 2000);
            //Sell all 2000 held:
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 0m);
            Assert.AreEqual(-2000, actual);
        }

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_LongToShort(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 0);
            //Set price to $25
            Update(msft, 25);
            //Half cash spent on 2000 MSFT shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, 2000);

            //Sell all 2000 held + -2000 to get to -50%
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -0.5m);
            Assert.AreEqual(-4000, actual);
        }

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_LongToShort_SmallConstantFeeStructure(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 1);
            //Set price to $25
            Update(msft, 25);
            //Half cash spent on 2000 MSFT shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, 2000);

            //Sell all 2000 held + -1999 to get to -50%
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -0.5m);
            Assert.AreEqual(-3999, actual);
        }

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_LongToShort_HighConstantFeeStructure(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 10000);
            //Set price to $25
            Update(msft, 25);
            //Half cash spent on 2000 MSFT shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, 2000);

            //Sell all 2000 held + -1600 to get to -50%
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -0.5m);
            Assert.AreEqual(-3600, actual);
        }

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_HalfLongToFullShort(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 0);
            //Set price to $25
            Update(msft, 25);
            //Half cash spent on 2000 MSFT shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, 2000);

            //Sell all 2000 held + -4000 to get to -100%
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -1m);
            Assert.AreEqual(-6000, actual);
        }

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_HalfLongToFullShort_SmallConstantFeeStructure(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 1);
            //Set price to $25
            Update(msft, 25);
            //Half cash spent on 2000 MSFT shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, 2000);

            //Sell all 2000 held + -3999 to get to -100%
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -1m);
            Assert.AreEqual(-5999, actual);
        }

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_HalfLongToFullShort_HighConstantFeeStructure(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 10000);
            //Set price to $25
            Update(msft, 25);
            //Half cash spent on 2000 MSFT shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, 2000);

            //Sell all 2000 held + -3600 to get to -100%
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -1m);
            Assert.AreEqual(-5600, actual);
        }

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_ShortToZero(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 0);
            //Set price to $25
            Update(msft, 25);
            //Half cash spent on 2000 MSFT shares.
            algo.Portfolio.SetCash(150000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, -2000);
            //Buy 2000 to get to 0 holdings.
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 0m);
            Assert.AreEqual(2000, actual);
        }

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_ShortToZero_SmallConstantFeeStructure(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 1);
            //Set price to $25
            Update(msft, 25);
            //Half cash spent on 2000 MSFT shares.
            algo.Portfolio.SetCash(150000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, -2000);
            //Buy 2000 to get to 0 holdings.
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 0m);
            Assert.AreEqual(2000, actual);
        }

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_ShortToZero_HighConstantFeeStructure(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 10000);
            //Set price to $25
            Update(msft, 25);
            //Half cash spent on 2000 MSFT shares.
            algo.Portfolio.SetCash(150000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, -2000);
            //Buy 2000 to get to 0 holdings.
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 0m);
            Assert.AreEqual(2000, actual);
        }

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_ShortToShorter(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 0);
            //Set price to $25
            Update(msft, 25);
            //Sold -2000 MSFT shares, +50k cash
            algo.Portfolio.SetCash(150000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, -2000);

            // Cash: 150k
            // MSFT: -50k
            // TPV:  100k

            // we should end with -3000 = -.75*(100k/25)

            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -0.75m);
            Assert.AreEqual(-1000, actual);
        }

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_ShortToShorter_SmallConstantFeeStructure(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 1);
            //Set price to $25
            Update(msft, 25);
            //Sold -2000 MSFT shares, +50k cash
            algo.Portfolio.SetCash(150000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, -2000);

            // Cash: 150k
            // MSFT: -50k
            // TPV:  100k

            // we should end with -3000 = -.75*(100k/25)

            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -0.75m);
            Assert.AreEqual(-999, actual);
        }

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_ShortToShorter_HighConstantFeeStructure(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 10000);
            //Set price to $25
            Update(msft, 25);
            //Sold -2000 MSFT shares, +50k cash
            algo.Portfolio.SetCash(150000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, -2000);

            // Cash: 150k
            // MSFT: -50k
            // TPV:  100k

            // we should end with -3000 = -.75*(100k/25)

            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -0.75m);
            Assert.AreEqual(-600, actual);
        }

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_ShortToLong(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 0);
            //Set price to $25
            Update(msft, 25);
            //Sold -2000 MSFT shares, +50k cash
            algo.Portfolio.SetCash(150000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, -2000);
            // TPV: 150k - 50k = 100k*.5=50k @ 25 = 2000, so we need 4000 since we start at -2k
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 0.5m);
            Assert.AreEqual(4000, actual);
        }

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_ShortToLong_SmallConstantFeeStructure(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 1);
            //Set price to $25
            Update(msft, 25);
            //Sold -2000 MSFT shares, +50k cash
            algo.Portfolio.SetCash(150000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, -2000);
            // TPV: 150k - 50k = 100k*.5=50k @ 25 = 2000, so we need 4000 since we start at -2k
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 0.5m);
            Assert.AreEqual(3999, actual);
        }

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_ShortToLong_HighConstantFeeStructure(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 10000);
            //Set price to $25
            Update(msft, 25);
            //Sold -2000 MSFT shares, +50k cash
            algo.Portfolio.SetCash(150000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, -2000);
            // TPV: 150k - 50k = 100k*.5=50k @ 25 = 2000, so we need 4000 since we start at -2k
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 0.5m);
            Assert.AreEqual(3600, actual);
        }

        [Test, TestCaseSource("TestParametersDifferentMargins")]
        public void SetHoldings_HalfLongToHalfShort_DifferentMargins(decimal initialMarginRequirement, decimal maintenanceMarginRequirement)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, initialMarginRequirement, maintenanceMarginRequirement, 0);
            //Set price to $25
            Update(msft, 25);
            //Half cash spent on 2000 MSFT shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, 2000);

            //Sell all 2000 held + -2000 to get to -50%
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -0.5m);
            Assert.AreEqual(-4000, actual);
        }

        [Test, TestCaseSource("TestParametersDifferentMargins")]
        public void SetHoldings_HalfLongToHalfShort_SmallConstantFeeStructure_DifferentMargins(decimal initialMarginRequirement, decimal maintenanceMarginRequirement)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, initialMarginRequirement, maintenanceMarginRequirement, 1);
            //Set price to $25
            Update(msft, 25);
            //Half cash spent on 2000 MSFT shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, 2000);

            //Sell all 2000 held + -1999 to get to -50%
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -0.5m);
            Assert.AreEqual(-3999, actual);
        }

        [Test, TestCaseSource("TestParametersDifferentMargins")]
        public void SetHoldings_HalfLongToHalfShort_HighConstantFeeStructure_DifferentMargins(decimal initialMarginRequirement, decimal maintenanceMarginRequirement)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, initialMarginRequirement, maintenanceMarginRequirement, 10000);
            //Set price to $25
            Update(msft, 25);
            //Half cash spent on 2000 MSFT shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, 2000);

            //Sell all 2000 held + -1600 to get to -50%
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -0.5m);
            Assert.AreEqual(-3600, actual);
        }

        [Test, TestCaseSource("TestParametersDifferentMargins")]
        public void SetHoldings_HalfLongToFullShort_DifferentMargins(decimal initialMarginRequirement, decimal maintenanceMarginRequirement)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, initialMarginRequirement, maintenanceMarginRequirement, 0);
            //Set price to $25
            Update(msft, 25);
            //Half cash spent on 2000 MSFT shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, 2000);

            //Sell all 2000 held + -4000 to get to -100%
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -1m);
            Assert.AreEqual(-6000, actual);
        }

        [Test, TestCaseSource("TestParametersDifferentMargins")]
        public void SetHoldings_HalfLongToFullShort_SmallConstantFeeStructure_DifferentMargins(decimal initialMarginRequirement, decimal maintenanceMarginRequirement)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, initialMarginRequirement, maintenanceMarginRequirement, 1);
            //Set price to $25
            Update(msft, 25);
            //Half cash spent on 2000 MSFT shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, 2000);

            //Sell all 2000 held + -3999 to get to -100%
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -1m);
            Assert.AreEqual(-5999, actual);
        }

        [Test, TestCaseSource("TestParametersDifferentMargins")]
        public void SetHoldings_HalfLongToFullShort_HighConstantFeeStructure_DifferentMargins(decimal initialMarginRequirement, decimal maintenanceMarginRequirement)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, initialMarginRequirement, maintenanceMarginRequirement, 10000);
            //Set price to $25
            Update(msft, 25);
            //Half cash spent on 2000 MSFT shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, 2000);

            //Sell all 2000 held + -3600 to get to -100%
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -1m);
            Assert.AreEqual(-5600, actual);
        }

        [Test, TestCaseSource("TestParametersDifferentMargins")]
        public void SetHoldings_HalfLongToFull2xShort_DifferentMargins(decimal initialMarginRequirement, decimal maintenanceMarginRequirement)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, initialMarginRequirement, maintenanceMarginRequirement, 0);
            //Set price to $25
            Update(msft, 25);
            //Half cash spent on 2000 MSFT shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, 2000);

            //Sell all 2000 held + -8000 to get to -200%
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -2m);
            Assert.AreEqual(-10000, actual);
        }

        [Test, TestCaseSource("TestParametersDifferentMargins")]
        public void SetHoldings_HalfLongToFull2xShort_SmallConstantFeeStructure_DifferentMargins(decimal initialMarginRequirement, decimal maintenanceMarginRequirement)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, initialMarginRequirement, maintenanceMarginRequirement, 1);
            //Set price to $25
            Update(msft, 25);
            //Half cash spent on 2000 MSFT shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, 2000);

            //Sell all 2000 held + -7999 to get to -200%
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -2m);
            Assert.AreEqual(-9999, actual);
        }

        [Test, TestCaseSource("TestParametersDifferentMargins")]
        public void SetHoldings_HalfLongToFull2xShort_HighConstantFeeStructure_DifferentMargins(decimal initialMarginRequirement, decimal maintenanceMarginRequirement)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, initialMarginRequirement, maintenanceMarginRequirement, 10000);
            //Set price to $25
            Update(msft, 25);
            //Half cash spent on 2000 MSFT shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, 2000);

            //Sell all 2000 held + -7200 to get to -200%
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -2m);
            Assert.AreEqual(-9200, actual);
        }


        /*****************************************************/
        //  Rising market conditions tests.
        /*****************************************************/

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_LongFixed_PriceRise(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 0);
            //Set price to $25
            Update(msft, 25);

            //Half cash spent on 2000 MSFT shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, 2000);

            //Price rises to $50.
            Update(msft, 50);

            //Now: 2000 * 50 = $100k Holdings, $50k Cash: $150k.
            //Calculate the new holdings for 50% MSFT::
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 0.5m);

            //Need to sell $25k so 50% of $150k: $25k / $50-share = -500 shares
            Assert.AreEqual(-500, actual);
        }

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_LongFixed_PriceRise_SmallConstantFeeStructure(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 1);
            //Set price to $25
            Update(msft, 25);

            //Half cash spent on 2000 MSFT shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, 2000);

            //Price rises to $50.
            Update(msft, 50);

            //Now: 2000 * 50 = $100k Holdings, $50k Cash: $150k.
            //Calculate the new holdings for 50% MSFT::
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 0.5m);

            //Need to sell $25k so 50% of $150k: $25k / $50-share = -500 shares, -1 in fees
            Assert.AreEqual(-499, actual);
        }

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_LongFixed_PriceRise_HighConstantFeeStructure(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 10000);
            //Set price to $25
            Update(msft, 25);

            //Half cash spent on 2000 MSFT shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, 2000);

            //Price rises to $50.
            Update(msft, 50);

            //Now: 2000 * 50 = $100k Holdings, $50k Cash: $150k.
            //Calculate the new holdings for 50% MSFT::
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 0.5m);

            //Need to sell $25k so 50% of $150k: $25k / $50-share = -500 shares, -200 in fees
            Assert.AreEqual(-300, actual);
        }

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_LongToLonger_PriceRise(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 0);
            //Set price to $25
            Update(msft, 25);
            //Half cash spent on 2000 MSFT shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, 2000);

            //Price rises to $50.
            Update(msft, 50);

            //Now: 2000 * 50 = $100k Holdings, $50k Cash: $150k. MSFT is already 66% of holdings.
            //Calculate the order for 75% MSFT:
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 0.75m);

            //Need to buy to make position $112.5k == $12.5k / 50 = 250 shares
            Assert.AreEqual(250, actual);
        }

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_LongToLonger_PriceRise_SmallConstantFeeStructure(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 1);
            //Set price to $25
            Update(msft, 25);
            //Half cash spent on 2000 MSFT shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, 2000);

            //Price rises to $50.
            Update(msft, 50);

            //Now: 2000 * 50 = $100k Holdings, $50k Cash: $150k. MSFT is already 66% of holdings.
            //Calculate the order for 75% MSFT:
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 0.75m);

            //Need to buy to make position $112.5k == $12.5k / 50 = 250 shares, -1 in fees = 49
            Assert.AreEqual(249, actual);
        }

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_LongToLonger_PriceRise_HighConstantFeeStructure(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 10000);
            //Set price to $25
            Update(msft, 25);
            //Half cash spent on 2000 MSFT shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, 2000);

            //Price rises to $50.
            Update(msft, 50);

            //Now: 2000 * 50 = $100k Holdings, $50k Cash: $150k. MSFT is already 66% of holdings.
            //Calculate the order for 75% MSFT:
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 0.75m);

            //Need to buy to make position $112.5k == $12.5k / 50 = 250 shares, -10k in fees = 50
            Assert.AreEqual(50, actual);
        }

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_LongerToLong_PriceRise(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 0);
            //Set price to $25
            Update(msft, 25);

            //75% cash spent on 3000 MSFT shares.
            algo.Portfolio.SetCash(25000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, 3000);

            //Price rises to $50.
            Update(msft, 50);

            //Now: 3000 * 50 = $150k Holdings, $25k Cash: $175k. MSFT is 86% of holdings.
            //Calculate the order for 50% MSFT:
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 0.5m);

            //Need to sell to 50% = 87.5k target from $150k = 62.5 / $50-share = 1250
            Assert.AreEqual(-1250, actual);
        }

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_LongToShort_PriceRise(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 0);
            //Set price to $25
            Update(msft, 25);
            //Half cash spent on 2000 MSFT shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, 2000);

            //Price rises to $50.
            Update(msft, 50);

            //Now: 2000 * 50 = $100k Holdings, $50k Cash: $150k. MSFT is 66% of holdings.
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -0.5m);

            // Need to hold -75k from $100k = delta: $175k / $50-share = -3500 shares.
            Assert.AreEqual(-3500, actual);
        }

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_ShortToShorter_PriceRise(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 0);
            //Set price to $25
            Update(msft, 25);
            //Sold -2000 MSFT shares, +50k cash
            algo.Portfolio.SetCash(150000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, -2000);

            //Price rises to $50.
            Update(msft, 50);

            // Cash: 150k
            // MSFT: -(2000*50) = -100K
            // TPV: 50k
            Assert.AreEqual(50000, algo.Portfolio.TotalPortfolioValue);

            // we should end with -750 shares (-.75*50000/50)
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -0.75m);

            // currently -2000, so plus 1250
            Assert.AreEqual(1250, actual);
        }

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_ShortToLong_PriceRise_ZeroValue(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 0);
            //Set price to $25
            Update(msft, 25);
            //Sold -2000 MSFT shares, +50k cash
            algo.Portfolio.SetCash(150000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, -2000);

            //Price rises to $50: holdings now worthless.
            Update(msft, 50m);

            //Now: 2000 * 50 = $0k Net Holdings, $50k Cash: $50k. MSFT is 0% of holdings.
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 0.5m);

            //We want to be 50% long, this is currently +2000 holdings + 50% 50k = $25k/ $50-share=500
            Assert.AreEqual(2500, actual);
        }

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_ShortToLong_PriceRise(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 0);
            //Set price to $25
            Update(msft, 25);
            //Sold -2000 MSFT shares, +50k cash
            algo.Portfolio.SetCash(150000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, -2000);

            //Price rises to $50
            Update(msft, 50m);

            // Cash: 150k
            // MSFT: -50*2000=100k
            // TPV: 50k
            Assert.AreEqual(50000, algo.Portfolio.TotalPortfolioValue);

            // 50k*0.5=25k = 500 end holdings
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 0.5m);

            // 500 will makes us 50% tpv, we hold -2000, so 2500 buy
            Assert.AreEqual(2500, actual);
        }



        /*****************************************************/
        //  Falling market conditions tests.
        /*****************************************************/

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_ShortFixed_PriceFall(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 0);
            //Set price to $25
            Update(msft, 25);
            //Sold -2000 MSFT shares, +50k cash
            algo.Portfolio.SetCash(150000);
            algo.Portfolio[Symbols.MSFT].SetHoldings(25, -2000);

            Update(msft, 12.5m);

            // Cash: 150k
            // MSFT: -25k
            // TPV : 125k

            // -50% of 125 = (62.5k) @ 12.5/share = -5000

            // to get to -5000 we'll need to short another 3000
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -0.5m);
            Assert.AreEqual(-3000, actual);
        }

        /*************************************************************************/
        //  Rounding the order quantity to the nearest multiple of lot size test
        /*************************************************************************/

        [Test]
        public void SetHoldings_Long_RoundOff()
        {
            var algo = new QCAlgorithm();
            algo.AddSecurity(SecurityType.Forex, "EURUSD");
            algo.SetCash(100000);
            algo.SetBrokerageModel(BrokerageName.FxcmBrokerage);
            algo.Securities[Symbols.EURUSD].TransactionModel = new ConstantFeeTransactionModel(0);
            Security eurusd = algo.Securities[Symbols.EURUSD];
            // Set Price to $26
            Update(eurusd, 26);
            // So 100000/26 = 3846, After Rounding off becomes 3000
            var actual = algo.CalculateOrderQuantity(Symbols.EURUSD, 1m);
            Assert.AreEqual(3000m, actual);

        }

        [Test]
        public void SetHoldings_Short_RoundOff()
        {
            var algo = new QCAlgorithm();
            algo.AddSecurity(SecurityType.Forex, "EURUSD");
            algo.SetCash(100000);
            algo.SetBrokerageModel(BrokerageName.FxcmBrokerage);
            algo.Securities[Symbols.EURUSD].TransactionModel = new ConstantFeeTransactionModel(0);
            Security eurusd = algo.Securities[Symbols.EURUSD];
            // Set Price to $26
            Update(eurusd, 26);
            // So -100000/26 = -3846, After Rounding off becomes -3000
            var actual = algo.CalculateOrderQuantity(Symbols.EURUSD, -1m);
            Assert.AreEqual(-3000m, actual);
        }

        [Test]
        public void SetHoldings_Long_ToZero_RoundOff()
        {
            var algo = new QCAlgorithm();
            algo.AddSecurity(SecurityType.Forex, "EURUSD");
            algo.SetCash(10000);
            algo.SetBrokerageModel(BrokerageName.FxcmBrokerage);
            algo.Securities[Symbols.EURUSD].TransactionModel = new ConstantFeeTransactionModel(0);
            Security eurusd = algo.Securities[Symbols.EURUSD];
            // Set Price to $25
            Update(eurusd, 25);
            // So 10000/25 = 400, After Rounding off becomes 0
            var actual = algo.CalculateOrderQuantity(Symbols.EURUSD, 1m);
            Assert.AreEqual(0m, actual);
        }

        //[Test]
        //public void SetHoldings_LongToLonger_PriceRise()
        //{
        //    var algo = GetAlgorithm();
        //    //Set price to $25
        //    Update(msft, 25));
        //    //Half cash spent on 2000 MSFT shares.
        //    algo.Portfolio.SetCash(50000);
        //    algo.Portfolio[Symbols.MSFT].SetHoldings(25, 2000);

        //    //Price rises to $50.
        //    Update(msft, 50));

        //    //Now: 2000 * 50 = $100k Holdings, $50k Cash: $150k. MSFT is already 66% of holdings.
        //    //Calculate the order for 75% MSFT:
        //    var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 0.75m);

        //    //Need to buy to make position $112.5k == $12.5k / 50 = 250 shares
        //    Assert.AreEqual(250, actual);
        //}

        //[Test]
        //public void SetHoldings_LongerToLong_PriceRise()
        //{
        //    var algo = GetAlgorithm();
        //    //Set price to $25
        //    Update(msft, 25));

        //    //75% cash spent on 3000 MSFT shares.
        //    algo.Portfolio.SetCash(25000);
        //    algo.Portfolio[Symbols.MSFT].SetHoldings(25, 3000);

        //    //Price rises to $50.
        //    Update(msft, 50));

        //    //Now: 3000 * 50 = $150k Holdings, $25k Cash: $175k. MSFT is 86% of holdings.
        //    //Calculate the order for 50% MSFT:
        //    var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 0.5m);

        //    //Need to sell to 50% = 87.5k target from $150k = 62.5 / $50-share = 1250
        //    Assert.AreEqual(-1250, actual);
        //}


        //[Test]
        //public void SetHoldings_LongToShort_PriceRise()
        //{
        //    var algo = GetAlgorithm();
        //    //Set price to $25
        //    Update(msft, 25));
        //    //Half cash spent on 2000 MSFT shares.
        //    algo.Portfolio.SetCash(50000);
        //    algo.Portfolio[Symbols.MSFT].SetHoldings(25, 2000);

        //    //Price rises to $50.
        //    Update(msft, 50));

        //    //Now: 2000 * 50 = $100k Holdings, $50k Cash: $150k. MSFT is 66% of holdings.
        //    var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -0.5m);

        //    // Need to hold -75k from $100k = delta: $175k / $50-share = -3500 shares.
        //    Assert.AreEqual(-3500, actual);
        //}

        //[Test]
        //public void SetHoldings_ShortToShorter_PriceRise()
        //{
        //    var algo = GetAlgorithm();
        //    //Set price to $25
        //    Update(msft, 25));
        //    //Half cash spent on -2000 MSFT shares.
        //    algo.Portfolio.SetCash(50000);
        //    algo.Portfolio[Symbols.MSFT].SetHoldings(25, -2000);

        //    //Price rises to $50.
        //    Update(msft, 50));

        //    //Now: 2000 * 50 = $0k Net Holdings, $50k Cash: $50k. MSFT is 0% of holdings.
        //    var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -0.75m);

        //    //Want to hold -75% of MSFT: 50k total, -37.5k / $50-share = -750 TOTAL.
        //    // Currently -2000, so net order +1250.
        //    Assert.AreEqual(1250, actual);
        //}

        //[Test]
        //public void SetHoldings_ShortToLong_PriceRise()
        //{
        //    var algo = GetAlgorithm();
        //    //Set price to $25
        //    Update(msft, 25));
        //    //Half cash spent on -2000 MSFT shares.
        //    algo.Portfolio.SetCash(50000);
        //    algo.Portfolio[Symbols.MSFT].SetHoldings(25, -2000);

        //    //Price rises to $50.
        //    Update(msft, 50));

        //    //Now: 2000 * 50 = $0k Net Holdings, $50k Cash: $50k. MSFT is 0% of holdings.
        //    var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 0.5m);

        //    //We want to be 50% long, this is currently +2000 holdings + 50% 50k = $25k/ $50-share=500
        //    Assert.AreEqual(2500, actual);
        //}

        [Test]
        public void OrderQuantityConversionTest()
        {
            Security msft;
            var algo = GetAlgorithm(out msft, 1, 0);
            //Set price to $25
            Update(msft, 25);

            algo.Portfolio.SetCash(150000);

            var mock = new Mock<IOrderProcessor>();
            var request = new Mock<Orders.SubmitOrderRequest>(null, null, null, null, null, null, null, null, null);
            mock.Setup(m => m.Process(It.IsAny<Orders.OrderRequest>())).Returns(new Orders.OrderTicket(null, request.Object));
            algo.Transactions.SetOrderProcessor(mock.Object);

            algo.Buy(Symbols.MSFT, 1);
            algo.Buy(Symbols.MSFT, 1.0);
            algo.Buy(Symbols.MSFT, 1.0m);
            algo.Buy(Symbols.MSFT, 1.0f);

            algo.Sell(Symbols.MSFT, 1);
            algo.Sell(Symbols.MSFT, 1.0);
            algo.Sell(Symbols.MSFT, 1.0m);
            algo.Sell(Symbols.MSFT, 1.0f);

            algo.Order(Symbols.MSFT, 1);
            algo.Order(Symbols.MSFT, 1.0);
            algo.Order(Symbols.MSFT, 1.0m);
            algo.Order(Symbols.MSFT, 1.0f);

            algo.MarketOrder(Symbols.MSFT, 1);
            algo.MarketOrder(Symbols.MSFT, 1.0);
            algo.MarketOrder(Symbols.MSFT, 1.0m);
            algo.MarketOrder(Symbols.MSFT, 1.0f);

            algo.MarketOnOpenOrder(Symbols.MSFT, 1);
            algo.MarketOnOpenOrder(Symbols.MSFT, 1.0);
            algo.MarketOnOpenOrder(Symbols.MSFT, 1.0m);

            algo.MarketOnCloseOrder(Symbols.MSFT, 1);
            algo.MarketOnCloseOrder(Symbols.MSFT, 1.0);
            algo.MarketOnCloseOrder(Symbols.MSFT, 1.0m);

            algo.LimitOrder(Symbols.MSFT, 1, 1);
            algo.LimitOrder(Symbols.MSFT, 1.0, 1);
            algo.LimitOrder(Symbols.MSFT, 1.0m, 1);

            algo.StopMarketOrder(Symbols.MSFT, 1, 1);
            algo.StopMarketOrder(Symbols.MSFT, 1.0, 1);
            algo.StopMarketOrder(Symbols.MSFT, 1.0m, 1);

            algo.StopLimitOrder(Symbols.MSFT, 1, 1, 2);
            algo.StopLimitOrder(Symbols.MSFT, 1.0, 1, 2);
            algo.StopLimitOrder(Symbols.MSFT, 1.0m, 1, 2);

            algo.SetHoldings(Symbols.MSFT, 1);
            algo.SetHoldings(Symbols.MSFT, 1.0);
            algo.SetHoldings(Symbols.MSFT, 1.0m);
            algo.SetHoldings(Symbols.MSFT, 1.0f);

            int expected = 32;
            Assert.AreEqual(expected, algo.Transactions.LastOrderId);
        }


        private QCAlgorithm GetAlgorithm(out Security msft, decimal leverage, decimal fee)
        {
            //Initialize algorithm
            var algo = new QCAlgorithm();
            algo.AddSecurity(SecurityType.Equity, "MSFT");
            algo.SetCash(100000);
            algo.Securities[Symbols.MSFT].TransactionModel = new ConstantFeeTransactionModel(fee);
            msft = algo.Securities[Symbols.MSFT];
            msft.SetLeverage(leverage);
            return algo;
        }

        private QCAlgorithm GetAlgorithm(out Security msft, decimal initialMarginRequirement, decimal maintenanceMarginRequirement, decimal fee)
        {
            //Initialize algorithm
            var algo = new QCAlgorithm();
            algo.AddSecurity(SecurityType.Equity, "MSFT");
            algo.SetCash(100000);
            algo.Securities[Symbols.MSFT].TransactionModel = new ConstantFeeTransactionModel(fee);
            msft = algo.Securities[Symbols.MSFT];
            msft.MarginModel = new SecurityMarginModel(initialMarginRequirement, maintenanceMarginRequirement);
            return algo;
        }

        private void Update(Security security, decimal close)
        {
            security.SetMarketPrice(new TradeBar
            {
                Time = DateTime.Now,
                Symbol = security.Symbol,
                Open = close,
                High = close,
                Low = close,
                Close = close
            });
        }
    }
}