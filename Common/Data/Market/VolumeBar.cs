/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// Base class that provides common fields for bar types that have OHLC and Volume
    /// </summary>
    public class VolumeBar : BaseData, IBar
    {
        /// <remarks>
        /// This class is not abstract because the tests need to instantiate a common type
        /// for the test data, however it is not inteded to be created at runtime by anything else
        /// </remarks>
        internal VolumeBar()
        {
        }

        /// <summary>
        /// The opening value of the bar
        /// </summary>
        public virtual decimal Open
        {
            get;
            set;
        }

        /// <summary>
        /// The highest price during the bar
        /// </summary>
        public virtual decimal High
        {
            get;
            set;
        }

        /// <summary>
        /// The lowest price during the bar
        /// </summary>
        public virtual decimal Low
        {
            get;
            set;
        }

        /// <summary>
        /// The final value for the bar
        /// </summary>
        public virtual decimal Close
        {
            get { return Value; }
            set { Value = value; }
        }

        /// <summary>
        /// The volume of trades during the bar
        /// </summary>
        public virtual long Volume
        {
            get;
            set;
        }
    }
}
