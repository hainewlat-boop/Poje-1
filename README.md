# Bütünleşik Kurumsal Ödeme ve Başvuru Yönetim Platformu

A comprehensive enterprise payment and application management platform designed for government institutions. Built with security-first principles, immutable audit trails, and zero-trust architecture.

## 🏗️ Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                           CLIENTS                                    │
│                    (Web Browser / Mobile)                            │
└─────────────────────────────────────────────────────────────────────┘
                                  │
                                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│                       API GATEWAY (Nginx)                            │
│              TLS Termination / Rate Limiting / IP Allowlist          │
└─────────────────────────────────────────────────────────────────────┘
                                  │
                    ┌─────────────┼─────────────┐
                    ▼             ▼             ▼
┌───────────┐ ┌───────────┐ ┌───────────┐ ┌───────────┐
│    IAM    │ │Application│ │  Payment  │ │  Ledger   │
│  Service  │ │  Service  │ │  Service  │ │  Service  │
└───────────┘ └───────────┘ └───────────┘ └───────────┘
                    │             │             │
┌───────────┐ ┌───────────┐ ┌───────────┐ ┌───────────┐
│   Audit   │ │ Document  │ │Notification│ │ Reference │
│  Service  │ │  Service  │ │  Service  │ │   Data    │
└───────────┘ └───────────┘ └───────────┘ └───────────┘
                                  │
                    ┌─────────────┼─────────────┐
                    ▼             ▼             ▼
            ┌───────────┐ ┌───────────┐ ┌───────────┐
            │PostgreSQL │ │   Redis   │ │ RabbitMQ  │
            │(per-svc)  │ │(cache/otp)│ │  (async)  │
            └───────────┘ └───────────┘ └───────────┘
```

## 📋 Features

### Security (Non-Negotiable)
- ✅ **No Card Data Storage**: PAN/CVV never stored; payments via bank virtual POS
- ✅ **RAM-Only Sensitive Data**: No localStorage/sessionStorage for tokens
- ✅ **Immutable Records**: Critical records never deleted; corrections via reversal
- ✅ **Hash Chain Audit**: Append-only logs with cryptographic chain (SHA-256)
- ✅ **OTPT**: One-Time-Per-Transaction tokens for critical operations
- ✅ **MFA**: TOTP-based two-factor authentication
- ✅ **Session Binding**: Fingerprint-based session validation
- ✅ **Progressive Lockout**: Escalating lockout on failed logins

### Architecture
- Clean Architecture with DDD bounded contexts
- Zero Trust security model
- Stateless microservices
- Event-driven with Outbox pattern
- CQRS with MediatR

### Observability
- Structured JSON logs with correlation IDs
- OpenTelemetry distributed tracing
- Prometheus metrics endpoints
- SIEM integration ready

## 🚀 Quick Start

### Prerequisites
- Docker & Docker Compose
- .NET 8 SDK
- Node.js 20+
- PostgreSQL 16 (via Docker)
- Redis 7 (via Docker)

### 1. Start Infrastructure

```bash
cd infra
docker-compose up -d postgres redis rabbitmq jaeger prometheus
```

### 2. Run Migrations

```bash
cd src/services/iam
dotnet ef database update --project src/IAM.Infrastructure
```

### 3. Start Services

```bash
# Start IAM Service
cd src/services/iam/src/IAM.API
dotnet run

# In another terminal, start frontend
cd src/frontend
npm install
npm run dev
```

### 4. Access the Application

- **Frontend**: http://localhost:5173
- **IAM API**: http://localhost:5001
- **Swagger UI**: http://localhost:5001/swagger
- **Jaeger UI**: http://localhost:16686
- **Prometheus**: http://localhost:9090

### Default Credentials

```
Email: admin@platform.gov.tr
Password: Admin123!@#$
```

## 📁 Project Structure

```
/
├── docs/                    # Documentation
│   ├── architecture.md      # System architecture
│   ├── security.md          # Security documentation
│   ├── threat-model.md      # STRIDE threat analysis
│   └── runbook.md           # Operations runbook
├── infra/                   # Infrastructure
│   ├── docker-compose.yml   # Docker services
│   ├── k8s/                 # Kubernetes manifests
│   ├── nginx/               # API Gateway config
│   └── prometheus/          # Monitoring config
├── src/
│   ├── shared/
│   │   ├── building-blocks/ # DDD primitives, Result pattern, Hash Chain
│   │   ├── contracts/       # DTOs, API contracts
│   │   └── security/        # Crypto, JWT, OTPT, Session
│   ├── services/
│   │   ├── iam/             # Identity & Access Management
│   │   ├── application/     # Case Management
│   │   ├── payment/         # Payment Processing
│   │   ├── ledger/          # Financial Records
│   │   ├── audit/           # Audit Trail
│   │   ├── document/        # Document Generation
│   │   ├── notification/    # Notifications
│   │   └── reference-data/  # Static Data
│   └── frontend/            # React SPA
├── tests/
│   ├── unit/                # Unit tests
│   ├── integration/         # Integration tests
│   ├── e2e/                 # End-to-end tests
│   └── security/            # Security tests
└── tools/
    ├── offline-bundle/      # Air-gapped deployment
    └── scripts/             # Utility scripts
