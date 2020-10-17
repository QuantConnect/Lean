using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Fills;
using QuantConnect.Orders.Slippage;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Brokerages
{
    public class SamcoBrokerageModel : DefaultBrokerageModel
    {
        public SamcoBrokerageModel(AccountType accountType = AccountType.Margin) : base(accountType)
        {
        }

        public override AccountType AccountType => base.AccountType;

        public override decimal RequiredFreeBuyingPowerPercent => base.RequiredFreeBuyingPowerPercent;

        public override void ApplySplit(List<OrderTicket> tickets, Split split)
        {
            base.ApplySplit(tickets, split);
        }

        public override bool CanExecuteOrder(Security security, Order order)
        {
            return base.CanExecuteOrder(security, order);
        }

        public override bool CanSubmitOrder(Security security, Order order, out BrokerageMessageEvent message)
        {
            return base.CanSubmitOrder(security, order, out message);
        }

        public override bool CanUpdateOrder(Security security, Order order, UpdateOrderRequest request, out BrokerageMessageEvent message)
        {
            return base.CanUpdateOrder(security, order, request, out message);
        }

        public override IFillModel GetFillModel(Security security)
        {
            return base.GetFillModel(security);
        }

        public override ISettlementModel GetSettlementModel(Security security)
        {
            return base.GetSettlementModel(security);
        }

        public override ISlippageModel GetSlippageModel(Security security)
        {
            return base.GetSlippageModel(security);
        }

        private const decimal _maxLeverage = 7m;

        /// <summary>
        /// Gets a map of the default markets to be used for each security type
        /// </summary>
        public override IReadOnlyDictionary<SecurityType, string> DefaultMarkets { get; } = GetDefaultMarkets();



        /// <summary>
        /// Gets a new buying power model for the security, returning the default model with the security's configured leverage.
        /// For cash accounts, leverage = 1 is used.
        /// For margin trading, max leverage = 7
        /// </summary>
        /// <param name="security">The security to get a buying power model for</param>
        /// <returns>The buying power model for this brokerage/security</returns>
        public override IBuyingPowerModel GetBuyingPowerModel(Security security)
        {
            return AccountType == AccountType.Cash
                ? (IBuyingPowerModel)new CashBuyingPowerModel()
                : new SecurityMarginModel(_maxLeverage);
        }

        /// <summary>
        /// Zerodha global leverage rule
        /// </summary>
        /// <param name="security"></param>
        /// <returns></returns>
        public override decimal GetLeverage(Security security)
        {
            if (AccountType == AccountType.Cash || security.IsInternalFeed() || security.Type == SecurityType.Base)
            {
                return 1m;
            }

            if (security.Type == SecurityType.Equity || security.Type == SecurityType.Future || security.Type == SecurityType.Option)
            {
                return _maxLeverage;
            }

            throw new ArgumentException($"Invalid security type: {security.Type}", nameof(security));
        }

        /// <summary>
        /// Provides Samco fee model
        /// </summary>
        /// <param name="security"></param>
        /// <returns></returns>
        public override IFeeModel GetFeeModel(Security security)
        {
            return new SamcoFeeModel();
        }

        private static IReadOnlyDictionary<SecurityType, string> GetDefaultMarkets()
        {
            var map = DefaultMarketMap.ToDictionary();
            map[SecurityType.Equity] = Market.NSE;
            map[SecurityType.Future] = Market.NFO;
            map[SecurityType.Option] = Market.NFO;
            map[SecurityType.Future] = Market.MCX;
            map[SecurityType.Option] = Market.MCX;
            map[SecurityType.Equity] = Market.BSE;
            return map.ToReadOnlyDictionary();
        }
    }
}
