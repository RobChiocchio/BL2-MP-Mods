#!/bin/bash
set -e #do not ignore error states

if ! git diff-index --quiet HEAD --; then #check for uncommitted changes
    echo Please commit and push any changes
	exit 1
fi

read -p 'Version number: ' VERSION
read -p 'Version description: ' DESC

git tag -a "$VERSION" -m "$DESC"

#BUILD PROJECT
#ZIP UP RELEASE

echo "Don't forget to upload the release files!"