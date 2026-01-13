# Threat Model

## System Context

This threat model covers the "Bütünleşik Kurumsal Ödeme ve Başvuru Yönetim Platformu" - an integrated enterprise payment and application management platform for government institutions.

## Assets

### Critical Assets
1. **Payment Transactions**: Financial operations and records
2. **User Credentials**: Passwords, MFA secrets
3. **Session Tokens**: Access and refresh tokens
4. **Audit Logs**: Immutable compliance records
5. **PII Data**: Citizen personal information
6. **Cryptographic Keys**: Encryption and signing keys

### Important Assets
1. Application case data
2. Document templates and archives
3. System configuration
4. Service-to-service credentials

## Threat Actors

| Actor | Capability | Motivation |
|-------|-----------|------------|
| External Attacker | Medium-High | Financial gain, data theft |
| Insider (Malicious) | High | Fraud, sabotage |
| Insider (Negligent) | Low | Accidental exposure |
| Nation State | Very High | Espionage, disruption |
| Automated Bots | Low-Medium | Credential stuffing, DDoS |

## STRIDE Analysis

### Spoofing (Identity)

| Threat | Mitigation | Status |
|--------|-----------|--------|
| Credential theft | Argon2id hashing, MFA | ✅ Implemented |
| Session hijacking | Fingerprint binding, secure cookies | ✅ Implemented |
| Token replay | Short-lived tokens, OTPT | ✅ Implemented |
| Phishing | User education, domain monitoring | ⚠️ External |

### Tampering

| Threat | Mitigation | Status |
|--------|-----------|--------|
| Audit log modification | Hash chain, WORM storage | ✅ Implemented |
| Payment callback tampering | Signature verification, nonce | ✅ Implemented |
| Request modification | TLS, input validation | ✅ Implemented |
| Database tampering | Immutable patterns, checksums | ✅ Implemented |

### Repudiation

| Threat | Mitigation | Status |
|--------|-----------|--------|
| Transaction denial | Immutable ledger, audit trail | ✅ Implemented |
| Action denial | Comprehensive logging, correlation IDs | ✅ Implemented |
| Timestamp manipulation | Server-side timestamps, NTP sync | ✅ Implemented |

### Information Disclosure

| Threat | Mitigation | Status |
|--------|-----------|--------|
| PII exposure | Column encryption, access control | ✅ Implemented |
| Credential leakage | No browser storage, RAM-only | ✅ Implemented |
| Log leakage | Log sanitization, access control | ✅ Implemented |
| Error message disclosure | Generic errors, problem+json | ✅ Implemented |

### Denial of Service

| Threat | Mitigation | Status |
|--------|-----------|--------|
| API flooding | Rate limiting, circuit breakers | ✅ Implemented |
| Resource exhaustion | Connection limits, timeouts | ✅ Implemented |
| Storage filling | Quotas, monitoring | ✅ Implemented |
| Hash chain computation | Background job, caching | ✅ Implemented |

### Elevation of Privilege

| Threat | Mitigation | Status |
|--------|-----------|--------|
| Role manipulation | Server-side RBAC, signed tokens | ✅ Implemented |
| Policy bypass | Zero Trust, every-request auth | ✅ Implemented |
| Admin compromise | PAM, JIT elevation, ephemeral tokens | ✅ Implemented |
| SQL injection | Parameterized queries, ORM | ✅ Implemented |

## Attack Trees

### Payment Fraud Attack Tree

```
Payment Fraud [ROOT]
├── Bypass Authentication
│   ├── Steal credentials (mitigated: MFA)
│   ├── Session hijack (mitigated: fingerprint binding)
│   └── Token replay (mitigated: OTPT)
├── Manipulate Payment
│   ├── Tamper callback (mitigated: signature + nonce)
│   ├── Replay callback (mitigated: idempotency key)
│   └── Amount manipulation (mitigated: server-side validation)
├── Tamper Records
│   ├── Modify ledger (mitigated: immutable + hash chain)
│   └── Delete audit (mitigated: append-only + WORM)
└── Insider Fraud
    ├── Unauthorized access (mitigated: RBAC + audit)
    └── Privilege abuse (mitigated: PAM + JIT)
```

### Audit Integrity Attack Tree

```
Audit Tampering [ROOT]
├── Direct Modification
│   ├── DB access (mitigated: access control + hash chain)
│   └── Application bypass (mitigated: immutable pattern)
├── Hash Chain Attack
│   ├── Recalculate chain (mitigated: genesis binding)
│   └── Parallel chain (mitigated: single source of truth)
└── Deletion
    ├── Soft delete (mitigated: no delete operations)
    └── Physical delete (mitigated: WORM backup)
```

## Security Controls Matrix

| Control | Threat Mitigated | Layer |
|---------|-----------------|-------|
| TLS 1.2+ | MITM, eavesdropping | Network |
| Rate limiting | DoS, brute force | Gateway |
| JWT validation | Spoofing | Application |
| MFA | Credential theft | Authentication |
| Session binding | Session hijacking | Session |
| OTPT | Replay, CSRF | Transaction |
| RBAC + ABAC | Privilege escalation | Authorization |
| Input validation | Injection attacks | Application |
| Hash chain | Tampering, repudiation | Data |
| Encryption | Information disclosure | Data |
| Audit logging | Repudiation | Monitoring |

## Risk Assessment

| Risk | Likelihood | Impact | Score | Status |
|------|-----------|--------|-------|--------|
| Payment fraud | Medium | Critical | High | Mitigated |
| Data breach | Medium | Critical | High | Mitigated |
| Audit tampering | Low | Critical | Medium | Mitigated |
| Service disruption | Medium | High | Medium | Mitigated |
| Insider threat | Low | High | Medium | Mitigated |

## Residual Risks

1. **Zero-day vulnerabilities**: Dependency monitoring, rapid patching
2. **Social engineering**: User training, awareness programs
3. **Physical security**: Out of scope (infrastructure responsibility)
4. **Supply chain attacks**: Dependency scanning, SBOM generation

## Review Schedule

- Quarterly threat model review
- After significant changes
- Following security incidents
- Annual penetration testing
