import os
import sys
import time
import tempfile
import re
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

specific_order_attributes = ['limit_price', 'trigger_price', 'trigger_touched', 'stop_price', 'stop_triggered', 'trailing_amount', 'trailing_as_percentage']

specific_ibase_data_attributes = ['is_fill_forward', 'volume', 'open', 'high', 'low', 'close', 'bid', 'bid_size', 'ask', 'ask_size', 'last_bid_size', 'last_ask_size', 'bid_price', 'ask_price', 'last_price', 'period', 'tick_type', 'quantity', 'exchange_code', 'exchange', 'sale_condition', 'parsed_sale_condition', 'suspicious', 'update']

specific_indicator_attributes = ['is_ready', 'samples', 'name', 'current', 'update', 'reset', 'updated']

def should_ignore(line: str, prev_line_ignored: bool) -> bool:
    result = any(to_ignore in line for to_ignore in (
        # this (None and object) is just noise the variable was initialized with None or mypy might not be able to resolve base class in some cases
        'None',
        '"object"',
        'Name "datetime" is not defined',
        'Name "np" is not defined',
        'Name "pd" is not defined',
        'Name "math" is not defined',
        'Name "time" is not defined',
        'Name "json" is not defined',
        'Name "timedelta" is not defined',
        'be derived from BaseException',
        'Argument 1 of "update" is incompatible with supertype "IndicatorBase"; supertype defines the argument type as "IBaseData"',
        'Module has no attribute "JsonConvert"',
        'Too many arguments for "update" of "IndicatorBase"',
        'Signature of "update" incompatible with supertype "IndicatorBase"',
        'has incompatible type "Symbol"; expected "str"',
        # This methods take an indicator and consolidator which might be instances of custom
        # indicator/consolidator Python classes that don't inherit from PythonIndicator or IDataConsolidator
        'No overload variant of "register_indicator" of "QCAlgorithm" matches argument types',
        'No overload variant of "warm_up_indicator" of "QCAlgorithm" matches argument types'
    ))

    if result or ('note: ' in line and prev_line_ignored):
        return True

    # Ignore accessing specific order types properties
    order_attributes_match = re.search(r'error: "Order" has no attribute "([^"]+)"', line)
    if order_attributes_match and order_attributes_match.group(1) in specific_order_attributes:
        return True

    # Ignore accessing specific properties of common data types derived from IBaseData, like Tick, TradeBar and QoteBar
    base_data_attributes_match = re.search(r'error: "IBaseData" has no attribute "([^"]+)"', line)
    if base_data_attributes_match and base_data_attributes_match.group(1) in specific_ibase_data_attributes:
        return True

    # Ignore accessing indicator properties. Useful for instance when adding indicators of different types
    # to a list and then iterating over them, the common type will be IIndicatorWarmUpPeriodProvider
    indicator_attributes_match = re.search(r'error: "IIndicatorWarmUpPeriodProvider" has no attribute "([^"]+)"', line)
    if indicator_attributes_match and indicator_attributes_match.group(1) in specific_indicator_attributes:
        return True

    # Ignore accessing specific properties of some models, just to reduce noise in regression algorithms asserting internal stuff.
    # We don't expect users to be accessing properties of models like this in most cases
    if re.search('error: "(IBuyingPowerModel)|(IBenchmark)|(IMarginInterestRateModel)" has no attribute "([^"]+)"', line):
        return True

    # In some cases Python developers use the same variable and redefine it, this is not a problem in Python but mypy doesn't like it
    if re.search(r'error: Incompatible types in assignment \(expression has type "([^"]+)", variable has type "([^"]+)"\)', line):
        return True

    return False


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
        prev_line_ignored = False
        for line in output.splitlines():
            ignored = not line.startswith(tmp_file.name) or should_ignore(line, prev_line_ignored)
            if not ignored:
                filtered_output += f"{line}\n"
            prev_line_ignored = ignored

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
        success_rate = round((sum(result) / len(result)) * 100, 1)
        log(f"SUCCESS RATE {success_rate}% took {time.time() - start_time}s")
        exit(0 if success_rate >= 98.6 else 1)
