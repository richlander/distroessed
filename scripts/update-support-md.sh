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

dotnet publish ../src/SupportedOsMd/SupportedOsMd.csproj -o SupportedOsMd
SupportedOsMd/SupportedOsMd 8 "$SOURCE_DIRECTORY"
SupportedOsMd/SupportedOsMd 9 "$SOURCE_DIRECTORY"
SupportedOsMd/SupportedOsMd 10 "$SOURCE_DIRECTORY"
