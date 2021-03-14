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

#Create a symbolic link to the notebook dir for easy download
ln -s $NOTEBOOK_DIR

# If the config doesn't exist, create it
CONFIG=$NOTEBOOK_DIR/config.json
if [ ! -f "$CONFIG" ]; then 
    echo "Creating research config at $CONFIG"
    echo "{
    \"data-folder\": \"${DATA_DIR}\",
    \"composer-dll-directory\": \"${BUILD_DIR}\", 
    \"algorithm-language\": \"Python\",
    \"messaging-handler\": \"QuantConnect.Messaging.Messaging\", 
    \"job-queue-handler\": \"QuantConnect.Queues.JobQueue\", 
    \"api-handler\": \"QuantConnect.Api.Api\" 
    \"job-user-id\": \"0\",
    \"api-access-token\": \"\",
    }" > $CONFIG
fi

# Copy over any notebooks in research directory
LEAN_RESEARCH=$(absolute_path "../Research")
if [ -d "$LEAN_RESEARCH" ]; then 
    echo "Copying research files to $LEAN_RESEARCH; will not overwrite existing files"
    cp -n $LEAN_RESEARCH/*.ipynb $NOTEBOOK_DIR
fi

# Launch jupyter-lab
jupyter-lab --allow-root --no-browser --notebook-dir=$NOTEBOOK_DIR --LabApp.token=''