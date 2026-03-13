# Guia de Usuario

## 1. Que es esta aplicacion

Esta aplicacion permite administrar y ejecutar instaladores empresariales desde una unica interfaz. Su objetivo es centralizar el acceso a aplicativos internos y controlar que usuario puede ver o instalar cada ejecutable.

## 2. Para que sirve

Con este sistema se puede:

- registrar usuarios normales diferentes al rol de administrador 
- registrar administradores
- iniciar sesion como usuario normal o como administrador
- recuperar contrasena desde la pantalla de acceso
- visualizar instaladores autorizados para cada usuario
- organizar instaladores por carpeta funcional
- asignar aplicativos a usuarios desde gestion de usuarios
- editar o eliminar usuarios registrados
- cargar nuevos instaladores al sistema si se tiene perfil administrador

## 3. Tipos de acceso

### Usuario normal

Un usuario normal puede:

- iniciar sesion desde el login principal
- ver solo los instaladores que le fueron asignados 
- ejecutar los instaladores visibles
- usar la recuperacion de contrasena si olvida sus credenciales

Un usuario normal no puede:

- agregar instaladores
- editar instaladores
- eliminar instaladores
- acceder a gestion de usuarios

### Administrador

Un administrador puede:

- iniciar sesion como usuario normal con su contraseña normal
- iniciar sesion como administrador con la contrasena administrativa
- acceder a gestion de usuarios
- crear usuarios
- editar usuarios
- eliminar usuarios
- asignar aplicativos a cada usuario
- registrar instaladores
- editar instaladores
- eliminar instaladores

## 4. Flujo de ingreso

### Login de usuario normal

Desde la pantalla principal de acceso se debe ingresar:

- usuario
- contrasena

El sistema valida si el usuario existe y, si es correcto, abre la ventana principal.

### Recordarme

Si se marca `Recuerdame`, el sistema guarda localmente las credenciales del ultimo acceso para rellenar automaticamente el login en el mismo equipo.

### Login de administrador

El acceso administrativo abre una pantalla separada. Ese login valida:

- usuario administrativo
- contrasena administrativa

Si el acceso es correcto, la sesion se abre en modo administrador y se habilita la gestion de usuarios.

## 5. Registro de usuario normal

En el registro normal se solicita:

- correo corporativo
- contrasena

Reglas importantes:

- el correo debe terminar en `@weg.net`
- si el correo coincide con el correo principal del equipo, el sistema toma como usuario el del dispositivo
- si el correo no pertenece al equipo, el sistema toma como usuario la parte anterior al `@`

Despues del registro, el sistema pasa al login con las credenciales precargadas.

## 6. Registro de administrador

El registro administrativo solicita:

- correo corporativo
- rol administrador
- contrasena normal
- contrasena administrativa

Reglas importantes:

- el correo debe terminar en `@weg.net`
- el rol del registro administrativo es `ADMINISTRADOR`
- la contrasena normal sirve para entrar por el login de usuarios normales
- la contrasena administrativa sirve para entrar por el login de administradores

## 7. Recuperacion de contrasena

La recuperacion se realiza desde el login principal.

### Pasos

1. escribir el correo registrado
2. marcar la validacion `No soy un robot`
3. validar identidad
4. observar el codigo temporal mostrado por la aplicacion
5. escribir el codigo de validacion
6. definir y confirmar la nueva contrasena

Si todo es correcto:

- la nueva contrasena se guarda en la base de datos
- el login principal queda precargado con las nuevas credenciales
- el usuario puede entrar inmediatamente

## 8. Modulo Instaladores

Esta es la pantalla principal de trabajo.

### Que muestra

- lista de instaladores disponibles
- carpetas de organizacion
  - `Punto local de desarrollo planta`
  - `Desarrollo global`
- boton de instalacion por cada aplicativo

### Comportamiento segun el perfil

#### Usuario normal

Ve solo los instaladores asignados por el administrador.

#### Administrador

Ademas de verlos, puede:

- buscar instalador
- registrar uno nuevo
- editar uno existente
- eliminar uno existente

## 9. Gestion de Usuarios

Este modulo solo aparece para administradores.

### Funciones disponibles

- crear usuario
- editar usuario
- eliminar usuario
- ver rol actual
- asignar aplicativos

### Panel de asignacion

Al hacer clic sobre un usuario se abre un panel inferior con:

- nombre del usuario seleccionado
- cargo actual
- listado de roles disponibles en modo solo lectura
- listado de aplicativos del instalador con check

Desde ese panel el administrador puede guardar la asignacion de aplicativos del usuario.

## 10. Roles del sistema

Los roles disponibles son:

- ADMINISTRADOR
- RRHH
- PRODUCCION
- INDUSTRIAL
- ALMACEN
- PCP
- VENTAS
- DESPACHOS
- INGENIERIA
- GERENCIA
- COMPRAS
- CONTRATOS
- SST
- MARKETING
- SISTEMAS (TI)
- CALIDAD

## 11. Comportamiento de las ventanas flotantes

Los formularios como `Agregar usuario` y `Buscar instalador` se abren como ventanas modales.

Eso significa que:

- el resto de la aplicacion queda visualmente atenuado
- el formulario permanece activo hasta que se cierre con `Guardar`, `Cancelar` o `X`

## 12. Recomendaciones de uso

- usar siempre correo corporativo correcto
- verificar el rol antes de guardar un usuario
- asignar aplicativos solo desde gestion de usuarios
- revisar la carpeta correcta al crear un instalador
- no cerrar la aplicacion mientras se este guardando un cambio importante

## 13. Soporte operativo

Si un usuario reporta que no ve un instalador:

1. verificar que el usuario exista en gestion de usuarios
2. verificar que tenga aplicativos asignados
3. verificar que el instalador exista en la carpeta correcta
4. verificar que la ruta del ejecutable siga siendo valida

Si un usuario no puede entrar:

1. verificar si el correo esta registrado
2. verificar si esta entrando por el login correcto
3. usar recuperacion de contrasena si olvido la clave
