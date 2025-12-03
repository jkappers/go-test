# GitHub Actions Workflow Architecture Implementation Plan

## Overview

This plan addresses structural issues in the current workflow architecture to eliminate redundancy, add missing operational workflows, and align with industry best practices.

## Current State

| Workflow | Purpose | Issues |
|----------|---------|--------|
| ci-cd.yml | Main pipeline | Triggers on PRs (redundant), develop branch builds but never deploys |
| pr-checks.yml | PR validation | Overlaps with ci-cd.yml on PRs |
| deploy.yml | Reusable deployment | Good as-is |
| rollback.yml | Manual rollback | Good as-is |

## Target State

| Workflow | Purpose | Trigger |
|----------|---------|---------|
| ci-pr.yml | PR validation (fast feedback) | pull_request |
| ci-main.yml | Branch builds + deploy | push to main/staging |
| cd-deploy.yml | Reusable deployment | workflow_call |
| cd-promote.yml | Environment promotion | workflow_dispatch |
| release.yml | Versioned releases | tag push (v*) |
| rollback.yml | Emergency rollback | workflow_dispatch |
| scheduled-security.yml | Nightly security scans | cron |
| scheduled-cleanup.yml | Registry/artifact cleanup | cron |

---

## Phase 1: Fix Critical Issues

### 1.1 Rename and Refactor ci-cd.yml → ci-main.yml

**Changes:**
- Rename file to `ci-main.yml`
- Remove `pull_request` trigger (eliminates redundancy with pr-checks.yml)
- Remove `develop` from branch triggers (no dev environment exists)
- Add `paths-ignore` for documentation files
- Update workflow name to "CI/CD Main"

**Result:** Branch pushes to main/staging trigger builds and deployments. PRs no longer trigger this workflow.

### 1.2 Rename and Enhance pr-checks.yml → ci-pr.yml

