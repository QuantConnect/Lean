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

# Get data directory from args position 2, or use default
DEFAULT_DATA_DIR=../Data
DATA_DIR=${2:-$DEFAULT_DATA_DIR}
DATA_DIR=$(absolute_path "${DATA_DIR}")

#Get Notebook location from args position 3, or use default
DEFAULT_NOTEBOOK_DIR=$BUILD_DIR/Notebooks
NOTEBOOK_DIR=${3:-$DEFAULT_NOTEBOOK_DIR}
NOTEBOOK_DIR=$(absolute_path "${NOTEBOOK_DIR}")

#If the folder doesn't exist create it.
if [ ! -d "$NOTEBOOK_DIR" ]; then
    echo "Creating Notebook Directory at $NOTEBOOK_DIR"
    mkdir $NOTEBOOK_DIR
fi

# Copy config.json to notebook directory
cp -n $BUILD_DIR/config.json $NOTEBOOK_DIR/config.json

# Copy over any notebooks from build directory to notebook directory
RESEARCH_FILES=$BUILD_DIR/*.ipynb
if [ -d "$BUILD_DIR" ]; then
    echo "Copying research files from $RESEARCH_FILES to $NOTEBOOK_DIR; will not overwrite existing files"
    cp -n $RESEARCH_FILES $NOTEBOOK_DIR
fi

# Launch jupyter-lab
jupyter-lab --allow-root --no-browser --notebook-dir=$NOTEBOOK_DIR --LabApp.token=''