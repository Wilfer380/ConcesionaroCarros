# BaseDeDatos

Esta guía explica la persistencia local del sistema: archivo activo, tablas, relaciones lógicas, migraciones y recomendaciones para evolucionar el esquema sin romper compatibilidad.

Debe revisarse junto con:

- [Developer](Docs/Developers/Developer.md)
- [Administradores](Docs/Administradores/Administradores.md)

## Archivo activo

La base utilizada por la aplicación es:

- `WegInstaladores.db`

La cadena de conexión se construye desde `Db/DatabaseInitializer.cs`, por lo que la base queda ubicada en el directorio de ejecución del programa.

Ubicaciones comunes:

- `bin/Debug/`
- `bin/Release/`

## Flujo de inicialización

```text
Inicio de la aplicación
        |
        v
Buscar WegInstaladores.db
        |
        +--> si no existe, intentar migrar una base legacy
        |
        v
Crear tablas faltantes
        |
        v
Asegurar columnas nuevas
        |
        v
Normalizar datos heredados
        |
        v
Aplicación lista para login
```

## Bases legacy que aún se migran

Si la base principal no existe, el inicializador puede migrar automáticamente:

- `WegInstallerSystems.db`
- `installer_systems.db`
- `carros.db`

## Esquema funcional actual

### Tabla `Usuarios`

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

Responsabilidad:

- identidad operativa del usuario;
- contraseña normal;
- rol funcional;
- permisos serializados en `AplicativosJson`.

### Tabla `Instaladores`

Esquema funcional esperado:

```sql
CREATE TABLE IF NOT EXISTS Instaladores (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Ruta TEXT,
    Nombre TEXT,
    Descripcion TEXT,
    Carpeta TEXT,
    FechaRegistro TEXT
);
```

Responsabilidad:

- catálogo de ejecutables;
- clasificación por carpeta funcional;
- fuente para asignación de aplicativos.

### Tabla `Administrador`

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

Responsabilidad:

- credencial privilegiada;
- acceso al login administrativo;
- unión lógica con `Usuarios` por correo.

### Tabla `PasswordRecoveryLog`

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

Responsabilidad:

- trazabilidad de recuperaciones de contraseña;
- soporte operativo y auditoría básica.

## Diagrama lógico de datos

```text
Usuarios
  |
  +--> PasswordRecoveryLog   (por UsuarioId)
  |
  +--> Administrador         (unión lógica por Correo)
  |
  \--> Instaladores          (unión lógica por rutas en AplicativosJson)
```

## Reglas críticas del modelo

### La relación usuario-aplicativo no es relacional

El permiso de un usuario se guarda como JSON con rutas de ejecutables. Eso significa:

- no existe tabla intermedia `UsuarioInstalador`;
- la integridad depende del código;
- si cambia la ruta, la asignación puede quedar obsoleta.

Ejemplo de contenido:

```json
[
  "\\\\servidor\\apps\\ERP.exe",
  "C:\\Instaladores\\Reporteador.exe"
]
```

### La identidad administrativa depende de `Usuarios`

El login administrativo valida en `Administrador`, pero después necesita resolver el usuario base en `Usuarios`.

### Las fechas se guardan como texto

No hay un tipo datetime fuerte. Cualquier consulta de orden o auditoría depende del formato grabado por la aplicación.

## Migraciones actuales

La estrategia usada hoy es programática, no versionada.

El inicializador realiza:

1. creación de tablas faltantes;
2. adición de columnas faltantes con `ALTER TABLE`;
3. migración de `Administradores` legacy hacia `Administrador`;
4. limpieza de tablas heredadas;
5. normalización de datos antiguos.

## Normalizaciones automáticas

Ejemplos de normalización que la app ejecuta al iniciar:

```sql
UPDATE Usuarios
SET AplicativosJson = '[]'
WHERE AplicativosJson IS NULL OR TRIM(AplicativosJson) = '';

UPDATE Usuarios
SET Rol = 'ADMINISTRADOR'
WHERE UPPER(TRIM(Rol)) = 'ADMIN';

UPDATE Instaladores
SET Carpeta = 'Desarrollo global'
WHERE Carpeta IS NULL OR TRIM(Carpeta) = '';
```

