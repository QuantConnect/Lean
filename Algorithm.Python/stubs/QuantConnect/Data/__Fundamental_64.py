from .__Fundamental_65 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime


class StockType(System.object):
    """ Helper class for the AssetClassification's StockType field QuantConnect.Data.Fundamental.AssetClassification.StockType """
    AggressiveGrowth: int
    ClassicGrowth: int
    Cyclicals: int
    Distressed: int
    HardAsset: int
    HighYield: int
    SlowGrowth: int
    SpeculativeGrowth: int
    __all__: list


class StyleBox(System.object):
    """
    Helper class for the AssetClassification's StyleBox field QuantConnect.Data.Fundamental.AssetClassification.StyleBox.
                For stocks and stock funds, it classifies securities according to market capitalization and growth and value factor
    """
    LargeCore: int
    LargeGrowth: int
    LargeValue: int
    MidCore: int
    MidGrowth: int
    MidValue: int
    SmallCore: int
    SmallGrowth: int
    SmallValue: int
    __all__: list


class SubordinatedLiabilitiesBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The total carrying value of securities loaned to other broker dealers, typically used by such parties to cover short sales, secured by
                cash or other securities furnished by such parties until the borrowing is closed; in a Non-Differentiated Balance Sheet.
    
    SubordinatedLiabilitiesBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.SubordinatedLiabilitiesBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class TangibleBookValueBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The company's total book value less the value of any intangible assets.
                Methodology: Common Stock Equity minus Goodwill and Other Intangible Assets
    
    TangibleBookValueBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TangibleBookValueBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class TaxAssetsTotalBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Sum of total tax assets in a Non-Differentiated Balance Sheet, includes Tax Receivables and Deferred Tax Assets.
    
    TaxAssetsTotalBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TaxAssetsTotalBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class TaxEffectOfUnusualItemsIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Tax effect of the usual items
    
    TaxEffectOfUnusualItemsIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TaxEffectOfUnusualItemsIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class TaxesAssetsCurrentBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Carrying amount due within one year of the balance sheet date (or one operating cycle, if longer) from tax authorities as of the
                balance sheet date representing refunds of overpayments or recoveries based on agreed-upon resolutions of disputes, and current
                deferred tax assets.
    
    TaxesAssetsCurrentBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TaxesAssetsCurrentBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class TaxesReceivableBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Carrying amount due within one year of the balance sheet date (or one operating cycle, if longer) from tax authorities as of the
                balance sheet date representing refunds of overpayments or recoveries based on agreed-upon resolutions of disputes. This item is
                usually not available for bank industry.
    
    TaxesReceivableBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TaxesReceivableBalanceSheet:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class TaxesRefundPaidCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Total tax paid or received on operating activities.
    
    TaxesRefundPaidCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TaxesRefundPaidCashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class TaxesRefundPaidDirectCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Tax paid/refund related to operating activities, for the direct cash flow.
    
    TaxesRefundPaidDirectCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TaxesRefundPaidDirectCashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class TaxLossCarryforwardBasicEPS(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The earnings attributable to the tax loss carry forward (during the reporting period).
    
    TaxLossCarryforwardBasicEPS(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TaxLossCarryforwardBasicEPS:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class TaxLossCarryforwardDilutedEPS(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The earnings from any tax loss carry forward (in the reporting period).
    
    TaxLossCarryforwardDilutedEPS(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TaxLossCarryforwardDilutedEPS:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class TaxProvisionIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Include any taxes on income, net of any investment tax credits for the current accounting PeriodAsByte.
    
    TaxProvisionIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TaxProvisionIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class TaxRate(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Refers to the ratio of tax provision to pretax income. Morningstar calculates the ratio by using the underlying data reported in the
                company filings or reports:   Tax Provision / Pretax Income.
                [Note: Valid only when positive pretax income, and positive tax expense (not tax benefit)]
    
    TaxRate(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TaxRate:
        pass

    NineMonths: float

    OneMonth: float

    OneYear: float

    SixMonths: float

    ThreeMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]
