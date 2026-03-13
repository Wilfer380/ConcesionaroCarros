# Base de Datos SQLite

## 1. Archivo y ubicacion de la base

La base operativa actual se llama:

- `WegInstaladores.db`

La ruta no esta fija en `App.config`. Se construye por codigo en `Db/DatabaseInitializer.cs`:

```csharp
private const string CurrentDbPath = "WegInstaladores.db";
public static string ConnectionString => $"Data Source={CurrentDbPath}";
```

Eso significa que la base se crea en el directorio desde el que corre la aplicacion, normalmente:

- `bin/Debug/`
- `bin/Release/`

## 2. Estrategia de inicializacion

La inicializacion ocurre al arrancar la aplicacion y esta centralizada en:

- `Db/DatabaseInitializer.cs`

El proceso es:

1. buscar si ya existe `WegInstaladores.db`
2. si no existe, copiar una base legacy si encuentra algun nombre historico
3. crear tablas faltantes
4. agregar columnas faltantes con `ALTER TABLE`
5. migrar datos de la tabla legacy `Administradores` hacia `Administrador`
6. eliminar tablas legacy ya obsoletas
7. normalizar algunos datos historicos

## 3. Bases heredadas que aun se migran

Si la base actual no existe, el sistema intenta reutilizar alguna de estas:

- `WegInstallerSystems.db`
- `installer_systems.db`
- `carros.db`

Si la copia a `WegInstaladores.db` sale bien, luego intenta eliminar el archivo legacy original.

## 4. Esquema actual

## 4.1 Tabla `Usuarios`

Proposito:

- autenticacion de usuarios normales
- datos basicos de la persona
- rol funcional
- lista de aplicativos asignados
- password normal del usuario

Creacion base:

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

Columnas:

- `Id`: identificador autoincremental
- `Nombres`: nombres del usuario
- `Apellidos`: apellidos del usuario
- `Correo`: correo unico, clave funcional principal
- `Telefono`: dato opcional
- `PasswordHash`: hash `SHA-256` en base64
- `Rol`: codigo funcional del rol
- `FechaRegistro`: texto con formato `yyyy-MM-dd HH:mm:ss`
- `FotoPerfil`: campo legacy/auxiliar
- `AplicativosJson`: JSON con rutas de instaladores permitidos

Ejemplo de `AplicativosJson`:

```json
[
  "\\\\servidor\\apps\\ClienteERP.exe",
  "C:\\Instaladores\\HerramientaInterna.exe"
]
```

## 4.2 Tabla `Instaladores`

Proposito:

- catalogo de ejecutables disponibles dentro de la aplicacion

Creacion base mas migraciones:

```sql
CREATE TABLE IF NOT EXISTS Instaladores (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Ruta TEXT,
    Carpeta TEXT,
    FechaRegistro TEXT
);
```

Despues, `DatabaseInitializer` asegura columnas faltantes:

- `Nombre`
- `Descripcion`
- `Carpeta`

Esquema funcional resultante:

- `Id`
- `Ruta`
- `Nombre`
- `Descripcion`
- `Carpeta`
- `FechaRegistro`

Observaciones tecnicas:

- `Ruta` funciona como identificador operativo en varias operaciones
- `Actualizar(...)` en `InstaladorDbService` hace `WHERE Ruta = $ruta`
- si la ruta cambia, la asignacion por usuario puede quedar desalineada

## 4.3 Tabla `Administrador`

Proposito:

- almacenar la credencial administrativa separada de la credencial normal

Esquema:

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

Columnas clave:

- `Correo`: relacion funcional con `Usuarios.Correo`
- `UsuarioSistema`: usuario con el que entra el admin
- `PasswordAdminHash`: hash de la clave administrativa

## 4.4 Tabla `PasswordRecoveryLog`

Proposito:

- auditoria de recuperaciones de contrasena

Esquema:

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

Observacion:

- `ValidadoMicrosoft` se conserva por compatibilidad historica, aunque el flujo actual es local

## 5. Migraciones de columnas

La estrategia no usa una herramienta formal de migraciones. En su lugar, `DatabaseInitializer` verifica columna por columna con:

- `PRAGMA table_info(...)`
- `ALTER TABLE ... ADD COLUMN ...`

Ventajas:

- permite abrir instalaciones antiguas sin scripts separados
- mantiene el despliegue simple

Costo:

- toda evolucion del esquema depende de no olvidar actualizar `DatabaseInitializer`
- no hay versionado explicito de migraciones

## 6. Tablas legacy que se eliminan

Despues de migrar, el inicializador elimina:

- `Carros`
- `Clientes`
- `Empleados`
- `Administradores`

