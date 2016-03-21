using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.Providers.LinearAlgebra.OpenBlas;

namespace QuantConnect.Data.Market
{
    public class VolumeBar : BaseData, IBar
    {
        public virtual decimal Open
        {
            get;
            set;
        }

        public virtual decimal High
        {
            get;
            set;
        }

        public virtual decimal Low
        {
            get;
            set;
        }

        public virtual decimal Close
        {
            get;
            set;
        }

        public virtual long Volume
        {
            get;
            set;
        }
    }
}