## Seguridad actual

Las contraseñas no se guardan en texto plano. Se almacenan como hash `SHA-256` codificado en base64.

Esto aplica para:

- `Usuarios.PasswordHash`
- `Administrador.PasswordAdminHash`

Nota técnica:

- el esquema actual mejora el almacenamiento plano;
- para una evolución futura conviene migrar a `PBKDF2`, `bcrypt` o `Argon2`.

## Bloqueos SQLite y concurrencia

La aplicación usa `busy_timeout` y reintentos ante base ocupada. Esto ayuda a disminuir errores transitorios, pero no convierte la solución en una base multiusuario fuerte.

Interpretación práctica:

- es adecuada para el uso actual de escritorio;
- no debe asumirse como una base central concurrente para muchos procesos.

## Operaciones y tablas afectadas

| Operación | Tablas impactadas |
|---|---|
| Registrar administrador | `Usuarios`, `Administrador` |
| Login administrativo | lectura de `Administrador` y `Usuarios` |
| Crear usuario | `Usuarios` |
| Editar usuario | `Usuarios` y, si aplica, `Administrador` |
| Eliminar usuario | `Usuarios`, `Administrador`, `PasswordRecoveryLog` |
| Registrar instalador | `Instaladores` |
| Editar instalador | `Instaladores` |
| Eliminar instalador | `Instaladores` |
| Asignar aplicativos | `Usuarios.AplicativosJson` |
| Recuperar contraseña | `Usuarios.PasswordHash`, `PasswordRecoveryLog` |

## Consultas útiles de soporte

### Usuarios

```sql
SELECT Id, Nombres, Apellidos, Correo, Rol, AplicativosJson
FROM Usuarios
ORDER BY Id DESC;
```

### Administradores

```sql
SELECT Id, Correo, UsuarioSistema, Rol, FechaRegistro
FROM Administrador
ORDER BY Id DESC;
```

### Instaladores

```sql
SELECT Id, Ruta, Nombre, Carpeta, FechaRegistro
FROM Instaladores
ORDER BY Id DESC;
```

### Recuperaciones de contraseña

```sql
SELECT Id, UsuarioId, CorreoUsuario, CorreoAdministrador, FechaRecuperacion
FROM PasswordRecoveryLog
ORDER BY Id DESC;
```

### Administradores sin usuario base

```sql
SELECT a.Id, a.Correo, a.UsuarioSistema
FROM Administrador a
LEFT JOIN Usuarios u ON UPPER(TRIM(a.Correo)) = UPPER(TRIM(u.Correo))
WHERE u.Id IS NULL;
```

## Documentos complementarios

- [Visión general del sistema](Docs/Sistema.md)
- [Guía de desarrollo](Docs/Developers/Developer.md)
- [Guía de administradores](Docs/Administradores/Administradores.md)

## Enlaces de apoyo externo

Para ampliar conceptos técnicos relacionados con esta capa:

- [Documentación oficial de SQLite](https://sqlite.org/docs.html)
- [Referencia de `PRAGMA busy_timeout` en SQLite](https://sqlite.org/pragma.html#pragma_busy_timeout)
- [Documentación oficial de Microsoft.Data.Sqlite en Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/)

## Checklist para cambios de esquema

Antes de modificar la base:

1. actualiza `Db/DatabaseInitializer.cs`;
2. valida el impacto sobre `UsuariosDbService`, `AdministradoresDbService` e `InstaladorDbService`;
3. revisa si la ayuda administrativa debe cambiar;
4. agrega o ajusta pruebas en `SistemaDeInstalacion.Tests`;
5. documenta el cambio en este archivo.

## Riesgos técnicos vigentes

- no existe versionado formal del esquema;
- `AplicativosJson` no impone integridad referencial;
- la ruta del instalador actúa como identificador funcional;
- la base depende del directorio de ejecución;
- la relación administrativa se sincroniza por correo.

## Evolución recomendada

- crear una tabla relacional para permisos por aplicativo;
- versionar migraciones;
- desacoplar el identificador funcional de la ruta física;
- introducir índices si crece el volumen de datos;
- mantener esta guía alineada con el código real.
