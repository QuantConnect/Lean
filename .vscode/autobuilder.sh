# Realpath polyfill, notably absent macOS and some debian distros
absolute_path() {
  echo "$(cd "$(dirname "${1}")" && pwd)/$(basename "${1}")"
}

DEFAULT_LAUNCHER_DIR=../Launcher
LAUNCHER_DIR=${1:-$DEFAULT_LAUNCHER_DIR}
LAUNCHER_DIR=$(absolute_path "${LAUNCHER_DIR}")

# Move to Launcher directory
cd $LAUNCHER_DIR

# Enable dotnet watch to trigger builds on file change
dotnet watch build --configuration Debug