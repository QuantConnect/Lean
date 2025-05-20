using System;
using System.Globalization;
using QuantConnect.Data;
using Newtonsoft.Json;
using QuantConnect.Securities;
using QuantConnect.Orders.Fees;
using QuantConnect.Brokerages;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp.ChinaTrade.Models
{
    public class AStockBrokerageModel : DefaultBrokerageModel
    {
        public override IFeeModel GetFeeModel(Security security)
        {
            return new AStockFeeModel();
        }
    }
    /// <summary>
    /// 实现A股费用计算
    /// </summary>
    public class AStockFeeModel : IFeeModel
    {
        public OrderFee GetOrderFee(OrderFeeParameters parameters)
        {
            // 计算佣金（双向收取0.02%）
            var commission = parameters.Security.Price * parameters.Order.AbsoluteQuantity * 0.0002m;
            // 计算印花税（仅卖出收取0.1%）
            var stampDuty = 0m;
            if (parameters.Order.Direction == OrderDirection.Sell)
            {
                stampDuty = parameters.Security.Price * parameters.Order.AbsoluteQuantity * 0.001m;
            }

            var totalFee = commission + stampDuty;
            
            // 返回费用结果（使用人民币结算）
            return new OrderFee(new CashAmount(totalFee, "CNY"));
        }
    }
}