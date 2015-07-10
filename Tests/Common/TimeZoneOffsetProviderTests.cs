using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class TimeZoneOffsetProviderTests
    {
        [Test]
        public void ReturnsCurrentOffset()
        {
            var utcDate = new DateTime(2015, 07, 07);
            var offsetProvider = new TimeZoneOffsetProvider(TimeZones.NewYork, utcDate, utcDate.AddDays(1));
            var currentOffset = offsetProvider.GetOffsetTicks(utcDate);
            Assert.AreEqual(-TimeSpan.FromHours(4).TotalHours, TimeSpan.FromTicks(currentOffset).TotalHours);
        }
    }
}
