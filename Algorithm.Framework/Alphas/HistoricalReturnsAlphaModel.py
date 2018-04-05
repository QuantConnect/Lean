from clr import AddReference
AddReference("QuantConnect.Algorithm.Framework")
AddReference("QuantConnect.Indicators")
AddReference("QuantConnect.Common")

from QuantConnect import *
from QuantConnect.Indicators import *
from QuantConnect.Algorithm.Framework.Alphas import *
from datetime import timedelta

class HistoricalReturnsAlphaModel:
    '''Uses Historical returns to create insights.'''

    def __init__(self, *args, **kwargs):
        '''Initializes a new default instance of the HistoricalReturnsAlphaModel class.
        Args:
            resolution: The resolution of historical data'''
        self.resolution = kwargs['resolution'] if 'resolution' in kwargs else Resolution.Daily
        self.predictionInterval = timedelta(1)
        self.symbolDataBySymbol = {}

    def Update(self, algorithm, data):
        '''Updates this alpha model with the latest data from the algorithm.
        This is called each time the algorithm receives data for subscribed securities
        Args:
            algorithm: The algorithm instance
            data: The new data available
        Returns:
            The new insights generated'''
        insights = []
        for symbol, symbolData in self.symbolDataBySymbol.items():
            if symbolData.CanEmit:
                insights.append(Insight(symbol, InsightType.Price, InsightDirection.Flat, self.predictionInterval, symbolData.Return, None))

        return insights

    def OnSecuritiesChanged(self, algorithm, changes):
        '''Event fired each time the we add/remove securities from the data feed
        Args:
            algorithm: The algorithm instance that experienced the change in securities
            changes: The security additions and removals from the algorithm'''

        # clean up data for removed securities
        removed = [ x.Symbol for x in changes.RemovedSecurities ]
        if len(removed) > 0:
            for subscription in algorithm.SubscriptionManager.Subscriptions:
                symbol = subscription.Symbol
                if symbol in removed and symbol in self.symbolDataBySymbol:
                    subscription.Consolidators.Clear()
                    self.symbolDataBySymbol.pop(symbol)

        # initialize data for added securities
        for added in changes.AddedSecurities:
            if added.Symbol not in self.symbolDataBySymbol:
                symbolData = SymbolData(added)
                self.symbolDataBySymbol[added.Symbol] = symbolData
                algorithm.RegisterIndicator(added.Symbol, symbolData.ROC, self.resolution)


class SymbolData:
    '''Contains data specific to a symbol required by this model'''
    def __init__(self, symbol):
        self.Symbol = symbol
        self.ROC = RateOfChange(1)
        self.previous = 0

    @property
    def Return(self):
        return float(self.ROC.Current.Value)

    @property
    def CanEmit(self):
        if self.previous == self.ROC.Samples:
            return False

        self.previous = self.ROC.Samples
        return self.ROC.IsReady

    def __str__(self, **kwargs):
        return '{} -> Return: {:.2f}'.format(self.Symbol, (1 + self.Return)**252 - 1)
