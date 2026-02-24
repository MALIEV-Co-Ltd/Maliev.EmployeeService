# Performance Testing with K6

This directory contains K6 performance test scripts for the Maliev Employee Service.

## Prerequisites

1. Install K6:
   ```bash
   # Windows (using Chocolatey)
   choco install k6

   # macOS
   brew install k6

   # Linux
   sudo gpg -k
   sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
   echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
   sudo apt-get update
   sudo apt-get install k6
   ```

2. Set up environment variables:
   ```bash
   export BASE_URL="https://your-service-url"
   export JWT_TOKEN="your-jwt-token"
   ```

## Test Scripts

### 1. Load Test (`load-test.js`)

Tests normal and peak load conditions with 500 concurrent users.

```bash
k6 run --vus 500 --duration 5m load-test.js
```

**Metrics:**
- Target: 500 concurrent users
- Duration: 8 minutes (including ramp-up/down)
- Success criteria: 95% of requests < 1s, error rate < 5%

### 2. Stress Test (`stress-test.js`)

Pushes the system beyond normal capacity to find breaking points.

```bash
k6 run stress-test.js
```

**Metrics:**
- Peak: 2000 concurrent users
- Duration: 12 minutes
- Mix: 60% reads, 30% complex queries, 10% writes
- Success criteria: p95 < 2s, error rate < 10%

### 3. Spike Test (`spike-test.js`)

Tests system behavior under sudden traffic spikes.

```bash
k6 run spike-test.js
```

**Metrics:**
- Spikes: 100 → 2000 → 100 → 3000 users
- Duration: ~4 minutes
- Success criteria: p95 < 3s, error rate < 15%

## Running Tests in Docker

```bash
docker run --rm -i grafana/k6 run - <load-test.js
```

## Running Tests in Kubernetes

```bash
kubectl run k6-test --rm -i --tty --image=grafana/k6 --restart=Never -- run - <load-test.js
```

## Interpreting Results

### Key Metrics

- **http_req_duration**: Request response time
  - p(95): 95th percentile (most requests should be below this)
  - p(99): 99th percentile

- **http_req_failed**: Failed request rate
  - Target: < 5% for load test

- **vus**: Virtual users (concurrent users)

- **iterations**: Total number of test iterations completed

### Success Criteria (T390)

For T390 completion, the following criteria must be met:

1. **Load Test (500 concurrent users)**:
   - ✅ p95 response time < 1000ms
   - ✅ Error rate < 5%
   - ✅ No database connection pool exhaustion
   - ✅ No memory leaks during sustained load

2. **Stress Test**:
   - ✅ System recovers after peak load
   - ✅ Graceful degradation under extreme load
   - ✅ No crash or unrecoverable errors

3. **Spike Test**:
   - ✅ System handles sudden traffic spikes
   - ✅ Auto-scaling (if configured) responds appropriately
   - ✅ Returns to normal performance after spike

## Example Output

```
     ✓ liveness is status 200
     ✓ get employee is status 200

     checks.........................: 98.50% ✓ 19700    ✗ 300
     data_received..................: 4.2 MB 70 kB/s
     data_sent......................: 1.8 MB 30 kB/s
     http_req_duration..............: avg=245ms  min=50ms med=200ms max=850ms p(95)=450ms p(99)=650ms
     http_req_failed................: 2.50%  ✓ 250      ✗ 9750
     http_reqs......................: 10000  166/s
     iterations.....................: 2500   41/s
     vus............................: 500    min=0      max=500
     vus_max........................: 500    min=500    max=500
```

## Integration with CI/CD

Add performance tests to your CI/CD pipeline:

```yaml
- name: Run Performance Tests
  run: |
    k6 run --summary-export=results.json load-test.js

- name: Upload Results
  uses: actions/upload-artifact@v3
  with:
    name: k6-results
    path: results.json
```

## Monitoring During Tests

Monitor the following during performance tests:

1. **Application Metrics** (Grafana):
   - Request rate
   - Response times
   - Error rates
   - Database query performance

2. **Infrastructure Metrics**:
   - CPU utilization
   - Memory usage
   - Network I/O
   - Database connections

3. **External Dependencies**:
   - RabbitMQ queue depth
   - Redis cache hit rate
   - Google Cloud Storage latency

## Troubleshooting

### High Error Rates

Check:
- Database connection pool size
- Rate limiting configuration
- External service availability

### High Response Times

Check:
- Database query performance (missing indexes)
- N+1 query problems
- Insufficient caching
- Slow external API calls

### System Crashes

Check:
- Memory leaks
- Database connection leaks
- Insufficient resources (CPU, memory)
- Unhandled exceptions in concurrent scenarios

## Next Steps After Testing

1. Analyze results and identify bottlenecks
2. Implement optimizations (caching, indexing, query optimization)
3. Rerun tests to verify improvements
4. Document performance baselines
5. Set up continuous performance monitoring
