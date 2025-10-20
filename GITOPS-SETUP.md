# GitOps Setup Guide - Maliev Employee Service

This document provides instructions for setting up GitOps manifests for the Maliev Employee Service in the `maliev-gitops` repository.

## Prerequisites

- Access to `MALIEV-Co-Ltd/maliev-gitops` repository
- Kubernetes cluster (GKE) configured
- ArgoCD installed and configured
- External Secrets Operator installed
- Google Secret Manager configured

## Directory Structure

The Employee Service manifests should be created in the `maliev-gitops` repository:

```
maliev-gitops/
└── 3-apps/
    └── maliev-employee-service/
        ├── base/
        │   ├── deployment.yaml
        │   ├── service.yaml
        │   ├── servicemonitor.yaml
        │   ├── externalsecret.yaml
        │   └── kustomization.yaml
        └── overlays/
            ├── development/
            │   ├── kustomization.yaml
            │   ├── deployment-patch.yaml
            │   └── hpa.yaml
            ├── staging/
            │   ├── kustomization.yaml
            │   ├── deployment-patch.yaml
            │   └── hpa.yaml
            └── production/
                ├── kustomization.yaml
                ├── deployment-patch.yaml
                └── hpa.yaml
```

## Base Manifests

### 1. deployment.yaml (T410)

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: maliev-employee-service
  labels:
    app: maliev-employee-service
    tier: backend
spec:
  replicas: 2
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxSurge: 1
      maxUnavailable: 0
  selector:
    matchLabels:
      app: maliev-employee-service
  template:
    metadata:
      labels:
        app: maliev-employee-service
        tier: backend
      annotations:
        prometheus.io/scrape: "true"
        prometheus.io/port: "8080"
        prometheus.io/path: "/employeeservice/metrics"
    spec:
      containers:
      - name: employee-service
        image: asia-southeast1-docker.pkg.dev/maliev-website/maliev-website-artifact/maliev-employee-service:latest
        ports:
        - containerPort: 8080
          name: http
        - containerPort: 8081
          name: metrics

        # Environment variables from External Secrets
        envFrom:
        - secretRef:
            name: maliev-employee-service-secrets

        # Resource limits
        resources:
          requests:
            cpu: 250m
            memory: 512Mi
          limits:
            cpu: 1000m
            memory: 1Gi

        # Liveness probe
        livenessProbe:
          httpGet:
            path: /employeeservice/liveness
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
          timeoutSeconds: 5
          failureThreshold: 3

        # Readiness probe
        readinessProbe:
          httpGet:
            path: /employeeservice/readiness
            port: 8080
          initialDelaySeconds: 15
          periodSeconds: 5
          timeoutSeconds: 3
          failureThreshold: 3

        # Security context
        securityContext:
          runAsNonRoot: true
          runAsUser: 1000
          allowPrivilegeEscalation: false
          readOnlyRootFilesystem: true
          capabilities:
            drop:
            - ALL

        # Volume mounts for secrets
        volumeMounts:
        - name: secrets
          mountPath: /mnt/secrets
          readOnly: true
        - name: tmp
          mountPath: /tmp

      volumes:
      - name: secrets
        secret:
          secretName: maliev-employee-service-secrets
      - name: tmp
        emptyDir: {}

      # Security settings
      securityContext:
        fsGroup: 1000
        runAsNonRoot: true
        seccompProfile:
          type: RuntimeDefault
```

### 2. service.yaml (T410)

```yaml
apiVersion: v1
kind: Service
metadata:
  name: maliev-employee-service
  labels:
    app: maliev-employee-service
    tier: backend
spec:
  type: ClusterIP
  ports:
  - port: 8080
    targetPort: 8080
    protocol: TCP
    name: http
  - port: 8081
    targetPort: 8081
    protocol: TCP
    name: metrics
  selector:
    app: maliev-employee-service
```

### 3. externalsecret.yaml (T412)

```yaml
apiVersion: external-secrets.io/v1beta1
kind: ExternalSecret
metadata:
  name: maliev-employee-service-secrets
spec:
  refreshInterval: 1h
  secretStoreRef:
    name: google-secret-manager
    kind: SecretStore
  target:
    name: maliev-employee-service-secrets
    creationPolicy: Owner
  data:
  # Database connection
  - secretKey: EmployeeServiceDbContext
    remoteRef:
      key: employee-service-db-connection

  # JWT settings
  - secretKey: JwtSettings__SecretKey
    remoteRef:
      key: jwt-secret-key
  - secretKey: JwtSettings__Issuer
    remoteRef:
      key: jwt-issuer
  - secretKey: JwtSettings__Audience
    remoteRef:
      key: jwt-audience

  # RabbitMQ
  - secretKey: RabbitMqSettings__ConnectionString
    remoteRef:
      key: rabbitmq-connection-string

  # Redis
  - secretKey: RedisSettings__ConnectionString
    remoteRef:
      key: redis-connection-string

  # Google Cloud Storage
  - secretKey: GoogleCloudStorage__BucketName
    remoteRef:
      key: gcs-bucket-name
  - secretKey: GoogleCloudStorage__ProjectId
    remoteRef:
      key: gcp-project-id

  # External services
  - secretKey: CareerServiceClient__BaseUrl
    remoteRef:
      key: career-service-url
  - secretKey: UploadServiceClient__BaseUrl
    remoteRef:
      key: upload-service-url

  # Encryption
  - secretKey: EncryptionSettings__Key
    remoteRef:
      key: data-encryption-key
