# Manual de Usuario

## 1. Objetivo de la aplicacion

`SistemaDeInstalacion` es una aplicacion de escritorio para administrar y ejecutar instaladores corporativos desde una unica interfaz. Su objetivo es que cada persona vea solamente los aplicativos que le fueron asignados y que la administracion del sistema quede centralizada en usuarios con rol `ADMINISTRADOR`.

La aplicacion fue implementada para:

- centralizar instaladores internos de la organizacion
- organizar instaladores por contexto funcional
- permitir que un administrador controle a que persona se le asigna cada aplicativo
- reducir instalaciones manuales sin control
- permitir recuperacion local de contrasena cuando un usuario olvida su acceso

## 2. Perfiles de uso

La aplicacion maneja dos tipos de acceso:

### Usuario normal

Es cualquier usuario que entra por el login principal con su usuario y su contrasena normal.

Puede:

- iniciar sesion desde el acceso principal
- ver solo los instaladores que le fueron asignados
- ejecutar los aplicativos visibles
- usar la recuperacion de contrasena

No puede:

- abrir gestion de usuarios
- crear usuarios
- editar usuarios
- eliminar usuarios
- registrar instaladores
- editar instaladores
- eliminar instaladores

### Administrador

Un administrador tiene dos formas de entrar al sistema:

1. como usuario normal
2. como administrador

#### Administrador entrando como usuario normal

Cuando el administrador entra por el login principal usando su contrasena normal, el sistema lo trata como un usuario operativo normal. En ese escenario:

- puede ver los aplicativos que tenga asignados
- puede ejecutar instaladores visibles
- no aparece el modulo de gestion de usuarios

Esto permite que un administrador se comporte como un usuario final cuando necesite validar su propia experiencia dentro del sistema.

#### Administrador entrando como administrador

Cuando el administrador entra por el login administrativo usando la contrasena administrativa:

- se habilita el acceso completo a gestion de usuarios
- puede crear usuarios
- puede editar usuarios
- puede eliminar usuarios
- puede registrar instaladores
- puede editar instaladores
- puede eliminar instaladores
- puede asignar aplicativos a cada usuario
- puede asignarse aplicativos a si mismo

## 3. Pantallas principales

### Login principal

Es la pantalla de acceso para cualquier persona usuaria del sistema.

Campos y opciones:

- `Usuario`
- `Contrasena`
- `Recuerdame`
- `Registrate`
- `Recuperar acceso`

### Login administrativo

Es una pantalla separada, exclusiva para administradores. Valida:

- usuario administrativo
- contrasena administrativa

### Registro normal

Pantalla para registro de usuarios que van a operar la aplicacion como usuarios normales.

### Registro administrativo

Pantalla para alta de administradores.

### Vista principal

Una vez iniciada la sesion, el sistema muestra:

- panel lateral con el nombre del usuario autenticado
- modulo `Instalador`
- modulo `Gestion de Usuarios` solo si la sesion es administrativa

## 4. Proceso de ingreso para usuario normal

### Paso 1. Abrir la aplicacion

Al iniciar la aplicacion se presenta el login principal.

### Paso 2. Ingresar credenciales

El usuario debe escribir:

- su usuario
- su contrasena normal

### Paso 3. Validacion del sistema

El sistema:

- busca el usuario registrado
- resuelve el correo asociado a ese usuario
- valida la contrasena
- abre la vista principal si las credenciales son correctas

### Opcion `Recuerdame`

Si el usuario marca `Recuerdame`:

- la aplicacion guarda localmente las credenciales recordadas en el equipo actual
- la proxima vez, el login puede aparecer precargado

Si no marca `Recuerdame`:

- las credenciales no se guardan
- el login aparecera vacio en el siguiente ingreso

## 5. Proceso de ingreso para administrador

### Acceso al login administrativo

Desde el login principal y tambien desde el registro normal, el sistema permite abrir el acceso administrativo con el atajo:

- `Ctrl + M`

Este acceso abre la pantalla de login administrativo.

### Credenciales que usa el administrador

El administrador tiene dos credenciales distintas:

- contrasena normal: para entrar por el login principal como usuario normal
- contrasena administrativa: para entrar por el login administrativo

### Resultado del ingreso administrativo

Si el acceso es correcto:

- se habilita `Gestion de Usuarios`
- la sesion queda marcada como administrativa
- la aplicacion permite administrar usuarios e instaladores

## 6. Registro de usuario normal

El registro normal solicita:

- correo corporativo
- contrasena

### Reglas del correo

- el correo debe terminar en `@weg.net`
- el sistema no acepta correos externos como `@gmail.com` o equivalentes

### Como se define el usuario de login

El sistema usa estas reglas:

- si el correo ingresado coincide con el correo principal del equipo actual, el usuario de login se toma del dispositivo
- si el correo no coincide con el del equipo, el usuario de login se construye con la parte anterior a `@`

Ejemplo:

- correo: `juan.perez@weg.net`
- usuario de login: `juan.perez`

