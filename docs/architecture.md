# Architecture Overview

## Bütünleşik Kurumsal Ödeme ve Başvuru Yönetim Platformu

### Architectural Principles

1. **Clean Architecture**: Domain-centric design with clear separation of concerns
2. **DDD Bounded Contexts**: Each microservice owns its domain and data
3. **Zero Trust Security**: Every request is authenticated and authorized
4. **Stateless Services**: All state externalized to Redis/PostgreSQL
5. **Event-Driven**: Async operations via RabbitMQ with Outbox pattern

### System Overview

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

### Bounded Contexts

| Service | Responsibility | Database Schema |
|---------|---------------|-----------------|
| IAM | Authentication, Authorization, Sessions, OTPT | iam_schema |
| Application | Case Management, Workflow, Tasks, Attachments | application_schema |
| Payment | Payment Intent, Transactions, Bank Callbacks | payment_schema |
| Ledger | Immutable Financial Records, Reversals | ledger_schema |
| Audit | Immutable Audit Events, Hash Chain | audit_schema |
| Document | PDF Generation, Templates, Archives | document_schema |
| Notification | SMS/Email Delivery, Retry Queue | notification_schema |
| Reference Data | Static Lookups, Caching | reference_schema |

### Communication Patterns

1. **Synchronous (REST/gRPC)**: Query operations, validation
2. **Asynchronous (RabbitMQ)**: Commands with side effects, notifications
3. **Outbox Pattern**: Reliable event publishing with transactional guarantees

### Security Layers

1. **Network**: TLS 1.2+, mTLS optional, IP allowlisting
2. **Authentication**: JWT tokens, MFA (TOTP), Session binding
3. **Authorization**: RBAC + ABAC policy engine
4. **Data**: Column-level encryption for PII, no PAN/CVV storage
5. **Audit**: Immutable logs with hash chain verification

### Data Immutability

- **Ledger Entries**: Never updated/deleted, corrections via ReversalEntry
- **Audit Events**: Append-only with hash chain (Hn = SHA-256(RecordCanonical + Hn-1))
- **Hash Verification**: Background job validates chain integrity
- **Breach Response**: Read-only mode + CRITICAL alert on chain break

### KPI Targets

| Metric | Target |
|--------|--------|
| P95 API Latency | < 300ms |
| Payment Failure Rate | < 1% (excluding bank issues) |
| RTO | < 2 hours |
| RPO | < 15 minutes |

### Observability Stack

- **Logs**: Structured JSON logs with correlation IDs
- **Metrics**: Prometheus endpoints per service
- **Traces**: OpenTelemetry distributed tracing
- **Alerts**: SIEM integration for security events
