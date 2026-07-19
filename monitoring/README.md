# Employee Service Monitoring

This directory contains Grafana dashboard configurations for monitoring the Employee Service in production.

## Dashboards

### 1. API Metrics Dashboard (`grafana-dashboard-api-metrics.json`)

**Purpose**: Monitor HTTP API performance, request rates, response times, and error rates.

**Panels**:
- **Request Rate**: Requests per second by endpoint and method
- **Response Time Percentiles**: p50, p95, p99 latency metrics
- **Error Rate**: 4xx and 5xx error percentages
- **Overall Error Rate**: Gauge showing total error percentage (alert threshold: 5%)
- **p95 Response Time**: Gauge showing 95th percentile latency (alert threshold: 1000ms)
- **HTTP Status Code Distribution**: Pie chart of status codes
- **Concurrent Requests**: In-flight requests by endpoint
- **Response Time by Endpoint**: p95 latency broken down by route

**Alert Thresholds**:
- Error Rate > 5%: Critical
- p95 Response Time > 1000ms: Critical
- p95 Response Time > 500ms: Warning

### 2. Database Performance Dashboard (`grafana-dashboard-database-performance.json`)

**Purpose**: Monitor PostgreSQL database performance, connection pooling, and EF Core metrics.

**Panels**:
- **Query Time Percentiles**: p50, p95, p99 database query duration
- **Connection Pool Status**: In-use, idle, and max pool size
- **p95 Query Time**: Gauge showing 95th percentile query latency
- **Connection Pool Utilization**: Percentage of pool being used
- **Query Rate**: Queries per second
- **Total Connections**: Database connection count over time
- **Slow Query Detection**: Identifies slowest queries
- **SaveChanges Rate**: EF Core SaveChanges operations per second
- **Optimistic Concurrency Failures**: Concurrency conflict rate
- **Query Time by Entity Type**: p95 latency by entity

**Alert Thresholds**:
- p95 Query Time > 500ms: Critical
- p95 Query Time > 100ms: Warning
- Connection Pool Utilization > 95%: Critical
- Connection Pool Utilization > 80%: Warning

## Prerequisites

- Grafana instance accessible (use `maliev-gitops/scripts/open-grafana.ps1`)
- Prometheus datasource configured with UID `prometheus`
- Service deployed with Prometheus metrics enabled (via `prometheus-net.AspNetCore`)
- ServiceMonitor configured in Kubernetes for scraping

## Installation

### 1. Import Dashboards to Grafana

