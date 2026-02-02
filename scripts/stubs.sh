#!/bin/bash
# Generate Python stubs for LEAN
#
# This repo IS the Lean fork - stubs are generated from repo root.

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_DIR="$(dirname "$SCRIPT_DIR")"
LEAN_DIR="$REPO_DIR"
STUBS_GENERATOR_DIR="$REPO_DIR/quantconnect-stubs-generator"
RUNTIME_DIR="$REPO_DIR/dotnet-runtime"
OUTPUT_DIR="$REPO_DIR/stubs"

# Verify we're in the Lean repo
if [ ! -d "$LEAN_DIR/Common" ]; then
    echo "Error: Not in Lean repo root. Expected Common/ directory."
    exit 1
fi

# Clone or update stubs generator
if [ ! -d "$STUBS_GENERATOR_DIR" ]; then
    echo "Cloning quantconnect-stubs-generator..."
    git clone https://github.com/QuantConnect/quantconnect-stubs-generator.git "$STUBS_GENERATOR_DIR"
else
    echo "Updating quantconnect-stubs-generator..."
    cd "$STUBS_GENERATOR_DIR" && git pull
fi

# Clone or update dotnet runtime (shallow clone - only need source files)
if [ ! -d "$RUNTIME_DIR" ]; then
    echo "Cloning dotnet/runtime (shallow)..."
    git clone --depth 1 https://github.com/dotnet/runtime.git "$RUNTIME_DIR"
else
    echo "Updating dotnet/runtime..."
    cd "$RUNTIME_DIR" && git pull
fi

# Create output directory
mkdir -p "$OUTPUT_DIR"

# Build and run stubs generator
echo "Generating Python stubs..."
cd "$STUBS_GENERATOR_DIR/QuantConnectStubsGenerator"
dotnet run "$LEAN_DIR" "$RUNTIME_DIR" "$OUTPUT_DIR"

echo ""
echo "Stubs generated in: $OUTPUT_DIR"
echo ""
echo "Installing stubs in editable mode..."
pip install -e "$OUTPUT_DIR"
echo "Done! LEAN stubs are now installed."
