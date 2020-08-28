from .____init___1 import *
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

# functions

def IIndicator(*args, **kwargs): # real signature unknown
    """
    Represents an indicator that can receive data updates and emit events when the value of
                the indicator has changed.
    """
    pass

# classes

class Indicator(QuantConnect.Indicators.IndicatorBase[IndicatorDataPoint], System.IComparable, QuantConnect.Indicators.IIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IndicatorDataPoint]]):
    """
    Represents a type capable of ingesting a piece of data and producing a new piece of data.
                Indicators can be used to filter and transform data into a new, more informative form.
    """
    def __init__(self, *args): #cannot find CLR constructor
        pass


class MovingAverageConvergenceDivergence(QuantConnect.Indicators.Indicator, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IndicatorDataPoint]]):
    """
    This indicator creates two moving averages defined on a base indicator and produces the difference
                between the fast and slow averages.
    
    MovingAverageConvergenceDivergence(fastPeriod: int, slowPeriod: int, signalPeriod: int, type: MovingAverageType)
    MovingAverageConvergenceDivergence(name: str, fastPeriod: int, slowPeriod: int, signalPeriod: int, type: MovingAverageType)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, fastPeriod: int, slowPeriod: int, signalPeriod: int, type: QuantConnect.Indicators.MovingAverageType) -> QuantConnect.Indicators.MovingAverageConvergenceDivergence:
        pass

    @typing.overload
    def __init__(self, name: str, fastPeriod: int, slowPeriod: int, signalPeriod: int, type: QuantConnect.Indicators.MovingAverageType) -> QuantConnect.Indicators.MovingAverageConvergenceDivergence:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.MovingAverageConvergenceDivergence:
        pass

    Fast: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    Histogram: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    IsReady: bool

    Signal: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    Slow: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    WarmUpPeriod: int



class AbsolutePriceOscillator(QuantConnect.Indicators.MovingAverageConvergenceDivergence, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IndicatorDataPoint]]):
    """
    This indicator computes the Absolute Price Oscillator (APO)
                The Absolute Price Oscillator is calculated using the following formula:
                APO[i] = FastMA[i] - SlowMA[i]
    
    AbsolutePriceOscillator(name: str, fastPeriod: int, slowPeriod: int, movingAverageType: MovingAverageType)
    AbsolutePriceOscillator(fastPeriod: int, slowPeriod: int, movingAverageType: MovingAverageType)
    """
    @typing.overload
    def __init__(self, name: str, fastPeriod: int, slowPeriod: int, movingAverageType: QuantConnect.Indicators.MovingAverageType) -> QuantConnect.Indicators.AbsolutePriceOscillator:
        pass

    @typing.overload
    def __init__(self, fastPeriod: int, slowPeriod: int, movingAverageType: QuantConnect.Indicators.MovingAverageType) -> QuantConnect.Indicators.AbsolutePriceOscillator:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.AbsolutePriceOscillator:
        pass


class AccelerationBands(QuantConnect.Indicators.IndicatorBase[IBaseDataBar], QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    The Acceleration Bands created by Price Headley plots upper and lower envelope bands around a moving average.
    
    AccelerationBands(name: str, period: int, width: Decimal, movingAverageType: MovingAverageType)
    AccelerationBands(period: int, width: Decimal)
    AccelerationBands(period: int)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str, period: int, width: float, movingAverageType: QuantConnect.Indicators.MovingAverageType) -> QuantConnect.Indicators.AccelerationBands:
        pass

    @typing.overload
    def __init__(self, period: int, width: float) -> QuantConnect.Indicators.AccelerationBands:
        pass

    @typing.overload
    def __init__(self, period: int) -> QuantConnect.Indicators.AccelerationBands:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.AccelerationBands:
        pass

    IsReady: bool

    LowerBand: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    MiddleBand: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    MovingAverageType: QuantConnect.Indicators.MovingAverageType

    UpperBand: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    WarmUpPeriod: int



class TradeBarIndicator(QuantConnect.Indicators.IndicatorBase[TradeBar], System.IComparable, QuantConnect.Indicators.IIndicator[TradeBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[TradeBar]]):
    """
    The TradeBarIndicator is an indicator that accepts TradeBar data as its input.
                
                This type is more of a shim/typedef to reduce the need to refer to things as IndicatorBase<TradeBar>
    """
    def __init__(self, *args): #cannot find CLR constructor
        pass


class AccumulationDistribution(QuantConnect.Indicators.TradeBarIndicator, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[TradeBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[TradeBar]]):
    """
    This indicator computes the Accumulation/Distribution (AD)
                The Accumulation/Distribution is calculated using the following formula:
                AD = AD + ((Close - Low) - (High - Close)) / (High - Low) * Volume
    
    AccumulationDistribution()
    AccumulationDistribution(name: str)
    """
    @typing.overload
    def __init__(self) -> QuantConnect.Indicators.AccumulationDistribution:
        pass

    @typing.overload
    def __init__(self, name: str) -> QuantConnect.Indicators.AccumulationDistribution:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.AccumulationDistribution:
        pass

    IsReady: bool

    WarmUpPeriod: int



class AccumulationDistributionOscillator(QuantConnect.Indicators.TradeBarIndicator, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[TradeBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[TradeBar]]):
    """
    This indicator computes the Accumulation/Distribution Oscillator (ADOSC)
                The Accumulation/Distribution Oscillator is calculated using the following formula:
                ADOSC = EMA(fast,AD) - EMA(slow,AD)
    
    AccumulationDistributionOscillator(fastPeriod: int, slowPeriod: int)
    AccumulationDistributionOscillator(name: str, fastPeriod: int, slowPeriod: int)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, fastPeriod: int, slowPeriod: int) -> QuantConnect.Indicators.AccumulationDistributionOscillator:
        pass

    @typing.overload
    def __init__(self, name: str, fastPeriod: int, slowPeriod: int) -> QuantConnect.Indicators.AccumulationDistributionOscillator:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.AccumulationDistributionOscillator:
        pass

    IsReady: bool

    WarmUpPeriod: int



