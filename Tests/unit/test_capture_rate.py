"""
Unit Test: Capture Rate Formula Edge Cases
==========================================
Tests Learning #3: Capture Rate Formula Edge Case

The capture rate formula (exit_pnl / peak_pnl) * 100 can produce
garbage values when peak is small positive and exit is negative.

This test validates the bounds checking logic.

Run:
    pytest tests/unit/test_capture_rate.py -v
"""

import pytest


def calculate_capture_rate(exit_pnl: float, peak_pnl: float, min_peak: float = 0.05) -> float | None:
    """
    Calculate capture rate with proper bounds checking.

    Args:
        exit_pnl: Exit P&L as decimal (e.g., 0.15 for 15%)
        peak_pnl: Peak P&L as decimal (e.g., 0.25 for 25%)
        min_peak: Minimum peak threshold to calculate capture rate (default 5%)

    Returns:
        Capture rate as percentage, or None if peak too small
    """
    # Learning #3: Skip calculation if peak is too small
    if peak_pnl <= min_peak:
        return None

    # Standard capture rate calculation
    capture_rate = (exit_pnl / peak_pnl) * 100
    return capture_rate


class TestCaptureRateFormula:
    """Test capture rate calculation with edge cases."""

    def test_normal_case_positive_capture(self):
        """Normal case: exit at 15%, peak was 25% = 60% capture"""
        result = calculate_capture_rate(0.15, 0.25)
        assert result == pytest.approx(60.0)

    def test_normal_case_full_capture(self):
        """Exit at peak = 100% capture"""
        result = calculate_capture_rate(0.25, 0.25)
        assert result == pytest.approx(100.0)

    def test_normal_case_partial_capture(self):
        """Exit at 20%, peak was 30% = 66.67% capture"""
        result = calculate_capture_rate(0.20, 0.30)
        assert result == pytest.approx(66.67, rel=0.01)

    def test_edge_case_small_peak_returns_none(self):
        """Learning #3: Small peak should return None, not garbage value"""
        # Peak was only 2%, exit at -10%
        # Without bounds check: (-0.10 / 0.02) * 100 = -500% (garbage)
        result = calculate_capture_rate(-0.10, 0.02)
        assert result is None

    def test_edge_case_very_small_peak(self):
        """Very small peak (0.5%) should return None"""
        result = calculate_capture_rate(-0.05, 0.005)
        assert result is None

    def test_edge_case_zero_peak(self):
        """Zero peak should return None (avoid division by zero)"""
        result = calculate_capture_rate(0.10, 0.0)
        assert result is None

    def test_edge_case_negative_peak(self):
        """Negative peak (never profitable) should return None"""
        result = calculate_capture_rate(-0.15, -0.05)
        assert result is None

    def test_negative_exit_with_valid_peak(self):
        """Negative exit with valid peak = negative capture (gave back profits)"""
        # Peak was 30%, exited at -5% = -16.67% capture (gave back all + more)
        result = calculate_capture_rate(-0.05, 0.30)
        assert result == pytest.approx(-16.67, rel=0.01)

    def test_custom_min_peak_threshold(self):
        """Custom minimum peak threshold"""
        # With default 5% threshold, 4% peak returns None
        assert calculate_capture_rate(0.02, 0.04) is None

        # With 3% threshold, 4% peak is valid
        result = calculate_capture_rate(0.02, 0.04, min_peak=0.03)
        assert result == pytest.approx(50.0)

    def test_boundary_exactly_at_min_peak(self):
        """Boundary: peak exactly at min_peak threshold"""
        # Peak at exactly 5% (default threshold) should return None
        result = calculate_capture_rate(0.03, 0.05, min_peak=0.05)
        assert result is None

        # Peak just above threshold should work
        result = calculate_capture_rate(0.03, 0.051, min_peak=0.05)
        assert result is not None


class TestCaptureRateRealScenarios:
    """Test with realistic trading scenarios."""

    def test_scenario_time_stop_loser(self):
        """Trade hit time stop, never profitable"""
        # Peak was 3%, exited at -12%
        result = calculate_capture_rate(-0.12, 0.03)
        assert result is None  # Peak too small to calculate meaningful capture

    def test_scenario_profit_target_winner(self):
        """Trade hit profit target"""
        # Peak was 35%, exited at 28%
        result = calculate_capture_rate(0.28, 0.35)
        assert result == pytest.approx(80.0)

    def test_scenario_momentum_exit(self):
        """Trade momentum exit (dropped from peak)"""
        # Peak was 42%, exited at 38%
        result = calculate_capture_rate(0.38, 0.42)
        assert result == pytest.approx(90.48, rel=0.01)

    def test_scenario_gave_back_all_profits(self):
        """Trade gave back all profits and more"""
        # Peak was 25%, exited at -8%
        result = calculate_capture_rate(-0.08, 0.25)
        assert result == pytest.approx(-32.0)
