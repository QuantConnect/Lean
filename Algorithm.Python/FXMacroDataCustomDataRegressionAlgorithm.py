import json
import os
from datetime import datetime, timedelta, timezone
from urllib.parse import urlencode

from AlgorithmImports import *
from QuantConnect.Data.UniverseSelection import BaseDataCollection


_FXMACRODATA_API_BASE = "https://api.fxmacrodata.com/v1"
_FXMACRODATA_PAGE_LIMIT = 100


def _collect_paginated_rows(fetch_page):
    """Collect all list-endpoint rows using limit/offset pagination."""
    offset = 0
    rows = []

    while True:
        payload = fetch_page(_FXMACRODATA_PAGE_LIMIT, offset)

        page_rows = payload.get("data", [])
        rows.extend(page_rows)

        pagination = payload.get("pagination", {})

        if not pagination.get("has_more", False):
            return rows

        returned_count = int(
            pagination.get("returned_count", len(page_rows))
        )

        if returned_count <= 0:
            raise ValueError(
                "FXMacroData pagination reported has_more=true "
                "without returning any rows"
            )

        current_offset = int(
            pagination.get("offset", offset)
        )

        offset = current_offset + returned_count


def _download_fxmacrodata_pages(
    download,
    endpoint,
    parameters=None
):
    """Download and merge every page from a public FXMacroData list endpoint."""

    base_parameters = dict(parameters or {})

    def fetch_page(limit, offset):
        query_parameters = {
            **base_parameters,
            "limit": limit,
            "offset": offset
        }

        query = urlencode(query_parameters)

        response = download(
            f"{_FXMACRODATA_API_BASE}/{endpoint}?{query}"
        )

        return json.loads(response)

    return _collect_paginated_rows(fetch_page)


class FXMacroDataCustomDataRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self._use_public_source = (
            (self.get_parameter("use-public-source") or "").lower()
            == "true"
        )

        if self._use_public_source:
            today = datetime.now(timezone.utc).date()
            end_date = today - timedelta(days=1)
            start_date = end_date - timedelta(days=89)

            # Run the smoke backtest after the requested history window.
            # Starting at start_date would make later releases future data
            # during initialize(), so LEAN would correctly truncate them.
            self.set_start_date(
                end_date.year,
                end_date.month,
                end_date.day
            )
            self.set_end_date(
                end_date.year,
                end_date.month,
                end_date.day
            )

            self._history_start = datetime(
                start_date.year,
                start_date.month,
                start_date.day
            )
            self._history_end = datetime(
                end_date.year,
                end_date.month,
                end_date.day
            )

            self._configure_public_sources(
                start_date,
                end_date
            )
        else:
            # CI/regression mode deliberately uses committed fixtures so it
            # never depends on the rolling public 90-day window or network.
            FXMacroDataMacroIndicator.use_fixture_data = True
            FXMacroDataMacroIndicator.object_store_key = None

            FXMacroDataReleaseCalendar.use_fixture_data = True
            FXMacroDataReleaseCalendar.object_store_key = None

            self.set_start_date(2026, 10, 1)
            self.set_end_date(2026, 10, 2)

            self._verify_pagination_helper()

        self._inflation = self.add_data(
            FXMacroDataMacroIndicator,
            "USD_INFLATION",
            Resolution.DAILY,
            TimeZones.UTC
        ).symbol

        self._calendar = self.add_data(
            FXMacroDataReleaseCalendar,
            "USD_RELEASE_CALENDAR",
            Resolution.DAILY,
            TimeZones.UTC
        ).symbol

        if self._use_public_source:
            self._verify_public_history()
        else:
            self._verify_indicator_history()
            self._verify_calendar_history()

    def _configure_public_sources(
        self,
        start_date,
        end_date
    ):
        date_parameters = {
            "start_date": start_date.isoformat(),
            "end_date": end_date.isoformat()
        }

        sources = [
            (
                FXMacroDataMacroIndicator,
                "announcements/usd/inflation",
                "fxmacrodata/public/usd_inflation.json"
            ),
            (
                FXMacroDataReleaseCalendar,
                "calendar/usd",
                "fxmacrodata/public/usd_calendar.json"
            )
        ]

        for data_type, endpoint, object_store_key in sources:
            rows = _download_fxmacrodata_pages(
                self.download,
                endpoint,
                date_parameters
            )

            if not rows:
                raise AssertionError(
                    f"FXMacroData returned no rows for {endpoint}"
                )

            self.object_store.save(
                object_store_key,
                json.dumps(
                    {"data": rows},
                    separators=(",", ":")
                )
            )

            data_type.use_fixture_data = False
            data_type.object_store_key = object_store_key

    def _verify_public_history(self):
        inflation = self.history(
            self._inflation,
            self._history_start,
            self._history_end,
            Resolution.DAILY
        )

        calendar = self.history(
            self._calendar,
            self._history_start,
            self._history_end,
            Resolution.DAILY
        )

        if inflation.empty:
            raise AssertionError(
                "Public FXMacroData inflation history was empty"
            )

        if calendar.empty:
            raise AssertionError(
                "Public FXMacroData calendar history was empty"
            )

    def _verify_pagination_helper(self):
        calls = []

        pages = {
            0: {
                "data": [
                    {"id": 1},
                    {"id": 2}
                ],
                "pagination": {
                    "limit": 100,
                    "offset": 0,
                    "returned_count": 2,
                    "has_more": True
                }
            },
            2: {
                "data": [
                    {"id": 3}
                ],
                "pagination": {
                    "limit": 100,
                    "offset": 2,
                    "returned_count": 1,
                    "has_more": False
                }
            }
        }

        def fetch_page(limit, offset):
            calls.append((limit, offset))
            return pages[offset]

        rows = _collect_paginated_rows(fetch_page)

        if [row["id"] for row in rows] != [1, 2, 3]:
            raise AssertionError(
                f"Unexpected pagination rows: {rows}"
            )

        if calls != [(100, 0), (100, 2)]:
            raise AssertionError(
                f"Unexpected pagination requests: {calls}"
            )

    def _verify_indicator_history(self):
        history = self.history(
            self._inflation,
            datetime(2026, 8, 1),
            datetime(2026, 9, 12),
            Resolution.DAILY
        ).droplevel(0).sort_index()

        if len(history.index) != 2:
            raise AssertionError(
                f"Expected 2 FXMacroData inflation observations, "
                f"got {len(history.index)}"
            )

        expected_times = [
            datetime(2026, 8, 12, 12, 30),
            datetime(2026, 9, 11, 12, 30)
        ]
        actual_times = [
            timestamp.to_pydatetime()
            for timestamp in history.index
        ]

        if actual_times != expected_times:
            raise AssertionError(
                f"Unexpected inflation announcement times: {actual_times}"
            )

    def _verify_calendar_history(self):
        history = self.history(
            self._calendar,
            datetime(2026, 8, 1),
            datetime(2026, 10, 1),
            Resolution.DAILY
        ).droplevel(0).sort_index()

        if history.empty:
            raise AssertionError(
                "Expected FXMacroData calendar fixture to contain rows"
            )

        required_columns = {
            "announcement_datetime",
            "announcement_datetime_utc",
            "market_tier",
            "name",
            "reference_date",
            "release",
            "source"
        }

        missing = required_columns.difference(history.columns)
        if missing:
            raise AssertionError(
                f"Calendar history is missing fields: {sorted(missing)}"
            )


