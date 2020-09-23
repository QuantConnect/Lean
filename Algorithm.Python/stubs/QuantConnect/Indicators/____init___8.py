from .____init___9 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Indicators
import QuantConnect.Data.Market
import QuantConnect.Data
import QuantConnect
import Python.Runtime
import datetime


class MovingAverageTypeExtensions(System.object):
    """ Provides extension methods for the MovingAverageType enumeration """
    @staticmethod
    @typing.overload
    def AsIndicator(movingAverageType: QuantConnect.Indicators.MovingAverageType, period: int) -> QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]:
        pass

    @staticmethod
    @typing.overload
    def AsIndicator(movingAverageType: QuantConnect.Indicators.MovingAverageType, name: str, period: int) -> QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]:
        pass

    def AsIndicator(self, *args) -> QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]:
        pass

    __all__: list


class NormalizedAverageTrueRange(QuantConnect.Indicators.BarIndicator, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    This indicator computes the Normalized Average True Range (NATR).
                The Normalized Average True Range is calculated with the following formula:
                NATR = (ATR(period) / Close) * 100
    
    NormalizedAverageTrueRange(name: str, period: int)
    NormalizedAverageTrueRange(period: int)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str, period: int) -> QuantConnect.Indicators.NormalizedAverageTrueRange:
        pass

    @typing.overload
    def __init__(self, period: int) -> QuantConnect.Indicators.NormalizedAverageTrueRange:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.NormalizedAverageTrueRange:
        pass

    IsReady: bool

    WarmUpPeriod: int



