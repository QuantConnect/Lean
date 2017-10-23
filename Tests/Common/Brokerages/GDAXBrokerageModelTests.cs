using Moq;
using NUnit.Framework;
using QuantConnect.Brokerages;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Tests.Brokerages.GDAX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Tests.Common.Brokerages
{
    [TestFixture()]
    public class GDAXBrokerageModelTests
    {

        GDAXBrokerageModel _unit = new GDAXBrokerageModel();

        [Test()]
        public void GetLeverageTest()
        {
            Assert.AreEqual(3, _unit.GetLeverage(GDAXTestsHelpers.GetSecurity()));
        }

        [Test()]
        public void GetFeeModelTest()
        {
            Assert.IsInstanceOf<GDAXFeeModel>(_unit.GetFeeModel(GDAXTestsHelpers.GetSecurity()));
        }

        [Test()]
        public void CanUpdateOrderTest()
        {
            BrokerageMessageEvent message;
            Assert.AreEqual(false, _unit.CanUpdateOrder(GDAXTestsHelpers.GetSecurity(), Mock.Of<QuantConnect.Orders.Order>(),
                new QuantConnect.Orders.UpdateOrderRequest(DateTime.UtcNow, 1, new QuantConnect.Orders.UpdateOrderFields()), out message));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void CanSubmitOrder_WhenBrokerageIdIsCorrect(bool isUpdate)
        {
            BrokerageMessageEvent message;
            var order = new Mock<QuantConnect.Orders.Order>();
            // Order quantity must be greater than 0.01
            // Order brokerageId is under test
            order.Object.Quantity = 0.01m;

            if (isUpdate)
            {
                order.Object.BrokerId = new List<string>() {"abc123"};
            }

            Assert.AreEqual(!isUpdate, _unit.CanSubmitOrder(GDAXTestsHelpers.GetSecurity(), order.Object, out message));
        }

        [TestCase(0.01, true)]
        [TestCase(0.009, false)]
        public void CanSubmitOrder_WhenQuantityIsLargeEnough(decimal orderQuantity, bool isValidOrderQuantity)
        {
            BrokerageMessageEvent message;
            var order = new Mock<QuantConnect.Orders.Order>();

            order.Object.Quantity = orderQuantity;

            Assert.AreEqual(isValidOrderQuantity, _unit.CanSubmitOrder(GDAXTestsHelpers.GetSecurity(), order.Object, out message));
        }
    }
}