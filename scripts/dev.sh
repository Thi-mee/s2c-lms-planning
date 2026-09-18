#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"
export ASPNETCORE_ENVIRONMENT=Development
export DOTNET_ENVIRONMENT=Development
export ConnectionStrings__Runtime="Host=localhost;Port=54637;Database=variable_lms;Username=variable_runtime;Password=variable-local-runtime-only"
export Installation__OrganizationId="4fa2e04e-860a-4e94-bd7f-202609180001"
export Installation__PublicOrigin="http://localhost:5178"
export DataProtection__KeyDirectory="$repo_root/.local/keys"
export Smtp__Host="localhost"
export Smtp__Port="1025"
export Smtp__Security="None"
export Smtp__FromAddress="variable@example.test"
app_project=src/backend/Variable.App/Variable.App.csproj
license_dir="$repo_root/.local/development-license"
case "${1:-help}" in
  db) docker compose -f deploy/compose/compose.dev.yml up -d --wait ;;
  stop-db) docker compose -f deploy/compose/compose.dev.yml down ;;
  migrate)
    ConnectionStrings__Migration="Host=localhost;Port=54637;Database=variable_lms;Username=variable_migrator;Password=variable-local-migrator-only" \
      Database__RuntimeRole=variable_runtime dotnet run --no-build --project "$app_project" -- migrate
    ;;
  bootstrap)
    python3 - <<'PY'
from pathlib import Path
import os, secrets
path = Path('.local/bootstrap-password')
path.parent.mkdir(parents=True, exist_ok=True)
if not path.exists():
    fd = os.open(path, os.O_CREAT | os.O_EXCL | os.O_WRONLY, 0o600)
    with os.fdopen(fd, 'w') as stream: stream.write(secrets.token_urlsafe(24))
PY
    Bootstrap__OrganizationName="Variable development" Bootstrap__AdministratorName="Development Administrator" \
      Bootstrap__Email="administrator@example.test" Bootstrap__PasswordFile="$repo_root/.local/bootstrap-password" \
      dotnet run --no-build --project "$app_project" -- bootstrap
    ;;
  license)
    dotnet run --no-build --project tools/Variable.LicenseFixture/Variable.LicenseFixture.csproj -- "$Installation__OrganizationId" 25 "$license_dir"
    ;;
  serve)
    if [[ ! -f "$license_dir/test-public.pem" ]]; then
      dotnet run --no-build --project tools/Variable.LicenseFixture/Variable.LicenseFixture.csproj -- "$Installation__OrganizationId" 25 "$license_dir"
    fi
    export Licensing__TrustedKeys__development_test_issuer="$license_dir/test-public.pem"
    dotnet run --no-build --project "$app_project" -- --urls http://localhost:5078
    ;;
  web) cd src/web && npm run dev ;;
  test) dotnet test Variable.slnx --no-build --disable-build-servers -m:1 ;;
  *) echo 'Usage: bash scripts/dev.sh {db|stop-db|migrate|bootstrap|license|serve|web|test}' ;;
esac
