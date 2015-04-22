using QuantConnect.Indicators;
using NUnit.Framework;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class StochasticTests
    {
        [Test]
        public void ComparesAgainstExternalDataOnStochasticsK()
        {
            var stochastics = new Stochastic("sto", 12, 3, 5);

            const double epsilon = 1e-3;

            TestHelper.TestIndicator(stochastics, "spy_with_stoch12k3.txt", "Stochastics 12 %K 3",
            (ind, expected) => Assert.AreEqual(expected, (double)((Stochastic)ind).StochK.Current.Value, epsilon)

            );
        }

        [Test]
        public void ComparesAgainstExternalDataOnStochasticsD()
        {
            var stochastics = new Stochastic("sto", 12, 3, 5);

            const double epsilon = 1e-3;
            TestHelper.TestIndicator(stochastics, "spy_with_stoch12k3.txt", "%D 5",
                 (ind, expected) => Assert.AreEqual(expected, (double)((Stochastic)ind).StochD.Current.Value, epsilon)

                );
        }
    }
}
