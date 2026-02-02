#!/bin/bash
# Setup script for Cascade Labs custom LEAN container
# This clones the necessary repos and sets up the project structure

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"

echo "=== Cascade Labs LEAN Container Setup ==="
echo ""

cd "$PROJECT_DIR"

# LEAN version to checkout (default: latest master)
LEAN_VERSION="${LEAN_VERSION:-origin/master}"

# Step 1: Clone LEAN repository
if [ -d "Lean" ]; then
    echo "LEAN repository already exists."
    cd Lean
    git fetch origin
    # Discard local changes (setup.sh will re-apply our customizations)
    git checkout -- .
    git clean -fd
    echo "Checking out LEAN $LEAN_VERSION"
    git checkout "$LEAN_VERSION"
    cd ..
else
    echo "Cloning LEAN repository..."
    git clone https://github.com/QuantConnect/Lean.git
    cd Lean
    git checkout "$LEAN_VERSION"
    cd ..
fi

# Step 2: Clone ThetaData data source (for reference/models)
cd Lean
if [ -d "Lean.DataSource.ThetaData" ]; then
    echo "ThetaData data source already exists. Updating..."
    cd Lean.DataSource.ThetaData
    git pull origin master
    cd ..
else
    echo "Cloning ThetaData data source..."
    git clone https://github.com/QuantConnect/Lean.DataSource.ThetaData.git
fi

# Step 3: Copy our CascadeThetaData source
echo "Copying CascadeThetaData source..."
rm -rf Lean.DataSource.CascadeThetaData
mkdir -p Lean.DataSource.CascadeThetaData

# Copy our modified source files
cp -r "$PROJECT_DIR/data_sources/CascadeThetaData/"* Lean.DataSource.CascadeThetaData/

# Copy required files from ThetaData
echo "Copying required files from ThetaData..."
cp -r Lean.DataSource.ThetaData/QuantConnect.ThetaData/Models Lean.DataSource.CascadeThetaData/
cp -r Lean.DataSource.ThetaData/QuantConnect.ThetaData/Converters Lean.DataSource.CascadeThetaData/
cp Lean.DataSource.ThetaData/QuantConnect.ThetaData/ThetaDataSymbolMapper.cs Lean.DataSource.CascadeThetaData/
cp Lean.DataSource.ThetaData/QuantConnect.ThetaData/ThetaDataExtensions.cs Lean.DataSource.CascadeThetaData/

# Fix namespaces in copied files (ThetaData -> CascadeThetaData)
echo "Fixing namespaces in copied files..."
find Lean.DataSource.CascadeThetaData -name "*.cs" -exec sed -i '' 's/QuantConnect.Lean.DataSource.ThetaData/QuantConnect.Lean.DataSource.CascadeThetaData/g' {} \;

# Fix class name references (ThetaDataProvider -> CascadeThetaDataProvider)
echo "Fixing class references..."
find Lean.DataSource.CascadeThetaData -name "*.cs" -exec sed -i '' 's/nameof(ThetaDataProvider)/nameof(CascadeThetaDataProvider)/g' {} \;

# Set full historical access for CascadeThetaData (no date restrictions)
echo "Setting full historical access..."
sed -i '' 's/new DateTime(2012, 06, 01);/new DateTime(2000, 01, 01);  \/\/ Full historical access/' Lean.DataSource.CascadeThetaData/Models/SubscriptionPlans/ProSubscriptionPlan.cs

# Step 4: Update the csproj file with proper references
echo "Creating CascadeThetaData.csproj..."
cat > Lean.DataSource.CascadeThetaData/CascadeThetaData.csproj << 'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AssemblyName>QuantConnect.Lean.DataSource.CascadeThetaData</AssemblyName>
    <RootNamespace>QuantConnect.Lean.DataSource.CascadeThetaData</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="../Common/QuantConnect.Common.csproj" />
    <ProjectReference Include="../Engine/QuantConnect.Lean.Engine.csproj" />
  </ItemGroup>

  <!-- Package versions inherited from LEAN project references -->
</Project>
EOF

# Step 5: Copy CascadeTradeAlert source
echo "Copying CascadeTradeAlert source..."
rm -rf Lean.DataSource.CascadeTradeAlert
mkdir -p Lean.DataSource.CascadeTradeAlert

# Copy our TradeAlert source files
cp -r "$PROJECT_DIR/data_sources/CascadeTradeAlert/"* Lean.DataSource.CascadeTradeAlert/

# Step 6: Add project references to Launcher
echo "Adding project references to Launcher..."
LAUNCHER_CSPROJ="Launcher/QuantConnect.Lean.Launcher.csproj"

# Check if CascadeThetaData reference already exists
if grep -q "CascadeThetaData" "$LAUNCHER_CSPROJ"; then
    echo "CascadeThetaData project reference already exists in Launcher"
else
    # Add the project reference before the closing </Project> tag
    sed -i.bak 's|</Project>|  <ItemGroup>\n    <ProjectReference Include="../Lean.DataSource.CascadeThetaData/CascadeThetaData.csproj" />\n  </ItemGroup>\n</Project>|' "$LAUNCHER_CSPROJ"
    rm -f "$LAUNCHER_CSPROJ.bak"
    echo "Added CascadeThetaData project reference to Launcher"
fi

# Check if CascadeTradeAlert reference already exists
if grep -q "CascadeTradeAlert" "$LAUNCHER_CSPROJ"; then
    echo "CascadeTradeAlert project reference already exists in Launcher"
else
    # Add the project reference before the closing </Project> tag
    sed -i.bak 's|</Project>|  <ItemGroup>\n    <ProjectReference Include="../Lean.DataSource.CascadeTradeAlert/CascadeTradeAlert.csproj" />\n  </ItemGroup>\n</Project>|' "$LAUNCHER_CSPROJ"
    rm -f "$LAUNCHER_CSPROJ.bak"
    echo "Added CascadeTradeAlert project reference to Launcher"
fi

cd "$PROJECT_DIR"

echo ""
echo "=== Setup Complete ==="
echo ""
echo "Next steps:"
echo "  1. cd $PROJECT_DIR"
echo "  2. ./scripts/build.sh"
echo ""
