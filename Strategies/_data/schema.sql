-- ============================================================
-- TRADE DATABASE SCHEMA
-- Database: trades.db (SQLite)
-- Version: 1.0
-- Created: 2024-12-09
-- ============================================================

-- ============================================================
-- TABLE: trades
-- Primary trade record. One row per trade.
-- ============================================================

CREATE TABLE IF NOT EXISTS trades (
    -- Identifiers
    trade_id            TEXT PRIMARY KEY,       -- e.g., "NVDA-2024-12-08-001"
    strategy            TEXT NOT NULL,          -- e.g., "uw-alert-straddle"
    symbol              TEXT NOT NULL,          -- e.g., "NVDA"

    -- Entry
    entry_date          DATE NOT NULL,
    entry_time          TIME,
    entry_price         REAL NOT NULL,          -- Straddle/position cost
    entry_underlying    REAL,                   -- Underlying price at entry
    entry_strike        REAL,                   -- Strike price
    entry_dte           INTEGER NOT NULL,       -- Days to expiration at entry
    entry_expiry        DATE,                   -- Option expiration date

    -- Exit
    exit_date           DATE,
    exit_time           TIME,
    exit_price          REAL,
    exit_underlying     REAL,
    exit_dte            INTEGER,
    exit_reason         TEXT,                   -- MOMENTUM_EXIT, TIME_STOP, STOP_LOSS, etc.

    -- Results
    pnl_dollars         REAL,
    pnl_percent         REAL,
    days_held           INTEGER,

    -- Entry Conditions (what triggered this trade)
    entry_alert_count   INTEGER,                -- UW alert density
    entry_ivr           REAL,                   -- IV Rank (0-100)
    entry_iv            REAL,                   -- Implied volatility
    entry_hv            REAL,                   -- Historical volatility
    entry_vix           REAL,
    entry_rsi           REAL,
    entry_net_gex       REAL,
    entry_gex_change    REAL,                   -- GEX change from prior day
    entry_gex_direction TEXT,                   -- "falling" or "rising"
    entry_volume_ratio  REAL,                   -- Volume vs average

    -- Sizing Factors Used
    base_position_usd   REAL,
    conviction_mult     REAL,                   -- 1.0, 1.25, or 1.5
    gex_magnitude_mult  REAL,                   -- 0.7 to 2.5
    gex_direction_mult  REAL,                   -- 0.7 or 1.5
    final_position_usd  REAL,                   -- Final $ amount
    contracts           INTEGER,                -- Number of contracts

    -- Position Details
    call_symbol         TEXT,                   -- Option contract symbol
    put_symbol          TEXT,
    call_entry_price    REAL,
    put_entry_price     REAL,
    call_exit_price     REAL,
    put_exit_price      REAL,

    -- Metadata
    created_at          TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at          TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    notes               TEXT,

    -- Indexes for common queries
    UNIQUE(strategy, symbol, entry_date, entry_time)
);

CREATE INDEX IF NOT EXISTS idx_trades_strategy ON trades(strategy);
CREATE INDEX IF NOT EXISTS idx_trades_symbol ON trades(symbol);
CREATE INDEX IF NOT EXISTS idx_trades_entry_date ON trades(entry_date);
CREATE INDEX IF NOT EXISTS idx_trades_exit_reason ON trades(exit_reason);


-- ============================================================
-- TABLE: trade_snapshots
-- Daily data while trade is open. One row per trade per day.
-- ============================================================

