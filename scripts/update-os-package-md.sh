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

../_tools/LinuxPackagesMd 9 "$SOURCE_DIRECTORY"
