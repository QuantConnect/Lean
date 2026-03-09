# Realpath polyfill, notably absent macOS and some debian distros
absolute_path() {
  echo "$(cd "$(dirname "${1}")" && pwd)/$(basename "${1}")"
}

# Get build directory from args position 1, or use default
DEFAULT_BUILD_DIR=../Launcher/bin/Debug/
BUILD_DIR=${1:-$DEFAULT_BUILD_DIR}
BUILD_DIR=$(absolute_path "${BUILD_DIR}")

#Add our build directory to python path for python kernel
export PYTHONPATH="${PYTHONPATH}:${BUILD_DIR}"

# Launch jupyter-lab
jupyter-lab --allow-root --no-browser --notebook-dir=$BUILD_DIR --LabApp.token=''