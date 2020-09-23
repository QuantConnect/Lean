from .____init___10 import *
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



class RollingWindow(System.object, QuantConnect.Indicators.IReadOnlyWindow[T], System.Collections.IEnumerable, System.Collections.Generic.IEnumerable[T]):
    """ RollingWindow[T](size: int) """
    def Add(self, item: QuantConnect.Indicators.T) -> None:
        pass

    def GetEnumerator(self) -> System.Collections.Generic.IEnumerator[QuantConnect.Indicators.T]:
        pass

    def Reset(self) -> None:
        pass

    def __init__(self, size: int) -> QuantConnect.Indicators.RollingWindow:
        pass

    Count: int

    IsReady: bool

    MostRecentlyRemoved: QuantConnect.Indicators.T

    Samples: float

    Size: int


    Item: indexer#


class SchaffTrendCycle(QuantConnect.Indicators.Indicator, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IndicatorDataPoint]]):
    """
    This indicator creates the Schaff Trend Cycle
    
    SchaffTrendCycle(cyclePeriod: int, fastPeriod: int, slowPeriod: int, type: MovingAverageType)
    SchaffTrendCycle(name: str, cyclePeriod: int, fastPeriod: int, slowPeriod: int, type: MovingAverageType)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, cyclePeriod: int, fastPeriod: int, slowPeriod: int, type: QuantConnect.Indicators.MovingAverageType) -> QuantConnect.Indicators.SchaffTrendCycle:
        pass

    @typing.overload
    def __init__(self, name: str, cyclePeriod: int, fastPeriod: int, slowPeriod: int, type: QuantConnect.Indicators.MovingAverageType) -> QuantConnect.Indicators.SchaffTrendCycle:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.SchaffTrendCycle:
        pass

    IsReady: bool

    WarmUpPeriod: int



class SimpleMovingAverage(QuantConnect.Indicators.WindowIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IndicatorDataPoint]]):
    """
    Represents the traditional simple moving average indicator (SMA)
    
    SimpleMovingAverage(name: str, period: int)
    SimpleMovingAverage(period: int)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str, period: int) -> QuantConnect.Indicators.SimpleMovingAverage:
        pass

    @typing.overload
    def __init__(self, period: int) -> QuantConnect.Indicators.SimpleMovingAverage:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.SimpleMovingAverage:
        pass

    IsReady: bool

    RollingSum: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    WarmUpPeriod: int



class Variance(QuantConnect.Indicators.WindowIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IndicatorDataPoint]]):
    """
    This indicator computes the n-period population variance.
    
    Variance(period: int)
    Variance(name: str, period: int)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, period: int) -> QuantConnect.Indicators.Variance:
        pass

    @typing.overload
    def __init__(self, name: str, period: int) -> QuantConnect.Indicators.Variance:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.Variance:
        pass

    WarmUpPeriod: int



class StandardDeviation(QuantConnect.Indicators.Variance, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IndicatorDataPoint]]):
    """
    This indicator computes the n-period population standard deviation.
    
    StandardDeviation(period: int)
    StandardDeviation(name: str, period: int)
    """
    @typing.overload
    def __init__(self, period: int) -> QuantConnect.Indicators.StandardDeviation:
        pass

    @typing.overload
    def __init__(self, name: str, period: int) -> QuantConnect.Indicators.StandardDeviation:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.StandardDeviation:
        pass


class Stochastic(QuantConnect.Indicators.BarIndicator, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    This indicator computes the Slow Stochastics %K and %D. The Fast Stochastics %K is is computed by 
                (Current Close Price - Lowest Price of given Period) / (Highest Price of given Period - Lowest Price of given Period)
                multiplied by 100. Once the Fast Stochastics %K is calculated the Slow Stochastic %K is calculated by the average/smoothed price of
                of the Fast %K with the given period. The Slow Stochastics %D is then derived from the Slow Stochastics %K with the given period.
    
    Stochastic(name: str, period: int, kPeriod: int, dPeriod: int)
    Stochastic(period: int, kPeriod: int, dPeriod: int)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str, period: int, kPeriod: int, dPeriod: int) -> QuantConnect.Indicators.Stochastic:
        pass

    @typing.overload
    def __init__(self, period: int, kPeriod: int, dPeriod: int) -> QuantConnect.Indicators.Stochastic:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.Stochastic:
        pass

    FastStoch: QuantConnect.Indicators.IndicatorBase[QuantConnect.Data.Market.IBaseDataBar]

    IsReady: bool

    StochD: QuantConnect.Indicators.IndicatorBase[QuantConnect.Data.Market.IBaseDataBar]

    StochK: QuantConnect.Indicators.IndicatorBase[QuantConnect.Data.Market.IBaseDataBar]

    WarmUpPeriod: int



class Sum(QuantConnect.Indicators.WindowIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IndicatorDataPoint]]):
    """
    Represents an indicator capable of tracking the sum for the given period
    
    Sum(name: str, period: int)
    Sum(period: int)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str, period: int) -> QuantConnect.Indicators.Sum:
        pass

    @typing.overload
    def __init__(self, period: int) -> QuantConnect.Indicators.Sum:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.Sum:
        pass

    WarmUpPeriod: int



