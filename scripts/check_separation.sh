#!/bin/bash
# =============================================================================
# Check Separation: Algorithm vs Analysis Code
# =============================================================================
# This script validates Learning #5: Research Separate from Algorithm
#
# Rule: Trading algorithms should NOT contain analysis/reporting logic.
# Analysis belongs in Research/ notebooks or separate scripts.
#
# REQUIRED: Run from repository root
# USAGE: ./scripts/check_separation.sh
#
# EXIT CODES:
#   0 = Pass (separation maintained)
#   1 = Fail (analysis code found in algorithm)
# =============================================================================

set -e

echo "=============================================="
echo "SEPARATION CHECK: Algorithm vs Analysis"
echo "=============================================="
echo ""

# Define patterns that indicate analysis code in algorithms
ANALYSIS_PATTERNS=(
    "pandas\.DataFrame"
    "matplotlib"
    "seaborn"
    "plotly"
    "\.to_csv\("
    "\.to_excel\("
    "import requests"  # API calls should be in separate scripts
    "capture_rate"     # Analysis metric
    "SNAPSHOT"         # Logging for analysis
    "EXIT_SUMMARY"     # Logging for analysis
)

# Directories to check (trading algorithms only)
ALGO_DIRS=(
    "Algorithm.Python"
    "LowIVRStraddle"
)

# Files to exclude (allowed to have analysis patterns)
EXCLUDE_PATTERNS=(
    "Research/"
    "scripts/"
    "tests/"
    "_analytics/"
    "parse_"
    "analyze_"
    "calculate_"
)

VIOLATIONS=0
WARNINGS=0

echo "Checking for analysis code in trading algorithms..."
echo ""

for algo_dir in "${ALGO_DIRS[@]}"; do
    if [ ! -d "$algo_dir" ]; then
        continue
    fi

    # Find Python files in algo directories
    while IFS= read -r -d '' file; do
        # Skip excluded patterns
        skip=false
        for exclude in "${EXCLUDE_PATTERNS[@]}"; do
            if [[ "$file" == *"$exclude"* ]]; then
                skip=true
                break
            fi
        done

        if [ "$skip" = true ]; then
            continue
        fi

        # Check for analysis patterns
        for pattern in "${ANALYSIS_PATTERNS[@]}"; do
            if grep -q "$pattern" "$file" 2>/dev/null; then
                # Some patterns are warnings, some are violations
                case "$pattern" in
                    "SNAPSHOT"|"EXIT_SUMMARY")
                        echo "WARNING: $file contains '$pattern' (logging for analysis)"
                        echo "  -> Consider: Is this necessary in the algorithm?"
                        ((WARNINGS++))
                        ;;
                    *)
                        echo "VIOLATION: $file contains '$pattern'"
                        echo "  -> This belongs in Research/ or scripts/, not in trading algorithm"
                        ((VIOLATIONS++))
                        ;;
                esac
            fi
        done
    done < <(find "$algo_dir" -name "*.py" -type f -print0 2>/dev/null)
done

echo ""
echo "=============================================="
echo "RESULTS"
echo "=============================================="
echo "Violations: $VIOLATIONS"
echo "Warnings:   $WARNINGS"
echo ""

if [ $VIOLATIONS -gt 0 ]; then
    echo "FAIL: Analysis code found in trading algorithms."
    echo "Move analysis logic to Research/ notebooks or scripts/ directory."
    exit 1
else
    if [ $WARNINGS -gt 0 ]; then
        echo "PASS with warnings: Review warnings above."
    else
        echo "PASS: Separation maintained."
    fi
    exit 0
fi
