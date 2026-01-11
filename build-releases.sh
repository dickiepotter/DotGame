#!/bin/bash
# Build script for creating release binaries

set -e

echo "Building DotGame release binaries..."

# Clean previous releases
rm -rf releases
mkdir -p releases

# Build Linux binary
echo "Building Linux x64..."
cargo build --release
cp target/release/dotgame releases/dotgame-linux-x64
echo "✓ Linux binary: releases/dotgame-linux-x64 ($(du -h releases/dotgame-linux-x64 | cut -f1))"

# Build Windows binary (requires mingw-w64)
echo "Building Windows x64..."
rustup target add x86_64-pc-windows-gnu 2>/dev/null || true
cargo build --release --target x86_64-pc-windows-gnu
cp target/x86_64-pc-windows-gnu/release/dotgame.exe releases/dotgame-windows-x64.exe
echo "✓ Windows binary: releases/dotgame-windows-x64.exe ($(du -h releases/dotgame-windows-x64.exe | cut -f1))"

# Create checksums
cd releases
sha256sum * > checksums.txt
echo "✓ Checksums: releases/checksums.txt"
cd ..

echo ""
echo "All binaries built successfully!"
echo "Binaries available in: ./releases/"
ls -lh releases/
