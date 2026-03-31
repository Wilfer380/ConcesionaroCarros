# Administradores

Esta guía explica el recorrido funcional del administrador dentro del sistema. A diferencia del usuario final, el administrador puede entrar por dos rutas distintas:

- por el login normal, cuando solo necesita operar como usuario estándar;
- por el login administrativo, cuando necesita usar funciones de gestión.

El objetivo de esta guía es dejar claras ambas rutas desde el inicio, diferenciando qué comparte el administrador con el usuario normal y qué pertenece únicamente al modo administrativo.

## Explicación principal

El administrador no siempre trabaja en modo administrativo. También puede abrir la aplicación, registrarse, iniciar sesión y operar como un usuario normal si entra por la ruta estándar.

Eso significa que existen dos escenarios distintos:

1. **Administrador por login normal**
   El administrador entra como cualquier usuario final. En este modo puede ver sus aplicativos asignados, consultar ayuda y cerrar sesión, pero no accede a los módulos de gestión.

2. **Administrador por login administrativo**
   El administrador entra por la ruta especial de administración. En este modo sí se habilitan módulos como `Gestión de Usuarios`, administración de instaladores, asignación de aplicativos y, si aplica, el módulo de logs.

## Objetivo de esta guía

Con esta guía el administrador podrá:

- entender cuándo usar el login normal y cuándo usar el login admin;
- seguir correctamente el proceso desde el acceso directo;
- registrarse si aún no tiene cuenta administrativa;
- iniciar sesión por la ruta adecuada según la tarea;
- gestionar instaladores;
- gestionar usuarios;
- asignar aplicativos;
- consultar ayuda y soporte;
- validar los cambios funcionales realizados.

## Qué debe entender primero el administrador

Antes del recorrido visual, el administrador debe tener claras estas reglas:

1. el acceso siempre comienza desde el mismo acceso directo principal del sistema;
2. entrar por login normal no activa permisos administrativos, aunque la persona tenga rol administrativo en el sistema;
3. si la cuenta administrativa aún no existe, primero debe registrarse desde la ruta administrativa;
4. solo después del login admin correcto se habilitan los módulos de gestión;
5. cada cambio administrativo afecta a otros usuarios y debe revisarse con cuidado.

## Flujo general administrativo

```text
Acceso directo
        |
        v
Pantalla inicial
        |
        +--> Login normal
        |        |
        |        v
        |   Operación como usuario estándar
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
                 +--> Gestión de Usuarios
                 +--> Ayuda
                 \--> Logs según permisos
```

## Inicio del recorrido visual

Desde este punto comienzan los pantallazos de administración. El orden recomendado para pegar las imágenes es:

1. acceso directo;
2. pantalla inicial;
3. diferencia entre login normal y login admin;
4. ingreso por login normal;
5. registro administrativo;
6. login admin;
7. vista principal;
8. instaladores;
9. gestión de usuarios;
10. asignaciones;
11. ayuda;
12. cierre de sesión.

## Paso 1. Abrir la aplicación desde el acceso directo

El administrador inicia desde el mismo acceso directo principal del sistema.

Historia de pantallazo: Acceso directo del sistema en el escritorio.
<!-- Aquí pegas la imagen del acceso directo para administración -->

Historia de pantallazo: Pantalla inicial del sistema antes de elegir tipo de ingreso.
<!-- Aquí pegas la imagen de la pantalla inicial -->

## Paso 2. Entender la diferencia entre login normal y login admin

Esta es una de las reglas más importantes del sistema:

- el login normal es para operación estándar;
- el login administrativo activa permisos de gestión;
- un administrador puede entrar por login normal, pero en ese caso opera igual que un usuario final;
- para ver `Gestión de Usuarios`, administrar instaladores y usar otras funciones avanzadas, debe ingresar por la ruta admin.

Historia de pantallazo: Diferencia visual entre acceso normal y acceso administrativo.
<!-- Aquí pegas la imagen que muestra las dos rutas de acceso -->

