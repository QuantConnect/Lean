from .__Fundamental_3 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime


class AmortizationOfSecuritiesCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Represents amortization of the allocation of a lump sum amount to different time periods, particularly for securities, debt, loans,
                and other forms of financing. Does not include amortization, amortization of capital expenditure and intangible assets.
    
    AmortizationOfSecuritiesCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.AmortizationOfSecuritiesCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class AmortizationSupplementalIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The current period expense charged against earnings on intangible asset over its useful life. It is a supplemental value which would
                be reported outside consolidated statements.
    
    AmortizationSupplementalIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.AmortizationSupplementalIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class AssetClassification(System.object):
    """
    Definition of the AssetClassification class
    
    AssetClassification()
    """
    def UpdateValues(self, update: QuantConnect.Data.Fundamental.AssetClassification) -> None:
        pass

    CANNAICS: int

    FinancialHealthGrade: str

    GrowthGrade: str

    GrowthScore: float

    MorningstarEconomySphereCode: int

    MorningstarIndustryCode: int

    MorningstarIndustryGroupCode: int

    MorningstarSectorCode: int

    NACE: float

    NAICS: int

    ProfitabilityGrade: str

    SIC: int

    SizeScore: float

    StockType: int

    StyleBox: int

    StyleScore: float

    ValueScore: float



class AssetImpairmentChargeCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The charge against earnings resulting from the aggregate write down of all assets from their carrying value to their fair value.
    
    AssetImpairmentChargeCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.AssetImpairmentChargeCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class AssetsHeldForSaleBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    This item is typically available for bank industry. It's a part of long-lived assets, which has been decided for sale in the future.
    
    AssetsHeldForSaleBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.AssetsHeldForSaleBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class AssetsHeldForSaleCurrentBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Short term assets set apart for sale to liquidate in the future and are measured at the lower of carrying amount and fair value less
                costs to sell.
    
    AssetsHeldForSaleCurrentBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.AssetsHeldForSaleCurrentBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class AssetsHeldForSaleNonCurrentBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Long term assets set apart for sale to liquidate in the future and are measured at the lower of carrying amount and fair value less
                costs to sell.
    
    AssetsHeldForSaleNonCurrentBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.AssetsHeldForSaleNonCurrentBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class AssetsOfDiscontinuedOperationsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    A portion of a company's business that has been disposed of or sold.
    
    AssetsOfDiscontinuedOperationsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.AssetsOfDiscontinuedOperationsBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class AssetsPledgedasCollateralSubjecttoSaleorRepledgingTotalBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Total value collateral assets pledged to the bank that can be sold or used as collateral for other loans.
    
    AssetsPledgedasCollateralSubjecttoSaleorRepledgingTotalBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.AssetsPledgedasCollateralSubjecttoSaleorRepledgingTotalBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class AssetsTurnover(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Revenue / Average Total Assets
    
    AssetsTurnover(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.AssetsTurnover:
        pass

    OneYear: float

    SixMonths: float

    ThreeMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class AvailableForSaleSecuritiesBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    For an unclassified balance sheet, this item represents equity securities categorized neither as held-to-maturity nor trading. Equity
                securities represent ownership interests or the right to acquire ownership interests in corporations and other legal entities which
                ownership interest is represented by shares of common or preferred stock (which is not mandatory redeemable or redeemable at
                the option of the holder), convertible securities, stock rights, or stock warrants. This category includes preferred stocks, available-
                for-sale and common stock, available-for-sale.
    
    AvailableForSaleSecuritiesBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.AvailableForSaleSecuritiesBalanceSheet:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class AverageDilutionEarningsIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Adjustments to reported net income to calculate Diluted EPS, by assuming that all convertible instruments are converted to
                Common Equity. The adjustments usually include the interest expense of debentures when assumed converted and preferred
                dividends of convertible preferred stock when assumed converted.
    
    AverageDilutionEarningsIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.AverageDilutionEarningsIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]
