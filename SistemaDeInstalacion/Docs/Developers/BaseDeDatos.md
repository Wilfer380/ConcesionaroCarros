# BaseDeDatos

Esta guía documenta la capa real de persistencia de `SistemaDeInstalacion` según el código actual en `Db/`, `ViewModels/`, `App.config` y `SistemaDeInstalacion.Tests/`.

Se complementa con:

- [Developer](help://developers/developer)
- [Administradores](help://administradores/administradores)

## Stack real

- proveedor: `Microsoft.Data.Sqlite` `10.0.2` en `SistemaDeInstalacion.csproj`;
- motor: `SQLite` sobre archivo local/compartido;
- runtime: `.NET Framework 4.8`;
- acceso a datos: SQL manual con `SqliteConnection`, `SqliteCommand` y parámetros `$nombre`;
- ORM/migrador: no existe.

## Archivo y configuración efectiva

La ruta activa se resuelve en `Db/DatabaseInitializer.cs`:

- archivo por defecto: `WegInstaladores.db`;
- clave de configuración: `CC_SHARED_DATABASE_PATH`;
- si la clave está vacía, la base cae en `AppDomain.CurrentDomain.BaseDirectory`;
- si la clave tiene variables de entorno, se expanden con `Environment.ExpandEnvironmentVariables()`;
- si la clave es relativa, se vuelve absoluta tomando como base el directorio de ejecución;
- si la clave es absoluta, se usa tal cual.

En `App.config` hoy la clave apunta a un UNC compartido:

```xml
<add key="CC_SHARED_DATABASE_PATH" value="\\comde019\DFSMDE\PUBLIC\CO_MDE_DISENO_DI\RESPALDO DISEÑOS\SAP - Respaldo diseños\FORMATOS SAP\InstallerSystem\BD\WegInstaladores.db" />
```

La cadena de conexión real es solo:

```text
Data Source={CurrentDbPath}
```

No hay `journal_mode`, `foreign_keys`, `synchronous`, `cache`, ni otras opciones en connection string.

Nota técnica: la preferencia visual (`ThemePreference`) no se persiste en `SQLite`. Vive como configuración local por perfil Windows en `Properties/Settings.settings`, se resuelve en runtime mediante `ThemeManager` y queda fuera del modelo relacional. La referencia funcional principal para Configuración y cambio de tema está en [Configuración](help://users/user#configuracion).

## Bootstrap real

`App.xaml.cs` ejecuta `DatabaseInitializer.Initialize()` en `OnStartup()` antes de abrir `LoginView`.

Flujo real:

```text
App.OnStartup()
  -> DatabaseInitializer.Initialize()
     -> crea directorio de la base si falta
     -> si WegInstaladores.db no existe, copia una base legacy si encuentra una
     -> abre SQLite
     -> crea tablas vigentes con CREATE TABLE IF NOT EXISTS
     -> agrega columnas faltantes con ALTER TABLE
     -> migra datos de Administradores -> Administrador
     -> elimina tablas legacy
     -> normaliza datos heredados
```

## Migraciones y alteraciones manuales

No existe versionado formal de esquema. La migración es programática e idempotente dentro de `DatabaseInitializer.Initialize()`.

### Archivos legacy que todavía se absorben

Si `CurrentDbPath` no existe, el bootstrap intenta copiar uno de estos nombres desde el mismo directorio:

- `WegInstallerSystems.db`
- `installer_systems.db`
- `carros.db`

Después, intenta borrar el archivo legacy migrado en un `try/catch` best effort.

### Estrategia de evolución de esquema

La app hace cambios manuales con `ALTER TABLE` usando `EnsureColumnExists()` y `PRAGMA table_info()`.

Columnas agregadas por compatibilidad:

- `Usuarios.FotoPerfil`
- `Usuarios.AplicativosJson`
- `Instaladores.Nombre`
- `Instaladores.Descripcion`
- `Instaladores.Carpeta`
- todas las columnas actuales de `Administrador`
- todas las columnas actuales de `PasswordRecoveryLog`

Punto importante: `Instaladores` se crea inicialmente con `Id`, `Ruta`, `Carpeta`, `FechaRegistro`, y luego completa `Nombre` y `Descripcion` por alteración. O sea: el esquema actual depende de bootstrap + alteraciones, no solo del `CREATE TABLE` inicial.

### Tablas legacy eliminadas

El bootstrap ejecuta:

```sql
DROP TABLE IF EXISTS Carros;
DROP TABLE IF EXISTS Clientes;
DROP TABLE IF EXISTS Empleados;
DROP TABLE IF EXISTS Administradores;
```

Además, si existe la tabla legacy `Administradores`, copia sus filas a `Administrador` con `INSERT OR IGNORE ... SELECT ...` antes del borrado.

### Normalizaciones automáticas de datos

En cada arranque se aplican estas correcciones:

- `Usuarios.AplicativosJson` vacío o `NULL` se fuerza a `'[]'`;
- `Usuarios.Rol = CLIENTE` se migra a `VENTAS`;
- `Usuarios.Rol = INGENIERO` se migra a `INGENIERIA`;
- `Usuarios.Rol = ADMIN` se migra a `ADMINISTRADOR`;
- `Instaladores.Carpeta` vacía o `NULL` se migra a `Desarrollo global`.

## Pragmas y concurrencia

Solo hay un pragma explícito en código:

```sql
PRAGMA busy_timeout = 5000;
```

Se aplica al abrir conexión en:

- `Db/UsuariosDbService.cs`
- `Db/AdministradoresDbService.cs`

No se aplica en `Db/InstaladorDbService.cs`.

Además, `UsuariosDbService` implementa reintentos manuales para `SqliteErrorCode == 5` con hasta 3 intentos y espera incremental `120ms`, `240ms`, `360ms`.

`AdministradoresDbService` no tiene reintentos manuales.

`InstaladorDbService` no tiene ni `busy_timeout` ni reintentos.

## Tablas reales y rol

### `Usuarios`

Definición vigente:

```sql
CREATE TABLE IF NOT EXISTS Usuarios (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Nombres TEXT NOT NULL,
    Apellidos TEXT NOT NULL,
    Correo TEXT NOT NULL UNIQUE,
    Telefono TEXT,
    PasswordHash TEXT NOT NULL,
    Rol TEXT NOT NULL,
    FechaRegistro TEXT NOT NULL,
    FotoPerfil TEXT,
    AplicativosJson TEXT DEFAULT '[]'
);
```

Rol real:

- identidad principal del sistema;
- autenticación normal;
- rol funcional de la UI;
- almacenamiento de foto de perfil por ruta;
- almacenamiento de aplicativos asignados como JSON de rutas.

Observaciones:

- `Correo` es el único `UNIQUE` explícito;
- `FechaRegistro` se guarda como `TEXT` con formato `yyyy-MM-dd HH:mm:ss`;
- `AplicativosJson` reemplaza una relación relacional real entre usuario e instalador.

### `Instaladores`

Definición funcional final, después del bootstrap:

```sql
CREATE TABLE IF NOT EXISTS Instaladores (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Ruta TEXT,
    Carpeta TEXT,
    FechaRegistro TEXT,
    Nombre TEXT,
    Descripcion TEXT
);
```

Rol real:

- catálogo de ejecutables lanzables desde la app;
- clasificación por carpeta funcional: `Desarrollo global` o `Punto local de desarrollo planta`;
- fuente de asignación de aplicativos para usuarios no admin.

Observaciones:

- no hay `UNIQUE` sobre `Ruta`;
- el código trata `Ruta` como identificador funcional para actualizar y eliminar;
- si `Nombre` es `NULL`, el servicio deriva el nombre desde `Path.GetFileNameWithoutExtension(Ruta)`.

### `Administrador`

```sql
CREATE TABLE IF NOT EXISTS Administrador (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Nombres TEXT NOT NULL,
    Apellidos TEXT,
    Correo TEXT NOT NULL UNIQUE,
    UsuarioSistema TEXT NOT NULL,
    Rol TEXT NOT NULL,
    PasswordAdminHash TEXT NOT NULL,
    FechaRegistro TEXT NOT NULL
);
```

Rol real:

- credencial privilegiada separada del login normal;
- resolución del usuario administrativo por `UsuarioSistema`;
- puente hacia `Usuarios` por `Correo`.

Observaciones:

- la UI admin NO navega con este registro solo;
- después del login admin, la app busca el usuario base en `Usuarios` usando `admin.Correo`.

### `PasswordRecoveryLog`

```sql
CREATE TABLE IF NOT EXISTS PasswordRecoveryLog (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UsuarioId INTEGER NOT NULL,
    CorreoUsuario TEXT NOT NULL,
    CorreoAdministrador TEXT,
    ValidadoMicrosoft INTEGER NOT NULL DEFAULT 0,
    FechaRecuperacion TEXT NOT NULL
);
```

Rol real:

- auditoría de reseteo de contraseña;
- registro de si hubo validación Microsoft o no.

Observaciones:

- hoy `MicrosoftRecoveryViewModel` siempre inserta `ValidadoMicrosoft = false` y `CorreoAdministrador = null`;
- no existe `FOREIGN KEY` contra `Usuarios`.

## Servicios DB y responsabilidades

### `Db/DatabaseInitializer.cs`

Responsabilidades:

- resolver ruta efectiva de la base;
- crear directorios;
- copiar base legacy si corresponde;
- asegurar tablas y columnas;
- migrar `Administradores` -> `Administrador`;
- eliminar tablas viejas;
- normalizar datos heredados.

### `Db/UsuariosDbService.cs`

Responsabilidades:

- alta de usuarios;
- login por correo + password;
- lookup por correo, alias o usuario de dispositivo;
- listado completo;
- actualización de datos básicos;
- actualización de password;
- actualización de `FotoPerfil`;
- actualización de `AplicativosJson`;
- borrado simple o con limpieza de `PasswordRecoveryLog`;
- registro de recuperaciones de password.

Detalle técnico:

- hash de password: `SHA256` + `Base64`;
- lecturas y escrituras parametrizadas;
- reintentos solo para bloqueo SQLite.

### `Db/AdministradoresDbService.cs`

Responsabilidades:

- alta o actualización de credencial admin por correo;
- login admin por `UsuarioSistema` + password admin;
- verificación de existencia por `UsuarioSistema`;
- borrado por correo;
- sincronización de datos de admin cuando cambia el `Usuario` base.

Detalle técnico:

- hash de password admin: `SHA256` + `Base64`;
- `GuardarOActualizar()` decide insert/update consultando antes por `Correo`.

### `Db/InstaladorDbService.cs`

Responsabilidades:

- alta de instaladores;
- edición por `Ruta`;
- listado ordenado por `Id DESC`;
- borrado por `Ruta`.

Detalle técnico:

- normaliza carpeta vacía a `Desarrollo global`;
- no aplica timeout ni reintentos ante bloqueo.

## Flujos reales desde ViewModels hasta DB

### 1. Arranque

`App.OnStartup()` -> `DatabaseInitializer.Initialize()` -> base lista -> `LoginView`.

### 2. Registro normal

`RegisterViewModel`:

1. valida correo `@weg.net`;
2. arma `Usuario` con datos derivados del correo o del perfil Windows;
3. consulta `UsuariosDbService.ObtenerTodos()` para detectar duplicado de forma preventiva;
4. persiste con `RegistrarYRetornarId()`;
5. deriva a `LoginView` con prefill.

### 3. Login normal

`LoginViewModel`:

1. resuelve correo real con `ObtenerCorreoPorUsuarioLogin()`;
2. autentica con `UsuariosDbService.Login()`;
3. guarda `SesionUsuario.UsuarioActual`;
4. abre `MainWindow`.

Persistencia asociada pero fuera de SQLite:

- `Recordarme` usa `%AppData%\ConcesionaroCarros\login.remember`;
- password cifrada con `ProtectedData`.

### 4. Registro administrativo

`AdminRegisterViewModel`:

1. valida correo y rol;
2. crea o actualiza primero el usuario base en `Usuarios`;
3. crea o actualiza después la credencial en `Administrador` con `GuardarOActualizar()`;
4. abre `AdminLoginView` con prefill.

Es una doble escritura deliberada: `Administrador` complementa a `Usuarios`, no lo reemplaza.

### 5. Login administrativo

`AdminLoginViewModel`:

1. valida existencia con `AdministradoresDbService.ExistePorUsuarioSistema()`;
2. autentica con `LoginPorUsuarioSistema()`;
3. busca el usuario base con `UsuariosDbService.ObtenerPorCorreo(admin.Correo)`;
4. si existe, activa `SesionUsuario.ModoAdministrador = true` y navega con ese usuario base.

Persistencia asociada pero fuera de SQLite:

- `%AppData%\ConcesionaroCarros\login.admin.remember`;
- password admin cifrada con `ProtectedData`.

### 6. Gestión de usuarios

`GestionUsuarioViewModel`:

- carga grilla con `UsuariosDbService.ObtenerTodos()`;
- elimina usuario con doble operación: `AdministradoresDbService.EliminarPorCorreo()` + `UsuariosDbService.EliminarConDependencias()`;
- arma panel de asignación leyendo `InstaladorDbService.ObtenerTodos()`;
- persiste aplicativos seleccionados en `Usuarios.AplicativosJson` con `ActualizarAplicativosJson()`;
- al editar, sincroniza admin por correo con `SincronizarDesdeUsuario()` o elimina el admin si el rol deja de ser administrativo.

### 7. Formulario de usuario

`FormularioUsuarioViewModel`:

- alta: `UsuariosDbService.Registrar()`;
- edición: `Actualizar()` y opcionalmente `ActualizarPassword()`.

### 8. Gestión de instaladores

`FormularioInstaladorViewModel` y `InstaladoresViewModel`:

- alta: `InstaladorDbService.Guardar()`;
- edición: `InstaladorDbService.Actualizar()` por `Ruta`;
- eliminación: `EliminarRuta()`;
- listado: `ObtenerTodos()`.

Para usuarios no admin, `InstaladoresViewModel` filtra en memoria contra las rutas presentes en `SesionUsuario.UsuarioActual.AplicativosJson`.

### 9. Recuperación de contraseña

`MicrosoftRecoveryViewModel`:

1. busca usuario por correo con `ObtenerPorCorreo()`;
2. si valida el código visual, actualiza password con `ActualizarPassword()`;
3. registra auditoría en `PasswordRecoveryLog` con `RegistrarLogRecuperacionPassword()`.

No hay validación Microsoft real en la persistencia observada. Solo queda trazado el booleano.

## Relaciones reales

No hay claves foráneas. Las relaciones son lógicas y mantenidas por código:

```text
Usuarios
  -> Administrador        por Correo
  -> PasswordRecoveryLog  por UsuarioId y CorreoUsuario
  -> Instaladores         por rutas serializadas en AplicativosJson
```

## Seguridad real de datos

- `Usuarios.PasswordHash` y `Administrador.PasswordAdminHash` usan `SHA-256` simple en Base64;
- no hay salt por usuario;
- no hay `PBKDF2`, `bcrypt` ni `Argon2`;
- `Recordarme` no usa SQLite: usa archivos en `%AppData%` con DPAPI;
- las consultas observadas están parametrizadas, así que el riesgo dominante no es SQL injection sino diseño del modelo y concurrencia.

## Tests existentes

Sí, hay cobertura de persistencia en `SistemaDeInstalacion.Tests/`.

Archivos relevantes:

- `DatabaseInitializerTests.cs`: creación de base, migración legacy, normalización y borrado de archivo viejo;
- `UsuariosDbServiceTests.cs`: registro, login, duplicados, cambio de password, `AplicativosJson`, limpieza de `PasswordRecoveryLog`;
- `AdministradoresDbServiceTests.cs`: guardado, login, eliminación y sincronización desde usuario;
- `InstaladorDbServiceTests.cs`: alta, edición, eliminación, normalización de carpeta y nombre derivado desde ruta.

Los tests usan `TestWorkspace`, limpian archivos `WegInstaladores.db` y legacies en el directorio de salida, y validan el esquema contra una base SQLite real de prueba.

## Rutas y archivos relevantes

- `SistemaDeInstalacion/Db/DatabaseInitializer.cs`
- `SistemaDeInstalacion/Db/UsuariosDbService.cs`
- `SistemaDeInstalacion/Db/AdministradoresDbService.cs`
- `SistemaDeInstalacion/Db/InstaladorDbService.cs`
- `SistemaDeInstalacion/App.xaml.cs`
- `SistemaDeInstalacion/App.config`
- `SistemaDeInstalacion/SistemaDeInstalacion.Tests/`

Rutas de persistencia no SQLite pero conectadas al flujo de acceso:

- `%AppData%\ConcesionaroCarros\login.remember`
- `%AppData%\ConcesionaroCarros\login.admin.remember`

## Capturas y assets vinculados a DB

No encontré capturas ni assets específicos de base de datos referenciados por `Docs/Developers/BaseDeDatos.md`.

Sí existen imágenes en:

- `Docs/Administradores/`
- `Docs/users/`

Pero están ligadas a flujos de UI y ayuda, no a estructura de base, migraciones ni inspección de SQLite.

## Riesgos y deuda técnica concreta

- no hay migraciones versionadas ni historial de schema;
- no hay transacciones explícitas para operaciones compuestas;
- no hay `FOREIGN KEY`, así que la integridad depende del código;
- `Instaladores.Ruta` actúa como identificador funcional sin `UNIQUE`;
- `Usuarios.AplicativosJson` serializa relaciones como texto, lo que rompe integridad y dificulta consultas;
- `InstaladorDbService` no usa `PRAGMA busy_timeout` ni reintentos, a diferencia de los otros servicios;
- passwords con `SHA-256` simple: mejor que texto plano, pero insuficiente para credenciales reales;
- la base puede vivir en un share UNC configurado, pero la app sigue siendo una app desktop sin estrategia robusta multiusuario;
- `PasswordRecoveryLog.ValidadoMicrosoft` existe, pero el flujo observado persiste siempre `false`;
- existe soporte de `FotoPerfil` en esquema y servicio, pero no apareció un flujo de escritura desde UI en esta auditoría.

## Checklist para tocar esta capa

Antes de cambiar persistencia:

1. actualizar `DatabaseInitializer.Initialize()` y sus `EnsureColumnExists()`;
2. revisar impacto en `UsuariosDbService`, `AdministradoresDbService` e `InstaladorDbService`;
3. revisar ViewModels que dependen del shape actual de datos;
4. ajustar tests en `SistemaDeInstalacion.Tests`;
5. actualizar esta guía con evidencia del código real.

## Referencias externas

- [SQLite Documentation](https://sqlite.org/docs.html)
- [PRAGMA busy_timeout](https://sqlite.org/pragma.html#pragma_busy_timeout)
- [Microsoft.Data.Sqlite](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/)
