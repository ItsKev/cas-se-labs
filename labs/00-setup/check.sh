#!/usr/bin/env bash
# Checks the development environment. Run it in the dev container: bash labs/00-setup/check.sh
# Exit code 0 = everything needed for the local labs works. Azure is reported separately.

ok=0

check() {
  local name="$1"; shift
  if out=$("$@" 2>&1); then
    printf '  [ok]   %-12s %s\n' "$name" "$(echo "$out" | head -1)"
  else
    printf '  [FAIL] %-12s %s\n' "$name" "$(echo "$out" | head -1)"
    ok=1
  fi
}

# Functional tests with neutral content: they prove the toolchain works, nothing more.
docker_hello() {
  docker run --rm hello-world 2>&1 | grep -q "Hello from Docker" && echo "container ran and network works"
}

dotnet_hello() {
  local dir out
  dir=$(mktemp -d)
  out=$(cd "$dir" && dotnet new console --no-restore -o app >/dev/null 2>&1 && cd app && dotnet run 2>&1 | tail -1)
  rm -rf "$dir"
  [ "$out" = "Hello, World!" ] && echo "program compiled and ran"
}

echo "Local tools:"
check dotnet    dotnet --version
check terraform terraform version
check az        az version --query '"azure-cli"' -o tsv
check docker    docker info --format 'daemon {{.ServerVersion}}'
check compose   docker compose version --short

echo
echo "Functional tests:"
check docker-run docker_hello
check dotnet-run dotnet_hello

echo
echo "Azure:"
if sub=$(az account show --query "{name:name, id:id}" -o tsv 2>/dev/null); then
  printf '  [ok]   signed in     %s\n' "$sub"
  if [ -z "${ARM_SUBSCRIPTION_ID:-}" ]; then
    echo "  [todo] ARM_SUBSCRIPTION_ID is not set: export ARM_SUBSCRIPTION_ID=\$(az account show --query id -o tsv)"
  else
    echo "  [ok]   ARM_SUBSCRIPTION_ID is set"
  fi
else
  echo "  [todo] not signed in: az login --use-device-code"
fi

echo
if [ "$ok" -eq 0 ]; then echo "Local environment is ready."; else echo "Fix the [FAIL] lines first."; fi
exit "$ok"
