#!/bin/bash
# bump-version.sh - Automate version number updates for GMan
# Usage: ./tools/bump-version.sh [major|minor|build]

set -e

CSPROJ="gman.csproj"
HELP_FILE="ui/help.txt"
CHANGELOG="doc/CHANGELOG.md"

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Check if we're in the right directory
if [ ! -f "$CSPROJ" ]; then
    echo -e "${RED}Error: $CSPROJ not found. Run this script from the project root.${NC}"
    exit 1
fi

# Check for required argument
if [ $# -ne 1 ]; then
    echo "Usage: $0 [major|minor|build]"
    echo ""
    echo "Examples:"
    echo "  $0 build   # 2.0.1 -> 2.0.2"
    echo "  $0 minor   # 2.0.15 -> 2.1.0"
    echo "  $0 major   # 2.0.15 -> 3.0.0"
    exit 1
fi

BUMP_TYPE="$1"

# Validate argument
if [[ ! "$BUMP_TYPE" =~ ^(major|minor|build)$ ]]; then
    echo -e "${RED}Error: Invalid argument '$BUMP_TYPE'. Use: major, minor, or build${NC}"
    exit 1
fi

# Extract current version from gman.csproj
CURRENT_PREFIX=$(grep -oP '<VersionPrefix>\K[^<]+' "$CSPROJ")
CURRENT_BUILD=$(grep -oP '<BuildNumber>\K[^<]+' "$CSPROJ")

if [ -z "$CURRENT_PREFIX" ] || [ -z "$CURRENT_BUILD" ]; then
    echo -e "${RED}Error: Could not parse version from $CSPROJ${NC}"
    echo "Expected <VersionPrefix> and <BuildNumber> tags"
    exit 1
fi

# Split version prefix into major.minor
IFS='.' read -r MAJOR MINOR <<< "$CURRENT_PREFIX"

CURRENT_VERSION="${CURRENT_PREFIX}.${CURRENT_BUILD}"
echo -e "${GREEN}Current version: $CURRENT_VERSION${NC}"

# Calculate new version based on bump type
case "$BUMP_TYPE" in
    major)
        MAJOR=$((MAJOR + 1))
        MINOR=0
        BUILD=0
        ;;
    minor)
        MINOR=$((MINOR + 1))
        BUILD=0
        ;;
    build)
        BUILD=$((CURRENT_BUILD + 1))
        ;;
esac

NEW_PREFIX="${MAJOR}.${MINOR}"
NEW_VERSION="${NEW_PREFIX}.${BUILD}"

echo -e "${GREEN}New version: $NEW_VERSION${NC}"
echo ""

# Confirm with user
read -p "Proceed with version update? (y/n): " -n 1 -r
echo ""
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "Aborted."
    exit 0
fi

# Update gman.csproj
echo "Updating $CSPROJ..."

# Use sed to update VersionPrefix and BuildNumber
sed -i "s|<VersionPrefix>.*</VersionPrefix>|<VersionPrefix>${NEW_PREFIX}</VersionPrefix>|" "$CSPROJ"
sed -i "s|<BuildNumber>.*</BuildNumber>|<BuildNumber>${BUILD}</BuildNumber>|" "$CSPROJ"

echo -e "${GREEN}✓ Updated $CSPROJ${NC}"
echo ""

# Reminder for manual updates
echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${YELLOW}MANUAL UPDATES REQUIRED:${NC}"
echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""
echo -e "1. Update ${YELLOW}$HELP_FILE${NC} VERSION section:"
echo -e "   ${GREEN}GMan $NEW_VERSION - $(date '+%B %Y')${NC}"
echo ""
echo -e "2. Update ${YELLOW}$CHANGELOG${NC}:"
echo -e "   Move ${GREEN}[Unreleased]${NC} section to ${GREEN}[$NEW_VERSION] - $(date '+%Y-%m-%d')${NC}"
echo ""
echo -e "3. Build release binary:"
echo -e "   ${GREEN}dotnet build -c Release${NC}"
echo ""
echo -e "4. Test the build:"
echo -e "   ${GREEN}./bin/Release/net8.0/gman${NC}"
echo -e "   Check Help → About dialog for version"
echo ""
echo -e "5. Commit and tag:"
echo -e "   ${GREEN}git add $CSPROJ $HELP_FILE $CHANGELOG${NC}"
echo -e "   ${GREEN}git commit -m \"Release version $NEW_VERSION\"${NC}"
echo -e "   ${GREEN}git tag v$NEW_VERSION${NC}"
echo -e "   ${GREEN}git push origin main --tags${NC}"
echo ""
echo -e "6. Create GitHub release:"
echo -e "   Tag: ${GREEN}v$NEW_VERSION${NC}"
echo -e "   Upload: ${GREEN}bin/Release/net8.0/gman${NC} and UI files"
echo ""
echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
