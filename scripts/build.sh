#!/bin/bash
# Build the custom LEAN Docker image using lean-cli
#
# This repo IS the Lean fork - no setup.sh needed for basic builds.
# Custom data sources are in DataSource/ directory.

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_DIR="$(dirname "$SCRIPT_DIR")"

# Add our scripts directory to PATH (for docker wrapper using podman)
export PATH="$SCRIPT_DIR:$PATH"

# Configuration
IMAGE_TAG="${IMAGE_TAG:-cascadelabs-lean}"

echo "=== Building Cascade Labs Custom LEAN Image ==="
echo ""

# Verify we're in the Lean repo (check for Common/ directory)
if [ ! -d "$REPO_DIR/Common" ]; then
    echo "Error: Not in Lean repo root. Expected Common/ directory."
    exit 1
fi

# Check if custom data sources exist
if [ ! -d "$REPO_DIR/DataSource/CascadeThetaData" ]; then
    echo "Warning: CascadeThetaData not found in DataSource/"
fi

if [ ! -d "$REPO_DIR/DataSource/CascadeTradeAlert" ]; then
    echo "Warning: CascadeTradeAlert not found in DataSource/"
fi

cd "$REPO_DIR"

echo "Building custom LEAN image with tag: $IMAGE_TAG"
echo "This may take several minutes..."
echo ""

# Build using lean-cli from repo root
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