Antes de eliminar `Administradores`, si existe, copia sus datos a `Administrador`.

## 7. Normalizacion automatica de datos

Al iniciar, el sistema ejecuta estas correcciones:

```sql
UPDATE Usuarios
SET AplicativosJson = '[]'
WHERE AplicativosJson IS NULL OR TRIM(AplicativosJson) = '';

UPDATE Usuarios
SET Rol = 'VENTAS'
WHERE UPPER(TRIM(Rol)) = 'CLIENTE';

UPDATE Usuarios
SET Rol = 'INGENIERIA'
WHERE UPPER(TRIM(Rol)) = 'INGENIERO';

UPDATE Usuarios
SET Rol = 'ADMINISTRADOR'
WHERE UPPER(TRIM(Rol)) = 'ADMIN';

UPDATE Instaladores
SET Carpeta = 'Desarrollo global'
WHERE Carpeta IS NULL OR TRIM(Carpeta) = '';
```

Lectura practica:

- el sistema asume que debe corregir historicos cada vez que arranca
- estas reglas son parte del dominio actual y no solo de un script puntual

## 8. Relacion entre tablas y modulos

`Usuarios`:

- login normal
- registro normal
- actualizacion de password
- asignacion de aplicativos
- base funcional del administrador

`Administrador`:

- login administrativo
- registro administrativo
- sincronizacion cuando cambia el usuario base administrador

`Instaladores`:

- catalogo visible en `InstaladoresViewModel`
- fuente del panel de asignacion de aplicativos

`PasswordRecoveryLog`:

- auditoria del flujo de recuperacion

## 9. Reglas operativas importantes

## 9.1 Hash de contrasenas

Las contrasenas no se guardan en texto plano. Se convierten con `SHA-256` y luego a base64.

Ejemplo real del codigo:

```csharp
using (var sha = SHA256.Create())
{
    var bytes = Encoding.UTF8.GetBytes(password ?? string.Empty);
    var hash = sha.ComputeHash(bytes);
    return Convert.ToBase64String(hash);
}
```

Esto aplica tanto para:

- `Usuarios.PasswordHash`
- `Administrador.PasswordAdminHash`

## 9.2 Control de bloqueo SQLite

`UsuariosDbService` incluye:

- `PRAGMA busy_timeout = 5000`
- reintentos automaticos cuando `SqliteErrorCode == 5`

Eso reduce errores de bloqueo, pero no elimina completamente problemas si varios procesos usan la misma base.

## 9.3 Relacion usuario-instalador

La asignacion de aplicativos **no** vive en una tabla relacional. Vive en `Usuarios.AplicativosJson`.

Ventaja:

- implementacion rapida y simple

Limitaciones:

- no hay integridad referencial real
- la asignacion depende de rutas de archivo
- consultar analiticamente la relacion es mas dificil

## 10. Consultas utiles para soporte y desarrollo

Listar usuarios:

```sql
SELECT Id, Nombres, Apellidos, Correo, Rol, AplicativosJson
FROM Usuarios
ORDER BY Id DESC;
```

Listar administradores:

```sql
SELECT Id, Correo, UsuarioSistema, Rol, FechaRegistro
FROM Administrador
ORDER BY Id DESC;
```

Listar instaladores:

```sql
SELECT Id, Ruta, Nombre, Carpeta, FechaRegistro
FROM Instaladores
ORDER BY Id DESC;
```

Ver auditoria de recuperaciones:

```sql
SELECT Id, UsuarioId, CorreoUsuario, FechaRecuperacion
FROM PasswordRecoveryLog
ORDER BY Id DESC;
```

Buscar usuarios con JSON vacio o nulo:

```sql
SELECT Id, Correo, AplicativosJson
FROM Usuarios
WHERE AplicativosJson IS NULL
   OR TRIM(AplicativosJson) = ''
   OR AplicativosJson = '[]';
```

## 11. Riesgos y deuda tecnica en persistencia

- no existe versionado formal de esquema
- `InstaladorDbService.Actualizar(...)` usa la ruta como clave operativa
- `AplicativosJson` no impone integridad referencial
- los campos de fecha se almacenan como `TEXT`
- la base se crea en el directorio de ejecucion, lo que puede variar entre entornos

## 12. Recomendaciones de evolucion

- mover `usuario -> instalador` a una tabla relacional como `UsuarioInstalador`
- agregar una estrategia de versionado de migraciones
- usar identificadores estables para instaladores en lugar de depender de `Ruta`
- considerar indices si crece el volumen de usuarios o catalogo
- centralizar la documentacion de cada cambio de esquema en `DatabaseInitializer` y en este archivo
