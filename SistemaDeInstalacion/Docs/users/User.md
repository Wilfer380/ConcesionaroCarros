# User

Esta guia esta pensada para el usuario final del sistema. Su objetivo es explicar, de forma clara y ordenada, como registrarse, ingresar, recuperar acceso, revisar los instaladores asignados y ejecutar los aplicativos disponibles.


## Objetivo de esta guia

Con esta guia el usuario podra:

- registrarse correctamente en el sistema;
- ingresar con sus credenciales;
- recuperar acceso si olvida la contrasena;
- entender que ve en la pantalla principal;
- abrir los aplicativos que tenga asignados;
- identificar que hacer cuando algo no aparece o falla.

## Que hace la aplicacion para el usuario

La aplicacion centraliza en una sola interfaz los instaladores y ejecutables que cada usuario tiene autorizados. Esto evita buscar accesos directos en varias carpetas, reduce errores operativos y ayuda a que cada persona vea solo lo que realmente puede usar.

## Antes de comenzar    

Antes de ingresar, confirma lo siguiente:

- tu usuario o correo ya fue registrado en el sistema;
- tienes una contrasena activa;
- el administrador ya te asigno los aplicativos que debes ver;
- estas entrando por el login normal y no por el acceso administrativo.

## Flujo general de uso

```text
Abrir la aplicacion
        |
        +--> Registrarse por primera vez
        |
        \--> Iniciar sesion
                |
                +--> Login correcto -> catalogo de instaladores asignados
                |
                \--> Login incorrecto -> mensaje especifico de validacion
```

### Paso 1. Abrir la aplicacion

Abre el acceso directo del sistema desde el escritorio o desde la ubicacion definida por soporte. Al iniciar, el sistema mostrara la pantalla de acceso para usuarios.

![alt text](image.png)
## Registro de usuario

El registro normal permite crear una cuenta operativa para uso diario del sistema.

### Paso 1. Abrir la opcion de registro

Desde la pantalla de login normal, pulsa la opcion `Registrate`.
![alt text](image-2.png)

### Paso 2. Completar los datos del registro

En el formulario de registro debes ingresar:

- correo corporativo;
- contrasena.

El sistema completara internamente la informacion base segun las reglas actuales del proyecto y validara que el correo pertenezca al dominio permitido.

### Paso 3. Guardar el registro

Pulsa el boton de registro y espera la confirmacion del sistema.

Si el correo ya existe, el sistema lo informara. Si el registro es correcto, volvera al login con los datos listos para ingresar.

![alt text](image-1.png)

## Paso a paso para iniciar sesion

### Paso 2. Escribir usuario o correo

En el campo de usuario puedes escribir:

- tu usuario registrado;
- o tu correo corporativo registrado.

Debes escribirlo exactamente como fue creado en el sistema para evitar errores de validacion.

### Paso 3. Escribir la contrasena

Ingresa tu contrasena en el campo correspondiente. Si cometes un error de digitacion, el sistema no permitira el ingreso.

Si tu teclado tiene mayusculas activadas, revisalo antes de continuar.

### Paso 4. Usar la opcion Recuerdame si aplica

Si deseas que el sistema recuerde tus datos en ese equipo, marca la opcion `Recuerdame`.

Usa esta opcion solo cuando el equipo sea personal o de uso controlado. No se recomienda activarla en equipos compartidos por varias personas.


### Paso 5. Pulsar Ingresar

Pulsa el boton `Ingresar`. Si los datos son correctos, el sistema abrira la vista principal con los instaladores que tienes asignados.

![alt text](image-3.png)

## Que pasa cuando el login falla

Si el sistema no te deja entrar, puede pasar una de estas situaciones:

- el usuario o correo no existe en la base;
- la contrasena no coincide;
- estas escribiendo un correo distinto al registrado;
- aun no te han creado la cuenta.

El sistema ya diferencia cuando el dato incorrecto es el usuario o correo, y cuando el dato incorrecto es la contrasena.

Historia de pantallazo: Mensaje de usuario o correo incorrecto.
![Mensaje de usuario o correo incorrecto](login-usuario-07-error-usuario.png)

Historia de pantallazo: Mensaje de contrasena incorrecta.
![Mensaje de contrasena incorrecta](login-usuario-08-error-contrasena.png)

## Recuperacion de contrasena

Si olvidaste la contrasena, usa la ruta de recuperacion incluida en la pantalla de acceso.

### Paso 1. Abrir la opcion de recuperacion

Desde la pantalla de login, entra a la opcion de recuperacion de acceso.

Historia de pantallazo: Opcion de recuperacion desde login.
![Opcion de recuperacion desde login](recuperacion-usuario-01-acceso.png)

### Paso 2. Ingresar el correo registrado

Escribe el correo corporativo con el que fue creada tu cuenta. El sistema validara si ese correo existe.

Historia de pantallazo: Campo de correo en recuperacion.
![Campo de correo en recuperacion](recuperacion-usuario-02-correo.png)

