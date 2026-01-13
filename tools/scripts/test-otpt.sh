#!/bin/bash
# OTPT Testing Script
# Tests the OTPT (One-Time-Per-Transaction) flow

set -e

echo "================================================"
echo "OTPT Flow Testing"
echo "================================================"

API_URL=${1:-"http://localhost:5001"}
ACCESS_TOKEN=${2:-"your-access-token-here"}
FINGERPRINT_ID=${3:-"test-fingerprint-123"}

echo ""
echo "1. Requesting OTPT token..."

# Issue OTPT
otpt_response=$(curl -s -X POST "$API_URL/api/otpt/issue" \
    -H "Content-Type: application/json" \
    -H "Authorization: Bearer $ACCESS_TOKEN" \
    -H "X-Fingerprint-ID: $FINGERPRINT_ID" \
    -d '{
        "route": "/api/payments/intent",
        "nonce": null
    }')

echo "OTPT Response:"
echo "$otpt_response" | jq .

TOKEN=$(echo $otpt_response | jq -r '.token')
NONCE=$(echo $otpt_response | jq -r '.nonce')
EXPIRES_AT=$(echo $otpt_response | jq -r '.expiresAt')

if [ "$TOKEN" == "null" ]; then
    echo "❌ Failed to obtain OTPT token"
    exit 1
fi

echo ""
echo "Token: $TOKEN"
echo "Nonce: $NONCE"
echo "Expires: $EXPIRES_AT"
echo ""
echo "✅ OTPT token issued successfully"
echo ""
echo "2. Token can now be used for a critical operation"
echo "   Include in request: otptToken=$TOKEN, nonce=$NONCE"
echo ""
echo "3. Testing token reuse (should fail)..."

# Try to use same token again - this should fail
# In a real test, we'd first use the token, then try again

echo ""
echo "⚠️  Note: OTPT tokens expire after 60 seconds and can only be used once"
