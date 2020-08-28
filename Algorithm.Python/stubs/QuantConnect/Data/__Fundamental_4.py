from .__Fundamental_5 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime



class BankIndebtednessBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    All indebtedness for borrowed money or the deferred purchase price of property or services, including without limitation
                reimbursement and other obligations with respect to surety bonds and letters of credit, all obligations evidenced by notes, bonds
                debentures or similar instruments, all capital lease obligations and all contingent obligations.
    
    BankIndebtednessBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.BankIndebtednessBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class BankLoansCurrentBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    A debt financing obligation issued by a bank or similar financial institution to a company, that entitles the lender or holder of the
                instrument to interest payments and the repayment of principal at a specified time within the next 12 months or operating cycle.
    
    BankLoansCurrentBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.BankLoansCurrentBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class BankLoansNonCurrentBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    A debt financing obligation issued by a bank or similar financial institution to a company, that entitles the lender or holder of the
                instrument to interest payments and the repayment of principal at a specified time beyond the current accounting PeriodAsByte.
    
    BankLoansNonCurrentBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.BankLoansNonCurrentBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class BankLoansTotalBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Total debt financing obligation issued by a bank or similar financial institution to a company that entitles the lender or holder of the
                instrument to interest payments and the repayment of principal at a specified time; in a Non-Differentiated Balance Sheet.
    
    BankLoansTotalBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.BankLoansTotalBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class BankOwnedLifeInsuranceBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The carrying amount of a life insurance policy on an officer, executive or employee for which the reporting entity (a bank) is entitled
                to proceeds from the policy upon death of the insured or surrender of the insurance policy.
    
    BankOwnedLifeInsuranceBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.BankOwnedLifeInsuranceBalanceSheet:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class BasicAccountingChange(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Basic EPS from the Cumulative Effect of Accounting Change is the earnings attributable to the accounting change (during the
                reporting period) divided by the weighted average number of common shares outstanding.
    
    BasicAccountingChange(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.BasicAccountingChange:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class BasicAverageShares(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The shares outstanding used to calculate Basic EPS, which is the weighted average common share outstanding through the whole
                accounting PeriodAsByte.  Note: If Basic Average Shares are not presented by the firm in the Income Statement, this data point will be
                null.
    
    BasicAverageShares(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.BasicAverageShares:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class BasicContinuousOperations(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Basic EPS from Continuing Operations is the earnings from continuing operations reported by the company divided by the weighted
                average number of common shares outstanding.
    
    BasicContinuousOperations(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.BasicContinuousOperations:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class BasicDiscontinuousOperations(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Basic EPS from Discontinued Operations is the earnings from discontinued operations reported by the company divided by the
                weighted average number of common shares outstanding. This only includes gain or loss from discontinued operations.
    
    BasicDiscontinuousOperations(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.BasicDiscontinuousOperations:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class BasicEPS(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Basic EPS is the bottom line net income divided by the weighted average number of common shares outstanding.
    
    BasicEPS(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.BasicEPS:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class BasicEPSOtherGainsLosses(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Basic EPS from the Other Gains/Losses is the earnings attributable to the other gains/losses (during the reporting period) divided by
                the weighted average number of common shares outstanding.
    
    BasicEPSOtherGainsLosses(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.BasicEPSOtherGainsLosses:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class BasicExtraordinary(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Basic EPS from the Extraordinary Gains/Losses is the earnings attributable to the gains or losses (during the reporting period) from
                extraordinary items divided by the weighted average number of common shares outstanding.
    
    BasicExtraordinary(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.BasicExtraordinary:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]
