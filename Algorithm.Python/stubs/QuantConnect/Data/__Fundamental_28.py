from .__Fundamental_29 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime


class FlightFleetVehicleAndRelatedEquipmentsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    It is one of the important fixed assets for transportation industry, which includes bicycles, cars, motorcycles, trains, ships, boats,
                and aircraft.  This item is typically available for transportation industry.
    
    FlightFleetVehicleAndRelatedEquipmentsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.FlightFleetVehicleAndRelatedEquipmentsBalanceSheet:
        pass

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ForeclosedAssetsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The carrying amount as of the balance sheet date of all assets obtained in full or partial satisfaction of a debt arrangement through
                foreclosure proceedings or defeasance; includes real and personal property; equity interests in corporations, partnerships, and joint
                ventures; and beneficial interest in trusts.  This item is typically typically available for bank industry.
    
    ForeclosedAssetsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ForeclosedAssetsBalanceSheet:
        pass

    NineMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ForeignCurrencyTranslationAdjustmentsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Changes to accumulated comprehensive income that results from the process of translating subsidiary financial statements and
                foreign equity investments into functional currency of the reporting company.
    
    ForeignCurrencyTranslationAdjustmentsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ForeignCurrencyTranslationAdjustmentsBalanceSheet:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ForeignExchangeTradingGainsIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Trading revenues that result from foreign exchange exposures such as cash instruments and off-balance sheet derivative
                instruments. This item is usually only available for bank industry.
    
    ForeignExchangeTradingGainsIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ForeignExchangeTradingGainsIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class FreeCashFlowCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Cash Flow Operations minus Capital Expenditures.
    
    FreeCashFlowCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.FreeCashFlowCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class FuelAndPurchasePowerIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Cost of fuel, purchase power and gas associated with revenue generation. This item is usually only available for utility industry.
    
    FuelAndPurchasePowerIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.FuelAndPurchasePowerIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class FuelIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The aggregate amount of fuel cost for current period associated with the revenue generation. This item is usually only available for
                transportation industry.
    
    FuelIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.FuelIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class Fundamentals(QuantConnect.Data.Fundamental.FineFundamental, QuantConnect.Data.IBaseData):
    """
    Defines a merged viw of QuantConnect.Data.Fundamental.FineFundamental and QuantConnect.Data.UniverseSelection.CoarseFundamental
    
    Fundamentals()
    """
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

    DollarVolume: float

    HasFundamentalData: bool

    Market: str

    Volume: int



class FundFromOperationCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Funds from operations; populated only for real estate investment trusts (REITs), defined as the sum of net income, gain/loss
                (realized and unrealized) on investment securities, asset impairment charge, depreciation and amortization and gain/ loss on the
                sale of business and property plant and equipment.
    
    FundFromOperationCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.FundFromOperationCashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class FuturePolicyBenefitsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Accounting policy pertaining to an insurance entity's net liability for future benefits (for example, death, cash surrender value) to be
                paid to or on behalf of policyholders, describing the bases, methodologies and components of the reserve, and assumptions
                regarding estimates of expected investment yields, mortality, morbidity, terminations and expenses.
    
    FuturePolicyBenefitsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.FuturePolicyBenefitsBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class GainLossonDerecognitionofAvailableForSaleFinancialAssetsIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Gain/loss on the write-off of financial assets available-for-sale.
    
    GainLossonDerecognitionofAvailableForSaleFinancialAssetsIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.GainLossonDerecognitionofAvailableForSaleFinancialAssetsIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]
