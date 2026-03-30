# Administradores

Esta guia explica, paso a paso, como debe operar un administrador dentro del sistema. Esta pensada para una persona responsable de mantener usuarios, instaladores, permisos y control funcional de la herramienta.

## Objetivo de esta guia

Con esta guia el administrador podra:

- ingresar correctamente por la ruta administrativa;
- entender la diferencia entre login normal y login admin;
- registrar, editar y eliminar instaladores;
- crear, editar y eliminar usuarios;
- asignar aplicativos por persona;
- reconocer el impacto funcional de cada accion.

## Alcance del rol administrador

El administrador es responsable de la operacion diaria del sistema. Esto incluye:

- mantener actualizado el catalogo de instaladores;
- gestionar cuentas de usuario;
- asignar aplicativos segun necesidad operativa;
- retirar registros obsoletos;
- garantizar que cada persona vea solo lo que debe usar.

## Flujo general administrativo

```text
Abrir la aplicacion
        |
        v
Entrar por login administrativo
        |
        v
Vista principal en modo administrador
   |
   +--> Instaladores
   |      +- ver
   |      +- agregar
   |      +- editar
   |      \- eliminar
   |
   +--> Gestion de Usuarios
   |      +- agregar
   |      +- editar
   |      +- eliminar
   |      \- asignar aplicativos
   |
   \--> Ayuda
```

## Diferencia entre login normal y login administrativo

Esta es una de las reglas mas importantes del sistema:

- el login normal sirve para usuarios finales;
- el login administrativo sirve para activar permisos de administracion;
- una persona con rol administrativo no obtiene esos permisos si entra por el login normal;
- para ver Gestion de Usuarios y opciones administrativas, siempre debes usar el login admin.

## Paso a paso para ingresar como administrador

### Paso 1. Abrir la ruta administrativa

Desde la pantalla inicial del sistema, entra por la opcion de acceso administrativo.

Historia de pantallazo: Acceso desde login normal hacia login administrativo.

### Paso 2. Escribir el usuario administrativo

En el acceso administrativo debes escribir el `UsuarioSistema` o identificador definido para el administrador.

Este campo no se comporta igual que el login normal, por lo que debes usar exactamente el dato registrado para administracion.

Historia de pantallazo: Campo de usuario administrativo diligenciado.

### Paso 3. Escribir la contrasena administrativa

Ingresa la contrasena administrativa correspondiente. Esta contrasena es distinta de la contrasena normal del usuario.

Historia de pantallazo: Campo de contrasena administrativa diligenciado.

### Paso 4. Confirmar el ingreso

Pulsa el boton de acceso. Si la validacion es correcta, el sistema abrira la vista principal con el modo administrador activo.

Historia de pantallazo: Resultado de login admin exitoso.

## Que cambia cuando el login admin es correcto

Cuando el acceso administrativo es exitoso:

- aparece la vista completa de instaladores;
- se habilita `Gestion de Usuarios`;
- se permiten formularios de alta, edicion y eliminacion;
- se habilita la asignacion de aplicativos por usuario.

## Vista principal del administrador

La vista administrativa conserva la estructura principal del sistema, pero agrega herramientas de gestion.

Normalmente veras:

- menu lateral;
- modulo `Instaladores`;
- modulo `Gestion de Usuarios`;
- opcion `Ayuda`;
- opcion `Cerrar sesion`.

Historia de pantallazo: Vista principal del administrador.

## Modulo de Instaladores

Este modulo controla los ejecutables visibles dentro del sistema. Desde aqui el administrador puede consultar, registrar, editar o eliminar instaladores.

Cada tarjeta puede incluir:

- nombre del aplicativo;
- descripcion;
- ruta del ejecutable;
- icono;
- boton `Ver`;
- boton `Editar`;
- boton `Eliminar`;
- boton de ejecucion.

Historia de pantallazo: Modulo Instaladores en modo administrador.

## Como agregar un instalador

### Paso 1. Entrar al modulo Instaladores

Desde el menu lateral selecciona `Instaladores`.

Historia de pantallazo: Acceso al modulo Instaladores.

### Paso 2. Abrir el formulario de nuevo instalador

Pulsa la opcion para buscar o registrar un nuevo instalador.

Historia de pantallazo: Boton para agregar o buscar instalador.

### Paso 3. Seleccionar el archivo ejecutable

Busca el archivo `.exe` correspondiente en la ubicacion correcta y seleccionelo.

Historia de pantallazo: Explorador de archivos seleccionando el ejecutable.

### Paso 4. Completar la informacion funcional

Registra:

- nombre visible;
- descripcion;
- carpeta o categoria funcional correspondiente.

Esta informacion es importante porque es la que luego vera el usuario final.

Historia de pantallazo: Formulario completo de instalador.

### Paso 5. Guardar

Guarda el registro. Cuando el proceso termine correctamente, el instalador quedara disponible en el catalogo para futuras asignaciones.

