#!/bin/bash
# Bash script to run OWASP ZAP security scan
# Prerequisites:
# - OWASP ZAP installed
# - Service running on localhost:8080
# - Valid JWT token for authentication

set -e

# Configuration
ZAP_PATH=${ZAP_PATH:-"/opt/zaproxy/zap.sh"}
TARGET_URL=${TARGET_URL:-"http://localhost:8080/employeeservice"}
JWT_TOKEN=${JWT_TOKEN:-""}
REPORT_DIR="./security-reports"
QUICK_SCAN=${QUICK_SCAN:-false}

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

echo -e "${CYAN}===================================${NC}"
echo -e "${CYAN}OWASP ZAP Security Scan${NC}"
echo -e "${CYAN}===================================${NC}"
echo ""

# Validate ZAP installation
if [ ! -f "$ZAP_PATH" ]; then
    echo -e "${RED}Error: OWASP ZAP not found at $ZAP_PATH${NC}"
    echo "Please install OWASP ZAP from https://www.zaproxy.org/download/"
    exit 1
fi

# Create report directory
mkdir -p "$REPORT_DIR"
echo -e "${GREEN}Report directory ready: $REPORT_DIR${NC}"

# Check if service is running
if ! curl -f -s -o /dev/null "$TARGET_URL/liveness"; then
    echo -e "${RED}Error: Service is not accessible at $TARGET_URL${NC}"
    echo "Please ensure the Employee Service is running"
    exit 1
fi
echo -e "${GREEN}Service is running and accessible${NC}"

# Validate JWT token
if [ -z "$JWT_TOKEN" ]; then
    echo -e "${YELLOW}Warning: No JWT token provided. Some endpoints may not be accessible.${NC}"
    echo "Set JWT_TOKEN environment variable for complete testing"
fi

# Generate timestamp
TIMESTAMP=$(date +%Y%m%d-%H%M%S)

echo ""
echo -e "${CYAN}Scan Configuration:${NC}"
echo "  Target URL: $TARGET_URL"
echo "  Report Directory: $REPORT_DIR"
echo "  Scan Mode: $([ "$QUICK_SCAN" = true ] && echo "Quick" || echo "Full")"
echo "  Timestamp: $TIMESTAMP"
echo ""

export JWT_TOKEN
export TARGET_URL
export TIMESTAMP

if [ "$QUICK_SCAN" = true ]; then
    # Quick scan - spider + passive scan only
    echo -e "${YELLOW}Running Quick Scan (Spider + Passive Scan)...${NC}"

    "$ZAP_PATH" -cmd \
        -quickurl "$TARGET_URL" \
        -quickout "$REPORT_DIR/zap-quickscan-$TIMESTAMP.html" \
        -quickprogress

else
    # Full scan using automation framework
    echo -e "${YELLOW}Running Full Scan (Spider + Passive + Active Scan)...${NC}"

    "$ZAP_PATH" -cmd \
        -autorun "./owasp-zap-config.yaml" \
        -autogenmin "$REPORT_DIR/zap-report-min-$TIMESTAMP.html" \
        -autogenmax "$REPORT_DIR/zap-report-full-$TIMESTAMP.html" \
        -autogenxml "$REPORT_DIR/zap-report-$TIMESTAMP.xml" \
        -autogenmd "$REPORT_DIR/zap-report-$TIMESTAMP.md"
fi

echo ""
echo -e "${GREEN}===================================${NC}"
echo -e "${GREEN}Scan completed successfully!${NC}"
echo -e "${GREEN}===================================${NC}"
echo ""
echo "Reports generated in: $REPORT_DIR"
echo "  - HTML Report: zap-report-$TIMESTAMP.html"
echo "  - JSON Report: zap-report-$TIMESTAMP.json"
echo "  - XML Report: zap-report-$TIMESTAMP.xml"
echo "  - Markdown Report: zap-report-$TIMESTAMP.md"
echo ""
echo -e "${YELLOW}Next Steps:${NC}"
echo "  1. Review the HTML report for detailed findings"
echo "  2. Address High and Medium severity issues"
echo "  3. Re-run scan to verify fixes"
echo ""
