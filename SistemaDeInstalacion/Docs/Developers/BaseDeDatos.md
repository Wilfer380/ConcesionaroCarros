# Base de Datos SQLite

## 1. Archivo activo

La base de datos operativa del sistema es:

- `WegInstaladores.db`

La cadena de conexion se define en `Db/DatabaseInitializer.cs`.

## 2. Politica de migración

Al iniciar la aplicación, `DatabaseInitializer.Initialize()` verifica si ya existe `WegInstaladores.db`.

### Si la base nueva no existe

El sistema intenta migrar automáticamente desde alguna base heredada con estos nombres:

- `WegInstallerSystems.db`
- `installer_systems.db`
- `carros.db`

### Despues de la migración

- la aplicación abre la nueva base
- aplica migraciones de estructura si faltan columnas
- elimina el archivo legacy migrado cuando la copia ya fue exitosa

## 3. Tablas activas

## Usuarios

### Proposito

Tabla principal de autenticación y autorización para usuarios normales.

### Columnas

- `Id`
- `Nombres`
- `Apellidos`
- `Correo`
- `Telefono`
- `PasswordHash`
- `Rol`
- `FechaRegistro`
- `FotoPerfil`
- `AplicativosJson`

### Uso funcional

- login normal
- registro normal
- sincronización del usuario base de administradores
- lectura del rol del usuario
- almacenamiento de aplicativos asignados
- actualización de contraseña en recuperación de acceso

### Observaciones

- `Correo` es único
- `PasswordHash` se guarda con SHA-256
- `AplicativosJson` contiene una lista JSON de rutas de instaladores asignadas al usuario
- `FotoPerfil` existe en el esquema pero actualmente no forma parte del flujo funcional principal

## Instaladores

### Proposito

Catálogo de instaladores disponibles en la aplicación.

### Columnas

- `Id`
- `Ruta`
- `Nombre`
- `Descripcion`
- `Carpeta`
- `FechaRegistro`

### Uso funcional

- vista principal de instaladores
- carga filtrada por carpeta funcional
- edición del catálogo por parte de administradores

### Observaciones

- `Ruta` es la referencia real del ejecutable
- `Nombre` puede venir vacio en bases antiguas; el sistema usa el nombre del archivo como respaldo
- `Carpeta` se normaliza a `Desarrollo global` cuando llega vacia

## Administrador

### Proposito

Separar la credencial administrativa de la credencial normal del mismo usuario.

### Columnas

- `Id`
- `Nombres`
- `Apellidos`
- `Correo`
- `UsuarioSistema`
- `Rol`
- `PasswordAdminHash`
- `FechaRegistro`

### Uso funcional

- login administrativo
- registro administrativo
- sincronización cuando un usuario cambia a rol `ADMINISTRADOR`

### Observaciones

- `Correo` es unico
- `UsuarioSistema` es el identificador usado por el login administrativo
- `PasswordAdminHash` es independiente del `PasswordHash` de `Usuarios`

## PasswordRecoveryLog

### Proposito

Tabla de auditoria para recuperaciones de contraseñas.

### Columnas

- `Id`
- `UsuarioId`
- `CorreoUsuario`
- `CorreoAdministrador`
- `ValidadoMicrosoft`
- `FechaRecuperacion`

### Uso funcional

- registrar cada cambio de contraseña originado desde el flujo de recuperación

### Observaciones

- el flujo actual de recuperación es local
- `ValidadoMicrosoft` permanece por compatibilidad historica, pero hoy no representa una autenticación activa contra Microsoft

## 4. Migraciones de columnas

`DatabaseInitializer` usa `EnsureColumnExists(...)` para completar columnas faltantes sobre bases existentes.

Esto permite:

- abrir instalaciones antiguas sin romper el arranque
- agregar nuevas columnas sin obligar a reconstruir manualmente la base
- mantener compatibilidad con migraciones incrementales

## 5. Limpieza de legado

Después de migrar, el inicializador elimina tablas heredadas que ya no hacen parte del dominio actual:

- `Carros`
- `Clientes`
- `Empleados`
- `Administradores` (plural)

Adicionalmente, si existe la tabla legacy `Administradores`, su contenido se copia antes a la tabla vigente `Administrador`.

## 6. Normalización de datos al iniciar

En el arranque se aplican normalizaciones para evitar inconsistencias historicas:

- `AplicativosJson` vacio se transforma en `[]`
- `CLIENTE` se convierte a `VENTAS`
- `INGENIERO` se convierte a `INGENIERIA`
- `ADMIN` se convierte a `ADMINISTRADOR`
- `Instaladores.Carpeta` vacia se convierte en `Desarrollo global`

## 7. Relación entre tablas y módulos

### Login normal

- tabla principal: `Usuarios`

### Login administrativo

- valida en `Administrador`
- resuelve el usuario base en `Usuarios`

### Gestión de usuarios

- lista y actualiza `Usuarios`
- sincroniza `Administrador` si el rol aplica

### Recuperación de contraseña

- actualiza `Usuarios.PasswordHash`
- inserta auditoria en `PasswordRecoveryLog`

### Catálogo de instaladores

- lee y actualiza `Instaladores`
- cruza permisos con `Usuarios.AplicativosJson`

## 8. Riesgos actuales

- `AplicativosJson` depende de rutas fisicas; si la ruta cambia, la asignación deja de ser util
- la base sigue incluyendo `FotoPerfil` por compatibilidad, aunque ese dato no se esta usando como parte principal del flujo
- la asignacion en JSON es simple y funcional, pero menos robusta que una tabla relacional

## 9. Recomendaciones de evolución

### Alta prioridad

- evaluar una tabla relacional `UsuarioInstalador` si el sistema sigue creciendo
- documentar cualquier nueva migración directamente en `DatabaseInitializer`
- mantener pruebas manuales de migración al cambiar nombres de base o tablas

### Media prioridad

- agregar indices si el volumen de usuarios o instaladores aumenta
- introducir una capa de repositorios o servicios de persistencia mas desacoplada del ViewModel

### Baja prioridad

- retirar campos legacy que ya no tengan ningun uso operativo
- consolidar auditorias adicionales si el sistema requiere trazabilidad ampliada
