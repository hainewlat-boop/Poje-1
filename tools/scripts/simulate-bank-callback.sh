#!/bin/bash
# Bank Callback Simulation Script
# Simulates a bank virtual POS callback for testing

set -e

echo "================================================"
echo "Bank Callback Simulation"
echo "================================================"

PAYMENT_INTENT_ID=${1:-"test-intent-id"}
STATUS=${2:-"SUCCESS"}
AMOUNT=${3:-"100.00"}
API_URL=${4:-"http://localhost:5003"}

# Generate callback data
TRANSACTION_ID="TXN-$(date +%s)"
TIMESTAMP=$(date +%s)
NONCE=$(openssl rand -hex 16)

# In production, this would be signed by the bank's private key
# For testing, we use a placeholder
SIGNATURE="test-signature-$(echo -n "$TRANSACTION_ID$STATUS$AMOUNT$TIMESTAMP$NONCE" | sha256sum | cut -d' ' -f1)"

echo ""
echo "Callback Details:"
echo "  Payment Intent ID: $PAYMENT_INTENT_ID"
echo "  Transaction ID: $TRANSACTION_ID"
echo "  Status: $STATUS"
echo "  Amount: $AMOUNT TRY"
echo "  Timestamp: $TIMESTAMP"
echo "  Nonce: $NONCE"
echo ""

# Send callback
response=$(curl -s -X POST "$API_URL/api/payments/callback" \
    -H "Content-Type: application/json" \
    -d "{
        \"transactionId\": \"$TRANSACTION_ID\",
        \"status\": \"$STATUS\",
        \"amount\": $AMOUNT,
        \"currency\": \"TRY\",
        \"signature\": \"$SIGNATURE\",
        \"timestamp\": $TIMESTAMP,
        \"nonce\": \"$NONCE\",
        \"bankReferenceNumber\": \"BANK-REF-$TRANSACTION_ID\"
    }")

echo "Response:"
echo "$response" | jq .

success=$(echo $response | jq -r '.success')

if [ "$success" == "true" ]; then
    echo ""
    echo "✅ Callback processed successfully"
else
    echo ""
    echo "❌ Callback processing failed"
    exit 1
fi
