from .____init___11 import *
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



class TripleExponentialMovingAverage(QuantConnect.Indicators.IndicatorBase[IndicatorDataPoint], QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IndicatorDataPoint]]):
    """
    This indicator computes the Triple Exponential Moving Average (TEMA). 
                The Triple Exponential Moving Average is calculated with the following formula:
                EMA1 = EMA(t,period)
                EMA2 = EMA(EMA(t,period),period)
                EMA3 = EMA(EMA(EMA(t,period),period),period)
                TEMA = 3 * EMA1 - 3 * EMA2 + EMA3
    
    TripleExponentialMovingAverage(name: str, period: int)
    TripleExponentialMovingAverage(period: int)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str, period: int) -> QuantConnect.Indicators.TripleExponentialMovingAverage:
        pass

    @typing.overload
    def __init__(self, period: int) -> QuantConnect.Indicators.TripleExponentialMovingAverage:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.TripleExponentialMovingAverage:
        pass

    IsReady: bool

    WarmUpPeriod: int



class Trix(QuantConnect.Indicators.Indicator, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IndicatorDataPoint]]):
    """
    This indicator computes the TRIX (1-period ROC of a Triple EMA)
                The TRIX is calculated as explained here:
                http://stockcharts.com/school/doku.php?id=chart_school:technical_indicators:trix
    
    Trix(name: str, period: int)
    Trix(period: int)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str, period: int) -> QuantConnect.Indicators.Trix:
        pass

    @typing.overload
    def __init__(self, period: int) -> QuantConnect.Indicators.Trix:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.Trix:
        pass

    IsReady: bool

    WarmUpPeriod: int



class TrueRange(QuantConnect.Indicators.BarIndicator, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    This indicator computes the True Range (TR).
                The True Range is the greatest of the following values:
                value1 = distance from today's high to today's low.
                value2 = distance from yesterday's close to today's high.
                value3 = distance from yesterday's close to today's low.
    
    TrueRange()
    TrueRange(name: str)
    """
    @typing.overload
    def __init__(self) -> QuantConnect.Indicators.TrueRange:
        pass

    @typing.overload
    def __init__(self, name: str) -> QuantConnect.Indicators.TrueRange:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.TrueRange:
        pass

    IsReady: bool

    WarmUpPeriod: int



class UltimateOscillator(QuantConnect.Indicators.BarIndicator, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    This indicator computes the Ultimate Oscillator (ULTOSC)
                The Ultimate Oscillator is calculated as explained here:
                http://stockcharts.com/school/doku.php?id=chart_school:technical_indicators:ultimate_oscillator
    
    UltimateOscillator(period1: int, period2: int, period3: int)
    UltimateOscillator(name: str, period1: int, period2: int, period3: int)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, period1: int, period2: int, period3: int) -> QuantConnect.Indicators.UltimateOscillator:
        pass

    @typing.overload
    def __init__(self, name: str, period1: int, period2: int, period3: int) -> QuantConnect.Indicators.UltimateOscillator:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.UltimateOscillator:
        pass

    IsReady: bool

    WarmUpPeriod: int



class VolumeWeightedAveragePriceIndicator(QuantConnect.Indicators.TradeBarIndicator, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[TradeBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[TradeBar]]):
    """
    Volume Weighted Average Price (VWAP) Indicator:
                It is calculated by adding up the dollars traded for every transaction (price multiplied
                by number of shares traded) and then dividing by the total shares traded for the day.
    
    VolumeWeightedAveragePriceIndicator(period: int)
    VolumeWeightedAveragePriceIndicator(name: str, period: int)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, period: int) -> QuantConnect.Indicators.VolumeWeightedAveragePriceIndicator:
        pass

    @typing.overload
    def __init__(self, name: str, period: int) -> QuantConnect.Indicators.VolumeWeightedAveragePriceIndicator:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.VolumeWeightedAveragePriceIndicator:
        pass

    IsReady: bool

    WarmUpPeriod: int