class FXMacroDataMacroIndicator(PythonData):

    use_fixture_data = False
    object_store_key = None

    def get_source(
        self,
        config: SubscriptionDataConfig,
        date: datetime,
        is_live_mode: bool
    ) -> SubscriptionDataSource:

        if self.use_fixture_data:
            source = os.path.join(
                Globals.data_folder,
                "fxmacrodata",
                "usd_inflation.json"
            )

            return SubscriptionDataSource(
                source,
                SubscriptionTransportMedium.LOCAL_FILE,
                FileFormat.UNFOLDING_COLLECTION
            )

        if not self.object_store_key:
            raise ValueError(
                "FXMacroData public source was not initialized"
            )

        return SubscriptionDataSource(
            self.object_store_key,
            SubscriptionTransportMedium.OBJECT_STORE,
            FileFormat.UNFOLDING_COLLECTION
        )

    def reader(
        self,
        config: SubscriptionDataConfig,
        line: str,
        date: datetime,
        is_live_mode: bool
    ) -> BaseData:

        if not line.strip():
            return None

        payload = json.loads(line)
        points = []

        for row in payload["data"]:
            announcement_datetime = row.get("announcement_datetime")

            if announcement_datetime is None:
                continue

            point = FXMacroDataMacroIndicator()
            point.symbol = config.symbol

            # Critical point-in-time rule:
            # use the release timestamp, NOT row["date"].
            point.time = datetime.fromtimestamp(
                announcement_datetime,
                timezone.utc
            ).replace(tzinfo=None)

            point.value = float(row["val"])

            # row["date"] is the economic reference period.
            point["reference_date"] = row.get("date")
            point["announcement_id"] = row.get("announcement_id")
            point["source"] = row.get("source")
            point["source_url"] = row.get("source_url")
            point["announcement_datetime"] = announcement_datetime
            point["announcement_datetime_utc"] = (
                point.time
                .replace(tzinfo=timezone.utc)
                .isoformat()
            )

            points.append(point)

        points.sort(key=lambda point: point.time)

        return BaseDataCollection(
            date,
            config.symbol,
            points
        )

class FXMacroDataReleaseCalendar(PythonData):

    use_fixture_data = False
    object_store_key = None

    def get_source(
        self,
        config: SubscriptionDataConfig,
        date: datetime,
        is_live_mode: bool
    ) -> SubscriptionDataSource:

        if self.use_fixture_data:
            source = os.path.join(
                Globals.data_folder,
                "fxmacrodata",
                "usd_calendar.json"
            )

            return SubscriptionDataSource(
                source,
                SubscriptionTransportMedium.LOCAL_FILE,
                FileFormat.UNFOLDING_COLLECTION
            )

        if not self.object_store_key:
            raise ValueError(
                "FXMacroData public source was not initialized"
            )

        return SubscriptionDataSource(
            self.object_store_key,
            SubscriptionTransportMedium.OBJECT_STORE,
            FileFormat.UNFOLDING_COLLECTION
        )

    def reader(
        self,
        config: SubscriptionDataConfig,
        line: str,
        date: datetime,
        is_live_mode: bool
    ) -> BaseData:

        if not line.strip():
            return None

        payload = json.loads(line)
        points = []

        for row in payload["data"]:
            announcement_datetime = row.get("announcement_datetime")

            if announcement_datetime is None:
                continue

            point = FXMacroDataReleaseCalendar()
            point.symbol = config.symbol

            point.time = datetime.fromtimestamp(
                announcement_datetime,
                timezone.utc
            ).replace(tzinfo=None)

            # Calendar events don't have a natural numeric observation value.
            point.value = 0.0

            point["announcement_datetime"] = announcement_datetime
            point["announcement_datetime_utc"] = row.get(
                "announcement_datetime_utc"
            )
            point["calendar_event_id"] = row.get("calendar_event_id")
            point["reference_date"] = row.get("date")
            point["release"] = row.get("release")
            point["name"] = row.get("name")
            point["market_tier"] = row.get("market_tier")
            point["source"] = row.get("source")
            point["source_url"] = row.get("source_url")

            points.append(point)

        points.sort(key=lambda point: point.time)

        return BaseDataCollection(
            date,
            config.symbol,
            points
        )
