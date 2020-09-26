using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities.Equity;
using QuantConnect.Securities.Option;

namespace QuantConnect.Algorithm.CSharp
{
    public class OptionAutoExerciseEarlyMarketCloseRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private bool purchasedEquity;
        private bool purchasedOption;

        private Equity GOOG;
        private Option GOOGOptionChain;

        private Option GOOGPutContract;
        private Option GOOGCallContract;

        public override void Initialize()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 28);
            SetCash(100000);
            GOOG = AddEquity("GOOG", Resolution.Minute);
            GOOGOptionChain = AddOption("GOOG");
            GOOGOptionChain.SetFilter(u => u.IncludeWeeklys()
                .Strikes(-20, +20)
                .Expiration(TimeSpan.Zero, TimeSpan.FromDays(1)));
        }

        public override void OnData(Slice data)
        {
            if (!purchasedEquity)
            {
                purchasedEquity = true;
                MarketOrder(GOOG.Symbol, 100);
            }

            if (GOOGCallContract == null)
            {
                // find a DITM call and a DITM put
                OptionChain chain;
                if (!data.OptionChains.TryGetValue(GOOGOptionChain.Symbol, out chain))
                {
                    return;
                }

                var deepInTheMoney = chain.Contracts.Values
                    .OrderByDescending(c => GetIntrinsicValue(c.Symbol))
                    .Take(10).ToList();

                GOOGPutContract = (Option) Securities[deepInTheMoney.First(c => c.Right == OptionRight.Put).Symbol];
                MarketOrder(GOOGPutContract.Symbol, 1);

                GOOGCallContract = (Option) Securities[deepInTheMoney.First(c => c.Right == OptionRight.Call).Symbol];
                MarketOrder(GOOGCallContract.Symbol, 1);
            }
        }

        public override void OnOrderEvent(OrderEvent fill)
        {
            var order = Transactions.GetOrderById(fill.OrderId);
            Debug($"{Time:O}:: ORDER: {order}");
            Debug($"{Time:O}::  FILL: {order}");
        }

        private decimal GetIntrinsicValue(Symbol contract)
        {
            var right = contract.ID.OptionRight;
            var strike = contract.ID.StrikePrice;
            var underlyingPrice = Securities[contract.Underlying].Price;
            return right == OptionRight.Call
                ? underlyingPrice - strike
                : strike - underlyingPrice;
        }

        public bool CanRunLocally { get; } = true;
        public Language[] Languages { get; } = {Language.CSharp};
        public Dictionary<string, string> ExpectedStatistics { get; }
    }
}