# CI/CD Documentation

This document describes the GitHub Actions workflows used for continuous integration, deployment, and maintenance of this project.

## Workflow Overview

| Workflow | File | Trigger | Purpose |
|----------|------|---------|---------|
| CI Pull Request | `ci-pr.yml` | Pull requests | Fast feedback on code changes |
| CI/CD Main | `ci-main.yml` | Push to main/staging | Build, test, and deploy |
| Deploy | `cd-deploy.yml` | Called by other workflows | Reusable deployment logic |
| Promote | `cd-promote.yml` | Manual | Promote images between environments |
| Release | `release.yml` | Version tags (v*.*.*) | Create GitHub releases |
| Rollback | `rollback.yml` | Manual | Emergency rollback |
| Security Scan | `scheduled-security.yml` | Daily at 2 AM UTC | Continuous security monitoring |
| Cleanup | `scheduled-cleanup.yml` | Weekly on Sundays | Registry and artifact cleanup |

## Workflow Details

### CI Pull Request (`ci-pr.yml`)

Provides fast feedback on pull requests. Runs in parallel jobs for efficiency.

**Triggers:**
- Pull request opened, synchronized, reopened, or marked ready for review
- Skips draft PRs
- Ignores changes to markdown files and docs/

**Jobs:**
1. **Validate** - Code formatting, go mod verify, diff stats, PR comment
2. **Test** - Unit tests with race detection and coverage
3. **Lint** - golangci-lint
4. **Security Scan** - gosec, go vet
5. **Build Validation** - Docker build (no push) to verify Dockerfile
6. **Dependency Review** - Check for vulnerable dependencies

### CI/CD Main (`ci-main.yml`)

Main pipeline for branch builds and deployments.

**Triggers:**
- Push to `main` or `staging` branches
- Manual workflow dispatch
- Ignores changes to markdown files and docs/

**Jobs:**
1. **Test** - Full test suite with coverage reports
2. **Security Scan** - gosec, staticcheck, govulncheck
3. **Build** - Docker image build and push to GHCR, SBOM generation
4. **Container Scan** - Trivy vulnerability scanning
5. **Prepare Deploy** - Compute deployment image tag
6. **Deploy Staging** - Deploy to staging (staging branch only)
7. **Deploy Production** - Deploy to production (main branch only)

**Concurrency:** Only one pipeline runs per branch at a time. Does not cancel in-progress runs.

### Deploy (`cd-deploy.yml`)

Reusable workflow for SSH-based Docker deployments.

**Inputs:**
- `environment` - Target environment (staging/production)
- `image-tag` - Docker image tag to deploy

**Process:**
1. SSH to Docker host
2. Pull new image from GHCR
3. Stop and remove existing container
4. Start new container with health checks
5. Wait for container to become healthy
6. Clean up old images (keeps last 3)

**Health Check:** Waits up to 60 seconds for container health status.

### Promote (`cd-promote.yml`)

Promotes a tested image from one environment to another without rebuilding.

**Inputs:**
- `source_environment` - Environment to promote from (staging)
- `target_environment` - Environment to promote to (production)
- `image_tag` - Image tag to promote (e.g., staging-abc1234)

**Process:**
1. Validate promotion path (staging → production only)
2. Verify source image exists
3. Pull and retag image for target environment
4. Deploy to target environment

**Use Case:** After testing in staging, promote the exact same image to production.

### Release (`release.yml`)

Creates versioned releases when tags are pushed.

**Triggers:**
- Push of tags matching `v*.*.*` (e.g., v1.0.0, v1.2.3-beta)

**Process:**
1. Validate tag is on main branch
2. Run tests
3. Build and push image with version tags
4. Generate changelog from commits since last release
5. Create GitHub Release with SBOM attachment

**Version Tags Created:**
- `v1.2.3` - Full version
- `v1.2` - Minor version
- `v1` - Major version (except for v0.x)
- `latest` - For stable releases only

**Prerelease Detection:** Tags containing `-` (e.g., v1.0.0-alpha) are marked as prereleases.

### Rollback (`rollback.yml`)

Emergency rollback to a previous image.

**Inputs:**
- `environment` - Environment to rollback (staging/production)
- `image_tag` - Image tag to rollback to (e.g., main-abc1234)

**Process:**
1. Verify target image exists in registry
2. Create backup of current container
3. Deploy rollback image
4. If health check fails, restore from backup

### Scheduled Security (`scheduled-security.yml`)

Daily security scanning independent of code changes.

**Schedule:** Daily at 2 AM UTC

**Scans:**
- gosec on main and staging branches
- govulncheck for Go vulnerability database
- Trivy container scans for deployed images