class AdvanceDeclineIndicator(QuantConnect.Indicators.TradeBarIndicator, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[TradeBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[TradeBar]]):
    """
    The advance-decline indicator compares the number of stocks 
                that closed higher against the number of stocks 
                that closed lower than their previous day's closing prices.
    
    AdvanceDeclineIndicator(name: str, compute: Func[IEnumerable[TradeBar], Decimal])
    """
    def AddStock(self, symbol: QuantConnect.Symbol) -> None:
        pass

    def RemoveStock(self, symbol: QuantConnect.Symbol) -> None:
        pass

    def Reset(self) -> None:
        pass

    def __init__(self, name: str, compute: typing.Callable[[typing.List[QuantConnect.Data.Market.TradeBar]], float]) -> QuantConnect.Indicators.AdvanceDeclineIndicator:
        pass

    IsReady: bool

    WarmUpPeriod: int



class AdvanceDeclineRatio(QuantConnect.Indicators.AdvanceDeclineIndicator, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[TradeBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[TradeBar]]):
    """
    The advance-decline ratio (ADR) compares the number of stocks 
                that closed higher against the number of stocks 
                that closed lower than their previous day's closing prices.
    
    AdvanceDeclineRatio(name: str)
    """
    def __init__(self, name: str) -> QuantConnect.Indicators.AdvanceDeclineRatio:
        pass


class AdvanceDeclineVolumeRatio(QuantConnect.Indicators.AdvanceDeclineIndicator, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[TradeBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[TradeBar]]):
    """
    The Advance Decline Volume Ratio is a Breadth indicator calculated as ratio of 
                summary volume of advancing stocks to summary volume of declining stocks. 
                AD Volume Ratio is used in technical analysis to see where the main trading activity is focused.
    
    AdvanceDeclineVolumeRatio(name: str)
    """
    def __init__(self, name: str) -> QuantConnect.Indicators.AdvanceDeclineVolumeRatio:
        pass
