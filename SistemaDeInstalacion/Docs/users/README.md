# Manual de Usuario

## 1. Objetivo de la aplicación

`SistemaDeInstalacion` es una aplicación de escritorio diseñada para centralizar el acceso a instaladores corporativos y controlar qué aplicativos puede ver o ejecutar cada persona dentro del sistema.

La aplicación fue implementada para:

- organizar instaladores empresariales en un solo lugar
- controlar permisos por usuario y por rol
- permitir administración centralizada de usuarios
- separar el acceso operativo del acceso administrativo
- facilitar la recuperación local de contraseña

## 2. Qué puede hacer la aplicación

Con este sistema se puede:

- registrar usuarios normales
- registrar administradores
- iniciar sesión como usuario normal
- iniciar sesión como administrador
- recuperar contraseña desde la pantalla de acceso
- ver instaladores autorizados
- ejecutar instaladores visibles
- organizar instaladores por carpeta funcional
- asignar aplicativos por usuario desde gestión de usuarios
- registrar, editar y eliminar instaladores cuando se tiene rol administrador

## 3. Perfiles de acceso

### Usuario normal

Puede:

- entrar por el login principal
- ver solo los instaladores que le fueron asignados
- ejecutar los instaladores visibles
- recuperar su contraseña si la olvida

No puede:

- agregar instaladores
- editar instaladores
- eliminar instaladores
- acceder a gestión de usuarios

### Administrador

Un administrador tiene dos formas de usar el sistema.

#### Como usuario normal

Si entra por el login principal con su contraseña normal:

- el sistema lo trata como un usuario operativo normal
- puede ver los instaladores que tenga asignados
- no aparece gestión de usuarios

#### Como administrador

Si entra por el login administrativo con su contraseña administrativa:

- se habilita gestión de usuarios
- puede crear, editar y eliminar usuarios
- puede asignar aplicativos
- puede registrar, editar y eliminar instaladores
- puede asignarse aplicativos a si mismo si lo necesita

## 4. Proceso de ingreso como usuario normal

### Paso 1. Abrir la aplicación

Al ejecutar el sistema aparece el login principal.

### Paso 2. Escribir credenciales

Debe ingresar:

- usuario
- contraseña

### Paso 3. Validación

Si las credenciales son correctas, la aplicación abre la vista principal.

### Opción `Recuérdame`

Si marca `Recuérdame`:

- el sistema guarda localmente las credenciales del último ingreso en ese equipo
- al volver a abrir la aplicación, el login puede aparecer precargado

Si no la marca:

- el login aparecerá vacío la próxima vez

## 5. Acceso al login administrativo

El acceso administrativo se abre con el atajo configurado para administradores. Ese acceso está reservado para las personas que conocen el ingreso administrativo del sistema.


Ese atajo funciona desde:

- el login principal
- el registro normal

El login administrativo valida:

- usuario administrativo
- contraseña administrativa

## 6. Registro de usuario normal

El registro normal solicita:

- correo corporativo
- contraseña

### Reglas del registro

- el correo debe terminar en `@weg.net`
- si el correo corresponde al equipo actual, el sistema toma el usuario del dispositivo
- si el correo no corresponde al equipo, el sistema toma la parte anterior a `@` como usuario de login

### Resultado del registro

Cuando el registro finaliza correctamente:

- el usuario queda guardado en el sistema
- la aplicación vuelve al login principal
- las credenciales quedan cargadas solo para ese ingreso
- si la persona quiere conservarlas, debe marcar `Recuérdame`

## 7. Registro de administrador

El registro administrativo solicita:

- correo corporativo
- rol administrador
- contraseña normal
- contraseña administrativa

### Reglas del registro administrativo

- el correo debe terminar en `@weg.net`
- el rol utilizado es `ADMINISTRADOR`
- la contraseña normal sirve para el login principal
- la contraseña administrativa sirve para el login administrativo

### Resultado del registro

Si el proceso termina bien:

- se guarda el usuario base
- se guarda el registro administrativo
- la aplicación devuelve al login administrativo con las credenciales cargadas

## 8. Recuperación de contraseña

La recuperación se realiza desde el login principal.

### Pasos

1. escribir el correo registrado
2. marcar `No soy un robot`
3. validar la identidad
4. visualizar el código temporal que muestra la aplicación
5. escribir el código en el formulario
6. definir la nueva contraseña
7. confirmar la nueva contraseña

### Resultado

Si el proceso es correcto:

- la nueva contraseña se guarda en la base de datos
- el sistema vuelve al login principal
- las nuevas credenciales quedan precargadas
- el usuario puede entrar inmediatamente

## 9. Vista principal de instaladores

Esta es la pantalla principal de trabajo.

### Qué muestra

- lista de instaladores registrados
- tarjetas por aplicativo
- botón `Ver`
- botón `Instalar`
- botones de `Editar` y `Eliminar` para administradores
- carpetas de organización visual

### Carpetas funcionales actuales

- `Punto local de desarrollo planta`
- `Desarrollo global`

### Comportamiento por perfil

#### Usuario normal

Ve solamente los instaladores que un administrador le haya asignado.

#### Administrador

Además de verlos, puede:

- registrar instaladores
- editar instaladores
- eliminar instaladores
- cambiar entre carpetas funcionales

## 10. Gestión de usuarios

Este módulo solo aparece en una sesión iniciada como administrador.

### Funciones disponibles

- crear usuario
- editar usuario
- eliminar usuario
- consultar rol actual
- asignar aplicativos

### Panel de asignación

Al hacer clic sobre un usuario se abre un panel inferior con:

- nombre del usuario seleccionado
- cargo actual
- listado visual de roles
- listado de aplicativos del sistema con casillas de selección

Desde ese panel el administrador puede guardar qué instaladores quedarán visibles para esa persona.

## 11. Roles disponibles

Los roles configurados actualmente son:

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

se muestran como ventanas modales. Eso significa que:

- el fondo de la aplicación se ve atenuado
- el formulario queda como foco principal
- solo se cierra con `Guardar`, `Cancelar` o `X`

## 13. Recomendaciones de uso

- usar siempre el correo corporativo correcto
- verificar el rol antes de guardar un usuario
- asignar aplicativos solo después de confirmar que el usuario seleccionado es el correcto
- revisar la carpeta funcional correcta antes de guardar un instalador
- no cerrar la aplicación mientras se está guardando información

## 14. Soporte operativo rápido

### Si una persona no ve instaladores

Revisar:

1. que el usuario exista en gestión de usuarios
2. que tenga aplicativos asignados
3. que el instalador exista en la carpeta correcta
4. que la ruta del ejecutable siga siendo válida

### Si una persona no puede iniciar sesión

Revisar:

1. que el usuario o correo exista en el sistema
2. que esté usando el login correcto
3. si es administrador, que no esté usando la contraseña equivocada
4. usar `Recuperar acceso` si olvidó la clave

### Si un administrador no ve gestión de usuarios

Revisar:

1. que haya ingresado por el login administrativo
2. que el rol del usuario sea `ADMINISTRADOR`
3. que exista el registro administrativo correspondiente
