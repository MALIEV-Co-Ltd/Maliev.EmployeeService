# Deployment Runbook - Maliev Employee Service

This runbook provides step-by-step procedures for deploying, rollback, and troubleshooting the Maliev Employee Service.

## Table of Contents

- [Pre-Deployment Checklist](#pre-deployment-checklist)
- [Deployment Procedures](#deployment-procedures)
- [Rollback Procedures](#rollback-procedures)
- [Post-Deployment Verification](#post-deployment-verification)
- [Troubleshooting Guide](#troubleshooting-guide)
- [Emergency Contacts](#emergency-contacts)

## Pre-Deployment Checklist

### Development Environment

- [ ] All tests passing (`dotnet test --verbosity normal`)
- [ ] Code review completed and approved
- [ ] No merge conflicts with target branch
- [ ] Database migrations reviewed and tested locally
- [ ] Configuration changes documented
- [ ] Security scan completed (OWASP ZAP)
- [ ] Performance tests passed (K6 load tests)
- [ ] Breaking changes documented

### Staging Environment

- [ ] Smoke tests passed
- [ ] Integration tests passed
- [ ] Database migration dry-run successful
- [ ] Load testing completed with acceptable results
- [ ] Monitoring dashboards reviewed
- [ ] Rollback plan documented
- [ ] Stakeholders notified of deployment window

### Production Environment

- [ ] Change request approved
- [ ] Deployment window scheduled
- [ ] Database backup completed and verified
- [ ] Rollback scripts tested in staging
- [ ] On-call engineer identified
- [ ] Communication plan established
- [ ] Health check URLs documented

## Deployment Procedures

### Automatic Deployment (GitOps with ArgoCD)

#### 1. Develop Branch → Development Environment

```bash
# 1. Merge feature branch to develop
git checkout develop
git pull origin develop
git merge feature/your-feature-branch
git push origin develop

# 2. CI/CD automatically triggers:
#    - Build and test
#    - Docker image build and push
#    - Create PR to maliev-gitops repository

# 3. Monitor GitHub Actions
#    https://github.com/MALIEV-Co-Ltd/maliev-employee-service/actions

# 4. Review and merge GitOps PR
#    ArgoCD will automatically deploy to development

# 5. Verify deployment
kubectl get pods -n maliev-dev | grep employee-service
kubectl logs -f deployment/maliev-employee-service -n maliev-dev
```

#### 2. Release Candidate → Staging Environment

```bash
# 1. Create release tag
git checkout develop
git pull origin develop
git tag -a release/v1.2.3 -m "Release v1.2.3 - Feature description"
git push origin release/v1.2.3

# 2. CI/CD automatically triggers staging deployment

# 3. Monitor and verify
kubectl get pods -n maliev-staging | grep employee-service
kubectl logs -f deployment/maliev-employee-service -n maliev-staging

# 4. Run smoke tests
./scripts/smoke-tests.sh https://staging-api.maliev.co.th
```

#### 3. Main Branch → Production Environment

⚠️ **PRODUCTION DEPLOYMENT - Requires Extra Caution**

```bash
# 1. Merge to main (requires approval)
git checkout main
git pull origin main
git merge release/v1.2.3
git push origin main

# 2. CI/CD creates GitOps PR for production

# 3. Final review checklist:
#    - All tests passed
#    - Staging deployment successful
#    - Smoke tests passed
#    - Database migration plan reviewed
#    - Rollback plan confirmed

# 4. Merge GitOps PR to trigger production deployment

# 5. Monitor deployment closely
kubectl get pods -n maliev-prod -w | grep employee-service

# 6. Watch logs for errors
kubectl logs -f deployment/maliev-employee-service -n maliev-prod

# 7. Monitor metrics in Grafana
#    - Request rate
#    - Error rate
#    - Response times
#    - Database connections
```

### Manual Deployment (Emergency Only)

⚠️ **Only use in emergency situations when GitOps is unavailable**

```bash
# 1. Build and push Docker image
docker build -t asia-southeast1-docker.pkg.dev/maliev-website/maliev-website-artifact-prod/maliev-employee-service:emergency-v1.2.3 \
  -f Maliev.EmployeeService.Api/Dockerfile .
docker push asia-southeast1-docker.pkg.dev/maliev-website/maliev-website-artifact-prod/maliev-employee-service:emergency-v1.2.3

# 2. Update deployment manually
kubectl set image deployment/maliev-employee-service \
  employee-service=asia-southeast1-docker.pkg.dev/maliev-website/maliev-website-artifact-prod/maliev-employee-service:emergency-v1.2.3 \
  -n maliev-prod

# 3. Monitor rollout
kubectl rollout status deployment/maliev-employee-service -n maliev-prod

# 4. IMPORTANT: Update GitOps repository to match
#    (Otherwise ArgoCD will revert your changes)
```

## Database Migration Procedures

### Pre-Migration Steps

```bash
# 1. Backup production database
kubectl exec -n maliev-prod postgres-cluster-1 -- \
  pg_dump -U postgres employee_service_db > backup-$(date +%Y%m%d-%H%M%S).sql

# 2. Verify backup
ls -lh backup-*.sql

# 3. Test migration in staging first
kubectl port-forward -n maliev-staging svc/postgres-cluster-rw 5432:5432 &
export EmployeeServiceDbContext="Server=localhost;Port=5432;Database=employee_service_db;User Id=postgres;Password=STAGING_PASSWORD;"
dotnet ef database update --project Maliev.EmployeeService.Infrastructure
```

### Production Migration (Zero-Downtime Strategy)

```bash
# 1. Port forward to production PostgreSQL (read-write service)
kubectl port-forward -n maliev-prod postgres-cluster-1 5432:5432 &

# 2. Set connection string
export EmployeeServiceDbContext="Server=localhost;Port=5432;Database=employee_service_db;User Id=postgres;Password=PROD_PASSWORD;"

# 3. Review pending migrations
dotnet ef migrations list --project Maliev.EmployeeService.Infrastructure

# 4. Apply migration
dotnet ef database update --project Maliev.EmployeeService.Infrastructure

# 5. Verify migration success
psql $EmployeeServiceDbContext -c "\dt"
psql $EmployeeServiceDbContext -c "SELECT version FROM __EFMigrationsHistory ORDER BY migration_id DESC LIMIT 5;"

# 6. Kill port forward
pkill -f "port-forward.*postgres"
```

### Rollback Migration (If Needed)

```bash
# Roll back to specific migration
dotnet ef database update PreviousMigrationName --project Maliev.EmployeeService.Infrastructure

# Or restore from backup
kubectl exec -n maliev-prod postgres-cluster-1 -- \
  psql -U postgres -d employee_service_db < backup-20251018-120000.sql
```

## Rollback Procedures

### Application Rollback (GitOps)

```bash
# 1. Identify last known good version
kubectl rollout history deployment/maliev-employee-service -n maliev-prod

# 2. Rollback using kubectl
kubectl rollout undo deployment/maliev-employee-service -n maliev-prod

# 3. Or revert GitOps repository
cd maliev-gitops
git log --oneline -n 10 3-apps/maliev-employee-service/overlays/production/
git revert <commit-hash>
git push origin main

# 4. Verify rollback
kubectl get pods -n maliev-prod | grep employee-service
kubectl logs -f deployment/maliev-employee-service -n maliev-prod
```

### Database Rollback

```bash
# Option 1: Rollback migration
dotnet ef database update PreviousMigrationName --project Maliev.EmployeeService.Infrastructure

# Option 2: Restore from backup
kubectl exec -n maliev-prod postgres-cluster-1 -- \
  psql -U postgres -d employee_service_db < backup-20251018-120000.sql

# Option 3: Point-in-time recovery (if supported by PostgreSQL cluster)
# Consult PostgreSQL operator documentation
```

## Post-Deployment Verification

### Automated Health Checks

```bash
# Liveness check
curl https://api.maliev.co.th/employeeservice/liveness

# Readiness check
curl https://api.maliev.co.th/employeeservice/readiness

# Metrics endpoint
curl https://api.maliev.co.th/employeeservice/metrics
```

### Manual Smoke Tests

```bash
# 1. Test authentication
curl -X POST https://api.maliev.co.th/employeeservice/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@maliev.co.th","password":"TestPassword123"}'

# 2. Test employee profile retrieval
curl -H "Authorization: Bearer $TOKEN" \
  https://api.maliev.co.th/employeeservice/api/employees/profile

# 3. Test leave balance query
curl -H "Authorization: Bearer $TOKEN" \
  https://api.maliev.co.th/employeeservice/api/employees/$EMPLOYEE_ID/leave-balances

# 4. Test org chart
curl -H "Authorization: Bearer $TOKEN" \
  https://api.maliev.co.th/employeeservice/api/departments/org-chart
```

### Grafana Dashboard Verification

1. Open Grafana: https://grafana.maliev.co.th
2. Navigate to "Employee Service - API Metrics" dashboard
3. Verify metrics:
   - Request rate (should be normal)
   - Error rate (should be <1%)
   - p95 response time (should be <500ms)
   - Database connection pool (should not be exhausted)
   - Active pods (should match desired replicas)

### Database Integrity Checks

```bash
# Connect to database
kubectl port-forward -n maliev-prod postgres-cluster-1 5432:5432 &
psql "Server=localhost;Port=5432;Database=employee_service_db;User Id=postgres;Password=PROD_PASSWORD;"

# Run integrity checks
SELECT COUNT(*) FROM employees WHERE employment_status = 'Active';
SELECT COUNT(*) FROM leave_requests WHERE created_date >= CURRENT_DATE;
SELECT migration_id FROM __EFMigrationsHistory ORDER BY migration_id DESC LIMIT 1;
```

## Troubleshooting Guide

### Pod Not Starting

```bash
# Check pod status
kubectl get pods -n maliev-prod | grep employee-service

# Describe pod for events
kubectl describe pod <pod-name> -n maliev-prod

# Check logs
kubectl logs <pod-name> -n maliev-prod

# Common issues:
# - Image pull errors: Check image tag and repository access
# - CrashLoopBackOff: Check application logs and configuration
# - Secrets not found: Verify External Secrets Operator
```

### High Error Rate

```bash
# Check logs for exceptions
kubectl logs -f deployment/maliev-employee-service -n maliev-prod | grep "ERROR"

# Check Grafana for error patterns
# Look for specific endpoint failures

# Common causes:
# - Database connection issues
# - External service timeouts (RabbitMQ, Redis)
# - Configuration errors
# - Migration not applied
```

### Database Connection Issues

```bash
# Check PostgreSQL cluster status
kubectl get postgresql -n maliev-prod

# Check connection pool metrics in Grafana
# Look for "Database Connection Pool" panel

# Test database connectivity
kubectl port-forward -n maliev-prod postgres-cluster-1 5432:5432
psql "Server=localhost;Port=5432;Database=employee_service_db;User Id=postgres;Password=PROD_PASSWORD;"

# Common fixes:
# - Increase connection pool size
# - Check PostgreSQL resource limits
# - Verify network policies
```

### Performance Degradation

```bash
# Check resource utilization
kubectl top pods -n maliev-prod | grep employee-service

# Check for memory leaks
kubectl logs deployment/maliev-employee-service -n maliev-prod | grep "OutOfMemory"

# Review slow queries in PostgreSQL
kubectl exec -n maliev-prod postgres-cluster-1 -- \
  psql -U postgres -d employee_service_db -c \
  "SELECT query, mean_exec_time FROM pg_stat_statements ORDER BY mean_exec_time DESC LIMIT 10;"

# Actions:
# - Scale horizontally (increase replicas)
# - Scale vertically (increase CPU/memory limits)
# - Optimize database queries
# - Review cache hit rates
```

### External Dependencies Down

```bash
# Check RabbitMQ health
kubectl get pods -n maliev-prod | grep rabbitmq

# Check Redis health
kubectl get pods -n maliev-prod | grep redis

# Check Google Cloud Storage connectivity
# Review logs for "Google.Cloud.Storage" errors

# Circuit breaker status
# Check metrics for open circuits
kubectl logs deployment/maliev-employee-service -n maliev-prod | grep "CircuitBreaker"

# Actions:
# - Verify external service health
# - Check network policies
# - Review Polly resilience policies
```

## Emergency Contacts

### On-Call Rotation

- **Primary On-Call**: Check PagerDuty schedule
- **Secondary On-Call**: Check PagerDuty schedule
- **Engineering Manager**: [Name] - [Contact]
- **DevOps Lead**: [Name] - [Contact]

### Escalation Path

1. **Level 1**: On-call engineer (response time: 15 minutes)
2. **Level 2**: Engineering manager (response time: 30 minutes)
3. **Level 3**: CTO (response time: 1 hour)

### Communication Channels

- **Slack**: #employee-service-alerts
- **PagerDuty**: https://maliev.pagerduty.com
- **Status Page**: https://status.maliev.co.th

## Appendix

### Useful Commands Reference

```bash
# View deployment status
kubectl get deployments -n maliev-prod

# View pods
kubectl get pods -n maliev-prod -l app=maliev-employee-service

# View logs
kubectl logs -f deployment/maliev-employee-service -n maliev-prod --tail=100

# Execute command in pod
kubectl exec -it <pod-name> -n maliev-prod -- /bin/bash

# Port forward for debugging
kubectl port-forward deployment/maliev-employee-service 8080:8080 -n maliev-prod

# Check resource usage
kubectl top pods -n maliev-prod

# View events
kubectl get events -n maliev-prod --sort-by='.lastTimestamp'

# Describe resource
kubectl describe deployment maliev-employee-service -n maliev-prod
```

### Configuration Files Location

- **GitOps Repository**: https://github.com/MALIEV-Co-Ltd/maliev-gitops
- **Base Manifests**: `3-apps/maliev-employee-service/base/`
- **Environment Overlays**: `3-apps/maliev-employee-service/overlays/{development|staging|production}/`
- **Secrets**: Google Secret Manager (never in Git)

### Monitoring Dashboards

- **Grafana**: https://grafana.maliev.co.th/d/employee-service
- **Prometheus**: https://prometheus.maliev.co.th
- **ArgoCD**: https://argocd.maliev.co.th
- **Kubernetes Dashboard**: https://k8s-dashboard.maliev.co.th

---

**Last Updated**: 2025-10-18
**Version**: 1.0
**Owner**: DevOps Team
