# RecEng — Developer Guide

## Stack

- **API** — ASP.NET Core (.NET 10), PostgreSQL, Entity Framework Core
- **Analytics Service** — .NET Worker Service, TimescaleDB, MassTransit
- **Client** — Flutter (web), served via Nginx
- **Infrastructure** — Docker Compose, RabbitMQ, Redis

## Running the project

### 1. Generate TLS certificates (first time only)

HTTPS requires a self-signed certificate. Replace the IP with the local IP of the machine running Docker:

```bash
mkdir -p nginx/certs
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout nginx/certs/key.pem \
  -out nginx/certs/cert.pem \
  -subj "/CN=192.168.50.200" \
  -addext "subjectAltName=IP:192.168.50.200"
```

The `nginx/certs/` directory is gitignored — certificates are generated locally and never committed.

On first visit the browser will warn that the certificate is not trusted. Click through the warning, or install `nginx/certs/cert.pem` as a trusted certificate on the device.

### 2. Configure environment variables

```bash
cp .env.example .env
```

Fill in `.env` with real values. The file is gitignored and never committed.

### 3. Start

```bash
docker compose up --build
```

| Service             | URL                          |
|---------------------|------------------------------|
| Client              | https://192.168.1.50         |
| RabbitMQ Management | http://localhost:15672        |
| Postgres            | localhost:5432                |
| TimescaleDB         | localhost:5433                |
| Redis               | localhost:6379                |

RabbitMQ Management credentials: values from `.env`

The API runs migrations and seeds data automatically on startup.

## Services

### API (`api/`)
ASP.NET Core — the single entry point for the client. Handles auth, videos, users, and interactions. Publishes events to RabbitMQ on likes and watch interactions.

### Analytics Service (`analytics-service/`)
.NET Worker Service — consumes events from RabbitMQ and stores them in TimescaleDB. No HTTP exposure.

### Shared Contracts (`RecEng.Contracts/`)
Class library containing event types shared between the API and Analytics Service. Both projects reference this — never define event contracts in a single service.

## Project structure

```
api/                   ASP.NET Core API
api.Tests/             API tests
analytics-service/     .NET Worker Service
RecEng.Contracts/      Shared event contracts
client/                Flutter web client
timescale-init/        TimescaleDB init SQL script
devAssets/             Test videos (checked in, dev only)
```

## Dev seeding

On startup in development, `DevSeeder` runs automatically if no videos exist in the database. It:

1. Creates a `seed_user` (password: `seed_password`)
2. Copies all `.mp4` files from `devAssets/` into `uploads/`
3. Inserts video records with titles, descriptions, and tags

To re-seed: clear the `videos` table (and `uploads/`) and restart the API.

To add or change seed metadata, edit `api/Data/DevSeeder.cs`.

## TimescaleDB

The init script at `timescale-init/01_init.sql` runs automatically on first start when the volume is empty. To recreate the schema from scratch:

```bash
docker compose down -v
docker compose up --build
```

## Configuration

All sensitive values are configured via `.env` (see `.env.example`). Docker Compose reads this file automatically on startup.

For local development outside Docker, `appsettings.json` defaults apply (connects to `localhost:5432`).

## Deployment

Pushes to `main` trigger an automatic deploy via GitHub Actions (`.github/workflows/deploy.yml`). The workflow SSHes into the server, pulls the latest code, writes `.env` from GitHub Secrets, and restarts the containers.

### First-time server setup

```bash
git clone https://github.com/your-username/RecEng.git ~/RecEng
cd ~/RecEng
mkdir -p nginx/certs
# Generate certificates (see above, using the server's IP)
docker compose up -d
```

### GitHub Secrets

Add the following secrets under `Settings → Secrets and variables → Actions`:

| Secret | Description |
|---|---|
| `SSH_HOST` | Server IP address |
| `SSH_USER` | SSH username on the server |
| `SSH_PRIVATE_KEY` | Private key for SSH access |
| `POSTGRES_USER` | PostgreSQL username |
| `POSTGRES_PASSWORD` | PostgreSQL password |
| `POSTGRES_DB` | PostgreSQL database name |
| `TIMESCALE_USER` | TimescaleDB username |
| `TIMESCALE_PASSWORD` | TimescaleDB password |
| `TIMESCALE_DB` | TimescaleDB database name |
| `RABBITMQ_USER` | RabbitMQ username |
| `RABBITMQ_PASSWORD` | RabbitMQ password |
| `JWT_KEY` | JWT signing key (min 32 characters) |
| `JWT_ISSUER` | JWT issuer |
| `JWT_AUDIENCE` | JWT audience |

### SSH key setup

Generate a dedicated deploy key on your local machine:

```bash
ssh-keygen -t ed25519 -C "github-deploy" -f ~/.ssh/receng_deploy
```

Add the public key to the server:

```bash
echo "$(cat ~/.ssh/receng_deploy.pub)" >> ~/.ssh/authorized_keys
```

Add the contents of `~/.ssh/receng_deploy` (private key) as the `SSH_PRIVATE_KEY` secret in GitHub.
