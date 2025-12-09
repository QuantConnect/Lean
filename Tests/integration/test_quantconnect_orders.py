"""
Integration Test: QuantConnect Orders API
==========================================
Tests the canonical QC Orders API pattern from solutions_learned.jsonl

REQUIRED CI ENVIRONMENT VARIABLES:
- QC_USER_ID: QuantConnect user ID
- QC_API_TOKEN: QuantConnect API token
- QC_PROJECT_ID: Project ID to test against
- QC_BACKTEST_ID: A known backtest ID with orders

These must be set to STAGING credentials, not production.
Do NOT commit real credentials. Use GitHub Secrets or similar.

Run locally:
    QC_USER_ID=xxx QC_API_TOKEN=xxx QC_PROJECT_ID=xxx QC_BACKTEST_ID=xxx pytest tests/integration/test_quantconnect_orders.py -v
"""

import os
import hashlib
import time
import pytest
import requests


def get_qc_auth():
    """Generate QC API authentication tuple."""
    user_id = os.environ.get("QC_USER_ID")
    api_token = os.environ.get("QC_API_TOKEN")

    if not user_id or not api_token:
        pytest.skip("QC_USER_ID and QC_API_TOKEN environment variables required")

    timestamp = str(int(time.time()))
    hashed = hashlib.sha256(f"{api_token}:{timestamp}".encode('utf-8')).hexdigest()
    return (user_id, hashed), {"Timestamp": timestamp}


class TestQuantConnectOrdersAPI:
    """Test suite for QC Orders API canonical pattern."""

    @pytest.fixture
    def qc_credentials(self):
        """Get QC credentials from environment."""
        return {
            "project_id": os.environ.get("QC_PROJECT_ID"),
            "backtest_id": os.environ.get("QC_BACKTEST_ID"),
        }

    def test_orders_api_returns_valid_response(self, qc_credentials):
        """
        Test: Orders API returns valid JSON response
        Canonical: GET /api/v2/backtests/orders/read with proper auth
        """
        if not qc_credentials["project_id"] or not qc_credentials["backtest_id"]:
            pytest.skip("QC_PROJECT_ID and QC_BACKTEST_ID required")

        auth, headers = get_qc_auth()

        params = {
            "projectId": qc_credentials["project_id"],
            "backtestId": qc_credentials["backtest_id"],
            "start": 0,
            "end": 10
        }

        resp = requests.get(
            "https://www.quantconnect.com/api/v2/backtests/orders/read",
            params=params,
            auth=auth,
            headers=headers
        )

        assert resp.status_code == 200, f"Expected 200, got {resp.status_code}"

        data = resp.json()
        assert "orders" in data, "Response must contain 'orders' key"
        assert isinstance(data["orders"], list), "'orders' must be a list"

    def test_orders_api_pagination_works(self, qc_credentials):
        """
        Test: Pagination with start/end parameters works correctly
        """
        if not qc_credentials["project_id"] or not qc_credentials["backtest_id"]:
            pytest.skip("QC_PROJECT_ID and QC_BACKTEST_ID required")

        auth, headers = get_qc_auth()

        # First page
        params_page1 = {
            "projectId": qc_credentials["project_id"],
            "backtestId": qc_credentials["backtest_id"],
            "start": 0,
            "end": 5
        }
        resp1 = requests.get(
            "https://www.quantconnect.com/api/v2/backtests/orders/read",
            params=params_page1,
            auth=auth,
            headers=headers
        )

        # Second page
        params_page2 = {
            "projectId": qc_credentials["project_id"],
            "backtestId": qc_credentials["backtest_id"],
            "start": 5,
            "end": 10
        }
        resp2 = requests.get(
            "https://www.quantconnect.com/api/v2/backtests/orders/read",
            params=params_page2,
            auth=auth,
            headers=headers
        )

        assert resp1.status_code == 200
        assert resp2.status_code == 200

        orders1 = resp1.json().get("orders", [])
        orders2 = resp2.json().get("orders", [])

        # If there are enough orders, pages should be different
        if len(orders1) >= 5 and len(orders2) > 0:
            # First order of page 2 should not be in page 1
            if orders1 and orders2:
                assert orders1[0].get("id") != orders2[0].get("id"), "Pagination should return different orders"

    def test_orders_api_invalid_auth_fails(self, qc_credentials):
        """
        Test: Invalid authentication returns error (not 200)
        """
        if not qc_credentials["project_id"] or not qc_credentials["backtest_id"]:
            pytest.skip("QC_PROJECT_ID and QC_BACKTEST_ID required")

        # Use invalid auth
        invalid_auth = ("invalid_user", "invalid_hash")
        headers = {"Timestamp": str(int(time.time()))}

        params = {
            "projectId": qc_credentials["project_id"],
            "backtestId": qc_credentials["backtest_id"],
            "start": 0,
            "end": 10
        }

        resp = requests.get(
            "https://www.quantconnect.com/api/v2/backtests/orders/read",
            params=params,
            auth=invalid_auth,
            headers=headers
        )

        # Should either return non-200 or success=false
        if resp.status_code == 200:
            data = resp.json()
            assert data.get("success") == False, "Invalid auth should fail"


class TestLogsAPIConfirmation:
    """
    Confirm that logs API does NOT return backtest logs.
    This documents the known limitation from Learning #1.
    """

    def test_logs_api_returns_empty_or_no_logs(self):
        """
        Test: Confirm /api/v2/backtests/read does not return useful log data
        This is a DOCUMENTATION test - it confirms the known limitation.
        """
        # This test exists to document that we KNOW this doesn't work
        # If QC ever fixes this, the test will fail and we can update the learning

        project_id = os.environ.get("QC_PROJECT_ID")
        backtest_id = os.environ.get("QC_BACKTEST_ID")

        if not project_id or not backtest_id:
            pytest.skip("QC credentials required")

        auth, headers = get_qc_auth()

        resp = requests.get(
            "https://www.quantconnect.com/api/v2/backtests/read",
            params={"projectId": project_id, "backtestId": backtest_id},
            auth=auth,
            headers=headers
        )

        if resp.status_code == 200:
            data = resp.json()
            backtest = data.get("backtest", {})
            logs = backtest.get("logs", [])

            # Document current behavior - logs are typically empty or minimal
            # This test passes if logs is empty or very short (not full log output)
            assert len(logs) < 100, f"Unexpected: logs API returned {len(logs)} entries. Learning #1 may need update."
