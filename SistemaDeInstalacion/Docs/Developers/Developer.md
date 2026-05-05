# Developer

## Propósito y alcance

Este documento describe el funcionamiento técnico de `SistemaDeInstalacion` siguiendo el recorrido real del código desde el arranque hasta los módulos principales. El foco no está en el uso funcional de la UI, sino en entender qué clases participan, qué estado comparten, dónde se persiste la información y qué restricciones condicionan cualquier cambio.

El sistema no está dividido en frontend y backend. La solución combina:

- un `launcher` WinForms para instalación y actualización;
- una aplicación `WPF` de escritorio para autenticación, administración y ejecución de instaladores;
- persistencia local o compartida en `SQLite` mediante `Microsoft.Data.Sqlite`.

## Stack y restricciones reales

| Tema | Evidencia en repo | Implicación técnica |
|---|---|---|
| UI principal | `SistemaDeInstalacion/SistemaDeInstalacion.csproj`, `Views/*.xaml` | Aplicación `WPF` sobre `.NET Framework 4.8`. |
| Launcher | `LauncherSistema/Program.cs`, `LauncherSistema/LauncherService.cs` | La distribución real arranca por un ejecutable auxiliar antes de abrir `SistemaDeInstalacion.exe`. |
| Persistencia | `Db/DatabaseInitializer.cs`, `Db/*.cs` | No hay ORM ni API remota; el acceso es SQL directo. |
| Tests | `SistemaDeInstalacion.Tests/*.cs`, `SistemaDeInstalacion.Tests.csproj` | Hay cobertura unitaria para DB, roles y modelos; no hay pruebas de UI. |
| Configuración | `App.config` | El path de base compartida y algunas reglas corporativas salen de `appSettings`. |
| Publicación | `SistemaDeInstalacion.csproj`, `LauncherSistema/LauncherService.cs` | La entrega depende de un share UNC corporativo y de un `SetupSistema.exe`. |

Restricciones importantes respaldadas por código:

1. La autenticación administrativa no depende solo del rol del usuario. También exige entrar por `AdminLoginViewModel` para activar `SesionUsuario.ModoAdministrador`.
2. Los permisos de aplicativos no son relacionales. Se guardan como JSON de rutas en `Usuario.AplicativosJson`.
3. El acceso a `SQLite` es local/directo; hay mitigación básica de bloqueos con `PRAGMA busy_timeout = 5000` y reintentos, no un modelo transaccional multiusuario fuerte.
4. La aplicación depende de servicios del equipo Windows para resolver nombre visible y correo del dispositivo (`WindowsProfileService`).

## Mapa del repositorio

| Ruta | Responsabilidad principal |
|---|---|
| `SistemaDeInstalacion.sln` | Solución Visual Studio con app WPF, tests y launcher. |
| `SistemaDeInstalacion/` | Aplicación principal. |
| `SistemaDeInstalacion/App.xaml.cs` | Arranque WPF, handlers globales de excepciones, inicialización de DB. |
| `SistemaDeInstalacion/Db/` | Inicialización de esquema y acceso SQL a `Usuarios`, `Administrador` e `Instaladores`. |
| `SistemaDeInstalacion/ViewModels/` | Flujo de login, navegación, gestión de usuarios, instaladores, ayuda y logs. |
| `SistemaDeInstalacion/Services/` | Sesión, roles, logging, documentación, utilidades de Windows y overlays modales. |
| `SistemaDeInstalacion/Docs/` | Documentación embebida en la aplicación de ayuda. |
| `SistemaDeInstalacion/LauncherSistema/` | Ejecutable que instala, actualiza y luego abre la app principal. |
| `SistemaDeInstalacion/SistemaDeInstalacion.Tests/` | Pruebas MSTest sobre lógica de persistencia y dominio. |

## Paso 1. Entrada real del producto: `LauncherSistema`

El primer punto de entrada del producto completo no es `App.xaml.cs`, sino `LauncherSistema/Program.cs`. `Main()` llama a `LauncherService.Ejecutar(args)`.

`LauncherService` resuelve cuatro escenarios antes de abrir la app WPF:

1. detecta un arranque post-instalación o post-actualización con `--post-update` o `--post-install`;
2. compara la versión local con `version.txt` en el recurso compartido definido por `SharedRoot`;
3. si `SistemaDeInstalacion.exe` no existe, solicita instalación y ejecuta `SetupSistema.exe`;
4. si hay una versión nueva, puede lanzar actualización silenciosa y cerrar el proceso actual con `Environment.Exit(0)`.

Archivos clave:

- `LauncherSistema/Program.cs`
- `LauncherSistema/LauncherService.cs`

Detalles que importan para mantenimiento:

- la ruta de distribución está hardcodeada en `LauncherService.SharedRoot`;
- el launcher usa archivos de estado locales (`build.version`, `update.pending`) para evitar prompts repetidos;
- el fallo al encontrar `SetupSistema.exe` no rompe el launcher, pero deja al usuario sin ruta automática de instalación.

## Paso 2. Arranque WPF y bootstrap de aplicación

Una vez abierto `SistemaDeInstalacion.exe`, el flujo entra en `App.OnStartup()`.

Secuencia real en `App.xaml.cs`:

1. registra `DispatcherUnhandledException` y `AppDomain.CurrentDomain.UnhandledException`;
2. registra `GlobalCopyContextService`;
3. escribe el evento `Inicio de aplicación` con `LogService.Info`;
4. inicializa la base con `DatabaseInitializer.Initialize()` y mide latencia;
5. muestra `LoginView`.

Esto implica que la base de datos y el subsistema de logs son prerequisitos de cualquier pantalla funcional. Si se toca el esquema o la resolución de rutas, el impacto cae antes del login.

Archivos clave:

- `App.xaml.cs`
- `Services/GlobalCopyContextService.cs`
- `Services/LogService.cs`
- `Views/LoginView.xaml`

## Paso 3. Inicialización de base de datos

`Db/DatabaseInitializer.cs` es el bootstrapper de persistencia. Hace mucho más que crear el archivo `WegInstaladores.db`.

Responsabilidades confirmadas por código:

1. resuelve `CurrentDbPath` a partir de `CC_SHARED_DATABASE_PATH`; si no existe configuración, usa el directorio de ejecución;
2. crea la carpeta contenedora si falta;
3. intenta migrar archivos legacy (`WegInstallerSystems.db`, `installer_systems.db`, `carros.db`) cuando la base actual todavía no existe;
4. crea tablas `Usuarios`, `Instaladores`, `Administrador` y `PasswordRecoveryLog`;
5. agrega columnas faltantes con `ALTER TABLE` mediante `EnsureColumnExists()`;
6. migra datos desde la tabla legacy `Administradores` a `Administrador`;
7. elimina tablas viejas (`Carros`, `Clientes`, `Empleados`, `Administradores`);
8. normaliza datos heredados, por ejemplo `ADMIN -> ADMINISTRADOR`, `CLIENTE -> VENTAS`, carpeta vacía -> `Desarrollo global`.

La explicación detallada del esquema queda más adelante, en la sección de persistencia, para no cortar el recorrido de arranque.

Puntos delicados:

- no existe versionado de migraciones; la estrategia es programática e idempotente dentro del inicializador;
- las fechas se guardan como texto con formato `yyyy-MM-dd HH:mm:ss`;
- la eliminación del archivo legacy migrado es best-effort: si Windows mantiene el archivo abierto, la app sigue igual.

## Paso 4. Login normal

`LoginViewModel` implementa la autenticación operativa común.

