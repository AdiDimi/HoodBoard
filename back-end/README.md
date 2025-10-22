# AdsApi (Production-Ready)

A .NET 8 Minimal API for classified ads with:
- RedisJSON repository + Outbox writer → atomic `Data/ads.json` snapshot
- FluentValidation with auto endpoint validation
- Photo uploads with ImageSharp (large + thumbnail)
- Global error middleware (ProblemDetails + `code` + `traceId`)
- OpenAPI/Swagger for model/client generation
- ETags (304/412) + pagination headers
- Serilog structured logging + deterministic sampling of INFO successes
- Docker Compose with Redis Stack (includes RedisJSON)

## Quick Start

### Local
```bash
dotnet restore
dotnet run
# Open http://localhost:5080 (Swagger UI) or /swagger
```

### Docker
```bash
docker compose up --build
# API: http://localhost:8080  |  Health: /healthz  |  Swagger: /
```

## Useful Headers
- `Idempotency-Key: <uuid>` on POST/PUT/DELETE (safe retries)
- `If-None-Match` on GET (ETag) → 304
- `If-Match` on PUT (ETag) → 412 on mismatch

## Uploads
POST `/api/ads/{id}/photos` with `multipart/form-data` (`files[]` or any file fields).
Files saved under `/wwwroot/uploads`, variants in `large/` and `thumbs/`.

## Config
See `appsettings.json` for repository + logging sampling rates.
Environment overrides via `Ads__Repository__*` variables.

## Notes
- Uses `redis/redis-stack` to enable RedisJSON commands.
- JSON snapshot at `Data/ads.json`
