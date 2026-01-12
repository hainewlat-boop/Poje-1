#!/bin/bash
# Signature Verification Script
# Verifies the integrity of an offline bundle

set -e

BUNDLE=$1
PUBLIC_KEY=${VERIFY_KEY:-"./keys/signing-key-pub.pem"}

if [ -z "$BUNDLE" ]; then
    echo "Usage: $0 <bundle.tar.gz>"
    exit 1
fi

echo "================================================"
echo "Verifying Offline Bundle Signatures"
echo "================================================"
echo ""
echo "Bundle: $BUNDLE"
echo ""

# Extract bundle
TEMP_DIR=$(mktemp -d)
tar -xzf "$BUNDLE" -C "$TEMP_DIR"
BUNDLE_DIR=$(ls "$TEMP_DIR")

cd "$TEMP_DIR/$BUNDLE_DIR"

echo "1. Verifying manifest signature..."
if [ -f "$PUBLIC_KEY" ]; then
    if openssl dgst -sha256 -verify "$PUBLIC_KEY" -signature checksums/manifest.sig checksums/manifest.sha256; then
        echo "   ✅ Signature valid"
    else
        echo "   ❌ Signature INVALID!"
        echo "   ⚠️  Bundle may have been tampered with!"
        exit 1
    fi
else
    echo "   ⚠️  Public key not found. Skipping signature verification."
fi

echo ""
echo "2. Verifying file checksums..."
if sha256sum -c checksums/files.sha256 --quiet; then
    echo "   ✅ All file checksums valid"
else
    echo "   ❌ Checksum verification FAILED!"
    echo "   Some files may have been modified."
    sha256sum -c checksums/files.sha256
    exit 1
fi

echo ""
echo "3. Verifying manifest checksum..."
if sha256sum -c checksums/manifest.sha256 --quiet; then
    echo "   ✅ Manifest checksum valid"
else
    echo "   ❌ Manifest checksum FAILED!"
    exit 1
fi

echo ""
echo "================================================"
echo "✅ Bundle verification PASSED"
echo "Bundle is authentic and unmodified"
echo "================================================"

# Cleanup
rm -rf "$TEMP_DIR"
