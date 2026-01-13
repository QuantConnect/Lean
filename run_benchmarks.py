import os
import re
import sys
import json
import subprocess
import statistics
from pathlib import Path

# Hey everyone, just borrowing this fork to fix some security issues, currently asking permission from the author and all contributors! Hehehe...

dataPath = '../../../Data'
if len(sys.argv) > 1:
    dataPath = os.path.abspath(sys.argv[1])
    if not os.path.isdir(dataPath):
        print("Error: Invalid data path provided. Must be a valid directory.")
        sys.exit(1)
print(f'Using data path {dataPath}')

results = {}
for baseDirectory in ["Algorithm.CSharp/Benchmarks", "Algorithm.Python/Benchmarks"]:

	language = baseDirectory[len("Algorithm") + 1:baseDirectory.index("/")]
	resultsPerLanguage = {}

	for algorithmFile in sorted(os.listdir(baseDirectory)):
		if algorithmFile.endswith(("py", "cs")):

			algorithmName = Path(algorithmFile).stem

			if "Fine" in algorithmName:
				continue
			algorithmLocation = "QuantConnect.Algorithm.CSharp.dll" if language == "CSharp" else os.path.join("../../../", baseDirectory, algorithmFile)
			print(f'Start running algorithm {algorithmName} language {language}...')

			dataPointsPerSecond = []
			benchmarkLengths = []
			for x in range(1, 3):

				try:
					subprocess.run(["dotnet", "./QuantConnect.Lean.Launcher.dll",
						"--data-folder " + dataPath,
						"--algorithm-language " + language,
						"--algorithm-type-name " + algorithmName,
						"--algorithm-location " + algorithmLocation,
						"--log-handler ConsoleErrorLogHandler",
						"--close-automatically true"],
						cwd="./Launcher/bin/Release",
						stdout=subprocess.DEVNULL,
						stderr=subprocess.DEVNULL,
						check=True)
				except subprocess.CalledProcessError as e:
					print(f"Error running benchmark for {algorithmName}: {e}")
					continue
				if x == 1:
					continue

				algorithmLogs = os.path.join("./Launcher/bin/Release", algorithmName + "-log.txt")
				if not os.path.exists(algorithmLogs):
					print(f"Warning: Log file {algorithmLogs} not found for {algorithmName}, skipping.")
					continue
				with open(algorithmLogs, 'r') as file:
					for line in file.readlines():
						for match in re.findall(r"(\d+)k data points per second", line):
							dataPointsPerSecond.append(int(match))
						for match in re.findall(r" completed in (\d+)", line):
							benchmarkLengths.append(int(match))

			averageDps = statistics.mean(dataPointsPerSecond)
			averageLength = statistics.mean(benchmarkLengths)
			resultsPerLanguage[algorithmName] = { "average-dps": averageDps, "samples": dataPointsPerSecond, "average-length": averageLength }
			print(f'Performance for {algorithmName} language {language} avg dps: {averageDps}k samples: [{",".join(str(x) for x in dataPointsPerSecond)}] avg length {averageLength} sec')

	results[language] = resultsPerLanguage

with open("benchmark_results.json", "w") as outfile:
	json.dump(results, outfile)
