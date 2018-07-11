using System;
using NUnit.Framework;
using QuantConnect.Orders;

namespace QuantConnect.Tests.Common.Orders
{
    [TestFixture]
    public class OrderTicketTests
    {
        [Test]
        public void TestInvalidUpdateOrderId()
        {
            var updateFields = new UpdateOrderFields { Quantity = 99, Tag = "Pepe", StopPrice = 77 , LimitPrice = 55 };
            var updateRequest = new UpdateOrderRequest(DateTime.Now, 11, updateFields);
            var ticket = OrderTicket.InvalidUpdateOrderId(null, updateRequest);
            Assert.AreEqual(ticket.OrderId, 11);
            Assert.AreEqual(ticket.Quantity, 0);
            Assert.AreEqual(ticket.Tag, "Pepe");
            Assert.AreEqual(ticket.Status, OrderStatus.Invalid);
            Assert.AreEqual(ticket.UpdateRequests.Count, 1);
            Assert.AreEqual(ticket.UpdateRequests[0].Status, OrderRequestStatus.Error);
            Assert.AreEqual(ticket.UpdateRequests[0].Response.ErrorCode, OrderResponseErrorCode.UnableToFindOrder);
            Assert.AreEqual(ticket.UpdateRequests[0].OrderId, 11);
            Assert.AreEqual(ticket.UpdateRequests[0].Quantity, 99);
            Assert.AreEqual(ticket.UpdateRequests[0].Tag, "Pepe");
            Assert.AreEqual(ticket.UpdateRequests[0].StopPrice, 77);
            Assert.AreEqual(ticket.UpdateRequests[0].LimitPrice, 55);
        }
        [Test]
        public void TestInvalidCancelOrderId()
        {
            var cancelRequest = new CancelOrderRequest(DateTime.Now, 11, "Pepe");
            var ticket = OrderTicket.InvalidCancelOrderId(null, cancelRequest);
            Assert.AreEqual(ticket.OrderId, 11);
            Assert.AreEqual(ticket.Quantity, 0);
            Assert.AreEqual(ticket.Tag, "Pepe");
            Assert.AreEqual(ticket.Status, OrderStatus.Invalid);
            Assert.AreEqual(ticket.CancelRequest, cancelRequest);
            Assert.AreEqual(ticket.CancelRequest.Status, OrderRequestStatus.Error);
            Assert.AreEqual(ticket.CancelRequest.Response.ErrorCode, OrderResponseErrorCode.UnableToFindOrder);
            Assert.AreEqual(ticket.CancelRequest.OrderId, 11);
            Assert.AreEqual(ticket.CancelRequest.Tag, "Pepe");
        }
        [Test]
        public void TestInvalidSubmitRequest()
        {
            var orderRequest = new SubmitOrderRequest(OrderType.Limit, SecurityType.Equity, Symbols.AAPL, 1000, 0, 1.11m, DateTime.Now, "Pepe");
            var order = Order.CreateOrder(orderRequest);
            orderRequest.SetOrderId(orderRequest.OrderId);
            var orderResponse = OrderResponse.InvalidStatus(orderRequest, order);
            var ticket = OrderTicket.InvalidSubmitRequest(null, orderRequest, orderResponse);
            Assert.AreEqual(ticket.OrderId, orderRequest.OrderId);
            Assert.AreEqual(ticket.Quantity, 1000);
            Assert.AreEqual(ticket.Tag, "Pepe");
            Assert.AreEqual(ticket.Status, OrderStatus.Invalid);
            Assert.AreEqual(ticket.OrderType, OrderType.Limit);
            Assert.AreEqual(ticket.SecurityType, SecurityType.Equity);
            Assert.AreEqual(ticket.Symbol, Symbols.AAPL);
            Assert.AreEqual(ticket.SubmitRequest, orderRequest);
            Assert.AreEqual(ticket.SubmitRequest.Status, OrderRequestStatus.Error);
            Assert.AreEqual(ticket.SubmitRequest.OrderId, orderRequest.OrderId);
            Assert.AreEqual(ticket.SubmitRequest.Quantity, 1000);
            Assert.AreEqual(ticket.SubmitRequest.Tag, "Pepe");
        }
        [Test]
        public void TestInvalidWarmingUp()
        {
            var orderRequest = new SubmitOrderRequest(OrderType.Limit, SecurityType.Equity, Symbols.AAPL, 1000, 0, 1.11m, DateTime.Now, "Pepe");
            orderRequest.SetOrderId(orderRequest.OrderId);
            var ticket = OrderTicket.InvalidWarmingUp(null, orderRequest);
            Assert.AreEqual(ticket.OrderId, orderRequest.OrderId);
            Assert.AreEqual(ticket.Quantity, 1000);
            Assert.AreEqual(ticket.Tag, "Pepe");
            Assert.AreEqual(ticket.Status, OrderStatus.Invalid);
            Assert.AreEqual(ticket.OrderType, OrderType.Limit);
            Assert.AreEqual(ticket.SecurityType, SecurityType.Equity);
            Assert.AreEqual(ticket.Symbol, Symbols.AAPL);
            Assert.AreEqual(ticket.SubmitRequest, orderRequest);
            Assert.AreEqual(ticket.SubmitRequest.Status, OrderRequestStatus.Error);
            Assert.AreEqual(ticket.SubmitRequest.OrderId, orderRequest.OrderId);
            Assert.AreEqual(ticket.SubmitRequest.Quantity, 1000);
            Assert.AreEqual(ticket.SubmitRequest.Tag, "Pepe");
        }
    }
}