### Resultado del registro

Si el registro termina bien:

- el usuario queda guardado en la base de datos
- el sistema vuelve al login principal
- las credenciales quedan precargadas solo para ese momento
- si la persona quiere recordarlas, debe marcar `Recuerdame` en el login

## 7. Registro de administrador

El registro administrativo solicita:

- correo corporativo
- rol
- contrasena normal
- contrasena administrativa

### Reglas importantes

- el correo debe terminar en `@weg.net`
- el rol permitido en este registro es `ADMINISTRADOR`
- la contrasena normal queda asociada al acceso por login principal
- la contrasena administrativa queda asociada al acceso por login de administrador

### Resultado del registro

Si el proceso termina correctamente:

- se crea o actualiza el usuario base en `Usuarios`
- se crea o actualiza el registro en `Administrador`
- el sistema lleva automaticamente al login administrativo con los datos precargados

## 8. Recuperacion de contrasena

La recuperacion se realiza desde el login principal.

### Flujo completo

1. escribir el correo registrado
2. marcar `No soy un robot`
3. validar la identidad
4. visualizar el codigo temporal mostrado por la aplicacion
5. ingresar el codigo recibido visualmente
6. escribir la nueva contrasena
7. confirmar la nueva contrasena

### Resultado

Si el proceso es correcto:

- la nueva contrasena se guarda en la base de datos
- el sistema regresa al login principal
- el login queda precargado con la nueva clave
- el usuario puede ingresar inmediatamente

## 9. Modulo Instalador

Esta es la vista principal de trabajo despues del login.

### Que muestra

- lista de instaladores registrados
- tarjeta por aplicativo
- boton `Ver`
- boton `Editar` para administradores
- boton `Eliminar` para administradores
- boton `Instalar`
- selector visual por carpeta funcional

### Carpetas funcionales actuales

- `Punto local de desarrollo planta`
- `Desarrollo global`

### Comportamiento por perfil

#### Usuario normal

Ve solo los instaladores que le fueron asignados.

#### Administrador en modo administrador

Puede ademas:

- buscar instalador
- registrar uno nuevo
- editar uno existente
- eliminar uno existente

## 10. Gestion de Usuarios

Este modulo solo se muestra cuando la sesion fue iniciada como administrador.

### Funciones del modulo

- crear usuario
- editar usuario
- eliminar usuario
- visualizar el rol actual
- asignar aplicativos por usuario

### Como funciona la asignacion

Cuando el administrador hace clic sobre un usuario en la tabla:

- se abre un panel inferior de asignacion
- se muestra el nombre del usuario seleccionado
- se muestra el cargo actual
- se listan los roles disponibles en modo visual
- se listan los aplicativos registrados en el sistema con check

El administrador puede:

- marcar o desmarcar aplicativos
- guardar la asignacion

### Regla de roles en el panel

El panel de asignacion refleja el rol actual del usuario. El cambio de rol como tal se controla desde la edicion del usuario, no desde el panel inferior de aplicativos.

## 11. Roles disponibles

Los roles actualmente disponibles son:

- `ADMINISTRADOR`
- `RRHH`
- `PRODUCCION`
- `INDUSTRIAL`
- `ALMACEN`
- `PCP`
- `VENTAS`
- `DESPACHOS`
- `INGENIERIA`
- `GERENCIA`
- `COMPRAS`
- `CONTRATOS`
- `SST`
- `MARKETING`
- `SISTEMAS (TI)`
- `CALIDAD`

## 12. Formularios flotantes

Los formularios como:

- `Agregar usuario`
- `Buscar instalador`

se comportan como ventanas modales.

Esto significa que:

- el contenido del fondo queda atenuado
- el formulario permanece como foco principal
- solo se cierra con `Guardar`, `Cancelar` o `X`

## 13. Recomendaciones operativas

- usar siempre el correo corporativo correcto
- verificar el rol antes de guardar un usuario
- no asignar aplicativos si antes no se ha seleccionado el usuario correcto
- revisar la carpeta funcional correcta al registrar un instalador
- confirmar que la ruta del ejecutable exista antes de registrarlo
- evitar cerrar la aplicacion mientras se guarda informacion

## 14. Soporte y validaciones basicas

### Si una persona no ve aplicativos

Verificar:

1. que el usuario exista en `Gestion de Usuarios`
2. que tenga aplicativos asignados
3. que el instalador exista en la carpeta correcta
4. que la ruta del ejecutable siga siendo valida

### Si una persona no puede iniciar sesion

Verificar:

1. que el usuario o correo exista en el sistema
2. que este usando el login correcto
3. si es administrador, que no este mezclando contrasena normal con contrasena administrativa
4. usar `Recuperar acceso` si olvido la clave

### Si un administrador no ve `Gestion de Usuarios`

Verificar:

1. que haya ingresado por el login administrativo
2. que el rol del usuario sea `ADMINISTRADOR`
3. que exista registro en la tabla de administradores
