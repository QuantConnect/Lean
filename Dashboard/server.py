"""
Control server for the LEAN dashboard.

Starts LEAN backtests as child processes, listens to each run's StreamingMessageHandler
(NetMQ PUSH socket) and relays the packets to the browser over a WebSocket.
"""
import asyncio
import json
import os
import re
import socket
import uuid
from datetime import datetime, timezone
from pathlib import Path

import zmq
import zmq.asyncio
from fastapi import FastAPI, HTTPException, WebSocket, WebSocketDisconnect
from fastapi.responses import FileResponse
from pydantic import BaseModel

HERE = Path(__file__).parent
LEAN_DIR = Path(os.environ.get("LEAN_DIR", "/Lean/Launcher/bin/Debug"))
DATA_DIR = os.environ.get("LEAN_DATA_DIR", "/Lean/Data")
RESULTS_DIR = Path(os.environ.get("RESULTS_DIR", "/results"))
ALGORITHMS_FILE = Path(os.environ.get("ALGORITHMS_FILE", "/Lean/algorithms.txt"))
ALGORITHM_SOURCE_DIR = os.environ.get("ALGORITHM_SOURCE_DIR")
MAX_LOGS = 1000

TERMINAL_STATUSES = {"Completed", "RuntimeError", "Stopped", "Error", "Liquidated", "Deleted"}

app = FastAPI()
zmq_context = zmq.asyncio.Context()
runs: dict[str, "Run"] = {}


def strip_json_comments(text: str) -> str:
    """LEAN's config.json allows // and /* */ comments and trailing commas; plain json does not"""
    out, i, n, in_string = [], 0, len(text), False
    while i < n:
        c = text[i]
        if in_string:
            out.append(c)
            if c == "\\":
                out.append(text[i + 1])
                i += 1
            elif c == '"':
                in_string = False
        elif c == '"':
            in_string = True
            out.append(c)
        elif text.startswith("//", i):
            while i < n and text[i] != "\n":
                i += 1
            continue
        elif text.startswith("/*", i):
            i = text.index("*/", i) + 2
            continue
        else:
            out.append(c)
        i += 1
    return re.sub(r",(\s*[}\]])", r"\1", "".join(out))


def load_algorithms() -> list[str]:
    if ALGORITHMS_FILE.exists():
        return sorted({line.strip() for line in ALGORITHMS_FILE.read_text().splitlines() if line.strip()})
    if ALGORITHM_SOURCE_DIR:
        pattern = re.compile(r"class (\w+) : QCAlgorithm\b")
        names = set()
        for path in Path(ALGORITHM_SOURCE_DIR).rglob("*.cs"):
            names.update(pattern.findall(path.read_text(errors="ignore")))
        return sorted(names)
    return []


ALGORITHMS = load_algorithms()


def free_port() -> int:
    with socket.socket() as s:
        s.bind(("127.0.0.1", 0))
        return s.getsockname()[1]


def now_iso() -> str:
    return datetime.now(timezone.utc).isoformat()


class BacktestRequest(BaseModel):
    algorithm: str
    start: str | None = None  # YYYY-MM-DD
    end: str | None = None
    cash: float | None = None
    parameters: dict[str, str] = {}