CREATE TABLE IF NOT EXISTS trade_snapshots (
    -- Keys
    id                  INTEGER PRIMARY KEY AUTOINCREMENT,
    trade_id            TEXT NOT NULL,
    snapshot_date       DATE NOT NULL,
    day_number          INTEGER NOT NULL,       -- Day 1, 2, 3... of trade

    -- Position State
    straddle_price      REAL,
    call_price          REAL,
    put_price           REAL,
    position_value      REAL,                   -- Current value
    position_pnl_dollars REAL,
    position_pnl_pct    REAL,
    peak_pnl_pct        REAL,                   -- Highest P&L so far
    pnl_from_peak_pct   REAL,                   -- Current - Peak (for momentum exit)

    -- Greeks
    delta               REAL,
    gamma               REAL,
    theta               REAL,
    vega                REAL,
    iv_call             REAL,
    iv_put              REAL,

    -- Underlying
    underlying_price    REAL,
    underlying_open     REAL,
    underlying_high     REAL,
    underlying_low      REAL,
    underlying_close    REAL,
    underlying_change_pct REAL,                 -- Daily % change
    underlying_volume   INTEGER,

    -- Volatility Metrics
    iv_avg              REAL,
    ivr                 REAL,                   -- IV Rank (0-100)
    iv_percentile       REAL,                   -- IV Percentile
    hv_10               REAL,
    hv_20               REAL,
    hv_30               REAL,
    iv_hv_spread        REAL,                   -- IV - HV (richness)
    vix                 REAL,
    vix_change          REAL,

    -- Volume & Flow
    call_volume         INTEGER,
    put_volume          INTEGER,
    total_option_volume INTEGER,
    put_call_ratio      REAL,
    call_oi             INTEGER,
    put_oi              INTEGER,
    total_oi            INTEGER,
    oi_change           INTEGER,

    -- GEX Data
    net_gex             REAL,
    gex_change          REAL,                   -- vs previous day
    gex_direction       TEXT,                   -- "falling" or "rising"
    call_gex            REAL,
    put_gex             REAL,

    -- Technicals
    rsi_14              REAL,
    sma_5               REAL,
    sma_10              REAL,
    sma_20              REAL,
    sma_50              REAL,
    sma_200             REAL,
    ema_9               REAL,
    ema_21              REAL,
    atr_14              REAL,
    bbands_upper        REAL,
    bbands_lower        REAL,

    -- UW Alerts (if tracking)
    alert_count         INTEGER,
    bullish_alerts      INTEGER,
    bearish_alerts      INTEGER,

    -- DTE
    current_dte         INTEGER,

    FOREIGN KEY (trade_id) REFERENCES trades(trade_id),
    UNIQUE(trade_id, snapshot_date)
);

CREATE INDEX IF NOT EXISTS idx_snapshots_trade_id ON trade_snapshots(trade_id);
CREATE INDEX IF NOT EXISTS idx_snapshots_date ON trade_snapshots(snapshot_date);


-- ============================================================
-- TABLE: pre_trade_context
-- 14 trading days before entry. Market context leading to trade.
-- ============================================================

CREATE TABLE IF NOT EXISTS pre_trade_context (
    -- Keys
    id                  INTEGER PRIMARY KEY AUTOINCREMENT,
    trade_id            TEXT NOT NULL,
    context_date        DATE NOT NULL,
    days_before_entry   INTEGER NOT NULL,       -- -14, -13, ... -1

    -- Underlying
    underlying_price    REAL,
    underlying_change_pct REAL,
    underlying_volume   INTEGER,

    -- Volatility
    iv_avg              REAL,
    ivr                 REAL,
    hv_20               REAL,
    iv_hv_spread        REAL,
    vix                 REAL,

    -- GEX
    net_gex             REAL,
    gex_change          REAL,
    gex_direction       TEXT,

    -- Volume & Flow
    call_volume         INTEGER,
    put_volume          INTEGER,
    total_option_volume INTEGER,
    put_call_ratio      REAL,
    total_oi            INTEGER,
    oi_change           INTEGER,

    -- Technicals
    rsi_14              REAL,
    sma_20              REAL,
    sma_50              REAL,
    price_vs_sma20_pct  REAL,

    -- UW Alerts
    alert_count         INTEGER,

    FOREIGN KEY (trade_id) REFERENCES trades(trade_id),
    UNIQUE(trade_id, context_date)
);

CREATE INDEX IF NOT EXISTS idx_pre_trade_trade_id ON pre_trade_context(trade_id);


