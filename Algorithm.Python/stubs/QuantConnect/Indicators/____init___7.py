from .____init___8 import *
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



class MidPoint(QuantConnect.Indicators.IndicatorBase[IndicatorDataPoint], QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IndicatorDataPoint]]):
    """
    This indicator computes the MidPoint (MIDPOINT)
                The MidPoint is calculated using the following formula:
                MIDPOINT = (Highest Value + Lowest Value) / 2
    
    MidPoint(name: str, period: int)
    MidPoint(period: int)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str, period: int) -> QuantConnect.Indicators.MidPoint:
        pass

    @typing.overload
    def __init__(self, period: int) -> QuantConnect.Indicators.MidPoint:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.MidPoint:
        pass

    IsReady: bool

    WarmUpPeriod: int



class MidPrice(QuantConnect.Indicators.BarIndicator, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    This indicator computes the MidPrice (MIDPRICE).
                The MidPrice is calculated using the following formula:
                MIDPRICE = (Highest High + Lowest Low) / 2
    
    MidPrice(name: str, period: int)
    MidPrice(period: int)
    """
    @typing.overload
    def __init__(self, name: str, period: int) -> QuantConnect.Indicators.MidPrice:
        pass

    @typing.overload
    def __init__(self, period: int) -> QuantConnect.Indicators.MidPrice:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.MidPrice:
        pass

    IsReady: bool

    WarmUpPeriod: int



class Minimum(QuantConnect.Indicators.WindowIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IndicatorDataPoint]]):
    """
    Represents an indicator capable of tracking the minimum value and how many periods ago it occurred
    
    Minimum(period: int)
    Minimum(name: str, period: int)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, period: int) -> QuantConnect.Indicators.Minimum:
        pass

    @typing.overload
    def __init__(self, name: str, period: int) -> QuantConnect.Indicators.Minimum:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.Minimum:
        pass

    IsReady: bool

    PeriodsSinceMinimum: int

    WarmUpPeriod: int



class Momentum(QuantConnect.Indicators.WindowIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IndicatorDataPoint]]):
    """
    This indicator computes the n-period change in a value using the following:
                value_0 - value_n
    
    Momentum(period: int)
    Momentum(name: str, period: int)
    """
    @typing.overload
    def __init__(self, period: int) -> QuantConnect.Indicators.Momentum:
        pass

    @typing.overload
    def __init__(self, name: str, period: int) -> QuantConnect.Indicators.Momentum:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.Momentum:
        pass

    WarmUpPeriod: int



class RateOfChange(QuantConnect.Indicators.WindowIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IndicatorDataPoint]]):
    """
    This indicator computes the n-period rate of change in a value using the following:
                (value_0 - value_n) / value_n
    
    RateOfChange(period: int)
    RateOfChange(name: str, period: int)
    """
    @typing.overload
    def __init__(self, period: int) -> QuantConnect.Indicators.RateOfChange:
        pass

    @typing.overload
    def __init__(self, name: str, period: int) -> QuantConnect.Indicators.RateOfChange:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.RateOfChange:
        pass

    WarmUpPeriod: int



class RateOfChangePercent(QuantConnect.Indicators.RateOfChange, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IndicatorDataPoint]]):
    """
    This indicator computes the n-period percentage rate of change in a value using the following:
                100 * (value_0 - value_n) / value_n
    
    RateOfChangePercent(period: int)
    RateOfChangePercent(name: str, period: int)
    """
    @typing.overload
    def __init__(self, period: int) -> QuantConnect.Indicators.RateOfChangePercent:
        pass

    @typing.overload
    def __init__(self, name: str, period: int) -> QuantConnect.Indicators.RateOfChangePercent:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.RateOfChangePercent:
        pass


class MomentumPercent(QuantConnect.Indicators.RateOfChangePercent, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IndicatorDataPoint]]):
    """
    This indicator computes the n-period percentage rate of change in a value using the following:
                 100 * (value_0 - value_n) / value_n
                
                 This indicator yields the same results of RateOfChangePercent
    
    MomentumPercent(period: int)
    MomentumPercent(name: str, period: int)
    """
    @typing.overload
    def __init__(self, period: int) -> QuantConnect.Indicators.MomentumPercent:
        pass

    @typing.overload
    def __init__(self, name: str, period: int) -> QuantConnect.Indicators.MomentumPercent:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.MomentumPercent:
        pass


class MomersionIndicator(QuantConnect.Indicators.WindowIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IndicatorDataPoint]]):
    """
    Oscillator indicator that measures momentum and mean-reversion over a specified
                period n.
                Source: Harris, Michael. "Momersion Indicator." Price Action Lab.,
                            13 Aug. 2015. Web. http://www.priceactionlab.com/Blog/2015/08/momersion-indicator/.
    
    MomersionIndicator(name: str, minPeriod: Nullable[int], fullPeriod: int)
    MomersionIndicator(minPeriod: Nullable[int], fullPeriod: int)
    MomersionIndicator(fullPeriod: int)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str, minPeriod: typing.Optional[int], fullPeriod: int) -> QuantConnect.Indicators.MomersionIndicator:
        pass

    @typing.overload
    def __init__(self, minPeriod: typing.Optional[int], fullPeriod: int) -> QuantConnect.Indicators.MomersionIndicator:
        pass

    @typing.overload
    def __init__(self, fullPeriod: int) -> QuantConnect.Indicators.MomersionIndicator:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.MomersionIndicator:
        pass

    IsReady: bool

    WarmUpPeriod: int



class MoneyFlowIndex(QuantConnect.Indicators.TradeBarIndicator, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[TradeBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[TradeBar]]):
    """
    The Money Flow Index (MFI) is an oscillator that uses both price and volume to
                 measure buying and selling pressure
                
                 Typical Price = (High + Low + Close)/3
                 Money Flow = Typical Price x Volume
                 Positive Money Flow = Sum of the money flows of all days where the typical
                     price is greater than the previous day's typical price
                 Negative Money Flow = Sum of the money flows of all days where the typical
                     price is less than the previous day's typical price
                 Money Flow Ratio = (14-period Positive Money Flow)/(14-period Negative Money Flow)
                
                 Money Flow Index = 100 x  Positive Money Flow / ( Positive Money Flow + Negative Money Flow)
    
    MoneyFlowIndex(period: int)
    MoneyFlowIndex(name: str, period: int)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, period: int) -> QuantConnect.Indicators.MoneyFlowIndex:
        pass

    @typing.overload
    def __init__(self, name: str, period: int) -> QuantConnect.Indicators.MoneyFlowIndex:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.MoneyFlowIndex:
        pass

    IsReady: bool

    NegativeMoneyFlow: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    PositiveMoneyFlow: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    PreviousTypicalPrice: float

    WarmUpPeriod: int



class MovingAverageType(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Defines the different types of moving averages
    
    enum MovingAverageType, values: Alma (10), DoubleExponential (4), Exponential (1), Hull (9), Kama (8), LinearWeightedMovingAverage (3), Simple (0), T3 (7), Triangular (6), TripleExponential (5), Wilders (2)
    """
    value__: int
    Alma: 'MovingAverageType'
    DoubleExponential: 'MovingAverageType'
    Exponential: 'MovingAverageType'
    Hull: 'MovingAverageType'
    Kama: 'MovingAverageType'
    LinearWeightedMovingAverage: 'MovingAverageType'
    Simple: 'MovingAverageType'
    T3: 'MovingAverageType'
    Triangular: 'MovingAverageType'
    TripleExponential: 'MovingAverageType'
    Wilders: 'MovingAverageType'
