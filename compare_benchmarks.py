import sys
import json

print(f'Will compare benchmark results {sys.argv[2]} against reference {sys.argv[1]}')

referenceBenchmark = json.load(open(sys.argv[1]))
newBenchmark = json.load(open(sys.argv[2]))

failed = False
failedAlgorithms = { "CSharp": [], "Python": [] }
for language in ["CSharp", "Python"]:

	for key, value in referenceBenchmark[language].items():
		if key not in newBenchmark[language]:
			failed = True
			failedAlgorithms[language].append(key)
			print(f'Performance benchmark {key} language {language} was not found in new results')
			continue
		newResult = newBenchmark[language][key]

		# allow 10% noise
		expectedValue = value["average-dps"] * 0.90
		if expectedValue > newResult["average-dps"]:
			failed = True
			failedAlgorithms[language].append(key)
			print(f'Performance benchmark Failed for algorithm {key} language {language}. Was {str(newResult["average-dps"])} expected as low as {str(expectedValue)}')
		else:
			print(f'Performance benchmark Passed for algorithm {key} language {language}. Was {str(newResult["average-dps"])} expected as low as {str(expectedValue)}')

# optional output file with the failed algorithms, so they can be run again
if len(sys.argv) > 3:
	with open(sys.argv[3], "w") as outfile:
		json.dump(failedAlgorithms, outfile)

if failed:
	exit(1)
