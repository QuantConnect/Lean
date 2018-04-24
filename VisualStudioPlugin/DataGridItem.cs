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
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace QuantConnect.VisualStudioPlugin
{
    /// <summary>
    /// Item that represents a row in the backtest data grid. Will notify property changes if there is a handler set.
    /// </summary>
    internal class DataGridItem : INotifyPropertyChanged
    {
        public const string BacktestSucceeded = "Successful";
        public const string BacktestFailed = "Failed";
        public const string BacktestInProgress = "In Progress";

        private string _name;
        public string Name
        {
            get
            {
                return _name;

            }
            set
            {
                if (value != _name)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }
        private decimal _progress;
        public decimal Progress
        {
            get
            {
                return _progress;

            }
            set
            {
                if (value != _progress)
                {
                    _progress = value;
                    OnPropertyChanged();
                }
            }
        }
        private DateTime _date;
        public DateTime Date
        {
            get
            {
                return _date;

            }
            set
            {
                if (value != _date)
                {
                    _date = value;
                    OnPropertyChanged();
                }
            }
        }
        private int _projectId;
        public int ProjectId
        {
            get
            {
                return _projectId;

            }
            set
            {
                if (value != _projectId)
                {
                    _projectId = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _backtestId;
        public string BacktestId
        {
            get
            {
                return _backtestId;

            }
            set
            {
                if (value != _backtestId)
                {
                    _backtestId = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _status;
        public string Status
        {
            get
            {
                return _status;

            }
            set
            {
                if (value != _status)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _note;
        public string Note
        {
            get
            {
                return _note;

            }
            set
            {
                if (value != _note)
                {
                    _note = value;
                    OnPropertyChanged();
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}