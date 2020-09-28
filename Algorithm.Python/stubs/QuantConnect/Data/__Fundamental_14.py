from .__Fundamental_15 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime


class ClassesofCashReceiptsfromOperatingActivitiesCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Sum of total cash receipts in the direct cash flow.
    
    ClassesofCashReceiptsfromOperatingActivitiesCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ClassesofCashReceiptsfromOperatingActivitiesCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CommercialLoanBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Short-term loan, typically 90 days, used by a company to finance seasonal working capital needs.
    
    CommercialLoanBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CommercialLoanBalanceSheet:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CommercialPaperBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Commercial paper is a money-market security issued by large banks and corporations. It represents the current obligation for the
                company. There are four basic kinds of commercial paper: promissory notes, drafts, checks, and certificates of deposit. The
                maturities of these money market securities generally do not exceed 270 days.
    
    CommercialPaperBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CommercialPaperBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CommissionExpensesIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """ CommissionExpensesIncomeStatement(store: IDictionary[str, Decimal]) """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CommissionExpensesIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CommissionPaidCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Cash paid for commissions in operating cash flow, using the direct method
    
    CommissionPaidCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CommissionPaidCashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CommonEquityToAssets(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    This is a financial ratio of common stock equity to total assets that indicates the relative proportion of equity used to finance a
                company's assets.
    
    CommonEquityToAssets(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CommonEquityToAssets:
        pass

    NineMonths: float

    OneMonth: float

    OneYear: float

    SixMonths: float

    ThreeMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CommonStockBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Common stock (all issues) at par value, as reported within the Stockholder's Equity section of the balance sheet; i.e. it is one
                component of Common Stockholder's Equity
    
    CommonStockBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CommonStockBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CommonStockDividendPaidCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The cash outflow from the distribution of an entity's earnings in the form of dividends to common shareholders.
    
    CommonStockDividendPaidCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CommonStockDividendPaidCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CommonStockEquityBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The portion of the Stockholders' Equity that reflects the amount of common stock, which are units of ownership.
    
    CommonStockEquityBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CommonStockEquityBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CommonStockIssuanceCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The cash inflow from offering common stock, which is the additional capital contribution to the entity during the PeriodAsByte.
    
    CommonStockIssuanceCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CommonStockIssuanceCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CommonStockPaymentsCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The cash outflow to reacquire common stock during the PeriodAsByte.
    
    CommonStockPaymentsCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CommonStockPaymentsCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CommonUtilityPlantBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The amount for the other plant related to the utility industry fix assets.
    
    CommonUtilityPlantBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CommonUtilityPlantBalanceSheet:
        pass

    NineMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CompanyProfile(System.object):
    """
    Definition of the CompanyProfile class
    
    CompanyProfile()
    """
    def UpdateValues(self, update: QuantConnect.Data.Fundamental.CompanyProfile) -> None:
        pass

    AverageEmployeeNumber: int

    ContactEmail: str

    EnterpriseValue: int

    HeadquarterAddressLine1: str

    HeadquarterAddressLine2: str

    HeadquarterAddressLine3: str

    HeadquarterAddressLine4: str

    HeadquarterAddressLine5: str

    HeadquarterCity: str

    HeadquarterCountry: str

    HeadquarterFax: str

    HeadquarterHomepage: str

    HeadquarterPhone: str

    HeadquarterPostalCode: str

    HeadquarterProvince: str

    IsHeadOfficeSameWithRegisteredOfficeFlag: bool

    MarketCap: int

    ReasonofSharesChange: str

    RegisteredAddressLine1: str

    RegisteredAddressLine2: str

    RegisteredAddressLine3: str

    RegisteredAddressLine4: str

    RegisteredCity: str

    RegisteredCountry: str

    RegisteredFax: str

    RegisteredPhone: str

    RegisteredPostalCode: str

    RegisteredProvince: str

    ShareClassLevelSharesOutstanding: int

    SharesOutstanding: int

    SharesOutstandingWithBalanceSheetEndingDate: int

    TotalEmployeeNumber: int
