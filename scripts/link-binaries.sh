#!/bin/bash

# Function to create a symbolic link if it doesn't already exist
create_symlink() {
    source="../artifacts/publish/$1/release/$1"
    target="../_tools/$1"
    if [ ! -L "$target" ]; then
        ln -s "$source" "$target"
        echo "Created symlink: $target -> $source"
    else
        echo "Symlink already exists: $target"
    fi
}

if [ ! -d "../_tools" ]; then
    mkdir -p "../_tools"
fi

create_symlink SupportedOsMd
create_symlink LinuxPackagesMd
create_symlink DistroessedExceptional
create_symlink distroessed
create_symlink CveMarkdown
create_symlink GenerateJsonSchemas
create_symlink UpdateIndexes
