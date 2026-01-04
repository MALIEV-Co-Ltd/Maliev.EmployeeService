# Maliev Employee Service

[![Build Status](https://img.shields.io/badge/Build-Passing-success)](https://github.com/ORGANIZATION/Maliev.EmployeeService)
[![.NET Version](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Database](https://img.shields.io/badge/Database-PostgreSQL%2018-blue)](https://www.postgresql.org/)

The core HR and organizational management microservice.

**Role in MALIEV Architecture**: Acts as the "Source of Truth" for employee profiles and organizational hierarchy. It publishes integration events consumed by Leave, Payroll, and IAM services.

---

## 🏗️ Architecture & Tech Stack

- **Framework**: ASP.NET Core 10.0 (C# 13)
- **Pattern**: 5-Layer Clean Architecture
- **Database**: PostgreSQL 18 with Entity Framework Core 10.x
- **Cache**: Redis 7.x (Employee metadata & org charts)
- **Messaging**: RabbitMQ via MassTransit
- **API Documentation**: OpenAPI 3.1 + Scalar UI

---

## ⚖️ Constitution Rules

### Banned Libraries
- ❌ **AutoMapper**: Explicit manual mapping only.
- ❌ **FluentValidation**: Data Annotations only.
- ❌ **FluentAssertions**: xUnit `Assert` only.
- ❌ **In-memory Test DB**: Testcontainers with PostgreSQL 18.

### Mandatory Practices
- ✅ **TreatWarningsAsErrors**: Enabled.
- ✅ **XML Documentation**: Required on all public members.
- ✅ **No Secrets in Code**: Environment variable injection only.
- ✅ **IAM Integration**: Permissions naming: `employee.{resource}.{action}`.

---

## ✨ Key Features

- **Employee Lifecycle**: Management of onboarding, transfers, and offboarding.
- **Org Chart Engine**: Real-time resolution of reporting lines and hierarchies.
- **Profile Self-Service**: Employee-facing API for personal data management.
- **Event-Driven HR**: Publishes `EmployeeCreated` and `EmployeeTerminated` events.

---

## 🚀 Quick Start

### Prerequisites
- .NET 10.0 SDK
- PostgreSQL 18
- Docker Desktop

### Local Development Setup

1. **Clone the repository**
```bash
git clone https://github.com/ORGANIZATION/Maliev.EmployeeService.git
cd Maliev.EmployeeService
```

2. **Apply Migrations**
```bash
dotnet ef database update --project Maliev.EmployeeService.Infrastructure --startup-project Maliev.EmployeeService.Api
```

3. **Run the Service**
```bash
dotnet run --project Maliev.EmployeeService.Api
```

---

## 📡 API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/v1/employees/{id}` | Get detailed employee profile |
| GET | `/v1/departments` | List organizational departments |
| POST | `/v1/employees` | Onboard a new employee (HR only) |

---

## 🏥 Health & Monitoring
- **Liveness**: `GET /employee/liveness`
- **Readiness**: `GET /employee/readiness`
- **Metrics**: `GET /employee/metrics`

---

## 🧪 Testing

```bash
# Run integration tests
dotnet test --verbosity normal
```

---

## 📦 Deployment
- **Docker Image**: `REGION-docker.pkg.dev/PROJECT_ID/REPOSITORY/maliev-employee-service:{sha}`

---

## 📄 License
Proprietary - © 2025 MALIEV Co., Ltd. All rights reserved.
