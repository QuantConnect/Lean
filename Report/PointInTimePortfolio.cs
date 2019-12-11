
using QuantConnect.Orders;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Report
{
    public class PointInTimePortfolio
    {
        public DateTime Time { get; private set; }
        public decimal TotalPortfolioValue { get; private set; }
        public Order Order { get; private set; }
        public List<PointInTimeHolding> Holdings { get; private set; }

        public PointInTimePortfolio(Order order, SecurityPortfolioManager portfolio)
        {
            Time = order.Time;
            Order = order;
            TotalPortfolioValue = portfolio.TotalPortfolioValue;
            Holdings = portfolio.Securities.Values.Select(x => new PointInTimeHolding(x.Symbol, x.Holdings.HoldingsValue, x.Holdings.Quantity)).ToList();
        }

        public class PointInTimeHolding
        {
            public Symbol Symbol { get; private set; }
            public decimal HoldingsValue { get; private set; }
            public decimal Quantity { get; private set; }
            public decimal AbsoluteHoldingsValue => Math.Abs(HoldingsValue);
            public decimal AbsoluteHoldingsQuantity => Math.Abs(Quantity);

            public PointInTimeHolding(Symbol symbol, decimal holdingsValue, decimal holdingsQuantity)
            {
                Symbol = symbol;
                HoldingsValue = holdingsValue;
                Quantity = holdingsQuantity;
            }
        }
    }
}