```

### 4. servicemonitor.yaml (T413)

```yaml
apiVersion: monitoring.coreos.com/v1
kind: ServiceMonitor
metadata:
  name: maliev-employee-service
  labels:
    app: maliev-employee-service
    release: prometheus
spec:
  selector:
    matchLabels:
      app: maliev-employee-service
  endpoints:
  - port: http
    path: /employeeservice/metrics
    interval: 30s
    scrapeTimeout: 10s
    scheme: http
    honorLabels: true
    metricRelabelings:
    # Drop high-cardinality metrics
    - sourceLabels: [__name__]
      regex: 'go_.*'
      action: drop
    - sourceLabels: [__name__]
      regex: 'process_.*'
      action: drop
```

### 5. base/kustomization.yaml

```yaml
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization

namespace: maliev-dev

resources:
- deployment.yaml
- service.yaml
- servicemonitor.yaml
- externalsecret.yaml

commonLabels:
  app.kubernetes.io/name: employee-service
  app.kubernetes.io/part-of: maliev-platform
  app.kubernetes.io/managed-by: argocd

images:
- name: asia-southeast1-docker.pkg.dev/maliev-website/maliev-website-artifact/maliev-employee-service
  newTag: latest
```

## Overlay Configurations

### Development Overlay (T411)

**overlays/development/kustomization.yaml:**

```yaml
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization

namespace: maliev-dev

resources:
- ../../base

patchesStrategicMerge:
- deployment-patch.yaml

images:
- name: asia-southeast1-docker.pkg.dev/maliev-website/maliev-website-artifact/maliev-employee-service
  newName: asia-southeast1-docker.pkg.dev/maliev-website/maliev-website-artifact-dev/maliev-employee-service
  newTag: latest

commonLabels:
  environment: development
```

**overlays/development/deployment-patch.yaml:**

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: maliev-employee-service
spec:
  replicas: 1
  template:
    spec:
      containers:
      - name: employee-service
        resources:
          requests:
            cpu: 100m
            memory: 256Mi
          limits:
            cpu: 500m
            memory: 512Mi
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Development"
        - name: Logging__LogLevel__Default
          value: "Debug"
```

### Staging Overlay (T411)

**overlays/staging/kustomization.yaml:**

```yaml
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization

namespace: maliev-staging

resources:
- ../../base
- hpa.yaml

patchesStrategicMerge:
- deployment-patch.yaml

images:
- name: asia-southeast1-docker.pkg.dev/maliev-website/maliev-website-artifact/maliev-employee-service
  newName: asia-southeast1-docker.pkg.dev/maliev-website/maliev-website-artifact-staging/maliev-employee-service
  newTag: latest

commonLabels:
  environment: staging
```

**overlays/staging/deployment-patch.yaml:**

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: maliev-employee-service
spec:
  replicas: 2
  template:
    spec:
      containers:
      - name: employee-service
        resources:
          requests:
            cpu: 200m
            memory: 512Mi
          limits:
            cpu: 750m
            memory: 1Gi
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Staging"
        - name: Logging__LogLevel__Default
          value: "Information"
```

**overlays/staging/hpa.yaml:**

```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: maliev-employee-service
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: maliev-employee-service
  minReplicas: 2
  maxReplicas: 5
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

### Production Overlay (T411)

**overlays/production/kustomization.yaml:**

```yaml
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization

namespace: maliev-prod

resources:
- ../../base
- hpa.yaml

patchesStrategicMerge:
- deployment-patch.yaml

images:
- name: asia-southeast1-docker.pkg.dev/maliev-website/maliev-website-artifact/maliev-employee-service
  newName: asia-southeast1-docker.pkg.dev/maliev-website/maliev-website-artifact-prod/maliev-employee-service
  newTag: latest

commonLabels:
  environment: production
```

**overlays/production/deployment-patch.yaml:**

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: maliev-employee-service
spec:
  replicas: 3
  template:
    spec:
      containers:
      - name: employee-service
        resources:
          requests:
            cpu: 250m
            memory: 512Mi
          limits:
            cpu: 1000m
            memory: 1Gi
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: Logging__LogLevel__Default
          value: "Warning"
        - name: DetailedErrors
          value: "false"
