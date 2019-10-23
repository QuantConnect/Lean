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
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using QuantConnect.Brokerages;
using Moq;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Tests.Common.Securities;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Algorithm
{
    [TestFixture]
    public class AlgorithmTradingTests
    {
        private static FakeOrderProcessor _fakeOrderProcessor;
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
            Assert.AreEqual(1995m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
        }

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_ZeroToLong_SmallConstantFeeStructure(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 1);
            //Set price to $25 & Target 50%
            Update(msft, 25);
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 0.5m);
            // $100k total value * 0.5 target * 0.9975 FreePortfolioValuePercentage / 25 ~= 1995 - fees
            Assert.AreEqual(1994m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
        }

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_ZeroToLong_HighConstantFeeStructure(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 10000);
            //Set price to $25 & Target 50%
            Update(msft, 25);
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 0.5m);

            // ($100k total value - 10 k fees) * 0.5 target * 0.9975 FreePortfolioValuePercentage / 25 ~= 1795m
            Assert.AreEqual(1795m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
        }

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_ZeroToShort(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 0);
            //Set price to $25 & Target 50%
            Update(msft, 25);
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -0.5m);
            Assert.AreEqual(-1995m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
        }

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_ZeroToShort_SmallConstantFeeStructure(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 1);
            //Set price to $25 & Target 50%
            Update(msft, 25);
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -0.5m);
            Assert.AreEqual(-1994m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
        }

        [Test, TestCaseSource("TestParameters")]
        public void SetHoldings_ZeroToShort_HighConstantFeeStructure(decimal leverage)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, leverage, 10000);
            //Set price to $25 & Target 50%
            Update(msft, 25);
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -0.5m);

            // ($100k total value - 10 k fees) * -0.5 target * 0.9975 FreePortfolioValuePercentage / 25 ~= -1795m
            Assert.AreEqual(-1795m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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
            Assert.AreEqual(992m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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
            Assert.AreEqual(992m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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
            Assert.AreEqual(693m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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
            Assert.AreEqual(-1005m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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
            Assert.AreEqual(-1005m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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
            Assert.AreEqual(-1204m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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

            //Need to sell to make position ($100k total value * -0.5 target * 0.9975 buffer - $50k current holdings) / 50 =~ -3995m
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -0.5m);
            Assert.AreEqual(-3995m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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

            //Need to sell to make position ($100k total value * -0.5 target * 0.9975 buffer - $50k current holdings) / 50 =~ -3995m - 1 due to fee
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -0.5m);
            Assert.AreEqual(-3994m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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

            //Need to sell to make position (($100k total value - 10 K)* -0.5 target * 0.9975 buffer - $50k current holdings) / 25 =~ -3795m
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -0.5m);
            Assert.AreEqual(-3795m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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

            //Need to sell to make position ($100k total value * -1 target * 0.9975 buffer - $50k current holdings) / 50 =~ -5990m
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -1m);
            Assert.AreEqual(-5990m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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

            //Need to sell to make position ($100k total value * -1 target * 0.9975 buffer - $50k current holdings) / 50 =~ -5990m - 1 due to fee
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -1m);
            Assert.AreEqual(-5989m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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

            // Fee is 10k / 25 ~= 400 shares
            // Need to sell to make position (($100k total value - 10k fees) * -1 target * 0.9975 buffer - $50k current holdings) / 25 =~ -5591m
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -1m);
            Assert.AreEqual(-5591m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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

            // ($100k total value * -0.75 target * 0.9975 buffer - $50k current holdings) / 25 =~ 992m
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -0.75m);
            Assert.AreEqual(-992m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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
            // ($100k total value * -0.75 target * 0.9975 buffer - $50k current holdings) / 25 =~ 992m
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -0.75m);
            Assert.AreEqual(-992m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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
            // (($100k total value - 10k fees) * -0.75 target * 0.9975 buffer + $50k current holdings) / 25 =~ -693m
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -0.75m);
            Assert.AreEqual(-693m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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
            // ($100k total value * 0.5 target * 0.9975 buffer - $50k current holdings) / 25 =~ 3995m
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 0.5m);
            Assert.AreEqual(3995m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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
            // ($100k total value * 0.5 target * 0.9975 buffer - $50k current holdings) / 25 =~ 3995m - 1 cause order fee
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 0.5m);
            Assert.AreEqual(3994m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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
            // (($100k total value - 10 k fees) * 0.5 target * 0.9975 buffer - $50k current holdings) / 25 =~ 3995m
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 0.5m);
            Assert.AreEqual(3795m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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
            // ($100k total value * -0.5 target * 0.9975 buffer - $50k current holdings) / 25 =~ 3995m
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -0.5m);
            Assert.AreEqual(-3995m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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
            // ($100k total value * -0.5 target * 0.9975 buffer - $50k current holdings) / 25 =~ 3995m - 1 due to fees
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -0.5m);
            Assert.AreEqual(-3994m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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
            // ($100k total value * -0.5 target * 0.9975 buffer - $50k current holdings) / 25 =~ 3995m - 200 due to fees
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -0.5m);
            Assert.AreEqual(-3795m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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

            //Need to sell to make position ($100k total value * -1 target * 0.9975 buffer - $50k current holdings) / 50 =~ -5990m
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -1m);
            Assert.AreEqual(-5990m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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

            //Need to sell to make position ($100k total value * -1 target * 0.9975 buffer - $50k current holdings) / 50 =~ -5990m - 1 due to fee
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -1m);
            Assert.AreEqual(-5989m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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

            // Fee is 10k / 25 ~= 400 shares
            //Need to sell to make position (($100k total value -10k fees) * -1 target * 0.9975 buffer - $50k current holdings) / 50 =~ -5591m
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -1m);
            Assert.AreEqual(-5591m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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
            // ($100k total value * -2 target * 0.9975 buffer - $50k current holdings) / 25 =~ 9980m
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -2m);
            Assert.AreEqual(-9980m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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
            // ($100k total value * -2 target * 0.9975 buffer - $50k current holdings) / 25 =~ 9980m - 1 due to fees
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -2m);
            Assert.AreEqual(-9979m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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
            // ($100k total value * -2 target * 0.9975 buffer - $50k current holdings) / 25 =~ 9980m - ~800 due to fees
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -2m);
            Assert.AreEqual(-9182m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
        }

        [Test, TestCaseSource("TestParametersDifferentMargins")]
        public void SetHoldings_ZeroToFullShort_SmallConstantFeeStructure_DifferentMargins(decimal initialMarginRequirement, decimal maintenanceMarginRequirement)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, initialMarginRequirement, maintenanceMarginRequirement, 10000);
            //Set price to $25
            Update(msft, 25);

            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -2m);
            // ($100k total value * -2 target * 0.9975 buffer - $10k fees * 2) / 25 =~-7182m
            Assert.AreEqual(-7182m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
        }

        [Test, TestCaseSource("TestParametersDifferentMargins")]
        public void SetHoldings_ZeroToAlmostFullShort_SmallConstantFeeStructure_DifferentMargins(decimal initialMarginRequirement, decimal maintenanceMarginRequirement)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, initialMarginRequirement, maintenanceMarginRequirement, 10000);
            //Set price to $25
            Update(msft, 25);

            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -1.5m);
            // ($100k total value * -1.5 target * 0.9975 buffer - $10k fees * 1.5) / 25 =~ -5386m
            Assert.AreEqual(-5386m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
        }

        [Test, TestCaseSource("TestParametersDifferentMargins")]
        public void SetHoldings_ZeroToFullLong_SmallConstantFeeStructure_DifferentMargins(decimal initialMarginRequirement, decimal maintenanceMarginRequirement)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, initialMarginRequirement, maintenanceMarginRequirement, 10000);
            //Set price to $25
            Update(msft, 25);

            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 2m);
            // ($100k total value * 2 target * 0.9975 buffer - $10k fees * 2) / 25 =~ 7182m
            Assert.AreEqual(7182m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
        }

        [Test, TestCaseSource("TestParametersDifferentMargins")]
        public void SetHoldings_ZeroToAlmostFullLong_SmallConstantFeeStructure_DifferentMargins(decimal initialMarginRequirement, decimal maintenanceMarginRequirement)
        {
            Security msft;
            var algo = GetAlgorithm(out msft, initialMarginRequirement, maintenanceMarginRequirement, 10000);
            //Set price to $25
            Update(msft, 25);

            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 1.5m);
            // ($100k total value * 1.5 target * 0.9975 buffer - $10k fees * 1.5) / 25 =~ 5386m
            Assert.AreEqual(5386m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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

            algo.Settings.FreePortfolioValue =
                algo.Portfolio.TotalPortfolioValue * algo.Settings.FreePortfolioValuePercentage;

            //Now: 2000 * 50 = $100k Holdings, $50k Cash: $150k.
            //Calculate the new holdings for 50% MSFT::
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 0.5m);

            // Need to sell ($150k total value * 0.5m  target * 0.9975 buffer - 100k current holdings) / 50 =~ -500
            Assert.AreEqual(-503m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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

            algo.Settings.FreePortfolioValue =
                algo.Portfolio.TotalPortfolioValue * algo.Settings.FreePortfolioValuePercentage;

            //Now: 2000 * 50 = $100k Holdings, $50k Cash: $150k.
            //Calculate the new holdings for 50% MSFT::
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 0.5m);

            // Need to sell ($150k total value * 0.5m  target * 0.9975 buffer - 100k current holdings) / 50 =~ -500
            Assert.AreEqual(-503m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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

            algo.Settings.FreePortfolioValue =
                algo.Portfolio.TotalPortfolioValue * algo.Settings.FreePortfolioValuePercentage;

            //Now: 2000 * 50 = $100k Holdings, $50k Cash: $150k.
            //Calculate the new holdings for 50% MSFT::
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 0.5m);

            // Need to sell (( $150k total value - 10 k fees) * 0.5m  target * 0.9975 buffer - 100k current holdings) / 50 =~ -603
            Assert.AreEqual(-603, actual);
            // After the trade: TPV 140k (due to fees), holdings at 1397 shares (2000 - 603) * $50 = 69850 value, which is 0.4989% holdings
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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

            algo.Settings.FreePortfolioValue =
                algo.Portfolio.TotalPortfolioValue * algo.Settings.FreePortfolioValuePercentage;

            //Now: 2000 * 50 = $100k Holdings, $50k Cash: $150k. MSFT is already 66% of holdings.
            //Calculate the order for 75% MSFT:
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 0.75m);

            //Need to buy to make position ($150k total value * 0.75  target * 0.9975 buffer - 100k current holdings) / 50 =~ 244
            Assert.AreEqual(244m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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

            algo.Settings.FreePortfolioValue =
                algo.Portfolio.TotalPortfolioValue * algo.Settings.FreePortfolioValuePercentage;

            //Now: 2000 * 50 = $100k Holdings, $50k Cash: $150k. MSFT is already 66% of holdings.
            //Calculate the order for 75% MSFT:
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 0.75m);

            //Need to buy to make position (150K total value * 0.75  target * 0.9975 buffer - 100k current holdings) / 50 =~ 244
            Assert.AreEqual(244m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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

            algo.Settings.FreePortfolioValue =
                algo.Portfolio.TotalPortfolioValue * algo.Settings.FreePortfolioValuePercentage;

            //Now: 2000 * 50 = $100k Holdings, $50k Cash: $150k. MSFT is already 66% of holdings.
            //Calculate the order for 75% MSFT:
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 0.75m);

            //Need to buy to make position ((150K total value - 10k fees) * 0.75  target * 0.9975 buffer - 100k current holdings) / 50 =~ 94
            Assert.AreEqual(94m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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

            algo.Settings.FreePortfolioValue =
                algo.Portfolio.TotalPortfolioValue * algo.Settings.FreePortfolioValuePercentage;

            //Now: 3000 * 50 = $150k Holdings, $25k Cash: $175k. MSFT is 86% of holdings.
            //Calculate the order for 50% MSFT:
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 0.5m);

            //Need to sell to make position ($175k total value * 0.5 target * 0.9975 buffer - $150k current holdings) / 50 =~ -1254m
            Assert.AreEqual(-1254m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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

            algo.Settings.FreePortfolioValue =
                algo.Portfolio.TotalPortfolioValue * algo.Settings.FreePortfolioValuePercentage;

            //Now: 2000 * 50 = $100k Holdings, $50k Cash: $150k. MSFT is 66% of holdings.
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -0.5m);

            //Need to sell to make position ($150k total value * -0.5 target * 0.9975 buffer - $100k current holdings) / 50 =~ -3496m
            Assert.AreEqual(-3496m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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

            algo.Settings.FreePortfolioValue =
                algo.Portfolio.TotalPortfolioValue * algo.Settings.FreePortfolioValuePercentage;

            // Cash: 150k
            // MSFT: -(2000*50) = -100K
            // TPV: 50k
            Assert.AreEqual(50000, algo.Portfolio.TotalPortfolioValue);

            // we should end with -750 shares (-.75*50000/50)
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -0.75m);

            // currently -2000, so plus 1251
            Assert.AreEqual(1251m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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

            algo.Settings.FreePortfolioValue =
                algo.Portfolio.TotalPortfolioValue * algo.Settings.FreePortfolioValuePercentage;

            //Now: 2000 * 50 = $0k Net Holdings, $50k Cash: $50k. MSFT is 0% of holdings.
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 0.5m);

            //We want to be 50% long, this is currently +2000 holdings + 50% 50k = $25k * 0.9975 buffer/ $50-share~=2498m
            Assert.AreEqual(2498m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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

            algo.Settings.FreePortfolioValue =
                algo.Portfolio.TotalPortfolioValue * algo.Settings.FreePortfolioValuePercentage;

            // Cash: 150k
            // MSFT: -50*2000=100k
            // TPV: 50k
            Assert.AreEqual(50000, algo.Portfolio.TotalPortfolioValue);

            // 50k*0.5=25k = 500 end holdings
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, 0.5m);

            // ($50k total value * 0.5 target * 0.9975 buffer - (-$100k current holdings)) / 50 =~ 2498m
            Assert.AreEqual(2498m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
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

            algo.Settings.FreePortfolioValue =
                algo.Portfolio.TotalPortfolioValue * algo.Settings.FreePortfolioValuePercentage;

            // Cash: 150k
            // MSFT: -25k
            // TPV : 125k
            // ($125k total value * -0.5 target * 0.9975 buffer - (-$25k current holdings)) / 12.5 =~ -2987m
            var actual = algo.CalculateOrderQuantity(Symbols.MSFT, -0.5m);
            Assert.AreEqual(-2987m, actual);
            Assert.IsTrue(HasSufficientBuyingPowerForOrder(actual, msft, algo));
        }

        /*************************************************************************/
        //  Rounding the order quantity to the nearest multiple of lot size test
        /*************************************************************************/

        [Test]
        public void SetHoldings_Long_RoundOff()
        {
            var algo = new QCAlgorithm();
            algo.SubscriptionManager.SetDataManager(new DataManagerStub(algo));
            algo.AddSecurity(SecurityType.Forex, "EURUSD");
            algo.SetCash(100000);
            algo.SetCash("BTC", 0, 8000);
            algo.SetBrokerageModel(BrokerageName.FxcmBrokerage);
            algo.Securities[Symbols.EURUSD].FeeModel = new ConstantFeeModel(0);
            Security eurusd = algo.Securities[Symbols.EURUSD];
            // Set Price to $26
            Update(eurusd, 26);
            // So 100000/26 = 3846, After Rounding off becomes 3000
            var actual = algo.CalculateOrderQuantity(Symbols.EURUSD, 1m);
            Assert.AreEqual(3000m, actual);

            var btcusd = algo.AddCrypto("BTCUSD", market: Market.GDAX);
            btcusd.FeeModel = new ConstantFeeModel(0);
            // Set Price to $26
            Update(btcusd, 26);
            // (100000 * 0.9975) / 26 = 3836.53846153m
            actual = algo.CalculateOrderQuantity(Symbols.BTCUSD, 1m);
            Assert.AreEqual(3836.538m, actual);
        }

        [Test]
        public void SetHoldings_Short_RoundOff()
        {
            var algo = new QCAlgorithm();
            algo.SubscriptionManager.SetDataManager(new DataManagerStub(algo));
            algo.AddSecurity(SecurityType.Forex, "EURUSD");
            algo.SetCash(100000);
            algo.SetBrokerageModel(BrokerageName.FxcmBrokerage);
            algo.Securities[Symbols.EURUSD].FeeModel = new ConstantFeeModel(0);
            Security eurusd = algo.Securities[Symbols.EURUSD];
            // Set Price to $26
            Update(eurusd, 26);
            // So -100000/26 = -3846, After Rounding off becomes -3000
            var actual = algo.CalculateOrderQuantity(Symbols.EURUSD, -1m);
            Assert.AreEqual(-3000m, actual);

            var btcusd = algo.AddCrypto("BTCUSD", market: Market.GDAX);
            btcusd.FeeModel = new ConstantFeeModel(0);
            // Set Price to $26
            Update(btcusd, 26);
            // Cash model does not allow shorts
            actual = algo.CalculateOrderQuantity(Symbols.BTCUSD, -1m);
            Assert.AreEqual(0, actual);
        }

        [Test]
        public void SetHoldings_Long_ToZero_RoundOff()
        {
            var algo = new QCAlgorithm();
            algo.SubscriptionManager.SetDataManager(new DataManagerStub(algo));
            algo.AddSecurity(SecurityType.Forex, "EURUSD");
            algo.SetCash(10000);
            algo.SetBrokerageModel(BrokerageName.FxcmBrokerage);
            algo.Securities[Symbols.EURUSD].FeeModel = new ConstantFeeModel(0);
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

            var mock = new Mock<ITransactionHandler>();
            var request = new Mock<SubmitOrderRequest>(null, null, null, null, null, null, null, null, null);
            mock.Setup(m => m.Process(It.IsAny<OrderRequest>())).Returns(new OrderTicket(null, request.Object));
            mock.Setup(m => m.GetOpenOrders(It.IsAny<Func<Order, bool>>())).Returns(new List<Order>());
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
            algo.SubscriptionManager.SetDataManager(new DataManagerStub(algo));
            algo.AddSecurity(SecurityType.Equity, "MSFT");
            algo.SetCash(100000);
            algo.SetFinishedWarmingUp();
            algo.Securities[Symbols.MSFT].FeeModel = new ConstantFeeModel(fee);
            _fakeOrderProcessor = new FakeOrderProcessor();
            algo.Transactions.SetOrderProcessor(_fakeOrderProcessor);
            msft = algo.Securities[Symbols.MSFT];
            msft.SetLeverage(leverage);
            return algo;
        }

        private QCAlgorithm GetAlgorithm(out Security msft, decimal initialMarginRequirement, decimal maintenanceMarginRequirement, decimal fee)
        {
            //Initialize algorithm
            var algo = new QCAlgorithm();
            algo.SubscriptionManager.SetDataManager(new DataManagerStub(algo));
            algo.AddSecurity(SecurityType.Equity, "MSFT");
            algo.SetCash(100000);
            algo.SetFinishedWarmingUp();
            algo.Securities[Symbols.MSFT].FeeModel = new ConstantFeeModel(fee);
            _fakeOrderProcessor = new FakeOrderProcessor();
            algo.Transactions.SetOrderProcessor(_fakeOrderProcessor);
            msft = algo.Securities[Symbols.MSFT];
            msft.BuyingPowerModel = new SecurityMarginModel(initialMarginRequirement, maintenanceMarginRequirement, 0);
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

        private bool HasSufficientBuyingPowerForOrder(decimal orderQuantity, Security security, IAlgorithm algo)
        {
            var order = new MarketOrder(security.Symbol, orderQuantity, DateTime.UtcNow);
            _fakeOrderProcessor.AddTicket(order.ToOrderTicket(algo.Transactions));
            var hashSufficientBuyingPower = security.BuyingPowerModel.HasSufficientBuyingPowerForOrder(algo.Portfolio,
                security, new MarketOrder(security.Symbol, orderQuantity, DateTime.UtcNow));
            return hashSufficientBuyingPower.IsSufficient;
        }
    }
}