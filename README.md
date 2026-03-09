# RecEng — Developer Guide

## Stack

- **API** — ASP.NET Core (.NET 10), PostgreSQL, Entity Framework Core
- **Client** — Flutter (web), served via Nginx
- **Infrastructure** — Docker Compose

## Running the project

```bash
docker compose up --build
```

| Service  | URL                    |
|----------|------------------------|
| Client   | http://localhost:8080  |
| API      | http://localhost:5000  |
| Postgres | localhost:5432         |

The API runs migrations and seeds data automatically on startup.

## Dev seeding

On startup in development, `DevSeeder` runs automatically if no videos exist in the database. It:

1. Creates a `seed_user` (password: `seed_password`)
2. Copies all `.mp4` files from `devAssets/` into `uploads/`
3. Inserts video records with titles, descriptions, and tags

To re-seed: clear the `videos` table (and `uploads/`) and restart the API.

To add or change seed metadata, edit `api/Data/DevSeeder.cs`.

## Project structure

```
api/           ASP.NET Core API
api.Tests/     API tests
client/        Flutter web client
devAssets/     Test videos (checked in, dev only)
```

## Configuration

The API reads config from `appsettings.json`. For local development outside Docker, the defaults work as-is (connects to `localhost:5432`).

JWT settings are in `appsettings.json` under `Jwt`. Change the key before deploying.
