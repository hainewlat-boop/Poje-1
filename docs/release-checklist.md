# Production Release Checklist

## Pre-Release Verification

### Security
- [ ] All high/critical vulnerabilities addressed or documented
- [ ] Dependency scan completed (no new critical CVEs)
- [ ] Secret scan passed (no secrets in codebase)
- [ ] CodeQL analysis passed
- [ ] Penetration test completed (if applicable)

### Testing
- [ ] Unit test coverage > 80% for security/domain code
- [ ] All integration tests passing
- [ ] E2E tests passing:
  - [ ] Login flow (with MFA)
  - [ ] Application creation → submission
  - [ ] Payment flow (intent → callback)
  - [ ] Document generation
  - [ ] Audit log access
- [ ] Performance tests meet KPIs:
  - [ ] P95 API latency < 300ms
  - [ ] Payment failure rate < 1%

### Security Features
- [ ] OTPT flow verified:
  - [ ] Token issued correctly
  - [ ] Token consumed atomically
  - [ ] Replay rejected
- [ ] Hash chain verified:
  - [ ] Audit chain intact
  - [ ] Ledger chain intact
  - [ ] Verification job running
- [ ] Session binding verified:
  - [ ] Fingerprint mismatch triggers revocation
  - [ ] Refresh with wrong fingerprint fails
- [ ] Rate limiting verified:
  - [ ] Login rate limit enforced
  - [ ] API rate limit enforced
- [ ] MFA verified:
  - [ ] TOTP enrollment works
  - [ ] TOTP verification works
  - [ ] MFA bypass not possible

### Infrastructure
- [ ] TLS certificates valid and configured
- [ ] Database backups configured
- [ ] Monitoring alerts configured:
  - [ ] Hash chain break alert
  - [ ] High error rate alert
  - [ ] High latency alert
  - [ ] Security event alert
- [ ] Log aggregation configured
- [ ] Metrics collection active

### Configuration
- [ ] All secrets in secret manager (not env vars)
- [ ] Database connection strings secure
- [ ] JWT secret rotated
- [ ] Master encryption key secured
- [ ] Redis password set
- [ ] RabbitMQ password set

## Deployment Steps

### 1. Pre-Deployment
```bash
# Verify current state
kubectl get pods -n platform
kubectl get pvc -n platform

# Backup database
pg_dump -h <host> -U platform platform_db > backup_$(date +%Y%m%d).sql

# Verify backup
pg_restore --list backup_$(date +%Y%m%d).sql | head
```

### 2. Deploy
```bash
# Apply migrations first
kubectl apply -f k8s/jobs/migration.yaml
kubectl wait --for=condition=complete job/migration --timeout=300s

# Deploy services
kubectl apply -f k8s/deployments/

# Wait for rollout
kubectl rollout status deployment/iam-service -n platform
kubectl rollout status deployment/payment-service -n platform
# ... etc
```

### 3. Post-Deployment Verification
```bash
# Health checks
for svc in iam application payment ledger audit document notification; do
  curl -f https://api.platform.gov.tr/$svc/health
done

# Hash chain verification
./tools/scripts/verify-hash-chain.sh audit
./tools/scripts/verify-hash-chain.sh ledger

# Smoke tests
./tools/scripts/smoke-test.sh
```

### 4. Rollback (if needed)
```bash
# Quick rollback
kubectl rollout undo deployment/iam-service -n platform

# Full rollback with DB
kubectl apply -f k8s/deployments/previous-version/
pg_restore -h <host> -U platform -d platform_db backup_$(date +%Y%m%d).sql
```

## Post-Release

### Immediate (Day 1)
- [ ] Monitor error rates
- [ ] Monitor latency
- [ ] Monitor security events
- [ ] Verify hash chain integrity
- [ ] Check audit logs for anomalies

### Short-term (Week 1)
- [ ] Review performance metrics
- [ ] Address any issues from monitoring
- [ ] Collect user feedback
- [ ] Update documentation if needed

### Long-term
- [ ] Schedule next security review
- [ ] Plan for next release
- [ ] Update threat model if needed

## Sign-off

| Role | Name | Date | Signature |
|------|------|------|-----------|
| Development Lead | | | |
| Security Lead | | | |
| Operations Lead | | | |
| Product Owner | | | |

## Emergency Contacts

| Role | Contact |
|------|---------|
| On-Call Engineer | oncall@platform.gov.tr |
| Security Team | security@platform.gov.tr |
| DBA | dba@platform.gov.tr |
| Management Escalation | management@platform.gov.tr |

---

**Version**: 1.0  
**Last Updated**: 2024-01-15  
**Next Review**: Quarterly
