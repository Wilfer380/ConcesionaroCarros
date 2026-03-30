# Sistema

`SistemaDeInstalacion` es una aplicacion de escritorio orientada a centralizar ejecutables corporativos, controlar acceso por rol y administrar que usuario puede ver o ejecutar cada aplicativo.

Su proposito es reducir la dispersion de instaladores, ordenar la operacion diaria y dejar rutas claras para usuarios finales, administradores y desarrolladores.

## Proposito de la aplicacion

La aplicacion existe para resolver estas necesidades:

1. centralizar los ejecutables autorizados en un unico catalogo;
2. controlar permisos por usuario y por rol;
3. separar el acceso administrativo del acceso operativo normal;
4. mantener continuidad documental y funcional del sistema.

## Alcance funcional actual

El sistema cubre hoy:

- acceso directo y apertura de la aplicacion;
- login normal de usuario;
- login administrativo;
- registro de usuario;
- registro de administrador;
- recuperacion de contrasena;
- catalogo de instaladores;
- gestion de usuarios;
- asignacion de aplicativos;
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
+- Operacion de usuario
|  +- Catalogo filtrado
|  +- Ayuda
|  \- Ejecucion de aplicativos asignados
|
\- Operacion administrativa
   +- Catalogo completo
   +- Gestion de Usuarios
   +- Asignacion de aplicativos
   +- Ayuda
   \- Logs restringidos
```

## Perfiles de uso

### User

El usuario final abre la aplicacion desde el acceso directo, puede registrarse si aun no tiene cuenta, iniciar sesion por el login normal, consultar sus instaladores asignados y ejecutar los aplicativos visibles para su cuenta.

### Administradores

El administrador abre la aplicacion desde el mismo acceso directo, pero opera por la ruta administrativa. Desde ahi puede registrarse como administrador, iniciar sesion con su acceso admin, gestionar usuarios, gestionar instaladores y asignar aplicativos.

### Developer

El desarrollador mantiene la aplicacion WPF, la logica, la persistencia, el empaquetado, los logs y la documentacion tecnica. Su guia sigue una orientacion mas tecnica y de codigo.

## Ruta funcional recomendada

Para la operacion funcional, el orden correcto de uso es este:

```text
Acceso directo
        |
        v
Pantalla inicial
        |
        +--> Registro
        |
        \--> Inicio de sesion
                 |
                 +--> Usuario final
                 \--> Administrador
```

Este orden es importante porque evita empezar directamente por modulos internos sin haber explicado primero como entra realmente una persona al sistema.

## Estructura documental oficial

La documentacion se distribuye asi:

| Ubicacion | Documento | Uso principal |
|---|---|---|
| `Docs/Sistema.md` | `Sistema` | vision general del producto |
| `Docs/users/User.md` | `User` | guia operativa para usuarios finales |
| `Docs/Administradores/Administradores.md` | `Administradores` | guia operativa para administracion |
| `Docs/Developers/Developer.md` | `Developer` | guia tecnica y de continuidad para desarrollo |
| `Docs/Developers/BaseDeDatos.md` | `BaseDeDatos` | estructura y operacion de la persistencia |

## Punto de partida recomendado

Si vas a operar el sistema:

- empieza por el acceso directo y la pantalla inicial;
- luego revisa el registro si aun no tienes cuenta;
- despues revisa el login correspondiente a tu perfil.

Segun el rol:

- consulta [User](Docs/users/User.md) si tu perfil es usuario final;
- consulta [Administradores](Docs/Administradores/Administradores.md) si tu perfil es administrativo.

Si vas a mantener o evolucionar el producto:

- empieza por [Developer](Docs/Developers/Developer.md);
- continua con [BaseDeDatos](Docs/Developers/BaseDeDatos.md).

## Enlaces complementarios

- [Guia de usuario](Docs/users/User.md)
- [Guia de administradores](Docs/Administradores/Administradores.md)
- [Guia tecnica para desarrolladores](Docs/Developers/Developer.md)
- [Guia de base de datos](Docs/Developers/BaseDeDatos.md)
