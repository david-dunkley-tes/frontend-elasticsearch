#!/usr/bin/env bash
# One-time AWS setup for the public demo. Creates a security group, an SSH
# key pair, and a t3.medium Ubuntu EC2 instance in eu-west-2 that runs the
# stack via cloud-init. The instance ID is written to tools/aws/.instance-id;
# from then on use demo-up.sh / demo-down.sh.
#
# Usage:
#   tools/aws/bootstrap.sh                # uses `git remote get-url origin`
#   tools/aws/bootstrap.sh --repo-url URL # override
set -euo pipefail

# Stop Git Bash / MSYS from translating arguments that start with "/" into
# Windows paths (it mangles things like /dev/sda1 → C:/Program Files/Git/dev/sda1).
# We do explicit cygpath conversion below for paths that genuinely need it.
export MSYS_NO_PATHCONV=1

REGION="eu-west-2"
NAME="student-search-demo"
INSTANCE_TYPE="t3.medium"
ROOT_VOL_GIB=30
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
INSTANCE_ID_FILE="${HERE}/.instance-id"
KEY_FILE="${HERE}/${NAME}.pem"
USER_DATA_TEMPLATE="${HERE}/user-data.sh"
USER_DATA_RENDERED="${HERE}/.user-data.rendered.sh"
trap 'rm -f "${USER_DATA_RENDERED}"' EXIT

REPO_URL=""
while [ $# -gt 0 ]; do
  case "$1" in
    --repo-url) REPO_URL="$2"; shift 2 ;;
    *) echo "unknown arg: $1" >&2; exit 2 ;;
  esac
done

if [ -z "${REPO_URL}" ]; then
  REPO_URL="$(git -C "${HERE}/../.." remote get-url origin)"
fi
case "${REPO_URL}" in
  git@github.com:*)
    REPO_URL="https://github.com/${REPO_URL#git@github.com:}"
    REPO_URL="${REPO_URL%.git}.git"
    ;;
esac
echo "Repo URL: ${REPO_URL}"

if [ -f "${INSTANCE_ID_FILE}" ]; then
  echo "Refusing to bootstrap: ${INSTANCE_ID_FILE} already exists (instance $(cat "${INSTANCE_ID_FILE}"))."
  echo "Use demo-up.sh / demo-down.sh, or delete the file to bootstrap again."
  exit 1
fi

aws sts get-caller-identity --region "${REGION}" >/dev/null

MY_IP="$(curl -fsS https://checkip.amazonaws.com)"
echo "Your public IP: ${MY_IP}"

echo "Resolving latest Ubuntu 24.04 LTS AMI in ${REGION}..."
AMI_ID="$(aws ec2 describe-images --region "${REGION}" \
  --owners 099720109477 \
  --filters \
    'Name=name,Values=ubuntu/images/hvm-ssd-gp3/ubuntu-noble-24.04-amd64-server-*' \
    'Name=state,Values=available' \
    'Name=architecture,Values=x86_64' \
  --query 'sort_by(Images, &CreationDate) | [-1].ImageId' --output text)"
if [ -z "${AMI_ID}" ] || [ "${AMI_ID}" = "None" ]; then
  echo "ERROR: could not resolve an Ubuntu 24.04 LTS AMI in ${REGION}." >&2
  exit 1
fi
echo "AMI: ${AMI_ID}"

VPC_ID="$(aws ec2 describe-vpcs --region "${REGION}" \
  --filters 'Name=is-default,Values=true' \
  --query 'Vpcs[0].VpcId' --output text)"
echo "Default VPC: ${VPC_ID}"

SG_ID="$(aws ec2 describe-security-groups --region "${REGION}" \
  --filters "Name=group-name,Values=${NAME}" "Name=vpc-id,Values=${VPC_ID}" \
  --query 'SecurityGroups[0].GroupId' --output text 2>/dev/null || echo None)"
if [ "${SG_ID}" = "None" ] || [ -z "${SG_ID}" ]; then
  echo "Creating security group ${NAME}..."
  SG_ID="$(aws ec2 create-security-group --region "${REGION}" \
    --group-name "${NAME}" --description "Public demo for student search" \
    --vpc-id "${VPC_ID}" --query 'GroupId' --output text)"
  aws ec2 authorize-security-group-ingress --region "${REGION}" \
    --group-id "${SG_ID}" --protocol tcp --port 80 --cidr 0.0.0.0/0 >/dev/null
  aws ec2 authorize-security-group-ingress --region "${REGION}" \
    --group-id "${SG_ID}" --protocol tcp --port 22 --cidr "${MY_IP}/32" >/dev/null
  echo "Security group ${SG_ID} created."
else
  echo "Security group ${SG_ID} already exists; leaving rules untouched."
fi

if ! aws ec2 describe-key-pairs --region "${REGION}" --key-names "${NAME}" >/dev/null 2>&1; then
  echo "Creating SSH key pair ${NAME}..."
  aws ec2 create-key-pair --region "${REGION}" --key-name "${NAME}" \
    --query 'KeyMaterial' --output text > "${KEY_FILE}"
  chmod 600 "${KEY_FILE}"
  echo "Private key written to ${KEY_FILE}"
else
  echo "Key pair ${NAME} already exists in AWS; assuming ${KEY_FILE} is present locally."
  if [ ! -f "${KEY_FILE}" ]; then
    echo "ERROR: AWS has the key pair but ${KEY_FILE} is missing. Delete the AWS key pair and re-run." >&2
    exit 1
  fi
fi

sed "s|__REPO_URL__|${REPO_URL}|g" "${USER_DATA_TEMPLATE}" > "${USER_DATA_RENDERED}"

# Native aws.exe on Windows doesn't understand Git Bash POSIX paths like
# /c/Users/... — convert to a Windows-style path when cygpath is around.
if command -v cygpath >/dev/null 2>&1; then
  USER_DATA_FILEARG="file://$(cygpath -w "${USER_DATA_RENDERED}")"
else
  USER_DATA_FILEARG="file://${USER_DATA_RENDERED}"
fi

echo "Launching ${INSTANCE_TYPE} instance..."
INSTANCE_ID="$(aws ec2 run-instances --region "${REGION}" \
  --image-id "${AMI_ID}" \
  --instance-type "${INSTANCE_TYPE}" \
  --key-name "${NAME}" \
  --security-group-ids "${SG_ID}" \
  --block-device-mappings "DeviceName=/dev/sda1,Ebs={VolumeSize=${ROOT_VOL_GIB},VolumeType=gp3,DeleteOnTermination=true}" \
  --user-data "${USER_DATA_FILEARG}" \
  --tag-specifications "ResourceType=instance,Tags=[{Key=Name,Value=${NAME}}]" \
  --query 'Instances[0].InstanceId' --output text)"

echo "${INSTANCE_ID}" > "${INSTANCE_ID_FILE}"
echo "Instance ${INSTANCE_ID} is launching..."

aws ec2 wait instance-running --region "${REGION}" --instance-ids "${INSTANCE_ID}"

DNS="$(aws ec2 describe-instances --region "${REGION}" --instance-ids "${INSTANCE_ID}" \
  --query 'Reservations[0].Instances[0].PublicDnsName' --output text)"

cat <<MSG

Instance running:
  id:   ${INSTANCE_ID}
  dns:  http://${DNS}

First boot is still installing Docker and building the images. Wait
~5 minutes, then run the one-time reindex from tools/aws/README.md.
After that the SPA is reachable at http://${DNS}/.

MSG