-- ============================================================
-- TABLE: trade_analytics
-- Computed metrics per trade. One row per trade.
-- ============================================================

CREATE TABLE IF NOT EXISTS trade_analytics (
    -- Keys
    trade_id            TEXT PRIMARY KEY,

    -- Time Metrics
    dte_at_entry        INTEGER,
    dte_at_exit         INTEGER,
    days_held           INTEGER,

    -- P&L Journey
    max_peak_profit_pct REAL,                   -- Highest P&L during trade
    max_peak_profit_day INTEGER,                -- Day number when peak occurred
    max_drawdown_pct    REAL,                   -- Lowest P&L during trade
    max_drawdown_day    INTEGER,
    profit_capture_rate REAL,                   -- exit_pnl / max_peak (0-1+)

    -- Recovery Analysis (thresholds touched during trade)
    touched_minus_5     INTEGER DEFAULT 0,      -- 1 if touched, 0 if not
    touched_minus_10    INTEGER DEFAULT 0,
    touched_minus_15    INTEGER DEFAULT 0,
    touched_minus_20    INTEGER DEFAULT 0,
    touched_plus_5      INTEGER DEFAULT 0,
    touched_plus_10     INTEGER DEFAULT 0,
    touched_plus_15     INTEGER DEFAULT 0,
    touched_plus_20     INTEGER DEFAULT 0,
    touched_plus_25     INTEGER DEFAULT 0,
    touched_plus_30     INTEGER DEFAULT 0,
    touched_plus_40     INTEGER DEFAULT 0,
    touched_plus_50     INTEGER DEFAULT 0,

    -- Days to Threshold (NULL if never reached)
    days_to_minus_10    INTEGER,
    days_to_plus_10     INTEGER,
    days_to_plus_20     INTEGER,
    days_to_plus_25     INTEGER,
    days_to_plus_30     INTEGER,

    -- Recovery from Drawdown
    recovered_from_minus_5  INTEGER DEFAULT 0,  -- 1 if went -5% then positive
    recovered_from_minus_10 INTEGER DEFAULT 0,

    -- Classification
    winner              INTEGER,                -- 1 if pnl > 0, 0 otherwise
    big_winner          INTEGER,                -- 1 if pnl > 25%
    big_loser           INTEGER,                -- 1 if pnl < -15%

    -- Calculated at
    calculated_at       TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    FOREIGN KEY (trade_id) REFERENCES trades(trade_id)
);


-- ============================================================
-- TABLE: strategy_stats
-- Aggregate statistics. One row per strategy per time period.
-- ============================================================

CREATE TABLE IF NOT EXISTS strategy_stats (
    -- Keys
    id                  INTEGER PRIMARY KEY AUTOINCREMENT,
    strategy            TEXT NOT NULL,
    period_type         TEXT NOT NULL,          -- "all_time", "ytd", "monthly", "quarterly"
    period_label        TEXT,                   -- e.g., "2024-11", "Q4-2024"
    period_start        DATE,
    period_end          DATE,

    -- Trade Counts
    total_trades        INTEGER,
    winners             INTEGER,
    losers              INTEGER,
    win_rate            REAL,

    -- Streak Info
    current_streak      INTEGER,                -- Positive = wins, negative = losses
    max_win_streak      INTEGER,
    max_lose_streak     INTEGER,

    -- Returns
    total_pnl_dollars   REAL,
    total_return_pct    REAL,
    avg_return_pct      REAL,
    median_return_pct   REAL,
    std_return_pct      REAL,
    avg_win_pct         REAL,
    avg_loss_pct        REAL,
    best_trade_pct      REAL,
    worst_trade_pct     REAL,

    -- Risk Metrics
    sharpe_ratio        REAL,
    sortino_ratio       REAL,
    calmar_ratio        REAL,
    max_drawdown_pct    REAL,
    avg_drawdown_pct    REAL,
    max_drawdown_days   INTEGER,
    ulcer_index         REAL,

    -- Time Metrics
    avg_days_held       REAL,
    avg_days_winners    REAL,
    avg_days_losers     REAL,
    avg_days_to_peak    REAL,
    avg_profit_capture  REAL,

    -- Statistical Significance
    t_statistic         REAL,
    p_value             REAL,
    confidence_95_low   REAL,
    confidence_95_high  REAL,
    probabilistic_sharpe REAL,                  -- Probability Sharpe > 0

    -- Profit Metrics
    profit_factor       REAL,                   -- gross_wins / gross_losses
    expectancy          REAL,                   -- avg $ per trade
    expectancy_pct      REAL,                   -- avg % per trade

    -- Exit Reason Breakdown
    exits_momentum      INTEGER,
    exits_time_stop     INTEGER,
    exits_stop_loss     INTEGER,
    exits_profit_target INTEGER,
    exits_other         INTEGER,

    -- Metadata
    calculated_at       TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    UNIQUE(strategy, period_type, period_start, period_end)
);

