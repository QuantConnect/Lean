using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Data.LevelOne
{
    /// <summary>
    /// Provides data for an event that is triggered when a new <see cref="BaseData"/> is received.
    /// </summary>
    public sealed class BaseDataEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the <see cref="BaseData"/> data associated with the event.
        /// </summary>
        public BaseData BaseData { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseDataEventArgs"/> class with the specified <see cref="BaseData"/>.
        /// </summary>
        /// <param name="tick">The <see cref="BaseData"/> data associated with the event.</param>
        public BaseDataEventArgs(BaseData tick)
        {
            BaseData = tick;
        }
    }
}
