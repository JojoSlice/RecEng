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
  -subj "/CN=192.168.1.50" \
  -addext "subjectAltName=IP:192.168.1.50"
```

The `nginx/certs/` directory is gitignored — certificates are generated locally and never committed.

On first visit the browser will warn that the certificate is not trusted. Click through the warning, or install `nginx/certs/cert.pem` as a trusted certificate on the device.

### 2. Start

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

RabbitMQ Management credentials: `receng` / `receng`

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

The API reads config from `appsettings.json`. For local development outside Docker, the defaults work as-is (connects to `localhost:5432`).

JWT settings are in `appsettings.json` under `Jwt`. Change the key before deploying.
