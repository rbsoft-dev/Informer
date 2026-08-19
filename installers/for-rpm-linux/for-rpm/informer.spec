Name:           informer
Version:        1.0.0
Release:        1%{?dist}
Summary:        Tray application for HTTP notifications
License:        MIT
URL:            https://rbsoft.ru
Source0:        informer-%{version}.tar.gz
Source1:        informer.desktop
Source2:        informer.png

# Self-contained сборка не включает эти системные библиотеки — так же, как и в .deb,
# rpm/dnf/yum сами проверят их наличие при установке и подтянут, если их нет.
# Точное имя пакета отличается между дистрибутивами (Fedora/RHEL/openSUSE) — проверь
# перед публикацией: dnf search libicu   (или zypper search libicu на openSUSE)
Requires:       libicu
Requires:       openssl-libs

BuildArch:      x86_64
%global debug_package %{nil}
%global __os_install_post %{nil}

%description
Информер принимает уведомления по HTTP (JSON) от внешних систем (1С, кассовое ПО,
скрипты), показывает их всплывающими тостами и хранит историю с фильтрацией по
отправителю. Поддерживает несколько языков интерфейса.

%prep
%setup -q -c -T
tar -xzf %{SOURCE0}

%build
# Нечего собирать — публикация уже готова (self-contained .NET build), просто
# раскладываем файлы по нужным местам на шаге.

%install
rm -rf %{buildroot}

mkdir -p %{buildroot}/usr/lib/informer
cp -a informer-%{version}/. %{buildroot}/usr/lib/informer/

mkdir -p %{buildroot}/usr/bin
cat > %{buildroot}/usr/bin/informer << 'EOF'
#!/bin/sh
exec /usr/lib/informer/Informer "$@"
EOF
chmod 755 %{buildroot}/usr/bin/informer
chmod 755 %{buildroot}/usr/lib/informer/Informer

mkdir -p %{buildroot}/usr/share/applications
install -m 644 %{SOURCE1} %{buildroot}/usr/share/applications/informer.desktop

mkdir -p %{buildroot}/usr/share/icons/hicolor/256x256/apps
install -m 644 %{SOURCE2} %{buildroot}/usr/share/icons/hicolor/256x256/apps/informer.png

%files
/usr/lib/informer/*
/usr/bin/informer
/usr/share/applications/informer.desktop
/usr/share/icons/hicolor/256x256/apps/informer.png

%post
gtk-update-icon-cache -f /usr/share/icons/hicolor >/dev/null 2>&1 || true
update-desktop-database /usr/share/applications >/dev/null 2>&1 || true

%changelog
* Mon Aug 17 2026 Evgeniy Ershov <online@rbsoft.ru> - 1.0.0-1
- Первый релиз
