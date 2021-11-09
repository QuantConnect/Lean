using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Interfaces;

namespace QuantConnect.Tests.Engine.DataCacheProviders
{
    /// <summary>
    /// Abstract class the provides common test cases for all DataCacheProviders
    /// </summary>
    public abstract class DataCacheProviderTests
    {
        protected IDataCacheProvider DataCacheProvider;

        [OneTimeSetUp]
        public void Setup()
        {
            DataCacheProvider = CreateDataCacheProvider();
        }

        public abstract IDataCacheProvider CreateDataCacheProvider();

        [Test]
        public void CanFetchDataThatExists()
        {
            var stream = DataCacheProvider.Fetch("../../../Data/equity/usa/minute/aapl/20140606_trade.zip");

            Assert.IsNotNull(stream);
        }

        [Test]
        public void CannotFetchDataThatDoesNotExist()
        {
            var stream = DataCacheProvider.Fetch("../../../Data/equity/usa/minute/aapl/19980606_trade.zip");

            Assert.IsNull(stream);
        }
    }
}
