# .NET Sample Application

A simple .NET 10 Minimal API HTTP server.

## Quick Start

### Local Development

```bash
# Build and run with Docker Compose
docker compose up --build

# Or build and run manually
docker build -t sample-app .
docker run -p 2593:2593 sample-app

# Test endpoints
curl http://localhost:2593
curl http://localhost:2593/health
```

## API Endpoints

- `GET /` - Returns greeting with hostname
- `GET /health` - Health check endpoint (returns 200 OK)

## Configuration

- `PORT` - HTTP port (default: 2593)
