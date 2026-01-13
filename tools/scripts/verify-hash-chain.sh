#!/bin/bash
# Hash Chain Verification Script
# Verifies the integrity of audit and ledger hash chains

set -e

echo "================================================"
echo "Hash Chain Verification Tool"
echo "================================================"

SERVICE=${1:-"audit"}
API_URL=${2:-"http://localhost:5005"}

if [ "$SERVICE" == "ledger" ]; then
    API_URL=${2:-"http://localhost:5004"}
fi

echo ""
echo "Verifying $SERVICE hash chain..."
echo "API URL: $API_URL"
echo ""

# Get chain status
response=$(curl -s "$API_URL/api/$SERVICE/chain/status")

is_valid=$(echo $response | jq -r '.isValid')
total_records=$(echo $response | jq -r '.totalRecords')
last_verified=$(echo $response | jq -r '.lastVerifiedAt')

echo "Chain Status:"
echo "  Valid: $is_valid"
echo "  Total Records: $total_records"
echo "  Last Verified: $last_verified"

if [ "$is_valid" == "true" ]; then
    echo ""
    echo "✅ Hash chain is VALID"
    exit 0
else
    first_invalid=$(echo $response | jq -r '.firstInvalidSequence')
    error_message=$(echo $response | jq -r '.errorMessage')
    
    echo ""
    echo "❌ Hash chain is BROKEN!"
    echo "  First Invalid Sequence: $first_invalid"
    echo "  Error: $error_message"
    echo ""
    echo "⚠️  CRITICAL: Investigate immediately!"
    echo "⚠️  System may have entered read-only mode"
    exit 1
fi