## Ruta 1. Administrador operando por login normal

Cuando el administrador entra por login normal, el flujo funcional es el mismo del usuario final.

En esta ruta puede:

- iniciar sesión como usuario estándar;
- ver sus aplicativos asignados;
- abrir la pestaña `Ayuda`;
- cerrar sesión.

En esta ruta no puede:

- ver `Gestión de Usuarios`;
- administrar instaladores;
- usar el módulo de logs;
- operar en modo administrativo.

### Véase también

Para los pasos que son exactamente iguales al flujo del usuario normal, revisa:

- [User](../users/User.md)

Especialmente en estos puntos:

- acceso directo;
- registro normal;
- login normal;
- recuperación de contraseña;
- vista principal estándar;
- uso de ayuda;
- cierre de sesión.

### Paso 3. Ingresar por login normal siendo administrador

Si el administrador solo necesita usar sus aplicativos como cualquier usuario, puede entrar por la ventana normal.

Historia de pantallazo: Login normal usado por un administrador.
<!-- Aquí pegas la imagen del administrador entrando por login normal -->

### Resultado del login normal para un administrador

Después de entrar por login normal:

- verá la misma vista general del usuario estándar;
- no verá módulos de gestión;
- no verá el módulo de logs;
- su experiencia será equivalente a la descrita en la guía `User`.

Historia de pantallazo: Vista estándar del administrador cuando entra por login normal.
<!-- Aquí pegas la imagen de la vista de usuario normal usada por administrador -->

## Ruta 2. Administrador operando por login administrativo

Esta es la ruta que debe usar el administrador cuando necesita hacer cambios funcionales dentro del sistema.

## Paso 4. Registro administrativo

Si la cuenta administrativa aún no existe, primero debe registrarse desde la ruta admin.

### Paso 4.1. Abrir la opción Regístrate del login admin

Desde la pantalla de login administrativo, pulsa la opción `Regístrate`.

Historia de pantallazo: Opción Regístrate dentro del login administrativo.
<!-- Aquí pegas la imagen del acceso al registro admin -->

### Paso 4.2. Completar el formulario de registro administrativo

En el registro administrativo se deben completar:

- correo corporativo;
- rol administrativo;
- contraseña normal;
- contraseña administrativa.

Este registro no es igual al normal. Aquí el sistema crea o actualiza la cuenta base y además registra la información administrativa necesaria para operar en modo admin.

Historia de pantallazo: Formulario completo de registro administrativo.
<!-- Aquí pegas la imagen del formulario de registro admin -->

### Paso 4.3. Guardar el registro administrativo

Pulsa el botón de registro y espera la confirmación.

Si el proceso es correcto:

- se crea o actualiza el usuario base;
- se registra la cuenta administrativa;
- el sistema regresa al login admin con los datos listos para ingresar.

Historia de pantallazo: Confirmación de registro administrativo exitoso.
<!-- Aquí pegas la imagen del mensaje de registro admin exitoso -->

Historia de pantallazo: Validación de registro administrativo por duplicado o error.
<!-- Aquí pegas la imagen de una validación de registro admin -->

## Paso 5. Iniciar sesión por login administrativo

Una vez abierto el acceso directo y, si aplica, completado el registro, el siguiente paso es ingresar por el login admin.

### Paso 5.1. Abrir el login administrativo

Desde la pantalla principal, entra a la opción de acceso administrativo.

Historia de pantallazo: Acceso al login administrativo desde la pantalla inicial.
<!-- Aquí pegas la imagen de apertura del login admin -->

### Paso 5.2. Escribir el usuario administrativo

En este login debes usar el `UsuarioSistema` o identificador administrativo definido para la cuenta.

Historia de pantallazo: Campo de usuario administrativo diligenciado.
<!-- Aquí pegas la imagen del campo de usuario admin -->

### Paso 5.3. Escribir la contraseña administrativa

Ingresa la contraseña administrativa correspondiente. Esta contraseña no se valida igual que la del login normal.

