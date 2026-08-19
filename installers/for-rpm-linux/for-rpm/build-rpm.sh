#!/bin/bash
# Сборка .rpm-пакета Информера. Запускать на RPM-дистрибутиве (Fedora/RHEL/CentOS/
# openSUSE) — нужен установленный rpmbuild:
#   Fedora/RHEL/CentOS: sudo dnf install rpm-build rpmdevtools
#   openSUSE:            sudo zypper install rpm-build rpmdevtools
#
# Использование:
#   ./build-rpm.sh /путь/до/Informer.App/bin/Release/net6.0/linux-x64/publish
set -euo pipefail

PUBLISH_DIR="${1:?Укажи путь к папке публикации: ./build-rpm.sh /путь/до/publish}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
VERSION="1.0.0"
OUT_DIR="$SCRIPT_DIR/../../dist/rpm"

# rpmbuild ожидает стандартную структуру папок в ~/rpmbuild — rpmdev-setuptree создаёт
# её автоматически (SPECS, SOURCES, BUILD, RPMS, SRPMS), если утилита не установлена,
# создаём вручную тем же способом.
if command -v rpmdev-setuptree >/dev/null 2>&1; then
    rpmdev-setuptree
else
    mkdir -p ~/rpmbuild/{SPECS,SOURCES,BUILD,RPMS,SRPMS}
fi

# Source0 в spec-файле — это tar.gz с папкой informer-1.0.0/ внутри, содержащей саму
# публикацию (self-contained сборку .NET). Собираем его прямо из переданной publish/.
TMP_TAR_DIR="$(mktemp -d)"
mkdir -p "$TMP_TAR_DIR/informer-$VERSION"
rsync -a --exclude 'informer.db*' --exclude 'crash.log' "$PUBLISH_DIR"/ "$TMP_TAR_DIR/informer-$VERSION/"
tar -czf ~/rpmbuild/SOURCES/informer-$VERSION.tar.gz -C "$TMP_TAR_DIR" "informer-$VERSION"
rm -rf "$TMP_TAR_DIR"

cp "$SCRIPT_DIR/informer.desktop" ~/rpmbuild/SOURCES/
cp "$SCRIPT_DIR/tray-icon.png" ~/rpmbuild/SOURCES/informer.png
cp "$SCRIPT_DIR/informer.spec" ~/rpmbuild/SPECS/

rpmbuild -bb ~/rpmbuild/SPECS/informer.spec

mkdir -p "$OUT_DIR"
find ~/rpmbuild/RPMS -name "informer-$VERSION*.rpm" -exec cp {} "$OUT_DIR/" \;

echo "Готово, смотри: $OUT_DIR"
