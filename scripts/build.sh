#!/bin/bash
# Build the custom LEAN Docker image using lean-cli
#
# Prerequisites:
# - Run setup.sh first to clone and configure repos
# - lean-cli installed and configured

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"

# Add our scripts directory to PATH (for docker wrapper using podman)
export PATH="$SCRIPT_DIR:$PATH"

# Configuration
# Note: Tag cannot have nested slashes, use simple name
IMAGE_TAG="${IMAGE_TAG:-cascadelabs-lean}"

echo "=== Building Cascade Labs Custom LEAN Image ==="
echo ""

# Check if Lean directory exists
if [ ! -d "$PROJECT_DIR/Lean" ]; then
    echo "Error: Lean directory not found. Run setup.sh first."
    exit 1
fi

# Check if CascadeThetaData was copied
if [ ! -d "$PROJECT_DIR/Lean/Lean.DataSource.CascadeThetaData" ]; then
    echo "Error: CascadeThetaData not found in Lean directory. Run setup.sh first."
    exit 1
fi

# Check if CascadeTradeAlert was copied
if [ ! -d "$PROJECT_DIR/Lean/Lean.DataSource.CascadeTradeAlert" ]; then
    echo "Error: CascadeTradeAlert not found in Lean directory. Run setup.sh first."
    exit 1
fi

cd "$PROJECT_DIR"

echo "Building custom LEAN image with tag: $IMAGE_TAG"
echo "This may take several minutes..."
echo ""

# Build using lean-cli (ROOT points to directory containing Lean/)
lean build --tag "$IMAGE_TAG" .

echo ""
echo "=== Build Complete ==="
echo ""
echo "To use the custom image:"
echo "  lean config set engine-image lean-cli/engine:$IMAGE_TAG"
echo "  lean config set research-image lean-cli/research:$IMAGE_TAG"
echo ""
echo "Then run backtests normally:"
echo "  lean backtest <project>"