Historia de pantallazo: Campo de contraseña administrativa diligenciado.
<!-- Aquí pegas la imagen del campo de contraseña admin -->

### Paso 5.4. Confirmar el ingreso

Pulsa el botón de acceso. Si todo es correcto, el sistema abrirá la vista principal con modo administrador activo.

Historia de pantallazo: Resultado de login admin exitoso.
<!-- Aquí pegas la imagen del acceso admin correcto -->

## Paso 6. Validaciones del login administrativo

El sistema diferencia mejor los errores del login admin.

### Cuando el usuario administrativo no existe

El sistema indicará que el usuario administrativo no se encuentra registrado.

Historia de pantallazo: Mensaje de usuario administrativo incorrecto.
<!-- Aquí pegas la imagen del error de usuario admin -->

### Cuando la contraseña administrativa es incorrecta

El sistema indicará que la contraseña administrativa es incorrecta.

Historia de pantallazo: Mensaje de contraseña administrativa incorrecta.
<!-- Aquí pegas la imagen del error de contraseña admin -->

## Paso 7. Vista principal del administrador

Cuando el login admin es correcto, la aplicación entra en modo administrador.

Normalmente verá:

- panel lateral;
- módulo `Instaladores`;
- módulo `Gestión de Usuarios`;
- opción `Ayuda`;
- opción `Cerrar sesión`;
- opción `Logs` solo si el perfil cumple la validación especial de soporte.

Historia de pantallazo: Vista principal del administrador con menú completo.
<!-- Aquí pegas la imagen de la vista principal admin -->

## Paso 8. Gestión de instaladores

Desde este módulo se controla el catálogo de ejecutables del sistema.

### Cómo agregar un instalador

1. entra al módulo `Instaladores`;
2. abre el formulario de nuevo instalador;
3. selecciona el ejecutable;
4. completa nombre, descripción y categoría;
5. guarda el registro.

Historia de pantallazo: Módulo Instaladores en modo administrador.
<!-- Aquí pegas la imagen del módulo de instaladores -->

Historia de pantallazo: Formulario de nuevo instalador.
<!-- Aquí pegas la imagen del formulario de instalador -->

Historia de pantallazo: Confirmación de instalador guardado.
<!-- Aquí pegas la imagen de la confirmación del instalador -->

### Cómo editar o eliminar un instalador

Para editar:

1. ubica la tarjeta correcta;
2. pulsa `Editar`;
3. actualiza la información;
4. guarda.

Para eliminar:

1. ubica la tarjeta;
2. pulsa `Eliminar`;
3. confirma la acción.

Historia de pantallazo: Edición de un instalador existente.
<!-- Aquí pegas la imagen de edición de instalador -->

Historia de pantallazo: Confirmación de eliminación de instalador.
<!-- Aquí pegas la imagen de eliminación de instalador -->

## Paso 9. Gestión de usuarios

Este módulo solo está disponible en modo administrador. Desde aquí se administran cuentas y permisos.

### Cómo agregar un usuario

1. abre `Gestión de Usuarios`;
2. pulsa la opción para crear un usuario;
3. completa nombres, apellidos, correo, teléfono, contraseña y rol;
4. guarda la información.

Historia de pantallazo: Vista principal de Gestión de Usuarios.
<!-- Aquí pegas la imagen de la vista principal de gestión de usuarios -->

Historia de pantallazo: Formulario de nuevo usuario.
<!-- Aquí pegas la imagen del formulario de usuario -->

Historia de pantallazo: Confirmación de usuario creado.
<!-- Aquí pegas la imagen de la confirmación de usuario creado -->

### Cómo editar o eliminar un usuario

Para editar:

1. localiza el registro;
2. pulsa `Editar`;
3. actualiza la información;
4. guarda.

Para eliminar:

1. localiza el registro;
2. pulsa `Eliminar`;
3. confirma la acción.

