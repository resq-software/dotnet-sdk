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

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
read -r REVISION < "${ROOT_DIR}/proto-source.lock"
TARGET_DIR="${ROOT_DIR}/protos"

# proto-source.lock pins only the schema REVISION. The module identifier it
# belongs to is not recorded in this repository: this repo is public, the
# module is not, and naming it here would disclose a non-public resource to
# every reader. Set RESQ_PROTO_MODULE to supply it.
#
# This costs public consumers nothing. `protos/` is a checked-in cache of the
# generated schemas, so building this SDK never contacts the registry; only
# someone re-syncing the cache needs the module, and they already have
# registry access.
if [[ -z "${REVISION//[[:space:]]/}" ]]; then
  echo "proto-source.lock is empty or invalid. Expected a bare '<revision>'." >&2
  exit 1
fi

if [[ ! "${REVISION}" =~ ^[0-9a-f]{32}$ ]]; then
  echo "proto-source.lock does not look like a Buf revision: '${REVISION}'." >&2
  echo "Expected 32 lowercase hex characters, with no module prefix." >&2
  exit 1
fi

if [[ -z "${RESQ_PROTO_MODULE:-}" ]]; then
  cat >&2 <<'MSG'
RESQ_PROTO_MODULE is not set, so the schema module to export is unknown.

This is expected when working from a clone of the public SDK: the module is
not public and is therefore not named in this repository. You do not need it
to build -- protos/ is a checked-in cache and the build reads that.

Set RESQ_PROTO_MODULE to the Buf module path if you are re-syncing the cache.
MSG
  exit 1
fi

SOURCE_REF="${RESQ_PROTO_MODULE}:${REVISION}"

TMP_DIR="$(mktemp -d)"
trap 'rm -rf "${TMP_DIR}"' EXIT

if command -v buf >/dev/null 2>&1; then
  buf export "${SOURCE_REF}" --output "${TMP_DIR}/export"
elif command -v docker >/dev/null 2>&1; then
  docker_args=(
    run --rm
    -u "$(id -u):$(id -g)"
    -e HOME=/tmp
    -e XDG_CACHE_HOME=/tmp/.cache
    -v "${TMP_DIR}:/out"
  )

  if [[ -n "${BUF_TOKEN:-}" ]]; then
    docker_args+=(-e BUF_TOKEN)
  fi

  docker "${docker_args[@]}" \
    bufbuild/buf:latest \
    export "${SOURCE_REF}" --output /out/export
else
  echo "buf CLI or docker is required to sync shared schemas from the Buf Schema Registry." >&2
  exit 1
fi

shopt -s nullglob
proto_files=("${TMP_DIR}/export/"*.proto)

if [[ ${#proto_files[@]} -eq 0 ]]; then
  echo "buf export did not produce any .proto files for ${SOURCE_REF}." >&2
  exit 1
fi

find "${TARGET_DIR}" -maxdepth 1 -type f -name '*.proto' -delete
cp "${proto_files[@]}" "${TARGET_DIR}/"

echo "Synced ${#proto_files[@]} shared protobuf files into ${TARGET_DIR}."
