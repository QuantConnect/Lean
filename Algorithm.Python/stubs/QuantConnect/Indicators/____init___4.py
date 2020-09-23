from .____init___5 import *
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



class HeikinAshi(QuantConnect.Indicators.BarIndicator, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    This indicator computes the Heikin-Ashi bar (HA)
                The Heikin-Ashi bar is calculated using the following formulas:
                HA_Close[0] = (Open[0] + High[0] + Low[0] + Close[0]) / 4
                HA_Open[0] = (HA_Open[1] + HA_Close[1]) / 2
                HA_High[0] = MAX(High[0], HA_Open[0], HA_Close[0])
                HA_Low[0] = MIN(Low[0], HA_Open[0], HA_Close[0])
    
    HeikinAshi(name: str)
    HeikinAshi()
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str) -> QuantConnect.Indicators.HeikinAshi:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Indicators.HeikinAshi:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.HeikinAshi:
        pass

    Close: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    High: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    IsReady: bool

    Low: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    Open: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    Volume: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    WarmUpPeriod: int



class HullMovingAverage(QuantConnect.Indicators.IndicatorBase[IndicatorDataPoint], QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IndicatorDataPoint]]):
    """
    Produces a Hull Moving Average as explained at http://www.alanhull.com/hull-moving-average/
                and derived from the instructions for the Excel VBA code at http://finance4traders.blogspot.com/2009/06/how-to-calculate-hull-moving-average.html
    
    HullMovingAverage(name: str, period: int)
    HullMovingAverage(period: int)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str, period: int) -> QuantConnect.Indicators.HullMovingAverage:
        pass

    @typing.overload
    def __init__(self, period: int) -> QuantConnect.Indicators.HullMovingAverage:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.HullMovingAverage:
        pass

    IsReady: bool

    WarmUpPeriod: int



class IchimokuKinkoHyo(QuantConnect.Indicators.BarIndicator, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    This indicator computes the Ichimoku Kinko Hyo indicator. It consists of the following main indicators:
                Tenkan-sen: (Highest High + Lowest Low) / 2 for the specific period (normally 9)
                Kijun-sen: (Highest High + Lowest Low) / 2 for the specific period (normally 26)
                Senkou A Span: (Tenkan-sen + Kijun-sen )/ 2 from a specific number of periods ago (normally 26)
                Senkou B Span: (Highest High + Lowest Low) / 2 for the specific period (normally 52), from a specific number of periods ago (normally 26)
    
    IchimokuKinkoHyo(tenkanPeriod: int, kijunPeriod: int, senkouAPeriod: int, senkouBPeriod: int, senkouADelayPeriod: int, senkouBDelayPeriod: int)
    IchimokuKinkoHyo(name: str, tenkanPeriod: int, kijunPeriod: int, senkouAPeriod: int, senkouBPeriod: int, senkouADelayPeriod: int, senkouBDelayPeriod: int)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, tenkanPeriod: int, kijunPeriod: int, senkouAPeriod: int, senkouBPeriod: int, senkouADelayPeriod: int, senkouBDelayPeriod: int) -> QuantConnect.Indicators.IchimokuKinkoHyo:
        pass

    @typing.overload
    def __init__(self, name: str, tenkanPeriod: int, kijunPeriod: int, senkouAPeriod: int, senkouBPeriod: int, senkouADelayPeriod: int, senkouBDelayPeriod: int) -> QuantConnect.Indicators.IchimokuKinkoHyo:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.IchimokuKinkoHyo:
        pass

    Chikou: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    DelayedKijunSenkouA: QuantConnect.Indicators.WindowIndicator[QuantConnect.Indicators.IndicatorDataPoint]

    DelayedMaximumSenkouB: QuantConnect.Indicators.WindowIndicator[QuantConnect.Indicators.IndicatorDataPoint]

    DelayedMinimumSenkouB: QuantConnect.Indicators.WindowIndicator[QuantConnect.Indicators.IndicatorDataPoint]

    DelayedTenkanSenkouA: QuantConnect.Indicators.WindowIndicator[QuantConnect.Indicators.IndicatorDataPoint]

    IsReady: bool

    Kijun: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    KijunMaximum: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    KijunMinimum: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    SenkouA: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    SenkouB: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    SenkouBMaximum: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    SenkouBMinimum: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    Tenkan: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    TenkanMaximum: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    TenkanMinimum: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    WarmUpPeriod: int



class Identity(QuantConnect.Indicators.Indicator, System.IComparable, QuantConnect.Indicators.IIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IndicatorDataPoint]]):
    """
    Represents an indicator that is a ready after ingesting a single sample and
                    always returns the same value as it is given.
    
    Identity(name: str)
    """
    def __init__(self, name: str) -> QuantConnect.Indicators.Identity:
        pass

    IsReady: bool



class IIndicatorWarmUpPeriodProvider:
    """ Represents an indicator with a warm up period provider. """
    WarmUpPeriod: int



class IndicatorBase(System.object, System.IComparable, QuantConnect.Indicators.IIndicator[T], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[T]]):
    # no doc
    @typing.overload
    def CompareTo(self, other: QuantConnect.Indicators.IIndicator[QuantConnect.Indicators.T]) -> int:
        pass

    @typing.overload
    def CompareTo(self, obj: object) -> int:
        pass

    def CompareTo(self, *args) -> int:
        pass

    def Equals(self, obj: object) -> bool:
        pass

    def Reset(self) -> None:
        pass

    def ToDetailedString(self) -> str:
        pass

    def ToString(self) -> str:
        pass

    @typing.overload
    def Update(self, input: QuantConnect.Data.IBaseData) -> bool:
        pass

    @typing.overload
    def Update(self, time: datetime.datetime, value: float) -> bool:
        pass

    def Update(self, *args) -> bool:
        pass

    def __init__(self, *args): #cannot find CLR constructor
        pass

    Current: QuantConnect.Indicators.IndicatorDataPoint

    IsReady: bool

    Name: str

    Samples: int


    Updated: BoundEvent


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
