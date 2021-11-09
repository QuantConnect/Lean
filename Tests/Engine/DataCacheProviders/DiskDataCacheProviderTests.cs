using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Interfaces;

namespace QuantConnect.Tests.Engine.DataCacheProviders
{
    [TestFixture]
    public class DiskDataCacheProviderTests : DataCacheProviderTests
    {
        public override IDataCacheProvider CreateDataCacheProvider()
        {
            return new DiskDataCacheProvider();
        }
    }
}
