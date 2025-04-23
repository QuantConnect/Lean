import os
import sys
import time
import tempfile
from pathlib import Path
from subprocess import run
from multiprocessing import Pool, Lock, freeze_support

target_files = []
lock = None
start_time = time.time()

# to resolve imports of other algorithms
expanded_envs = os.environ.copy()
expanded_envs["MYPYPATH"] = os.path.join(os.getcwd(), "./Algorithm.Python")

def init_pool(l):
    global lock
    lock = l

def log(message: str):
    print(message)
    with open('log.txt', "a") as file:
        file.write(message)

def sync_log(message: str):
    with lock:
        log(message)

for subdir, dirs, files_in_folder in os.walk("./Algorithm.Python"):
    for file_name in files_in_folder:
        file_path = subdir + os.sep + file_name
        if file_path.endswith(".py"):
            target_files.append(file_path)
target_files.sort()

def adjust_file_contents(target_file: str):
    try:
        file = Path(target_file)
        file_content = file.read_text(encoding='utf-8')
        adjusted_import = 'from AlgorithmImports import *;from datetime import date, time, datetime, timedelta;import pandas as pd;import numpy as np;import math;import json;import os;'

        tmp_file = tempfile.NamedTemporaryFile(prefix=f"{file.name}_", delete=False)
        Path(tmp_file.name).write_text("# mypy: disable-error-code=\"no-redef\"\n" + file_content.replace("from AlgorithmImports import *", adjusted_import), encoding='utf-8')
        return tmp_file
    except:
        import traceback
        sync_log(f"{target_file} failed An exception occurred: {traceback.format_exc()}")
        return None

def run_syntax_check(target_file: str):
    tmp_file = adjust_file_contents(target_file)
    if not tmp_file:
        return False

    try:
        algorithm_result = run([sys.executable, "-m", "mypy", "--skip-cache-mtime-checks", "--skip-version-check", "--show-error-codes",
            "--no-error-summary", "--no-color-output", "--ignore-missing-imports", "--check-untyped-defs",  tmp_file.name], capture_output=True, text=True, env=expanded_envs)

        output = ''
        if algorithm_result.stderr:
            output += algorithm_result.stderr
        if algorithm_result.stdout:
            output += algorithm_result.stdout

        filtered_output = ''
        for line in output.splitlines():
            if line.startswith(tmp_file.name):
                filtered_output += f"{line}\n"

        if filtered_output:
            sync_log(filtered_output)
            return False
        return True
    except:
        import traceback
        sync_log(f"{target_file} failed An exception occurred: {traceback.format_exc()}")
    finally:
        tmp_file.close()
        os.unlink(tmp_file.name)
    return False

if __name__ == '__main__':
    freeze_support()

    with Pool(12, initializer=init_pool, initargs=(Lock(),)) as pool:
        if len(sys.argv) > 1:
            target_files = [target for target in target_files if sys.argv[1] in target]
        result = pool.map(run_syntax_check, target_files)
        log(f"ALGOS: {target_files}")
        log(str(result))
        log(f"SUCCESS RATE {round((sum(result) / len(result)) * 100, 1)}% took {time.time() - start_time}s")
        exit(0 if all(result) else 1)
