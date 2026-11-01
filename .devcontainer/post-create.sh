#!/usr/bin/env bash
# Runs once after the codespace is created: show the tool versions so problems are visible early.
set -u

echo "== tool versions =="
dotnet --version
terraform version | head -1
az version --query '"azure-cli"' -o tsv
docker --version
func --version 2>/dev/null || echo "func: not installed"
