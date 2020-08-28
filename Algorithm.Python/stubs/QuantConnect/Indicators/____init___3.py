from .____init___4 import *
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



class DonchianChannel(QuantConnect.Indicators.BarIndicator, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    This indicator computes the upper and lower band of the Donchian Channel.
                The upper band is computed by finding the highest high over the given period.
                The lower band is computed by finding the lowest low over the given period.
                The primary output value of the indicator is the mean of the upper and lower band for 
                the given timeframe.
    
    DonchianChannel(period: int)
    DonchianChannel(upperPeriod: int, lowerPeriod: int)
    DonchianChannel(name: str, period: int)
    DonchianChannel(name: str, upperPeriod: int, lowerPeriod: int)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, period: int) -> QuantConnect.Indicators.DonchianChannel:
        pass

    @typing.overload
    def __init__(self, upperPeriod: int, lowerPeriod: int) -> QuantConnect.Indicators.DonchianChannel:
        pass

    @typing.overload
    def __init__(self, name: str, period: int) -> QuantConnect.Indicators.DonchianChannel:
        pass

    @typing.overload
    def __init__(self, name: str, upperPeriod: int, lowerPeriod: int) -> QuantConnect.Indicators.DonchianChannel:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.DonchianChannel:
        pass

    IsReady: bool

    LowerBand: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    UpperBand: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    WarmUpPeriod: int



class DoubleExponentialMovingAverage(QuantConnect.Indicators.IndicatorBase[IndicatorDataPoint], QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IndicatorDataPoint]]):
    """
    This indicator computes the Double Exponential Moving Average (DEMA).
                The Double Exponential Moving Average is calculated with the following formula:
                EMA2 = EMA(EMA(t,period),period)
                DEMA = 2 * EMA(t,period) - EMA2
                The Generalized DEMA (GD) is calculated with the following formula:
                GD = (volumeFactor+1) * EMA(t,period) - volumeFactor * EMA2
    
    DoubleExponentialMovingAverage(name: str, period: int, volumeFactor: Decimal)
    DoubleExponentialMovingAverage(period: int, volumeFactor: Decimal)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str, period: int, volumeFactor: float) -> QuantConnect.Indicators.DoubleExponentialMovingAverage:
        pass

    @typing.overload
    def __init__(self, period: int, volumeFactor: float) -> QuantConnect.Indicators.DoubleExponentialMovingAverage:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.DoubleExponentialMovingAverage:
        pass

    IsReady: bool

    WarmUpPeriod: int



class EaseOfMovementValue(QuantConnect.Indicators.TradeBarIndicator, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[TradeBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[TradeBar]]):
    """
    This indicator computes the n-period Ease of Movement Value using the following:
                MID = (high_1 + low_1)/2 - (high_0 + low_0)/2 
                RATIO = (currentVolume/10000) / (high_1 - low_1)
                EMV = MID/RATIO
                _SMA = n-period of EMV
                Returns _SMA
                Source: https://www.investopedia.com/terms/e/easeofmovement.asp
    
    EaseOfMovementValue(period: int, scale: int)
    EaseOfMovementValue(name: str, period: int, scale: int)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, period: int, scale: int) -> QuantConnect.Indicators.EaseOfMovementValue:
        pass

    @typing.overload
    def __init__(self, name: str, period: int, scale: int) -> QuantConnect.Indicators.EaseOfMovementValue:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.EaseOfMovementValue:
        pass

    IsReady: bool

    WarmUpPeriod: int



class ExponentialMovingAverage(QuantConnect.Indicators.Indicator, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IndicatorDataPoint]]):
    """
    Represents the traditional exponential moving average indicator (EMA)
    
    ExponentialMovingAverage(name: str, period: int)
    ExponentialMovingAverage(name: str, period: int, smoothingFactor: Decimal)
    ExponentialMovingAverage(period: int)
    ExponentialMovingAverage(period: int, smoothingFactor: Decimal)
    """
    @staticmethod
    def SmoothingFactorDefault(period: int) -> float:
        pass

    @typing.overload
    def __init__(self, name: str, period: int) -> QuantConnect.Indicators.ExponentialMovingAverage:
        pass

    @typing.overload
    def __init__(self, name: str, period: int, smoothingFactor: float) -> QuantConnect.Indicators.ExponentialMovingAverage:
        pass

    @typing.overload
    def __init__(self, period: int) -> QuantConnect.Indicators.ExponentialMovingAverage:
        pass

    @typing.overload
    def __init__(self, period: int, smoothingFactor: float) -> QuantConnect.Indicators.ExponentialMovingAverage:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.ExponentialMovingAverage:
        pass

    IsReady: bool

    WarmUpPeriod: int



