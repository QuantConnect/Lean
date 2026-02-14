#!/usr/bin/env python3
"""
Kernel bridge for LEAN research.

Runs inside the QuantConnect Docker container. Bootstraps the LEAN engine
via PythonNet/CLR, creates a persistent IPython InteractiveShell, and
implements a JSON-line protocol over stdin/stdout for code execution
with rich output capture (matplotlib charts, pandas HTML tables, etc.).

Protocol:
  stdin  -> one JSON object per line: {"action": "execute", "code": "..."}
  stdout <- one JSON object per line with results (see execute_cell)
"""

import base64
import io
import json
import os
import sys
import traceback

# ---------------------------------------------------------------------------
# Redirect stdout to stderr at the OS file-descriptor level during
# initialization.  C# / PythonNet writes directly to fd 1, bypassing
# Python's sys.stdout, so a Python-level redirect is not sufficient.
# ---------------------------------------------------------------------------
sys.stdout.flush()
_saved_stdout_fd = os.dup(1)   # duplicate real stdout fd
os.dup2(2, 1)                  # fd 1 now points to stderr

LEAN_ROOT = os.environ.get("WORK", "/Lean/Launcher/bin/Debug")

# Ensure LEAN_ROOT is on the Python path (should already be via PYTHONPATH
# in the Docker image, but be explicit).
if LEAN_ROOT not in sys.path:
    sys.path.insert(0, LEAN_ROOT)

# ---------------------------------------------------------------------------
# Bootstrap PythonNet / CLR  (mirrors Research/start.py)
# ---------------------------------------------------------------------------
try:
    import clr_loader
    from pythonnet import set_runtime

    set_runtime(clr_loader.get_coreclr(
        runtime_config=os.path.join(
            LEAN_ROOT,
            "QuantConnect.Lean.Launcher.runtimeconfig.json",
        )
    ))

    from AlgorithmImports import *          # noqa: F401,F403

    AddReference("Fasterflect")

    Config.Reset()
    Initializer.Start()
    algorithmHandlers = Initializer.GetAlgorithmHandlers(researchMode=True)
    PythonInitializer.Initialize(False)

    _init_ok = True
    _init_error = None

except Exception as exc:
    _init_ok = False
    _init_error = traceback.format_exc()

# ---------------------------------------------------------------------------
# Matplotlib — use non-interactive Agg backend
# ---------------------------------------------------------------------------
try:
    import matplotlib
    matplotlib.use("Agg")
    import matplotlib.pyplot as plt
except ImportError:
    plt = None

# ---------------------------------------------------------------------------
# IPython InteractiveShell for persistent state
# ---------------------------------------------------------------------------
from IPython.core.interactiveshell import InteractiveShell
from IPython.utils.capture import capture_output

shell = InteractiveShell.instance()

# Pre-populate the shell namespace with commonly used objects
if _init_ok:
    shell.run_cell("from AlgorithmImports import *", store_history=False, silent=True)
    shell.run_cell("from QuantConnect.Research import *", store_history=False, silent=True)
    shell.run_cell("import numpy as np", store_history=False, silent=True)
    shell.run_cell("import pandas as pd", store_history=False, silent=True)
    shell.run_cell(
        "import matplotlib; matplotlib.use('Agg'); import matplotlib.pyplot as plt",
        store_history=False,
        silent=True,
    )
    shell.run_cell("qb = QuantBook()", store_history=False, silent=True)

# ---------------------------------------------------------------------------
# Protocol helpers
# ---------------------------------------------------------------------------

# Restore real stdout at the fd level and create a dedicated file object for
# the JSON-line protocol.  This ensures no C# or Python init output leaks in.
sys.stdout.flush()
os.dup2(_saved_stdout_fd, 1)   # restore fd 1 to real stdout
os.close(_saved_stdout_fd)
# Build a fresh, line-buffered file object on fd 1 (closefd=False so we
# don't accidentally close stdout if this object is GC'd).
_protocol_out = open(1, "w", buffering=1, closefd=False)


def send(obj: dict) -> None:
    """Write a JSON line to stdout."""
    _protocol_out.write(json.dumps(obj, default=str) + "\n")
    _protocol_out.flush()


def execute_cell(code: str) -> dict:
    """Run *code* in the persistent shell and return structured results."""
    response = {
        "stdout": "",
        "stderr": "",
        "success": True,
        "result": None,
        "displays": [],
        "error": None,
    }

    try:
        # Redirect fd 1 to stderr while executing user code so any C#
        # trace output doesn't corrupt the JSON-line protocol.
        _protocol_out.flush()
        _exec_save = os.dup(1)
        os.dup2(2, 1)

        with capture_output() as captured:
            result = shell.run_cell(code)

        response["stdout"] = captured.stdout
        response["stderr"] = captured.stderr
        response["success"] = result.success

        # Last-expression value
        if result.result is not None:
            response["result"] = repr(result.result)

        # Error info
        if not result.success:
            if result.error_in_exec:
                response["error"] = "".join(
                    traceback.format_exception(
                        type(result.error_in_exec),
                        result.error_in_exec,
                        result.error_in_exec.__traceback__,
                    )
                )
            elif result.error_before_exec:
                response["error"] = str(result.error_before_exec)

        # Rich display outputs captured by IPython (e.g. display(HTML(...)))
        for output in captured.outputs:
            display_item = {}
            if hasattr(output, "data"):
                for mime_type, data in output.data.items():
                    if mime_type == "image/png":
                        if isinstance(data, bytes):
                            display_item[mime_type] = base64.b64encode(data).decode()
                        else:
                            display_item[mime_type] = data
                    elif mime_type in ("text/html", "text/plain", "text/latex"):
                        display_item[mime_type] = str(data)[:50_000]
            if display_item:
                response["displays"].append(display_item)

        # Auto-capture any open matplotlib figures
        if plt is not None:
            for fig_num in plt.get_fignums():
                fig = plt.figure(fig_num)
                buf = io.BytesIO()
                fig.savefig(buf, format="png", dpi=150, bbox_inches="tight")
                buf.seek(0)
                response["displays"].append(
                    {"image/png": base64.b64encode(buf.read()).decode()}
                )
            plt.close("all")

    except Exception:
        response["success"] = False
        response["error"] = traceback.format_exc()
    finally:
        # Restore fd 1 so send() can write the JSON response
        os.dup2(_exec_save, 1)
        os.close(_exec_save)

    return response


# ---------------------------------------------------------------------------
# Main loop
# ---------------------------------------------------------------------------

# Signal ready (or init failure)
if _init_ok:
    send({"ready": True})
else:
    send({"ready": False, "error": _init_error})

for line in sys.stdin:
    line = line.strip()
    if not line:
        continue

    try:
        cmd = json.loads(line)
    except json.JSONDecodeError as exc:
        send({"error": f"Invalid JSON: {exc}"})
        continue

    action = cmd.get("action", "execute")

    if action == "execute":
        send(execute_cell(cmd.get("code", "")))
    elif action == "ping":
        send({"pong": True})
    else:
        send({"error": f"Unknown action: {action}"})
