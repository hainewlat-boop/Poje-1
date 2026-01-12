# Security Documentation

## Security Architecture

### Non-Negotiable Security Requirements

1. **No Card Data Storage**: PAN/CVV never stored; payments via bank virtual POS (redirect/iframe + callback)
2. **No Browser Storage for Sensitive Data**: No localStorage/sessionStorage; RAM-only (Zustand store)
3. **Immutable Records**: Critical records never deleted; corrections via reversal entries
4. **Hash Chain Audit**: Append-only audit log with cryptographic hash chain
5. **OTPT for Critical Operations**: One-Time-Per-Transaction tokens with 60s TTL

### Authentication

#### Password Security
- **Hashing**: Argon2id (primary), bcrypt (fallback)
- **Argon2id Parameters**: 
  - Memory: 64MB
  - Iterations: 3
  - Parallelism: 4
  - Salt: 16 bytes (cryptographically random)

#### Multi-Factor Authentication (MFA)
- **Method**: TOTP (Time-based One-Time Password)
- **Algorithm**: HMAC-SHA1
- **Digits**: 6
- **Period**: 30 seconds
- **Window**: ±1 step tolerance

#### Session Management
- **Access Token**: JWT, 15-minute expiry
- **Refresh Token**: Opaque, 7-day expiry, stored in Redis
- **Session Binding**: FingerprintID + SessionID
- **Mismatch Handling**: Immediate session revocation

### Progressive Lockout

| Failed Attempts | Lockout Duration |
|-----------------|------------------|
| 3 | 1 minute |
| 5 | 5 minutes |
| 7 | 15 minutes |
| 10+ | Account locked (admin intervention) |

### OTPT (One-Time-Per-Transaction Token)

```
┌─────────────────────────────────────────────────────────────┐
│                    OTPT Flow                                 │
├─────────────────────────────────────────────────────────────┤
│ 1. Client requests OTPT for critical operation              │
│ 2. Server validates policy (may require step-up MFA)        │
│ 3. Server generates OTPT:                                   │
│    - Token: Cryptographically random 32 bytes               │
│    - Bound to: route + nonce + sessionId + fingerprintId    │
│    - TTL: 60 seconds                                        │
│    - Storage: Redis with write-once semantics               │
│ 4. Client includes OTPT in critical request                 │
│ 5. Server atomically consumes OTPT (Lua script)             │
│ 6. OTPT cannot be reused                                    │
└─────────────────────────────────────────────────────────────┘
```

### Authorization

#### RBAC + ABAC Policy Engine

```json
{
  "policy": {
    "resource": "payment:intent:create",
    "effect": "allow",
    "conditions": {
      "roles": ["payment_initiator", "admin"],
      "attributes": {
        "risk_score": { "lte": 50 },
        "network": { "in": ["internal", "vpn"] },
        "time_window": { "between": ["08:00", "18:00"] },
        "mfa_verified": true,
        "otpt_required": true
      }
    }
  }
}
```

#### Standard Error Responses

| Code | Meaning |
|------|---------|
| 401 | Unauthorized - Authentication required |
| 403 | Forbidden - Insufficient permissions |
| 409 | Conflict - Concurrent modification |
| 423 | Locked - Resource locked (fraud/risk) |

### Data Protection

#### PII Encryption
- **Algorithm**: AES-256-GCM (envelope encryption)
- **Key Management**: KMS/HSM adapter (file-based for dev)
- **Encrypted Fields**: National ID, Phone, Email, Address

#### Secrets Management
- Environment variables for runtime secrets
- No plaintext secrets in repository
- Secret rotation support

### Hash Chain Implementation

```
Genesis Block:
  H0 = SHA-256(ENV_ID + VERSION + TIMESTAMP)

Subsequent Blocks:
  Hn = SHA-256(CanonicalJSON(Record) + Hn-1)

Canonical JSON Rules:
  - Keys sorted alphabetically
  - No whitespace
  - Dates in ISO 8601 UTC
  - Numbers without trailing zeros
  - Null values excluded
```

### Security Headers

```nginx
# HTTPS enforcement
add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;

# Content Security Policy
add_header Content-Security-Policy "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; connect-src 'self'; frame-ancestors 'none';" always;

# Prevent clickjacking
add_header X-Frame-Options "DENY" always;

# Prevent MIME type sniffing
add_header X-Content-Type-Options "nosniff" always;

# XSS Protection
add_header X-XSS-Protection "1; mode=block" always;

# Referrer Policy
add_header Referrer-Policy "strict-origin-when-cross-origin" always;
```

### File Upload Security

1. **MIME Type Whitelist**: PDF, PNG, JPG, JPEG only
2. **Size Limit**: 10MB per file
3. **Virus Scan**: Hook for AV integration (stubbed in dev)
4. **Content Validation**: Magic bytes verification
5. **Secure Storage**: Hashed filenames, access-controlled

### Rate Limiting

| Endpoint Type | Limit |
|---------------|-------|
| Login | 5/minute per IP |
| API General | 100/minute per user |
| Payment Operations | 10/minute per user |
| File Upload | 5/minute per user |

### Compliance Checklist

- [ ] TLS 1.2+ for all connections
- [ ] No PAN/CVV storage
- [ ] PII encryption at rest
- [ ] Immutable audit logs
- [ ] Hash chain verification enabled
- [ ] MFA enforced for privileged operations
- [ ] Session binding validated
- [ ] OTPT for critical transactions
- [ ] Security headers configured
- [ ] Rate limiting enabled
- [ ] Input validation on all endpoints
- [ ] Output encoding for XSS prevention
