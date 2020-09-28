from .__CandlestickPatterns_2 import *
import typing
import QuantConnect.Indicators.CandlestickPatterns
import datetime



class DojiStar(QuantConnect.Indicators.CandlestickPatterns.CandlestickPattern, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    Doji Star candlestick pattern indicator
    
    DojiStar(name: str)
    DojiStar()
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str) -> QuantConnect.Indicators.CandlestickPatterns.DojiStar:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Indicators.CandlestickPatterns.DojiStar:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.CandlestickPatterns.DojiStar:
        pass

    IsReady: bool



class DragonflyDoji(QuantConnect.Indicators.CandlestickPatterns.CandlestickPattern, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    Dragonfly Doji candlestick pattern indicator
    
    DragonflyDoji(name: str)
    DragonflyDoji()
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str) -> QuantConnect.Indicators.CandlestickPatterns.DragonflyDoji:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Indicators.CandlestickPatterns.DragonflyDoji:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.CandlestickPatterns.DragonflyDoji:
        pass

    IsReady: bool



class Engulfing(QuantConnect.Indicators.CandlestickPatterns.CandlestickPattern, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    Engulfing candlestick pattern
    
    Engulfing(name: str)
    Engulfing()
    """
    @typing.overload
    def __init__(self, name: str) -> QuantConnect.Indicators.CandlestickPatterns.Engulfing:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Indicators.CandlestickPatterns.Engulfing:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.CandlestickPatterns.Engulfing:
        pass

    IsReady: bool



class EveningDojiStar(QuantConnect.Indicators.CandlestickPatterns.CandlestickPattern, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    Evening Doji Star candlestick pattern
    
    EveningDojiStar(name: str, penetration: Decimal)
    EveningDojiStar(penetration: Decimal)
    EveningDojiStar()
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str, penetration: float) -> QuantConnect.Indicators.CandlestickPatterns.EveningDojiStar:
        pass

    @typing.overload
    def __init__(self, penetration: float) -> QuantConnect.Indicators.CandlestickPatterns.EveningDojiStar:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Indicators.CandlestickPatterns.EveningDojiStar:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.CandlestickPatterns.EveningDojiStar:
        pass

    IsReady: bool



class EveningStar(QuantConnect.Indicators.CandlestickPatterns.CandlestickPattern, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    Evening Star candlestick pattern
    
    EveningStar(name: str, penetration: Decimal)
    EveningStar(penetration: Decimal)
    EveningStar()
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str, penetration: float) -> QuantConnect.Indicators.CandlestickPatterns.EveningStar:
        pass

    @typing.overload
    def __init__(self, penetration: float) -> QuantConnect.Indicators.CandlestickPatterns.EveningStar:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Indicators.CandlestickPatterns.EveningStar:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.CandlestickPatterns.EveningStar:
        pass

    IsReady: bool



class GapSideBySideWhite(QuantConnect.Indicators.CandlestickPatterns.CandlestickPattern, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    Up/Down-gap side-by-side white lines candlestick pattern
    
    GapSideBySideWhite(name: str)
    GapSideBySideWhite()
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str) -> QuantConnect.Indicators.CandlestickPatterns.GapSideBySideWhite:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Indicators.CandlestickPatterns.GapSideBySideWhite:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.CandlestickPatterns.GapSideBySideWhite:
        pass

    IsReady: bool



class GravestoneDoji(QuantConnect.Indicators.CandlestickPatterns.CandlestickPattern, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    Gravestone Doji candlestick pattern indicator
    
    GravestoneDoji(name: str)
    GravestoneDoji()
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str) -> QuantConnect.Indicators.CandlestickPatterns.GravestoneDoji:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Indicators.CandlestickPatterns.GravestoneDoji:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.CandlestickPatterns.GravestoneDoji:
        pass

    IsReady: bool



class Hammer(QuantConnect.Indicators.CandlestickPatterns.CandlestickPattern, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    Hammer candlestick pattern indicator
    
    Hammer(name: str)
    Hammer()
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str) -> QuantConnect.Indicators.CandlestickPatterns.Hammer:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Indicators.CandlestickPatterns.Hammer:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.CandlestickPatterns.Hammer:
        pass

    IsReady: bool



class HangingMan(QuantConnect.Indicators.CandlestickPatterns.CandlestickPattern, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    Hanging Man candlestick pattern indicator
    
    HangingMan(name: str)
    HangingMan()
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str) -> QuantConnect.Indicators.CandlestickPatterns.HangingMan:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Indicators.CandlestickPatterns.HangingMan:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.CandlestickPatterns.HangingMan:
        pass

    IsReady: bool



class Harami(QuantConnect.Indicators.CandlestickPatterns.CandlestickPattern, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    Harami candlestick pattern indicator
    
    Harami(name: str)
    Harami()
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str) -> QuantConnect.Indicators.CandlestickPatterns.Harami:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Indicators.CandlestickPatterns.Harami:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.CandlestickPatterns.Harami:
        pass

    IsReady: bool



class HaramiCross(QuantConnect.Indicators.CandlestickPatterns.CandlestickPattern, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    Harami Cross candlestick pattern indicator
    
    HaramiCross(name: str)
    HaramiCross()
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str) -> QuantConnect.Indicators.CandlestickPatterns.HaramiCross:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Indicators.CandlestickPatterns.HaramiCross:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.CandlestickPatterns.HaramiCross:
        pass

    IsReady: bool



class HighWaveCandle(QuantConnect.Indicators.CandlestickPatterns.CandlestickPattern, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    High-Wave Candle candlestick pattern indicator
    
    HighWaveCandle(name: str)
    HighWaveCandle()
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str) -> QuantConnect.Indicators.CandlestickPatterns.HighWaveCandle:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Indicators.CandlestickPatterns.HighWaveCandle:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.CandlestickPatterns.HighWaveCandle:
        pass

    IsReady: bool



class Hikkake(QuantConnect.Indicators.CandlestickPatterns.CandlestickPattern, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    Hikkake candlestick pattern
    
    Hikkake(name: str)
    Hikkake()
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str) -> QuantConnect.Indicators.CandlestickPatterns.Hikkake:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Indicators.CandlestickPatterns.Hikkake:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.CandlestickPatterns.Hikkake:
        pass

    IsReady: bool
