namespace QuantConnect.VisualStudioPlugin
{
    /// <summary>
    /// Backtest observer interface. Used to notify backtest status executed from the IDE.
    /// </summary>
    internal interface IBacktestObserver
    {
        void BacktestCreated(int projectId, Api.Backtest backtestStatus);
        void BacktestStatusUpdated(int projectId, Api.Backtest backtestStatus);
        void BacktestFinished(int projectId, Api.Backtest backtestStatus);
    }
}