class OnBalanceVolume(QuantConnect.Indicators.TradeBarIndicator, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[TradeBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[TradeBar]]):
    """
    This indicator computes the On Balance Volume (OBV). 
                The On Balance Volume is calculated by determining the price of the current close price and previous close price.
                If the current close price is equivalent to the previous price the OBV remains the same,
                If the current close price is higher the volume of that day is added to the OBV, while a lower close price will
                result in negative value.
    
    OnBalanceVolume()
    OnBalanceVolume(name: str)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Indicators.OnBalanceVolume:
        pass

    @typing.overload
    def __init__(self, name: str) -> QuantConnect.Indicators.OnBalanceVolume:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.OnBalanceVolume:
        pass

    IsReady: bool

    WarmUpPeriod: int



class ParabolicStopAndReverse(QuantConnect.Indicators.BarIndicator, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    Parabolic SAR Indicator 
                Based on TA-Lib implementation
    
    ParabolicStopAndReverse(name: str, afStart: Decimal, afIncrement: Decimal, afMax: Decimal)
    ParabolicStopAndReverse(afStart: Decimal, afIncrement: Decimal, afMax: Decimal)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str, afStart: float, afIncrement: float, afMax: float) -> QuantConnect.Indicators.ParabolicStopAndReverse:
        pass

    @typing.overload
    def __init__(self, afStart: float, afIncrement: float, afMax: float) -> QuantConnect.Indicators.ParabolicStopAndReverse:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.ParabolicStopAndReverse:
        pass

    IsReady: bool

    WarmUpPeriod: int



class PercentagePriceOscillator(QuantConnect.Indicators.AbsolutePriceOscillator, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IndicatorDataPoint]]):
    """
    This indicator computes the Percentage Price Oscillator (PPO)
                The Percentage Price Oscillator is calculated using the following formula:
                PPO[i] = 100 * (FastMA[i] - SlowMA[i]) / SlowMA[i]
    
    PercentagePriceOscillator(name: str, fastPeriod: int, slowPeriod: int, movingAverageType: MovingAverageType)
    PercentagePriceOscillator(fastPeriod: int, slowPeriod: int, movingAverageType: MovingAverageType)
    """
    @typing.overload
    def __init__(self, name: str, fastPeriod: int, slowPeriod: int, movingAverageType: QuantConnect.Indicators.MovingAverageType) -> QuantConnect.Indicators.PercentagePriceOscillator:
        pass

    @typing.overload
    def __init__(self, fastPeriod: int, slowPeriod: int, movingAverageType: QuantConnect.Indicators.MovingAverageType) -> QuantConnect.Indicators.PercentagePriceOscillator:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.PercentagePriceOscillator:
        pass


class PythonIndicator(QuantConnect.Indicators.IndicatorBase[IBaseData], System.IComparable, QuantConnect.Indicators.IIndicator[IBaseData], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseData]]):
    """
    Provides a wrapper for QuantConnect.Indicators.IndicatorBase implementations written in python
    
    PythonIndicator()
    PythonIndicator(*args: Array[PyObject])
    PythonIndicator(indicator: PyObject)
    """
    def SetIndicator(self, indicator: Python.Runtime.PyObject) -> None:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Indicators.PythonIndicator:
        pass

    @typing.overload
    def __init__(self, args: typing.List[Python.Runtime.PyObject]) -> QuantConnect.Indicators.PythonIndicator:
        pass

    @typing.overload
    def __init__(self, indicator: Python.Runtime.PyObject) -> QuantConnect.Indicators.PythonIndicator:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.PythonIndicator:
        pass

    IsReady: bool



class RateOfChangeRatio(QuantConnect.Indicators.RateOfChange, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IndicatorDataPoint]]):
    """
    This indicator computes the Rate Of Change Ratio (ROCR). 
                The Rate Of Change Ratio is calculated with the following formula:
                ROCR = price / prevPrice
    
    RateOfChangeRatio(name: str, period: int)
    RateOfChangeRatio(period: int)
    """
    @typing.overload
    def __init__(self, name: str, period: int) -> QuantConnect.Indicators.RateOfChangeRatio:
        pass

    @typing.overload
    def __init__(self, period: int) -> QuantConnect.Indicators.RateOfChangeRatio:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.RateOfChangeRatio:
        pass


class RegressionChannel(QuantConnect.Indicators.Indicator, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IndicatorDataPoint]]):
    """
    The Regression Channel indicator extends the QuantConnect.Indicators.LeastSquaresMovingAverage
                with the inclusion of two (upper and lower) channel lines that are distanced from
                the linear regression line by a user defined number of standard deviations.
                Reference: http://www.onlinetradingconcepts.com/TechnicalAnalysis/LinRegChannel.html
    
    RegressionChannel(name: str, period: int, k: Decimal)
    RegressionChannel(period: int, k: Decimal)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str, period: int, k: float) -> QuantConnect.Indicators.RegressionChannel:
        pass

    @typing.overload
    def __init__(self, period: int, k: float) -> QuantConnect.Indicators.RegressionChannel:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.RegressionChannel:
        pass

    Intercept: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    IsReady: bool

    LinearRegression: QuantConnect.Indicators.LeastSquaresMovingAverage

    LowerChannel: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    Slope: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    UpperChannel: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    WarmUpPeriod: int



class RelativeStrengthIndex(QuantConnect.Indicators.Indicator, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IndicatorDataPoint]]):
    """
    Represents the  Relative Strength Index (RSI) developed by K. Welles Wilder.
                You can optionally specified a different moving average type to be used in the computation
    
    RelativeStrengthIndex(period: int, movingAverageType: MovingAverageType)
    RelativeStrengthIndex(name: str, period: int, movingAverageType: MovingAverageType)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, period: int, movingAverageType: QuantConnect.Indicators.MovingAverageType) -> QuantConnect.Indicators.RelativeStrengthIndex:
        pass

    @typing.overload
    def __init__(self, name: str, period: int, movingAverageType: QuantConnect.Indicators.MovingAverageType) -> QuantConnect.Indicators.RelativeStrengthIndex:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.RelativeStrengthIndex:
        pass

    AverageGain: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    AverageLoss: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    IsReady: bool

    MovingAverageType: QuantConnect.Indicators.MovingAverageType

    WarmUpPeriod: int
