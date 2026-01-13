# Operations Runbook

## Quick Reference

### Service Ports

| Service | HTTP Port | gRPC Port | Health Endpoint |
|---------|-----------|-----------|-----------------|
| Gateway | 80/443 | - | /health |
| IAM | 5001 | 5051 | /health |
| Application | 5002 | 5052 | /health |
| Payment | 5003 | 5053 | /health |
| Ledger | 5004 | 5054 | /health |
| Audit | 5005 | 5055 | /health |
| Document | 5006 | 5056 | /health |
| Notification | 5007 | 5057 | /health |
| Reference Data | 5008 | 5058 | /health |

### Infrastructure

| Component | Port | Purpose |
|-----------|------|---------|
| PostgreSQL | 5432 | Primary database |
| Redis | 6379 | Cache, sessions, OTPT |
| RabbitMQ | 5672/15672 | Message queue |
| Jaeger | 16686 | Tracing UI |
| Prometheus | 9090 | Metrics |

## Startup Procedures

### Local Development

```bash
# Start infrastructure
docker-compose -f infra/docker-compose.yml up -d

# Run migrations
dotnet ef database update --project src/services/iam

# Start services
dotnet run --project src/services/iam
dotnet run --project src/services/application
# ... etc

# Start frontend
cd src/frontend && npm run dev
```

### Production Deployment

```bash
# Deploy to Kubernetes
kubectl apply -f infra/k8s/namespace.yaml
kubectl apply -f infra/k8s/secrets.yaml
kubectl apply -f infra/k8s/configmaps.yaml
kubectl apply -f infra/k8s/deployments/
kubectl apply -f infra/k8s/services/
```

## Health Checks

### Service Health

```bash
# Check all services
for port in 5001 5002 5003 5004 5005 5006 5007 5008; do
  curl -s http://localhost:$port/health | jq .
done
```

### Database Connectivity

```bash
# PostgreSQL
psql -h localhost -U platform -d platform_db -c "SELECT 1"

# Redis
redis-cli ping
```

### Message Queue

```bash
# RabbitMQ
curl -u admin:admin http://localhost:15672/api/healthchecks/node
```

## Common Issues

### Issue: Hash Chain Verification Failed

**Symptoms:**
- CRITICAL alert raised
- Service enters read-only mode
- Audit/Ledger operations return 503

**Investigation:**
```bash
# Check last valid hash
curl http://localhost:5005/api/audit/chain/status

# View verification logs
docker logs audit-service | grep "HashChain"
```

**Resolution:**
1. Do NOT attempt to "fix" the hash chain
2. Investigate the cause (data tampering, bug, etc.)
3. Restore from last known good backup
4. Forensic analysis required

### Issue: OTPT Validation Failing

**Symptoms:**
- Users cannot complete critical operations
- 401/403 errors on payment/approval endpoints

**Investigation:**
```bash
# Check Redis connectivity
redis-cli ping

# Check OTPT keys
redis-cli keys "otpt:*"

# Check time synchronization
ntpq -p
```

**Resolution:**
1. Verify Redis is accessible
2. Check clock synchronization (OTPT is time-sensitive)
3. Verify session binding (fingerprint mismatch?)

### Issue: Payment Callback Verification Failed

**Symptoms:**
- Payments stuck in PENDING state
- Callback signature validation errors

**Investigation:**
```bash
# Check callback logs
docker logs payment-service | grep "callback"

# Verify bank public key
curl http://localhost:5003/api/payment/bank/key-status
```

**Resolution:**
1. Verify bank public key is current
2. Check timestamp window (callbacks must arrive within 5 minutes)
3. Contact bank if signature consistently fails

### Issue: Service Not Starting

**Symptoms:**
- Container exits immediately
- Health check fails

**Investigation:**
```bash
# Check logs
docker logs <service-name>

# Check resource limits
docker stats <service-name>

# Check dependencies
docker-compose ps
```

**Resolution:**
1. Check database migrations
2. Verify environment variables
3. Check dependent services are running

## Backup Procedures

### Database Backup

```bash
# Full backup
pg_dump -h localhost -U platform platform_db > backup_$(date +%Y%m%d).sql

# Specific schema
pg_dump -h localhost -U platform -n iam_schema platform_db > iam_backup.sql
```

### Redis Backup

```bash
# Trigger RDB snapshot
redis-cli BGSAVE

# Copy RDB file
cp /var/lib/redis/dump.rdb /backup/redis_$(date +%Y%m%d).rdb
```

## Restore Procedures

### Database Restore

```bash
# Restore from backup
psql -h localhost -U platform platform_db < backup_20240101.sql

# Run migrations after restore
dotnet ef database update --project src/services/iam
```

### Recovery Time Objectives

| Component | RTO | RPO |
|-----------|-----|-----|
| Database | 2 hours | 15 minutes |
| Application | 30 minutes | N/A (stateless) |
| Redis | 15 minutes | 1 hour |

## Monitoring & Alerts

### Key Metrics

| Metric | Warning | Critical |
|--------|---------|----------|
| API Latency P95 | > 250ms | > 500ms |
| Error Rate | > 1% | > 5% |
| CPU Usage | > 70% | > 90% |
| Memory Usage | > 80% | > 95% |
| Disk Usage | > 70% | > 90% |

### Alert Response

1. **Info**: Log for review
2. **Warning**: Investigate within 4 hours
3. **Critical**: Immediate response required
4. **Hash Chain Break**: CRITICAL - potential security incident

## Security Incident Response

### Level 1: Suspected Breach

1. Preserve logs
2. Notify security team
3. Do not make changes

### Level 2: Confirmed Breach

1. Isolate affected systems
2. Revoke compromised credentials
3. Enable read-only mode if data integrity affected
4. Forensic analysis

### Level 3: Data Exfiltration

1. All Level 2 steps
2. Legal/compliance notification
3. Customer notification (if required)
4. Regulatory reporting

## Maintenance Windows

### Scheduled Maintenance

- **Time**: Sunday 02:00-06:00 UTC
- **Notification**: 72 hours advance
- **Duration**: 4 hours maximum

### Emergency Maintenance

- **Authorization**: Security team or on-call lead
- **Notification**: Immediate
- **Documentation**: Post-incident report required

## Contacts

| Role | Contact |
|------|---------|
| On-Call Engineer | oncall@example.gov.tr |
| Security Team | security@example.gov.tr |
| DBA | dba@example.gov.tr |
| Vendor Support | support@bankvendor.com |
