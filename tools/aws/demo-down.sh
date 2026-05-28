#!/usr/bin/env bash
# Stop the demo EC2 instance. Compute billing pauses immediately; the EBS
# volume continues to cost ~$2.40/month while stopped.
set -euo pipefail

REGION="eu-west-2"
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
INSTANCE_ID_FILE="${HERE}/.instance-id"

if [ ! -f "${INSTANCE_ID_FILE}" ]; then
  echo "No instance id found at ${INSTANCE_ID_FILE}." >&2
  exit 1
fi

INSTANCE_ID="$(cat "${INSTANCE_ID_FILE}")"

echo "Stopping ${INSTANCE_ID}..."
aws ec2 stop-instances --region "${REGION}" --instance-ids "${INSTANCE_ID}" >/dev/null
aws ec2 wait instance-stopped --region "${REGION}" --instance-ids "${INSTANCE_ID}"

cat <<MSG

Stopped. Compute billing is paused; only the EBS volume (~$2.40/month) accrues.
Start again with: tools/aws/demo-up.sh
MSG
