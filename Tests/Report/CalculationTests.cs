
using Deedle;
using NUnit.Framework;
using QuantConnect.Report;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Tests.Report
{
    [TestFixture]
    public class CalculationTests
    {
        [TestCase(new double[] { 1, 2, 4, 8 }, new double[] { 1, 1, 1 })]
        [TestCase(new double[] { 0, 4, 5, 2.5 }, new double[] { 0.25, -0.5 })]
        public void PercentChangeProducesCorrectValues(double[] inputs, double[] expected)
        {
            var series = CreateFakeSeries(inputs).PercentChange().Values.ToList();

            Assert.AreEqual(expected, series);
        }

        [TestCase(new double[] { 1, 2, 3, 4 }, new double[] { 1, 3, 6, 10 })]
        [TestCase(new double[] { 0, 0, 0, 0 }, new double[] { 0, 0, 0, 0 })]
        [TestCase(new double[] { 0.25, 0.5, 0.75, 1}, new double[] { 0.25, 0.75, 1.5, 2.5 })]
        public void CumulativeSumProducesCorrectValues(double[] inputs, double[] expected)
        {
            var series = CreateFakeSeries(inputs).CumulativeSum().Values.ToList();

            Assert.AreEqual(expected, series);
        }

        private Series<DateTime, double> CreateFakeSeries(double[] inputs)
        {
            var i = 0;
            return new Series<DateTime, double>(inputs.Select(_ =>
            {
                var time = new DateTime(1, 1, 1).AddDays(i);
                i++;

                return time;
            }).ToList(), inputs);
        }
    }
}
