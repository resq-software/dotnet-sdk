#!/usr/bin/env bash

# Copyright 2026 ResQ
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

# Sets up the ResQ .NET SDK development environment.
#
# Usage:
#   ./scripts/setup.sh [--check] [--yes]
#
# Options:
#   --check   Verify the environment without making changes.
#   --yes     Auto-confirm all prompts (CI mode).
#
# What this does:
#   1. Installs Nix with flakes support (if missing).
#   2. Re-enters inside `nix develop` — provides dotnet-sdk 9, protobuf (Linux).
#   3. Installs Docker (if missing).
#   4. On macOS: dotnet is not in nixpkgs; prints manual install instructions.
#   5. Configures git hooks (.git-hooks/).
#
# Requirements:
#   curl, git, bash 4+
#
# Exit codes:
#   0  Success.
#   1  A required step failed.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

# shellcheck source=lib/shell-utils.sh
source "${SCRIPT_DIR}/lib/shell-utils.sh"

# ── Argument parsing ──────────────────────────────────────────────────────────
CHECK_ONLY=false
for arg in "$@"; do
    case "$arg" in
        --check)  CHECK_ONLY=true ;;
        --yes)    export YES=1 ;;
        --help|-h)
            sed -n '/^# Usage/,/^$/p' "$0"
            exit 0
            ;;
    esac
done

# ── Check mode ────────────────────────────────────────────────────────────────
if [ "$CHECK_ONLY" = true ]; then
    log_info "Checking ResQ .NET SDK environment..."
    ERRORS=0

    command_exists nix    || { log_error "nix not found";    ERRORS=$((ERRORS+1)); }
    command_exists docker || { log_warning "docker not found"; }

    if [[ "$(detect_os)" == "linux" ]]; then
        command_exists dotnet || { log_warning "dotnet not found (run: nix develop)"; }
        command_exists protoc || { log_warning "protoc not found (run: nix develop)"; }
    else
        if command_exists dotnet; then
            log_success "dotnet: $(dotnet --version)"
        else
            log_warning "dotnet not found — install from https://dotnet.microsoft.com/download"
        fi
    fi

    [ $ERRORS -eq 0 ] && log_success "Environment looks good." || exit 1
    exit 0
fi

# ── Main setup ────────────────────────────────────────────────────────────────
echo "╔══════════════════════════════════════╗"
echo "║  ResQ .NET SDK — Environment Setup   ║"
echo "╚══════════════════════════════════════╝"
echo ""

# 1. Nix
install_nix

# 2. Re-enter inside nix develop
#    On Linux this provides: dotnet-sdk_9, protobuf, ripgrep, fd, jq
ensure_nix_env "$@"

# 3. Docker (for running tests in containers)
install_docker

# 4. macOS notice (dotnet not in nixpkgs for Darwin)
if [[ "$(detect_os)" == "macos" ]]; then
    if ! command_exists dotnet; then
        log_warning ".NET SDK is not available via nixpkgs on macOS."
        log_info "Install from: https://dotnet.microsoft.com/download/dotnet/9.0"
        log_info "Or via Homebrew: brew install --cask dotnet-sdk"
    fi
fi

# 5. Configure git hooks
if [ -d "$PROJECT_ROOT/.git-hooks" ]; then
    log_info "Configuring git hooks..."
    git -C "$PROJECT_ROOT" config core.hooksPath .git-hooks
    chmod +x "$PROJECT_ROOT"/.git-hooks/* 2>/dev/null || true
    log_success "Git hooks configured (.git-hooks/)."
else
    log_warning ".git-hooks/ not found — skipping hook setup."
fi

# 6. Verify
if command_exists dotnet; then
    log_info ".NET SDK:"
    dotnet --version
fi
if command_exists protoc; then
    log_info "Protobuf:"
    protoc --version
fi

echo ""
echo "╔══════════════════════════════════════════╗"
echo "║  ✓ ResQ .NET SDK setup complete          ║"
echo "╚══════════════════════════════════════════╝"
echo ""
echo "Next steps:"
echo "  nix develop                          # Enter dev shell (Linux)"
echo "  dotnet build -c Release              # Build all projects"
echo "  dotnet test -c Release               # Run test suite"
echo "  docker build -t resq-dotnet-sdk .    # Build + test via Docker"
echo "  docker build --target pack .         # Produce NuGet packages"

