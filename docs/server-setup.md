# Self-Hosted Server Setup

Deploys the Quest Board app to Proxmox using 4 LXC containers. GitHub Actions runs on a dedicated deploy CT and SSHes into the app CT to deploy.

## Architecture

```
Internet ──80/443──► [Proxy CT]  Caddy + Let's Encrypt
                          │
                     :5000│
                     [App CT]  .NET 10 + systemd
                          │
                    :1433 │
                    [SQL Server CT]  already exists

GitHub ──HTTPS──► [Deploy CT]  GitHub Actions runner
                       │
                  SSH  └──────► [App CT]
```

---

## Prerequisites

- Domain name with an A record pointing to your public IP
- Router with ports 80 and 443 forwarded to the Proxy CT's internal IP
- SQL Server CT already running and accessible on the internal network
- All CTs on the same Proxmox internal bridge (e.g. `vmbr0`)

---

## 1. Deploy CT

Create an Ubuntu 22.04 LXC (unprivileged, nesting off). 1 CPU, 512MB RAM is sufficient.

### Create the runner user

```bash
useradd -m -s /bin/bash github-runner
```

### Generate the SSH key pair

This key is used to SSH into the App CT. It never leaves this machine.

```bash
sudo -u github-runner ssh-keygen -t ed25519 -f /home/github-runner/.ssh/id_ed25519 -N ""
```

Print the public key — you will need it in step 2:

```bash
cat /home/github-runner/.ssh/id_ed25519.pub
```

### Install the GitHub Actions runner

Go to your GitHub repository → **Settings → Actions → Runners → New self-hosted runner**.
Select **Linux / x64** and follow the shown commands. Install it under `/home/github-runner/actions-runner/` as the `github-runner` user.

After configuration, install and start it as a service:

```bash
cd /home/github-runner/actions-runner
sudo ./svc.sh install github-runner
sudo ./svc.sh start
```

Verify the runner appears as **Online** in GitHub → Settings → Actions → Runners.

---

## 2. App CT

Create an Ubuntu 22.04 LXC (unprivileged, nesting off). 1–2 CPU, 512MB RAM minimum.

### Install .NET 10 runtime

```bash
apt update && apt install -y wget
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
apt update && apt install -y aspnetcore-runtime-10.0
```

### Create the app user and directories

```bash
useradd -m -s /bin/bash questboard
mkdir -p /opt/questboard
chown questboard:questboard /opt/questboard
mkdir -p /etc/questboard
```

### Add the deploy CT's SSH public key

Paste the public key from step 1 here:

```bash
mkdir -p /home/questboard/.ssh
echo "<paste public key here>" >> /home/questboard/.ssh/authorized_keys
chown -R questboard:questboard /home/questboard/.ssh
chmod 700 /home/questboard/.ssh
chmod 600 /home/questboard/.ssh/authorized_keys
```

Test from the Deploy CT:

```bash
sudo -u github-runner ssh questboard@<APP_CT_IP> "echo connected"
```

### Create the environment file

This file holds all secrets. It is never committed to git.

```bash
cat > /etc/questboard/env <<EOF
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://localhost:5000
ConnectionStrings__DefaultConnection=Server=<SQL_SERVER_CT_IP>;Database=QuestBoard;User Id=sa;Password=<SA_PASSWORD>;TrustServerCertificate=true;
EmailSettings__SmtpUsername=<GMAIL_ADDRESS>
EmailSettings__SmtpPassword=<GMAIL_APP_PASSWORD>
EmailSettings__FromEmail=<FROM_EMAIL>
EOF

chmod 600 /etc/questboard/env
chown questboard:questboard /etc/questboard/env
```

### Create the deploy script

```bash
cat > /opt/questboard/deploy.sh <<'EOF'
#!/bin/bash
set -e

TAG=$1
REPO="theunschut/dnd-quest-board"   # update if repo name differs

if [ -z "$TAG" ]; then
  echo "Usage: deploy.sh <tag>"
  exit 1
fi

echo "Deploying $TAG..."
wget -q -O /tmp/questboard.zip "https://github.com/$REPO/releases/download/$TAG/questboard-$TAG.zip"

sudo systemctl stop questboard
rm -rf /opt/questboard/*
unzip -q /tmp/questboard.zip -d /opt/questboard/
rm /tmp/questboard.zip

# Restore the deploy script itself (unzip overwrote it)
cp "$0" /opt/questboard/deploy.sh
chmod +x /opt/questboard/deploy.sh

sudo systemctl start questboard
echo "Done: $TAG deployed."
EOF

chmod +x /opt/questboard/deploy.sh
chown questboard:questboard /opt/questboard/deploy.sh
```

