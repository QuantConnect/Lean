import typing
import System.Collections.Generic
import System
import QuantConnect.Securities
import QuantConnect.Indicators
import QuantConnect.Data.UniverseSelection
import QuantConnect.Data
import QuantConnect.Algorithm.Framework.Alphas.Serialization
import QuantConnect.Algorithm.Framework.Alphas.Analysis
import QuantConnect.Algorithm.Framework.Alphas
import QuantConnect.Algorithm
import QuantConnect
import Python.Runtime
import datetime



class InsightScoreType(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Defines a specific type of score for a insight
    
    enum InsightScoreType, values: Direction (0), Magnitude (1)
    """
    value__: int
    Direction: 'InsightScoreType'
    Magnitude: 'InsightScoreType'


class InsightType(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Specifies the type of insight
    
    enum InsightType, values: Price (0), Volatility (1)
    """
    value__: int
    Price: 'InsightType'
    Volatility: 'InsightType'


class MacdAlphaModel(QuantConnect.Algorithm.Framework.Alphas.AlphaModel, QuantConnect.Algorithm.Framework.INotifiedSecurityChanges, QuantConnect.Algorithm.Framework.Alphas.IAlphaModel, QuantConnect.Algorithm.Framework.Alphas.INamedModel):
    """
    Defines a custom alpha model that uses MACD crossovers. The MACD signal line is
                used to generate up/down insights if it's stronger than the bounce threshold.
                If the MACD signal is within the bounce threshold then a flat price insight is returned.
    
    MacdAlphaModel(fastPeriod: int, slowPeriod: int, signalPeriod: int, movingAverageType: MovingAverageType, resolution: Resolution)
    """
    def OnSecuritiesChanged(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, changes: QuantConnect.Data.UniverseSelection.SecurityChanges) -> None:
        pass

    def Update(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, data: QuantConnect.Data.Slice) -> typing.List[QuantConnect.Algorithm.Framework.Alphas.Insight]:
        pass

    def __init__(self, fastPeriod: int, slowPeriod: int, signalPeriod: int, movingAverageType: QuantConnect.Indicators.MovingAverageType, resolution: QuantConnect.Resolution) -> QuantConnect.Algorithm.Framework.Alphas.MacdAlphaModel:
        pass


class NullAlphaModel(QuantConnect.Algorithm.Framework.Alphas.AlphaModel, QuantConnect.Algorithm.Framework.INotifiedSecurityChanges, QuantConnect.Algorithm.Framework.Alphas.IAlphaModel, QuantConnect.Algorithm.Framework.Alphas.INamedModel):
    """
    Provides a null implementation of an alpha model
    
    NullAlphaModel()
    """
    def Update(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, data: QuantConnect.Data.Slice) -> typing.List[QuantConnect.Algorithm.Framework.Alphas.Insight]:
        pass


class PearsonCorrelationPairsTradingAlphaModel(QuantConnect.Algorithm.Framework.Alphas.BasePairsTradingAlphaModel, QuantConnect.Algorithm.Framework.INotifiedSecurityChanges, QuantConnect.Algorithm.Framework.Alphas.IAlphaModel, QuantConnect.Algorithm.Framework.Alphas.INamedModel):
    """
    This alpha model is designed to rank every pair combination by its pearson correlation
                and trade the pair with the hightest correlation
                This model generates alternating long ratio/short ratio insights emitted as a group
    
    PearsonCorrelationPairsTradingAlphaModel(lookback: int, resolution: Resolution, threshold: Decimal, minimumCorrelation: float)
    """
    def HasPassedTest(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, asset1: QuantConnect.Symbol, asset2: QuantConnect.Symbol) -> bool:
        pass

    def OnSecuritiesChanged(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, changes: QuantConnect.Data.UniverseSelection.SecurityChanges) -> None:
        pass

    def __init__(self, lookback: int, resolution: QuantConnect.Resolution, threshold: float, minimumCorrelation: float) -> QuantConnect.Algorithm.Framework.Alphas.PearsonCorrelationPairsTradingAlphaModel:
        pass


class RsiAlphaModel(QuantConnect.Algorithm.Framework.Alphas.AlphaModel, QuantConnect.Algorithm.Framework.INotifiedSecurityChanges, QuantConnect.Algorithm.Framework.Alphas.IAlphaModel, QuantConnect.Algorithm.Framework.Alphas.INamedModel):
    """
    Uses Wilder's RSI to create insights. Using default settings, a cross over below 30 or above 70 will
                trigger a new insight.
    
    RsiAlphaModel(period: int, resolution: Resolution)
    """
    def OnSecuritiesChanged(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, changes: QuantConnect.Data.UniverseSelection.SecurityChanges) -> None:
        pass

    def Update(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, data: QuantConnect.Data.Slice) -> typing.List[QuantConnect.Algorithm.Framework.Alphas.Insight]:
        pass

    def __init__(self, period: int, resolution: QuantConnect.Resolution) -> QuantConnect.Algorithm.Framework.Alphas.RsiAlphaModel:
        pass