class WilderMovingAverage(QuantConnect.Indicators.Indicator, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IndicatorDataPoint]]):
    """
    Represents the moving average indicator defined by Welles Wilder in his book:
                New Concepts in Technical Trading Systems.
    
    WilderMovingAverage(name: str, period: int)
    WilderMovingAverage(period: int)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str, period: int) -> QuantConnect.Indicators.WilderMovingAverage:
        pass

    @typing.overload
    def __init__(self, period: int) -> QuantConnect.Indicators.WilderMovingAverage:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.WilderMovingAverage:
        pass

    IsReady: bool

    WarmUpPeriod: int



class WilliamsPercentR(QuantConnect.Indicators.BarIndicator, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    Williams %R, or just %R, is the current closing price in relation to the high and low of
                the past N days (for a given N). The value of this indicator fluctuates between -100 and 0.
                The symbol is said to be oversold when the oscillator is below -80%,
                and overbought when the oscillator is above -20%.
    
    WilliamsPercentR(period: int)
    WilliamsPercentR(name: str, period: int)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, period: int) -> QuantConnect.Indicators.WilliamsPercentR:
        pass

    @typing.overload
    def __init__(self, name: str, period: int) -> QuantConnect.Indicators.WilliamsPercentR:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.WilliamsPercentR:
        pass

    IsReady: bool

    Maximum: QuantConnect.Indicators.Maximum

    Minimum: QuantConnect.Indicators.Minimum

    WarmUpPeriod: int



class WindowIdentity(QuantConnect.Indicators.WindowIndicator[IndicatorDataPoint], System.IComparable, QuantConnect.Indicators.IIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IndicatorDataPoint]]):
    """
    Represents an indicator that is a ready after ingesting enough samples (# samples > period) 
                and always returns the same value as it is given.
    
    WindowIdentity(name: str, period: int)
    WindowIdentity(period: int)
    """
    @typing.overload
    def __init__(self, name: str, period: int) -> QuantConnect.Indicators.WindowIdentity:
        pass

    @typing.overload
    def __init__(self, period: int) -> QuantConnect.Indicators.WindowIdentity:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.WindowIdentity:
        pass

    IsReady: bool



class WindowIndicator(QuantConnect.Indicators.IndicatorBase[T], System.IComparable, QuantConnect.Indicators.IIndicator[T], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[T]]):
    # no doc
    def Reset(self) -> None:
        pass

    def __init__(self, *args): #cannot find CLR constructor
        pass

    IsReady: bool

    Period: int

# classes

class IIndicatorWarmUpPeriodProvider:
    """ Represents an indicator with a warm up period provider. """
    WarmUpPeriod: int



class IndicatorDataPoint(QuantConnect.Data.BaseData, System.IEquatable[IndicatorDataPoint], QuantConnect.Data.IBaseData, System.IComparable, System.IComparable[IndicatorDataPoint]):
    """
    Represents a piece of data at a specific time
    
    IndicatorDataPoint()
    IndicatorDataPoint(time: DateTime, value: Decimal)
    IndicatorDataPoint(symbol: Symbol, time: DateTime, value: Decimal)
    """
    @typing.overload
    def CompareTo(self, other: QuantConnect.Indicators.IndicatorDataPoint) -> int:
        pass

    @typing.overload
    def CompareTo(self, obj: object) -> int:
        pass

    def CompareTo(self, *args) -> int:
        pass

    @typing.overload
    def Equals(self, other: QuantConnect.Indicators.IndicatorDataPoint) -> bool:
        pass

    @typing.overload
    def Equals(self, obj: object) -> bool:
        pass

    def Equals(self, *args) -> bool:
        pass

    def GetHashCode(self) -> int:
        pass

    @typing.overload
    def GetSource(self, config: QuantConnect.Data.SubscriptionDataConfig, date: datetime.datetime, isLiveMode: bool) -> QuantConnect.Data.SubscriptionDataSource:
        pass

    @typing.overload
    def GetSource(self, config: QuantConnect.Data.SubscriptionDataConfig, date: datetime.datetime, datafeed: QuantConnect.DataFeedEndpoint) -> str:
        pass

    def GetSource(self, *args) -> str:
        pass

    @typing.overload
    def Reader(self, config: QuantConnect.Data.SubscriptionDataConfig, line: str, date: datetime.datetime, isLiveMode: bool) -> QuantConnect.Data.BaseData:
        pass

    @typing.overload
    def Reader(self, config: QuantConnect.Data.SubscriptionDataConfig, stream: System.IO.StreamReader, date: datetime.datetime, isLiveMode: bool) -> QuantConnect.Data.BaseData:
        pass

    @typing.overload
    def Reader(self, config: QuantConnect.Data.SubscriptionDataConfig, line: str, date: datetime.datetime, datafeed: QuantConnect.DataFeedEndpoint) -> QuantConnect.Data.BaseData:
        pass

    def Reader(self, *args) -> QuantConnect.Data.BaseData:
        pass

    def ToString(self) -> str:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Indicators.IndicatorDataPoint:
        pass

    @typing.overload
    def __init__(self, time: datetime.datetime, value: float) -> QuantConnect.Indicators.IndicatorDataPoint:
        pass

    @typing.overload
    def __init__(self, symbol: QuantConnect.Symbol, time: datetime.datetime, value: float) -> QuantConnect.Indicators.IndicatorDataPoint:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.IndicatorDataPoint:
        pass
