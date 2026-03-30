# Administradores

Esta guia explica el recorrido funcional del administrador dentro del sistema. La secuencia sigue el uso real de la herramienta: abrir el acceso directo, registrar una cuenta administrativa si hace falta, iniciar sesion por la ruta correcta y despues operar los modulos de gestion.

## Explicacion principal

El administrador no entra solamente para abrir aplicativos. Entra para mantener el sistema funcionando correctamente, gestionar usuarios, administrar instaladores y controlar que cada persona vea solo lo que le corresponde.

Desde la vista administrativa se puede:

- administrar el catalogo de instaladores;
- crear, editar y eliminar usuarios;
- asignar o retirar aplicativos;
- consultar ayuda funcional;
- acceder al modulo de logs si el perfil tiene autorizacion especial.

## Objetivo de esta guia

Con esta guia el administrador podra:

- abrir correctamente la aplicacion desde el acceso directo;
- diferenciar el login normal del login admin;
- registrar una cuenta administrativa;
- iniciar sesion por la ruta correcta;
- gestionar instaladores;
- gestionar usuarios;
- asignar aplicativos;
- validar los cambios funcionales realizados.

## Que debe entender primero el administrador

Antes del recorrido visual, el administrador debe tener claras estas reglas:

1. el acceso empieza desde el mismo acceso directo principal del sistema;
2. el login normal y el login administrativo no son equivalentes;
3. si la cuenta administrativa aun no existe, primero debe registrarse;
4. solo despues del login admin correcto se habilitan los modulos de gestion;
5. cada cambio administrativo afecta a otros usuarios y debe revisarse con cuidado.

## Flujo general administrativo

```text
Acceso directo
        |
        v
Pantalla inicial
        |
        +--> Registro administrativo
        |        |
        |        v
        |   Regreso al login admin
        |
        \--> Login administrativo
                 |
                 v
        Vista principal admin
                 |
                 +--> Instaladores
                 +--> Gestion de Usuarios
                 +--> Ayuda
                 \--> Logs segun permisos
```

## Inicio del recorrido visual

Desde este punto comienzan los pantallazos de administracion. El orden recomendado para pegar las imagenes es:

1. acceso directo;
2. pantalla inicial;
3. diferencia entre login normal y admin;
4. registro administrativo;
5. login admin;
6. vista principal;
7. instaladores;
8. gestion de usuarios;
9. asignaciones;
10. cierre de sesion.

## Paso 1. Abrir la aplicacion desde el acceso directo

El administrador tambien inicia desde el acceso directo principal del sistema. Desde ahi debe dirigirse a la ruta administrativa.

Historia de pantallazo: Acceso directo del sistema en el escritorio.
<!-- Aqui pegas la imagen del acceso directo para administracion -->

Historia de pantallazo: Pantalla inicial del sistema antes de elegir login admin.
<!-- Aqui pegas la imagen de la pantalla inicial -->

## Paso 2. Entender la diferencia entre login normal y login admin

Esta es una de las reglas mas importantes del sistema:

- el login normal es para usuarios finales;
- el login administrativo activa permisos de administracion;
- un usuario con rol administrativo no obtiene acceso completo si entra por login normal;
- para ver `Gestion de Usuarios` y demas funciones de administracion, siempre debe ingresar por la ruta admin.

Historia de pantallazo: Diferencia visual entre acceso normal y acceso administrativo.
<!-- Aqui pegas la imagen que muestra las dos rutas de acceso -->

## Paso 3. Registro administrativo

Si la cuenta administrativa aun no existe, primero debe registrarse desde la ruta admin.

### Paso 3.1. Abrir la opcion Registrate del login admin

Desde la pantalla de login administrativo, pulsa la opcion `Registrate`.

Historia de pantallazo: Opcion Registrate dentro del login administrativo.
<!-- Aqui pegas la imagen del acceso al registro admin -->

### Paso 3.2. Completar el formulario de registro administrativo

En el registro administrativo se deben completar:

- correo corporativo;
- rol administrativo;
- contrasena normal;
- contrasena administrativa.

Este registro no es igual al normal. Aqui el sistema crea o actualiza la cuenta base y ademas registra la informacion administrativa necesaria para operar en modo admin.

