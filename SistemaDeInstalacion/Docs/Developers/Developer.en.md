# Developer

## Purpose and scope

This document explains the technical behavior of `SistemaDeInstalacion` following the real code path from startup to the main modules. The focus is not the functional use of the UI, but understanding which classes participate, which state they share, where information is persisted, and which constraints must be respected before changing the product.

The system is not split into frontend and backend. The solution combines:

- a WinForms `launcher` for installation and update;
- a desktop `WPF` application for authentication, administration, documentation, settings, and installer execution;
- local or shared `SQLite` persistence through `Microsoft.Data.Sqlite`.

## Real stack and constraints

| Topic | Evidence in repo | Technical implication |
|---|---|---|
| Main UI | `SistemaDeInstalacion.csproj`, `Views/*.xaml` | `WPF` application over `.NET Framework 4.8`. |
| Launcher | `LauncherSistema/Program.cs`, `LauncherSistema/LauncherService.cs` | Real distribution starts with an auxiliary executable before opening `SistemaDeInstalacion.exe`. |
| Persistence | `Db/DatabaseInitializer.cs`, `Db/*.cs` | There is no ORM and no remote API; access is direct SQL. |
| Tests | `SistemaDeInstalacion.Tests/*.cs` | There is unit coverage for DB, roles, and models; there are no UI tests. |
| Configuration | `App.config` | Shared database path and several corporate rules come from `appSettings`. |
| Publication | `SistemaDeInstalacion.csproj`, `LauncherSistema/LauncherService.cs` | Delivery depends on a corporate UNC share and `SetupSistema.exe`. |

Key constraints supported by code:

1. Administrative authentication does not depend only on the user role. It also requires entering through `AdminLoginViewModel` to activate `SesionUsuario.ModoAdministrador`.
2. Application permissions are not relational. They are stored as JSON route arrays in `Usuario.AplicativosJson`.
3. `SQLite` access is local/direct; the code mitigates locks with `PRAGMA busy_timeout = 5000` and retries, not with a strong multi-user transactional model.
4. The application depends on Windows profile services to resolve display name and device email (`WindowsProfileService`).
5. UI localization is based on `.resx` resources, `LocalizationService`, `LocalizedText`, and `TranslateExtension`.

## Repository map

| Path | Main responsibility |
|---|---|
| `SistemaDeInstalacion.sln` | Visual Studio solution with WPF app, tests, and launcher. |
| `SistemaDeInstalacion/` | Main application. |
| `App.xaml.cs` | WPF startup, global exception handlers, theme and localization initialization, DB bootstrap. |
| `Db/` | Schema initialization and SQL access for users, admins, installers, and recovery log. |
| `ViewModels/` | Login, shell navigation, user management, installers, help, settings, logs, and localization refresh. |
| `Services/` | Session, roles, logging, documentation, localization, themes, Windows utilities, and modal helpers. |
| `Docs/` | Embedded documentation rendered inside the Help module. |
| `LauncherSistema/` | Executable that installs, updates, and then opens the main app. |
| `SistemaDeInstalacion.Tests/` | MSTest project for persistence and domain rules. |

## Step 1. Real entry point: `LauncherSistema`

The real entry point of the complete product is not `App.xaml.cs`, but `LauncherSistema/Program.cs`. `Main()` delegates to `LauncherService.Ejecutar(args)`.

`LauncherService` resolves four main scenarios before opening the WPF app:

1. detects a post-install or post-update startup with `--post-update` or `--post-install`;
2. compares the local version against `version.txt` in the shared root;
3. if `SistemaDeInstalacion.exe` does not exist, requests installation and runs `SetupSistema.exe`;
4. if a newer version exists, it can launch a silent update and close the current process.

Key files:

- `LauncherSistema/Program.cs`
- `LauncherSistema/LauncherService.cs`

## Step 2. WPF startup and bootstrap

Once `SistemaDeInstalacion.exe` is opened, the flow enters `App.OnStartup()`.

Real sequence in `App.xaml.cs`:

1. register `DispatcherUnhandledException` and `AppDomain.CurrentDomain.UnhandledException`;
2. initialize `LocalizationService`;
3. register `GlobalCopyContextService`;
4. write the `Inicio de aplicación` event through `LogService.Info`;
5. initialize the database with `DatabaseInitializer.Initialize()` and measure latency;
6. show `LoginView`.

This means the database, logging subsystem, and localization service are prerequisites for any functional screen.

## Step 3. Database initialization

