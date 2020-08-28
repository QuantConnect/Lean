from .__CandlestickPatterns_4 import *
import typing
import QuantConnect.Indicators.CandlestickPatterns
import datetime



class MorningDojiStar(QuantConnect.Indicators.CandlestickPatterns.CandlestickPattern, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    Morning Doji Star candlestick pattern
    
    MorningDojiStar(name: str, penetration: Decimal)
    MorningDojiStar(penetration: Decimal)
    MorningDojiStar()
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str, penetration: float) -> QuantConnect.Indicators.CandlestickPatterns.MorningDojiStar:
        pass

    @typing.overload
    def __init__(self, penetration: float) -> QuantConnect.Indicators.CandlestickPatterns.MorningDojiStar:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Indicators.CandlestickPatterns.MorningDojiStar:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.CandlestickPatterns.MorningDojiStar:
        pass

    IsReady: bool



class MorningStar(QuantConnect.Indicators.CandlestickPatterns.CandlestickPattern, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    Morning Star candlestick pattern
    
    MorningStar(name: str, penetration: Decimal)
    MorningStar(penetration: Decimal)
    MorningStar()
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str, penetration: float) -> QuantConnect.Indicators.CandlestickPatterns.MorningStar:
        pass

    @typing.overload
    def __init__(self, penetration: float) -> QuantConnect.Indicators.CandlestickPatterns.MorningStar:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Indicators.CandlestickPatterns.MorningStar:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.CandlestickPatterns.MorningStar:
        pass

    IsReady: bool



class OnNeck(QuantConnect.Indicators.CandlestickPatterns.CandlestickPattern, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    On-Neck candlestick pattern indicator
    
    OnNeck(name: str)
    OnNeck()
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str) -> QuantConnect.Indicators.CandlestickPatterns.OnNeck:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Indicators.CandlestickPatterns.OnNeck:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.CandlestickPatterns.OnNeck:
        pass

    IsReady: bool



class Piercing(QuantConnect.Indicators.CandlestickPatterns.CandlestickPattern, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    Piercing candlestick pattern
    
    Piercing(name: str)
    Piercing()
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str) -> QuantConnect.Indicators.CandlestickPatterns.Piercing:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Indicators.CandlestickPatterns.Piercing:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.CandlestickPatterns.Piercing:
        pass

    IsReady: bool



class RickshawMan(QuantConnect.Indicators.CandlestickPatterns.CandlestickPattern, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    Rickshaw Man candlestick pattern
    
    RickshawMan(name: str)
    RickshawMan()
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str) -> QuantConnect.Indicators.CandlestickPatterns.RickshawMan:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Indicators.CandlestickPatterns.RickshawMan:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.CandlestickPatterns.RickshawMan:
        pass

    IsReady: bool



class RiseFallThreeMethods(QuantConnect.Indicators.CandlestickPatterns.CandlestickPattern, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    Rising/Falling Three Methods candlestick pattern
    
    RiseFallThreeMethods(name: str)
    RiseFallThreeMethods()
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str) -> QuantConnect.Indicators.CandlestickPatterns.RiseFallThreeMethods:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Indicators.CandlestickPatterns.RiseFallThreeMethods:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.CandlestickPatterns.RiseFallThreeMethods:
        pass

    IsReady: bool



class SeparatingLines(QuantConnect.Indicators.CandlestickPatterns.CandlestickPattern, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    Separating Lines candlestick pattern indicator
    
    SeparatingLines(name: str)
    SeparatingLines()
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str) -> QuantConnect.Indicators.CandlestickPatterns.SeparatingLines:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Indicators.CandlestickPatterns.SeparatingLines:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.CandlestickPatterns.SeparatingLines:
        pass

    IsReady: bool



class ShootingStar(QuantConnect.Indicators.CandlestickPatterns.CandlestickPattern, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    Shooting Star candlestick pattern
    
    ShootingStar(name: str)
    ShootingStar()
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str) -> QuantConnect.Indicators.CandlestickPatterns.ShootingStar:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Indicators.CandlestickPatterns.ShootingStar:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.CandlestickPatterns.ShootingStar:
        pass

    IsReady: bool



class ShortLineCandle(QuantConnect.Indicators.CandlestickPatterns.CandlestickPattern, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    Short Line Candle candlestick pattern indicator
    
    ShortLineCandle(name: str)
    ShortLineCandle()
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str) -> QuantConnect.Indicators.CandlestickPatterns.ShortLineCandle:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Indicators.CandlestickPatterns.ShortLineCandle:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.CandlestickPatterns.ShortLineCandle:
        pass

    IsReady: bool



class SpinningTop(QuantConnect.Indicators.CandlestickPatterns.CandlestickPattern, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    Spinning Top candlestick pattern indicator
    
    SpinningTop(name: str)
    SpinningTop()
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str) -> QuantConnect.Indicators.CandlestickPatterns.SpinningTop:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Indicators.CandlestickPatterns.SpinningTop:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.CandlestickPatterns.SpinningTop:
        pass

    IsReady: bool



class StalledPattern(QuantConnect.Indicators.CandlestickPatterns.CandlestickPattern, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    Stalled Pattern candlestick pattern
    
    StalledPattern(name: str)
    StalledPattern()
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str) -> QuantConnect.Indicators.CandlestickPatterns.StalledPattern:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Indicators.CandlestickPatterns.StalledPattern:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.CandlestickPatterns.StalledPattern:
        pass

    IsReady: bool



class StickSandwich(QuantConnect.Indicators.CandlestickPatterns.CandlestickPattern, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    Stick Sandwich candlestick pattern indicator
    
    StickSandwich(name: str)
    StickSandwich()
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str) -> QuantConnect.Indicators.CandlestickPatterns.StickSandwich:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Indicators.CandlestickPatterns.StickSandwich:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.CandlestickPatterns.StickSandwich:
        pass

    IsReady: bool



class Takuri(QuantConnect.Indicators.CandlestickPatterns.CandlestickPattern, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    Takuri (Dragonfly Doji with very long lower shadow) candlestick pattern indicator
    
    Takuri(name: str)
    Takuri()
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str) -> QuantConnect.Indicators.CandlestickPatterns.Takuri:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Indicators.CandlestickPatterns.Takuri:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.CandlestickPatterns.Takuri:
        pass

    IsReady: bool