```

**overlays/production/hpa.yaml:**

```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: maliev-employee-service
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: maliev-employee-service
  minReplicas: 3
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 60
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 70
  behavior:
    scaleDown:
      stabilizationWindowSeconds: 300
      policies:
      - type: Percent
        value: 50
        periodSeconds: 60
    scaleUp:
      stabilizationWindowSeconds: 0
      policies:
      - type: Percent
        value: 100
        periodSeconds: 30
      - type: Pods
        value: 2
        periodSeconds: 30
      selectPolicy: Max
```

## Google Secret Manager Setup

Create the following secrets in Google Secret Manager:

```bash
# Database
gcloud secrets create employee-service-db-connection \
  --data-file=- <<EOF
Server=postgres-cluster-rw.maliev-prod.svc.cluster.local;Port=5432;Database=employee_service_db;User Id=postgres;Password=SECURE_PASSWORD;
EOF

# JWT
gcloud secrets create jwt-secret-key --data-file=<(openssl rand -base64 32)
gcloud secrets create jwt-issuer --data-file=- <<EOF
https://api.maliev.co.th
EOF
gcloud secrets create jwt-audience --data-file=- <<EOF
https://api.maliev.co.th
EOF

# RabbitMQ
gcloud secrets create rabbitmq-connection-string \
  --data-file=- <<EOF
amqp://user:password@rabbitmq.maliev-prod.svc.cluster.local:5672
EOF

# Redis
gcloud secrets create redis-connection-string \
  --data-file=- <<EOF
redis.maliev-prod.svc.cluster.local:6379,password=SECURE_PASSWORD
EOF

# Google Cloud Storage
gcloud secrets create gcs-bucket-name --data-file=- <<EOF
maliev-employee-documents
EOF

# Data encryption
gcloud secrets create data-encryption-key --data-file=<(openssl rand -base64 32)
```

## ArgoCD Application

Create ArgoCD application for each environment:

**Development:**

```yaml
apiVersion: argoproj.io/v1alpha1
kind: Application
metadata:
  name: maliev-employee-service-dev
  namespace: argocd
spec:
  project: default
  source:
    repoURL: https://github.com/MALIEV-Co-Ltd/maliev-gitops
    targetRevision: main
    path: 3-apps/maliev-employee-service/overlays/development
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

## Verification Checklist

- [ ] All base manifests created in `maliev-gitops/3-apps/maliev-employee-service/base/`
- [ ] All overlay configurations created for development, staging, production
- [ ] External Secrets configured in Google Secret Manager
- [ ] ServiceMonitor created for Prometheus scraping
- [ ] HorizontalPodAutoscaler configured for staging and production
- [ ] ArgoCD applications created for all environments
- [ ] Namespace labels and annotations configured
- [ ] Resource limits and requests set appropriately
- [ ] Health check endpoints configured correctly
- [ ] Security contexts applied (non-root, read-only filesystem)

## Testing

1. **Validate Kustomize manifests:**

```bash
cd maliev-gitops/3-apps/maliev-employee-service/overlays/development
kustomize build . | kubectl apply --dry-run=client -f -
```

2. **Deploy to development:**

```bash
kubectl apply -k overlays/development
```

3. **Verify deployment:**

```bash
kubectl get pods -n maliev-dev -l app=maliev-employee-service
kubectl logs -f deployment/maliev-employee-service -n maliev-dev
kubectl describe externalsecret maliev-employee-service-secrets -n maliev-dev
```

4. **Test service endpoints:**

```bash
kubectl port-forward -n maliev-dev svc/maliev-employee-service 8080:8080
curl http://localhost:8080/employeeservice/liveness
curl http://localhost:8080/employeeservice/readiness
```

5. **Verify metrics scraping:**

```bash
curl http://localhost:8080/employeeservice/metrics
```

## Troubleshooting

### External Secrets not syncing

```bash
# Check External Secrets Operator logs
kubectl logs -n external-secrets-system deployment/external-secrets

# Check ExternalSecret status
kubectl describe externalsecret maliev-employee-service-secrets -n maliev-dev
```

### Pods not starting

```bash
# Check pod events
kubectl describe pod <pod-name> -n maliev-dev

# Check logs
kubectl logs <pod-name> -n maliev-dev

# Common issues:
# - Image pull errors: Verify image exists and GCR access
# - Secrets not mounted: Check External Secrets status
# - Resource limits: Check if node has sufficient resources
```

### ServiceMonitor not working

```bash
# Check if ServiceMonitor is created
kubectl get servicemonitor -n maliev-dev

# Check Prometheus targets
kubectl port-forward -n monitoring svc/prometheus 9090:9090
# Visit http://localhost:9090/targets
```

---

**Last Updated**: 2025-10-18
**Version**: 1.0
**Owner**: DevOps Team