### Allow questboard to restart the service

```bash
echo "questboard ALL=(ALL) NOPASSWD: /bin/systemctl stop questboard, /bin/systemctl start questboard" \
  > /etc/sudoers.d/questboard
chmod 440 /etc/sudoers.d/questboard
```

### Create the systemd service

```bash
cat > /etc/systemd/system/questboard.service <<EOF
[Unit]
Description=D&D Quest Board
After=network.target

[Service]
User=questboard
WorkingDirectory=/opt/questboard
ExecStart=/usr/bin/dotnet /opt/questboard/EuphoriaInn.Service.dll
Restart=always
RestartSec=10
EnvironmentFile=/etc/questboard/env

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable questboard
```

The service will start automatically on the first deploy.

---

## 3. SQL Server CT

Ensure SQL Server accepts remote connections from the App CT.

### Verify SQL Server listens on all interfaces

```bash
# Check the listening address
ss -tlnp | grep 1433
```

If it only shows `127.0.0.1:1433`, edit `/etc/mssql/mssql.conf` and restart:

```bash
systemctl restart mssql-server
```

### Restrict firewall access

Allow connections only from the App CT:

```bash
ufw allow from <APP_CT_IP> to any port 1433
ufw deny 1433
```

### Test from the App CT

```bash
apt install -y mssql-tools18 unixodbc-dev
/opt/mssql-tools18/bin/sqlcmd -S <SQL_SERVER_CT_IP> -U sa -P '<SA_PASSWORD>' -C -Q "SELECT 1"
```

---

## 4. Proxy CT

Create an Ubuntu 22.04 LXC (unprivileged). 1 CPU, 256MB RAM is enough.

### Install Caddy

```bash
apt install -y debian-keyring debian-archive-keyring apt-transport-https curl
curl -1sLf 'https://dl.cloudsmith.io/public/caddy/stable/gpg.key' | gpg --dearmor -o /usr/share/keyrings/caddy-stable-archive-keyring.gpg
curl -1sLf 'https://dl.cloudsmith.io/public/caddy/stable/debian.deb.txt' | tee /etc/apt/sources.list.d/caddy-stable.list
apt update && apt install -y caddy
```

### Configure

```bash
cat > /etc/caddy/Caddyfile <<EOF
yourdomain.com {
    reverse_proxy <APP_CT_IP>:5000
}
EOF

systemctl restart caddy
systemctl enable caddy
```

Caddy automatically provisions and renews a Let's Encrypt certificate. Port 80 and 443 must be reachable from the internet for this to work.

---

## 5. DNS & Router

| What | Value |
|---|---|
| DNS A record | `yourdomain.com` → your public IP |
| Router port forward 80 | → Proxy CT IP |
| Router port forward 443 | → Proxy CT IP |

Do **not** expose port 5000 (app), 1433 (SQL Server), or 22 (SSH) to the internet.

---

## 6. GitHub Repository Configuration

Add the App CT's internal IP as a repository variable so the workflow can reach it:

**Settings → Secrets and variables → Actions → Variables → New repository variable**

| Name | Value |
|---|---|
| `APP_CT_IP` | internal IP of the App CT (e.g. `10.0.0.5`) |

---

## Deploying

### First deploy (manual)

After all CTs are set up, trigger the first deploy manually from GitHub:

**Actions → Binary Release → Run workflow** → enter the latest tag (e.g. `v1.0.0`)

Or SSH into the App CT and run it directly:

```bash
sudo -u questboard /opt/questboard/deploy.sh v1.0.0
```

### Subsequent deploys

Push a semver tag — the workflow builds, releases, and deploys automatically:

```bash
git tag v1.2.3
git push origin v1.2.3
```

To redeploy an existing release without a new tag: **Actions → Binary Release → Run workflow** → enter the tag.

---

## Checking logs

```bash
# App logs
journalctl -u questboard -f

# Caddy logs
journalctl -u caddy -f
```
