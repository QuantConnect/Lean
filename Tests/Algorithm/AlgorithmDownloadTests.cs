using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Algorithm;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuantConnect.Tests.Algorithm
{
    [TestFixture]
    public class AlgorithmDownloadTests
    {
        [Test]
        public void Download_Without_Parameters_Successfully()
        {
            var algo = new QCAlgorithm();
            var content = string.Empty;
            Assert.DoesNotThrow(() => content = algo.Download("https://www.quantconnect.com/"));
            Assert.IsNotEmpty(content);
        }

        [Test]
        public void Download_With_CSharp_Parameter_Successfully()
        {
            var algo = new QCAlgorithm();

            var byteKey = Encoding.ASCII.GetBytes($"UserName:Password");
            var headers = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Authorization", $"Basic ({Convert.ToBase64String(byteKey)})")
            };

            var content = string.Empty;
            Assert.DoesNotThrow(() => content = algo.Download("https://www.quantconnect.com/", headers));
            Assert.IsNotEmpty(content);
        }

        [Test, Category("TravisExclude")]
        public void Download_With_Python_Parameter_Successfully()
        {
            var algo = new QCAlgorithm();

            var byteKey = Encoding.ASCII.GetBytes($"UserName:Password");
            var value = $"Basic ({Convert.ToBase64String(byteKey)})";

            var headers = new PyDict();
            using (Py.GIL())
            {
                headers.SetItem("Authorization".ToPython(), value.ToPython());
            }

            var content = string.Empty;
            Assert.DoesNotThrow(() => content = algo.Download("https://www.quantconnect.com/", headers));
            Assert.IsNotEmpty(content);
        }
    }
}
