#!/usr/bin/env python3
"""
MCP server for LEAN quantitative research.

Runs on the host machine. Manages a Docker container running the LEAN
research image with a persistent IPython kernel (bridge.py). Exposes
MCP tools that let Claude Code execute QuantConnect Python code
interactively.

Environment variables (all optional):
  LEAN_RESEARCH_IMAGE  Docker image  (default: lean-cli/research:cascadelabs-lean)
  LEAN_DATA_DIR        Host path to LEAN market data
  LEAN_CONTAINER_NAME  Container name (default: lean_research_mcp)
"""

from __future__ import annotations

import atexit
import base64
import json
import os
import subprocess
import sys
import tempfile
import threading
from pathlib import Path

from mcp.server.fastmcp import FastMCP

# ---------------------------------------------------------------------------
# Configuration
# ---------------------------------------------------------------------------

LEAN_IMAGE = os.environ.get(
    "LEAN_RESEARCH_IMAGE", "lean-cli/research:cascadelabs-lean"
)
LEAN_DATA_DIR = os.environ.get("LEAN_DATA_DIR", "")
CONTAINER_NAME = os.environ.get("LEAN_CONTAINER_NAME", "lean_research_mcp")
# bridge.py is baked into the Docker image at this path
BRIDGE_PATH_IN_CONTAINER = "/Lean/Launcher/bin/Debug/bridge.py"


def _detect_container_runtime() -> str:
    """Return the container CLI command (podman or docker)."""
    for cmd in ["podman", "docker"]:
        try:
            subprocess.run([cmd, "--version"], capture_output=True, check=True)
            return cmd
        except (FileNotFoundError, subprocess.CalledProcessError):
            continue
    raise RuntimeError("Neither podman nor docker found in PATH")


CONTAINER_CMD = _detect_container_runtime()


def _load_lean_credentials() -> dict:
    """Load credentials from ~/.lean/credentials (lean-cli credential store)."""
    creds_path = Path.home() / ".lean" / "credentials"
    if creds_path.is_file():
        try:
            with open(creds_path) as f:
                return json.load(f)
        except Exception:
            pass
    return {}


LEAN_CREDENTIALS = _load_lean_credentials()


# ---------------------------------------------------------------------------
# Market-specific data provider configurations
# ---------------------------------------------------------------------------

MARKET_PROVIDERS = {
    "polygon": {
        "label": "Equities (Polygon)",
        "data-provider":
            "QuantConnect.Lean.Engine.DataFeeds.DownloaderDataProvider",
        "data-downloader":
            "QuantConnect.Lean.DataSource.Polygon.PolygonDataDownloader",
        "history-provider":
            "QuantConnect.Lean.DataSource.Polygon.PolygonDataProvider",
        "map-file-provider":
            "QuantConnect.Lean.DataSource.Polygon.PolygonMapFileProvider",
        "factor-file-provider":
            "QuantConnect.Lean.DataSource.Polygon.PolygonFactorFileProvider",
    },
    "kalshi": {
        "label": "Prediction Markets (Kalshi)",
        "data-provider":
            "QuantConnect.Lean.Engine.DataFeeds.DownloaderDataProvider",
        "data-downloader":
            "QuantConnect.Lean.DataSource.CascadeKalshiData.CascadeKalshiDataDownloader",
        "history-provider":
            "QuantConnect.Lean.DataSource.CascadeKalshiData.CascadeKalshiDataProvider",
        "map-file-provider":
            "QuantConnect.Data.Auxiliary.LocalDiskMapFileProvider",
        "factor-file-provider":
            "QuantConnect.Data.Auxiliary.LocalDiskFactorFileProvider",
    },
    "hyper": {
        "label": "Crypto Futures (Hyperliquid)",
        "data-provider":
            "QuantConnect.Lean.Engine.DataFeeds.DownloaderDataProvider",
        "data-downloader":
            "QuantConnect.Lean.DataSource.CascadeHyperliquid.HyperliquidDataDownloader",
        "history-provider":
            "QuantConnect.Lean.DataSource.CascadeHyperliquid.HyperliquidHistoryProvider",
        "map-file-provider":
            "QuantConnect.Data.Auxiliary.LocalDiskMapFileProvider",
        "factor-file-provider":
            "QuantConnect.Data.Auxiliary.LocalDiskFactorFileProvider",
    },
}

DEFAULT_MARKET = "polygon"


