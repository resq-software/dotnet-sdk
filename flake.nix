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

{
  description = "ResQ .NET SDK — core libraries and client packages";

  inputs = {
    nixpkgs.url = "github:nixos/nixpkgs/nixos-24.11";
    flake-utils.url = "github:numtide/flake-utils";
  };

  outputs =
    { self, nixpkgs, flake-utils, ... }:
    let
      supportedSystems = [
        "x86_64-linux"
        "aarch64-linux"
        "x86_64-darwin"
        "aarch64-darwin"
      ];

      mkDevShell = pkgs: system:
        let
          devPackages = with pkgs;
            builtins.filter (p: p != null) [
              git
              ripgrep
              fd
              jq

              # Protobuf (protos/ used for code generation)
              protobuf

              # Vulnerability scanner (pre-commit OSV scan)
              osv-scanner

              # .NET SDK (Linux only — not packaged for macOS in nixpkgs)
              (if stdenv.isLinux then dotnet-sdk_9 else null)
            ];

          shellHook = ''
            echo "--- ResQ .NET SDK Dev Environment (${system}) ---"

            version_check() {
              local cmd="$1" name="$2"
              if command -v "$cmd" >/dev/null 2>&1; then
                echo "$name: $("$cmd" --version 2>/dev/null | head -n1 | xargs)"
              else
                echo "$name: NOT FOUND"
              fi
            }

            version_check protoc "Protobuf"

            if ${pkgs.lib.boolToString pkgs.stdenv.isLinux}; then
              version_check dotnet ".NET"
              echo ""
              echo "Build:   dotnet build -c Release"
              echo "Test:    dotnet test -c Release"
              echo "Pack:    dotnet pack -c Release --no-build"
            else
              echo ".NET: not available on macOS via nixpkgs — install from https://dotnet.microsoft.com"
            fi

            echo "--------------------------------------------------"
          '';
        in
        {
          default = pkgs.mkShell {
            packages = devPackages;
            inherit shellHook;
          };
        };
    in
    flake-utils.lib.eachSystem supportedSystems (system:
      let
        pkgs = import nixpkgs {
          inherit system;
          config.allowUnfree = true;
        };
      in
      {
        formatter = pkgs.alejandra or pkgs.nixpkgs-fmt;
        devShells = mkDevShell pkgs system;
      }
    );
}
