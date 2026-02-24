// K6 Load Test for Maliev Employee Service
// Run with: k6 run --vus 500 --duration 5m load-test.js

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';

// Custom metrics
const errorRate = new Rate('errors');

// Test configuration
export const options = {
  stages: [
    { duration: '2m', target: 100 },  // Ramp up to 100 users
    { duration: '3m', target: 500 },  // Ramp up to 500 users
    { duration: '2m', target: 500 },  // Stay at 500 users
    { duration: '1m', target: 0 },    // Ramp down to 0 users
  ],
  thresholds: {
    http_req_duration: ['p(95)<1000'], // 95% of requests should be below 1s
    http_req_failed: ['rate<0.05'],    // Error rate should be less than 5%
    errors: ['rate<0.05'],
  },
};

// Base URL - update with your service endpoint
const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080';

// JWT Token - should be provided via environment variable
const TOKEN = __ENV.JWT_TOKEN || '';

const headers = {
  'Content-Type': 'application/json',
  'Authorization': `Bearer ${TOKEN}`,
};

export default function () {
  // Test 1: Health check (liveness)
  let res1 = http.get(`${BASE_URL}/employeeservice/liveness`);
  check(res1, {
    'liveness is status 200': (r) => r.status === 200,
  }) || errorRate.add(1);

  sleep(0.5);

  // Test 2: Get employee profile
  let employeeId = 'e8b8c8d0-1234-5678-9abc-def012345678'; // Replace with actual employee ID
  let res2 = http.get(`${BASE_URL}/employeeservice/api/employees/${employeeId}`, { headers });
  check(res2, {
    'get employee is status 200': (r) => r.status === 200,
    'get employee returns data': (r) => r.json('id') !== undefined,
  }) || errorRate.add(1);

  sleep(1);

  // Test 3: Get leave balances
  let res3 = http.get(`${BASE_URL}/employeeservice/api/employees/${employeeId}/leave-balances`, { headers });
  check(res3, {
    'get leave balances is status 200': (r) => r.status === 200,
  }) || errorRate.add(1);

  sleep(1);

  // Test 4: Get org chart
  let res4 = http.get(`${BASE_URL}/employeeservice/api/departments/org-chart`, { headers });
  check(res4, {
    'get org chart is status 200': (r) => r.status === 200,
  }) || errorRate.add(1);

  sleep(2);
}

// Setup function - runs once before the test
export function setup() {
  console.log('Starting load test...');
  console.log(`Target URL: ${BASE_URL}`);
  console.log(`Max concurrent users: 500`);
}

// Teardown function - runs once after the test
export function teardown(data) {
  console.log('Load test completed');
}
