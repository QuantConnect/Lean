using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Fills;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Algorithm.CSharp
{

    /// <summary>
    /// This regression algorithm tests the order processing of the backtesting brokerage.
    /// We open an equity position that should fill in two parts, on two different bars.
    /// We open a long option position and let it expire so we can exercise the position.
    /// To check the orders we use OnOrderEvent and throw exceptions if verification fails.
    /// </summary>
    /// <meta name="tag" content="backtesting brokerage" />
    /// <meta name="tag" content="regression test" />
    /// <meta name="tag" content="options" />
    class BacktestingBrokerageRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Security _security;
        public Symbol Spy;
        private Option _option;
        public Symbol OptionSymbol;
        private bool _optionBought = false;
        private bool _equityBought = false;

        private decimal _optionStrikePrice;

        /// <summary>
        /// Initialize the algorithm
        /// </summary>
        public override void Initialize()
        {
            SetCash(1000000);
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 28);

            _security = AddEquity("SPY", Resolution.Hour);
            Spy = _security.Symbol;

            _security.SetFillModel(new PartialMarketFillModel(2));


            _option = AddOption("GOOG");
            OptionSymbol = _option.Symbol;

            // Set our strike/expiry filter for this option chain
            _option.SetFilter(u => u.IncludeWeeklys()
                                   .Strikes(-2, +2)
                                   .Expiration(TimeSpan.Zero, TimeSpan.FromDays(10)));
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {   
            if (!_equityBought && data.ContainsKey(Spy)) {
                //Buy our Equity
                var quantity = CalculateOrderQuantity(Spy, .1m);
                MarketOrder(Spy, quantity, asynchronous: true);
                _equityBought = true;
            }

            if (!_optionBought)
            {
                // Buy our option
                OptionChain chain;
                if (data.OptionChains.TryGetValue(OptionSymbol, out chain))
                {
                    // Find the second call strike under market price expiring today
                    var contracts = (
                        from optionContract in chain.OrderByDescending(x => x.Strike)
                        where optionContract.Right == OptionRight.Call
                        where optionContract.Expiry == Time.Date
                        where optionContract.Strike < chain.Underlying.Price
                        select optionContract
                        ).Take(2);

                    if (contracts.Any())
                    {
                        var optionToBuy = contracts.FirstOrDefault();
                        _optionStrikePrice = optionToBuy.Strike;
                        MarketOrder(optionToBuy.Symbol, 2);
                        _optionBought = true;
                    }
                }
            }
        }

        /// <summary>
        /// All order events get pushed through this function
        /// </summary>
        /// <param name="orderEvent">OrderEvent object that contains all the information about the event</param>
        public override void OnOrderEvent(OrderEvent orderEvent)
        {   
            // Get the order from our transactions
            var order = Transactions.GetOrderById(orderEvent.OrderId);

            // Based on the type verify the order
            switch(order.Type)
            {
                case OrderType.Market:
                    VerifyMarketOrder(order);
                    break;

                case OrderType.OptionExercise:
                    VerifyOptionExercise(order);
                    break;
            }
        }

        /// <summary>
        /// To verify Market orders is process correctly
        /// </summary>
        /// <param name="order">Order object to analyze</param>
        public void VerifyMarketOrder(Order order)
        {
            switch(order.Status)
            {
                case OrderStatus.Submitted:
                    break;

                // All PartiallyFilled orders should have a LastFillTime
                case OrderStatus.PartiallyFilled:
                    if (order.LastFillTime == null) 
                    {
                        throw new Exception("LastFillTime should not be null");
                    }
                    break;

                // All filled equity orders should have filled after creation because of our fill model!
                case OrderStatus.Filled:
                    if (order.SecurityType == SecurityType.Equity && order.CreatedTime == order.LastFillTime)
                    {
                        throw new Exception("Order should not finish during the CreatedTime bar");
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// To verify OptionExercise orders is process correctly
        /// </summary>
        /// <param name="order">Order object to analyze</param>
        public void VerifyOptionExercise(Order order)
        {
            // If the option price isn't the same as the strike price, its incorrect
            if (order.Price != _optionStrikePrice)
            {
                throw new Exception("OptionExercise order price should be strike price!!");
            }
        }

        /// <summary>
        /// PartialMarketFillModel that allows the user to set the number of fills and restricts
        /// the fill to only one per bar.
        /// </summary>
        public class PartialMarketFillModel : ImmediateFillModel
        {
            private readonly decimal _percent;
            private readonly Dictionary<long, decimal> _absoluteRemainingByOrderId = new Dictionary<long, decimal>();

            /// <param name="numberOfFills"></param>
            public PartialMarketFillModel(int numberOfFills = 1)
            {
                _percent = 1m / numberOfFills;
            }

            /// <summary>
            /// Performs partial market fills once per time step
            /// </summary>
            /// <param name="asset">The security being ordered</param>
            /// <param name="order">The order</param>
            /// <returns>The order fill</returns>
            public override OrderEvent MarketFill(Security asset, MarketOrder order)
            {
                var currentUtcTime = asset.LocalTime.ConvertToUtc(asset.Exchange.TimeZone);

                // Only fill once a time slice
                if (order.LastFillTime != null && currentUtcTime <= order.LastFillTime)
                {
                    return new OrderEvent(order, currentUtcTime, OrderFee.Zero);
                }

                decimal absoluteRemaining;
                if (!_absoluteRemainingByOrderId.TryGetValue(order.Id, out absoluteRemaining))
                {
                    absoluteRemaining = order.AbsoluteQuantity;
                    _absoluteRemainingByOrderId.Add(order.Id, order.AbsoluteQuantity);
                }

                var fill = base.MarketFill(asset, order);
                var absoluteFillQuantity = (int)(Math.Min(absoluteRemaining, (int)(_percent * order.Quantity)));
                fill.FillQuantity = Math.Sign(order.Quantity) * absoluteFillQuantity;

                if (absoluteRemaining == absoluteFillQuantity)
                {
                    fill.Status = OrderStatus.Filled;
                    _absoluteRemainingByOrderId.Remove(order.Id);
                }
                else
                {
                    absoluteRemaining = absoluteRemaining - absoluteFillQuantity;
                    _absoluteRemainingByOrderId[order.Id] = absoluteRemaining;
                    fill.Status = OrderStatus.PartiallyFilled;
                }

                return fill;
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "3"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.08%"},
            {"Compounding Annual Return", "-6.043%"},
            {"Drawdown", "0.100%"},
            {"Expectancy", "-1"},
            {"Net Profit", "-0.080%"},
            {"Sharpe Ratio", "-7.915"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.005"},
            {"Beta", "0.116"},
            {"Annual Standard Deviation", "0.002"},
            {"Annual Variance", "0"},
            {"Information Ratio", "10.295"},
            {"Tracking Error", "0.018"},
            {"Treynor Ratio", "-0.164"},
            {"Total Fees", "$3.54"},
            {"Fitness Score", "0.062"},
            {"OrderListHash", "1399223394"}
        };
    }
}
