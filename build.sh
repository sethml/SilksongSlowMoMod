#!/bin/bash

# Build script for SlowMo
# Usage: ./build.sh [--install]
# By default, always installs after building

GAME_PATH="/Applications/Hollow Knight Silksong"
PLUGIN_NAME="SlowMo.dll"
PLUGINS_DIR="$GAME_PATH/BepInEx/plugins"

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

echo "Building SlowMo..."

# Build the project
dotnet build -c Debug

if [ $? -ne 0 ]; then
    echo -e "${RED}Build failed!${NC}"
    exit 1
fi

echo -e "${GREEN}Build successful!${NC}"

# Check if --install flag is provided
if [[ "$1" == "--install" ]]; then
    echo ""
    echo "Installing plugin..."
    
    # Check if game directory exists
    if [ ! -d "$GAME_PATH" ]; then
        echo -e "${RED}Error: Game directory not found at $GAME_PATH${NC}"
        exit 1
    fi
    
    # Check if BepInEx plugins directory exists
    if [ ! -d "$PLUGINS_DIR" ]; then
        echo -e "${YELLOW}Warning: BepInEx plugins directory not found. Creating it...${NC}"
        mkdir -p "$PLUGINS_DIR"
    fi
    
    # Copy the DLL
    SOURCE_FILE="bin/Debug/$PLUGIN_NAME"
    
    if [ ! -f "$SOURCE_FILE" ]; then
        echo -e "${RED}Error: Built plugin not found at $SOURCE_FILE${NC}"
        exit 1
    fi
    
    cp "$SOURCE_FILE" "$PLUGINS_DIR/"
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}Plugin installed successfully to $PLUGINS_DIR${NC}"
    else
        echo -e "${RED}Failed to install plugin${NC}"
        exit 1
    fi
else
    echo ""
    echo "Build complete. DLL is at: bin/Debug/$PLUGIN_NAME"
    echo "To install, run: ./build.sh --install"
fi

