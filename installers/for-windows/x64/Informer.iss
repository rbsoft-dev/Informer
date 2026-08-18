; Информер — установщик для Windows (Inno Setup 6+)
;
; АРХИТЕКТУРНОЕ РЕШЕНИЕ: устанавливаем в %LocalAppData%\Programs\Informer (per-user,
; НЕ Program Files) — приложение пишет informer.db и crash.log рядом с exe, а
; Program Files доступен для записи только с правами администратора. При установке в
; LocalAppData этот конфликт не возникает вообще, и не нужна UAC-элевация ни на
; установке, ни при обычной работе программы. Тот же подход использует VS Code, Discord
; и большинство современных приложений с автообновлением.

#define MyAppName "Информер"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "RBSoft"
#define MyAppURL "https://rbsoft.ru"
#define MyAppExeName "Informer.exe"

#define PublishDir "..\..\..\Informer.App\bin\Release\net6.0\win-x64\publish"

[Setup]
AppId={{8F3B2C1A-4D6E-4A9C-9B2F-1E7D5C3A8B4F}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={localappdata}\Programs\Informer
; Явно НЕ Program Files — см. пояснение вверху файла.
DefaultGroupName={#MyAppName}
; Устанавливаем только для текущего пользователя — не требует прав администратора вообще.
PrivilegesRequired=lowest
OutputDir=..\..\..\dist\windows
OutputBaseFilename=Informer-Setup-{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
LicenseFile=..\..\..\LICENSE
SetupIconFile=..\..\..\Informer.App\Assets\tray-icon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Создать значок на рабочем столе"; GroupDescription: "Дополнительные значки:"
Name: "startupicon"; Description: "Запускать Информер при входе в Windows"; GroupDescription: "Автозапуск:"

[Files]
; Копируем ВСЁ содержимое папки публикации, КРОМЕ пользовательских данных —
; informer.db/crash.log в исходной папке публикации и не должно быть (их создаёт сама
; программа при первом запуске), но на всякий случай явно исключаем, чтобы при
; повторной сборке в той же папке случайно не затащить в инсталлятор чужие тестовые данные.
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "informer.db,informer.db-shm,informer.db-wal,crash.log"

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Удалить {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startupicon

[Run]
; Не запускаем сразу через "checked", т.к. если пользователь уже открывал программу
; (переустановка/обновление) — свежий процесс мог бы конфликтовать с уже запущенным (см. [Code]
; ниже, где мы явно закрываем старый процесс перед копированием файлов).
Filename: "{app}\{#MyAppExeName}"; Description: "Запустить {#MyAppName}"; Flags: nowait postinstall skipifsilent unchecked

[Code]
// Проверка версии Windows — минимально поддерживаемая: Windows 7 (см. README проекта).
// Self-contained сборка не требует отдельной проверки/докачки .NET — рантайм уже внутри.
function InitializeSetup(): Boolean;
begin
  Result := True;
  if not IsWin64 then
  begin
    // Наша сборка win-x64 — на 32-битной Windows физически не запустится.
    MsgBox('Информер требует 64-битную версию Windows. Установка невозможна на этой системе.', mbCriticalError, MB_OK);
    Result := False;
  end;
end;

// Если Информер уже запущен (например, обновление поверх работающей версии) — корректно
// закрываем его перед копированием файлов. Без этого Windows не даст перезаписать
// заблокированный работающим процессом .exe/.dll, и установка упадёт с невнятной ошибкой.
procedure CloseRunningInstance();
var
  ResultCode: Integer;
begin
  Exec('taskkill.exe', '/F /IM {#MyAppExeName}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  // Небольшая пауза, чтобы ОС гарантированно освободила файловые дескрипторы .exe/.dll
  // после завершения процесса, прежде чем Inno Setup попытается их перезаписать.
  Sleep(500);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssInstall then
  begin
    CloseRunningInstance();
  end;
end;
