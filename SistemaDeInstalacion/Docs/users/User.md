# User

Esta guía está pensada para el usuario final del sistema. Su objetivo es explicar, de forma clara y ordenada, cómo registrarse, ingresar, recuperar acceso, revisar los instaladores asignados y ejecutar los aplicativos disponibles.

## Objetivo de esta guía

Con esta guía el usuario podrá:

- registrarse correctamente en el sistema;
- ingresar con sus credenciales;
- recuperar acceso si olvida la contraseña;
- entender qué ve en la pantalla principal;
- abrir los aplicativos que tenga asignados;
- identificar qué hacer cuando algo no aparece o falla.

## Qué hace la aplicación para el usuario

La aplicación centraliza en una sola interfaz los instaladores y ejecutables que cada usuario tiene autorizados. Esto evita buscar accesos directos en varias carpetas, reduce errores operativos y ayuda a que cada persona vea solo lo que realmente puede usar.

## Antes de comenzar

Antes de ingresar, confirma lo siguiente:

- tu usuario o correo ya fue registrado en el sistema;
- tienes una contraseña activa;
- el administrador ya te asignó los aplicativos que debes ver;
- estás entrando por el login normal y no por el acceso administrativo.

## Flujo general de uso

```text
Abrir la aplicación
        |
        +--> Registrarse por primera vez
        |
        \--> Iniciar sesión
                |
                +--> Login correcto -> catálogo de instaladores asignados
                |
                \--> Login incorrecto -> mensaje específico de validación
```

### Paso 1. Abrir la aplicación

Abre el acceso directo del sistema desde el escritorio o desde la ubicación definida por soporte. Al iniciar, el sistema mostrará la pantalla de acceso para usuarios.

![Pantalla inicial de inicio de sesión normal](image.png)

## Registro de usuario

El registro normal permite crear una cuenta operativa para uso diario del sistema.

### Paso 1. Abrir la opción de registro

Desde la pantalla de login normal, pulsa la opción `Regístrate`.

![Opción de registro desde login normal](image-2.png)

### Paso 2. Completar los datos del registro

En el formulario de registro debes ingresar:

- correo corporativo;
- contraseña.

El sistema completará internamente la información base según las reglas actuales del proyecto y validará que el correo pertenezca al dominio permitido.

### Paso 3. Guardar el registro

Pulsa el botón de registro y espera la confirmación del sistema.

Si el correo ya existe, el sistema lo informará. Si el registro es correcto, volverá al login con los datos listos para ingresar.

![Confirmación de registro de usuario](image-1.png)

## Paso a paso para iniciar sesión

### Paso 2. Escribir usuario o correo

En el campo de usuario puedes escribir:

- tu usuario registrado;
- o tu correo corporativo registrado.

Debes escribirlo exactamente como fue creado en el sistema para evitar errores de validación.

### Paso 3. Escribir la contraseña

Ingresa tu contraseña en el campo correspondiente. Si cometes un error de digitación, el sistema no permitirá el ingreso.

Si tu teclado tiene mayúsculas activadas, revísalo antes de continuar.

### Paso 4. Usar la opción Recuérdame si aplica

Si deseas que el sistema recuerde tus datos en ese equipo, marca la opción `Recuérdame`.

Usa esta opción solo cuando el equipo sea personal o de uso controlado. No se recomienda activarla en equipos compartidos por varias personas.

### Paso 5. Pulsar Ingresar

Pulsa el botón `Ingresar`. Si los datos son correctos, el sistema abrirá la vista principal con los instaladores que tienes asignados.

![Pantalla principal después del login correcto](image-13.png)

## Qué pasa cuando el login falla

Si el sistema no te deja entrar, puede pasar una de estas situaciones:

- el usuario o correo no existe en la base;
- la contraseña no coincide;
- estás escribiendo un correo distinto al registrado;
- aún no te han creado la cuenta.

El sistema ya diferencia cuándo el dato incorrecto es el usuario o correo, y cuándo el dato incorrecto es la contraseña.

![Validación de login fallido](image-6.png)

## Recuperación de contraseña

Si olvidaste la contraseña, usa la ruta de recuperación incluida en la pantalla de acceso.

### Paso 1. Abrir la opción de recuperación

Desde la pantalla de login, entra a la opción de recuperación de acceso.

![Acceso a recuperación de contraseña](image-7.png)

### Paso 2. Ingresar el correo registrado

Escribe el correo corporativo con el que fue creada tu cuenta. El sistema validará si ese correo existe.

### Paso 3. Validar el proceso de recuperación

Sigue las instrucciones que te muestre la pantalla. El sistema puede pedirte validaciones previas antes de permitir el cambio de contraseña.

![Pantalla de validación de recuperación](image-8.png)

### Paso 4. Registrar la nueva contraseña

Cuando el sistema lo permita, escribe la nueva contraseña y confirma el cambio.

![Registro de nueva contraseña](image-12.png)

## Qué pasa cuando el login es correcto

## Vista principal del usuario

Después de iniciar sesión, el usuario accede a la vista de instaladores.

