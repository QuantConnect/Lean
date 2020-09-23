from .__Fundamental_16 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime



class CompanyReference(System.object):
    """
    Definition of the CompanyReference class
    
    CompanyReference()
    """
    def UpdateValues(self, update: QuantConnect.Data.Fundamental.CompanyReference) -> None:
        pass

    Advisor: str

    AdvisorLanguageCode: str

    Auditor: str

    AuditorLanguageCode: str

    BusinessCountryID: str

    CIK: str

    CompanyId: str

    CompanyStatus: str

    CountryId: str

    ExpectedFiscalYearEnd: datetime.datetime

    FiscalYearEnd: int

    IndustryTemplateCode: str

    IsLimitedLiabilityCompany: bool

    IsLimitedPartnership: bool

    IsREIT: bool

    LegalName: str

    LegalNameLanguageCode: str

    PrimaryExchangeID: str

    PrimaryMIC: str

    PrimaryShareClassID: str

    PrimarySymbol: str

    ReportStyle: int

    ShortName: str

    StandardName: str

    YearofEstablishment: str



class ComTreShaNumBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The treasury stock number of common shares. This represents the number of common shares owned by the company as a result of
                share repurchase programs or donations.
    
    ComTreShaNumBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ComTreShaNumBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ConstructionInProgressBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    It represents carrying amount of long-lived asset under construction that includes construction costs to date on capital projects.
                Assets constructed, but not completed.
    
    ConstructionInProgressBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ConstructionInProgressBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ConsumerLoanBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    A loan that establishes consumer credit that is granted for personal use; usually unsecured and based on the borrower's integrity
                and ability to pay.
    
    ConsumerLoanBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ConsumerLoanBalanceSheet:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ContinuingAndDiscontinuedBasicEPS(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Basic EPS from Continuing Operations plus Basic EPS from Discontinued Operations.
    
    ContinuingAndDiscontinuedBasicEPS(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ContinuingAndDiscontinuedBasicEPS:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ContinuingAndDiscontinuedDilutedEPS(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Diluted EPS from Continuing Operations plus Diluted EPS from Discontinued Operations.
    
    ContinuingAndDiscontinuedDilutedEPS(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ContinuingAndDiscontinuedDilutedEPS:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ConvertibleLoansCurrentBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    This represents loans that entitle the lender (or the holder of loan debenture) to convert the loan to common or preferred stock
                (ordinary or preference shares) within the next 12 months or operating cycle.
    
    ConvertibleLoansCurrentBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ConvertibleLoansCurrentBalanceSheet:
        pass

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ConvertibleLoansNonCurrentBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    A long term loan with a warrant attached that gives the debt holder the option to exchange all or a portion of the loan principal for
                an equity position in the company at a predetermined rate of conversion within a specified period of time.
    
    ConvertibleLoansNonCurrentBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ConvertibleLoansNonCurrentBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ConvertibleLoansTotalBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Loans that entitles the lender (or the holder of loan debenture) to convert the loan to common or preferred stock (ordinary or
                preference shares) at a specified rate conversion rate and a specified time frame; in a Non-Differentiated Balance Sheet.
    
    ConvertibleLoansTotalBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ConvertibleLoansTotalBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CostOfRevenueIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The aggregate cost of goods produced and sold and services rendered during the reporting PeriodAsByte. It excludes all operating
                expenses such as depreciation, depletion, amortization, and SG&A. For the must have cost industry, if the number is not reported
                by the company, it will be calculated based on accounting equation.
                Cost of Revenue = Revenue - Operating Expenses - Operating Profit.
    
    CostOfRevenueIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CostOfRevenueIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CreditCardIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Income earned from credit card services including late, over limit, and annual fees. This item is usually only available for bank
                industry.
    
    CreditCardIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CreditCardIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CreditLossesProvisionIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    A charge to income which represents an expense deemed adequate by management given the composition of a bank's credit
                portfolios, their probability of default, the economic environment and the allowance for credit losses already established. Specific
                provisions are established to reduce the book value of specific assets (primarily loans) to establish the amount expected to be
                recovered on the loans.
    
    CreditLossesProvisionIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CreditLossesProvisionIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]