Historia de pantallazo: Formulario completo de registro administrativo.
<!-- Aqui pegas la imagen del formulario de registro admin -->

### Paso 3.3. Guardar el registro administrativo

Pulsa el boton de registro y espera la confirmacion.

Si el proceso es correcto:

- se crea o actualiza el usuario base;
- se registra la cuenta administrativa;
- el sistema regresa al login admin con los datos listos para ingresar.

Historia de pantallazo: Confirmacion de registro administrativo exitoso.
<!-- Aqui pegas la imagen del mensaje de registro admin exitoso -->

Historia de pantallazo: Validacion de registro administrativo por duplicado o error.
<!-- Aqui pegas la imagen de una validacion de registro admin -->

## Paso 4. Iniciar sesion por login administrativo

Una vez abierto el acceso directo y, si aplica, completado el registro, el siguiente paso es ingresar por el login admin.

### Paso 4.1. Abrir el login administrativo

Desde la pantalla principal, entra a la opcion de acceso administrativo.

Historia de pantallazo: Acceso al login administrativo desde la pantalla inicial.
<!-- Aqui pegas la imagen de apertura del login admin -->

### Paso 4.2. Escribir el usuario administrativo

En este login debes usar el `UsuarioSistema` o identificador administrativo definido para la cuenta.

Historia de pantallazo: Campo de usuario administrativo diligenciado.
<!-- Aqui pegas la imagen del campo de usuario admin -->

### Paso 4.3. Escribir la contrasena administrativa

Ingresa la contrasena administrativa correspondiente. Esta contrasena no se valida igual que la del login normal.

Historia de pantallazo: Campo de contrasena administrativa diligenciado.
<!-- Aqui pegas la imagen del campo de contrasena admin -->

### Paso 4.4. Confirmar el ingreso

Pulsa el boton de acceso. Si todo es correcto, el sistema abrira la vista principal con modo administrador activo.

Historia de pantallazo: Resultado de login admin exitoso.
<!-- Aqui pegas la imagen del acceso admin correcto -->

## Paso 5. Validaciones del login administrativo

El sistema diferencia mejor los errores del login admin.

### Cuando el usuario administrativo no existe

El sistema indicara que el usuario administrativo no se encuentra registrado.

Historia de pantallazo: Mensaje de usuario administrativo incorrecto.
<!-- Aqui pegas la imagen del error de usuario admin -->

### Cuando la contrasena administrativa es incorrecta

El sistema indicara que la contrasena administrativa es incorrecta.

Historia de pantallazo: Mensaje de contrasena administrativa incorrecta.
<!-- Aqui pegas la imagen del error de contrasena admin -->

## Paso 6. Vista principal del administrador

Cuando el login admin es correcto, la aplicacion entra en modo administrador.

Normalmente vera:

- panel lateral;
- modulo `Instaladores`;
- modulo `Gestion de Usuarios`;
- opcion `Ayuda`;
- opcion `Cerrar sesion`;
- opcion `Logs` solo si el perfil cumple la validacion especial de soporte.

Historia de pantallazo: Vista principal del administrador con menu completo.
<!-- Aqui pegas la imagen de la vista principal admin -->

## Paso 7. Gestion de instaladores

Desde este modulo se controla el catalogo de ejecutables del sistema.

### Como agregar un instalador

1. entra al modulo `Instaladores`;
2. abre el formulario de nuevo instalador;
3. selecciona el ejecutable;
4. completa nombre, descripcion y categoria;
5. guarda el registro.

Historia de pantallazo: Modulo Instaladores en modo administrador.
<!-- Aqui pegas la imagen del modulo de instaladores -->

Historia de pantallazo: Formulario de nuevo instalador.
<!-- Aqui pegas la imagen del formulario de instalador -->

Historia de pantallazo: Confirmacion de instalador guardado.
<!-- Aqui pegas la imagen de la confirmacion del instalador -->

### Como editar o eliminar un instalador

Para editar:

1. ubica la tarjeta correcta;
2. pulsa `Editar`;
3. actualiza la informacion;
4. guarda.

Para eliminar:

