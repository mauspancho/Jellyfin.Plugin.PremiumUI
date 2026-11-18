#!/usr/bin/env bash
#
# Empaqueta Jellyfin Premium UI en un ZIP instalable.
#
# Uso:
#   ./build/package.sh            # compila Release y genera dist/Jellyfin.PremiumUI_<version>.zip
#   ./build/package.sh --no-build # reutiliza la compilación Release existente
#
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DOTNET="${DOTNET:-dotnet}"
PROJECT="$ROOT_DIR/src/Jellyfin.Plugin.PremiumUI/Jellyfin.Plugin.PremiumUI.csproj"
META="$ROOT_DIR/build/meta.json"
OUT_DIR="$ROOT_DIR/dist"
STAGE_DIR="$OUT_DIR/stage"

if ! command -v python3 >/dev/null 2>&1; then
    echo "error: python3 es necesario para leer meta.json" >&2
    exit 1
fi

VERSION="$(python3 -c 'import json,sys; print(json.load(open(sys.argv[1]))["version"])' "$META")"
ASSEMBLY="$(python3 -c 'import json,sys; print(json.load(open(sys.argv[1]))["artifacts"][0])' "$META")"
ZIP_NAME="Jellyfin.PremiumUI_${VERSION}.zip"

echo "==> Plugin    : $ASSEMBLY"
echo "==> Versión   : $VERSION"
echo "==> Salida    : $OUT_DIR/$ZIP_NAME"

if [[ "${1:-}" != "--no-build" ]]; then
    if [[ -d "$ROOT_DIR/frontend" ]]; then
        if ! command -v npm >/dev/null 2>&1; then
            echo "error: npm es necesario para compilar el frontend" >&2
            exit 1
        fi

        if [[ ! -d "$ROOT_DIR/frontend/node_modules" ]]; then
            echo "==> Instalando dependencias del frontend"
            (cd "$ROOT_DIR/frontend" && npm ci --no-audit --no-fund)
        fi

        echo "==> Compilando frontend"
        (cd "$ROOT_DIR/frontend" && npm run build)

        for asset in app.js app.css; do
            if [[ ! -s "$ROOT_DIR/src/Jellyfin.Plugin.PremiumUI/Web/$asset" ]]; then
                echo "error: falta el asset Web/$asset tras compilar el frontend" >&2
                exit 1
            fi
        done
    fi

    echo "==> Compilando Release"
    DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 \
        "$DOTNET" build "$PROJECT" -c Release --nologo -v minimal
fi

BUILD_DLL="$ROOT_DIR/src/Jellyfin.Plugin.PremiumUI/bin/Release/net10.0/$ASSEMBLY"
if [[ ! -f "$BUILD_DLL" ]]; then
    echo "error: no se encontró $BUILD_DLL (ejecuta sin --no-build)" >&2
    exit 1
fi

rm -rf "$STAGE_DIR"
mkdir -p "$STAGE_DIR"
cp "$BUILD_DLL" "$STAGE_DIR/$ASSEMBLY"
cp "$META" "$STAGE_DIR/meta.json"
cp "$ROOT_DIR/THIRD-PARTY-NOTICES.md" "$STAGE_DIR/THIRD-PARTY-NOTICES.md"

# Verifica que la página de configuración, el frontend y el arte de estudios
# estén embebidos.
for asset in configPage.html app.js app.css disney.webp disney.mp4; do
    if ! grep -qa "$asset" "$STAGE_DIR/$ASSEMBLY"; then
        echo "error: $asset no está embebido en el ensamblado" >&2
        exit 1
    fi
done

rm -f "$OUT_DIR/$ZIP_NAME"
(
    cd "$STAGE_DIR"
    zip -q -9 "$OUT_DIR/$ZIP_NAME" "$ASSEMBLY" meta.json THIRD-PARTY-NOTICES.md
)

rm -rf "$STAGE_DIR"

echo "==> Contenido del paquete"
unzip -l "$OUT_DIR/$ZIP_NAME"
echo "==> Listo: $OUT_DIR/$ZIP_NAME"
