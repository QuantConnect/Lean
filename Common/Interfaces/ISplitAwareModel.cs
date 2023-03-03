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

using System.ComponentModel.Composition;

using QuantConnect.Data.Market;

namespace QuantConnect.Interfaces
{
    /// <summary>
    /// </summary>
    [InheritedExport(typeof(ISplitAwareModel))]
    public interface ISplitAwareModel
    {
        /// <summary>
        /// Applies a dividend to the model
        /// </summary>
        /// <param name="dividend">The dividend to be applied</param>
        /// <param name="liveMode">True if live mode, false for backtest</param>
        /// <param name="dataNormalizationMode">The <see cref="DataNormalizationMode"/> for the security</param>
        void ApplyDividend(Dividend dividend, bool liveMode, DataNormalizationMode dataNormalizationMode);

        /// <summary>
        /// Applies a split to the model
        /// </summary>
        /// <param name="split">The split to be applied</param>
        /// <param name="liveMode">True if live mode, false for backtest</param>
        /// <param name="dataNormalizationMode">The <see cref="DataNormalizationMode"/> for the security</param>
        void ApplySplit(Split split, bool liveMode, DataNormalizationMode dataNormalizationMode);
    }
}
