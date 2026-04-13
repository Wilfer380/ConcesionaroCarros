# Database

This guide documents the real persistence layer of `SistemaDeInstalacion` according to the current code in `Db/`, `ViewModels/`, `App.config`, and `SistemaDeInstalacion.Tests/`.

It complements:

- [Developer](help://developers/developer)
- [Administrators](help://administradores/administradores)

## Real stack

- provider: `Microsoft.Data.Sqlite` `10.0.2` in `SistemaDeInstalacion.csproj`;
- engine: `SQLite` over a local/shared file;
- runtime: `.NET Framework 4.8`;
- data access: manual SQL with `SqliteConnection`, `SqliteCommand`, and parameterized queries;
- ORM / migration tool: none.

## Effective file and configuration

The active path is resolved in `Db/DatabaseInitializer.cs`:

- default file: `WegInstaladores.db`;
- configuration key: `CC_SHARED_DATABASE_PATH`;
- if the key is empty, the database falls back to `AppDomain.CurrentDomain.BaseDirectory`;
- if the key contains environment variables, they are expanded with `Environment.ExpandEnvironmentVariables()`;
- if the key is relative, it becomes absolute taking the execution directory as base;
- if the key is absolute, it is used as-is.

The real connection string is only:

```text
Data Source={CurrentDbPath}
```

There is no connection-string level configuration for `journal_mode`, `foreign_keys`, `cache`, or other advanced SQLite options.

Technical note: the visual preference (`ThemePreference`) is not persisted in `SQLite`. It lives as a local setting per Windows profile in `Properties/Settings.settings`, is resolved at runtime through `ThemeManager`, and stays outside the relational model. The main functional reference for settings and theme change is [Settings](help://users/user#settings).

## Real bootstrap

`App.xaml.cs` executes `DatabaseInitializer.Initialize()` inside `OnStartup()` before opening `LoginView`.

Real flow:

```text
App.OnStartup()
  -> DatabaseInitializer.Initialize()
     -> create database directory if missing
     -> if WegInstaladores.db does not exist, copy a legacy database if one is found
     -> open SQLite
     -> create current tables with CREATE TABLE IF NOT EXISTS
     -> add missing columns with ALTER TABLE
     -> migrate data from Administradores -> Administrador
     -> remove legacy tables
     -> normalize inherited data
```

## Migrations and manual schema evolution

There is no formal schema versioning. Migration is programmatic and idempotent inside `DatabaseInitializer.Initialize()`.

### Legacy files that are still absorbed

If `CurrentDbPath` does not exist, the bootstrap attempts to copy one of these files from the same directory:

- `WegInstallerSystems.db`
- `installer_systems.db`
- `carros.db`

Afterwards, it attempts to delete the migrated legacy file on a best-effort basis.

### Evolution strategy

The app performs manual changes with `ALTER TABLE` through `EnsureColumnExists()` and `PRAGMA table_info()`.

Compatibility columns added by bootstrap:

- `Usuarios.FotoPerfil`
- `Usuarios.AplicativosJson`
- `Instaladores.Nombre`
- `Instaladores.Descripcion`
- `Instaladores.Carpeta`
- all current columns of `Administrador`
- all current columns of `PasswordRecoveryLog`

Important point: `Instaladores` is first created with a minimal shape and then completed by later alterations. In practice, the current schema depends on bootstrap + alterations, not only on the original `CREATE TABLE` statement.

### Legacy tables removed

The bootstrap executes:

```sql
DROP TABLE IF EXISTS Carros;
DROP TABLE IF EXISTS Clientes;
DROP TABLE IF EXISTS Empleados;
DROP TABLE IF EXISTS Administradores;
```

If the legacy table `Administradores` exists, its rows are first copied into `Administrador` using `INSERT OR IGNORE ... SELECT ...` before removal.

### Automatic data normalization

On startup the following corrections are applied:

- empty or `NULL` `Usuarios.AplicativosJson` becomes `'[]'`;
- `Usuarios.Rol = CLIENTE` is migrated to `VENTAS`;
- `Usuarios.Rol = INGENIERO` is migrated to `INGENIERIA`;
- `Usuarios.Rol = ADMIN` is migrated to `ADMINISTRADOR`;
- empty or `NULL` `Instaladores.Carpeta` is migrated to `Desarrollo global`.

## Pragmas and concurrency

Only one pragma is explicitly configured in code:

```sql
PRAGMA busy_timeout = 5000;
```

It is applied when opening a connection in:

- `Db/UsuariosDbService.cs`
- `Db/AdministradoresDbService.cs`

It is not applied in `Db/InstaladorDbService.cs`.

In addition, `UsuariosDbService` implements manual retries for `SqliteErrorCode == 5` with up to three attempts and incremental waits.

`AdministradoresDbService` does not implement manual retries.

`InstaladorDbService` does not implement either `busy_timeout` or explicit retries.

## Real tables and roles

### `Usuarios`

Current definition:

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

Real role:

- main identity of the system;
- standard authentication;
- functional role of the UI;
- profile picture path storage;
- assigned application storage as JSON of routes.

### `Instaladores`

Functional definition after bootstrap:

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

Real role:

- catalog of executables launched from the app;
- folder classification such as `Desarrollo global` and `Punto local de desarrollo planta`;
- source for application assignment when the user is not admin.

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

Real role:

- separate privileged credential for admin login;
- administrative user resolution by `UsuarioSistema`;
- bridge toward the base user through `Correo`.

## Service layer over SQLite

Main services:

- `UsuariosDbService`: registration, login, queries, and assignment save;
- `AdministradoresDbService`: admin registration and admin login;
- `InstaladorDbService`: create, update, delete, and list installers.

These services build the real contract with the WPF ViewModels. There is no repository layer or ORM abstraction above them.

## Operational recommendations

- if you modify schema, review `DatabaseInitializer`, affected services, and tests together;
- if you add a new column, preserve idempotent bootstrap behavior;
- if you change the shared database path, review launcher/update flow too;
- if you localize persistence error messages, remember that the stored data itself is not translated.

## Reference links

- [Developer](help://developers/developer)
- [SQLite Documentation](https://sqlite.org/docs.html)
- [PRAGMA busy_timeout](https://sqlite.org/pragma.html#pragma_busy_timeout)
- [Microsoft.Data.Sqlite](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/)