def _detect_data_directory() -> str:
    """Resolve the LEAN data directory, matching lean-cli's logic.

    Priority:
      1. LEAN_DATA_DIR environment variable (explicit override)
      2. ``data-folder`` from lean.json (resolved relative to lean.json location,
         exactly as lean-cli does via LeanConfigManager.get_data_directory)
      3. Well-known fallback paths
    """
    # 1. Explicit env override
    if LEAN_DATA_DIR:
        return LEAN_DATA_DIR

    # 2. Resolve from lean.json via ~/.lean/cache known paths
    cache_path = Path.home() / ".lean" / "cache"
    if cache_path.is_file():
        try:
            cache = json.loads(cache_path.read_text())
            for cfg_str in cache.get("known-lean-config-paths", []):
                cfg_path = Path(cfg_str)
                if cfg_path.is_file():
                    cfg = json.loads(cfg_path.read_text())
                    data_folder = cfg.get("data-folder", "")
                    if data_folder:
                        resolved = cfg_path.parent / data_folder
                        if resolved.is_dir():
                            return str(resolved)
        except Exception:
            pass

    # 3. Fallback candidates
    for candidate in [
        Path.home() / "code" / "Lean" / "Data",
        Path.home() / ".lean" / "data",
        Path.home() / ".lean-data",
    ]:
        if candidate.is_dir():
            return str(candidate)

    return ""


LEAN_DATA_DIR = _detect_data_directory()

# Directory for saving chart images locally
CHART_DIR = Path(tempfile.mkdtemp(prefix="lean_charts_"))

# ---------------------------------------------------------------------------
# Kernel bridge — manages Docker container + subprocess communication
# ---------------------------------------------------------------------------


class KernelBridge:
    """Manages the Docker container and bridge.py subprocess."""

    def __init__(self) -> None:
        self._proc: subprocess.Popen | None = None
        self._lock = threading.Lock()
        self._config_path: str | None = None
        self._market: str = DEFAULT_MARKET

    # -- lifecycle ----------------------------------------------------------

    def ensure_running(self) -> None:
        with self._lock:
            if self._proc is not None and self._proc.poll() is None:
                return
            self._start()

    def _start(self) -> None:
        # Clean up any stale container with the same name
        subprocess.run(
            [CONTAINER_CMD, "rm", "-f", CONTAINER_NAME],
            capture_output=True,
        )

        # Write LEAN config with the selected market's data provider.
        # Credentials are loaded from ~/.lean/credentials (lean-cli store).
        provider_cfg = MARKET_PROVIDERS[self._market]
        config = {
            "data-folder": "/Lean/Data",
            "composer-dll-directory": "/Lean/Launcher/bin/Debug",
            "algorithm-language": "Python",
            "messaging-handler": "QuantConnect.Messaging.Messaging",
            "job-queue-handler": "QuantConnect.Queues.JobQueue",
            "api-handler": "QuantConnect.Api.Api",
            "log-handler": "QuantConnect.Logging.CompositeLogHandler",
            "research-object-store-name": "research",
            # Market-specific data providers
            "data-provider": provider_cfg["data-provider"],
            "data-downloader": provider_cfg["data-downloader"],
            "history-provider": provider_cfg["history-provider"],
            "map-file-provider": provider_cfg["map-file-provider"],
            "factor-file-provider": provider_cfg["factor-file-provider"],
        }
        # Inject all lean-cli credentials into the container config
        config.update(LEAN_CREDENTIALS)
        cfg_fd, self._config_path = tempfile.mkstemp(suffix=".json", prefix="lean_cfg_")
        with os.fdopen(cfg_fd, "w") as f:
            json.dump(config, f)

        # Build the docker run command
        cmd: list[str] = [
            CONTAINER_CMD, "run",
            "-i",                   # Keep stdin open for the protocol
            "--rm",                 # Remove container on exit
            "--name", CONTAINER_NAME,
            "--memory", "8g",
            "-v", f"{self._config_path}:/Lean/Launcher/bin/Debug/config.json:ro",
        ]

        # Mount market data if available
        if LEAN_DATA_DIR and Path(LEAN_DATA_DIR).is_dir():
            cmd.extend(["-v", f"{LEAN_DATA_DIR}:/Lean/Data:ro"])

        cmd.extend([
            "-w", "/Lean/Launcher/bin/Debug",
            LEAN_IMAGE,
            "python", "-u", BRIDGE_PATH_IN_CONTAINER,
        ])

        self._proc = subprocess.Popen(
            cmd,
            stdin=subprocess.PIPE,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
        )

        # Wait for the bridge to signal readiness
        raw = self._proc.stdout.readline()
        if not raw:
            err = self._read_stderr()
            raise RuntimeError(
                f"Bridge process exited immediately.\nstderr: {err}"
            )

        msg = json.loads(raw)
        if not msg.get("ready"):
            raise RuntimeError(
                f"Bridge initialization failed:\n{msg.get('error', 'unknown')}"
            )

    def _read_stderr(self) -> str:
        """Non-blocking read of whatever is on stderr."""
        try:
            return self._proc.stderr.read(8192).decode(errors="replace")
        except Exception:
            return "(stderr unavailable)"

    def execute(self, code: str) -> dict:
        """Send code to the bridge and return the result dict."""
        self.ensure_running()

        payload = json.dumps({"action": "execute", "code": code}) + "\n"
        try:
            self._proc.stdin.write(payload.encode())
            self._proc.stdin.flush()
        except (BrokenPipeError, OSError) as exc:
            raise RuntimeError(f"Bridge process died: {exc}") from exc

        raw = self._proc.stdout.readline()
        if not raw:
            err = self._read_stderr()
            raise RuntimeError(f"Bridge returned no output.\nstderr: {err}")

        return json.loads(raw)

    def restart(self, market: str | None = None) -> None:
        """Kill current bridge and start fresh, optionally switching market."""
        with self._lock:
            if market:
                self._market = market
            self._kill()
            self._start()

    def stop(self) -> None:
        """Clean shutdown."""
        self._kill()
        if self._config_path:
            try:
                os.unlink(self._config_path)
            except OSError:
                pass

    def _kill(self) -> None:
        if self._proc and self._proc.poll() is None:
            self._proc.terminate()
            try:
                self._proc.wait(timeout=10)
            except subprocess.TimeoutExpired:
                self._proc.kill()
        self._proc = None
        subprocess.run(
            [CONTAINER_CMD, "rm", "-f", CONTAINER_NAME],
            capture_output=True,
        )