Si necesitás la narrativa funcional de esta pantalla, ver [inicio de sesión de usuario](help://users/user#paso-a-paso-para-iniciar-sesion).

Archivos clave:

- `ViewModels/LoginViewModel.cs`
- `Db/UsuariosDbService.cs`
- `Views/LoginView.xaml`

Flujo real:

1. el usuario ingresa alias o correo en `Usuario`;
2. `UsuariosDbService.ObtenerCorreoPorUsuarioLogin()` intenta resolver el correo real usando correo completo, alias antes de `@`, o relación con el usuario/correo del dispositivo;
3. `UsuariosDbService.Login()` compara `Correo` y `PasswordHash` con `SHA-256` en base64;
4. si autentica, `AbrirSesionUsuario()` asigna `SesionUsuario.UsuarioActual` y fuerza `SesionUsuario.ModoAdministrador = false`;
5. se abre `MainWindow`.

El feature de `Recordarme` no usa `SQLite`. Guarda dos líneas en `%AppData%\ConcesionaroCarros\login.remember` y cifra la password con `ProtectedData` (`DataProtectionScope.CurrentUser`).

Caveats técnicos:

- el login intenta mapear el usuario Windows al correo corporativo; un cambio en ese criterio afecta tanto autenticación como trazabilidad de logs;
- el comentario en `LoginViewModel` ya reconoce datos históricos sucios donde `Nombres == Apellidos`, y limpia ese caso en memoria para no duplicar nombre en la cabecera;
- tener rol `ADMINISTRADOR` en `Usuarios` no habilita por sí solo la UI administrativa cuando se entra por este flujo.

## Paso 5. Login administrativo

`AdminLoginViewModel` define el flujo privilegiado. Este es el punto que realmente habilita el modo administrador del shell principal.

Archivos clave:

- `ViewModels/AdminLoginViewModel.cs`
- `Db/AdministradoresDbService.cs`
- `Db/UsuariosDbService.cs`
- `Services/SesionUsuario.cs`

Flujo real:

1. valida que exista un registro administrativo por `UsuarioSistema` con `ExistePorUsuarioSistema()`;
2. autentica contra la tabla `Administrador` usando `LoginPorUsuarioSistema()`;
3. resuelve el usuario base en `Usuarios` mediante el `Correo` del admin;
4. si el usuario base existe, asegura que su `Rol` sea `ADMINISTRADOR`;
5. asigna `SesionUsuario.UsuarioActual = usuarioNormal` y `SesionUsuario.ModoAdministrador = true`;
6. abre `MainWindow`.

No es un doble login independiente. En realidad, `Administrador` aporta la credencial privilegiada y `Usuarios` sigue siendo la identidad principal sobre la que navega la app.

Si necesitás la narrativa funcional exacta de esta ruta, ver [flujo funcional de login administrativo](help://administradores/administradores#paso-4-iniciar-sesion-por-login-administrativo).

Detalles relevantes:

- `Recordarme` administrativo persiste en `%AppData%\ConcesionaroCarros\login.admin.remember` con cifrado DPAPI;
- si existe admin pero no usuario base asociado, el login falla aunque la password administrativa sea correcta;
- el identificador de entrada es `UsuarioSistema`, no necesariamente el correo.

## Paso 6. Sesión, shell principal y modo administrador

La sesión vive en `Services/SesionUsuario.cs` y se consume sobre todo desde `MainViewModel`.

Estado compartido:

```csharp
public static Usuario UsuarioActual { get; set; }
public static bool ModoAdministrador { get; set; }

public static bool EsAdmin =>
    ModoAdministrador && RolesSistema.EsAdministrador(UsuarioActual?.Rol);
```

Esto define una regla central del sistema: el rol no alcanza; hace falta haber pasado por el flujo administrativo.

`MainViewModel` arma la navegación interna:

- `MostrarInstaladores()` carga `InstaladoresView` como vista por defecto;
- `ShowGestionUsuariosCommand` solo funciona si `EsAdministrador` es `true`;
- `ShowLogsCommand` expone el centro de logs con una restricción específica de soporte/desarrollo; el detalle canónico queda en [Paso 12. Logs y observabilidad local](help://developers/developer#paso-12-logs-y-observabilidad-local);
- `ShowAyudaCommand` carga `HelpViewModel(EsAdministrador)` para filtrar la documentación por perfil;
- `ShowSettingsCommand` carga `SettingsView` con `SettingsViewModel`, fija `VistaActiva = "Configuracion"` y queda expuesto en el menú lateral antes de `Cerrar sesión` según `MainWindow.xaml`;
- `CerrarSesionCommand` limpia la sesión, vuelve a `LoginView` y cierra la ventana actual.

Para la vista funcional de este shell y sus variantes, ver [vista principal del usuario](help://users/user#vista-principal-del-usuario) y [vista principal del administrador](help://administradores/administradores#paso-6-vista-principal-del-administrador).

Archivos clave:

- `Services/SesionUsuario.cs`
- `Services/RolesSistema.cs`
- `MainWindow.xaml.cs`
- `ViewModels/MainViewModel.cs`

## Paso 7. Módulo de instaladores

Este módulo mezcla catálogo, permisos y ejecución de procesos externos.

Archivos clave:

- `ViewModels/InstaladoresViewModel.cs`
- `ViewModels/FormularioInstaladorViewModel.cs`
- `Db/InstaladorDbService.cs`
- `Models/Instalador.cs`
- `Views/InstaladoresView.xaml`

Responsabilidades reales:

1. leer `Instaladores` desde `InstaladorDbService.ObtenerTodos()`;
2. normalizar carpetas a `Punto local` o `Desarrollo global`;
3. si la sesión no es admin, filtrar por las rutas presentes en `SesionUsuario.UsuarioActual.ObtenerAplicativosAsignados()`;
4. permitir alta, edición y baja solo en modo admin;
5. ejecutar el archivo configurado con `Process.Start()` y `UseShellExecute = true`.

El permiso no se calcula por rol, sino por coincidencia exacta de ruta. Si cambia `Instaladores.Ruta` sin actualizar `AplicativosJson`, el aplicativo desaparece para usuarios no administradores aunque siga existiendo en la tabla.

El módulo registra eventos de apertura, edición, eliminación y ejecución en `LogService`.

Si necesitás el comportamiento funcional desde cada perfil, ver [gestión de instaladores](help://administradores/administradores#paso-7-gestion-de-instaladores) y [cómo ejecutar un aplicativo asignado](help://users/user#como-ejecutar-un-aplicativo-asignado).

## Paso 8. Gestión de usuarios y sincronización con administradores

`GestionUsuarioViewModel` es el centro de mantenimiento de usuarios. Además de CRUD, contiene el panel lateral de asignación de aplicativos.

Archivos clave:

- `ViewModels/GestionUsuarioViewModel.cs`
- `ViewModels/FormularioUsuarioViewModel.cs`
- `Db/UsuariosDbService.cs`
- `Db/AdministradoresDbService.cs`

Flujo técnico:

1. carga `Usuarios` desde `UsuariosDbService.ObtenerTodos()`;
2. al seleccionar un usuario, abre el panel de asignación y llena `RolesAsignables` y `AplicativosAsignables`;
3. guarda las rutas seleccionadas serializándolas con `Usuario.EstablecerAplicativosAsignados()`;
4. persiste ese JSON en `Usuarios.AplicativosJson` con `ActualizarAplicativosJson()`;
5. al editar usuarios, sincroniza la tabla `Administrador` con `SincronizarAdministrador()`.

La sincronización con admins tiene dos ramas:

- si el rol sigue siendo administrador, `AdministradoresDbService.SincronizarDesdeUsuario()` actualiza nombres, correo, `UsuarioSistema` y rol sin tocar la password admin existente;
- si deja de ser administrador, elimina el registro de `Administrador` por correo.

También es importante lo que NO hace este módulo: el panel de roles es efectivamente de solo lectura (`SeleccionarRol()` está vacío y `EsEditable = false`), por lo que la asignación interactiva vigente es la de aplicativos, no una administración completa de roles desde ese panel.

La referencia funcional correspondiente está en [gestión de usuarios](help://administradores/administradores#paso-8-gestion-de-usuarios) y [asignación de aplicativos](help://administradores/administradores#paso-9-asignacion-de-aplicativos).

## Paso 9. Registro de usuarios y registro administrativo

Los formularios de alta inicial viven fuera de `GestionUsuarioViewModel`, en los flujos de login.

### Registro normal

`RegisterViewModel`:

- exige correo `@weg.net`;
- intenta derivar nombre y apellido desde el perfil Windows o desde el alias del correo;
- crea usuarios con rol por defecto `VENTAS`;
- tras registrar, vuelve a `LoginView` con credenciales precargadas, pero no activa `Recordarme` automáticamente.

### Registro administrativo

`AdminRegisterViewModel`:

- también exige dominio `@weg.net`;
- crea o actualiza primero el `Usuario` base en `Usuarios`;
- luego inserta o actualiza la fila en `Administrador` con `GuardarOActualizar()`;
- usa `UsuarioSistema` derivado del correo o del usuario Windows, según corresponda al equipo actual.

Esa doble escritura explica por qué la tabla `Administrador` no reemplaza a `Usuarios`: la complementa.

Para el recorrido funcional de alta, ver [registro de usuario](help://users/user#registro-de-usuario) y [registro administrativo](help://administradores/administradores#paso-3-registro-administrativo).

## Paso 10. Recuperación de contraseña

La recuperación vive en `MicrosoftRecoveryViewModel`, aunque hoy no hay integración real con Microsoft Entra o Graph.

Archivos clave:

- `ViewModels/MicrosoftRecoveryViewModel.cs`
- `Views/MicrosoftRecoveryView.xaml`
- `Views/RecoveryCodePopupView.xaml`
- `Db/UsuariosDbService.cs`

Flujo real:

1. valida formato de correo y el checkbox `NoSoyRobot`;
2. busca el usuario por correo;
3. genera un código aleatorio de 6 dígitos con `RandomNumberGenerator`;
4. muestra el código en la UI por `ShowCodeRequested`, no por correo externo;
5. al confirmar, actualiza `Usuarios.PasswordHash`;
6. registra la operación en `PasswordRecoveryLog` con `ValidadoMicrosoft = false`.

El nombre del módulo sugiere una validación Microsoft que hoy está desactivada en la práctica. La propia configuración `CC_REQUIRE_MICROSOFT_EMAIL_VALIDATION=false` y el log grabado con `false` lo dejan claro.

La guía funcional de esta pantalla está en [recuperación de contraseña](help://users/user#recuperacion-de-contrasena).

## Paso 11. Centro de ayuda y documentación embebida

La ayuda no es texto estático pegado en una vista. Hay un subsistema de carga y render de markdown.

Archivos clave:

- `Services/DocumentationService.cs`
- `Services/MarkdownDocumentRenderer.cs`
- `ViewModels/HelpViewModel.cs`
- `Views/HelpView.xaml`

`DocumentationService` resuelve el root `Docs` desde el directorio de ejecución y registra documentos por `docId`, por ejemplo:

- `developers/developer`
- `developers/base-de-datos`
- `administradores/administradores`
- `users/user`

`HelpViewModel` usa ese registro para:

1. filtrar documentos según `EsAdministrador`;
2. mantener historial interno por documento y anchor;
3. mostrar una URI canónica tipo `help://...`;
4. recordar posición de scroll y anchors navegados.

`MarkdownDocumentRenderer` convierte markdown a `FlowDocument` y soporta:

- headings con anchors normalizados;
- listas y bloques de código;
- imágenes locales;
- hyperlinks markdown, incluidos enlaces `help://` entre documentos.

Esto significa que cambiar títulos o anchors en un `.md` puede romper navegación interna aunque el archivo siga existiendo.

Para el uso funcional de la ayuda según perfil, ver [cómo usar la pestaña de ayuda](help://users/user#como-usar-la-pestana-de-ayuda) y [uso de la pestaña ayuda para administradores](help://administradores/administradores#paso-10-uso-de-la-pestana-ayuda).

### Configuración visual y efecto sobre la documentación

La preferencia visual no forma parte del estado de negocio ni de `SQLite`. `ThemeManager` se inicializa en `App.OnStartup()` antes del login, resuelve la preferencia efectiva (`System`, `Light`, `Dark`), reemplaza el diccionario activo en `Application.Resources.MergedDictionaries` y persiste el valor elegido en `Properties.Settings.Default.ThemePreference`.

Archivos clave:

- `Services/ThemeManager.cs`
- `Views/SettingsView.xaml`
- `ViewModels/SettingsViewModel.cs`
- `Themes/Light.xaml`
- `Themes/Dark.xaml`
- `Properties/Settings.settings`

Implicación técnica:

1. `SettingsViewModel` no guarda nada en base; expone `UseLightThemeCommand` y `UseDarkThemeCommand`, delega en `ThemeManager.ApplyThemePreference()` y refresca el estado derivado (`IsLightSelected`, `IsDarkSelected`, `ResolvedThemeMode`).
2. `SettingsView` es solo la superficie WPF para esa preferencia; los recursos visuales concretos salen de `Themes/Light.xaml` y `Themes/Dark.xaml`, que definen brushes y gradientes consumidos por la UI vía `DynamicResource`.
3. La ayuda embebida también queda afectada. `MarkdownDocumentRenderer` y `HelpView` resuelven `MarkdownRenderTheme.CreateForCurrentSystemTheme()`, que a su vez consulta `ThemeManager.CurrentPreference`; por eso la documentación renderizada cambia de paleta junto con el resto de la app aunque el markdown no cambie.
4. La referencia funcional principal para esta pantalla, incluido el orden visible en el menú lateral y el cambio de tema, está en [Configuración](help://users/user#configuracion).

## Paso 12. Logs y observabilidad local

El logging sigue centralizado en `LogService`, pero la lectura de métricas actual ya no es una lista plana de eventos. El flujo real queda repartido entre `LogDashboardService` (agregación), `LogsViewModel` (estado/filtros/refresco) y `LogsView.xaml` (render + hover + drill-down).

Archivos clave:

- `Services/LogService.cs`
- `Services/LogDashboardService.cs`
- `Services/AppLogEntry.cs`
- `ViewModels/LogsViewModel.cs`
- `Views/LogsView.xaml`
- `Views/LogsView.xaml.cs`

### Restricción developer-only confirmada por código

Estas métricas NO están pensadas para usuarios finales ni para cualquier admin genérico. En el código actual quedan expuestas solo dentro del centro de logs, y ese centro solo abre cuando se cumplen ambas condiciones:

1. `SesionUsuario.EsAdmin` es `true`;
2. el correo de sesión está hardcodeado en `MainViewModel.AllowedLogViewerEmails` (`wandica@weg.net`, `maicolj@weg.net`).

Además, `LogsViewModel` aplica un contexto inicial especial solo si `LogService.ResolveCurrentAuditUserName()` coincide con `DeveloperUsers` (`wandica`, `maicolj`): busca el último evento visible de ese developer y preselecciona su equipo. O sea: la pantalla completa ya está restringida a soporte/desarrollo, y la experiencia por defecto además está sesgada a esos developers hardcodeados.

### Fuente de datos y formato real

1. `LogService.Write()` persiste líneas tabuladas con 8 columnas: timestamp, nivel, equipo, usuario, source, `DurationMs`, mensaje y detalles.
2. La ruta primaria sale de `CC_SHARED_DATABASE_PATH`: toma el directorio de la base, sube un nivel y usa `Logs`. Si falla la escritura, cae a `%LocalAppData%\SistemaDeInstalacion\LogsFallback`.
3. La estructura física es `Logs/<Machine>/<yyyy-MM-dd>/events.log`.
4. `LogDashboardService` lee ambos roots retornados por `LogService.GetReadableLogsDirectories()` y parsea cada `.log` en `AppLogEntry` con `ParseFile()`.
5. Las métricas semánticas no salen de `SQLite`. Salen del texto de `Details`, donde el parser busca tokens `key=value` separados por `|` como `event=validation`, `accepted=false`, `signal=heartbeat`, `dependency=sql`, `state=degraded` o `interval_minutes=5`.
6. Hay compatibilidad hacia atrás para logs viejos en español: `GetSemanticValue()` mapea `evento=` hacia `event=`.

### Flujo técnico de snapshot y filtros

1. `LogsViewModel` arma un `LogDashboardQuery` con equipo, rango, severidad, módulo, usuario y texto libre.
2. `LogDashboardService.GetDashboardSnapshot()` primero carga `baseEntries` solo por equipo + ventana temporal, y recién después aplica filtros adicionales para producir `filteredEntries`.
3. Esa separación importa: la cobertura e inferencia health usan `baseEntries`, mientras que los contadores visibles, distribuciones y grilla usan `filteredEntries`.
4. El modo `realtime` fija `SelectedTimeRangeKey = "2h"`; el modo `history` restaura el último rango histórico elegido.
5. En tiempo real, `LogsViewModel` se suscribe a `LogService.LogWritten`, debouncing de `150 ms`, y además mantiene auto-refresh cada `2 s`. No consulta DB ni servicios remotos: vuelve a pedir un snapshot al agregador.

### Métricas implementadas hoy

1. Tarjetas superiores desde `LogDashboardSummary`: eventos visibles (`TotalEvents`), errores (`ErrorCount`), advertencias (`WarningCount`) y P95 (`P95LatencyMs`). El promedio (`AverageLatencyMs`) también se calcula y se reutiliza en secciones narrativas.
2. Estado ejecutivo desde `BuildExecutiveStatus()`: cobertura observable `0..5` sobre cinco señales base (`health`, `heartbeat`, `dependencies`, `session`, `validation`) más etiquetas y detalles de última señal, heartbeat, dependencias e incidentes.
3. Secciones narrativas desde `BuildStatusSections()`: `incidents`, `validations`, `latency`, `activity` y `health`. Cada una expone `Summary`, `Detail`, `Facts`, `TimelineSegments` y `TrendCells`.
4. Distribuciones por actividad desde `BuildDistribution()`: top 6 por `Source`, `UserName` y `MachineName`, siempre calculadas sobre `filteredEntries`.
5. Distribución de latencia desde `BuildLatencyDistribution()`: bandas `< 250 ms`, `250-499 ms`, `500-999 ms`, `1-2 s` y `>= 2 s`, usando solo entradas con `DurationMs`.
6. Timeline de incidentes desde `BuildCriticalEvents()`: últimos 8 eventos críticos visibles (`ERROR`, `WARNING`, `VALIDATION` rechazada, incidentes/degradaciones `HEALTH`).
7. La instrumentación visible (`InstrumentationStatus`) no inventa salud: cuando faltan señales, el dashboard muestra explícitamente gaps de cobertura en vez de fabricar uptime.

Detalle importante del estado actual: el snapshot todavía expone `ErrorSeries` y `WarningSeries`, pero `LogsView.xaml` ya no bindéa esas colecciones de forma directa. La UI actual prioriza `StatusSections`, distribuciones, hover contextual, timeline de incidentes y grilla filtrada.

### Interacción UI -> ViewModel -> Service

1. `LogsView.xaml` bindea las colecciones del snapshot aplicado por `LogsViewModel`: tarjetas, filtros, `StatusSections`, distribuciones, timeline y `FilteredEntries`.
2. Los bloques de `SourceActivity`, `UserActivity` y `MachineActivity` son botones: click aplica filtros contextuales (`ApplySourceFilterCommand`, `ApplyUserFilterCommand`, `ApplyMachineFilterCommand`).
3. Los bloques de latencia no hacen drill-down por click; solo disparan hover informativo.
4. `LogsView.xaml.cs` controla sesiones de hover persistente y delega a `LogsViewModel.PreviewMetricHover()`, `PreviewFactHover()` y `PreviewNarrativeSegment()` para construir la `DashboardHoverCard`.
5. El drill-down de timeline usa `ApplyIncidentDrillDown()` y `ApplyNarrativeSegmentDrillDown()`, que sincronizan la lista de incidentes y la grilla inferior con el segmento seleccionado sin tocar la persistencia.

El subsistema no depende de `SQLite`, pero sí de escritura/lectura a disco. Si el share principal falla, sigue logueando localmente y el dashboard intenta leer tanto el root primario como el fallback.

## Persistencia y modelo de datos

La app trabaja sobre cuatro tablas activas: `Usuarios`, `Instaladores`, `Administrador` y `PasswordRecoveryLog`.

Lo importante para desarrollo no es solo el esquema, sino las relaciones implícitas:

- `Administrador` se vincula con `Usuarios` por `Correo`;
- `Usuarios` se vincula con `Instaladores` por rutas guardadas como JSON en `AplicativosJson`;
- `PasswordRecoveryLog` se vincula con `Usuarios` por `UsuarioId` y también por `CorreoUsuario` para limpieza.

Para detalle de tablas, normalizaciones y consultas de soporte, ver [Base de datos](help://developers/base-de-datos).

## Configuración relevante

`App.config` define configuraciones que impactan directamente en la ejecución:

| Key | Uso real |
|---|---|
| `CC_SHARED_DATABASE_PATH` | Path efectivo de la base; también condiciona el root principal de logs. |
| `CC_CORPORATE_EMAIL_DOMAIN` | Declara dominio corporativo, aunque varias validaciones siguen hardcodeadas a `@weg.net` en view models. |
| `CC_REQUIRE_MICROSOFT_EMAIL_VALIDATION` | Hoy está en `false`; el flujo de recuperación opera sin integración Microsoft real. |
| `CC_AZURE_TENANT_ID`, `CC_AZURE_CLIENT_ID`, `CC_AZURE_CLIENT_SECRET` | Reservados para una integración futura; no sostienen el flujo actual de recuperación. |

Además, `SistemaDeInstalacion.csproj` contiene configuración de publicación hacia un recurso UNC corporativo y firma de manifiestos. No es solo metadata decorativa: forma parte del modelo de entrega actual.

## Testing, build y entrega

### Testing respaldado por repo

Hay un proyecto `SistemaDeInstalacion.Tests` con MSTest (`Microsoft.NET.Test.Sdk`, `MSTest.TestAdapter`, `MSTest.TestFramework`). La cobertura actual se concentra en lógica no visual:

- `DatabaseInitializerTests.cs`: creación de base, migración legacy y normalización de datos;
- `UsuariosDbServiceTests.cs`: registro, login, cambio de password, aplicativos asignados y eliminación con dependencias;
- `AdministradoresDbServiceTests.cs`: login admin, actualización y sincronización;
- `RolesSistemaTests.cs`: lista de roles y exclusión de admin en roles asignables.

`TestWorkspace.cs` aísla cada test limpiando bases conocidas dentro del directorio de ejecución del runner.

### Build y solución

La solución versionada es `SistemaDeInstalacion.sln` e incluye tres proyectos:

1. `SistemaDeInstalacion`
2. `SistemaDeInstalacion.Tests`
3. `LauncherSistema`

No se verificó compilación en esta revisión. Este documento solo se apoyó en lectura de código y configuración.

### Entrega

La entrega actual combina dos piezas:

1. publicación de `SistemaDeInstalacion` al share corporativo configurado en el `.csproj`;
2. distribución/apertura vía `LauncherSistema`, que chequea versión y ejecuta `SetupSistema.exe`.

Eso explica por qué el launcher y la app principal deben leerse como un producto único, aunque estén en proyectos separados.

## Riesgos técnicos y deuda real

1. `SHA-256` directo para passwords en `UsuariosDbService` y `AdministradoresDbService` mejora contra texto plano, pero sigue siendo débil frente a `PBKDF2`, `bcrypt` o `Argon2`.
2. `SesionUsuario` es estado global estático. Simplifica la UI, pero acopla fuertemente vistas y view models y dificulta pruebas integradas.
3. Los permisos de aplicativos dependen de rutas serializadas en JSON. No hay integridad referencial entre usuario e instalador.
4. `CC_CORPORATE_EMAIL_DOMAIN` existe en configuración, pero `RegisterViewModel` y `AdminRegisterViewModel` validan `@weg.net` de forma hardcodeada. Hay duplicación de regla.
5. El nombre `MicrosoftRecoveryViewModel` hoy sobrepromete: el flujo real es local, con código visual y sin validación Microsoft efectiva.
6. La solución contiene una segunda copia de documentación bajo `LauncherSistema/Docs`. Si ambas se editan por separado, la ayuda puede derivar en contenido divergente según artefacto o empaquetado.
7. El `RootNamespace` del `.csproj` es `SistemaDeInstalacion`, pero el código principal usa namespace `ConcesionaroCarros`. No rompe por sí mismo, pero es una inconsistencia histórica que complica lectura y tooling.

## Referencias técnicas

- [WPF data binding](https://learn.microsoft.com/dotnet/desktop/wpf/data/)
- [FlowDocument](https://learn.microsoft.com/dotnet/api/system.windows.documents.flowdocument)
- [Hyperlink en WPF](https://learn.microsoft.com/dotnet/api/system.windows.documents.hyperlink)
- [Microsoft.Data.Sqlite](https://learn.microsoft.com/dotnet/standard/data/sqlite/)
- [SQLite `PRAGMA busy_timeout`](https://www.sqlite.org/pragma.html#pragma_busy_timeout)
- [MSTest](https://learn.microsoft.com/dotnet/core/testing/unit-testing-mstest)
