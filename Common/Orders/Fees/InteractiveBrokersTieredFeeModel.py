# QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
# Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

from AlgorithmImports import *

### <summary>
### Provides the implementation of "IFeeModel" for Interactive Brokers Tiered Fee Structure
### </summary>
class InteractiveBrokersTieredFeeModel(FeeModel):
    equity_minimum_order_fee = 0.35
    equity_commission_rate = None
    future_commission_tier = None
    forex_commission_rate = None
    forex_minimum_order_fee = None
    crypto_commission_rate = None
    crypto_minimum_order_fee = 1.75
    # option commission function takes number of contracts and the size of the option premium and returns total commission
    option_fee = {}
    last_order_time = datetime.min
    # List of Option exchanges susceptible to pay ORF regulatory fee.
    option_exchanges_orf_fee = [Market.CBOE, Market.USA]
    volume_by_order = {}
    
    def __init__(self, monthly_equity_trade_volume: float = 0, monthly_future_trade_volume: float = 0, monthly_forex_trade_amount_in_us_dollars: float = 0,
        monthly_options_trade_amount_in_contracts: float = 0, monthly_crypto_trade_amount_in_us_dollars: float = 0) -> None:
        '''Initializes a new instance of the "InteractiveBrokersTieredFeeModel"
        Args:
            monthly_equity_trade_volume: Monthly Equity shares traded
            monthly_future_trade_volume: Monthly Future contracts traded
            monthly_forex_trade_amount_in_us_dollars: Monthly FX dollar volume traded
            monthly_options_trade_amount_in_contracts: Monthly options contracts traded
            monthly_crypto_trade_amount_in_us_dollars: Monthly Crypto dollar volume traded (in USD)'''
        self.reprocess_rate_schedule(monthly_equity_trade_volume,
                                     monthly_future_trade_volume,
                                     monthly_forex_trade_amount_in_us_dollars,
                                     monthly_options_trade_amount_in_contracts,
                                     monthly_crypto_trade_amount_in_us_dollars)
        
        # IB fee + exchange fee
        # Reference at https://www.interactivebrokers.com/en/index.php?f=commission&p=futures1
        self.future_fee = {
            Market.USA: self.united_states_future_fees,
            Market.HKFE: self.hong_kong_future_fees,
            Market.EUREX: self.eurex_future_fees
        }
        
        self.monthly_trade_volume = {
            SecurityType.EQUITY: monthly_equity_trade_volume,
            SecurityType.FUTURE: monthly_future_trade_volume,
            SecurityType.FOREX: monthly_forex_trade_amount_in_us_dollars,
            SecurityType.OPTION: monthly_options_trade_amount_in_contracts,
            SecurityType.CRYPTO: monthly_crypto_trade_amount_in_us_dollars
        }

    def update_month_volume(self) -> None:
        for order, volume in self.volume_by_order.copy().items():
            # Tier only changed by the filled volume.
            if order.status == OrderStatus.FILLED:
                self.monthly_trade_volume[order.security_type] += volume
                # Remove the processed order.
                self.volume_by_order.pop(order)
        
    def reprocess_rate_schedule(self, monthly_equity_trade_volume: float, monthly_future_trade_volume: float, monthly_forex_trade_amount_in_us_dollars: float,
        monthly_options_trade_amount_in_contracts: float, monthly_crypto_trade_amount_in_us_dollars: float) -> None:
        '''Reprocess the rate schedule based on the current traded volume in various assets.
        Args:
            monthly_equity_trade_volume: Monthly Equity shares traded
            monthly_future_trade_volume: Monthly Future contracts traded
            monthly_forex_trade_amount_in_us_dollars: Monthly FX dollar volume traded
            monthly_options_trade_amount_in_contracts: Monthly options contracts traded
            monthly_crypto_trade_amount_in_us_dollars: Monthly Crypto dollar volume traded (in USD)'''
        self.equity_commission_rate = self.process_equity_rate_schedule(monthly_equity_trade_volume)
        self.future_commission_tier = self.process_future_rate_schedule(monthly_future_trade_volume)
        self.forex_commission_rate, self.forex_minimum_order_fee = self.process_forex_rate_schedule(monthly_forex_trade_amount_in_us_dollars)
        self.option_fee[Market.USA] = self.process_option_rate_schedule(monthly_options_trade_amount_in_contracts)
        self.crypto_commission_rate = self.process_crypto_rate_schedule(monthly_crypto_trade_amount_in_us_dollars)
        
    def process_equity_rate_schedule(self, monthly_equity_trade_volume: float) -> float:
        '''Determines which tier an account falls into based on the monthly trading volume of Equities (in shares)
        Args:
            monthly_equity_trade_volume: Monthly Equity shares traded
        Remarks:
            https://www.interactivebrokers.com/en/pricing/commissions-stocks.php?re=amer
        Return:
            Commission rate per each share of equity trade'''
        if monthly_equity_trade_volume <= 300000:
            return 0.0035
        elif monthly_equity_trade_volume <= 3000000:
            return 0.002
        elif monthly_equity_trade_volume <= 20000000:
            return 0.0015
        elif monthly_equity_trade_volume <= 100000000:
            return 0.001
        return 0.0005
        
    def process_future_rate_schedule(self, monthly_future_trade_volume: float) -> int:
        '''Determines which tier an account falls into based on the monthly trading volume of Futures (in contracts)
        Args:
            monthly_future_trade_volume: Monthly Future contracts traded
        Remarks:
            https://www.interactivebrokers.com/en/pricing/commissions-futures.php?re=amer
        Return:
            Commission tier of Future & FOP trades'''
        if monthly_future_trade_volume <= 1000:
            return 0
        elif monthly_future_trade_volume <= 10000:
            return 1
        elif monthly_future_trade_volume <= 20000:
            return 2
        return 3
        
    def process_forex_rate_schedule(self, monthly_forex_trade_amount_in_us_dollars: float) -> Tuple[float, float]:
        '''Determines which tier an account falls into based on the monthly trading volume of Forex
        Args:
            monthly_forex_trade_amount_in_us_dollars: Monthly FX dollar volume traded
        Remarks:
            https://www.interactivebrokers.com/en/pricing/commissions-spot-currencies.php?re=amer
        Return:
            Commission rate per each dollar of forex traded'''
        bp = 0.0001
        if monthly_forex_trade_amount_in_us_dollars <= 1000000000:
            return 0.2 * bp, 2
        elif monthly_forex_trade_amount_in_us_dollars <= 2000000000:
            return 0.15 * bp, 1.5
        elif monthly_forex_trade_amount_in_us_dollars <= 5000000000:
            return 0.1 * bp, 1.25
        return 0.08 * bp, 1
        
    def process_option_rate_schedule(self, monthly_options_trade_amount_in_contracts: float) -> Callable[[float, float], CashAmount]:
        '''Determines which tier an account falls into based on the monthly trading volume of Options
        Args:
            monthly_options_trade_amount_in_contracts: Monthly options contracts traded
        Remarks:
            https://www.interactivebrokers.com/en/pricing/commissions-options.php?re=amer
        Return:
            Function to calculate the commission rate per each each option contract traded'''
        if monthly_options_trade_amount_in_contracts <= 10000:
            return lambda order_size, premium: CashAmount(max(order_size * (0.65 if premium >= 0.1 else (0.25 if premium < 0.05 else 0.5)), 1), Currencies.USD)
        elif monthly_options_trade_amount_in_contracts <= 50000:
            return lambda order_size, premium: CashAmount(max(order_size * (0.5 if premium >= 0.05 else 0.25), 1), Currencies.USD)
        elif monthly_options_trade_amount_in_contracts <= 100000:
            return lambda order_size, _: CashAmount(max(order_size * 0.25, 1), Currencies.USD)
        return lambda order_size, _: CashAmount(max(order_size * 0.15, 1), Currencies.USD)
        
    def process_crypto_rate_schedule(self, monthly_crypto_trade_amount_in_us_dollars: float) -> float:
        '''Determines which tier an account falls into based on the monthly trading volume of Crypto
        Args:
            monthly_crypto_trade_amount_in_us_dollars: Monthly Crypto dollar volume traded (in USD)
        Remarks:
            https://www.interactivebrokers.com/en/pricing/commissions-cryptocurrencies.php?re=amer
        Return:
            Commission rate of crypto trades'''
        if monthly_crypto_trade_amount_in_us_dollars <= 100000:
            return 0.0018
        elif monthly_crypto_trade_amount_in_us_dollars <= 1000000:
            return 0.0015
        return 0.0012
    
    def get_order_fee(self, parameters: OrderFeeParameters) -> OrderFee:
        '''Gets the order fee associated with the specified order. This returns the cost of the transaction in the account currency
        Args:
            parameters: An "OrderFeeParameters" object containing the security and order
        Return:
            The cost of the order in units of the account currency'''
        order = parameters.order
        security = parameters.security
        
        # Update the monthly volume with filled quantity.
        self.update_month_volume()
        # Reset monthly trade value tracker when month rollover.
        if self.last_order_time.month != order.time.month and self.last_order_time != datetime.min:
            self.monthly_trade_volume = {key: 0 for key in self.monthly_trade_volume.keys()}
            self.volume_by_order = {}
        # Reprocess the rate schedule based on the current traded volume in various assets.
        self.reprocess_rate_schedule(self.monthly_trade_volume[SecurityType.EQUITY],
                                     self.monthly_trade_volume[SecurityType.FUTURE],
                                     self.monthly_trade_volume[SecurityType.FOREX],
                                     self.monthly_trade_volume[SecurityType.OPTION],
                                     self.monthly_trade_volume[SecurityType.CRYPTO])
        
        # Option exercise for equity options is free of charge
        if order.type == OrderType.OPTION_EXERCISE:
            # For Futures Options, contracts are charged the standard commission at expiration of the contract.
            # Read more here: https://www1.interactivebrokers.com/en/index.php?f=14718#trading-related-fees
            if order.symbol.id.security_type == SecurityType.OPTION:
                return OrderFee.ZERO
            
        quantity = order.absolute_quantity
        market = security.symbol.id.market
        
        if security.Type == SecurityType.FOREX:
            fee_result, fee_currency, trade_value = self.calculate_forex_fee(security, order, self.forex_commission_rate, self.forex_minimum_order_fee)
            
            # Update the monthly value traded
            self.volume_by_order[order] = trade_value
            
        elif security.Type == SecurityType.OPTION or security.Type == SecurityType.INDEX_OPTION:
            fee_result, fee_currency, order_price = self.calculate_option_fee(security, order, quantity, market, self.option_fee)
            # Regulatory Fee: Options Regulatory Fee (ORF) + FINRA Consolidated Audit Trail Fees
            regulatory = ((0.01915 + 0.0048) if market in self.option_exchanges_orf_fee else 0.0048) * quantity
            # Transaction Fees: SEC Transaction Fee + FINRA Trading Activity Fee (only charge on sell)
            transaction = 0.0000278 * abs(order.get_value(security)) + 0.00279 * quantity if order.direction == OrderDirection.SELL else 0
            # Clearing Fee
            clearing = min(0.02 * quantity, 55)
            
            fee_result += regulatory + transaction + clearing
            
            # Update the monthly value traded
            self.volume_by_order[order] = quantity * order_price
            
        elif security.Type == SecurityType.FUTURE or security.Type == SecurityType.FUTURE_OPTION:
            fee_result, fee_currency = self.calculate_future_fop_fee(security, quantity, market, self.future_fee)
            
            # Update the monthly value traded
            self.volume_by_order[order] = quantity
            
        elif security.Type == SecurityType.EQUITY:
            trade_value = abs(order.get_value(security))
            fee_result, fee_currency = self.calculate_equity_fee(quantity, trade_value, market, self.equity_commission_rate, self.equity_minimum_order_fee)
            
            # Tiered fee model has the below extra cost.
            # FINRA Trading Activity Fee only applies to sale of security.
            finra_trading_activity_fee = min(8.3, quantity * 0.000166) if order.direction == OrderDirection.SELL else 0
            # Regulatory Fees: SEC Transaction Fee + FINRA Trading Activity Fee + FINRA Consolidated Audit Trail Fees.
            regulatory = trade_value * 0.0000278 \
                + finra_trading_activity_fee \
                + quantity * 0.000048
            # Clearing Fee: NSCC, DTC Fees.
            clearing = min(0.0002 * quantity, 0.005 * trade_value)
            # Exchange related handling fees.
            exchange = self.get_equity_exchange_fee(order, security.primary_exchange, trade_value, fee_result)
            # FINRA Pass Through Fees.
            pass_through = min(8.3, fee_result * 0.00056)
            
            fee_result += regulatory + exchange + clearing + pass_through
            
            # Update the monthly value traded
            self.volume_by_order[order] = quantity
            
        elif security.Type == SecurityType.CFD:
            fee_result, fee_currency = self.calculate_cfd_fee(security, order)
            
        elif security.Type == SecurityType.CRYPTO:
            fee_result, fee_currency, trade_value = self.calculate_crypto_fee(security, order, self.crypto_commission_rate, self.crypto_minimum_order_fee)
            
            # Update the monthly value traded
            self.volume_by_order[order] = trade_value
            
        else:
            # unsupported security type
            raise ArgumentException(Messages.FeeModel.unsupported_security_type(security))
        
        self.last_order_time = order.time
        
        return OrderFee(CashAmount(fee_result, fee_currency))
    
    def calculate_forex_fee(self, security: Security, order: Order, forex_commission_rate: float, forex_minimum_order_fee: float) -> Tuple[float, str, float]:
        '''Calculate the transaction fee of a Forex order
        Return:
            The fee, fee currency, and traded value of the transaction'''
        # get the total order value in the account currency
        total_order_value = abs(order.get_value(security))
        base_fee = forex_commission_rate * total_order_value
        
        fee = max(forex_minimum_order_fee, base_fee)
        # IB Forex fees are all in USD
        return fee, Currencies.USD, total_order_value
    
    def calculate_option_fee(self, security: Security, order: Order, quantity: float, market: str, fee_ref: Dict[str, Callable[[float, float], CashAmount]]) -> Tuple[float, str, float]:
        '''Calculate the transaction fee of an Option order
        Return:
            The fee, fee currency, and traded value of the transaction'''
        option_commission_func = fee_ref.get(market)
        if not option_commission_func:
            raise Exception(Messages.InteractiveBrokersFeeModel.unexpected_option_market(market))
        # applying commission function to the order
        order_price = self.get_potential_order_price(order, security)
        option_fee = option_commission_func(quantity, order_price)
        
        return option_fee.amount, option_fee.currency, order_price * quantity
    
    def calculate_future_fop_fee(self, security: Security, quantity: float, market: str, fee_ref: Dict[str, Callable[[Security], CashAmount]]) -> Tuple[float, str]:
        '''Calculate the transaction fee of a Future or FOP order
        Return:
            The fee, and fee currency of the transaction'''
        # The futures options fee model is exactly the same as futures' fees on IB.
        if market == Market.GLOBEX or market == Market.NYMEX or market == Market.CBOT or market == Market.ICE \
        or market == Market.CFE or market == Market.COMEX or market == Market.CME or market == Market.NYSELIFFE:
            market = Market.USA
        
        fee_rate_per_contract_func = fee_ref.get(market)
        if not fee_rate_per_contract_func:
            raise Exception(Messages.InteractiveBrokersFeeModel.unexpected_future_market(market))
        
        fee_rate_per_contract = fee_rate_per_contract_func(security)
        fee = quantity * fee_rate_per_contract.amount
        return fee, fee_rate_per_contract.currency
    
    def calculate_equity_fee(self, quantity: float, trade_value: float, market: str, us_fee_rate: float, us_minimum_fee: float) -> Tuple[float, str]:
        '''Calculate the transaction fee of an Equity order
        Return:
            The fee, and fee currency of the transaction'''
        if market == Market.USA:
            equity_fee = EquityFee(Currencies.USD, fee_per_share=us_fee_rate, minimum_fee=us_minimum_fee, maximum_fee_rate=0.01)
        elif market == Market.India:
            equity_fee = EquityFee(Currencies.INR, fee_per_share=0.01, minimum_fee=6, maximum_fee_rate=20)
        else:
            raise Exception(Messages.InteractiveBrokersFeeModel.unexpected_equity_market(market))
        
        # Per share fees
        trade_fee = equity_fee.fee_per_share * quantity
        
        # Maximum Per Order: equity_fee.maximum_fee_rate
        # Minimum per order. $equity_fee.minimum_fee
        maximum_per_order = equity_fee.maximum_fee_rate * trade_value
        if trade_fee < equity_fee.minimum_fee:
            trade_fee = equity_fee.minimum_fee
        elif trade_fee > maximum_per_order:
            trade_fee = maximum_per_order
            
        return abs(trade_fee), equity_fee.currency
    
    def calculate_cfd_fee(self, security: Security, order: Order) -> Tuple[float, str, float]:
        '''Calculate the transaction fee of a CFD order
        Return:
            The fee, and fee currency of the transaction'''
        value = abs(order.get_value(security))
        fee = 0.00002 * value
        currency = security.quote_currency.symbol
        minimum_fee = 40 if currency == "JPY" else (10 if currency == "HKD" else 1)
        
        return max(fee, minimum_fee), currency
    
    def calculate_crypto_fee(self, security: Security, order: Order, crypto_commission_rate: float, crypto_minimum_order_fee: float) -> Tuple[float, str, float]:
        '''Calculate the transaction fee of a Crypto order
        Return:
            The fee, fee currency, and traded value of the transaction'''
        # get the total order value in the account currency
        total_order_value = abs(order.get_value(security))
        base_fee = crypto_commission_rate * total_order_value
        # 1% maximum fee
        fee = max(crypto_minimum_order_fee, min(0.01 * total_order_value, base_fee))
        # IB Crypto fees are all in USD
        return fee, Currencies.USD, total_order_value
    
    def united_states_future_fees(self, security: Security) -> CashAmount:
        if security.symbol.security_type == SecurityType.FUTURE:
            fees = self.usa_futures_fees
            exchange_fees = self.usa_futures_exchange_fees
            symbol = security.symbol.id.symbol
        elif security.symbol.security_type == SecurityType.FUTURE_OPTION:
            fees = self.usa_future_options_fees
            exchange_fees = self.usa_future_options_exchange_fees
            symbol = security.symbol.underlying.id.symbol
        else:
            raise ArgumentException(Messages.InteractiveBrokersFeeModel.united_states_future_fees_unsupported_security_type(security))
        
        fee_per_contract = fees.get(symbol)
        if not fee_per_contract:
            fee_per_contract = [0.85, 0.65, 0.45, 0.25]
        
        exchange_fee_per_contract = exchange_fees.get(symbol)
        if not exchange_fee_per_contract:
            exchange_fee_per_contract = 1.6
        
        # Add exchange fees + IBKR regulatory fee (0.02)
        return CashAmount(fee_per_contract[self.future_commission_tier] + exchange_fee_per_contract + 0.02, Currencies.USD)
    
    def hong_kong_future_fees(self, security: Security) -> CashAmount:
        '''See https://www.hkex.com.hk/Services/Rules-and-Forms-and-Fees/Fees/Listed-Derivatives/Trading/Transaction?sc_lang=en'''
        if security.symbol.id.symbol.lower() == "hsi":
            # IB fee + exchange fee
            return CashAmount(30 + 10, Currencies.HKD)
        
        currency = security.quote_currency.symbol
        if currency == Currencies.CNH:
            fee_per_contract = 13
        elif currency == Currencies.HKD:
            fee_per_contract = 20
        elif currency == Currencies.USD:
            fee_per_contract = 2.4
        else:
            raise ArgumentException(Messages.InteractiveBrokersFeeModel.hong_kong_future_fees_unexpected_quote_currency(security))
        
        # let's add a 50% extra charge for exchange fees
        return CashAmount(fee_per_contract * 1.5, currency)
    
    def eurex_future_fees(self, security: Security) -> CashAmount:
        if security.symbol.security_type == SecurityType.FUTURE:
            fees = self.eurex_futures_fees
            exchange_fees = self.eurex_futures_exchange_fees
            symbol = security.symbol.id.symbol
        else:
            raise ArgumentException(Messages.InteractiveBrokersFeeModel.eurex_future_fees_unsupported_security_type(security))
        
        fee_per_contract = fees.get(symbol)
        if not fee_per_contract:
            fee_per_contract = 1
        
        exchange_fee_per_contract = exchange_fees.get(symbol)
        if not exchange_fee_per_contract:
            exchange_fee_per_contract = 0
        
        # Add exchange fees + IBKR regulatory fee (0.02)
        return CashAmount(fee_per_contract + exchange_fee_per_contract + 0.02, Currencies.EUR)
    
    def get_equity_exchange_fee(self, order: Order, exchange: Exchange, trade_value: float, commission: float) -> float:
        '''Get the exchange fees of an Equity trade.
        Remarks:
            Refer to https://www.interactivebrokers.com/en/pricing/commissions-stocks.php, section United States - Third Party Fees.
        Return:
            Exchange fee of the Equity transaction'''
        penny_stock = order.price < 1
        if order.type == OrderType.MARKET_ON_OPEN:
            if exchange == Exchange.AMEX:
                return order.absolute_quantity * 0.0005
            elif exchange == Exchange.BATS:
                return order.absolute_quantity * 0.00075
            elif penny_stock:
                if exchange == Exchange.ARCA:
                    return trade_value * 0.001
                elif exchange == Exchange.NYSE:
                    return trade_value * 0.003 + commission * 0.000175
                return trade_value * 0.003
            else:
                if exchange == Exchange.NYSE:
                    return order.absolute_quantity * 0.001 + commission * 0.000175
                return order.absolute_quantity * 0.0015
        elif order.type == OrderType.MARKET_ON_CLOSE:
            if exchange == Exchange.AMEX:
                return order.absolute_quantity * 0.0005
            elif exchange == Exchange.BATS:
                return order.absolute_quantity * 0.001
            if penny_stock:
                if exchange == Exchange.ARCA:
                    return trade_value * 0.001
                elif exchange == Exchange.NYSE:
                    return trade_value * 0.003 + commission * 0.000175
                return trade_value * 0.003
            else:
                if exchange == Exchange.ARCA:
                    return order.absolute_quantity * 0.0012
                elif exchange == Exchange.NYSE:
                    return order.absolute_quantity * 0.001 + commission * 0.000175
                return order.absolute_quantity * 0.0015
        elif penny_stock:
            if exchange == Exchange.AMEX:
                return trade_value * 0.0025
            elif exchange == Exchange.NYSE:
                return trade_value * 0.003 + commission * 0.000175
            return trade_value * 0.003
        elif exchange == Exchange.NYSE:
            return order.absolute_quantity * 0.003 + commission * 0.000175
        return order.absolute_quantity * 0.003
    
    def get_potential_order_price(self, order: Order, security: Security) -> float:
        '''Approximates the order's price based on the order type'''
        if order.type == OrderType.TRAILING_STOP or order.type == OrderType.STOP_MARKET:
            return order.stop_price
        elif order.type == OrderType.COMBO_MARKET or order.type == OrderType.MARKET_ON_OPEN \
        or order.type == OrderType.MARKET_ON_CLOSE or order.type == OrderType.MARKET:
            if order.direction == OrderDirection.BUY:
                return security.bid_price
            return security.ask_price
        elif order.type == OrderType.COMBO_LEG_LIMIT or order.type == OrderType.STOP_LIMIT \
        or order.type == OrderType.LIMIT_IF_TOUCHED or order.type == OrderType.LIMIT:
            return order.limit_price
        elif order.type == OrderType.COMBO_LIMIT:
            return order.group_order_manager.limit_price
        return 0
    
    @property
    def usa_futures_fees(self) -> Dict[str, float]:
        '''Reference at https://www.interactivebrokers.com/en/pricing/commissions-futures.php?re=amer'''
        return {
            # Micro E-mini Futures
            "MYM": [0.25, 0.2, 0.15, 0.1], "M2K": [0.25, 0.2, 0.15, 0.1], "MES": [0.25, 0.2, 0.15, 0.1],
            "MNQ": [0.25, 0.2, 0.15, 0.1], "2YY": [0.25, 0.2, 0.15, 0.1], "5YY": [0.25, 0.2, 0.15, 0.1],
            "10Y": [0.25, 0.2, 0.15, 0.1], "30Y": [0.25, 0.2, 0.15, 0.1], "MCL": [0.25, 0.2, 0.15, 0.1],
            "MGC": [0.25, 0.2, 0.15, 0.1], "SIL": [0.25, 0.2, 0.15, 0.1],
            # Cryptocurrency Futures
            "BTC": [5, 5, 5, 5], "MBT": [2.25, 2.25, 2.25, 2.25], "ETH": [3, 3, 3, 3], "MET": [0.2, 0.2, 0.2, 0.2],
            # E-mini FX (currencies) Futures
            "E7": [0.5, 0.4, 0.3, 0.15], "J7": [0.5, 0.4, 0.3, 0.15],
            # Micro E-mini FX (currencies) Futures
            "M6E": [0.15, 0.12, 0.08, 0.05], "M6A": [0.15, 0.12, 0.08, 0.05], "M6B": [0.15, 0.12, 0.08, 0.05],
            "MCD": [0.15, 0.12, 0.08, 0.05], "MJY": [0.15, 0.12, 0.08, 0.05], "MSF": [0.15, 0.12, 0.08, 0.05],
            "M6J": [0.15, 0.12, 0.08, 0.05], "MIR": [0.15, 0.12, 0.08, 0.05], "M6C": [0.15, 0.12, 0.08, 0.05],
            "M6S": [0.15, 0.12, 0.08, 0.05], "MNH": [0.15, 0.12, 0.08, 0.05]
        }
    
    @property
    def usa_future_options_fees(self) -> Dict[str, float]:
        return {
            # Micro E-mini Future Options
            "MYM": [0.25, 0.2, 0.15, 0.1], "M2K": [0.25, 0.2, 0.15, 0.1], "MES": [0.25, 0.2, 0.15, 0.1],
            "MNQ": [0.25, 0.2, 0.15, 0.1], "2YY": [0.25, 0.2, 0.15, 0.1], "5YY": [0.25, 0.2, 0.15, 0.1],
            "10Y": [0.25, 0.2, 0.15, 0.1], "30Y": [0.25, 0.2, 0.15, 0.1], "MCL": [0.25, 0.2, 0.15, 0.1],
            "MGC": [0.25, 0.2, 0.15, 0.1], "SIL": [0.25, 0.2, 0.15, 0.1],
            # Cryptocurrency Future Options
            "BTC": [5, 5, 5, 5], "MBT": [1.25, 1.25, 1.25, 1.25], "ETH": [3, 3, 3, 3], "MET": [0.1, 0.1, 0.1, 0.1]
        }
    
    @property
    def usa_futures_exchange_fees(self) -> Dict[str, float]:
        return {
            # E-mini Futures
            "ES": 1.28, "NQ": 1.28, "YM": 1.28, "RTY": 1.28, "EMD": 1.28,
            # Micro E-mini Futures
            "MYM": 0.30, "M2K": 0.30, "MES": 0.30, "MNQ": 0.30, "2YY": 0.30, "5YY": 0.30, "10Y": 0.30,
            "30Y": 0.30, "MCL": 0.30, "MGC": 0.30, "SIL": 0.30,
            # Cryptocurrency Futures
            "BTC": 6, "MBT": 2.5, "ETH": 4, "MET": 0.20,
            # E-mini FX (currencies) Futures
            "E7": 0.85, "J7": 0.85,
            # Micro E-mini FX (currencies) Futures
            "M6E": 0.24, "M6A": 0.24, "M6B": 0.24, "MCD": 0.24, "MJY": 0.24, "MSF": 0.24, "M6J": 0.24,
            "MIR": 0.24, "M6C": 0.24, "M6S": 0.24, "MNH": 0.24
        }
    
    @property
    def usa_future_options_exchange_fees(self) -> Dict[str, float]:
        return {
            # E-mini Future Options
            "ES": 0.55, "NQ": 0.55, "YM": 0.55, "RTY": 0.55, "EMD": 0.55,
            # Micro E-mini Future Options
            "MYM": 0.20, "M2K": 0.20, "MES": 0.20, "MNQ": 0.20, "2YY": 0.20, "5YY": 0.20, "10Y": 0.20,
            "30Y": 0.20, "MCL": 0.20, "MGC": 0.20, "SIL": 0.20,
            # Cryptocurrency Future Options
            "BTC": 5, "MBT": 2.5, "ETH": 4, "MET": 0.20
        }
    
    @property
    def eurex_futures_fees(self) -> Dict[str, float]:
        '''Reference at https://www.interactivebrokers.com/en/pricing/commissions-futures-europe.php?re=europe'''
        return {
            # Futures
            "FESX": 1
        }
    
    @property
    def eurex_futures_exchange_fees(self) -> Dict[str, float]:
        return {
            # Futures
            "FESX": 0
        }
    
    
class EquityFee:
    '''Helper class to handle IB Equity fees'''
    
    def __init__(self, currency: str, fee_per_share: float, minimum_fee: float, maximum_fee_rate: float) -> None:
        self.currency = currency
        self.fee_per_share = fee_per_share
        self.minimum_fee = minimum_fee
        self.maximum_fee_rate = maximum_fee_rate
