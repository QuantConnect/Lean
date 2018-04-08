using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace QuantConnect.VisualStudioPlugin
{
    /// <summary>
    /// Item that represents a row in the backtest data grid. Will notify property changes if there is a handler set.
    /// </summary>
    class DataGridItem : INotifyPropertyChanged
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
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}