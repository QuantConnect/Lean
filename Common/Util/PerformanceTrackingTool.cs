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
 *
*/

using System;
using QuantConnect.Interfaces;
using System.Runtime.CompilerServices;

namespace QuantConnect.Util
{
    /// <summary>
    /// Helper class to track algorithm performance
    /// </summary>
    public class PerformanceTrackingTool
    {
        private Series _onDataSeries;
        private Series _dataSubscriptionSeries;
        private Series _scheduleSeries;
        private Series _selectionSeries;
        private Series _sliceCreationSeries;
        private Series _wallTimeSeries;

        private Series _activeSecuritiesCount;
        private Series _consumedDataPointsCount;

        private Series _cpuUsage;
        private Series _ramUsage;

        private PerformanceTimer _onData;
        private PerformanceTimer _dataSubscription;
        private PerformanceTimer _schedule;
        private PerformanceTimer _selection;
        private PerformanceTimer _sliceCreation;
        private bool _sampleEnabled;

        private IAlgorithm _algorithm;
        private long _previousDps;
        private DateTime _startWallTime;
        private DateTime _nextSampleAlgoTime;
        private DateTime _previousSampleAlgoTime;

        private DateTime _previousSampleWallTime;

        /// <summary>
        /// Gets the number of data points processed per second
        /// </summary>
        public long DataPoints { get; private set; }

        public void Initialize(IAlgorithm algorithm)
        {
            _algorithm = algorithm;
            _sampleEnabled = algorithm.Settings.PerformanceSamplePeriod > TimeSpan.Zero;
            if (_sampleEnabled)
            {
                _onData = new();
                _dataSubscription = new();
                _schedule = new();
                _selection = new();
                _sliceCreation = new();

                var chart = new Chart("Performance");

                _onDataSeries = new Series(PerformanceTarget.OnData.ToString());
                _dataSubscriptionSeries = new Series(PerformanceTarget.DataSubscription.ToString());
                _scheduleSeries = new Series(PerformanceTarget.Schedule.ToString());
                _selectionSeries = new Series(PerformanceTarget.Selection.ToString());
                _sliceCreationSeries = new Series(PerformanceTarget.SliceCreation.ToString());
                _wallTimeSeries = new Series("WallTime");
                _activeSecuritiesCount = new Series("ActiveSecurities");
                _consumedDataPointsCount = new Series("DataPoints");
                _cpuUsage = new Series("CPU");
                _ramUsage = new Series("RAM");

                chart.AddSeries(_cpuUsage);
                chart.AddSeries(_ramUsage);
                chart.AddSeries(_onDataSeries);
                chart.AddSeries(_dataSubscriptionSeries);
                chart.AddSeries(_scheduleSeries);
                chart.AddSeries(_wallTimeSeries);
                chart.AddSeries(_selectionSeries);
                chart.AddSeries(_sliceCreationSeries);
                chart.AddSeries(_activeSecuritiesCount);
                chart.AddSeries(_consumedDataPointsCount);
                algorithm.AddChart(chart);

                _previousSampleWallTime = _startWallTime = DateTime.UtcNow;
                _previousSampleAlgoTime = algorithm.UtcTime;
                _nextSampleAlgoTime = _previousSampleAlgoTime + _algorithm.Settings.PerformanceSamplePeriod;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Sample(int dataPointCount, DateTime utcAlgoTime)
        {
            DataPoints += dataPointCount;
            if (!_sampleEnabled)
            {
                return;
            }

            if (utcAlgoTime >= _nextSampleAlgoTime)
            {
                var nowUtc = DateTime.UtcNow;

                // these share the same unit, real wall time
                _onDataSeries.AddPoint(utcAlgoTime, _onData.GetAndReset());
                _dataSubscriptionSeries.AddPoint(utcAlgoTime, _dataSubscription.GetAndReset());
                _scheduleSeries.AddPoint(utcAlgoTime, _schedule.GetAndReset());
                _selectionSeries.AddPoint(utcAlgoTime, _selection.GetAndReset());
                _sliceCreationSeries.AddPoint(utcAlgoTime, _sliceCreation.GetAndReset());
                _wallTimeSeries.AddPoint(utcAlgoTime, (decimal)Math.Round((nowUtc - _previousSampleWallTime).TotalSeconds, 5));

                _activeSecuritiesCount.AddPoint(utcAlgoTime, _algorithm.UniverseManager.ActiveSecurities.Count);
                _consumedDataPointsCount.AddPoint(utcAlgoTime, DataPoints - _previousDps);

                _cpuUsage.AddPoint(utcAlgoTime, OS.CpuUsage);
                _ramUsage.AddPoint(utcAlgoTime, OS.TotalPhysicalMemoryUsed);

                _previousDps = DataPoints;
                _previousSampleWallTime = nowUtc;
                _nextSampleAlgoTime = utcAlgoTime + _algorithm.Settings.PerformanceSamplePeriod;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Start(PerformanceTarget target)
        {
            if (!_sampleEnabled)
            {
                return;
            }
            Get(target).Start();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Stop(PerformanceTarget target)
        {
            if (!_sampleEnabled)
            {
                return;
            }
            Get(target).Stop();
        }

        public void Shutdown()
        {
            if (!_sampleEnabled)
            {
                return;
            }

            var endTime = DateTime.UtcNow;
            var message = $"Performance - Dps {DataPoints}." +
                $" TotalRuntime: {endTime - _startWallTime}." +
                $" OnData: {_onData.GetTotalTime()}s." +
                $" DataSubscription: {_dataSubscription.GetTotalTime()}s." +
                $" SliceCreation: {_sliceCreation.GetTotalTime()}s." +
                $" Selection: {_selection.GetTotalTime()}s." +
                $" Schedule: {_schedule.GetTotalTime()}s." +
                $" ActiveSecurities: {_algorithm.UniverseManager.ActiveSecurities}";

            Logging.Log.Trace($"PerformanceTrackingTool.Summary(): {message}");
            _algorithm.Debug(message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private PerformanceTimer Get(PerformanceTarget target)
        {
            switch (target)
            {
                case PerformanceTarget.DataSubscription:
                    return _dataSubscription;
                case PerformanceTarget.SliceCreation:
                    return _sliceCreation;
                case PerformanceTarget.Selection:
                    return _selection;
                case PerformanceTarget.Schedule:
                    return _schedule;
                case PerformanceTarget.OnData:
                    return _onData;
                default:
                    throw new ArgumentException(nameof(target));
            }
        }
    }

    public enum PerformanceTarget
    {
        Selection,
        DataSubscription,
        SliceCreation,
        OnData,
        Schedule,
    }
}
