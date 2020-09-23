from .__Fundamental_46 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime


class NonCurrentDeferredTaxesLiabilitiesBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The estimated future tax obligations, which usually arise when different accounting methods are used for financial statements and
                tax statement It is also an add-back to the cash flow statement. Deferred income taxes include accumulated tax deferrals due to
                accelerated depreciation and investment credit.
    
    NonCurrentDeferredTaxesLiabilitiesBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NonCurrentDeferredTaxesLiabilitiesBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class NonCurrentNoteReceivablesBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    An amount representing an agreement for an unconditional promise by the maker to pay the entity (holder) a definite sum of money
                at a future date(s), excluding the portion that is expected to be received within one year of the balance sheet date or the normal
                operating cycle, whichever is longer.
    
    NonCurrentNoteReceivablesBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NonCurrentNoteReceivablesBalanceSheet:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class NonCurrentOtherFinancialLiabilitiesBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Other long term financial liabilities not categorized and due over one year or a normal operating cycle (whichever is longer).
    
    NonCurrentOtherFinancialLiabilitiesBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NonCurrentOtherFinancialLiabilitiesBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class NonCurrentPensionAndOtherPostretirementBenefitPlansBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    A loan issued by an insurance company that uses the cash value of a person's life insurance policy as collateral.  This item is usually
                only available in the insurance industry.
    
    NonCurrentPensionAndOtherPostretirementBenefitPlansBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NonCurrentPensionAndOtherPostretirementBenefitPlansBalanceSheet:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class NonCurrentPrepaidAssetsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Sum of the carrying amounts that are paid in advance for expenses, which will be charged against earnings in periods after one
                year or beyond the operating cycle, if longer.
    
    NonCurrentPrepaidAssetsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NonCurrentPrepaidAssetsBalanceSheet:
        pass

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class NonInterestBearingBorrowingsCurrentBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Non-interest bearing deposits in other financial institutions for short periods of time, usually less than 12 months.
    
    NonInterestBearingBorrowingsCurrentBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NonInterestBearingBorrowingsCurrentBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class NonInterestBearingBorrowingsNonCurrentBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Non-interest bearing borrowings due after a year.
    
    NonInterestBearingBorrowingsNonCurrentBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NonInterestBearingBorrowingsNonCurrentBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class NonInterestBearingBorrowingsTotalBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Non-interest bearing deposits in other financial institutions for relatively short periods of time; on a Non-Differentiated Balance
                Sheet.
    
    NonInterestBearingBorrowingsTotalBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NonInterestBearingBorrowingsTotalBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class NonInterestBearingDepositsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The aggregate amount of all domestic and foreign deposits in the banks that do not draw interest.
    
    NonInterestBearingDepositsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NonInterestBearingDepositsBalanceSheet:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class NonInterestExpenseIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Any expenses that not related to interest. It includes labor and related expense, occupancy and equipment, commission,
                professional expense and contract services expenses, selling, general and administrative, research and development depreciation,
                amortization and depletion, and any other special income/charges.
    
    NonInterestExpenseIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NonInterestExpenseIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class NonInterestIncomeIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The total amount of non-interest income which may be derived from: (1) fees and commissions; (2) premiums earned; (3) equity
                investment; (4) the sale or disposal of assets; and (5) other sources not otherwise specified.
    
    NonInterestIncomeIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NonInterestIncomeIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class NormalizedBasicEPS(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The basic normalized earnings per share. Normalized EPS removes onetime and unusual items from EPS, to provide investors with a
                more accurate measure of the company's true earnings. Normalized Earnings / Basic Weighted Average Shares Outstanding.
    
    NormalizedBasicEPS(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.NormalizedBasicEPS:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]
