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

using System;
using QuantConnect.Data.Market;

namespace QuantConnect.Data.Consolidators
{
    /// <summary>
    /// This consolidator can transform a stream of <see cref="BaseData"/> instances into a stream
    /// </summary>
    public class RenkoConsolidator : DataConsolidator<IBaseData>
    {
        /// <summary>
        /// Event handler that fires when a new piece of data is produced
        /// </summary>
        public new EventHandler<RenkoBar> DataConsolidated; 
        
        private RenkoBar _currentBlock;

        private readonly decimal _brickSize;
        private readonly bool _evenBricks;
        private readonly Func<IBaseData, decimal> _selector;
        private readonly Func<IBaseData, long> _volumeSelector;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="RenkoConsolidator"/> class using the specified <paramref name="brickSize"/>.
        /// The value selector will by default select <see cref="IBaseData.Value"/>
        /// The volume selector will by default select zero.
        /// </summary>
        /// <param name="brickSize">The constant value size of each block</param>
        /// <param name="evenBricks">When true brick open/close will be a multiple of the brickSize</param>
        public RenkoConsolidator(decimal brickSize, bool evenBricks = true)
        {
            _brickSize = brickSize;
            _selector = x => x.Value;
            _volumeSelector = x => 0;
            _evenBricks = evenBricks;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RenkoConsolidator" /> class.
        /// </summary>
        /// <param name="brickSize">The size of each block in units of the value produced by <paramref name="selector"/></param>
        /// <param name="selector">Extracts the value from a data instance to be formed into a <see cref="RenkoBar"/>. The default
        /// value is (x => x.Value) the <see cref="IBaseData.Value"/> property on <see cref="IBaseData"/></param>
        /// <param name="volumeSelector">Extracts the volume from a data instance. The default value is null which does 
        /// not aggregate volume per bar.</param>
        /// <param name="evenBricks">When true brick open/close will be a multiple of the brickSize</param>
        public RenkoConsolidator(decimal brickSize, Func<IBaseData, decimal> selector, Func<IBaseData, long> volumeSelector = null, bool evenBricks = true)
        {
            if (brickSize < Extensions.GetDecimalEpsilon())
            {
                throw new ArgumentOutOfRangeException("brickSize", "RenkoConsolidator block size must be positve and greater than 1e-28");
            }

            _brickSize = brickSize;
            _evenBricks = evenBricks;
            _selector = selector ?? (x => x.Value);
            _volumeSelector = volumeSelector ?? (x => 0);
        }

        /// <summary>
        /// Gets the block size used by this consolidator
        /// </summary>
        public decimal BrickSize
        {
            get { return _brickSize; }
        }

        /// <summary>
        /// Gets <see cref="RenkoBar"/> which is the type emitted in the <see cref="IDataConsolidator.DataConsolidated"/> event.
        /// </summary>
        public override Type OutputType
        {
            get { return typeof(RenkoBar); }
        }

        /// <summary>
        /// Updates this consolidator with the specified data. This method is
        /// responsible for raising the DataConsolidated event
        /// </summary>
        /// <param name="data">The new data for the consolidator</param>
        public override void Update(IBaseData data)
        {
            var currentValue = _selector(data);
            var volume = _volumeSelector(data);

            decimal? close = null;
            
            // if we're already in a block then update it
            if (_currentBlock != null)
            {
                _currentBlock.Update(data.Time, currentValue, volume);

                // if the update caused this block to close, fire the event and reset the block
                if (_currentBlock.IsClosed)
                {
                    close = _currentBlock.Close;
                    OnDataConsolidated(_currentBlock);
                    _currentBlock = null;
                }
            }

            if (_currentBlock == null)
            {
                var open = close ?? currentValue;
                if (_evenBricks && !close.HasValue)
                {
                    open = Math.Ceiling(open/_brickSize)*_brickSize;
                }
                _currentBlock = new RenkoBar(data.Symbol, data.Time, _brickSize, open, volume);
            }
        }

        /// <summary>
        /// Event invocator for the DataConsolidated event. This should be invoked
        /// by derived classes when they have consolidated a new piece of data.
        /// </summary>
        /// <param name="consolidated">The newly consolidated data</param>
        protected virtual void OnDataConsolidated(RenkoBar consolidated)
        {
            var handler = DataConsolidated;
            if (handler != null) handler(this, consolidated);

            base.OnDataConsolidated(consolidated);
        }
    }
}
