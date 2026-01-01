# Employee Service

Core HR and employee management microservice for Maliev Co. Ltd.

## Overview

The Employee Service is a .NET 10.0 microservice that manages core aspects of the employee lifecycle including:

- **Employee Profile Management** - Self-service profile updates and emergency contacts
- **Organizational Structure** - Departments, teams, and reporting hierarchies
- **Reporting & Analytics** - Core organizational metrics and org charts

## Architecture

- **Framework**: ASP.NET Core 10.0
- **Database**: PostgreSQL 18 with Entity Framework Core
- **Messaging**: RabbitMQ via MassTransit (for integration events)
- **Caching**: Redis (optional, falls back to in-memory)
- **Authentication**: JWT Bearer tokens
- **Authorization**: Role-based and resource-based policies
- **API Documentation**: OpenAPI with Scalar UI (development only) and comprehensive XML comments
- **Logging**: Serilog with structured logging and correlation IDs

## Authentication & Authorization

### JWT Token Requirements

The Employee Service uses JWT (JSON Web Token) Bearer authentication for all protected endpoints.

#### Token Structure

JWT tokens must be obtained from the Auth Service and include the following claims:

```json
{
  "sub": "employee-guid",
  "email": "employee@maliev.co.th",
  "role": "Employee|Manager|HR|Admin",
  "name": "John Doe",
  "employeeNumber": "EMP001",
  "iss": "https://maliev.co.th",
  "aud": "employee-service",
  "exp": 1234567890,
  "iat": 1234567890
}
```

### Authorization Roles

The service supports four primary roles:

#### 1. Employee (Base Role)
- View own profile and emergency contacts
- Update limited profile fields (personal email, phone, preferred name)
- Manage own emergency contacts
- View own team memberships

#### 2. Manager
- All Employee permissions
- View direct reports' profiles
- View organizational chart for their subtree
- Manage team assignments
- Create and update teams

#### 3. HR
- All Manager permissions
- View all employee profiles
- Create new employees
- Transfer employees between departments
- Create and manage departments
- Access core reports and analytics

#### 4. Admin
- All HR permissions
- Full system access
- Manage system configuration
- Access audit logs
- Perform bulk operations

## Getting Started

### Prerequisites

- .NET 10.0 SDK
- PostgreSQL 18
- Docker (optional, for Redis and RabbitMQ)
- Git

### Local Development Setup

1. **Clone the repository**:

```bash
git clone https://github.com/MALIEV-Co-Ltd/Maliev.EmployeeService.git
cd Maliev.EmployeeService
```

2. **Run database migrations**:

```bash
dotnet ef database update --project Maliev.EmployeeService.Infrastructure --startup-project Maliev.EmployeeService.Api
```

3. **Run the service**:

```bash
dotnet run --project Maliev.EmployeeService.Api
```

The service will be available at `https://localhost:7001` (HTTPS) or `http://localhost:5000` (HTTP).

## API Documentation

### Base URL

- **Development**: `http://localhost:5000/employee`
- **Production**: `https://api.maliev.co.th/employee`

### Endpoints

#### Employee Profile Management

```
GET    /v1/profile/{employeeId}/profile               - Get employee profile
PUT    /v1/profile/{employeeId}/profile               - Update employee profile
POST   /v1/profile/{employeeId}/emergency-contacts    - Create emergency contact
PUT    /v1/profile/{employeeId}/emergency-contacts/{contactId} - Update emergency contact
DELETE /v1/profile/{employeeId}/emergency-contacts/{contactId} - Delete emergency contact
```

#### Team Management

```
GET    /v1/teams/{teamId}                             - Get team details
GET    /v1/teams/employee/{employeeId}                - Get employee teams
POST   /v1/teams                                      - Create team (Manager/HR)
POST   /v1/teams/{teamId}/members                     - Add team member (Manager/HR)
DELETE /v1/teams/{teamId}/members/{employeeId}        - Remove team member (Manager/HR)
```

#### Department Management

```
GET    /v1/departments                                - List all departments
GET    /v1/departments/{departmentId}                 - Get department details
GET    /v1/departments/{departmentId}/employees       - Get department employees
POST   /v1/departments                                - Create department (HR/Admin)
PUT    /v1/departments/{departmentId}                 - Update department (HR/Admin)
```

## Integration Events

The service publishes integration events to RabbitMQ for cross-service communication:

### Published Events

- `EmployeeCreatedIntegrationEvent` - New employee created
- `EmployeeTerminatedIntegrationEvent` - Employee offboarded
- `DepartmentTransferredIntegrationEvent` - Employee transferred to new department

## Support

For issues or questions:

- **GitHub Issues**: https://github.com/MALIEV-Co-Ltd/Maliev.EmployeeService/issues
- **Email**: dev@maliev.co.th
- **Documentation**: https://docs.maliev.co.th/employee-service

## License

Copyright © 2025 Maliev Co. Ltd. All rights reserved.