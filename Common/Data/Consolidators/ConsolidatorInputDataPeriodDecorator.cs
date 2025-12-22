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

namespace QuantConnect.Data.Consolidators
{
    /// <summary>
    /// Decorates an <see cref="IDataConsolidator"/> with a fixed <see cref="IConsolidatorInputDataRequirement.MaxInputDataPeriod"/>
    /// so that subscription selection can be performed deterministically.
    /// </summary>
    public sealed class ConsolidatorInputDataPeriodDecorator : IDataConsolidator, IConsolidatorInputDataRequirement
    {
        private readonly IDataConsolidator _inner;
        private bool _disposed;

        public ConsolidatorInputDataPeriodDecorator(IDataConsolidator inner, TimeSpan maxInputDataPeriod)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            MaxInputDataPeriod = maxInputDataPeriod;
            _inner.DataConsolidated += OnInnerDataConsolidated;
        }

        public TimeSpan? MaxInputDataPeriod { get; }

        public IBaseData Consolidated => _inner.Consolidated;
        public IBaseData WorkingData => _inner.WorkingData;
        public Type InputType => _inner.InputType;
        public Type OutputType => _inner.OutputType;

        public event DataConsolidatedHandler DataConsolidated;

        public void Update(IBaseData data) => _inner.Update(data);

        public void Scan(DateTime currentLocalTime) => _inner.Scan(currentLocalTime);

        public void Reset() => _inner.Reset();

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;

            _inner.DataConsolidated -= OnInnerDataConsolidated;
            _inner.Dispose();
            DataConsolidated = null;
        }

        private void OnInnerDataConsolidated(object sender, IBaseData consolidated)
        {
            DataConsolidated?.Invoke(this, consolidated);
        }
    }
}

