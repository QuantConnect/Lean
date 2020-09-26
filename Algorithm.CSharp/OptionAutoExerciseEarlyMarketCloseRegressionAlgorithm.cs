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
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This is a regression algorithm ensuring that auto-exercised option orders are filled even when the market closes early.
    /// </summary>
    /// <meta name="tag" content="regression test" />
    /// <meta name="tag" content="options" />
    public class OptionAutoExerciseEarlyMarketCloseRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
    	private int optionQuantity = 1;
    	private int equityQuantity = 100;
    	private OptionRight? right = OptionRight.Put;

    	private bool logAvailableContracts = false;
    	private DateTime lastTime;
    	private bool purchasedOption;
    	private bool purchasedUnderlying;
        private DateTime? forceOptionExercise = null;// new DateTime(2020, 09, 04, 15, 59, 0);
        private bool loggedContracts;
        private Symbol symbol;
        private Symbol optionContract;
        private readonly HashSet<Symbol> contractsAdded = new HashSet<Symbol>();

        public override void Initialize()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 28);
            SetCash(1000000);
            var equity = AddEquity("GOOG", Resolution.Minute);
            equity.SetDataNormalizationMode(DataNormalizationMode.Raw);
            symbol = equity.Symbol;

            //If we explicitly add the option then this test passes
            //If we instead add the option via AddOptionContract, it fails
            //AddOption("GOOG");

            Debug($"SETTINGS: EquityQuantity: {equityQuantity} | Right: {right} OptionQuantity: {optionQuantity} ForceExercise: {forceOptionExercise}");
        }

        public override void OnData(Slice data)
        {
        	lastTime = Time;
            if (!purchasedUnderlying)
            {
                Debug($"Submitting market order for {equityQuantity} {GetSymbolString(symbol)}");
                MarketOrder(symbol, equityQuantity);
                purchasedUnderlying = true;
            }

            if (contractsAdded.Count == 0)
            {
            	AddOptionContracts(data);
            }
            else if (!purchasedOption && Securities[optionContract].Price != 0)
            {
            	purchasedOption = true;
                Debug($"Submitting market order for {optionQuantity} {GetSymbolString(optionContract)}");
                MarketOrder(optionContract, optionQuantity);
            }

            if (forceOptionExercise == Time && Portfolio[optionContract].Invested)
            {
            	Debug($"Exercising option: {optionQuantity} of {GetSymbolString(optionContract)}");
            	ExerciseOption(optionContract, optionQuantity);
            }
        }

        public override void OnOrderEvent(OrderEvent fill)
        {
            var order = Transactions.GetOrderById(fill.OrderId);
            Debug($"CASH: {Portfolio.Cash} | GOOG: {Portfolio["GOOG"].Quantity} | {GetSymbolString(fill.Symbol)} >> ORDER:: {order} >> FILL:: {fill}");

            if (order.Type == OrderType.OptionExercise && fill.Status == OrderStatus.Canceled)
            {
                throw new Exception("OptionExerciseOrder was canceled!");
            }
        }

        public override void OnEndOfDay()
        {
        	Debug($"--------LAST TIME: {lastTime:O} CASH: {Portfolio.Cash} GOOG: {Portfolio["GOOG"].Quantity}--------");
        }

        private string GetSymbolString(Symbol symbol)
        {
            switch (symbol.SecurityType)
            {
                case SecurityType.Equity:
                    return $"Equity: {symbol.Value}";

                case SecurityType.Option:
                    return $"Option: {symbol.Underlying.Value}: {symbol.ID.OptionRight}@{symbol.ID.StrikePrice:0.00}({GetIntrinsicValue(symbol):0.00}";

                default:
                    return symbol.ToString();
            }
        }

        private void AddOptionContracts(Slice data)
        {
            var contracts = OptionChainProvider.GetOptionContractList(symbol, data.Time)
                .Where(c => right == null || c.ID.OptionRight == right)
                .GroupBy(c => c.ID.Date)
                .OrderBy(grp => grp.Key)
                .Take(1)
                .SelectMany(grp => grp)
                .ToList();

            if (contracts.Count == 0)
            {
                return;
            }

            if (logAvailableContracts && !loggedContracts)
            {
                foreach (var c in contracts)
                {
                    Debug($"{c}: {c.ID.Date:yy-MM-dd}: {c.ID.StrikePrice:0.00}");
                }

                loggedContracts = true;
            }

            // find the most ITM contract
            var contract = contracts.OrderByDescending(GetIntrinsicValue).First();

            optionContract = contract;
            if (contractsAdded.Add(optionContract))
            {
                // use AddOptionContract() to subscribe the data for specified contract
                Debug($"Adding Option Contract: {GetSymbolString(optionContract)}:: Intrinsic Value: {GetIntrinsicValue(optionContract)}");
                AddOptionContract(optionContract, Resolution.Minute);
            }
        }

        private decimal GetIntrinsicValue(Symbol contract)
        {
        	var right = contract.ID.OptionRight;
        	var strike = contract.ID.StrikePrice;
        	var underlying = Securities[contract.Underlying].Price;
        	return right == OptionRight.Call
                    ? underlying - strike
                    : strike - underlying;
        }

        public bool CanRunLocally { get; } = true;
        public Language[] Languages { get; } = {Language.CSharp};
        public Dictionary<string, string> ExpectedStatistics { get; } //TBD - requires fixing OptionExerciseOrder cancellation
    }
}