class Run:
    def __init__(self, request: BacktestRequest):
        self.id = datetime.now().strftime("%Y%m%d-%H%M%S-") + uuid.uuid4().hex[:6]
        self.request = request
        self.folder = RESULTS_DIR / self.id
        self.status = "Starting"
        self.progress = 0.0
        self.created = now_iso()
        self.finished: str | None = None
        self.statistics: dict = {}
        self.process: asyncio.subprocess.Process | None = None
        # Packets replayed to browsers that connect mid-run; result packets are collapsed to the latest final one
        self.results: list[dict] = []
        self.logs: list[dict] = []
        self.subscribers: set[asyncio.Queue] = set()

    def meta(self) -> dict:
        return {
            "id": self.id,
            "algorithm": self.request.algorithm,
            "request": self.request.model_dump(),
            "status": self.status,
            "progress": self.progress,
            "created": self.created,
            "finished": self.finished,
            "statistics": self.statistics,
        }

    def save_meta(self):
        (self.folder / "meta.json").write_text(json.dumps(self.meta(), indent=1))

    def publish(self, packet: dict):
        for queue in self.subscribers:
            queue.put_nowait(packet)

    def write_config(self, port: int) -> Path:
        config = json.loads(strip_json_comments((LEAN_DIR / "config.json").read_text()))
        r = self.request
        config.update({
            "environment": "backtesting",
            "algorithm-type-name": r.algorithm,
            "algorithm-language": "CSharp",
            "algorithm-location": "QuantConnect.Algorithm.CSharp.dll",
            "data-folder": DATA_DIR,
            "results-destination-folder": str(self.folder),
            "object-store-root": str(RESULTS_DIR / "storage"),
            "close-automatically": True,
            "messaging-handler": "QuantConnect.Messaging.StreamingMessageHandler",
            "desktop-http-port": str(port),
            "parameters": r.parameters,
        })
        # period-start / period-finish / cash-amount are read by Queues/JobQueue.cs
        if r.start:
            config["period-start"] = r.start.replace("-", "")
        if r.end:
            config["period-finish"] = r.end.replace("-", "")
        if r.cash:
            config["cash-amount"] = str(r.cash)
        path = self.folder / "config.json"
        path.write_text(json.dumps(config, indent=1))
        return path

    async def start(self):
        self.folder.mkdir(parents=True)
        port = free_port()
        config_path = self.write_config(port)

        # Connect before LEAN binds so no packets are lost; zmq retries the connection until the bind exists
        sock = zmq_context.socket(zmq.PULL)
        sock.connect(f"tcp://127.0.0.1:{port}")

        console = open(self.folder / "console.log", "wb")
        self.process = await asyncio.create_subprocess_exec(
            "dotnet", "QuantConnect.Lean.Launcher.dll", "--config", str(config_path),
            cwd=LEAN_DIR, stdout=console, stderr=asyncio.subprocess.STDOUT,
            # A fixed culture keeps dates Gregorian (a Thai locale yields Buddhist years) and keeps
            # LEAN's culture-sensitive type name matching working (it fails under C.UTF-8 on macOS)
            env={**os.environ, "LC_ALL": "en_US.UTF-8", "LANG": "en_US.UTF-8"},
        )
        self.status = "Running"
        self.save_meta()
        listener = asyncio.create_task(self.listen(sock))
        asyncio.create_task(self.wait_for_exit(listener, sock, console))

    async def listen(self, sock):
        while True:
            frames = await sock.recv_multipart()
            try:
                packet = json.loads(frames[0])
            except ValueError:
                continue
            self.handle_packet(packet)

    def handle_packet(self, packet: dict):
        kind = packet.get("Type")
        if kind == "BacktestNode":
            return  # the job packet carries the compiled algorithm bytes, nothing to show
        if kind == "AlgorithmStatus":
            self.status = packet.get("Status", self.status)
        elif kind == "BacktestResult":
            self.progress = float(packet.get("Progress") or 0)
            if self.progress >= 1:
                self.results = [packet]
                self.statistics = (packet.get("Results") or {}).get("Statistics") or {}
            else:
                self.results.append(packet)
        elif kind in ("Debug", "SystemDebug", "Log", "HandledError", "RuntimeError"):
            self.logs.append(packet)
            del self.logs[:-MAX_LOGS]
            if kind == "RuntimeError":
                self.status = "RuntimeError"
        self.publish(packet)

    async def wait_for_exit(self, listener, sock, console):
        code = await self.process.wait()
        # LEAN sends the final result just before exiting; give the socket a moment to drain
        await asyncio.sleep(1)
        listener.cancel()
        sock.close(linger=0)
        console.close()
        if self.status not in TERMINAL_STATUSES:
            self.status = "Completed" if code == 0 and self.progress >= 1 else "Error"
        self.finished = now_iso()
        self.save_meta()
        self.publish({"Type": "RunFinished", "Status": self.status, "ExitCode": code})

    def stop(self):
        if self.process and self.process.returncode is None:
            self.status = "Stopped"
            self.process.terminate()


def find_meta(run_id: str) -> dict:
    if run_id in runs:
        return runs[run_id].meta()
    path = RESULTS_DIR / run_id / "meta.json"
    if not re.fullmatch(r"[\w-]+", run_id) or not path.exists():
        raise HTTPException(404, "ไม่พบการรันนี้")
    return json.loads(path.read_text())


@app.get("/")
def index():
    return FileResponse(HERE / "index.html")


@app.get("/api/algorithms")
def algorithms():
    return ALGORITHMS


@app.get("/api/backtests")
def list_backtests():
    metas = {run.id: run.meta() for run in runs.values()}
    for path in RESULTS_DIR.glob("*/meta.json"):
        if path.parent.name not in metas:
            try:
                metas[path.parent.name] = json.loads(path.read_text())
            except ValueError:
                pass
    return sorted(metas.values(), key=lambda m: m["created"], reverse=True)


@app.post("/api/backtests")
async def create_backtest(request: BacktestRequest):
    if ALGORITHMS and request.algorithm not in ALGORITHMS:
        raise HTTPException(400, f"ไม่รู้จักกลยุทธ์ {request.algorithm}")
    for value in (request.start, request.end):
        if value and not re.fullmatch(r"\d{4}-\d{2}-\d{2}", value):
            raise HTTPException(400, "วันที่ต้องอยู่ในรูปแบบ YYYY-MM-DD")
    run = Run(request)
    runs[run.id] = run
    await run.start()
    return run.meta()


@app.post("/api/backtests/{run_id}/stop")
def stop_backtest(run_id: str):
    if run_id not in runs:
        raise HTTPException(404, "การรันนี้ไม่ได้ทำงานอยู่")
    runs[run_id].stop()
    return runs[run_id].meta()


@app.get("/api/backtests/{run_id}/result")
def backtest_result(run_id: str):
    meta = find_meta(run_id)
    path = RESULTS_DIR / run_id / f"{meta['algorithm']}.json"
    if not path.exists():
        raise HTTPException(404, "ยังไม่มีไฟล์ผลลัพธ์")
    return FileResponse(path)


@app.websocket("/ws/backtests/{run_id}")
async def stream_backtest(ws: WebSocket, run_id: str):
    await ws.accept()
    run = runs.get(run_id)
    if run is None:
        await ws.close(code=4404)
        return
    queue: asyncio.Queue = asyncio.Queue()
    # Snapshot the replay buffer and subscribe in the same step so no packet is missed or duplicated
    backlog = [*run.logs, *run.results]
    run.subscribers.add(queue)
    try:
        await ws.send_json({"Type": "RunInfo", **run.meta()})
        for packet in backlog:
            await ws.send_json(packet)
        if run.finished:
            await ws.send_json({"Type": "RunFinished", "Status": run.status})
            return
        while True:
            packet = await queue.get()
            await ws.send_json(packet)
            if packet.get("Type") == "RunFinished":
                return
    except WebSocketDisconnect:
        pass
    finally:
        run.subscribers.discard(queue)
