#!/bin/bash
# Cloud-init script that runs on the EC2 instance's first boot. It is templated
# by tools/aws/bootstrap.sh which substitutes __REPO_URL__ with the public
# GitHub clone URL before passing the file as --user-data.
set -euxo pipefail

export DEBIAN_FRONTEND=noninteractive

apt-get update
apt-get install -y docker.io docker-compose-v2 git curl ca-certificates
systemctl enable --now docker

if [ ! -d /opt/student-search ]; then
  git clone __REPO_URL__ /opt/student-search
fi

cd /opt/student-search
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