### Paso 3. Validar el proceso de recuperacion

Sigue las instrucciones que te muestre la pantalla. El sistema puede pedirte validaciones previas antes de permitir el cambio de contrasena.

Historia de pantallazo: Paso intermedio del flujo de recuperacion.
![Paso intermedio del flujo de recuperacion](recuperacion-usuario-03-validacion.png)

### Paso 4. Registrar la nueva contrasena

Cuando el sistema lo permita, escribe la nueva contrasena y confirma el cambio.

Historia de pantallazo: Formulario para definir nueva contrasena.
![Formulario para definir nueva contrasena](recuperacion-usuario-04-nueva-contrasena.png)

## Que pasa cuando el login es correcto

Cuando el acceso es exitoso:

- se abre el catalogo principal;
- se muestra tu nombre en el panel lateral;
- solo ves los aplicativos autorizados para tu cuenta;
- puedes revisar informacion del instalador y ejecutarlo.

## Vista principal del usuario

Despues de iniciar sesion, el usuario accede a la vista de instaladores.

En esta pantalla normalmente encontrara:

- panel lateral con su nombre;
- opcion `Instaladores`;
- opcion `Ayuda`;
- boton `Cerrar sesion`;
- tarjetas con los aplicativos asignados.

Historia de pantallazo: Vista general del modulo de instaladores del usuario.
![Vista general del modulo de instaladores del usuario](usuario-vista-01-catalogo.png)

## Como leer una tarjeta de instalador

Cada tarjeta puede mostrar:

- nombre del aplicativo;
- ruta o referencia del ejecutable;
- icono del programa;
- boton `Ver`;
- boton `Instalar` o ejecutar segun la configuracion del sistema.

Esto te permite identificar rapidamente que aplicacion estas usando y confirmar si corresponde a tu proceso.

Historia de pantallazo: Tarjeta individual de un instalador.
![Tarjeta individual de un instalador](usuario-vista-02-tarjeta-instalador.png)

## Como revisar el detalle de un aplicativo

Si deseas ampliar informacion antes de abrirlo:

1. ubica la tarjeta del aplicativo;
2. pulsa `Ver`;
3. revisa el nombre, la descripcion y la informacion visible del ejecutable.

Historia de pantallazo: Vista o panel de detalle del instalador.
![Vista o panel de detalle del instalador](usuario-vista-03-detalle-instalador.png)

## Como ejecutar un aplicativo asignado

Para abrir un aplicativo:

1. localiza la tarjeta correspondiente;
2. confirma que sea el aplicativo correcto;
3. pulsa el boton de ejecucion o instalacion disponible en la tarjeta;
4. espera a que Windows abra el ejecutable.

Si el archivo esta correctamente registrado y la ruta existe, el sistema abrira la aplicacion sin necesidad de pasos adicionales.

Historia de pantallazo: Ejecucion de un aplicativo desde la tarjeta.
![Ejecucion de un aplicativo desde la tarjeta](usuario-vista-04-ejecucion-aplicativo.png)

## Como cerrar sesion

Cuando termines tu trabajo:

1. ve al panel lateral;
2. pulsa `Cerrar sesion`;
3. el sistema cerrara tu sesion actual y volvera a la pantalla de acceso.

Esto es especialmente importante si el equipo es compartido.

Historia de pantallazo: Boton Cerrar sesion en el menu lateral.
![Boton Cerrar sesion en el menu lateral](usuario-vista-05-cerrar-sesion.png)

## Que no puede hacer un usuario final

Un usuario final no puede:

- registrar instaladores nuevos;
- editar instaladores;
- eliminar instaladores;
- entrar a Gestion de Usuarios;
- asignar aplicativos a otras personas;
- abrir el modulo de logs;
- usar permisos administrativos desde el login normal.

## Problemas comunes y que revisar

### No veo mis instaladores

Revisa lo siguiente:

1. que tu cuenta este correctamente creada;
2. que el administrador te haya asignado aplicativos;
3. que el ejecutable siga existiendo en la ruta registrada.

### El sistema no me deja ingresar

Verifica:

1. que el usuario o correo este bien escrito;
2. que la contrasena sea correcta;
3. que no estes usando una cuenta sin registrar;
4. que no estes intentando entrar por una ruta distinta.

### El aplicativo no abre

Puede ocurrir por:

- ruta inexistente;
- archivo movido o eliminado;
- permisos de Windows sobre la ruta;
- cambios operativos no actualizados por administracion.

## Recomendaciones de uso

- usa siempre tu cuenta corporativa correcta;
- no compartas tu contrasena;
- cierra sesion al terminar;
- informa a soporte si una ruta ya no abre;
- evita crear cuentas duplicadas cuando el problema real sea de contrasena.

## Contacto

Si el problema no se resuelve con esta guia, utiliza el canal de soporte visible dentro del centro de ayuda del sistema.
