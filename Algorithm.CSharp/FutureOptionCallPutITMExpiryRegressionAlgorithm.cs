using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;

namespace QuantConnect.Algorithm.CSharp
{
    public class FutureOptionCallPutITMExpiryRegressionAlgorithm : QCAlgorithm
    {
        private Symbol _es18z20;
        private Symbol _es19h21;
        private List<Symbol> _esChains;
        private List<Symbol> _expectedContracts;

        public override void Initialize()
        {
            SetStartDate(2020, 9, 22);
            typeof(QCAlgorithm)
                .GetField("_endDate", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(this, new DateTime(2021, 3, 22));

            _es18z20 = AddFutureContract(
                QuantConnect.Symbol.CreateFuture(
                    Futures.Indices.SP500EMini,
                    Market.CME,
                    new DateTime(2020, 12, 18)),
                Resolution.Minute).Symbol;

            _es19h21 = AddFutureContract(
                QuantConnect.Symbol.CreateFuture(
                    Futures.Indices.SP500EMini,
                    Market.CME,
                    new DateTime(2021, 3, 19)),
                Resolution.Minute).Symbol;

            _esChains = OptionChainProvider.GetOptionContractList(_es18z20, Time)
                .Where(x => x.ID.StrikePrice <= 3250m)
                .OrderByDescending(x => x.ID.StrikePrice)
                .Take(1)
                .Concat(OptionChainProvider.GetOptionContractList(_es19h21, Time)
                .Where(x => x.ID.StrikePrice <= 3250m)
                .OrderByDescending(x => x.ID.StrikePrice)
                .Take(1))
                .Select(x => AddFutureOptionContract(x, Resolution.Minute).Symbol)
                .ToList();

            _expectedContracts = new List<Symbol>
            {
                QuantConnect.Symbol.CreateOption(_es18z20, Market.CME, OptionStyle.American, OptionRight.Call, 3250m, new DateTime(2020, 12, 18)),
                QuantConnect.Symbol.CreateOption(_es19h21, Market.CME, OptionStyle.American, OptionRight.Call, 3250m, new DateTime(2021, 3, 19)),
            };

            foreach (var contract in _expectedContracts)
            {
                if (!_esChains.Contains(contract))
                {
                    throw new Exception($"Contract {contract} was not found in the chain");
                }
            }

            Schedule.On(DateRules.Today, TimeRules.AfterMarketOpen(_es19h21, 1), () =>
            {
                foreach (var contract in _esChains)
                {
                    MarketOrder(contract, 1);
                }
            });
        }

        public override void OnData(Slice data)
        {
            foreach (var delisting in data.Delistings.Values)
            {
                if (delisting.Type == DelistingType.Warning)
                {
                    if (delisting.Time != new DateTime(2020, 12, 18) && delisting.Time != new DateTime(2021, 3, 19))
                    {
                        throw new Exception($"Delisting warning issued at unexpected date: {delisting.Time}");
                    }
                }
                if (delisting.Type == DelistingType.Delisted)
                {
                    if (delisting.Time != new DateTime(2020, 12, 19) && delisting.Time != new DateTime(2021, 3, 20))
                    {
                        throw new Exception($"Delisting warning issued at unexpected date: {delisting.Time}");
                    }
                }
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            //Log($"{orderEvent.UtcTime} -- {orderEvent.OrderId} :: {orderEvent.Symbol} - Price: {orderEvent.FillPrice} - Qty: {orderEvent.Quantity} - Msg: {orderEvent.Message}");
            if (orderEvent.Status != OrderStatus.Filled)
            {
                return;
            }

            if (!Securities.ContainsKey(orderEvent.Symbol))
            {
                throw new Exception($"Order event Symbol not found in Securities collection: {orderEvent.Symbol}");
            }

            var security = Securities[orderEvent.Symbol];
            if (security.Symbol == _es18z20)
            {
                AssertFutureOptionOrderExercise(orderEvent, security, Securities[_expectedContracts[0]]);
            }
            else if (security.Symbol == _es19h21)
            {
                AssertFutureOptionOrderExercise(orderEvent, security, Securities[_expectedContracts[0]]);
            }
            // Expected contract is ES18Z20 Call Option
            else if (security.Symbol == _expectedContracts[0] || security.Symbol == _expectedContracts[1])
            {
                AssertFutureOptionContractOrder(orderEvent, security);
            }
            else
            {
                throw new Exception($"Received order event for unknown Symbol: {orderEvent.Symbol}");
            }

            Log($"{Time:yyyy-MM-dd HH:mm:ss} -- {orderEvent.Symbol} :: Price: {Securities[orderEvent.Symbol].Holdings.Price} Qty: {Securities[orderEvent.Symbol].Holdings.Quantity} Direction: {orderEvent.Direction} Msg: {orderEvent.Message}");
        }

        private void AssertFutureOptionOrderExercise(OrderEvent orderEvent, Security future, Security optionContract)
        {
            var expectedLiquidationTimeUtc = future.Symbol == _expectedContracts[0]
                ? new DateTime(2020, 12, 18, 5, 0, 0)
                : new DateTime(2021, 3, 19, 5, 0, 0);

            if (orderEvent.Direction == OrderDirection.Sell && future.Holdings.Quantity != 0)
            {
                // We expect the contract to have been liquidated immediately
                throw new Exception($"Did not liquidate existing holdings for Symbol {future.Symbol}");
            }
            if (orderEvent.Direction == OrderDirection.Sell && orderEvent.UtcTime != expectedLiquidationTimeUtc)
            {
                throw new Exception($"Liquidated contract, but not at the expected time. Expected: {expectedLiquidationTimeUtc:yyyy-MM-dd HH:mm:ss} - found {orderEvent.UtcTime:yyyy-MM-dd HH:mm:ss}");
            }

            if (orderEvent.Message.Contains("Option Exercise"))
            {
                if (future.Holdings.Quantity != 1)
                {
                    // Here, we expect to have some holdings in the underlying, but not in the future option anymore.
                    throw new Exception($"Exercised option contract, but we have no holdings for Future {future.Symbol}");
                }

                if (optionContract.Holdings.Quantity != 0)
                {
                    throw new Exception($"Exercised option contract, but we have holdings for Option contract {optionContract.Symbol}");
                }
            }
        }

        private void AssertFutureOptionContractOrder(OrderEvent orderEvent, Security option)
        {
            if (orderEvent.Direction == OrderDirection.Buy && option.Holdings.Quantity != 1)
            {
                throw new Exception($"No holdings were created for option contract {option.Symbol}");
            }
            if (orderEvent.Direction == OrderDirection.Sell && option.Holdings.Quantity != 0)
            {
                throw new Exception($"Holdings were found after a filled option exercise");
            }
            if (orderEvent.Message.Contains("Exercise") && option.Holdings.Quantity != 0)
            {
                throw new Exception($"Holdings were found after exercising option contract {option.Symbol}");
            }
        }
    }
}

