using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using System;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class VortexTests : CommonIndicatorTests<IBaseDataBar>
    {
        protected override IndicatorBase<IBaseDataBar> CreateIndicator()
        {
            RenkoBarSize = 1m;
            return new Vortex(14);
        }

        protected override string TestFileName => "spy_with_vtx.csv";

        protected override string TestColumnName => "plus_vtx";  

        [Test]
        public override void ComparesAgainstExternalData()
        {
            const double epsilon = 0.0001; 

            var vortex = CreateIndicator(); 

            TestHelper.TestIndicator(vortex, TestFileName, "plus_vtx",
                (ind, expected) => Assert.AreEqual(expected, (double)((Vortex)ind).PlusVI.Current.Value, epsilon)
            );

   

        }

        [Test]
        public override void ComparesAgainstExternalDataAfterReset()
        {
            const double epsilon = 0.0001;  

            var vortex = CreateIndicator(); 

          
            TestHelper.TestIndicator(vortex, TestFileName, "plus_vtx",
                (ind, expected) => Assert.AreEqual(expected, (double)((Vortex)ind).PlusVI.Current.Value, epsilon)
            );

            vortex.Reset();  
     
            TestHelper.TestIndicator(vortex, TestFileName, "minus_vtx",
                (ind, expected) => Assert.AreEqual(expected, (double)((Vortex)ind).MinusVI.Current.Value, epsilon)
            );
   

        }
    }
}
