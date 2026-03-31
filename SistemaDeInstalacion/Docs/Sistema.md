# Sistema

`SistemaDeInstalacion` es una aplicación de escritorio orientada a centralizar ejecutables corporativos, controlar acceso por rol y administrar qué usuario puede ver o ejecutar cada aplicativo.

Su propósito es reducir la dispersión de instaladores, ordenar la operación diaria y dejar rutas claras para usuarios finales, administradores y desarrolladores.

## Propósito de la aplicación

La aplicación existe para resolver estas necesidades:

1. centralizar los ejecutables autorizados en un único catálogo;
2. controlar permisos por usuario y por rol;
3. separar el acceso administrativo del acceso operativo normal;
4. mantener continuidad documental y funcional del sistema.

## Alcance funcional actual

El sistema cubre hoy:

- acceso directo y apertura de la aplicación;
- login normal de usuario;
- login administrativo;
- registro de usuario;
- registro de administrador;
- recuperación de contraseña;
- catálogo de instaladores;
- gestión de usuarios;
- asignación de aplicativos;
- ayuda documental interna por perfil;
- logs funcionales y de soporte.

## Mapa general del producto

```text
SistemaDeInstalacion
|
+- Apertura desde acceso directo
|  |
|  \- Pantalla inicial
|      |
|      +-> Registro normal
|      +-> Login normal
|      +-> Registro administrativo
|      \-> Login administrativo
|
+- Operación de usuario
|  +- Catálogo filtrado
|  +- Ayuda
|  \- Ejecución de aplicativos asignados
|
\- Operación administrativa
   +- Catálogo completo
   +- Gestión de Usuarios
   +- Asignación de aplicativos
   +- Ayuda
   \- Logs restringidos
```

## Perfiles de uso

### User

El usuario final abre la aplicación desde el acceso directo, puede registrarse si aún no tiene cuenta, iniciar sesión por el login normal, consultar sus instaladores asignados y ejecutar los aplicativos visibles para su cuenta.

### Administradores

El administrador abre la aplicación desde el mismo acceso directo, pero opera por la ruta administrativa. Desde ahí puede registrarse como administrador, iniciar sesión con su acceso admin, gestionar usuarios, gestionar instaladores y asignar aplicativos.

### Developer

El desarrollador mantiene la aplicación WPF, la lógica, la persistencia, el empaquetado, los logs y la documentación técnica. Su guía sigue una orientación más técnica y de código.

## Ruta funcional recomendada

Para la operación funcional, el orden correcto de uso es este:

```text
Acceso directo
        |
        v
Pantalla inicial
        |
        +--> Registro
        |
        \--> Inicio de sesión
                 |
                 +--> Usuario final
                 \--> Administrador
```

Este orden es importante porque evita empezar directamente por módulos internos sin haber explicado primero cómo entra realmente una persona al sistema.

## Estructura documental oficial

La documentación se distribuye así:

| Ubicación | Documento | Uso principal |
|---|---|---|
| `Docs/Sistema.md` | `Sistema` | visión general del producto |
| `Docs/users/User.md` | `User` | guía operativa para usuarios finales |
| `Docs/Administradores/Administradores.md` | `Administradores` | guía operativa para administración |
| `Docs/Developers/Developer.md` | `Developer` | guía técnica y de continuidad para desarrollo |
| `Docs/Developers/BaseDeDatos.md` | `BaseDeDatos` | estructura y operación de la persistencia |

## Punto de partida recomendado

Si vas a operar el sistema:

- empieza por el acceso directo y la pantalla inicial;
- luego revisa el registro si aún no tienes cuenta;
- después revisa el login correspondiente a tu perfil.

Según el rol:

- consulta [User](Docs/users/User.md) si tu perfil es usuario final;
- consulta [Administradores](Docs/Administradores/Administradores.md) si tu perfil es administrativo.

Si vas a mantener o evolucionar el producto:

- empieza por [Developer](Docs/Developers/Developer.md);
- continúa con [BaseDeDatos](Docs/Developers/BaseDeDatos.md).

## Enlaces complementarios

- [Guía de usuario](Docs/users/User.md)
- [Guía de administradores](Docs/Administradores/Administradores.md)
- [Guía técnica para desarrolladores](Docs/Developers/Developer.md)
- [Guía de base de datos](Docs/Developers/BaseDeDatos.md)
