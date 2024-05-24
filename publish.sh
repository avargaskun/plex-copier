#!/bin/sh
CURRENT_DIR=$(pwd)
SCRIPT_DIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )
rm build.zip
dotnet publish -c Release
cd "$SCRIPT_DIR/src/bin/Release/net8.0/publish/"
zip -r "$CURRENT_DIR/build.zip" *
cd "$CURRENT_DIR"