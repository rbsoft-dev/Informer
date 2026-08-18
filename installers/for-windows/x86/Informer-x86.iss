; Информер — установщик для 32-битной (x86) Windows (Inno Setup 6+)
;
; Отличие от 64-битного скрипта (installers/windows/Informer.iss): здесь НЕ
; ограничиваем архитектуру — win-x86 сборка запускается и на 32-битной, и на 64-битной
; Windows (через встроенную совместимость WOW64), поэтому SetupArchitecture/
; ArchitecturesInstallIn64BitMode здесь намеренно не заданы — иначе на 64-битной Windows
; Inno Setup мог бы поставить программу в 64-битный Program Files/реестр, что нам тут не
; нужно (используем LocalAppData, не Program Files, так что на практике разница
; малозаметна — но лучше не задавать лишних ограничений архитектуры без необходимости).

#define MyAppName "Информер"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "RBSoft"
#define MyAppURL "https://rbsoft.ru"
#define MyAppExeName "Informer.exe"

; Путь к результату публикации ПОД x86 — поправь под свою машину перед компиляцией
#define PublishDir "..\..\..\Informer.App\bin\Release\net6.0\win-x86\publish"

[Setup]
AppId={{8F3B2C1A-4D6E-4A9C-9B2F-1E7D5C3A8B4F}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={localappdata}\Programs\Informer
DefaultGroupName={#MyAppName}
PrivilegesRequired=lowest
OutputDir=..\..\..\dist\windows
; Отдельное имя файла — чтобы не перепутать с 64-битным инсталлятором при раздаче
OutputBaseFilename=Informer-Setup-{#MyAppVersion}-x86
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
LicenseFile=..\..\..\LICENSE
SetupIconFile=..\..\..\Informer.App\Assets\tray-icon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
; Windows 7 SP1 — тот же реальный минимум, что и для x64 (ограничение .NET 6, не архитектуры).
MinVersion=6.1sp1

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Создать значок на рабочем столе"; GroupDescription: "Дополнительные значки:"
Name: "startupicon"; Description: "Запускать Информер при входе в Windows"; GroupDescription: "Автозапуск:"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "informer.db,informer.db-shm,informer.db-wal,crash.log"

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Удалить {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startupicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Запустить {#MyAppName}"; Flags: nowait postinstall skipifsilent unchecked

[Code]
// Здесь НЕТ проверки IsWin64 — в отличие от x64-скрипта, эта сборка обязана работать
// и на 32-битной, и на 64-битной Windows, так что ограничивать нечего.

procedure CloseRunningInstance();
var
  ResultCode: Integer;
begin
  Exec('taskkill.exe', '/F /IM {#MyAppExeName}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Sleep(500);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssInstall then
  begin
    CloseRunningInstance();
  end;
end;
