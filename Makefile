# Makefile for Cascade Labs LEAN Container
#
# Usage:
#   make lean_container  - Build the custom LEAN Docker container
#   make setup          - Add DataSource project references to Launcher
#   make clean          - Clean build artifacts
#
# Prerequisites:
#   - lean-cli installed (pip install lean)
#   - Docker or Podman available

.PHONY: lean_container setup clean check-deps stubs stubs_install all push-registry

# Image tag for the custom LEAN container
IMAGE_TAG ?= cascadelabs-lean

# OCI Container Registry settings
REGISTRY ?= iad.ocir.io
REGISTRY_NAMESPACE ?= idvfareebwfp
REGISTRY_USERNAME ?= $(REGISTRY_NAMESPACE)/j.brown9513@icloud.com

# DataSource projects to include
# Note: These have pre-existing issues that need fixing before they can be included
DATASOURCES := CascadeThetaData CascadeKalshiData CascadeTradeAlert CascadeHyperliquid

LAUNCHER_CSPROJ := Launcher/QuantConnect.Lean.Launcher.csproj

# Main target: build the lean container (fast, uses official foundation)
lean_container: check-deps setup compile
	@echo "=== Building Cascade Labs Custom LEAN Container ==="
	@echo "Image tag: $(IMAGE_TAG)"
	@echo ""
	@# Build engine image directly using official foundation (skip foundation rebuild)
	@# Uses docker wrapper script to route to podman
	PATH="$(CURDIR)/scripts:$$PATH" docker build -t lean-cli/engine:$(IMAGE_TAG) -f Dockerfile .
	@echo ""
	@# Build research image from our engine image (includes all DataSource DLLs)
	@# Tag engine as quantconnect/lean so DockerfileJupyter's FROM can find it
	PATH="$(CURDIR)/scripts:$$PATH" docker tag lean-cli/engine:$(IMAGE_TAG) quantconnect/lean:$(IMAGE_TAG)
	PATH="$(CURDIR)/scripts:$$PATH" docker build -t lean-cli/research:$(IMAGE_TAG) --build-arg LEAN_TAG=$(IMAGE_TAG) -f DockerfileJupyter .
	@echo ""
	@echo "=== Build Complete ==="
	@echo ""
	@echo "Setting as default images..."
	@lean config set engine-image lean-cli/engine:$(IMAGE_TAG)
	@lean config set research-image lean-cli/research:$(IMAGE_TAG)
	@echo ""
	@echo "Custom images ready:"
	@echo "  Engine:   lean-cli/engine:$(IMAGE_TAG)"
	@echo "  Research: lean-cli/research:$(IMAGE_TAG)"

# Compile LEAN locally (much faster than inside container)
compile:
	@echo "=== Compiling LEAN ==="
	dotnet build QuantConnect.Lean.sln -c Debug --nologo -v q
	@echo "Compilation complete"
	@echo ""

# Full build using lean-cli (slower, rebuilds foundation if different)
lean_container_full: check-deps setup
	@echo "=== Building Cascade Labs Custom LEAN Container (Full) ==="
	@echo "Image tag: $(IMAGE_TAG)"
	@echo ""
	@# Add scripts directory to PATH for docker wrapper (uses podman)
	@# lean build expects to be run from parent of Lean/ directory
	PATH="$(CURDIR)/scripts:$$PATH" && cd .. && lean build --tag $(IMAGE_TAG) .
	@echo ""
	@echo "=== Build Complete ==="
	@echo ""
	@echo "To use the custom image:"
	@echo "  lean config set engine-image lean-cli/engine:$(IMAGE_TAG)"
	@echo "  lean config set research-image lean-cli/research:$(IMAGE_TAG)"

# Setup: Add DataSource project references to Launcher
setup:
	@echo "=== Setting up DataSource Project References ==="
	@for ds in $(DATASOURCES); do \
		if [ -d "DataSource/$$ds" ]; then \
			if grep -q "$$ds" $(LAUNCHER_CSPROJ) 2>/dev/null; then \
				echo "  $$ds: already referenced"; \
			else \
				echo "  $$ds: adding reference..."; \
				sed -i.bak 's|</Project>|  <ItemGroup>\n    <ProjectReference Include="../DataSource/'$$ds'/'$$ds'.csproj" />\n  </ItemGroup>\n</Project>|' $(LAUNCHER_CSPROJ); \
				rm -f $(LAUNCHER_CSPROJ).bak; \
			fi \
		else \
			echo "  $$ds: not found in DataSource/, skipping"; \
		fi \
	done
	@echo ""

# Check dependencies
check-deps:
	@echo "=== Checking Dependencies ==="
	@command -v lean >/dev/null 2>&1 || { echo "Error: lean-cli not found. Install with: pip install lean"; exit 1; }
	@(command -v docker >/dev/null 2>&1 || command -v podman >/dev/null 2>&1) || { echo "Error: docker or podman not found"; exit 1; }
	@echo "  lean-cli: OK"
	@echo "  container runtime: OK"
	@echo ""

# Clean build artifacts
clean:
	@echo "=== Cleaning Build Artifacts ==="
	find . -type d -name "bin" -exec rm -rf {} + 2>/dev/null || true
	find . -type d -name "obj" -exec rm -rf {} + 2>/dev/null || true
	@echo "Done"

# Show current configuration
info:
	@echo "=== Cascade Labs LEAN Configuration ==="
	@echo "DataSource projects:"
	@for ds in $(DATASOURCES); do \
		if [ -d "DataSource/$$ds" ]; then \
			echo "  - $$ds"; \
		fi \
	done
	@echo ""
	@echo "Current engine image:"
	@lean config get engine-image 2>/dev/null || echo "  (not set)"
	@echo ""

# Generate Python stubs from LEAN source
stubs:
	@echo "=== Generating LEAN Python Stubs ==="
	@./scripts/stubs.sh

# Install stubs in editable mode
stubs_install: stubs
	@echo "=== Installing LEAN Python Stubs ==="
	python3 -m pip install --break-system-packages -e .stubs/output
	@echo "Done! LEAN stubs installed."

# Push container image to OCI Container Registry using lean-cli
push-registry:
	@echo "=== Pushing to OCI Container Registry ==="
	lean cloud container push --type engine --image lean-cli/engine:$(IMAGE_TAG)
	@echo ""
	@echo "=== Push Complete ==="

# Build container AND install stubs
all: lean_container stubs_install
	@echo ""
	@echo "=== All Complete ==="