**Results:** Uploaded to GitHub Security tab and stored as artifacts.

### Scheduled Cleanup (`scheduled-cleanup.yml`)

Weekly cleanup of old images and artifacts.

**Schedule:** Sundays at 3 AM UTC

**Cleanup Targets:**
- Container images older than 90 days
- Workflow artifacts older than 90 days

**Protected Tags (never deleted):**
- `latest`
- `main`
- `staging`
- Version tags (`v*.*.*`)

**Manual Mode:** Supports dry-run for testing cleanup logic.

## Workflow Relationships

```
┌─────────────────┐
│  Pull Request   │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│   ci-pr.yml     │ ──► Fast feedback (no deployment)
└─────────────────┘

┌─────────────────┐
│ Push to Branch  │
└────────┬────────┘
         │
         ▼
┌─────────────────┐     ┌─────────────────┐
│  ci-main.yml    │ ──► │  cd-deploy.yml  │ ──► staging/production
└─────────────────┘     └─────────────────┘

┌─────────────────┐     ┌─────────────────┐
│ Manual Promote  │ ──► │ cd-promote.yml  │ ──► staging → production
└─────────────────┘     └─────────────────┘

┌─────────────────┐     ┌─────────────────┐
│  Tag: v*.*.*    │ ──► │  release.yml    │ ──► GitHub Release
└─────────────────┘     └─────────────────┘

┌─────────────────┐
│ Manual Rollback │ ──► rollback.yml ──► Restore previous version
└─────────────────┘

┌─────────────────┐
│  Cron Schedule  │ ──► scheduled-security.yml (daily)
│                 │ ──► scheduled-cleanup.yml (weekly)
└─────────────────┘
```

## Environment Configuration

### GitHub Environments

Configure these environments in GitHub repository settings:

**staging:**
- No approval required
- Branch restriction: `staging`

**production:**
- Approval required (recommended: 2 reviewers)
- Branch restriction: `main`

### Required Secrets

| Secret | Description | Scope |
|--------|-------------|-------|
| `DOCKER_HOST` | SSH host address for Docker server | Per environment |
| `DOCKER_USER` | SSH username | Per environment |
| `SSH_PRIVATE_KEY` | SSH private key for authentication | Per environment |
| `SSH_PORT` | SSH port (optional, defaults to 22) | Per environment |

### Required Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `CONTAINER_NAME` | Docker container name | `go-app-staging` |
| `HOST_PORT` | Port exposed on host | `8080` |
| `CONTAINER_PORT` | Port inside container | `8080` |
| `DOCKER_HOST` | Host address for health checks | `staging.example.com` |

## Common Operations

### Deploying to Staging

Push to the staging branch:
```bash
git checkout staging
git merge feature/my-feature
git push origin staging
```

The `ci-main.yml` workflow will automatically build and deploy.

### Promoting to Production

Option 1: Push to main
```bash
git checkout main
git merge staging
git push origin main
```

Option 2: Use the promote workflow
1. Go to Actions → Promote Environment
2. Select source: staging
3. Select target: production
4. Enter the image tag (e.g., `staging-abc1234`)

### Creating a Release

```bash
git checkout main
git tag v1.0.0
git push origin v1.0.0
```

The `release.yml` workflow will create a GitHub Release automatically.

### Rolling Back

1. Go to Actions → Rollback Deployment
2. Select the environment
3. Enter the image tag to rollback to

Find available tags in the GitHub Container Registry or recent workflow runs.

### Running Security Scans Manually

1. Go to Actions → Scheduled Security Scan
2. Click "Run workflow"

### Testing Cleanup (Dry Run)

1. Go to Actions → Scheduled Cleanup
2. Click "Run workflow"
3. Keep "Dry run" checked to see what would be deleted

## Troubleshooting

### Workflow Not Triggering

- Check if the file changed is in `paths-ignore`
- Verify branch protection rules
- Check if PR is in draft state (skipped by ci-pr.yml)

### Deployment Failed

1. Check the workflow logs for SSH connection errors
2. Verify secrets are configured for the environment
3. Check container health in deployment logs
4. Use rollback workflow if needed

### Container Not Healthy

The deployment waits 60 seconds for health checks. If failing:
1. Check application logs: `docker logs <container-name>`
2. Verify the health check endpoint is responding
3. Check if the correct port is exposed

### Image Not Found

When promoting or rolling back:
1. Verify the exact image tag exists in GHCR
2. Check the tag format matches (e.g., `staging-abc1234`)
3. Ensure the image wasn't cleaned up by the scheduled cleanup
