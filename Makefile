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

.PHONY: lean_container setup clean check-deps

# Image tag for the custom LEAN container
IMAGE_TAG ?= cascadelabs-lean

# DataSource projects to include
# Note: These have pre-existing issues that need fixing before they can be included
# DATASOURCES := CascadeThetaData CascadeKalshiData CascadeTradeAlert CascadeHyperliquid
DATASOURCES :=

LAUNCHER_CSPROJ := Launcher/QuantConnect.Lean.Launcher.csproj

# Main target: build the lean container
lean_container: check-deps setup
	@echo "=== Building Cascade Labs Custom LEAN Container ==="
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
