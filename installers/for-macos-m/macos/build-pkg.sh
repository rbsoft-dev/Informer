#!/bin/bash
# Сборка Informer.app и Informer.pkg. ЗАПУСКАТЬ ТОЛЬКО НА macOS — использует
# iconutil и pkgbuild, инструменты, которые есть только в составе Xcode Command Line
# Tools на самой macOS (см. README проекта — та же настройка, что для сборки
# iOS-версии: xcode-select --install).
#
# Использование:
#   ./build-pkg.sh /путь/до/Informer.App/bin/Release/net6.0/osx-x64/publish   (Intel)
#   ./build-pkg.sh /путь/до/Informer.App/bin/Release/net6.0/osx-arm64/publish (Apple Silicon)
set -euo pipefail

PUBLISH_DIR="${1:?Укажи путь к папке публикации}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
APP_DIR="$SCRIPT_DIR/build/Informer.app"
OUT_DIR="$SCRIPT_DIR/../../dist/macos"

rm -rf "$APP_DIR"
mkdir -p "$APP_DIR/Contents/MacOS" "$APP_DIR/Contents/Resources"
cp "$SCRIPT_DIR/app-template/Contents/Info.plist" "$APP_DIR/Contents/Info.plist"

# Копируем результат публикации внутрь бандла — исключаем пользовательские данные
# по тем же причинам, что и в Linux-скрипте.
rsync -a --exclude 'informer.db*' --exclude 'crash.log' "$PUBLISH_DIR"/ "$APP_DIR/Contents/MacOS/"
chmod +x "$APP_DIR/Contents/MacOS/Informer"

# Собираем .icns из уже готового PNG (256x256) — iconutil требует набор из нескольких
# размеров в папке .iconset, поэтому генерируем нужные размеры через sips.
ICONSET="$SCRIPT_DIR/build/AppIcon.iconset"
rm -rf "$ICONSET"
mkdir -p "$ICONSET"
SRC_PNG="$SCRIPT_DIR/tray-icon.png"

for size in 16 32 64 128 256 512; do
    sips -z $size $size "$SRC_PNG" --out "$ICONSET/icon_${size}x${size}.png" >/dev/null
    double=$((size * 2))
    sips -z $double $double "$SRC_PNG" --out "$ICONSET/icon_${size}x${size}@2x.png" >/dev/null
done

iconutil -c icns "$ICONSET" -o "$APP_DIR/Contents/Resources/AppIcon.icns"

# Снимаем карантинную метку прямо на этапе сборки — не заменяет полноценную подпись/
# нотаризацию (см. предупреждение ниже), но избавляет от лишнего шага для тех, кто
# соберёт .app из исходников сам.
xattr -cr "$APP_DIR"

mkdir -p "$OUT_DIR"
pkgbuild --root "$APP_DIR" \
         --install-location "/Applications/Informer.app" \
         --identifier "ru.rbsoft.informer" \
         --version "1.0.0" \
         --min-os-version "11.0" \
         "$OUT_DIR/Informer.pkg"

echo "Готово: $OUT_DIR/Informer.pkg"
echo ""
echo "ВАЖНО: пакет НЕ подписан сертификатом разработчика Apple — см. предупреждение"
echo "в комментариях этого скрипта и в README."