CREATE INDEX IF NOT EXISTS idx_stats_strategy ON strategy_stats(strategy);
CREATE INDEX IF NOT EXISTS idx_stats_period ON strategy_stats(period_type, period_start);


-- ============================================================
-- TABLE: monthly_performance
-- Dedicated monthly tracking for quick access.
-- ============================================================

CREATE TABLE IF NOT EXISTS monthly_performance (
    -- Keys
    id                  INTEGER PRIMARY KEY AUTOINCREMENT,
    strategy            TEXT NOT NULL,
    year                INTEGER NOT NULL,
    month               INTEGER NOT NULL,       -- 1-12

    -- Performance
    pnl_dollars         REAL,
    pnl_percent         REAL,
    cumulative_pct      REAL,                   -- YTD cumulative

    -- Trade Stats
    trades_opened       INTEGER,
    trades_closed       INTEGER,
    winners             INTEGER,
    losers              INTEGER,
    win_rate            REAL,

    -- Best/Worst
    best_trade_pct      REAL,
    worst_trade_pct     REAL,
    max_drawdown_pct    REAL,

    -- Streak
    month_result        TEXT,                   -- "W" or "L"
    streak_length       INTEGER,                -- Running streak at end of month

    -- Calculated
    calculated_at       TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    UNIQUE(strategy, year, month)
);

CREATE INDEX IF NOT EXISTS idx_monthly_strategy ON monthly_performance(strategy);


-- ============================================================
-- VIEW: v_trade_summary
-- Quick trade summary view.
-- ============================================================

CREATE VIEW IF NOT EXISTS v_trade_summary AS
SELECT
    t.trade_id,
    t.strategy,
    t.symbol,
    t.entry_date,
    t.exit_date,
    t.days_held,
    t.entry_price,
    t.exit_price,
    t.pnl_percent,
    t.pnl_dollars,
    t.exit_reason,
    t.entry_ivr,
    t.entry_net_gex,
    t.entry_gex_direction,
    t.entry_alert_count,
    a.max_peak_profit_pct,
    a.max_drawdown_pct,
    a.profit_capture_rate,
    a.winner
FROM trades t
LEFT JOIN trade_analytics a ON t.trade_id = a.trade_id
ORDER BY t.entry_date DESC;


-- ============================================================
-- VIEW: v_open_positions
-- Currently open positions.
-- ============================================================

CREATE VIEW IF NOT EXISTS v_open_positions AS
SELECT
    trade_id,
    strategy,
    symbol,
    entry_date,
    entry_price,
    entry_dte,
    julianday('now') - julianday(entry_date) as days_held,
    entry_ivr,
    entry_net_gex,
    final_position_usd,
    notes
FROM trades
WHERE exit_date IS NULL
ORDER BY entry_date;


-- ============================================================
-- INITIAL DATA / SETTINGS
-- ============================================================

-- You can add any initial configuration here if needed.


-- ============================================================
-- END OF SCHEMA
-- ============================================================