**Changes:**
- Rename file to `ci-pr.yml`
- Add `paths-ignore` for documentation files
- Add `ready_for_review` trigger type
- Add lightweight security scan (gosec only, no container scan)
- Add Docker build validation (build but don't push)
- Update workflow name to "CI Pull Request"

**Result:** Single, fast workflow for all PR validation.

### 1.3 Rename deploy.yml → cd-deploy.yml

**Changes:**
- Rename file to `cd-deploy.yml`
- Update references in ci-main.yml

**Result:** Consistent naming convention (ci-* for integration, cd-* for deployment).

---

## Phase 2: Add Promotion Workflow

### 2.1 Create cd-promote.yml

**Purpose:** Promote a tested image from one environment to another without rebuilding.

**Trigger:** Manual workflow_dispatch with inputs:
- `source_environment`: staging
- `target_environment`: production
- `image_tag`: Tag to promote (e.g., staging-abc1234)

**Logic:**
1. Validate source image exists
2. Verify source environment is healthy
3. Retag image for target environment
4. Call cd-deploy.yml to deploy
5. Verify target environment health

**Result:** Promotes the exact tested artifact instead of rebuilding.

---

## Phase 3: Add Release Management

### 3.1 Create release.yml

**Purpose:** Create GitHub releases with changelog when version tags are pushed.

**Trigger:** Push of tags matching `v*.*.*`

**Logic:**
1. Validate tag is on main branch
2. Generate changelog from commits since last release
3. Build and push image with version tag
4. Create GitHub Release with:
   - Changelog
   - SBOM attachment
   - Docker image reference

**Result:** Versioned releases with proper documentation.

---

## Phase 4: Add Scheduled Workflows

### 4.1 Create scheduled-security.yml

**Purpose:** Run comprehensive security scans nightly, independent of code changes.

**Trigger:**
- Cron: `0 2 * * *` (2 AM daily)
- Manual workflow_dispatch

**Logic:**
1. Checkout main and staging branches
2. Run full security suite (gosec, govulncheck, Trivy)
3. Create GitHub issues for new vulnerabilities
4. Update Security tab with SARIF results

**Result:** Catches newly disclosed vulnerabilities in existing code.

### 4.2 Create scheduled-cleanup.yml

**Purpose:** Prevent registry bloat and reduce storage costs.

**Trigger:**
- Cron: `0 3 * * 0` (3 AM every Sunday)
- Manual workflow_dispatch

**Logic:**
1. List all container images in GHCR
2. Delete images older than 90 days, except:
   - Images tagged `latest`
   - Images tagged with version (v*.*.*)
   - Images currently deployed
3. Delete workflow run artifacts older than 90 days
4. Report cleanup summary

**Result:** Automatic housekeeping of old artifacts.

---

## Implementation Order

### Week 1: Critical Fixes

| Step | Task | Estimated Effort |
|------|------|------------------|
| 1 | Rename ci-cd.yml → ci-main.yml, remove PR triggers | 15 min |
| 2 | Rename pr-checks.yml → ci-pr.yml, add path filtering | 15 min |
| 3 | Rename deploy.yml → cd-deploy.yml, update references | 10 min |
| 4 | Add lightweight security scan to ci-pr.yml | 20 min |
| 5 | Test PR and push workflows separately | 30 min |

### Week 2: Promotion & Release

| Step | Task | Estimated Effort |
|------|------|------------------|
| 6 | Create cd-promote.yml | 45 min |
| 7 | Create release.yml | 45 min |
| 8 | Test promotion workflow staging → production | 30 min |
| 9 | Test release workflow with test tag | 30 min |

### Week 3: Scheduled Workflows

| Step | Task | Estimated Effort |
|------|------|------------------|
| 10 | Create scheduled-security.yml | 30 min |
| 11 | Create scheduled-cleanup.yml | 30 min |
| 12 | Test scheduled workflows via manual dispatch | 20 min |
| 13 | Update documentation | 20 min |

---

## File Changes Summary

### Renamed Files
```
.github/workflows/ci-cd.yml      → .github/workflows/ci-main.yml
.github/workflows/pr-checks.yml  → .github/workflows/ci-pr.yml
.github/workflows/deploy.yml     → .github/workflows/cd-deploy.yml
```

### New Files
```
.github/workflows/cd-promote.yml
.github/workflows/release.yml
.github/workflows/scheduled-security.yml
.github/workflows/scheduled-cleanup.yml
```

### Unchanged Files
```
.github/workflows/rollback.yml (keep as-is)
```

---

## Workflow Relationships

```
                    ┌─────────────────┐
                    │   Developer     │
                    └────────┬────────┘
                             │
              ┌──────────────┴──────────────┐
              │                             │
              ▼                             ▼
    ┌─────────────────┐          ┌─────────────────┐
    │  Pull Request   │          │   Push to       │
    │  (any branch)   │          │   main/staging  │
    └────────┬────────┘          └────────┬────────┘
             │                            │
             ▼                            ▼
    ┌─────────────────┐          ┌─────────────────┐
    │   ci-pr.yml     │          │  ci-main.yml    │
    │  - lint         │          │  - test         │
    │  - test         │          │  - security     │
    │  - gosec        │          │  - build+push   │
    │  - build (no    │          │  - container    │
    │    push)        │          │    scan         │
    └─────────────────┘          └────────┬────────┘
                                          │
                                          ▼
                                 ┌─────────────────┐
                                 │  cd-deploy.yml  │
                                 │  (reusable)     │
                                 └────────┬────────┘
                                          │
                        ┌─────────────────┴─────────────────┐
                        │                                   │
                        ▼                                   ▼
               ┌─────────────────┐                ┌─────────────────┐
               │    staging      │                │   production    │
               │   environment   │                │   environment   │
               └────────┬────────┘                └─────────────────┘
                        │                                   ▲
                        │         ┌─────────────────┐       │
                        └────────►│ cd-promote.yml  │───────┘
                                  │ (manual)        │
                                  └─────────────────┘

    ┌─────────────────┐          ┌─────────────────┐
    │  Tag: v*.*.*    │          │   Cron Jobs     │
    └────────┬────────┘          └────────┬────────┘
             │                            │
             ▼                            ├──► scheduled-security.yml
    ┌─────────────────┐                   │    (nightly)
    │  release.yml    │                   │
    │  - changelog    │                   └──► scheduled-cleanup.yml
    │  - github       │                        (weekly)
    │    release      │
    └─────────────────┘

    ┌─────────────────┐
    │ rollback.yml    │◄──── Manual trigger (emergency)
    └─────────────────┘
```

---

## Success Criteria

- [ ] PRs trigger only ci-pr.yml (no duplicate runs)
- [ ] Pushes to main/staging trigger ci-main.yml and deploy
- [ ] Documentation changes don't trigger workflows
- [ ] Promotion workflow successfully moves images between environments
- [ ] Release workflow creates GitHub releases on version tags
- [ ] Security scans run nightly without manual intervention
- [ ] Old images are automatically cleaned up weekly
- [ ] All workflows pass validation

---

## Rollback Plan

If issues arise after implementation:

1. **Immediate:** Revert file renames via git
2. **Workflows broken:** Disable via GitHub UI, restore from git history
3. **Deployments affected:** Use existing rollback.yml

All changes are additive or renames; no destructive changes to deployment logic.

---

## Notes

- This plan focuses on workflow architecture, not step-level optimizations (already done)
- Environment protection rules should be configured in GitHub UI after workflows are in place
- Consider adding Dependabot configuration after scheduled workflows are stable
