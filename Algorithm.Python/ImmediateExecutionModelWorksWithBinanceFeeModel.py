# region imports
from AlgorithmImports import *
from datetime import timezone
from Execution.ImmediateExecutionModel import ImmediateExecutionModel
from QuantConnect.Orders import OrderEvent

# endregion

class ImmediateExecutionModelWorksWithBinanceFeeModel(QCAlgorithm):

    def Initialize(self):
        # *** initial configurations and backtest ***
        self.SetStartDate(2022, 12, 13)  # Set Start Date
        self.SetEndDate(2022, 12, 14)  # Set End Date
        self.SetAccountCurrency("BUSD") # Set Account Currency
        self.SetCash("BUSD", 100000, 1)  # Set Strategy Cash

        self.universe_settings.resolution = Resolution.MINUTE

        symbols = [ Symbol.create("BTCBUSD", SecurityType.CRYPTO, Market.BINANCE) ]

        # set algorithm framework models
        self.set_universe_selection(ManualUniverseSelectionModel(symbols))
        self.set_alpha(ConstantAlphaModel(InsightType.PRICE, InsightDirection.UP, timedelta(minutes = 20), 0.025, None))

        self.set_portfolio_construction(CustomPortfolioConstructionModel())
        self.set_execution(ImmediateExecutionModel())
        
        
        self.SetBrokerageModel(BrokerageName.Binance, AccountType.Margin)
    
    def on_order_event(self, order_event: OrderEvent) -> None:
        if order_event.status == OrderStatus.FILLED:
            if abs(order_event.quantity - 5.8) > 0.01:
                raise Exception(f"The expected quantity was {5.8} but the quantity from the order was {order_event.quantity}")

class CustomPortfolioConstructionModel(EqualWeightingPortfolioConstructionModel):
    def __init__(self):
        super().__init__(Resolution.DAILY)

    def create_targets(self, algorithm: QCAlgorithm, insights: List[Insight]) -> List[IPortfolioTarget]:
        targets = super().create_targets(algorithm, insights)
        return CustomPortfolioConstructionModel.add_p_portfolio_targets_tags(targets)

    @staticmethod
    def generate_portfolio_target_tag(target: IPortfolioTarget) -> str:
        return f"Portfolio target tag: {target.symbol} - {target.quantity}"

    @staticmethod
    def add_p_portfolio_targets_tags(targets: List[IPortfolioTarget]) -> List[IPortfolioTarget]:
        return [PortfolioTarget(target.symbol, target.quantity, CustomPortfolioConstructionModel.generate_portfolio_target_tag(target))
                for target in targets]
