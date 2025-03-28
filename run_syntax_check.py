import os
import sys
import tempfile
from pathlib import Path
from subprocess import run
from multiprocessing import Pool, freeze_support

target_files = []

for subdir, dirs, files_in_folder in os.walk("./Algorithm.Python"):
    for file_name in files_in_folder:
        file_path = subdir + os.sep + file_name
        if file_path.endswith(".py"):
            target_files.append(file_path)

def adjust_file_contents(target_file: str):
    file = Path(target_file)
    file_content = file.read_text()
    adjusted_import = 'from AlgorithmImports import *;from datetime import date, time, datetime, timedelta;import pandas as pd;import numpy as np;'

    tmp_file = tempfile.NamedTemporaryFile(prefix=f"{file.name}_", delete=False)
    Path(tmp_file.name).write_text("# mypy: disable-error-code=\"no-redef\"\n" + file_content.replace("from AlgorithmImports import *", adjusted_import))
    return tmp_file

def run_syntax_check(target_file: str):
    tmp_file = adjust_file_contents(target_file)
    try:
        algorithm_result = run([sys.executable, "-m", "mypy", "--skip-cache-mtime-checks", "--skip-version-check", "--show-error-codes",
            "--no-error-summary", "--no-color-output", "--ignore-missing-imports", "--check-untyped-defs", "--cache-fine-grained", "--install-types",
            "--non-interactive", "--cache-dir", "mypy_cache", tmp_file.name], capture_output=True, text=True)

        print(algorithm_result.stdout)
        if algorithm_result.stderr:
            print(algorithm_result.stderr)
            return False
        return True
    except:
        import traceback
        print(f"{target_file} failed An exception occurred: {traceback.format_exc()}")
    finally:
        tmp_file.close()
        os.unlink(tmp_file.name)
    return False

if __name__ == '__main__':
    freeze_support()

    with Pool(8) as pool:
        if len(sys.argv) > 1:
            target_files = [target for target in target_files if sys.argv[1] in target]
        result = pool.map(run_syntax_check, target_files)
        print(result)
        print(f"SUCCESS RATE {round((sum(result) / len(result)) * 100, 1)}%")
        exit(0 if all(result) else 1)