Historia de pantallazo: Confirmacion de instalador guardado.

## Como editar un instalador

Para editar un instalador:

1. localiza la tarjeta correcta;
2. pulsa `Editar`;
3. actualiza nombre, descripcion, ruta o clasificacion segun corresponda;
4. guarda los cambios.

Usa esta opcion cuando el ejecutable cambie, la descripcion necesite ajuste o el aplicativo deba reclasificarse.

Historia de pantallazo: Edicion de un instalador existente.

## Como eliminar un instalador

Para retirar un instalador del sistema:

1. localiza la tarjeta correcta;
2. pulsa `Eliminar`;
3. confirma la accion.

Debes tener cuidado con esta accion, porque el aplicativo dejara de estar visible para nuevos procesos de consulta y asignacion.

Historia de pantallazo: Confirmacion de eliminacion de instalador.

## Modulo Gestion de Usuarios

Este modulo solo esta disponible en modo administrador. Desde aqui se controlan las cuentas y los permisos operativos.

Funciones principales:

- agregar usuario;
- editar usuario;
- eliminar usuario;
- revisar rol;
- asignar aplicativos.

Historia de pantallazo: Vista principal de Gestion de Usuarios.

## Como agregar un usuario

### Paso 1. Abrir Gestion de Usuarios

Selecciona `Gestion de Usuarios` desde el menu lateral.

Historia de pantallazo: Entrada al modulo Gestion de Usuarios.

### Paso 2. Abrir formulario de nuevo usuario

Pulsa la opcion para crear un usuario nuevo.

Historia de pantallazo: Boton Agregar usuario.

### Paso 3. Completar el formulario

Registra como minimo:

- nombres;
- apellidos;
- correo;
- telefono;
- contrasena;
- rol.

Historia de pantallazo: Formulario completo de usuario.

### Paso 4. Guardar

Guarda la informacion. Si todo es correcto, el usuario quedara disponible para login y para asignacion de aplicativos.

Historia de pantallazo: Confirmacion de usuario creado.

## Como editar un usuario

Para editar un usuario:

1. localiza la fila o registro correcto;
2. pulsa `Editar`;
3. modifica los datos necesarios;
4. guarda los cambios.

Esta accion sirve para actualizar nombres, correo, telefono, contrasena o rol.

Historia de pantallazo: Edicion de usuario existente.

## Como eliminar un usuario

Para eliminar un usuario:

1. localiza el registro correcto;
2. pulsa `Eliminar`;
3. confirma la accion.

Al hacerlo, el sistema retira la cuenta y limpia la informacion relacionada segun la logica actual del sistema.

Historia de pantallazo: Confirmacion de eliminacion de usuario.

## Como asignar aplicativos a un usuario

### Paso 1. Seleccionar el usuario

Dentro de Gestion de Usuarios, selecciona la persona a la que deseas asignar aplicativos.

Historia de pantallazo: Seleccion de usuario dentro de la tabla.

### Paso 2. Abrir el panel de asignacion

El sistema despliega el panel de asignacion dentro de la misma vista, sin abrir una ventana aparte.

Historia de pantallazo: Panel de asignacion abierto.

### Paso 3. Marcar los aplicativos permitidos

Selecciona las casillas correspondientes a los aplicativos que esa persona debe ver y ejecutar.

Historia de pantallazo: Casillas de aplicativos marcadas.

### Paso 4. Guardar la asignacion

Guarda los cambios. A partir de ese momento, el catalogo del usuario quedara filtrado segun la seleccion realizada.

Historia de pantallazo: Confirmacion de asignacion guardada.

## Que revisar despues de un cambio importante

Despues de agregar, editar o asignar, valida:

- que el usuario si aparezca en la tabla;
- que el instalador si aparezca en el catalogo;
- que el usuario pueda ingresar correctamente;
- que el catalogo visible coincida con la asignacion hecha.

## Casos de soporte frecuentes

### El usuario no ve su aplicativo

Revisa:

1. que el instalador siga registrado;
2. que el usuario tenga aplicativos asignados;
3. que la ruta del ejecutable siga existiendo.

### El ejecutable no abre

Revisa:

1. que el archivo exista en la ruta;
2. que no haya sido movido o renombrado;
3. que Windows no este bloqueando el archivo;
4. que el usuario tenga permisos sobre esa ubicacion.

### No aparece Gestion de Usuarios

Revisa:

1. que realmente se haya ingresado por el login administrativo;
2. que la contrasena usada sea la administrativa;
3. que el modo administrador este activo.

## Buenas practicas para administracion

- no publiques instaladores con nombres ambiguos;
- valida que la ruta funcione antes de asignarla;
- evita dejar cuentas sin rol definido;
- confirma asignaciones despues de cada cambio relevante;
- usa nombres y descripciones claras para facilitar soporte.

## Contacto y apoyo

Si surge una duda funcional o tecnica, utiliza el centro de ayuda interno del sistema y el canal de soporte definido por la organizacion.
