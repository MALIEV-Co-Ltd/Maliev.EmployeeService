# Deployment Guide - Employee Service

This guide provides comprehensive instructions for deploying the Employee Service to development, staging, and production environments using GitOps and Kubernetes.

## Table of Contents

1. [Deployment Architecture](#deployment-architecture)
2. [Prerequisites](#prerequisites)
3. [CI/CD Pipeline](#cicd-pipeline)
4. [GitOps Workflow](#gitops-workflow)
5. [Environment Configuration](#environment-configuration)
6. [Database Migration Strategy](#database-migration-strategy)
7. [Deployment Procedure](#deployment-procedure)
8. [Rollback Procedure](#rollback-procedure)
9. [Health Checks & Monitoring](#health-checks--monitoring)
10. [Troubleshooting](#troubleshooting)

## Deployment Architecture

### Overview

```
┌──────────────┐     ┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│              │     │              │     │              │     │              │
│    GitHub    │────>│ GitHub       │────>│   GCP        │────>│  ArgoCD      │
│  Repository  │     │ Actions      │     │ Artifact     │     │  (GitOps)    │
│              │     │  (CI/CD)     │     │ Registry     │     │              │
└──────────────┘     └──────────────┘     └──────────────┘     └──────┬───────┘
                                                                        │
                                                                        │ sync
                                                                        ▼
                                                              ┌──────────────────┐
                                                              │                  │
                                                              │  GKE Cluster     │
                                                              │  (Kubernetes)    │
                                                              │                  │
                                                              └──────────────────┘
```

### Components

- **GitHub Repository**: Source code versioning
- **GitHub Actions**: CI/CD automation (build, test, push)
- **GCP Artifact Registry**: Docker image storage
- **maliev-gitops Repository**: Kubernetes manifests (GitOps source of truth)
- **ArgoCD**: Continuous deployment and sync
- **GKE (Google Kubernetes Engine)**: Container orchestration

### Environments

| Environment | Branch | Namespace | Replicas | Resources | Auto-Deploy |
|-------------|--------|-----------|----------|-----------|-------------|
| Development | `develop` | `maliev-dev` | 1 | 512Mi/0.5 CPU | Yes |
| Staging | `staging` | `maliev-staging` | 2 | 1Gi/1 CPU | Yes |
| Production | `main` | `maliev-prod` | 3-5 (HPA) | 2Gi/2 CPU | Manual approval |

## Prerequisites

### Tools Required

```bash
# kubectl (Kubernetes CLI)
curl -LO "https://dl.k8s.io/release/$(curl -L -s https://dl.k8s.io/release/stable.txt)/bin/windows/amd64/kubectl.exe"

# Kustomize (Kubernetes manifest customization)
curl -s "https://raw.githubusercontent.com/kubernetes-sigs/kustomize/master/hack/install_kustomize.sh" | bash

# gcloud CLI (Google Cloud SDK)
# Download from: https://cloud.google.com/sdk/docs/install

# .NET 9.0 SDK
# Download from: https://dotnet.microsoft.com/download/dotnet/9.0

# Docker Desktop (for local testing)
# Download from: https://www.docker.com/products/docker-desktop
```

### Access Requirements

1. **GitHub Access**:
   - Write access to `MALIEV-Co-Ltd/Maliev.EmployeeService` repository
   - Write access to `MALIEV-Co-Ltd/maliev-gitops` repository
   - Personal Access Token (PAT) for GitOps updates

2. **GCP Access**:
   - Service account with Artifact Registry access
   - GKE cluster access (viewer for dev/staging, admin for prod)
   - Secret Manager access

3. **Kubernetes Access**:
   - RBAC permissions for deployment namespace
   - ArgoCD UI access (optional)

### Authentication Setup

```bash
# Authenticate with GCP
gcloud auth login
gcloud config set project maliev-website

# Configure kubectl for GKE
gcloud container clusters get-credentials maliev-cluster --region=asia-southeast1

# Verify access
kubectl get pods -n maliev-dev
```

## CI/CD Pipeline

### Pipeline Overview

The service uses GitHub Actions with three separate workflows:

1. **ci-develop.yml** - Development deployments
2. **ci-staging.yml** - Staging deployments
3. **ci-main.yml** - Production deployments

### Pipeline Stages

```yaml
┌─────────────┐
│   Trigger   │  Push to branch (develop/staging/main)
└──────┬──────┘
       │
       ▼
┌─────────────┐
│  Checkout   │  Clone repository code
└──────┬──────┘
       │
       ▼
┌─────────────┐
│    Build    │  dotnet build (all projects)
└──────┬──────┘
       │
       ▼
┌─────────────┐
│    Test     │  dotnet test (unit + integration)
└──────┬──────┘
       │
       ▼
┌─────────────┐
│   Docker    │  Build and tag Docker image
│   Build     │  Tag: {branch}-{sha}-{timestamp}
└──────┬──────┘
       │
       ▼
┌─────────────┐
│   Docker    │  Push to GCP Artifact Registry
│   Push      │  asia-southeast1-docker.pkg.dev/...
└──────┬──────┘
       │
       ▼
┌─────────────┐
│  Kustomize  │  Update image tag in maliev-gitops
│   Update    │  Commit and push to GitOps repo
└──────┬──────┘
       │
       ▼
┌─────────────┐
│   ArgoCD    │  Auto-sync detects change
│    Sync     │  Deploys to Kubernetes cluster
└─────────────┘
```

### Workflow Configuration

**ci-develop.yml** example:

```yaml
name: CI - Develop

on:
  push:
    branches: [develop]

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout code
        uses: actions/checkout@v5

      - name: Setup .NET
        uses: actions/setup-dotnet@v5
        with:
          dotnet-version: '9.x'

      - name: Restore dependencies
        run: dotnet restore Maliev.EmployeeService.sln

      - name: Build
        run: dotnet build Maliev.EmployeeService.sln --no-restore --configuration Release

      - name: Test
        run: dotnet test Maliev.EmployeeService.sln --no-build --verbosity normal --configuration Release

      - name: Authenticate to Google Cloud
        uses: google-github-actions/auth@v3
        with:
          credentials_json: ${{ secrets.GCP_SA_KEY }}

      - name: Configure Docker for GCP
        run: gcloud auth configure-docker asia-southeast1-docker.pkg.dev

      - name: Build Docker image
        run: |
          docker build -t asia-southeast1-docker.pkg.dev/maliev-website/maliev-website-artifact-dev/employee-service:${{ github.sha }} \
            -f Maliev.EmployeeService.Api/Dockerfile .

      - name: Push Docker image
        run: docker push asia-southeast1-docker.pkg.dev/maliev-website/maliev-website-artifact-dev/employee-service:${{ github.sha }}

      - name: Checkout GitOps repository
        uses: actions/checkout@v5
        with:
          repository: 'MALIEV-Co-Ltd/maliev-gitops'
          token: ${{ secrets.GITOPS_PAT }}
          path: 'maliev-gitops'

      - name: Install Kustomize
        run: |
          curl -s "https://raw.githubusercontent.com/kubernetes-sigs/kustomize/master/hack/install_kustomize.sh" | bash
          sudo mv kustomize /usr/local/bin/

      - name: Update Kustomize image
        run: |
          cd maliev-gitops/3-apps/employee-service/overlays/development
          kustomize edit set image employee-service=asia-southeast1-docker.pkg.dev/maliev-website/maliev-website-artifact-dev/employee-service:${{ github.sha }}

      - name: Commit and push GitOps changes
        run: |
          cd maliev-gitops
          git config --global user.name 'github-actions[bot]'
          git config --global user.email 'github-actions[bot]@users.noreply.github.com'
          git add .
          git commit -m "Update employee-service image to ${{ github.sha }}"
          git pull --rebase origin main
          git push origin main
```

### GitHub Secrets

Required secrets in repository settings:

| Secret Name | Description | Example |
|-------------|-------------|---------|
| `GCP_SA_KEY` | GCP service account JSON key | `{...}` |
| `GITOPS_PAT` | Personal access token for maliev-gitops | `ghp_...` |

## GitOps Workflow

### Repository Structure

```
maliev-gitops/
├── 0-bootstrap/          # ArgoCD and core infrastructure
├── 1-infrastructure/     # Shared infrastructure (Postgres, Redis, RabbitMQ)
├── 2-platform/          # Platform services (monitoring, logging)
└── 3-apps/              # Application deployments
    └── employee-service/
        ├── base/
        │   ├── deployment.yaml
        │   ├── service.yaml
        │   ├── servicemonitor.yaml
        │   └── kustomization.yaml
        └── overlays/
            ├── development/
            │   ├── kustomization.yaml
            │   ├── configmap.yaml
            │   └── replicas.yaml
            ├── staging/
            │   ├── kustomization.yaml
            │   ├── configmap.yaml
            │   └── replicas.yaml
            └── production/
                ├── kustomization.yaml
                ├── configmap.yaml
                ├── replicas.yaml
                └── hpa.yaml
```

### Base Manifests

**base/deployment.yaml**:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: employee-service
  labels:
    app: employee-service
    version: v1
spec:
  replicas: 1  # Overridden by overlays
  selector:
    matchLabels:
      app: employee-service
  template:
    metadata:
      labels:
        app: employee-service
        version: v1
    spec:
      containers:
      - name: employee-service
        image: employee-service:latest  # Replaced by Kustomize
        imagePullPolicy: Always
        ports:
        - name: http
          containerPort: 8080
          protocol: TCP
        - name: metrics
          containerPort: 8080
          protocol: TCP
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        envFrom:
        - secretRef:
            name: employee-service-secrets
        - configMapRef:
            name: employee-service-config
        livenessProbe:
          httpGet:
            path: /employees/liveness
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
          timeoutSeconds: 5
          failureThreshold: 3
        readinessProbe:
          httpGet:
            path: /employees/readiness
            port: 8080
          initialDelaySeconds: 10
          periodSeconds: 5
          timeoutSeconds: 3
          failureThreshold: 3
        resources:
          requests:
            memory: "512Mi"
            cpu: "500m"
          limits:
            memory: "1Gi"
            cpu: "1000m"
        volumeMounts:
        - name: secrets
          mountPath: /mnt/secrets
          readOnly: true
      volumes:
      - name: secrets
        csi:
          driver: secrets-store.csi.k8s.io
          readOnly: true
          volumeAttributes:
            secretProviderClass: "employee-service-secrets"
```

**base/service.yaml**:

```yaml
apiVersion: v1
kind: Service
metadata:
  name: employee-service
  labels:
    app: employee-service
spec:
  type: ClusterIP
  ports:
  - name: http
    port: 8080
    targetPort: 8080
    protocol: TCP
  selector:
    app: employee-service
```

**base/servicemonitor.yaml**:

```yaml
apiVersion: monitoring.coreos.com/v1
kind: ServiceMonitor
metadata:
  name: employee-service
  labels:
    app: employee-service
spec:
  selector:
    matchLabels:
      app: employee-service
  endpoints:
  - port: metrics
    path: /employees/metrics
    interval: 30s
```

### Overlay Configurations

**overlays/development/kustomization.yaml**:

```yaml
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization

namespace: maliev-dev

resources:
- ../../base

images:
- name: employee-service
  newName: asia-southeast1-docker.pkg.dev/maliev-website/maliev-website-artifact-dev/employee-service
  newTag: latest  # Updated by CI/CD

configMapGenerator:
- name: employee-service-config
  literals:
  - REDIS_ENABLED=true
  - RABBITMQ_ENABLED=true
  - CORS_ALLOWED_ORIGINS=http://localhost:3000

replicas:
- name: employee-service
  count: 1

patches:
- path: replicas.yaml
```

**overlays/production/hpa.yaml** (Horizontal Pod Autoscaler):

```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: employee-service
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: employee-service
  minReplicas: 3
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
```

### ArgoCD Application

**argocd/employee-service-dev.yaml**:

```yaml
apiVersion: argoproj.io/v1alpha1
kind: Application
metadata:
  name: employee-service-dev
  namespace: argocd
spec:
  project: default
  source:
    repoURL: https://github.com/MALIEV-Co-Ltd/maliev-gitops.git
    targetRevision: main
    path: 3-apps/employee-service/overlays/development
  destination:
    server: https://kubernetes.default.svc
    namespace: maliev-dev
  syncPolicy:
    automated:
      prune: true
      selfHeal: true
      allowEmpty: false
    syncOptions:
    - CreateNamespace=true
    retry:
      limit: 5
      backoff:
        duration: 5s
        factor: 2
        maxDuration: 3m
```

## Environment Configuration

### Secret Management

Secrets are stored in **Google Secret Manager** and mounted via External Secrets Operator.

**SecretProviderClass** (managed in infrastructure):

```yaml
apiVersion: secrets-store.csi.x-k8s.io/v1
kind: SecretProviderClass
metadata:
  name: employee-service-secrets
  namespace: maliev-prod
spec:
  provider: gcp
  parameters:
    secrets: |
      - resourceName: "projects/maliev-website/secrets/employee-service-jwt-secret/versions/latest"
        path: "JWT_SECRET_KEY"
      - resourceName: "projects/maliev-website/secrets/employee-service-db-password/versions/latest"
        path: "DATABASE_PASSWORD"
      - resourceName: "projects/maliev-website/secrets/rabbitmq-password/versions/latest"
        path: "RABBITMQ_PASSWORD"
      - resourceName: "projects/maliev-website/secrets/redis-password/versions/latest"
        path: "REDIS_PASSWORD"
```

### ConfigMap

Non-sensitive configuration via ConfigMap:

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: employee-service-config
  namespace: maliev-prod
data:
  JWT_ISSUER: "https://maliev.co.th"
  JWT_AUDIENCE: "employee-service"
  REDIS_ENABLED: "true"
  RABBITMQ_ENABLED: "true"
  RABBITMQ_HOST: "rabbitmq-cluster.maliev-prod.svc.cluster.local"
  RABBITMQ_PORT: "5672"
  RABBITMQ_USERNAME: "employee-service"
  RABBITMQ_VHOST: "/"
  REDIS_CONNECTION_STRING: "redis-cluster.maliev-prod.svc.cluster.local:6379"
  UPLOAD_SERVICE_URL: "http://upload-service.maliev-prod.svc.cluster.local:8082"
  CAREER_SERVICE_URL: "http://career-service.maliev-prod.svc.cluster.local:8081"
  CORS_ALLOWED_ORIGINS: "https://maliev.co.th,https://app.maliev.co.th"
```

### Environment Variables

Connection string built from secrets and config:

```bash
DATABASE_URL="Server=postgres-cluster-rw.maliev-prod.svc.cluster.local;Port=5432;Database=employee_db;User Id=employee_service;Password=${DATABASE_PASSWORD}"
```

## Database Migration Strategy

### Migration Philosophy

- **Zero-downtime migrations**: All migrations must support rolling deployments
- **Backward compatible**: New code must work with old schema during rollout
- **Forward compatible**: Old code must tolerate new schema
- **Reversible**: All migrations must have a rollback plan

### Migration Workflow

#### 1. Development

```bash
# Create migration locally
cd Maliev.EmployeeService
dotnet ef migrations add AddNewFeature \
  --project Maliev.EmployeeService.Infrastructure \
  --startup-project Maliev.EmployeeService.Api

# Review generated migration code
code Maliev.EmployeeService.Infrastructure/Migrations/

# Test migration locally
dotnet ef database update \
  --project Maliev.EmployeeService.Infrastructure \
  --startup-project Maliev.EmployeeService.Api
```

#### 2. Commit and PR

```bash
git add .
git commit -m "feat: Add new feature with database migration"
git push origin feature/new-feature

# Create pull request
# Ensure migration is reviewed by team lead
```

#### 3. Production Migration (Manual Process)

**CRITICAL**: Database migrations are NOT automated in production for safety.

```bash
# Step 1: Port forward to production database
kubectl port-forward -n maliev-prod postgres-cluster-1 5432:5432 &

# Step 2: Set production connection string
export DATABASE_URL="Server=localhost;Port=5432;Database=employee_db;User Id=postgres;Password=ACTUAL_PASSWORD;"

# Step 3: DRY RUN - Generate SQL script
dotnet ef migrations script \
  --project Maliev.EmployeeService.Infrastructure \
  --startup-project Maliev.EmployeeService.Api \
  --output migration.sql

# Step 4: Review SQL script
cat migration.sql

# Step 5: Backup database
kubectl exec -n maliev-prod postgres-cluster-1 -- \
  pg_dump -U postgres employee_db > backup_$(date +%Y%m%d_%H%M%S).sql

# Step 6: Apply migration
dotnet ef database update \
  --project Maliev.EmployeeService.Infrastructure \
  --startup-project Maliev.EmployeeService.Api

# Step 7: Verify migration
kubectl exec -n maliev-prod postgres-cluster-1 -- \
  psql -U postgres employee_db -c "\dt"

# Step 8: Kill port forward
kill %1
```

### Migration Best Practices

#### ✅ Safe Migration Patterns

1. **Add nullable columns**:
```csharp
migrationBuilder.AddColumn<string>(
    name: "NewColumn",
    table: "Employees",
    nullable: true);  // Start nullable
```

2. **Add tables** (always safe):
```csharp
migrationBuilder.CreateTable(
    name: "NewTable",
    columns: table => new { ... });
```

3. **Add indexes** (use CONCURRENTLY in raw SQL):
```csharp
migrationBuilder.Sql(
    "CREATE INDEX CONCURRENTLY IX_Employee_NewColumn ON Employees(NewColumn)");
```

#### ❌ Unsafe Migration Patterns

1. **Drop columns** (causes errors in running pods):
```csharp
// DON'T DO THIS without two-phase deployment
migrationBuilder.DropColumn(
    name: "OldColumn",
    table: "Employees");
```

2. **Rename columns** (breaks running code):
```csharp
// DON'T DO THIS without aliasing or two-phase deployment
migrationBuilder.RenameColumn(
    name: "OldName",
    table: "Employees",
    newName: "NewName");
```

3. **Change column types** (can cause data loss):
```csharp
// CAREFUL - may truncate data
migrationBuilder.AlterColumn<int>(
    name: "Age",
    table: "Employees",
    nullable: false,
    oldClrType: typeof(string));
```

### Two-Phase Migration Strategy

For breaking changes, use a two-phase approach:

**Phase 1** (Deploy first):
```csharp
// Migration: Add new column, keep old column
migrationBuilder.AddColumn<string>(
    name: "NewEmailAddress",
    table: "Employees",
    nullable: true);

// Code: Write to both columns
employee.Email = newEmail;              // Old column
employee.NewEmailAddress = newEmail;    // New column
```

**Phase 2** (Deploy after Phase 1 is fully rolled out):
```csharp
// Migration: Drop old column
migrationBuilder.DropColumn(
    name: "Email",
    table: "Employees");

// Code: Use new column only
employee.NewEmailAddress = newEmail;
```

### Rollback Strategy

If a migration causes issues:

```bash
# Option 1: Rollback migration
dotnet ef database update PreviousMigrationName \
  --project Maliev.EmployeeService.Infrastructure

# Option 2: Restore from backup
kubectl exec -n maliev-prod postgres-cluster-1 -- \
  psql -U postgres employee_db < backup_20251018_120000.sql

# Option 3: Point-in-time recovery (if WAL enabled)
# Contact DBA team
```

## Deployment Procedure

### Pre-Deployment Checklist

- [ ] All tests passing in CI
- [ ] Code review approved
- [ ] Migration script reviewed (if applicable)
- [ ] Database backup completed (production only)
- [ ] Runbook updated with any new procedures
- [ ] Monitoring dashboards checked for baseline
- [ ] On-call engineer notified (production only)

### Development Deployment

Fully automated via GitHub Actions:

```bash
# 1. Merge PR to develop branch
git checkout develop
git pull origin develop
git merge --no-ff feature/new-feature
git push origin develop

# 2. GitHub Actions automatically:
#    - Builds and tests
#    - Creates Docker image
#    - Pushes to Artifact Registry
#    - Updates maliev-gitops
#
# 3. ArgoCD automatically:
#    - Detects change in maliev-gitops
#    - Syncs to maliev-dev namespace
#    - Performs rolling update

# 4. Verify deployment
kubectl get pods -n maliev-dev
kubectl logs -f deployment/employee-service -n maliev-dev

# 5. Test deployment
curl https://api-dev.maliev.co.th/employees/liveness
```

### Staging Deployment

Similar to development, triggered by push to `staging` branch:

```bash
git checkout staging
git merge develop
git push origin staging

# Verify
kubectl get pods -n maliev-staging
```

### Production Deployment

Production deployments require manual approval:

```bash
# 1. Create release branch
git checkout -b release/v1.2.0 main
git merge staging
git push origin release/v1.2.0

# 2. Create GitHub Release
# - Tag: v1.2.0
# - Title: "Release v1.2.0 - Employee Service"
# - Description: Changelog and migration notes

# 3. Merge to main (triggers CI/CD)
git checkout main
git merge release/v1.2.0
git push origin main

# 4. Apply database migration (MANUAL)
# See "Database Migration Strategy" section above

# 5. Monitor ArgoCD sync
kubectl get applications -n argocd
argocd app get employee-service-prod

# 6. Monitor deployment rollout
kubectl rollout status deployment/employee-service -n maliev-prod

# 7. Verify health
kubectl get pods -n maliev-prod
curl https://api.maliev.co.th/employees/liveness
curl https://api.maliev.co.th/employees/readiness

# 8. Monitor metrics
# Check Grafana dashboards for errors, latency spikes
```

### Deployment Verification

After deployment, verify:

```bash
# 1. Pods are running
kubectl get pods -n maliev-prod -l app=employee-service

# 2. Health checks passing
kubectl describe pod <pod-name> -n maliev-prod

# 3. Logs are clean
kubectl logs -f deployment/employee-service -n maliev-prod

# 4. Metrics endpoint
kubectl port-forward -n maliev-prod deployment/employee-service 8080:8080
curl http://localhost:8080/employees/metrics

# 5. Database connectivity
kubectl exec -n maliev-prod <pod-name> -- \
  curl http://localhost:8080/employees/readiness

# 6. Integration health checks
# Check Redis, RabbitMQ, Upload Service connectivity
```

## Rollback Procedure

### Application Rollback

#### Option 1: ArgoCD Rollback (Fastest)

```bash
# Via ArgoCD UI
# Navigate to employee-service-prod application
# Click "History and Rollback"
# Select previous successful sync
# Click "Rollback"

# Via CLI
argocd app rollback employee-service-prod <previous-revision>
```

#### Option 2: GitOps Rollback

```bash
# Revert the commit in maliev-gitops
cd maliev-gitops
git log --oneline  # Find the commit to revert
git revert <commit-hash>
git push origin main

# ArgoCD will auto-sync the revert
```

#### Option 3: Kubernetes Rollback

```bash
# Check rollout history
kubectl rollout history deployment/employee-service -n maliev-prod

# Rollback to previous revision
kubectl rollout undo deployment/employee-service -n maliev-prod

# Rollback to specific revision
kubectl rollout undo deployment/employee-service -n maliev-prod --to-revision=3

# Monitor rollback
kubectl rollout status deployment/employee-service -n maliev-prod
```

### Database Rollback

**WARNING**: Database rollbacks are complex and risky.

```bash
# Option 1: Rollback migration
dotnet ef database update PreviousMigrationName \
  --project Maliev.EmployeeService.Infrastructure

# Option 2: Restore from backup
kubectl exec -n maliev-prod postgres-cluster-1 -- \
  psql -U postgres employee_db < backup_20251018_120000.sql

# Option 3: Point-in-time recovery
# Contact DBA team immediately
```

### Emergency Rollback Runbook

For critical production issues:

1. **Stop the bleeding**:
```bash
# Scale down to 0 replicas (extreme measure)
kubectl scale deployment employee-service --replicas=0 -n maliev-prod
```

2. **Identify the issue**:
```bash
# Check logs
kubectl logs --tail=100 deployment/employee-service -n maliev-prod

# Check metrics
# Navigate to Grafana dashboard
```

3. **Execute rollback**:
```bash
# Use fastest method (ArgoCD UI or kubectl rollout undo)
kubectl rollout undo deployment/employee-service -n maliev-prod
```

4. **Verify rollback**:
```bash
kubectl rollout status deployment/employee-service -n maliev-prod
curl https://api.maliev.co.th/employees/liveness
```

5. **Post-mortem**:
   - Document the incident
   - Create GitHub issue
   - Update runbook

## Health Checks & Monitoring

### Liveness Probe

Checks if the application is alive:

```bash
GET /employees/liveness

# Expected response:
{
  "status": "Healthy",
  "service": "Employee Service"
}
```

If liveness fails 3 times, Kubernetes **restarts the pod**.

### Readiness Probe

Checks if the application is ready to serve traffic:

```bash
GET /employees/readiness

# Expected response (healthy):
{
  "status": "Healthy",
  "checks": {
    "database": "Healthy",
    "rabbitmq": "Healthy",
    "redis": "Healthy",
    "gcs": "Healthy"
  },
  "totalDuration": "00:00:00.1234567"
}
```

If readiness fails, Kubernetes **removes pod from service endpoints** (stops sending traffic).

### Metrics Monitoring

#### Prometheus Metrics

Available at `/employees/metrics`:

```bash
# Technical metrics
http_requests_total{method="GET",endpoint="/v1/profile",status="200"} 1234
http_request_duration_seconds{endpoint="/v1/profile",quantile="0.95"} 0.045
database_query_duration_seconds{quantile="0.95"} 0.012
rabbitmq_publish_total{event="EmployeeCreated",status="success"} 567
circuit_breaker_state{name="rabbitmq",state="closed"} 1

# Business metrics
employees_total 10500
employees_by_status{status="Active"} 9800
leave_requests_pending 45
leave_utilization_rate 0.62
average_tenure_days 730
```

#### Grafana Dashboards

1. **API Metrics Dashboard**:
   - Request rate (req/sec)
   - Error rate (%)
   - P50, P95, P99 latency
   - Status code distribution

2. **Database Performance Dashboard**:
   - Query duration (P95, P99)
   - Connection pool usage
   - Slow query count

3. **Business Metrics Dashboard**:
   - Employee count trends
   - Leave request trends
   - Onboarding progress

### Alerts

Configured in Prometheus AlertManager:

| Alert | Threshold | Severity | Action |
|-------|-----------|----------|--------|
| High Error Rate | >5% for 5min | Critical | Page on-call |
| High Latency | P95 >1s for 5min | Warning | Investigate |
| Pod CrashLoop | Any pod | Critical | Page on-call |
| Database Down | Readiness fails | Critical | Page on-call & DBA |
| Low Leave Balance | <3 days | Info | Notify employee |

## Troubleshooting

### Common Issues

#### Issue 1: Pods in CrashLoopBackOff

**Symptoms**:
```bash
kubectl get pods -n maliev-prod
# NAME                              READY   STATUS             RESTARTS
# employee-service-xxx-yyy          0/1     CrashLoopBackOff   5
```

**Diagnosis**:
```bash
# Check logs
kubectl logs employee-service-xxx-yyy -n maliev-prod

# Check previous logs
kubectl logs employee-service-xxx-yyy -n maliev-prod --previous

# Describe pod
kubectl describe pod employee-service-xxx-yyy -n maliev-prod
```

**Common Causes**:
- Database connection failure
- Missing secrets
- Startup exception
- OOM kill

**Resolution**:
```bash
# Fix secret
kubectl get secret employee-service-secrets -n maliev-prod -o yaml

# Check database connectivity
kubectl exec -n maliev-prod <pod-name> -- nc -zv postgres-cluster-rw 5432

# Check resource limits
kubectl describe pod <pod-name> -n maliev-prod | grep -A 5 "Limits"
```

#### Issue 2: Readiness Probe Failing

**Symptoms**:
```bash
kubectl get pods -n maliev-prod
# NAME                              READY   STATUS    RESTARTS
# employee-service-xxx-yyy          0/1     Running   0
```

**Diagnosis**:
```bash
# Check readiness endpoint
kubectl port-forward -n maliev-prod employee-service-xxx-yyy 8080:8080
curl http://localhost:8080/employees/readiness

# Check integration health
kubectl logs employee-service-xxx-yyy -n maliev-prod | grep "Health"
```

**Common Causes**:
- Redis connection failure
- RabbitMQ connection failure
- Database migration pending

**Resolution**:
```bash
# Check Redis
kubectl get pods -n maliev-prod -l app=redis-cluster

# Check RabbitMQ
kubectl get pods -n maliev-prod -l app=rabbitmq-cluster

# Apply pending migrations (see Database Migration section)
```

#### Issue 3: High CPU/Memory Usage

**Symptoms**:
```bash
kubectl top pods -n maliev-prod
# NAME                              CPU    MEMORY
# employee-service-xxx-yyy          1800m  1900Mi
```

**Diagnosis**:
```bash
# Check metrics
kubectl port-forward -n maliev-prod deployment/employee-service 8080:8080
curl http://localhost:8080/employees/metrics | grep process

# Check for memory leak
kubectl exec -n maliev-prod <pod-name> -- dotnet-counters monitor --process-id 1
```

**Resolution**:
```bash
# Increase resource limits
# Edit deployment in maliev-gitops

# Scale horizontally
kubectl scale deployment employee-service --replicas=5 -n maliev-prod
```

#### Issue 4: Database Migration Failed

**Symptoms**:
- Migration script errors
- Schema version mismatch

**Resolution**:
```bash
# Check current migration version
kubectl exec -n maliev-prod postgres-cluster-1 -- \
  psql -U postgres employee_db -c "SELECT * FROM __EFMigrationsHistory ORDER BY MigrationId DESC LIMIT 5;"

# Rollback to previous version
dotnet ef database update PreviousMigrationName

# Re-apply migration
dotnet ef database update
```

### Debug Commands

```bash
# Get pod details
kubectl get pods -n maliev-prod -l app=employee-service -o wide

# Describe pod
kubectl describe pod <pod-name> -n maliev-prod

# View logs (real-time)
kubectl logs -f deployment/employee-service -n maliev-prod

# View logs (last hour)
kubectl logs deployment/employee-service -n maliev-prod --since=1h

# Exec into pod
kubectl exec -it <pod-name> -n maliev-prod -- /bin/bash

# Port forward for debugging
kubectl port-forward -n maliev-prod deployment/employee-service 8080:8080

# Check secrets
kubectl get secret employee-service-secrets -n maliev-prod -o jsonpath='{.data}' | jq

# Check configmap
kubectl get configmap employee-service-config -n maliev-prod -o yaml

# Check service endpoints
kubectl get endpoints employee-service -n maliev-prod

# Check ingress
kubectl get ingress -n maliev-prod

# View events
kubectl get events -n maliev-prod --sort-by='.lastTimestamp'
```

### Support Contacts

| Issue Type | Contact | SLA |
|------------|---------|-----|
| Application Bug | Dev Team | 4 hours |
| Infrastructure | Platform Team | 2 hours |
| Database | DBA Team | 1 hour (critical) |
| Security | Security Team | Immediate (critical) |
| ArgoCD/GitOps | DevOps Team | 2 hours |

## Appendix

### Deployment Checklist

#### Pre-Deployment
- [ ] Feature branch merged to develop/staging/main
- [ ] All tests passing
- [ ] Code reviewed and approved
- [ ] Migration script reviewed (if applicable)
- [ ] Backup completed (production)
- [ ] Change notification sent
- [ ] Runbook updated

#### Deployment
- [ ] GitHub Actions workflow completed
- [ ] Docker image pushed to registry
- [ ] GitOps repository updated
- [ ] ArgoCD sync successful
- [ ] Database migration applied (manual, production)
- [ ] Pods running and ready
- [ ] Health checks passing

#### Post-Deployment
- [ ] Smoke tests completed
- [ ] Metrics validated
- [ ] Logs reviewed
- [ ] Error rates within threshold
- [ ] Performance within SLA
- [ ] Documentation updated
- [ ] Team notified

### Glossary

- **ArgoCD**: Declarative GitOps continuous delivery tool for Kubernetes
- **Kustomize**: Kubernetes manifest customization tool
- **GKE**: Google Kubernetes Engine
- **HPA**: Horizontal Pod Autoscaler
- **GitOps**: Infrastructure/application management using Git as source of truth
- **Rollout**: Kubernetes deployment update process
- **Sync**: ArgoCD operation to align cluster state with Git repository

### References

- [Kubernetes Documentation](https://kubernetes.io/docs/)
- [ArgoCD Documentation](https://argo-cd.readthedocs.io/)
- [Kustomize Documentation](https://kustomize.io/)
- [Google Cloud Documentation](https://cloud.google.com/docs)
- [Entity Framework Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)

---

**Document Version**: 1.0
**Last Updated**: October 2025
**Maintainer**: DevOps Team