```

## 🔐 Security Model

### Authentication Flow
```
1. User submits email/password
2. System validates credentials (Argon2id)
3. If MFA enabled: Issue temporary MFA token
4. User submits TOTP code
5. System creates session with fingerprint binding
6. Issue JWT access token (15min) + refresh token (7 days)
```

### OTPT (One-Time-Per-Transaction)
```
1. Client requests OTPT for critical operation
2. Server validates session + policy
3. Server issues token bound to: route + nonce + sessionId + fingerprintId
4. Client includes OTPT in request
5. Server atomically consumes token (Lua script in Redis)
6. Token cannot be reused
```

### Hash Chain Verification
```
Genesis: H0 = SHA-256(ENV_ID + VERSION + TIMESTAMP)
Chain:   Hn = SHA-256(CanonicalJSON(Record) + Hn-1)

Verification runs as background job.
On chain break: CRITICAL alert + read-only mode
```

## 🧪 Testing

### Run Unit Tests
```bash
cd tests/unit
dotnet test
```

### Run Integration Tests
```bash
cd tests/integration
dotnet test
```

### Run All Tests with Coverage
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov
```

### Security Tests
```bash
# SAST with SonarQube
dotnet sonarscanner begin /k:"platform"
dotnet build
dotnet sonarscanner end

# DAST with OWASP ZAP
docker run -t owasp/zap2docker-stable zap-baseline.py -t http://localhost:5001
```

## 🔧 Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection | See appsettings |
| `ConnectionStrings__Redis` | Redis connection | localhost:6379 |
| `Jwt__SecretKey` | JWT signing key | Required |
| `Jwt__Issuer` | JWT issuer | Platform |
| `Jwt__Audience` | JWT audience | Platform |
| `Security__MasterKey` | Data encryption key | Required in prod |

### Service Ports

| Service | HTTP | gRPC | Health |
|---------|------|------|--------|
| IAM | 5001 | 5051 | /health |
| Application | 5002 | 5052 | /health |
| Payment | 5003 | 5053 | /health |
| Ledger | 5004 | 5054 | /health |
| Audit | 5005 | 5055 | /health |
| Document | 5006 | 5056 | /health |
| Notification | 5007 | 5057 | /health |
| Reference Data | 5008 | 5058 | /health |

## 📊 Observability

### Logs
Structured JSON logs with:
- `timestamp`: ISO 8601 UTC
- `level`: Info/Warn/Error
- `correlationId`: Request tracking
- `userId`: Authenticated user
- `service`: Service name
- `message`: Log message

### Metrics
Prometheus endpoints at `/metrics`:
- HTTP request duration
- Request count by status
- Active connections
- Business metrics (payments, applications)

### Traces
OpenTelemetry traces to Jaeger:
- Full request path
- Database calls
- External service calls
- Message queue operations

## 🚢 Deployment

### Docker Compose (Development)
```bash
docker-compose -f infra/docker-compose.yml up -d
```

### Kubernetes (Production)
```bash
kubectl apply -f infra/k8s/namespace.yaml
kubectl apply -f infra/k8s/secrets.yaml
kubectl apply -f infra/k8s/configmaps.yaml
kubectl apply -f infra/k8s/deployments/
kubectl apply -f infra/k8s/services/
```

### Offline/Air-gapped
```bash
# Build offline bundle
./tools/offline-bundle/create-bundle.sh

# Verify signatures
./tools/offline-bundle/verify-signatures.sh

# Deploy
./tools/offline-bundle/deploy.sh
```

## 📈 KPI Targets

| Metric | Target | Status |
|--------|--------|--------|
| P95 API Latency | < 300ms | ✅ |
| Payment Failure Rate | < 1% | ✅ |
| RTO (Recovery Time) | < 2 hours | ✅ |
| RPO (Recovery Point) | < 15 minutes | ✅ |
| Unit Test Coverage | > 80% | 🔄 |

## 📝 License

This software is proprietary and intended for government use only.

## 🤝 Contributing

Please read the security guidelines before contributing. All changes require security review.

---

**IMPORTANT**: This system handles sensitive government data. Unauthorized access is prohibited and monitored. All actions are logged and auditable.
