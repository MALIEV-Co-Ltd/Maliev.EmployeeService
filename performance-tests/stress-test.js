// K6 Stress Test for Maliev Employee Service
// Pushes the system beyond normal capacity to find breaking points
// Run with: k6 run stress-test.js

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

// Custom metrics
const errorRate = new Rate('errors');
const successRate = new Rate('success');
const responseTime = new Trend('response_time');

// Test configuration - aggressive stress test
export const options = {
  stages: [
    { duration: '1m', target: 100 },   // Warm up
    { duration: '2m', target: 500 },   // Normal load
    { duration: '3m', target: 1000 },  // High load
    { duration: '3m', target: 2000 },  // Extreme load - find breaking point
    { duration: '2m', target: 100 },   // Recovery
    { duration: '1m', target: 0 },     // Cool down
  ],
  thresholds: {
    http_req_duration: ['p(95)<2000', 'p(99)<5000'], // More lenient for stress test
    http_req_failed: ['rate<0.10'],    // Allow up to 10% errors
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080';
const TOKEN = __ENV.JWT_TOKEN || '';

const headers = {
  'Content-Type': 'application/json',
  'Authorization': `Bearer ${TOKEN}`,
};

// Mix of read and write operations
export default function () {
  const scenario = Math.random();

  if (scenario < 0.6) {
    // 60% read operations
    readOperations();
  } else if (scenario < 0.9) {
    // 30% complex queries
    complexQueries();
  } else {
    // 10% write operations
    writeOperations();
  }

  sleep(Math.random() * 3); // Random think time
}

function readOperations() {
  // Simulate reading employee profiles
  let employeeId = generateRandomEmployeeId();
  let res = http.get(`${BASE_URL}/employeeservice/api/employees/${employeeId}`, { headers });

  const success = check(res, {
    'read is successful': (r) => r.status === 200,
  });

  errorRate.add(!success);
  successRate.add(success);
  responseTime.add(res.timings.duration);
}

function complexQueries() {
  // Simulate complex queries like org charts and reports
  const queries = [
    '/employeeservice/api/departments/org-chart',
    '/employeeservice/api/reports/headcount',
    '/employeeservice/api/reports/diversity-metrics',
  ];

  const query = queries[Math.floor(Math.random() * queries.length)];
  let res = http.get(`${BASE_URL}${query}`, { headers });

  const success = check(res, {
    'complex query is successful': (r) => r.status === 200,
  });

  errorRate.add(!success);
  successRate.add(success);
  responseTime.add(res.timings.duration);
}

function writeOperations() {
  // Simulate write operations (emergency contact updates)
  let employeeId = generateRandomEmployeeId();
  let payload = JSON.stringify({
    contactName: 'Emergency Contact',
    relationship: 'Spouse',
    phoneNumber: '+66-12-345-6789',
    email: 'emergency@example.com',
    priorityOrder: 1,
  });

  let res = http.post(
    `${BASE_URL}/employeeservice/api/employees/${employeeId}/emergency-contacts`,
    payload,
    { headers }
  );

  const success = check(res, {
    'write is successful': (r) => r.status === 200 || r.status === 201,
  });

  errorRate.add(!success);
  successRate.add(success);
  responseTime.add(res.timings.duration);
}

function generateRandomEmployeeId() {
  // Generate a random employee ID (would need to be replaced with actual IDs)
  const ids = [
    'e8b8c8d0-1234-5678-9abc-def012345678',
    'f9c9d9e1-2345-6789-abcd-ef0123456789',
    // Add more real employee IDs here
  ];
  return ids[Math.floor(Math.random() * ids.length)];
}

export function setup() {
  console.log('Starting stress test...');
  console.log('WARNING: This test will push the system to its limits');
  console.log(`Target URL: ${BASE_URL}`);
  console.log(`Peak concurrent users: 2000`);
}

export function teardown(data) {
  console.log('Stress test completed');
}