class SwissArmyKnife(QuantConnect.Indicators.Indicator, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IndicatorDataPoint]]):
    """
    Swiss Army Knife indicator by John Ehlers
    
    SwissArmyKnife(period: int, delta: float, tool: SwissArmyKnifeTool)
    SwissArmyKnife(name: str, period: int, delta: float, tool: SwissArmyKnifeTool)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, period: int, delta: float, tool: QuantConnect.Indicators.SwissArmyKnifeTool) -> QuantConnect.Indicators.SwissArmyKnife:
        pass

    @typing.overload
    def __init__(self, name: str, period: int, delta: float, tool: QuantConnect.Indicators.SwissArmyKnifeTool) -> QuantConnect.Indicators.SwissArmyKnife:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.SwissArmyKnife:
        pass

    IsReady: bool

    WarmUpPeriod: int



class SwissArmyKnifeTool(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    The tools of the Swiss Army Knife. Some of the tools lend well to chaining with the "Of" Method, others may be treated as moving averages
    
    enum SwissArmyKnifeTool, values: BandPass (4), Butter (1), Gauss (0), HighPass (2), TwoPoleHighPass (3)
    """
    value__: int
    BandPass: 'SwissArmyKnifeTool'
    Butter: 'SwissArmyKnifeTool'
    Gauss: 'SwissArmyKnifeTool'
    HighPass: 'SwissArmyKnifeTool'
    TwoPoleHighPass: 'SwissArmyKnifeTool'


class T3MovingAverage(QuantConnect.Indicators.IndicatorBase[IndicatorDataPoint], QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IndicatorDataPoint]]):
    """
    This indicator computes the T3 Moving Average (T3).
                The T3 Moving Average is calculated with the following formula:
                EMA1(x, Period) = EMA(x, Period)
                EMA2(x, Period) = EMA(EMA1(x, Period),Period)
                GD(x, Period, volumeFactor) = (EMA1(x, Period)*(1+volumeFactor)) - (EMA2(x, Period)* volumeFactor)
                T3 = GD(GD(GD(t, Period, volumeFactor), Period, volumeFactor), Period, volumeFactor);
    
    T3MovingAverage(name: str, period: int, volumeFactor: Decimal)
    T3MovingAverage(period: int, volumeFactor: Decimal)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str, period: int, volumeFactor: float) -> QuantConnect.Indicators.T3MovingAverage:
        pass

    @typing.overload
    def __init__(self, period: int, volumeFactor: float) -> QuantConnect.Indicators.T3MovingAverage:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.T3MovingAverage:
        pass

    IsReady: bool

    WarmUpPeriod: int



class TriangularMovingAverage(QuantConnect.Indicators.Indicator, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IndicatorDataPoint]]):
    """
    This indicator computes the Triangular Moving Average (TRIMA). 
                The Triangular Moving Average is calculated with the following formula:
                (1) When the period is even, TRIMA(x,period)=SMA(SMA(x,period/2),(period/2)+1)
                (2) When the period is odd,  TRIMA(x,period)=SMA(SMA(x,(period+1)/2),(period+1)/2)
    
    TriangularMovingAverage(name: str, period: int)
    TriangularMovingAverage(period: int)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str, period: int) -> QuantConnect.Indicators.TriangularMovingAverage:
        pass

    @typing.overload
    def __init__(self, period: int) -> QuantConnect.Indicators.TriangularMovingAverage:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.TriangularMovingAverage:
        pass

    IsReady: bool

    WarmUpPeriod: int