Historia de pantallazo: Edición de usuario existente.
<!-- Aquí pegas la imagen de edición de usuario -->

Historia de pantallazo: Confirmación de eliminación de usuario.
<!-- Aquí pegas la imagen de eliminación de usuario -->

## Paso 10. Asignación de aplicativos

Dentro de `Gestión de Usuarios`, el administrador puede asignar o retirar aplicativos a una persona.

El flujo general es:

1. seleccionar el usuario;
2. abrir el panel de asignación;
3. marcar o desmarcar aplicativos;
4. guardar la asignación.

Historia de pantallazo: Selección de usuario para asignación.
<!-- Aquí pegas la imagen de selección de usuario -->

Historia de pantallazo: Panel de asignación abierto.
<!-- Aquí pegas la imagen del panel de asignación -->

Historia de pantallazo: Aplicativos marcados o desmarcados.
<!-- Aquí pegas la imagen de las casillas de asignación -->

Historia de pantallazo: Confirmación de asignación guardada.
<!-- Aquí pegas la imagen de confirmación de asignación -->

## Paso 11. Uso de la pestaña Ayuda

La pestaña `Ayuda` permite al administrador consultar documentación funcional dentro del sistema, sin tener que salir de la aplicación para buscar manuales o instrucciones.

Desde esta vista normalmente se puede:

- navegar entre carpetas de documentación;
- abrir guías administrativas y funcionales;
- revisar información complementaria;
- ubicar el canal de soporte disponible.

### Paso 11.1. Abrir la pestaña Ayuda

Desde el panel lateral, pulsa la opción `Ayuda`.

Historia de pantallazo: Opción Ayuda visible en el panel lateral del administrador.
<!-- Aquí pegas la imagen de acceso a ayuda desde administración -->

### Paso 11.2. Revisar las carpetas y documentos disponibles

En la vista de ayuda, el administrador verá las carpetas y documentos disponibles para su perfil.

Historia de pantallazo: Vista general del centro de ayuda para administración.
<!-- Aquí pegas la imagen de la vista general de ayuda admin -->

### Paso 11.3. Abrir un documento y consultar su contenido

Selecciona el documento requerido y revisa la información en el panel principal.

Historia de pantallazo: Documento abierto dentro de la pestaña Ayuda para administración.
<!-- Aquí pegas la imagen de un documento abierto en ayuda admin -->

## Paso 12. Cerrar sesión

Cuando el administrador termine:

1. debe volver al panel lateral;
2. pulsar `Cerrar sesión`;
3. confirmar el cierre si aplica;
4. dejar el sistema en la pantalla inicial.

Historia de pantallazo: Cierre de sesión del administrador.
<!-- Aquí pegas la imagen del cierre de sesión admin -->

## Qué revisar después de un cambio importante

Después de agregar, editar, eliminar o asignar, valida:

- que el usuario aparezca correctamente;
- que el instalador siga visible en el catálogo;
- que el usuario pueda ingresar;
- que el catálogo filtrado coincida con la asignación realizada.

## Casos de soporte frecuentes

### El usuario no ve su aplicativo

Revisa:

1. que el instalador exista;
2. que el usuario tenga asignaciones;
3. que la ruta del ejecutable siga siendo válida.

### El ejecutable no abre

Revisa:

1. que el archivo exista en la ruta;
2. que no haya sido movido;
3. que Windows no lo bloquee;
4. que el usuario tenga permisos sobre esa ubicación.

### No aparece Gestión de Usuarios

Revisa:

1. que el ingreso haya sido por login admin;
2. que la contraseña usada sea la administrativa;
3. que el modo administrador esté activo.

## Buenas prácticas para administración

- usa nombres claros en instaladores;
- valida rutas antes de guardar;
- evita cuentas duplicadas;
- confirma asignaciones después de cada cambio;
- diferencia siempre login normal de login admin.

## Contacto y apoyo

Si surge una duda funcional o técnica, utiliza el centro de ayuda interno del sistema y el canal de soporte definido por la organización.
