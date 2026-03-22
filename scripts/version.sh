#!/bin/bash
# Semantic versioning based on conventional commits.
# Analyzes commits since last tag and determines the next version.
#
# Bump rules:
#   MAJOR — commit subject contains "!" before ":" or body contains "BREAKING CHANGE"
#   MINOR — commit subject starts with "feat"
#   PATCH — commit subject starts with "fix"
#   SKIP  — anything else (no version bump)
#
# Usage: ./scripts/version.sh
# Output: "v1.2.3" or "skip" (if no bump needed)

set -euo pipefail

LATEST_TAG=$(git describe --tags --abbrev=0 --match "v*" 2>/dev/null || echo "v0.0.0")
VERSION=${LATEST_TAG#v}
IFS='.' read -r MAJOR MINOR PATCH <<< "$VERSION"

# Get commit messages since last tag (subject + body)
if [ "$LATEST_TAG" = "v0.0.0" ]; then
  COMMITS=$(git log --pretty=format:"%B---END---" 2>/dev/null)
else
  COMMITS=$(git log "${LATEST_TAG}..HEAD" --pretty=format:"%B---END---" 2>/dev/null)
fi

if [ -z "$COMMITS" ]; then
  echo "skip"
  exit 0
fi

BUMP=""

# Process each commit
while IFS= read -r -d '---END---' commit_msg || [ -n "$commit_msg" ]; do
  subject=$(echo "$commit_msg" | head -1)

  # Check for breaking change
  if echo "$subject" | grep -qE "^[a-z]+(\(.+\))?!:"; then
    BUMP="major"
    break
  fi
  if echo "$commit_msg" | grep -q "BREAKING CHANGE"; then
    BUMP="major"
    break
  fi

  # Check for feat
  if echo "$subject" | grep -qE "^feat(\(.+\))?:"; then
    if [ "$BUMP" != "major" ]; then
      BUMP="minor"
    fi
  fi

  # Check for fix
  if echo "$subject" | grep -qE "^fix(\(.+\))?:"; then
    if [ -z "$BUMP" ]; then
      BUMP="patch"
    fi
  fi
done <<< "$COMMITS"

if [ -z "$BUMP" ]; then
  echo "skip"
  exit 0
fi

case $BUMP in
  major) MAJOR=$((MAJOR + 1)); MINOR=0; PATCH=0 ;;
  minor) MINOR=$((MINOR + 1)); PATCH=0 ;;
  patch) PATCH=$((PATCH + 1)) ;;
esac

echo "v${MAJOR}.${MINOR}.${PATCH}"
