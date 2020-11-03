import typing
import System.Collections.Concurrent
import System
import QuantConnect.Securities.Option
import QuantConnect.Securities
import QuantConnect.Orders.OptionExercise
import QuantConnect.Orders
import QuantConnect.Data.Market
import QuantConnect.Data
import QuantConnect
import Python.Runtime
import datetime



class OptionPriceModels(System.object):
    """ Static class contains definitions of major option pricing models that can be used in LEAN """
    @staticmethod
    def AdditiveEquiprobabilities() -> QuantConnect.Securities.Option.IOptionPriceModel:
        pass

    @staticmethod
    def BaroneAdesiWhaley() -> QuantConnect.Securities.Option.IOptionPriceModel:
        pass

    @staticmethod
    def BinomialCoxRossRubinstein() -> QuantConnect.Securities.Option.IOptionPriceModel:
        pass

    @staticmethod
    def BinomialJarrowRudd() -> QuantConnect.Securities.Option.IOptionPriceModel:
        pass

    @staticmethod
    def BinomialJoshi() -> QuantConnect.Securities.Option.IOptionPriceModel:
        pass

    @staticmethod
    def BinomialLeisenReimer() -> QuantConnect.Securities.Option.IOptionPriceModel:
        pass

    @staticmethod
    def BinomialTian() -> QuantConnect.Securities.Option.IOptionPriceModel:
        pass

    @staticmethod
    def BinomialTrigeorgis() -> QuantConnect.Securities.Option.IOptionPriceModel:
        pass

    @staticmethod
    def BjerksundStensland() -> QuantConnect.Securities.Option.IOptionPriceModel:
        pass

    @staticmethod
    def BlackScholes() -> QuantConnect.Securities.Option.IOptionPriceModel:
        pass

    @staticmethod
    def CrankNicolsonFD() -> QuantConnect.Securities.Option.IOptionPriceModel:
        pass

    @staticmethod
    def Integral() -> QuantConnect.Securities.Option.IOptionPriceModel:
        pass

    __all__: list


class OptionStrategies(System.object):
    # no doc
    @staticmethod
    def BearCallSpread(canonicalOption: QuantConnect.Symbol, leg1Strike: float, leg2Strike: float, expiration: datetime.datetime) -> QuantConnect.Securities.Option.OptionStrategy:
        pass

    @staticmethod
    def BearPutSpread(canonicalOption: QuantConnect.Symbol, leg1Strike: float, leg2Strike: float, expiration: datetime.datetime) -> QuantConnect.Securities.Option.OptionStrategy:
        pass

    @staticmethod
    def BullCallSpread(canonicalOption: QuantConnect.Symbol, leg1Strike: float, leg2Strike: float, expiration: datetime.datetime) -> QuantConnect.Securities.Option.OptionStrategy:
        pass

    @staticmethod
    def BullPutSpread(canonicalOption: QuantConnect.Symbol, leg1Strike: float, leg2Strike: float, expiration: datetime.datetime) -> QuantConnect.Securities.Option.OptionStrategy:
        pass

    @staticmethod
    def CallButterfly(canonicalOption: QuantConnect.Symbol, leg1Strike: float, leg2Strike: float, leg3Strike: float, expiration: datetime.datetime) -> QuantConnect.Securities.Option.OptionStrategy:
        pass

    @staticmethod
    def CallCalendarSpread(canonicalOption: QuantConnect.Symbol, strike: float, expiration1: datetime.datetime, expiration2: datetime.datetime) -> QuantConnect.Securities.Option.OptionStrategy:
        pass

    @staticmethod
    def PutButterfly(canonicalOption: QuantConnect.Symbol, leg1Strike: float, leg2Strike: float, leg3Strike: float, expiration: datetime.datetime) -> QuantConnect.Securities.Option.OptionStrategy:
        pass

    @staticmethod
    def PutCalendarSpread(canonicalOption: QuantConnect.Symbol, strike: float, expiration1: datetime.datetime, expiration2: datetime.datetime) -> QuantConnect.Securities.Option.OptionStrategy:
        pass

    @staticmethod
    def Straddle(canonicalOption: QuantConnect.Symbol, strike: float, expiration: datetime.datetime) -> QuantConnect.Securities.Option.OptionStrategy:
        pass

    @staticmethod
    def Strangle(canonicalOption: QuantConnect.Symbol, leg1Strike: float, leg2Strike: float, expiration: datetime.datetime) -> QuantConnect.Securities.Option.OptionStrategy:
        pass

    __all__: list


class OptionStrategy(System.object):
    """
    Option strategy specification class. Describes option strategy and its parameters for trading.
    
    OptionStrategy()
    """
    Name: str

    OptionLegs: typing.List[QuantConnect.Securities.Option.OptionLegData]

    Underlying: QuantConnect.Symbol

    UnderlyingLegs: typing.List[QuantConnect.Securities.Option.UnderlyingLegData]


    OptionLegData: type
    UnderlyingLegData: type


class OptionSymbol(System.object):
    """ Static class contains common utility methods specific to symbols representing the option contracts """
    @staticmethod
    def GetLastDayOfTrading(symbol: QuantConnect.Symbol) -> datetime.datetime:
        pass

    @staticmethod
    def IsOptionContractExpired(symbol: QuantConnect.Symbol, currentTimeUtc: datetime.datetime) -> bool:
        pass

    @staticmethod
    def IsStandard(symbol: QuantConnect.Symbol) -> bool:
        pass

    @staticmethod
    def IsStandardContract(symbol: QuantConnect.Symbol) -> bool:
        pass

    @staticmethod
    def IsWeekly(symbol: QuantConnect.Symbol) -> bool:
        pass

    __all__: list


class OptionSymbolProperties(QuantConnect.Securities.SymbolProperties):
    """
    Represents common properties for a specific option contract
    
    OptionSymbolProperties(description: str, quoteCurrency: str, contractMultiplier: Decimal, pipSize: Decimal, lotSize: Decimal)
    OptionSymbolProperties(properties: SymbolProperties)
    """
    @typing.overload
    def __init__(self, description: str, quoteCurrency: str, contractMultiplier: float, pipSize: float, lotSize: float) -> QuantConnect.Securities.Option.OptionSymbolProperties:
        pass

    @typing.overload
    def __init__(self, properties: QuantConnect.Securities.SymbolProperties) -> QuantConnect.Securities.Option.OptionSymbolProperties:
        pass

    def __init__(self, *args) -> QuantConnect.Securities.Option.OptionSymbolProperties:
        pass

    ContractUnitOfTrade: int