En esta pantalla normalmente encontrará:

- panel lateral con su nombre;
- opción `Instaladores`;
- opción `Ayuda`;
- botón `Cerrar sesión`;
- tarjetas con los aplicativos asignados tanto en la carpeta local de desarrollo planta como en la carpeta de desarrollo global.

![Vista principal del usuario](image-10.png)

## Cómo leer una tarjeta de instalador

Cada tarjeta puede mostrar visualmente:

- nombre del aplicativo;
- ruta o referencia del ejecutable;
- ícono del programa;
- botón `Ver`;
- botón `Instalar` o ejecutar, según la configuración del sistema.

Esto te permite identificar rápidamente qué aplicación estás usando y confirmar si corresponde a tu proceso.

![Lectura de tarjeta de instalador](image-11.png)

## Cómo revisar el detalle de un aplicativo

Si deseas ampliar información antes de abrirlo:

1. ubica la tarjeta del aplicativo;
2. pulsa `Ver`;
3. revisa el nombre, la descripción y la información visible del ejecutable.

![Detalle de un aplicativo](image-5.png)

## Cómo ejecutar un aplicativo asignado

Para abrir un aplicativo:

1. localiza la tarjeta correspondiente;

![Ubicación del aplicativo en la tarjeta](image-15.png)

2. pulsa el botón `Instalar` disponible en la tarjeta;

![Botón Instalar del aplicativo](image-17.png)

3. espera a que Windows abra el ejecutable y da clic en `Ejecutar` para continuar con el proceso;

![Confirmación de ejecución en Windows](image-18.png)

4. espera unos minutos mientras carga la ventana de instalación; después verifica que todo esté correcto y sigue el proceso dando clic en `Instalar` o `Actualizar`;

![Ventana de instalación o actualización](image-19.png)

5. espera unos minutos mientras se instala y, al final, te mostrará una ventana que indica que la instalación finalizó con éxito;

![Instalación finalizada con éxito](image-20.png)

6. al final se abrirá la aplicación automáticamente. Si el archivo está correctamente registrado y la ruta existe, el sistema abrirá la aplicación sin necesidad de pasos adicionales.

![Apertura automática del aplicativo](image-21.png)

## Cómo usar la pestaña de Ayuda

La pestaña `Ayuda` permite consultar la documentación interna del sistema sin salir de la aplicación. Esta vista sirve para revisar procedimientos, entender flujos funcionales y ubicar información de soporte cuando el usuario lo necesite.

Dentro de la pestaña `Ayuda`, normalmente podrás:

- revisar carpetas de documentación;
- abrir guías funcionales;
- leer instrucciones paso a paso;
- ubicar datos de soporte visibles en la pantalla.

### Paso 1. Abrir la pestaña Ayuda

Desde el panel lateral, pulsa la opción `Ayuda`.

![Acceso a la pestaña Ayuda](image-22.png)

### Paso 2. Revisar las carpetas y documentos disponibles

En el centro de ayuda se mostrarán las carpetas y documentos organizados para consulta.

![Carpetas y documentos del centro de ayuda](image-24.png)

### Paso 3. Abrir y leer un documento

Selecciona el documento que necesites y revisa su contenido en el panel principal.

![Documento abierto en Ayuda](image-25.png)

## Cómo cerrar sesión

Cuando termines tu trabajo:

1. ve al panel lateral;
2. pulsa `Cerrar sesión`;
3. el sistema cerrará tu sesión actual y volverá a la pantalla de acceso.

Esto es especialmente importante si el equipo es compartido.

![Cierre de sesión del usuario](image-26.png)

## Qué no puede hacer un usuario final

Un usuario final no puede:

- registrar instaladores nuevos;
- editar instaladores;
- eliminar instaladores;
- entrar a Gestión de Usuarios;
- asignar aplicativos a otras personas;
- abrir el módulo de logs;
- usar permisos administrativos desde el login normal.

## Problemas comunes y qué revisar

### No veo mis instaladores

Revisa lo siguiente:

1. que tu cuenta esté correctamente creada;
2. que el administrador te haya asignado aplicativos;
3. que el ejecutable siga existiendo en la ruta registrada.

### El sistema no me deja ingresar

Verifica:

1. que el usuario o correo esté bien escrito;
2. que la contraseña sea correcta;
3. que no estés usando una cuenta sin registrar;
4. que no estés intentando entrar por una ruta distinta.

### El aplicativo no abre

Puede ocurrir por:

- ruta inexistente;
- archivo movido o eliminado;
- permisos de Windows sobre la ruta;
- cambios operativos no actualizados por administración.

## Recomendaciones de uso

- usa siempre tu cuenta corporativa correcta;
- no compartas tu contraseña;
- cierra sesión al terminar;
- informa a soporte si una ruta ya no abre;
- evita crear cuentas duplicadas cuando el problema real sea de contraseña.

## Contacto

Si el problema no se resuelve con esta guía, utiliza el canal de soporte visible dentro del centro de ayuda del sistema.

![Información de contacto y soporte](image-27.png)
