from .__Fundamental_54 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime


class PaymentTurnover(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Cost of Goods Sold / Average Accounts Payables
    
    PaymentTurnover(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.PaymentTurnover:
        pass

    OneYear: float

    SixMonths: float

    ThreeMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class PensionAndEmployeeBenefitExpenseCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The amount of pension and other (such as medical, dental and life insurance) postretirement benefit costs recognized during the
                PeriodAsByte.
    
    PensionAndEmployeeBenefitExpenseCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.PensionAndEmployeeBenefitExpenseCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class PensionandOtherPostRetirementBenefitPlansCurrentBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Total of the carrying values as of the balance sheet date of obligations incurred through that date and payable for obligations related
                to services received from employees, such as accrued salaries and bonuses, payroll taxes and fringe benefits.
    
    PensionandOtherPostRetirementBenefitPlansCurrentBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.PensionandOtherPostRetirementBenefitPlansCurrentBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class PensionAndOtherPostretirementBenefitPlansTotalBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Total of the carrying values as of the balance sheet date of obligations incurred through that date and payable for obligations related
                to services received from employees, such as accrued salaries and bonuses, payroll taxes and fringe benefits. Used to reflect the
                current portion of the liabilities (due within one year or within the normal operating cycle if longer).
    
    PensionAndOtherPostretirementBenefitPlansTotalBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.PensionAndOtherPostretirementBenefitPlansTotalBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class PensionCostsIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The expense that a company incurs each year by providing a pension plan for its employees. Major expenses in the pension cost
                include employer matching contributions and management fees.
    
    PensionCostsIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.PensionCostsIncomeStatement:
        pass

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class Period(System.object):
    """ Period constants for multi-period fields """
    FiveYears: str
    NineMonths: str
    OneMonth: str
    OneYear: str
    SixMonths: str
    TenYears: str
    ThreeMonths: str
    ThreeYears: str
    TwelveMonths: str
    TwoMonths: str
    TwoYears: str
    __all__: list


class PolicyAcquisitionExpenseIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Costs that vary with and are primarily related to the acquisition of new and renewal insurance contracts. Also referred to as
                underwriting expenses.
    
    PolicyAcquisitionExpenseIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.PolicyAcquisitionExpenseIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class PolicyholderBenefitsCededIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The provision in current period for future policy benefits, claims, and claims settlement, which is under reinsurance arrangements.
                This item is usually only available for insurance industry.
    
    PolicyholderBenefitsCededIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.PolicyholderBenefitsCededIncomeStatement:
        pass

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class PolicyholderBenefitsGrossIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The gross amount of provision in current period for future policyholder benefits, claims, and claims settlement, incurred in the
                claims settlement process before the effects of reinsurance arrangements. This item is usually only available for insurance industry.
    
    PolicyholderBenefitsGrossIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.PolicyholderBenefitsGrossIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class PolicyholderDepositInvestmentReceivedCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Cash received from policyholder deposit investment activities in operating cash flow, using the direct method. This item is usually
                only available for insurance industry
    
    PolicyholderDepositInvestmentReceivedCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.PolicyholderDepositInvestmentReceivedCashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class PolicyholderDividendsIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Payments made or credits extended to the insured by the company, usually at the end of a policy year results in reducing the net
                insurance cost to the policyholder. Such dividends may be paid in cash to the insured or applied by the insured as reductions of the
                premiums due for the next policy year. This item is usually only available for insurance industry.
    
    PolicyholderDividendsIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.PolicyholderDividendsIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class PolicyholderFundsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The total liability as of the balance sheet date of amounts due to policy holders, excluding future policy benefits and claims,
                including unpaid policy dividends, retrospective refunds, and undistributed earnings on participating business.
    
    PolicyholderFundsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.PolicyholderFundsBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]