class FilteredIdentity(QuantConnect.Indicators.IndicatorBase[IBaseData], System.IComparable, QuantConnect.Indicators.IIndicator[IBaseData], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseData]]):
    """
    Represents an indicator that is a ready after ingesting a single sample and
                always returns the same value as it is given if it passes a filter condition
    
    FilteredIdentity(name: str, filter: Func[IBaseData, bool])
    """
    def __init__(self, name: str, filter: typing.Callable[[QuantConnect.Data.IBaseData], bool]) -> QuantConnect.Indicators.FilteredIdentity:
        pass

    IsReady: bool



class FisherTransform(QuantConnect.Indicators.BarIndicator, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    The Fisher transform is a mathematical process which is used to convert any data set to a modified
                 data set whose Probability Distribution Function is approximately Gaussian. Once the Fisher transform
                 is computed, the transformed data can then be analyzed in terms of it's deviation from the mean.
                
                 The equation is y = .5 * ln [ 1 + x / 1 - x ] where
                 x is the input
                 y is the output
                 ln is the natural logarithm
                
                 The Fisher transform has much sharper turning points than other indicators such as MACD
                
                 For more info, read chapter 1 of Cybernetic Analysis for Stocks and Futures by John F. Ehlers
                
                 We are implementing the latest version of this indicator found at Fig. 4 of
                 http://www.mesasoftware.com/papers/UsingTheFisherTransform.pdf
    
    FisherTransform(period: int)
    FisherTransform(name: str, period: int)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, period: int) -> QuantConnect.Indicators.FisherTransform:
        pass

    @typing.overload
    def __init__(self, name: str, period: int) -> QuantConnect.Indicators.FisherTransform:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.FisherTransform:
        pass

    IsReady: bool

    WarmUpPeriod: int



class FractalAdaptiveMovingAverage(QuantConnect.Indicators.BarIndicator, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    The Fractal Adaptive Moving Average (FRAMA) by John Ehlers
    
    FractalAdaptiveMovingAverage(name: str, n: int, longPeriod: int)
    FractalAdaptiveMovingAverage(n: int, longPeriod: int)
    FractalAdaptiveMovingAverage(n: int)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str, n: int, longPeriod: int) -> QuantConnect.Indicators.FractalAdaptiveMovingAverage:
        pass

    @typing.overload
    def __init__(self, n: int, longPeriod: int) -> QuantConnect.Indicators.FractalAdaptiveMovingAverage:
        pass

    @typing.overload
    def __init__(self, n: int) -> QuantConnect.Indicators.FractalAdaptiveMovingAverage:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.FractalAdaptiveMovingAverage:
        pass

    IsReady: bool

    WarmUpPeriod: int



class FunctionalIndicator(QuantConnect.Indicators.IndicatorBase[T], System.IComparable, QuantConnect.Indicators.IIndicator[T], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[T]]):
    """
    FunctionalIndicator[T](name: str, computeNextValue: Func[T, Decimal], isReady: Func[IndicatorBase[T], bool])
    FunctionalIndicator[T](name: str, computeNextValue: Func[T, Decimal], isReady: Func[IndicatorBase[T], bool], reset: Action)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str, computeNextValue: typing.Callable[[QuantConnect.Indicators.T], float], isReady: typing.Callable[[QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.T]], bool]) -> QuantConnect.Indicators.FunctionalIndicator:
        pass

    @typing.overload
    def __init__(self, name: str, computeNextValue: typing.Callable[[QuantConnect.Indicators.T], float], isReady: typing.Callable[[QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.T]], bool], reset: System.Action) -> QuantConnect.Indicators.FunctionalIndicator:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.FunctionalIndicator:
        pass

    IsReady: bool
