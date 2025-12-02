#!/bin/bash
set -euo pipefail

# Ignored vulnerabilities (pipe-separated)
# GO-2025-4155: Go 1.25.5 not yet available in GitHub Actions setup-go
IGNORED_VULNS="GO-2025-4155"

VULNS_FILE=$(mktemp)
trap "rm -f $VULNS_FILE" EXIT

echo "Running govulncheck..."
govulncheck -format json ./... > "$VULNS_FILE" || true

# Extract unique vulnerability IDs from streaming JSON (Finding messages have .finding.osv field)
FOUND=$(jq -r 'select(.finding) | .finding.osv' "$VULNS_FILE" 2>/dev/null | sort -u | grep -v -E "^($IGNORED_VULNS)$" || true)

if [ -n "$FOUND" ]; then
    echo "Vulnerabilities found:"
    echo "$FOUND"
    echo ""
    echo "Run 'govulncheck ./...' for details"
    exit 1
fi

echo "No actionable vulnerabilities found"
if [ -n "$IGNORED_VULNS" ]; then
    echo "Ignored: $IGNORED_VULNS"
fi
