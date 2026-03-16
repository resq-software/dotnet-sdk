#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SOURCE_REF="$(tr -d '\n' < "${ROOT_DIR}/proto-source.lock")"
TARGET_DIR="${ROOT_DIR}/protos"
PLACEHOLDER="BOOTSTRAP_PENDING_FIRST_PUBLISH"

if [[ "${SOURCE_REF}" == *"${PLACEHOLDER}"* ]]; then
  echo "proto-source.lock still points at the bootstrap placeholder."
  echo "Publish resq-proto to the BSR, replace the placeholder with a concrete revision, then rerun sync."
  exit 0
fi

if ! command -v buf >/dev/null 2>&1; then
  echo "buf CLI is required to sync shared schemas from the Buf Schema Registry." >&2
  exit 1
fi

TMP_DIR="$(mktemp -d)"
trap 'rm -rf "${TMP_DIR}"' EXIT

buf export "${SOURCE_REF}" --output "${TMP_DIR}/export"

shopt -s nullglob
proto_files=("${TMP_DIR}/export/"*.proto)

if [[ ${#proto_files[@]} -eq 0 ]]; then
  echo "buf export did not produce any .proto files for ${SOURCE_REF}." >&2
  exit 1
fi

find "${TARGET_DIR}" -maxdepth 1 -type f -name '*.proto' -delete
cp "${proto_files[@]}" "${TARGET_DIR}/"

echo "Synced ${#proto_files[@]} shared protobuf files into ${TARGET_DIR}."
