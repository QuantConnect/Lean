from .__Fundamental_17 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime


class CreditRiskProvisionsIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Provision for the risk of loss of principal or loss of a financial reward stemming from a borrower's failure to repay a loan or otherwise
                meet a contractual obligation. Credit risk arises whenever a borrower is expecting to use future cash flows to pay a current debt.
                Investors are compensated for assuming credit risk by way of interest payments from the borrower or issuer of a debt obligation.
                This is a contra account under Total Revenue in banks.
    
    CreditRiskProvisionsIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CreditRiskProvisionsIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CurrentAccruedExpensesBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    An expense recognized before it is paid for. Includes compensation, interest, pensions and all other miscellaneous accruals
                reported by the company. Expenses incurred during the accounting period, but not required to be paid until a later date.
    
    CurrentAccruedExpensesBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CurrentAccruedExpensesBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CurrentAssetsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The total amount of assets considered to be convertible into cash within a relatively short period of time, usually a year.
    
    CurrentAssetsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CurrentAssetsBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CurrentCapitalLeaseObligationBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Represents the total amount of long-term capital leases that must be paid within the next accounting PeriodAsByte. Capital lease
                obligations are contractual obligations that arise from obtaining the use of property or equipment via a capital lease contract.
    
    CurrentCapitalLeaseObligationBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CurrentCapitalLeaseObligationBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CurrentDebtAndCapitalLeaseObligationBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    All borrowings due within one year including current portions of long-term debt and capital leases as well as short-term debt such
                as bank loans and commercial paper.
    
    CurrentDebtAndCapitalLeaseObligationBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CurrentDebtAndCapitalLeaseObligationBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CurrentDebtBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Represents the total amount of long-term debt such as bank loans and commercial paper, which is due within one year.
    
    CurrentDebtBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CurrentDebtBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CurrentDeferredAssetsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Payments that will be assigned as expenses with one accounting period, but that are paid in advance and temporarily set up as
                current assets on the balance sheet.
    
    CurrentDeferredAssetsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CurrentDeferredAssetsBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CurrentDeferredLiabilitiesBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Represents the current portion of obligations, which is a liability that usually would have been paid but is now past due.
    
    CurrentDeferredLiabilitiesBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CurrentDeferredLiabilitiesBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CurrentDeferredRevenueBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Represents collections of cash or other assets related to revenue producing activity for which revenue has not yet been recognized.
                Generally, an entity records deferred revenue when it receives consideration from a customer before achieving certain criteria that
                must be met for revenue to be recognized in conformity with GAAP. It can be either current or non-current item. Also called
                unearned revenue.
    
    CurrentDeferredRevenueBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CurrentDeferredRevenueBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CurrentDeferredTaxesAssetsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Meaning a future tax asset, resulting from temporary differences between book (accounting) value of assets and liabilities and their
                tax value, or timing differences between the recognition of gains and losses in financial statements and their recognition in a tax
                computation. It is also called future tax.
    
    CurrentDeferredTaxesAssetsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CurrentDeferredTaxesAssetsBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CurrentDeferredTaxesLiabilitiesBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Meaning a future tax liability, resulting from temporary differences between book (accounting) value of assets and liabilities and
                their tax value, or timing differences between the recognition of gains and losses in financial statements and their recognition in a
                tax computation. Deferred tax liabilities generally arise where tax relief is provided in advance of an accounting expense, or income
                is accrued but not taxed until received.
    
    CurrentDeferredTaxesLiabilitiesBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CurrentDeferredTaxesLiabilitiesBalanceSheet:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]
