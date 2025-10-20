// K6 Spike Test for Maliev Employee Service
// Tests system behavior under sudden traffic spikes
// Run with: k6 run spike-test.js

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';

const errorRate = new Rate('errors');

// Spike test configuration
export const options = {
  stages: [
    { duration: '30s', target: 100 },  // Normal load
    { duration: '10s', target: 2000 }, // Sudden spike!
    { duration: '1m', target: 2000 },  // Sustain spike
    { duration: '10s', target: 100 },  // Drop to normal
    { duration: '30s', target: 100 },  // Recover
    { duration: '10s', target: 3000 }, // Second spike!
    { duration: '1m', target: 3000 },  // Sustain higher spike
    { duration: '10s', target: 0 },    // Complete drop
  ],
  thresholds: {
    http_req_duration: ['p(95)<3000'], // Allow higher latency during spikes
    http_req_failed: ['rate<0.15'],    // Allow up to 15% errors during spikes
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080';
const TOKEN = __ENV.JWT_TOKEN || '';

const headers = {
  'Content-Type': 'application/json',
  'Authorization': `Bearer ${TOKEN}`,
};

export default function () {
  // Focus on most common read operations during spike
  let res1 = http.get(`${BASE_URL}/employeeservice/liveness`);
  check(res1, {
    'liveness check successful': (r) => r.status === 200,
  }) || errorRate.add(1);

  sleep(0.1);

  // Simulate user profile access
  let employeeId = 'e8b8c8d0-1234-5678-9abc-def012345678';
  let res2 = http.get(`${BASE_URL}/employeeservice/api/employees/${employeeId}`, { headers });
  check(res2, {
    'profile access successful': (r) => r.status === 200,
  }) || errorRate.add(1);

  sleep(0.5);
}

export function setup() {
  console.log('Starting spike test...');
  console.log('This test simulates sudden traffic spikes');
  console.log(`Target URL: ${BASE_URL}`);
  console.log(`Peak: 3000 concurrent users`);
}

export function teardown() {
  console.log('Spike test completed');
}
