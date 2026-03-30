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
read SOURCE_REF < "${ROOT_DIR}/proto-source.lock"
TARGET_DIR="${ROOT_DIR}/protos"

if [[ -z "${SOURCE_REF//[[:space:]]/}" ]]; then
  echo "proto-source.lock is empty or invalid. Expected '<module>:<revision>'." >&2
  exit 1
fi

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
