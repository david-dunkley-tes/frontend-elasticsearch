#!/usr/bin/env bash
# Start the demo EC2 instance and print the (possibly new) public URL.
set -euo pipefail

REGION="eu-west-2"
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
INSTANCE_ID_FILE="${HERE}/.instance-id"

if [ ! -f "${INSTANCE_ID_FILE}" ]; then
  echo "No instance id found at ${INSTANCE_ID_FILE}. Run bootstrap.sh first." >&2
  exit 1
fi

INSTANCE_ID="$(cat "${INSTANCE_ID_FILE}")"

STATE="$(aws ec2 describe-instances --region "${REGION}" --instance-ids "${INSTANCE_ID}" \
  --query 'Reservations[0].Instances[0].State.Name' --output text)"
echo "Instance ${INSTANCE_ID} is ${STATE}."

if [ "${STATE}" != "running" ]; then
  echo "Starting..."
  aws ec2 start-instances --region "${REGION}" --instance-ids "${INSTANCE_ID}" >/dev/null
  aws ec2 wait instance-running --region "${REGION}" --instance-ids "${INSTANCE_ID}"
fi

DNS="$(aws ec2 describe-instances --region "${REGION}" --instance-ids "${INSTANCE_ID}" \
  --query 'Reservations[0].Instances[0].PublicDnsName' --output text)"

cat <<MSG

Demo URL: http://${DNS}/

Note: the URL changes every start. Containers usually come back within
~30 seconds; if it's the first boot after bootstrap, allow ~5 minutes
for the image build to finish.

To stop billing for compute: tools/aws/demo-down.sh
MSG
