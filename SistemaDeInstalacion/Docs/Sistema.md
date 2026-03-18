# Sistema

`SistemaDeInstalacion` es una aplicación de escritorio orientada a centralizar ejecutables corporativos, controlar el acceso por rol y administrar qué usuario puede ver o ejecutar cada aplicativo.

Su propósito es reducir la dispersión de instaladores, formalizar la operación diaria y dejar una continuidad clara para usuarios, administradores y desarrolladores.

## Propósito de la aplicación

La aplicación existe para resolver estas necesidades:

1. centralizar los ejecutables autorizados en un único catálogo corporativo;
2. controlar permisos por usuario y por rol;
3. separar el acceso administrativo del acceso operativo normal;
4. mantener una base local con usuarios, administradores, instaladores y recuperaciones de contraseña.

## Alcance funcional actual

El sistema cubre hoy:

- inicio de sesión de usuario;
- inicio de sesión de administrador;
- registro de usuario;
- registro de administrador;
- recuperación local de contraseña;
- catálogo de instaladores;
- gestión de usuarios;
- asignación de aplicativos por persona;
- ayuda documental interna por perfil.

## Mapa del producto

```text
SistemaDeInstalacion
|
+- Acceso de usuarios
|  +- Login normal
|  +- Registro normal
|  +- Recuperación de contraseña
|  \- Vista de instaladores asignados
|
+- Acceso administrativo
|  +- Login administrativo
|  +- Registro administrativo
|  +- Gestión de usuarios
|  +- Alta, edición y baja de instaladores
|  \- Asignación de aplicativos
|
\- Persistencia local
   +- SQLite
   +- Usuarios
   +- Administrador
   +- Instaladores
   \- PasswordRecoveryLog
```

## Perfiles de uso

### User

El usuario final puede autenticarse, recuperar su contraseña, consultar sus instaladores asignados y ejecutar los aplicativos visibles para su cuenta.

### Administradores

El administrador puede autenticarse por la ruta administrativa, gestionar usuarios, clasificar ejecutables, asignar aplicativos y mantener el catálogo operativo.

### Developer

El desarrollador mantiene la aplicación WPF, la lógica de negocio, la base SQLite, las migraciones, el empaquetado y la documentación técnica.

## Estructura documental oficial

La documentación se distribuye así:

| Ubicación | Documento | Uso principal |
|---|---|---|
| `Docs/Sistema.md` | `Sistema` | visión general del producto |
| `Docs/users/User.md` | `User` | guía operativa para usuarios |
| `Docs/Administradores/Administradores.md` | `Administradores` | guía operativa y funcional para administración |
| `Docs/Developers/Developer.md` | `Developer` | guía técnica de continuidad para desarrollo |
| `Docs/Developers/BaseDeDatos.md` | `BaseDeDatos` | persistencia, tablas y migraciones |

## Flujo general del sistema

```text
Inicio de la aplicación
        |
        v
Inicialización de base de datos
        |
        v
Pantalla de login
   |                |
   |                +--> Login administrativo
   |                         |
   v                         v
Login normal           Modo administrador
   |                         |
   v                         v
Catálogo filtrado      Catálogo completo + Gestión de Usuarios
```

## Documentos complementarios

- [Guía de usuario](Docs/users/User.md)
- [Guía de administradores](Docs/Administradores/Administradores.md)
- [Guía técnica para desarrolladores](Docs/Developers/Developer.md)
- [Guía de base de datos](Docs/Developers/BaseDeDatos.md)

## Punto de partida recomendado

Si vas a operar el sistema:

- revisa [User](Docs/users/User.md) si tu perfil es usuario final;
- revisa [Administradores](Docs/Administradores/Administradores.md) si tu perfil es administrativo.

Si vas a mantener o evolucionar el producto:

- empieza por [Developer](Docs/Developers/Developer.md);
- continúa con [BaseDeDatos](Docs/Developers/BaseDeDatos.md).
