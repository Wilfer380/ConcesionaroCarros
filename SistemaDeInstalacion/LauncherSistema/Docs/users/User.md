# User

Esta guía describe el funcionamiento del sistema para usuarios finales. Su objetivo es que cualquier persona pueda iniciar sesión, recuperar su contraseña y ejecutar sus aplicativos sin depender de soporte para cada paso.

## Qué hace esta aplicación para el usuario

La aplicación permite:

- iniciar sesión con credenciales corporativas registradas;
- recuperar contraseña desde la pantalla de acceso;
- ver solo los instaladores autorizados para su cuenta;
- ejecutar aplicativos asignados desde una interfaz única.

## Flujo de ingreso

```text
Abrir la aplicación
        |
        v
Escribir usuario y contraseña
        |
        v
Validación de credenciales
        |
        +--> Correctas -> vista principal de instaladores
        |
        \--> Incorrectas -> mensaje de error
```

## Inicio de sesión

Para entrar al sistema:

1. abre la aplicación;
2. escribe tu usuario o correo;
3. escribe tu contraseña;
4. si lo deseas, marca `Recuérdame`;
5. pulsa el botón de acceso.

Si la validación es correcta, el sistema abre la vista principal con los instaladores permitidos para tu cuenta.

## Recuperación de contraseña

Si olvidaste tu contraseña:

1. abre la opción de recuperación;
2. escribe tu correo registrado;
3. completa la verificación solicitada;
4. ingresa el código temporal mostrado por la aplicación;
5. define la nueva contraseña;
6. confirma el cambio.

Cuando el proceso termina correctamente, la nueva contraseña queda activa de inmediato.

## Vista principal

La pantalla principal del usuario contiene:

- menú lateral con acceso a `Instaladores`;
- nombre del usuario autenticado;
- estado de conexión;
- listado de instaladores asignados;
- botones `Ver` e `Instalar`.

## Cómo interpretar el catálogo

Cada tarjeta de instalador muestra:

- nombre visible del aplicativo;
- ruta registrada;
- ícono del ejecutable, cuando es posible obtenerlo;
- acción para consultar detalles;
- acción para ejecutar el aplicativo.

El sistema solo mostrará lo que el administrador haya asignado a tu cuenta.

## Qué no puede hacer un usuario final

Un usuario final no puede:

- agregar instaladores;
- editar instaladores;
- eliminar instaladores;
- abrir `Gestión de Usuarios`;
- asignar aplicativos;
- entrar en modo administrador con el login normal.

## Buenas prácticas para el usuario

- usa siempre tu correo corporativo correcto;
- verifica que el aplicativo que vas a ejecutar sea el esperado;
- informa a soporte si una ruta ya no abre o si una tarjeta aparece vacía;
- usa recuperación de contraseña antes de intentar crear una cuenta duplicada.

## Soporte rápido

### Si no ves tus instaladores

Valida con administración o soporte:

1. que tu usuario exista en el sistema;
2. que tengas aplicativos asignados;
3. que la ruta del ejecutable siga vigente.

### Si no puedes iniciar sesión

Revisa:

1. que estés escribiendo el usuario correcto;
2. que la contraseña no tenga errores de digitación;
3. que no estés usando la ruta administrativa por equivocación;
4. que tu cuenta ya haya sido registrada.

## Contacto

Para cualquier información adicional, usa el canal de soporte indicado dentro del centro de ayuda del sistema.
