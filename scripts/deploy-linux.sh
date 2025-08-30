#!/usr/bin/env bash
set -euo pipefail

# Full Linux deploy: publish, build .deb, install and run

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
APPNAME="srvsurvey"
AVNAME="SrvSurvey.UI.Avalonia"

export PATH="$HOME/.dotnet:$PATH"

cd "$ROOT_DIR"

# Resolve version from version.json if present
if [[ -f "$ROOT_DIR/version.json" ]]; then
  VERSION=$(grep -oP '"version"\s*:\s*"\K[^"]+' "$ROOT_DIR/version.json" | head -n1 || true)
else
  VERSION="0.0.0"
fi
VERSION=${VERSION:-0.0.0}

PUBLISH_DIR="$ROOT_DIR/publish/linux"
DIST_ROOT="$ROOT_DIR/dist"
DEBROOT="$DIST_ROOT/debroot"
DEB_FILE="$DIST_ROOT/${APPNAME}_${VERSION}_amd64.deb"

echo "=== SrvSurvey Linux Deploy & Install Script ==="
echo "Version: $VERSION"
echo ""

echo "[1/4] Publishing $AVNAME (linux-x64, self-contained)..."
dotnet publish "$ROOT_DIR/$AVNAME/$AVNAME.csproj" -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -o "$PUBLISH_DIR"

echo "[2/4] Preparing .deb structure..."
rm -rf "$DEBROOT"
mkdir -p "$DEBROOT/DEBIAN" "$DEBROOT/opt/$APPNAME" "$DEBROOT/usr/bin" "$DEBROOT/usr/share/applications" "$DEBROOT/usr/share/icons/hicolor/128x128/apps"
cp -r "$PUBLISH_DIR"/* "$DEBROOT/opt/$APPNAME/"

cat > "$DEBROOT/DEBIAN/control" <<EOF
Package: $APPNAME
Version: $VERSION
Section: utils
Priority: optional
Architecture: amd64
Maintainer: SrvSurvey Team <noreply@example.com>
Description: SrvSurvey (Avalonia) cross-platform client for Elite Dangerous journals.
EOF

install -m 0755 /dev/stdin "$DEBROOT/DEBIAN/postinst" <<'EOF'
#!/bin/sh
set -e
exit 0
EOF
install -m 0755 /dev/stdin "$DEBROOT/DEBIAN/prerm" <<'EOF'
#!/bin/sh
set -e
exit 0
EOF
install -m 0755 /dev/stdin "$DEBROOT/DEBIAN/postrm" <<'EOF'
#!/bin/sh
set -e
exit 0
EOF

install -m 0755 /dev/stdin "$DEBROOT/usr/bin/$APPNAME" <<EOF
#!/bin/sh
exec "/opt/$APPNAME/$AVNAME" "\$@"
EOF

cat > "$DEBROOT/usr/share/applications/$APPNAME.desktop" <<EOF
[Desktop Entry]
Name=SrvSurvey
Comment=SrvSurvey (Avalonia) for Elite Dangerous
Exec=/usr/bin/$APPNAME
Terminal=false
Type=Application
Categories=Game;Utility;
Icon=$APPNAME
EOF

# Icon
ICON_SRC=""
if [[ -f "$ROOT_DIR/SrvSurvey/logo.ico" ]]; then
  ICON_SRC="$ROOT_DIR/SrvSurvey/logo.ico"
elif [[ -f "$ROOT_DIR/docs/logo.png" ]]; then
  ICON_SRC="$ROOT_DIR/docs/logo.png"
fi
if command -v convert >/dev/null 2>&1 && [[ -n "$ICON_SRC" ]]; then
  convert "$ICON_SRC" -resize 128x128 "$DEBROOT/usr/share/icons/hicolor/128x128/apps/$APPNAME.png"
elif [[ -n "$ICON_SRC" ]]; then
  cp "$ICON_SRC" "$DEBROOT/usr/share/icons/hicolor/128x128/apps/$APPNAME.png"
fi

echo "[3/4] Building .deb..."
mkdir -p "$DIST_ROOT"
dpkg-deb --build "$DEBROOT" "$DEB_FILE"

echo "[4/4] Installing and running..."

# Check if package is already installed
if dpkg -l | grep -q "^ii.*$APPNAME"; then
    echo "Removing existing $APPNAME package..."
    sudo dpkg -r "$APPNAME" || true
fi

echo "Installing $APPNAME version $VERSION..."
sudo dpkg -i "$DEB_FILE"

# Fix dependencies if needed
if ! dpkg -l | grep -q "^ii.*$APPNAME"; then
    echo "Fixing dependencies..."
    sudo apt install -f -y
fi

echo ""
echo "=== Installation Complete ==="
echo "Package: $APPNAME $VERSION"
echo "Files:"
echo " - $PUBLISH_DIR (published app)"
echo " - $DEB_FILE (deb package)"
echo " - /opt/$APPNAME/ (installed app)"
echo " - /usr/bin/$APPNAME (executable)"
echo ""

# Ask user if they want to run the app
echo "Would you like to run SrvSurvey now? (y/N)"
read -r -n 1 -t 10 response
echo ""

if [[ "$response" =~ ^[Yy]$ ]]; then
    echo "Starting SrvSurvey..."
    sleep 2
    exec "$APPNAME"
else
    echo "SrvSurvey installed successfully!"
    echo "You can run it later with: $APPNAME"
    echo "Or find it in your applications menu."
fi


