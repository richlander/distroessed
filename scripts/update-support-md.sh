#!/bin/bash

if [ -z "$1" ]; then
    echo "Usage: $0 <directory>"
    exit 1
fi

SOURCE_DIRECTORY=$1

if [ ! -d "$SOURCE_DIRECTORY" ]; then
    echo "Directory $SOURCE_DIRECTORY does not exist."
    exit 1
fi

run_supported_os_md() {
    echo "$SOURCE_DIRECTORY"
    ../tools/SupportedOsMd "$1" "$SOURCE_DIRECTORY"
}

run_supported_os_md 8
run_supported_os_md 9
run_supported_os_md 10