1. ubica la tarjeta;
2. pulsa `Eliminar`;
3. confirma la accion.

Historia de pantallazo: Edicion de un instalador existente.
<!-- Aqui pegas la imagen de edicion de instalador -->

Historia de pantallazo: Confirmacion de eliminacion de instalador.
<!-- Aqui pegas la imagen de eliminacion de instalador -->

## Paso 8. Gestion de usuarios

Este modulo solo esta disponible en modo administrador. Desde aqui se administran cuentas y permisos.

### Como agregar un usuario

1. abre `Gestion de Usuarios`;
2. pulsa la opcion para crear un usuario;
3. completa nombres, apellidos, correo, telefono, contrasena y rol;
4. guarda la informacion.

Historia de pantallazo: Vista principal de Gestion de Usuarios.
<!-- Aqui pegas la imagen de la vista principal de gestion de usuarios -->

Historia de pantallazo: Formulario de nuevo usuario.
<!-- Aqui pegas la imagen del formulario de usuario -->

Historia de pantallazo: Confirmacion de usuario creado.
<!-- Aqui pegas la imagen de la confirmacion de usuario creado -->

### Como editar o eliminar un usuario

Para editar:

1. localiza el registro;
2. pulsa `Editar`;
3. actualiza la informacion;
4. guarda.

Para eliminar:

1. localiza el registro;
2. pulsa `Eliminar`;
3. confirma la accion.

Historia de pantallazo: Edicion de usuario existente.
<!-- Aqui pegas la imagen de edicion de usuario -->

Historia de pantallazo: Confirmacion de eliminacion de usuario.
<!-- Aqui pegas la imagen de eliminacion de usuario -->

## Paso 9. Asignacion de aplicativos

Dentro de `Gestion de Usuarios`, el administrador puede asignar o retirar aplicativos a una persona.

El flujo general es:

1. seleccionar el usuario;
2. abrir el panel de asignacion;
3. marcar o desmarcar aplicativos;
4. guardar la asignacion.

Historia de pantallazo: Seleccion de usuario para asignacion.
<!-- Aqui pegas la imagen de seleccion de usuario -->

Historia de pantallazo: Panel de asignacion abierto.
<!-- Aqui pegas la imagen del panel de asignacion -->

Historia de pantallazo: Aplicativos marcados o desmarcados.
<!-- Aqui pegas la imagen de las casillas de asignacion -->

Historia de pantallazo: Confirmacion de asignacion guardada.
<!-- Aqui pegas la imagen de confirmacion de asignacion -->

## Paso 10. Cerrar sesion

Cuando el administrador termine:

1. debe volver al panel lateral;
2. pulsar `Cerrar sesion`;
3. confirmar el cierre si aplica;
4. dejar el sistema en la pantalla inicial.

Historia de pantallazo: Cierre de sesion del administrador.
<!-- Aqui pegas la imagen del cierre de sesion admin -->

## Que revisar despues de un cambio importante

Despues de agregar, editar, eliminar o asignar, valida:

- que el usuario aparezca correctamente;
- que el instalador siga visible en el catalogo;
- que el usuario pueda ingresar;
- que el catalogo filtrado coincida con la asignacion realizada.

## Casos de soporte frecuentes

### El usuario no ve su aplicativo

Revisa:

1. que el instalador exista;
2. que el usuario tenga asignaciones;
3. que la ruta del ejecutable siga siendo valida.

### El ejecutable no abre

Revisa:

1. que el archivo exista en la ruta;
2. que no haya sido movido;
3. que Windows no lo bloquee;
4. que el usuario tenga permisos sobre esa ubicacion.

### No aparece Gestion de Usuarios

Revisa:

1. que el ingreso haya sido por login admin;
2. que la contrasena usada sea la administrativa;
3. que el modo administrador este activo.

## Buenas practicas para administracion

- usa nombres claros en instaladores;
- valida rutas antes de guardar;
- evita cuentas duplicadas;
- confirma asignaciones despues de cada cambio;
- diferencia siempre login normal de login admin.

## Contacto y apoyo

Si surge una duda funcional o tecnica, utiliza el centro de ayuda interno del sistema y el canal de soporte definido por la organizacion.
