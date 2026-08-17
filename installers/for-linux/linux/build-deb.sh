#!/bin/bash
# Сборка .deb-пакета Информера. Запускать на Linux (или в WSL) из папки
# installers/linux/.
#
# Использование:
#   ./build-deb.sh /путь/до/Informer.App/bin/Release/net6.0/linux-x64/publish
set -euo pipefail

PUBLISH_DIR="${1:?Укажи путь к папке публикации: ./build-deb.sh /путь/до/publish}"
TEMPLATE_DIR="$(dirname "$0")/deb-template"
BUILD_DIR="$(dirname "$0")/build/informer_1.0.0_amd64"
OUT_DIR="$(dirname "$0")/../../dist/linux"

rm -rf "$BUILD_DIR"
mkdir -p "$BUILD_DIR"
cp -r "$TEMPLATE_DIR"/* "$BUILD_DIR"/

# Копируем результат публикации в /usr/lib/informer — исключаем пользовательские
# данные, которые могут случайно оказаться в папке публикации при локальном тестировании
# (informer.db создаётся приложением при первом запуске, не должен попадать в пакет).
rsync -a --exclude 'informer.db*' --exclude 'crash.log' "$PUBLISH_DIR"/ "$BUILD_DIR/usr/lib/informer/"

# Иконка для меню приложений — берём уже подготовленный PNG из основного проекта.
cp "$(dirname "$0")/tray-icon.png" \
   "$BUILD_DIR/usr/share/icons/hicolor/256x256/apps/informer.png"

chmod 0755 "$BUILD_DIR/DEBIAN/postinst"
chmod 0755 "$BUILD_DIR/usr/bin/informer"
chmod 0755 "$BUILD_DIR/usr/lib/informer/Informer"

mkdir -p "$OUT_DIR"
dpkg-deb --build --root-owner-group "$BUILD_DIR" "$OUT_DIR/informer_1.0.0_amd64.deb"

echo "Готово: $OUT_DIR/informer_1.0.0_amd64.deb"
