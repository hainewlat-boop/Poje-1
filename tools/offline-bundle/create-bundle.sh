#!/bin/bash
# Offline Bundle Creation Script
# Creates a signed bundle for air-gapped deployments

set -e

BUNDLE_DIR="./bundle-$(date +%Y%m%d-%H%M%S)"
PRIVATE_KEY=${SIGNING_KEY:-"./keys/signing-key.pem"}

echo "================================================"
echo "Creating Offline Deployment Bundle"
echo "================================================"
echo ""

# Create bundle directory
mkdir -p "$BUNDLE_DIR"/{images,manifests,checksums}

echo "1. Exporting container images..."
services=("iam" "application" "payment" "ledger" "audit" "document" "notification" "reference-data" "frontend")

for service in "${services[@]}"; do
    echo "   - platform-$service"
    docker save "platform-$service:latest" -o "$BUNDLE_DIR/images/$service.tar" 2>/dev/null || echo "     (image not found, skipping)"
done

echo ""
echo "2. Copying Kubernetes manifests..."
cp -r ../../infra/k8s/* "$BUNDLE_DIR/manifests/" 2>/dev/null || echo "   (manifests not found)"

echo ""
echo "3. Generating checksums..."
cd "$BUNDLE_DIR"

# Generate SHA-256 checksums for all files
find . -type f ! -name "*.sha256" ! -name "*.sig" -exec sha256sum {} \; > checksums/files.sha256

# Generate manifest checksum
sha256sum checksums/files.sha256 > checksums/manifest.sha256

echo ""
echo "4. Signing the bundle..."
if [ -f "$PRIVATE_KEY" ]; then
    openssl dgst -sha256 -sign "$PRIVATE_KEY" -out checksums/manifest.sig checksums/manifest.sha256
    echo "   Bundle signed successfully"
else
    echo "   ⚠️  Signing key not found. Bundle is NOT signed."
    echo "   For production, set SIGNING_KEY environment variable"
fi

echo ""
echo "5. Creating archive..."
cd ..
tar -czf "${BUNDLE_DIR}.tar.gz" "$BUNDLE_DIR"

echo ""
echo "================================================"
echo "Bundle created: ${BUNDLE_DIR}.tar.gz"
echo ""
echo "Contents:"
ls -lh "${BUNDLE_DIR}.tar.gz"
echo ""
echo "To deploy:"
echo "  1. Transfer bundle to air-gapped environment"
echo "  2. Run: ./verify-signatures.sh ${BUNDLE_DIR}.tar.gz"
echo "  3. Run: ./deploy.sh ${BUNDLE_DIR}"
echo "================================================"
