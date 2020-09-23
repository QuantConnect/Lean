from .__Fundamental_2 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime


class AdvanceFromFederalHomeLoanBanksBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    This item is typically available for bank industry. It's the amount of borrowings as of the balance sheet date from the Federal Home
                Loan Bank, which are primarily used to cover shortages in the required reserve balance and liquidity shortages.
    
    AdvanceFromFederalHomeLoanBanksBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.AdvanceFromFederalHomeLoanBanksBalanceSheet:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class AdvancesfromCentralBanksBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Borrowings from the central bank, which are primarily used to cover shortages in the required reserve balance and liquidity
                shortages.
    
    AdvancesfromCentralBanksBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.AdvancesfromCentralBanksBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class AllowanceForDoubtfulAccountsReceivableBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    An Allowance for Doubtful Accounts measures receivables recorded but not expected to be collected.
    
    AllowanceForDoubtfulAccountsReceivableBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.AllowanceForDoubtfulAccountsReceivableBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class AllowanceForLoansAndLeaseLossesBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    A contra account sets aside as an allowance for bad loans (e.g. customer defaults).
    
    AllowanceForLoansAndLeaseLossesBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.AllowanceForLoansAndLeaseLossesBalanceSheet:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class AllowanceForNotesReceivableBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    This item is typically available for bank industry. It represents a provision relating to a written agreement to receive money  with the
                terms of the note (at a specified future date(s) within one year from the reporting date (or the normal operating cycle, whichever is
                longer), consisting of principal as well as any accrued interest) for the portion that is expected to be uncollectible.
    
    AllowanceForNotesReceivableBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.AllowanceForNotesReceivableBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class AllTaxesPaidCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Cash paid to tax authorities in operating cash flow, using the direct method
    
    AllTaxesPaidCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.AllTaxesPaidCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class AmortizationCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The systematic and rational apportionment of the acquisition cost of intangible operational assets to future periods in which the benefits
                contribute to revenue. This field is to include Amortization and any variation where Amortization is the first account listed in the line item,
                excluding Amortization of Intangibles.
    
    AmortizationCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.AmortizationCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class AmortizationIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The non-cash expense recognized on intangible assets over the benefit period of the asset.
    
    AmortizationIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.AmortizationIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class AmortizationOfFinancingCostsAndDiscountsCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The component of interest expense representing the non-cash expenses charged against earnings in the period to allocate debt
                discount and premium, and the costs to issue debt and obtain financing over the related debt instruments. This item is usually only
                available for bank industry.
    
    AmortizationOfFinancingCostsAndDiscountsCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.AmortizationOfFinancingCostsAndDiscountsCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class AmortizationOfIntangiblesCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The aggregate expense charged against earnings to allocate the cost of intangible assets (nonphysical assets not used in
                production) in a systematic and rational manner to the periods expected to benefit from such assets.
    
    AmortizationOfIntangiblesCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.AmortizationOfIntangiblesCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class AmortizationOfIntangiblesIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The aggregate expense charged against earnings to allocate the cost of intangible assets (nonphysical assets not used in
                production) in a systematic and rational manner to the periods expected to benefit from such assets.
    
    AmortizationOfIntangiblesIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.AmortizationOfIntangiblesIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]
