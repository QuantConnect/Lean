/*
 * Simple live test algorithm for IG Markets brokerage
 * Subscribes to EURUSD, logs market data, and places a small test trade
 */

using System;
using QuantConnect.Data;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    public class IGLiveTestAlgorithm : QCAlgorithm
    {
        private Symbol _eurusd;
        private bool _tradePlaced;
        private int _dataPoints;

        public override void Initialize()
        {
            SetStartDate(DateTime.UtcNow.Date);
            SetCash(100000);

            SetBrokerageModel(Brokerages.BrokerageName.IG, AccountType.Margin);

            // Subscribe to EURUSD forex pair on IG market
            _eurusd = AddForex("EURUSD", Resolution.Second, Market.IG).Symbol;

            Log("IGLiveTestAlgorithm: Initialized - subscribing to EURUSD on IG Markets");
        }

        public override void OnData(Slice data)
        {
            _dataPoints++;

            if (data.QuoteBars.ContainsKey(_eurusd))
            {
                var bar = data.QuoteBars[_eurusd];
                Log($"IGLiveTestAlgorithm: EURUSD Bid={bar.Bid.Close} Ask={bar.Ask.Close} Time={bar.EndTime}");
            }

            // Place a small market order after receiving some data
            if (!_tradePlaced && _dataPoints >= 3)
            {
                Log("IGLiveTestAlgorithm: Placing test market order - Buy 1000 EURUSD");
                var ticket = MarketOrder(_eurusd, 1000);
                _tradePlaced = true;
                Log($"IGLiveTestAlgorithm: Order placed - Ticket ID: {ticket.OrderId}");
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Log($"IGLiveTestAlgorithm: OrderEvent - {orderEvent}");
        }

        public override void OnEndOfAlgorithm()
        {
            Log($"IGLiveTestAlgorithm: Algorithm ended. Total data points received: {_dataPoints}");
        }
    }
}