**Via UI**:
1. Open Grafana (http://localhost:3000 after port-forwarding)
2. Navigate to **Dashboards** → **Import**
3. Upload JSON file or paste JSON content
4. Select **Prometheus** datasource
5. Click **Import**

**Via API** (automated):
```bash
# Set Grafana credentials
GRAFANA_URL="http://localhost:3000"
GRAFANA_USER="admin"
GRAFANA_PASSWORD="your-password"

# Import API Metrics Dashboard
curl -X POST "${GRAFANA_URL}/api/dashboards/db" \
  -H "Content-Type: application/json" \
  -u "${GRAFANA_USER}:${GRAFANA_PASSWORD}" \
  -d @grafana-dashboard-api-metrics.json

# Import Database Performance Dashboard
curl -X POST "${GRAFANA_URL}/api/dashboards/db" \
  -H "Content-Type: application/json" \
  -u "${GRAFANA_USER}:${GRAFANA_PASSWORD}" \
  -d @grafana-dashboard-database-performance.json
```

### 2. Configure Prometheus Scraping

Ensure your ServiceMonitor in `maliev-gitops` is configured:

```yaml
apiVersion: monitoring.coreos.com/v1
kind: ServiceMonitor
metadata:
  name: employee-service
  namespace: maliev-dev
spec:
  selector:
    matchLabels:
      app: maliev-employee-service
  endpoints:
  - port: http
    path: /metrics
    interval: 15s
```

### 3. Verify Metrics

Check Prometheus is scraping metrics:

```bash
# Port-forward to Prometheus
kubectl port-forward -n monitoring svc/prometheus-operated 9090:9090

# Open http://localhost:9090 and query:
http_requests_received_total{job="maliev-employee-service"}
efcore_query_duration_seconds_bucket{job="maliev-employee-service"}
npgsql_connection_pool_in_use{job="maliev-employee-service"}
```

## Metrics Reference

### HTTP Metrics (prometheus-net.AspNetCore)

| Metric | Type | Description |
|--------|------|-------------|
| `http_requests_received_total` | Counter | Total HTTP requests by method, route, code |
| `http_request_duration_seconds` | Histogram | HTTP request duration in seconds |
| `http_requests_in_progress` | Gauge | Current in-flight requests |

### Database Metrics (Npgsql/EF Core)

| Metric | Type | Description |
|--------|------|-------------|
| `npgsql_connection_pool_in_use` | Gauge | Active connections in pool |
| `npgsql_connection_pool_idle` | Gauge | Idle connections in pool |
| `npgsql_connection_pool_max` | Gauge | Maximum pool size |
| `npgsql_connections_total` | Counter | Total connections created |
| `efcore_queries_total` | Counter | Total EF Core queries executed |
| `efcore_query_duration_seconds` | Histogram | Query execution time |
| `efcore_savechanges_total` | Counter | SaveChanges operations |
| `efcore_optimistic_concurrency_failures_total` | Counter | Concurrency conflicts |

## Customization

### Update Prometheus Job Name

If your Prometheus job name differs from `maliev-employee-service`, update all queries:

```json
{job="maliev-employee-service"}  →  {job="your-job-name"}
```

### Adjust Alert Thresholds

Edit threshold values in the JSON files:

```json
"thresholds": {
  "mode": "absolute",
  "steps": [
    { "color": "green", "value": null },
    { "color": "yellow", "value": 500 },  // Warning threshold
    { "color": "red", "value": 1000 }     // Critical threshold
  ]
}
```

### Add Custom Panels

Use Grafana's query editor to add custom panels. Example queries:

**Top 5 Slowest Endpoints**:
```promql
topk(5,
  histogram_quantile(0.95,
    sum by (route, le) (rate(http_request_duration_seconds_bucket[5m]))
  )
) * 1000
```

**Error Rate by Endpoint**:
```promql
sum by (route) (rate(http_requests_received_total{code=~"[45].."}[5m]))
/
sum by (route) (rate(http_requests_received_total[5m])) * 100
```

**Database Connection Pool Saturation**:
```promql
(npgsql_connection_pool_in_use / npgsql_connection_pool_max) * 100
```

## Alerting Rules (T394)

### Overview

The `alerting-rules.yaml` file contains Prometheus alerting rules for the Employee Service. These rules are deployed as a `PrometheusRule` custom resource in Kubernetes.

### Alert Categories

#### 1. API Alerts (`employee-service-api-alerts`)

| Alert | Severity | Threshold | Duration | Description |
|-------|----------|-----------|----------|-------------|
| `HighErrorRate` | Critical | >5% | 2m | Overall error rate (4xx + 5xx) exceeds 5% |
| `CriticalErrorRate` | Critical (Page) | >10% | 1m | Error rate exceeds 10% - immediate action required |
| `High5xxErrorRate` | Critical | >2% | 2m | Server-side (5xx) errors exceed 2% |
| `SlowApiResponseTime` | Warning | p95 >1s | 5m | p95 response time exceeds 1 second |
| `VerySlowApiResponseTime` | Critical (Page) | p95 >3s | 2m | p95 response time exceeds 3 seconds |
| `UnusuallyHighRequestRate` | Warning | >1000 req/s | 5m | Possible DDoS or traffic spike |

#### 2. Database Alerts (`employee-service-database-alerts`)

| Alert | Severity | Threshold | Duration | Description |
|-------|----------|-----------|----------|-------------|
| `SlowDatabaseQueries` | Warning | p95 >500ms | 5m | Database queries taking longer than 500ms |
| `VerySlowDatabaseQueries` | Critical | p95 >2s | 2m | Database queries taking longer than 2 seconds |
| `HighConnectionPoolUtilization` | Warning | >80% | 5m | Connection pool usage exceeds 80% |
| `ConnectionPoolExhausted` | Critical (Page) | >95% | 2m | Connection pool nearly exhausted |
| `HighDatabaseQueryRate` | Info | >1000 qps | 10m | High database query rate detected |
| `HighOptimisticConcurrencyFailures` | Warning | >5/s | 5m | High rate of concurrent update conflicts |

#### 3. Integration Alerts (`employee-service-integration-alerts`)

| Alert | Severity | Threshold | Duration | Description |
|-------|----------|-----------|----------|-------------|
| `RabbitMQPublishingFailures` | Warning | >0/s | 2m | Failed to publish events to RabbitMQ |
| `RabbitMQCircuitBreakerOpen` | Critical | Open | 1m | Circuit breaker protecting RabbitMQ is open |
| `RedisConnectionFailures` | Warning | >0/s | 2m | Cannot connect to Redis cache |
| `HealthCheckFailure` | Critical (Page) | Down | 1m | Service health endpoint is down |
| `DatabaseHealthCheckFailed` | Critical | Failed | 2m | Database health check failed |

#### 4. Resource Alerts (`employee-service-resource-alerts`)

| Alert | Severity | Threshold | Duration | Description |
|-------|----------|-----------|----------|-------------|
| `HighMemoryUsage` | Warning | >80% | 5m | Pod memory usage exceeds 80% of limit |
| `HighCPUUsage` | Warning | >80% | 5m | Pod CPU usage exceeds 80% of limit |
| `FrequentPodRestarts` | Warning | >0/15m | 5m | Pod is restarting frequently |
| `PodNotReady` | Critical | Not Ready | 5m | Pod readiness probe failing |

### Deployment

#### 1. Apply Alerting Rules to Kubernetes

```bash
# Apply to development environment
kubectl apply -f monitoring/alerting-rules.yaml -n maliev-dev

# Apply to staging environment
kubectl apply -f monitoring/alerting-rules.yaml -n maliev-staging

# Apply to production environment
kubectl apply -f monitoring/alerting-rules.yaml -n maliev-prod
```

#### 2. Verify Rules Loaded

```bash
# Check PrometheusRule resource
kubectl get prometheusrule -n maliev-dev

# Port-forward to Prometheus and check rules
kubectl port-forward -n monitoring svc/prometheus-operated 9090:9090

# Visit http://localhost:9090/rules
# Ensure "employee-service-api-alerts", "employee-service-database-alerts",
# "employee-service-integration-alerts", "employee-service-resource-alerts" groups are loaded
```

#### 3. Configure AlertManager

Ensure AlertManager is configured to route alerts. Example configuration:

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: alertmanager-config
  namespace: monitoring
data:
  alertmanager.yml: |
    global:
      resolve_timeout: 5m

    route:
      group_by: ['alertname', 'service']
      group_wait: 10s
      group_interval: 10s
      repeat_interval: 12h
      receiver: 'default'
      routes:
        # Page on-call for critical alerts
        - match:
            page: "true"
          receiver: 'pagerduty'
          continue: true

        # Send all employee-service alerts to Slack
        - match:
            service: 'employee-service'
          receiver: 'slack-employee-service'

    receivers:
      - name: 'default'
        webhook_configs:
          - url: 'http://alertmanager-webhook:5001/'

      - name: 'pagerduty'
        pagerduty_configs:
          - service_key: '<PAGERDUTY_SERVICE_KEY>'
            description: '{{ .GroupLabels.alertname }}: {{ .Annotations.summary }}'

      - name: 'slack-employee-service'
        slack_configs:
          - api_url: '<SLACK_WEBHOOK_URL>'
            channel: '#employee-service-alerts'
            title: '{{ .GroupLabels.alertname }}'
            text: '{{ .Annotations.description }}'
```

### Testing Alerts

#### Trigger Test Alerts

```bash
# Test high error rate
curl -X POST http://localhost:8080/api/test/generate-errors?count=100

# Test slow response time
curl -X POST http://localhost:8080/api/test/slow-endpoint?delay=2000

# Test database connection pool
curl -X POST http://localhost:8080/api/test/exhaust-connections
```

#### Verify Alert Firing

```bash
# Port-forward to Prometheus
kubectl port-forward -n monitoring svc/prometheus-operated 9090:9090

# Visit http://localhost:9090/alerts
# Check for alerts in "Firing" state

# Port-forward to AlertManager
kubectl port-forward -n monitoring svc/alertmanager-operated 9093:9093

# Visit http://localhost:9093/#/alerts
# Verify alerts are received by AlertManager
```

### Alert Severity Levels

- **Critical (Page)**: Requires immediate on-call response (e.g., service down, critical error rate)
- **Critical**: Serious issue requiring prompt attention (e.g., high error rate, slow queries)
- **Warning**: Issue that needs investigation soon (e.g., high resource usage, integration failures)
- **Info**: Informational alert for awareness (e.g., high query rate)

### Runbooks

Create runbooks at `https://github.com/MALIEV-Co-Ltd/maliev-employee-service/wiki/Runbooks` for each alert with:
- **Symptoms**: What the alert indicates
- **Impact**: Effect on users and service
- **Diagnosis**: How to investigate the issue
- **Resolution**: Steps to fix the problem
- **Prevention**: How to prevent recurrence

## Troubleshooting

### Dashboards show "No Data"

1. **Check Prometheus scraping**:
   ```bash
   kubectl port-forward -n monitoring svc/prometheus-operated 9090:9090
   # Visit http://localhost:9090/targets
   # Ensure employee-service target is UP
   ```

2. **Verify metrics endpoint**:
   ```bash
   kubectl port-forward -n maliev-dev svc/maliev-employee-service 8080:8080
   curl http://localhost:8080/metrics
   # Should return Prometheus metrics
   ```

3. **Check datasource configuration**:
   - Go to Grafana → Configuration → Data Sources
   - Ensure Prometheus datasource with UID `prometheus` exists
   - Test connection

### Incorrect metric names

Different versions of `prometheus-net.AspNetCore` may use different metric names. Check actual metrics:

```bash
curl http://localhost:8080/metrics | grep http_
curl http://localhost:8080/metrics | grep efcore_
curl http://localhost:8080/metrics | grep npgsql_
```

Update dashboard queries to match actual metric names.

## Related Documentation

- [Prometheus Metrics Endpoint](../Maliev.EmployeeService.Api/Program.cs#L391) - T391 implementation
- [ServiceMonitor Configuration](../../maliev-gitops/3-apps/employee-service/base/servicemonitor.yaml) - T413
- [Alert Rules](./alerting-rules.yaml) - T394
- [Grafana Access Script](../../maliev-gitops/scripts/open-grafana.ps1)

## Phase 16 Tasks

- [X] T392: Create Grafana dashboard JSON for API metrics
- [X] T393: Create Grafana dashboard JSON for database performance
- [ ] T394: Configure alerting rules for error rates >5%, response times >1s

**Phase 16 - Monitoring and Observability**