`Db/DatabaseInitializer.cs` is the persistence bootstrapper. It does much more than create `WegInstaladores.db`.

Confirmed responsibilities:

1. resolve `CurrentDbPath` from `CC_SHARED_DATABASE_PATH`;
2. create the container directory if missing;
3. migrate legacy database files when the current database does not yet exist;
4. create current tables (`Usuarios`, `Instaladores`, `Administrador`, `PasswordRecoveryLog`);
5. add missing columns with `ALTER TABLE` through `EnsureColumnExists()`;
6. migrate data from `Administradores` to `Administrador`;
7. remove old tables;
8. normalize inherited values, for example roles and default folders.

For the full persistence detail, review [Database](help://developers/base-de-datos).

## Step 4. Standard login

`LoginViewModel` implements the common operational authentication flow.

If you need the functional narrative of this screen, review [step-by-step sign in](help://users/user#step-by-step-to-sign-in).

Key files:

- `ViewModels/LoginViewModel.cs`
- `Db/UsuariosDbService.cs`
- `Views/LoginView.xaml`

Real flow:

1. the user enters alias or email in `Usuario`;
2. `UsuariosDbService.ObtenerCorreoPorUsuarioLogin()` resolves the real email using full email, alias before `@`, or device mapping;
3. `UsuariosDbService.Login()` compares `Correo` and `PasswordHash` using `SHA-256` in Base64;
4. on success, `AbrirSesionUsuario()` assigns `SesionUsuario.UsuarioActual` and forces `SesionUsuario.ModoAdministrador = false`;
5. `MainWindow` is opened.

## Step 5. Administrative login

`AdminLoginViewModel` defines the privileged flow. This is the point that really enables administrator mode in the main shell.

Key files:

- `ViewModels/AdminLoginViewModel.cs`
- `Db/AdministradoresDbService.cs`
- `Db/UsuariosDbService.cs`
- `Services/SesionUsuario.cs`

Real flow:

1. validate that an administrative record exists by `UsuarioSistema`;
2. authenticate against table `Administrador` with the administrative password;
3. resolve the base user in `Usuarios` using the admin email;
4. ensure the base user role is `ADMINISTRADOR`;
5. assign `SesionUsuario.UsuarioActual = usuarioNormal` and `SesionUsuario.ModoAdministrador = true`;
6. open `MainWindow`.

If you need the functional route, review [Step 4. Sign in through administrative login](help://administradores/administradores#step-4-sign-in-through-administrative-login).

## Step 6. Session, shell, and administrator mode

Session state lives in `Services/SesionUsuario.cs` and is mainly consumed from `MainViewModel`.

Relevant shared state:

```csharp
public static Usuario UsuarioActual { get; set; }
public static bool ModoAdministrador { get; set; }

public static bool EsAdmin =>
    ModoAdministrador && RolesSistema.EsAdministrador(UsuarioActual?.Rol);
```

This defines a central rule of the system: role alone is not enough; the user must have entered through the administrative flow.

`MainViewModel` builds the internal navigation:

- `MostrarInstaladores()` loads `InstaladoresView` as default view;
- `ShowGestionUsuariosCommand` only works if `EsAdmin` is `true`;
- `ShowLogsCommand` exposes the logs center behind a developer/support-only restriction; the canonical detail is in [Step 12. Logs and support diagnostics](help://developers/developer#step-12-logs-and-support-diagnostics);
- `ShowAyudaCommand` loads `HelpViewModel(EsAdministrador)` and filters documentation by profile and language;
- `ShowSettingsCommand` loads `SettingsView`;
- `CerrarSesionCommand` clears the session, returns to `LoginView`, and closes the current window.

For the functional side of the shell, review [User main view](help://users/user#user-main-view) and [Step 6. Administrator main view](help://administradores/administradores#step-6-administrator-main-view).

## Step 7. Installers module

This module mixes catalog, permissions, and external process execution.

Key files:

- `ViewModels/InstaladoresViewModel.cs`
- `ViewModels/FormularioInstaladorViewModel.cs`
- `Db/InstaladorDbService.cs`
- `Models/Instalador.cs`
- `Views/InstaladoresView.xaml`

Real responsibilities:

1. load installers from `InstaladorDbService.ObtenerTodos()`;
2. normalize folders to local or global categories;
3. if the session is not admin, filter by the routes stored in `SesionUsuario.UsuarioActual.ObtenerAplicativosAsignados()`;
4. allow create, edit, and delete only in admin mode;
5. execute the registered file using Windows process start.

For the functional route, review [Step 7. Installer management](help://administradores/administradores#step-7-installer-management) and [How to run an assigned application](help://users/user#how-to-run-an-assigned-application).

## Step 8. User management and assignment

`GestionUsuarioViewModel` controls the user list and application assignment.

Key files:

- `ViewModels/GestionUsuarioViewModel.cs`
- `ViewModels/FormularioUsuarioViewModel.cs`
- `Db/UsuariosDbService.cs`
- `Views/GestionUsuarioView.xaml`
- `Views/FormularioUsuarioView.xaml`

Technical responsibilities:

1. load all users from the DB service;
2. open create and edit modal forms;
3. delete selected users;
4. open the assignment panel;
5. assign applications by serializing the selected routes to `AplicativosJson`.

For the functional walkthrough, review [Step 8. User management](help://administradores/administradores#step-8-user-management) and [Step 9. Application assignment](help://administradores/administradores#step-9-application-assignment).

## Step 9. Registration and recovery

The registration and recovery flows are split into different ViewModels:

- `RegisterViewModel` for standard registration;
- `AdminRegisterViewModel` for administrative registration;
- `MicrosoftRecoveryViewModel` and `RecoveryCodePopupView` for password recovery.

These flows combine validation rules, DB checks, and localized feedback.

Functional references:

- [User registration](help://users/user#user-registration)
- [Step 3. Administrative registration](help://administradores/administradores#step-3-administrative-registration)
- [Password recovery](help://users/user#password-recovery)

## Step 10. Help and embedded documentation

The Help module loads markdown documents from `Docs/` and renders them inside the WPF application.

Key files:

- `Services/DocumentationService.cs`
- `Services/MarkdownDocumentRenderer.cs`
- `ViewModels/HelpViewModel.cs`
- `Views/HelpView.xaml`
- `Views/HelpView.xaml.cs`

Important behavior:

1. documentation is filtered by profile;
2. the service resolves the localized file according to the selected culture (`.en.md`, `.pt-BR.md`, or base Spanish document);
3. internal links such as `help://users/user#...` are resolved inside the embedded viewer;
4. when the language changes, Help reloads the sections and attempts to keep the selected document and anchor.

Functional references:

- [How to use the Help tab](help://users/user#how-to-use-the-help-tab)
- [Step 10. Using the Help tab](help://administradores/administradores#step-10-using-the-help-tab)

## Step 11. Localization and theme

The current localization stack is based on:

- `Properties/Resources.resx`
- `Properties/Resources.en.resx`
- `Properties/Resources.pt-BR.resx`
- `Services/LocalizationService.cs`
- `Services/LocalizedText.cs`
- `Markup/TranslateExtension.cs`
- `ViewModels/ILocalizableViewModel.cs`

`LocalizationService` changes `Thread.CurrentCulture`, `Thread.CurrentUICulture`, `Resources.Culture`, refreshes open ViewModels, and notifies the UI.

Theme switching is handled by `ThemeManager`, `SettingsViewModel`, and XAML theme dictionaries.

The functional reference of the settings screen is in [Settings](help://users/user#settings).

## Step 12. Logs and support diagnostics

The current logs metrics flow is split across `LogService` (write path), `LogDashboardService` (aggregation), `LogsViewModel` (filters, refresh, drill-down), and `LogsView.xaml` + `LogsView.xaml.cs` (rendering and hover behavior).

Key files:

- `Services/LogService.cs`
- `Services/LogDashboardService.cs`
- `Services/AppLogEntry.cs`
- `ViewModels/LogsViewModel.cs`
- `Views/LogsView.xaml`
- `Views/LogsView.xaml.cs`

Developer-only constraints verified from code:

- `MainViewModel.PuedeVerLogs` only opens the logs center when `SesionUsuario.EsAdmin` is `true` and the current email is in `AllowedLogViewerEmails` (`wandica@weg.net`, `maicolj@weg.net`);
- `LogsViewModel` applies a developer-biased default machine context only when `LogService.ResolveCurrentAuditUserName()` resolves to `wandica` or `maicolj`.

In practice, these metrics are developer/support diagnostics, not a screen intended for end users or general administrators.

Technical data flow:

1. `LogService.Write()` persists 8 tab-separated columns: timestamp, level, machine, user, source, `DurationMs`, message, and details.
2. The primary root is derived from `CC_SHARED_DATABASE_PATH`; if write access fails, logging falls back to `%LocalAppData%\SistemaDeInstalacion\LogsFallback`.
3. Files are stored as `Logs/<Machine>/<yyyy-MM-dd>/events.log`.
4. `LogDashboardService` reads every readable root returned by `LogService.GetReadableLogsDirectories()`, parses `.log` files into `AppLogEntry`, and extracts semantic tokens from `Details` with `key=value|...` pairs.
5. Semantic metrics come from those log details, not from `SQLite`. The parser looks for tokens such as `event=validation`, `accepted=false`, `signal=heartbeat`, `dependency=...`, `state=degraded`, and `interval_minutes=...`.
6. Older Spanish logs remain supported through the `evento=` -> `event=` compatibility mapping.

Current metrics implementation:

1. `GetDashboardSnapshot()` loads `baseEntries` by machine + time range, then applies severity/source/user/search filters to produce `filteredEntries`.
2. Top cards come from `LogDashboardSummary`: visible events, errors, warnings, and P95 latency. Average latency is also computed and reused in the narrative sections.
3. `BuildExecutiveStatus()` computes observable coverage over five base signals: `health`, `heartbeat`, `dependencies`, `session`, and `validation`.
4. `BuildStatusSections()` creates the visible narrative sections: `incidents`, `validations`, `latency`, `activity`, and `health`, each with facts, timeline segments, and trend cells.
5. `BuildDistribution()` produces top-6 activity blocks by source, user, and machine from `filteredEntries`.
6. `BuildLatencyDistribution()` groups entries with `DurationMs` into `< 250 ms`, `250-499 ms`, `500-999 ms`, `1-2 s`, and `>= 2 s`.
7. `BuildCriticalEvents()` exposes the last 8 visible critical events (`ERROR`, `WARNING`, rejected `VALIDATION`, and degraded/unhealthy `HEALTH` signals).
8. `InstrumentationStatus` is intentionally honest: if a signal is missing, the dashboard reports the instrumentation gap instead of inventing uptime.

UI / ViewModel / service interaction:

1. `LogsViewModel` builds `LogDashboardQuery`, requests snapshots, and applies them into bindable collections used by `LogsView.xaml`.
2. Real-time mode forces the `2h` window, subscribes to `LogService.LogWritten`, debounces refreshes by `150 ms`, and also keeps a `2 s` auto-refresh timer.
3. Source, user, and machine metric blocks are clickable and apply contextual filters; latency blocks are hover-only.
4. `LogsView.xaml.cs` manages persistent hover sessions and delegates hover card composition to `PreviewMetricHover()`, `PreviewFactHover()`, and `PreviewNarrativeSegment()`.
5. Incident and narrative drill-down only reshape the current filtered view; they do not modify persisted logs.

Current implementation note: `LogDashboardSnapshot` still exposes `ErrorSeries` and `WarningSeries`, but the present XAML no longer binds those collections directly. The visible dashboard now prioritizes `StatusSections`, distributions, contextual hover, incident timeline, and the filtered grid.

Historical log entries remain immutable data; changing UI language does not rewrite previously persisted messages.

## Build, release, and operational workflow

For development workflow, the current branch convention is:

```text
ProgramTranslation -> Homologation -> Produccion
```

Meaning:

- `ProgramTranslation`: active development branch;
- `Homologation`: validation branch;
- `Produccion`: release branch.

The WPF app should be developed in `ProgramTranslation`, then reviewed and promoted to `Homologation`, and only after validation promoted to `Produccion`.

## Technical recommendations

- do not work directly on `Produccion`;
- avoid changing persistence rules without reviewing `DatabaseInitializer` and DB services together;
- when localizing a screen, update both XAML and ViewModel messages;
- when localizing documentation, create language-specific markdown files instead of mixing languages in a single file;
- treat launcher, WPF app, documentation, and shared database as one coordinated product.

## Reference links

- [WPF data binding](https://learn.microsoft.com/dotnet/desktop/wpf/data/)
- [FlowDocument](https://learn.microsoft.com/dotnet/api/system.windows.documents.flowdocument)
- [Hyperlink in WPF](https://learn.microsoft.com/dotnet/api/system.windows.documents.hyperlink)
- [Microsoft.Data.Sqlite](https://learn.microsoft.com/dotnet/standard/data/sqlite/)
- [SQLite `PRAGMA busy_timeout`](https://www.sqlite.org/pragma.html#pragma_busy_timeout)
- [MSTest](https://learn.microsoft.com/dotnet/core/testing/unit-testing-mstest)
