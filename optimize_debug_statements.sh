#!/bin/bash
# Debug Statement Optimization Script
# Replaces Debug.Log calls with optimized MOBALogger calls

echo "Starting comprehensive debug statement optimization..."

# Create backup directory
mkdir -p "/Users/jamesmykil/Desktop/M0BA/Backup_$(date +%Y%m%d_%H%M%S)"

# Find all C# files with Debug.Log statements
find "/Users/jamesmykil/Desktop/M0BA/Assets/Scripts" -name "*.cs" -type f | while read -r file; do
    if grep -q "Debug\.Log\|Debug\.LogWarning\|Debug\.LogError" "$file"; then
        echo "Optimizing: $file"
        
        # Create backup
        cp "$file" "/Users/jamesmykil/Desktop/M0BA/Backup_$(date +%Y%m%d_%H%M%S)/$(basename "$file")"
        
        # Replace Debug.Log with conditional logging
        sed -i '' 's/Debug\.Log(/MOBALogger.LogConditional(/g' "$file"
        sed -i '' 's/Debug\.LogWarning(/MOBALogger.LogWarning(/g' "$file"
        sed -i '' 's/Debug\.LogError(/MOBALogger.LogError(/g' "$file"
        
        # Add using statement if not present
        if ! grep -q "using MOBA\.Core;" "$file"; then
            sed -i '' '/^using UnityEngine;/a\
using MOBA.Core;' "$file"
        fi
    fi
done

echo "Debug statement optimization complete!"
echo "Run 'grep -r \"Debug\.Log\" Assets/Scripts --include=\"*.cs\" | wc -l' to verify reduction."
