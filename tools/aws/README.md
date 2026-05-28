# Public AWS demo

A one-off public demo on a single t3.medium EC2 in **eu-west-2**, plain
HTTP via Caddy on port 80. Designed to be cheap when idle: stop the
instance and only the EBS volume bills (~$2.40/mo); start it again when
you want to demo.

The unsigned dev bearer token is intentionally still in play — the demo
data is fake.

## Prerequisites

- AWS CLI configured for an account in **eu-west-2** with EC2 + VPC + SSM
  permissions. Verify with `aws sts get-caller-identity`.
- This repo pushed to **public** GitHub. The EC2 user-data script clones
  it over HTTPS without credentials. If the repo is private, switch to a
  deploy key (out of scope here).
- The working tree has an `origin` remote that points at the public repo.

## One-off setup

```
bash tools/aws/bootstrap.sh
```

That:
1. Resolves your current public IP and opens TCP 22 to it (plus TCP 80
   to the world).
2. Creates the SSH key pair `student-search-demo` and saves the private
   key to `tools/aws/student-search-demo.pem`.
3. Launches a t3.medium Ubuntu 24.04 instance with 30 GB gp3 root.
4. Cloud-init installs Docker, clones the repo, and runs
   `docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build`.
5. Writes the instance id to `tools/aws/.instance-id` (gitignored) and
   prints the public DNS.

First boot takes ~5 minutes to finish the image build. SSH is reachable
first; HTTP isn't ready until the build completes.

## One-off reindex

The seed isn't loaded into Elasticsearch automatically. After first boot,
populate it once:

```
DNS=$(aws ec2 describe-instances --region eu-west-2 \
  --instance-ids $(cat tools/aws/.instance-id) \
  --query 'Reservations[0].Instances[0].PublicDnsName' --output text)
TOKEN=$(printf '%s' '{"sub":"admin","name":"admin","scopes":[{"type":"global"}]}' \
  | base64 -w0 | tr '+/' '-_' | tr -d '=')
curl -X POST "http://${DNS}/api/admin/reindex" -H "Authorization: Bearer ${TOKEN}"
```

Expect `{"indexName":"students","documentsIndexed":2511,"status":"ok"}`.

The EBS volume persists across stop/start, so reindex is genuinely only
needed once.

## Day-to-day on/off

```
bash tools/aws/demo-up.sh    # start; prints the (possibly new) URL
bash tools/aws/demo-down.sh  # stop; compute billing pauses
```

The public DNS **changes every start**. Re-share the URL each demo, or
add an Elastic IP if a stable URL is worth ~$3.60/mo while stopped.

## When you're done forever

```
INSTANCE_ID=$(cat tools/aws/.instance-id)
aws ec2 terminate-instances --region eu-west-2 --instance-ids "${INSTANCE_ID}"
aws ec2 wait instance-terminated --region eu-west-2 --instance-ids "${INSTANCE_ID}"
aws ec2 delete-security-group --region eu-west-2 --group-name student-search-demo
aws ec2 delete-key-pair --region eu-west-2 --key-name student-search-demo
rm tools/aws/.instance-id tools/aws/student-search-demo.pem
```

## Cost (May 2026, eu-west-2)

| State    | Compute             | Storage      |
|----------|---------------------|--------------|
| running  | $0.0464/hr (~$33/mo if always on) | ~$2.40/mo (30 GB gp3) |
| stopped  | $0                  | ~$2.40/mo    |

Example: 4 hours of demo use per month → ~$2.60/mo total.

## SSH for debugging

```
DNS=$(aws ec2 describe-instances --region eu-west-2 \
  --instance-ids $(cat tools/aws/.instance-id) \
  --query 'Reservations[0].Instances[0].PublicDnsName' --output text)
ssh -i tools/aws/student-search-demo.pem ubuntu@"${DNS}"
```

Cloud-init logs at `/var/log/cloud-init-output.log`. The app lives at
`/opt/student-search`.
