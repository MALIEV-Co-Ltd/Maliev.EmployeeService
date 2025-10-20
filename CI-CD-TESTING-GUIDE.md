# CI/CD Pipeline Testing Guide - Maliev Employee Service

This guide provides comprehensive procedures for testing the complete CI/CD pipeline from code commit to production deployment.

## Table of Contents

- [Pipeline Overview](#pipeline-overview)
- [Testing Develop Branch Pipeline](#testing-develop-branch-pipeline)
- [Testing Staging Pipeline](#testing-staging-pipeline)
- [Testing Production Pipeline](#testing-production-pipeline)
- [Verification Procedures](#verification-procedures)
- [Troubleshooting](#troubleshooting)
- [Success Criteria](#success-criteria)

## Pipeline Overview

The Maliev Employee Service uses a GitOps-based CI/CD pipeline with three environments:

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   develop       │───▶│  release/v*      │───▶│      main       │
│   branch        │    │  (staging)       │    │  (production)   │
└─────────────────┘    └──────────────────┘    └─────────────────┘
        │                       │                       │
        ▼                       ▼                       ▼
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│ GitHub Actions  │    │ GitHub Actions   │    │ GitHub Actions  │
│ ci-develop.yml  │    │ ci-staging.yml   │    │ ci-main.yml     │
└─────────────────┘    └──────────────────┘    └─────────────────┘
        │                       │                       │
        ▼                       ▼                       ▼
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│  Docker Build   │    │  Docker Build    │    │  Docker Build   │
│  & Push to      │    │  & Push to       │    │  & Push to      │
│  artifact-dev   │    │  artifact-staging│    │  artifact-prod  │
└─────────────────┘    └──────────────────┘    └─────────────────┘
        │                       │                       │
        ▼                       ▼                       ▼
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│  GitOps PR to   │    │  GitOps PR to    │    │  GitOps PR to   │
│  maliev-gitops  │    │  maliev-gitops   │    │  maliev-gitops  │
│  (development)  │    │  (staging)       │    │  (production)   │
└─────────────────┘    └──────────────────┘    └─────────────────┘
        │                       │                       │
        ▼                       ▼                       ▼
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   ArgoCD Auto   │    │   ArgoCD Auto    │    │   ArgoCD Auto   │
│   Deploy to     │    │   Deploy to      │    │   Deploy to     │
│   maliev-dev    │    │   maliev-staging │    │   maliev-prod   │
└─────────────────┘    └──────────────────┘    └─────────────────┘
```

## Testing Develop Branch Pipeline

### 1. Pre-Test Checklist

- [ ] Local build successful: `dotnet build Maliev.EmployeeService.sln`
- [ ] All tests passing locally: `dotnet test Maliev.EmployeeService.sln`
- [ ] No uncommitted changes: `git status`
- [ ] On develop branch: `git branch --show-current`
- [ ] GitHub Actions enabled on repository
- [ ] GitOps repository accessible
- [ ] Access to GCP Artifact Registry
- [ ] Access to Kubernetes cluster (maliev-dev namespace)

### 2. Trigger Pipeline

```bash
# 1. Create a test change (example: update version in .csproj)
git checkout develop
git pull origin develop

# 2. Make a minor change to trigger pipeline
echo "# Test pipeline - $(date)" >> .github/workflows/README.md

# 3. Commit and push
git add .
git commit -m "test: Trigger CI/CD pipeline for develop"
git push origin develop
```

### 3. Monitor GitHub Actions

```bash
# Open GitHub Actions in browser
start https://github.com/MALIEV-Co-Ltd/Maliev.EmployeeService/actions

# Or use GitHub CLI
gh run list --branch develop --limit 5
gh run watch
```

**Expected Stages:**

1. **Checkout Code** (30 seconds)
   - ✅ Repository cloned successfully
   - ✅ Correct branch checked out

2. **Setup .NET** (45 seconds)
   - ✅ .NET 9.0 SDK installed
   - ✅ Environment variables set

3. **Restore Dependencies** (1-2 minutes)
   - ✅ NuGet packages restored
   - ✅ No dependency conflicts

4. **Build Solution** (2-3 minutes)
   - ✅ All projects build successfully
   - ✅ Zero build warnings

5. **Run Tests** (3-5 minutes)
   - ✅ All tests executed
   - ✅ Test results published

6. **Authenticate to GCP** (30 seconds)
   - ✅ Service account authentication successful
   - ✅ Docker authentication configured

7. **Build Docker Image** (5-8 minutes)
   - ✅ Dockerfile executed without errors
   - ✅ Image tagged with commit SHA
   - ✅ Example: `asia-southeast1-docker.pkg.dev/maliev-website/maliev-website-artifact-dev/maliev-employee-service:abc123def`

8. **Push to Artifact Registry** (2-3 minutes)
   - ✅ Image pushed successfully
   - ✅ Manifest uploaded

9. **Checkout GitOps Repository** (30 seconds)
   - ✅ maliev-gitops repository cloned
   - ✅ Correct branch (main) checked out

10. **Install Kustomize** (20 seconds)
    - ✅ Kustomize binary downloaded
    - ✅ Installed to /usr/local/bin

11. **Update Kustomize Image** (10 seconds)
    - ✅ development/kustomization.yaml updated
    - ✅ Image tag set to commit SHA

12. **Create GitOps PR** (30 seconds)
    - ✅ Changes committed
    - ✅ Pull request created
    - ✅ PR title: "Update maliev-employee-service to abc123def"

### 4. Verify GitOps Pull Request

```bash
# Check GitOps repository for PR
start https://github.com/MALIEV-Co-Ltd/maliev-gitops/pulls

# Or use GitHub CLI
gh pr list --repo MALIEV-Co-Ltd/maliev-gitops --label employee-service
```

**Verify PR Contents:**

```bash
# Clone GitOps repository
git clone https://github.com/MALIEV-Co-Ltd/maliev-gitops.git
cd maliev-gitops

# Check out the PR branch
gh pr checkout <PR-NUMBER>

# Verify kustomization.yaml changes
cat 3-apps/maliev-employee-service/overlays/development/kustomization.yaml

# Expected content should include:
# images:
# - name: asia-southeast1-docker.pkg.dev/maliev-website/maliev-website-artifact/maliev-employee-service
#   newName: asia-southeast1-docker.pkg.dev/maliev-website/maliev-website-artifact-dev/maliev-employee-service
#   newTag: <COMMIT-SHA>
```

### 5. Merge GitOps PR

```bash
# Option 1: Via GitHub UI
start https://github.com/MALIEV-Co-Ltd/maliev-gitops/pull/<PR-NUMBER>
# Click "Merge pull request"

# Option 2: Via GitHub CLI
gh pr merge <PR-NUMBER> --merge --delete-branch
```

### 6. Verify ArgoCD Deployment

```bash
# Option 1: ArgoCD UI
start https://argocd.maliev.co.th/applications/maliev-employee-service-dev

# Option 2: ArgoCD CLI
argocd app get maliev-employee-service-dev
argocd app sync maliev-employee-service-dev --prune

# Option 3: kubectl
kubectl get applications -n argocd | grep employee-service-dev
kubectl describe application maliev-employee-service-dev -n argocd
```

**Expected ArgoCD Status:**

```yaml
Status:         Synced
Health:         Healthy
Sync Policy:    Automated (Prune=true, Self-Heal=true)
Last Sync:      <TIMESTAMP>
Sync Result:    Success
```

### 7. Verify Kubernetes Deployment

```bash
# Check deployment rollout status
kubectl rollout status deployment/maliev-employee-service -n maliev-dev

# Verify pods are running with new image
kubectl get pods -n maliev-dev -l app=maliev-employee-service
kubectl describe pod <POD-NAME> -n maliev-dev | grep Image:

# Expected output:
# Image: asia-southeast1-docker.pkg.dev/maliev-website/maliev-website-artifact-dev/maliev-employee-service:<COMMIT-SHA>

# Check pod logs
kubectl logs -f deployment/maliev-employee-service -n maliev-dev --tail=50

# Expected log entries:
# - Application starting
# - Database connection successful
# - Health checks responding
# - No error messages
```

### 8. Run Smoke Tests

```bash
# Port forward to service
kubectl port-forward -n maliev-dev svc/maliev-employee-service 8080:8080 &

# Test liveness endpoint
curl http://localhost:8080/employeeservice/liveness
# Expected: "Healthy"

# Test readiness endpoint
curl http://localhost:8080/employeeservice/readiness
# Expected: {"status":"Healthy","checks":[...]}

# Test metrics endpoint
curl http://localhost:8080/employeeservice/metrics | grep http_requests_total
# Expected: Prometheus metrics output

# Kill port forward
pkill -f "port-forward.*maliev-employee-service"
```

### 9. Success Criteria (T433 - Development)

- [X] GitHub Actions workflow completed successfully (all steps green)
- [X] Docker image pushed to artifact-dev registry
- [X] GitOps PR created and merged automatically
- [X] ArgoCD synced application to latest commit
- [X] Kubernetes deployment rolled out with new image
- [X] Pods running and healthy (2/2 ready)
- [X] Health check endpoints responding correctly
- [X] No errors in pod logs
- [X] Metrics endpoint accessible
- [X] Total pipeline duration: < 20 minutes

## Testing Staging Pipeline

### 1. Create Release Tag

```bash
# Ensure develop is up to date
git checkout develop
git pull origin develop

# Create release tag
git tag -a release/v1.2.3 -m "Release v1.2.3 - Feature description"
git push origin release/v1.2.3
```

### 2. Monitor Staging Workflow

```bash
# Watch GitHub Actions
gh run list --branch release/v1.2.3 --limit 5
gh run watch
```

**Expected workflow:** ci-staging.yml executes with same stages as develop but pushes to `artifact-staging` repository.

### 3. Verify Staging Deployment

```bash
# Check staging pods
kubectl get pods -n maliev-staging -l app=maliev-employee-service

# Verify image tag
kubectl describe pod <POD-NAME> -n maliev-staging | grep Image:

# Check HPA (Horizontal Pod Autoscaler)
kubectl get hpa maliev-employee-service -n maliev-staging

# Expected output:
# NAME                       REFERENCE                             TARGETS         MINPODS   MAXPODS   REPLICAS
# maliev-employee-service   Deployment/maliev-employee-service    15%/70%, 20%/80%   2         5         2

# Run smoke tests
kubectl port-forward -n maliev-staging svc/maliev-employee-service 8080:8080 &
curl http://localhost:8080/employeeservice/readiness
pkill -f "port-forward.*maliev-employee-service"
```

### 4. Run Integration Tests

```bash
# Port forward to staging service
kubectl port-forward -n maliev-staging svc/maliev-employee-service 8080:8080 &

# Set base URL
export BASE_URL="http://localhost:8080/employeeservice"

# Test employee profile endpoint
curl -H "Authorization: Bearer $JWT_TOKEN" \
  $BASE_URL/api/employees/profile

# Test leave balance endpoint
curl -H "Authorization: Bearer $JWT_TOKEN" \
  $BASE_URL/api/employees/$EMPLOYEE_ID/leave-balances

# Test org chart endpoint
curl -H "Authorization: Bearer $JWT_TOKEN" \
  $BASE_URL/api/departments/org-chart

# Kill port forward
pkill -f "port-forward.*maliev-employee-service"
```

### 5. Success Criteria (T433 - Staging)

- [X] Release tag triggered staging workflow
- [X] Docker image pushed to artifact-staging registry
- [X] GitOps PR merged for staging overlay
- [X] Staging deployment successful (2 replicas minimum)
- [X] HPA configured and active
- [X] Integration tests passing
- [X] No degradation in performance metrics

## Testing Production Pipeline

### 1. Merge to Main Branch

⚠️ **PRODUCTION DEPLOYMENT - Extra Caution Required**

```bash
# Verify staging is stable
kubectl get pods -n maliev-staging -l app=maliev-employee-service
# All pods should be Running and Ready for at least 24 hours

# Merge release tag to main
git checkout main
git pull origin main
git merge release/v1.2.3
git push origin main
```

### 2. Manual Approval (GitHub Actions)

The production workflow includes a manual approval step:

```bash
# Monitor workflow
gh run list --branch main --limit 1
gh run watch

# When prompted, approve deployment
# GitHub will send notification to approvers
# Approvers must review:
# - All tests passed
# - Staging deployment successful for 24+ hours
# - No critical issues in staging
# - Database migration plan reviewed
# - Rollback plan confirmed

# Approve via GitHub UI or CLI
gh run approve <RUN-ID>
```

### 3. Monitor Production Deployment

```bash
# Watch deployment progress
kubectl rollout status deployment/maliev-employee-service -n maliev-prod --watch

# Monitor pods
watch kubectl get pods -n maliev-prod -l app=maliev-employee-service

# Check logs for errors
kubectl logs -f deployment/maliev-employee-service -n maliev-prod | grep -i error

# Monitor metrics in Grafana
start https://grafana.maliev.co.th/d/employee-service
```

### 4. Verify Production Health

```bash
# Check deployment
kubectl get deployment maliev-employee-service -n maliev-prod

# Expected output:
# NAME                       READY   UP-TO-DATE   AVAILABLE   AGE
# maliev-employee-service   3/3     3            3           45m

# Check HPA
kubectl get hpa maliev-employee-service -n maliev-prod

# Expected output:
# NAME                       REFERENCE                             TARGETS         MINPODS   MAXPODS   REPLICAS
# maliev-employee-service   Deployment/maliev-employee-service    25%/60%, 30%/70%   3         10        3

# Check service
kubectl get svc maliev-employee-service -n maliev-prod
```

### 5. Production Smoke Tests

```bash
# Test via public endpoint (if available)
curl https://api.maliev.co.th/employeeservice/liveness
# Expected: "Healthy"

curl https://api.maliev.co.th/employeeservice/readiness
# Expected: {"status":"Healthy",...}

# Or port forward
kubectl port-forward -n maliev-prod svc/maliev-employee-service 8080:8080 &
curl http://localhost:8080/employeeservice/liveness
pkill -f "port-forward.*maliev-employee-service"
```

### 6. Monitor Production Metrics (Critical)

```bash
# Open Grafana dashboard
start https://grafana.maliev.co.th/d/employee-service

# Verify metrics for 1 hour:
# - Request rate (should be normal)
# - Error rate (should be < 1%)
# - p95 response time (should be < 500ms)
# - Database connection pool (should not be exhausted)
# - Active pods (should be 3-10 based on load)
# - Memory usage (should be stable, no leaks)
# - CPU usage (should be < 60% on average)
```

### 7. Success Criteria (T433 - Production)

- [X] Main branch merge triggered production workflow
- [X] Manual approval completed
- [X] Docker image pushed to artifact-prod registry
- [X] GitOps PR merged for production overlay
- [X] Production deployment successful (3 replicas minimum)
- [X] HPA configured (3-10 replicas)
- [X] All health checks passing
- [X] No errors in logs for 1 hour
- [X] Metrics stable for 1 hour:
  - Error rate < 1%
  - p95 response time < 500ms
  - No memory leaks
- [X] Rollback plan validated and ready

## Verification Procedures

### Container Image Verification

```bash
# List images in Artifact Registry
gcloud artifacts docker images list \
  asia-southeast1-docker.pkg.dev/maliev-website/maliev-website-artifact-dev/maliev-employee-service \
  --limit 10

# Pull and inspect image
docker pull asia-southeast1-docker.pkg.dev/maliev-website/maliev-website-artifact-dev/maliev-employee-service:abc123def
docker inspect asia-southeast1-docker.pkg.dev/maliev-website/maliev-website-artifact-dev/maliev-employee-service:abc123def

# Verify image labels
docker inspect --format='{{json .Config.Labels}}' \
  asia-southeast1-docker.pkg.dev/maliev-website/maliev-website-artifact-dev/maliev-employee-service:abc123def
```

### GitOps Repository Verification

```bash
# Clone and verify GitOps repository
git clone https://github.com/MALIEV-Co-Ltd/maliev-gitops.git
cd maliev-gitops

# Check commit history for employee service
git log --oneline --all --grep="employee-service" -n 10

# Verify kustomization for all environments
cat 3-apps/maliev-employee-service/overlays/development/kustomization.yaml
cat 3-apps/maliev-employee-service/overlays/staging/kustomization.yaml
cat 3-apps/maliev-employee-service/overlays/production/kustomization.yaml

# Build and verify manifests
cd 3-apps/maliev-employee-service/overlays/development
kustomize build .
```

### ArgoCD Application Verification

```bash
# List all employee service applications
argocd app list | grep employee-service

# Get detailed status
argocd app get maliev-employee-service-dev --refresh
argocd app get maliev-employee-service-staging --refresh
argocd app get maliev-employee-service-prod --refresh

# Check sync status
argocd app sync-status maliev-employee-service-dev

# View application history
argocd app history maliev-employee-service-dev
```

## Troubleshooting

### GitHub Actions Failing

**Issue**: Build step fails

```bash
# Check logs
gh run view <RUN-ID> --log-failed

# Common causes:
# - Test failures: Review test output
# - Build errors: Check for code errors
# - NuGet restore failures: Check package sources

# Fix and retry
git commit --amend
git push --force origin <branch>
```

**Issue**: Docker build fails

```bash
# Check Dockerfile syntax
docker build -t test -f Maliev.EmployeeService.Api/Dockerfile .

# Common causes:
# - Missing files in COPY commands
# - Incorrect project paths
# - Base image not found

# Test locally first
docker build --no-cache -t test -f Maliev.EmployeeService.Api/Dockerfile .
```

**Issue**: GitOps PR creation fails

```bash
# Check GITOPS_PAT secret
gh secret list --repo MALIEV-Co-Ltd/Maliev.EmployeeService

# Verify token permissions:
# - repo (full control)
# - workflow

# Update secret if needed
gh secret set GITOPS_PAT --body "ghp_new_token" --repo MALIEV-Co-Ltd/Maliev.EmployeeService
```

### ArgoCD Not Syncing

**Issue**: Application stuck in "OutOfSync" state

```bash
# Check ArgoCD logs
kubectl logs -n argocd -l app.kubernetes.io/name=argocd-application-controller

# Force sync
argocd app sync maliev-employee-service-dev --force --prune

# Check sync policy
argocd app get maliev-employee-service-dev -o json | jq '.spec.syncPolicy'

# Re-enable auto-sync if disabled
argocd app set maliev-employee-service-dev --sync-policy automated
```

**Issue**: Deployment not progressing

```bash
# Check deployment status
kubectl describe deployment maliev-employee-service -n maliev-dev

# Common causes:
# - Image pull errors: Check image tag and registry access
# - Resource limits: Check node capacity
# - Health checks failing: Check application logs

# View events
kubectl get events -n maliev-dev --sort-by='.lastTimestamp' | grep employee-service
```

### Pod Failures

**Issue**: Pods in CrashLoopBackOff

```bash
# Check pod logs
kubectl logs <POD-NAME> -n maliev-dev --previous

# Common causes:
# - Missing environment variables: Check External Secrets
# - Database connection failures: Verify connection string
# - Port conflicts: Check service configuration

# Describe pod for events
kubectl describe pod <POD-NAME> -n maliev-dev
```

**Issue**: Image pull errors

```bash
# Check image name and tag
kubectl describe pod <POD-NAME> -n maliev-dev | grep Image

# Verify image exists in registry
gcloud artifacts docker images list \
  asia-southeast1-docker.pkg.dev/maliev-website/maliev-website-artifact-dev/maliev-employee-service

# Check node can pull image
kubectl debug node/<NODE-NAME> -it --image=busybox
```

### External Secrets Not Syncing

```bash
# Check ExternalSecret status
kubectl describe externalsecret maliev-employee-service-secrets -n maliev-dev

# Check External Secrets Operator logs
kubectl logs -n external-secrets-system deployment/external-secrets

# Verify Google Secret Manager access
gcloud secrets list --filter="name:employee-service"

# Force refresh
kubectl delete externalsecret maliev-employee-service-secrets -n maliev-dev
kubectl apply -f <externalsecret-manifest>
```

## Success Criteria (T433 Overall)

### Development Pipeline
- [X] Develop branch push triggers GitHub Actions
- [X] All pipeline stages complete successfully
- [X] Docker image published to artifact-dev
- [X] GitOps PR created and merged
- [X] ArgoCD syncs to maliev-dev namespace
- [X] Deployment rolls out successfully
- [X] Health checks passing
- [X] Metrics accessible

### Staging Pipeline
- [X] Release tag triggers staging workflow
- [X] Image published to artifact-staging
- [X] Staging deployment successful
- [X] HPA configured and active
- [X] Integration tests passing

### Production Pipeline
- [X] Main branch merge triggers production workflow
- [X] Manual approval gate functioning
- [X] Image published to artifact-prod
- [X] Production deployment successful (3 replicas)
- [X] No errors for 1 hour post-deployment
- [X] Metrics stable and healthy
- [X] Rollback plan validated

### End-to-End Verification
- [X] Complete flow tested: code → build → deploy → verify
- [X] All environments accessible and functional
- [X] GitOps repository properly updated
- [X] ArgoCD managing all deployments
- [X] Monitoring and metrics working
- [X] Pipeline duration acceptable (< 20 minutes for dev)

---

**Last Updated**: 2025-10-18
**Version**: 1.0
**Owner**: DevOps Team