# Global bridge instance
bridge = KernelBridge()
atexit.register(bridge.stop)

# ---------------------------------------------------------------------------
# MCP server
# ---------------------------------------------------------------------------

mcp = FastMCP("lean-research")

_chart_counter = 0


@mcp.tool()
def execute_code(code: str) -> str:
    """Execute Python code in the persistent LEAN research kernel.

    The kernel has a pre-initialized QuantBook as `qb`, plus numpy (np),
    pandas (pd), and matplotlib.pyplot (plt). All QuantConnect namespaces
    are imported. Variables persist across calls.

    Matplotlib figures are auto-captured and saved as PNG chart images.
    """
    global _chart_counter

    try:
        result = bridge.execute(code)
    except RuntimeError as exc:
        return f"BRIDGE ERROR: {exc}"

    parts: list[str] = []

    if result.get("stdout"):
        parts.append(result["stdout"].rstrip())

    if result.get("error"):
        parts.append(f"ERROR:\n{result['error']}")

    if result.get("result"):
        parts.append(f"=> {result['result']}")

    for display in result.get("displays", []):
        if "image/png" in display:
            chart_path = CHART_DIR / f"chart_{_chart_counter}.png"
            chart_path.write_bytes(base64.b64decode(display["image/png"]))
            parts.append(f"[Chart saved: {chart_path}]")
            _chart_counter += 1
        if "text/html" in display:
            html = display["text/html"]
            if len(html) > 5000:
                html = html[:5000] + "\n... (truncated)"
            parts.append(html)
        elif "text/plain" in display:
            parts.append(display["text/plain"])

    return "\n\n".join(parts) if parts else "(no output)"


@mcp.tool()
def reset_kernel(market: str = "") -> str:
    """Restart the LEAN research kernel with a fresh QuantBook.

    Use when kernel state is corrupted or you want a clean environment.
    All variables will be lost.

    Args:
        market: Data provider market. One of "polygon" (equities, default),
                "kalshi" (prediction markets), or "hyper" (Hyperliquid crypto).
                If empty, keeps the current market setting.
    """
    market = market.strip().lower()
    if market and market not in MARKET_PROVIDERS:
        return (
            f"Unknown market '{market}'. "
            f"Valid options: {', '.join(MARKET_PROVIDERS.keys())}"
        )
    try:
        bridge.restart(market=market or None)
    except RuntimeError as exc:
        return f"RESTART FAILED: {exc}"
    label = MARKET_PROVIDERS[bridge._market]["label"]
    return f"Kernel restarted with {label} data provider. Fresh QuantBook available as `qb`."


@mcp.tool()
def kernel_status() -> str:
    """Check whether the LEAN research kernel is running."""
    if bridge._proc and bridge._proc.poll() is None:
        label = MARKET_PROVIDERS[bridge._market]["label"]
        return (
            f"Kernel is running (container: {CONTAINER_NAME}, "
            f"image: {LEAN_IMAGE}, market: {label})"
        )
    return "Kernel is not running. It will start on the next execute_code call."


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------

if __name__ == "__main__":
    mcp.run(transport="stdio")
