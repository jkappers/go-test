# Go Sample Application

[![CI/CD Pipeline](https://github.com/example/go-sample/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/example/go-sample/actions/workflows/ci-cd.yml)

A simple Go HTTP server

## Quick Start

### Local Development

```bash
# Run tests
go test -v ./...

# Build and run locally
docker build -t sample-app .
docker run -p 2593:2593 sample-app

# Test endpoints
curl http://localhost:2593
curl http://localhost:2593/health
```

## API Endpoints

- `GET /` - Returns greeting with hostname
- `GET /health` - Health check endpoint (returns 200 OK)

## Rollback

To rollback to a previous version:

1. Go to Actions → Rollback Deployment
2. Run workflow with previous image tag
3. Example tag: `main-abc1234` or `sha-abc1234`
