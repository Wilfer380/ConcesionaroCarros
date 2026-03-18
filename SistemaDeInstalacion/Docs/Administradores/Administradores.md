# Administradores

Esta guía describe el funcionamiento completo del sistema para administradores. Su objetivo es que una persona encargada de la operación pueda usar la aplicación con criterio, entender el impacto de cada acción y mantener el catálogo de ejecutables de forma ordenada.

## Finalidad del rol administrador

El administrador es responsable de:

- registrar y mantener ejecutables;
- crear, editar y eliminar usuarios;
- asignar aplicativos por persona;
- clasificar correctamente los proyectos por carpeta funcional;
- mantener ordenado el acceso operativo del sistema.

## Flujo administrativo general

```text
Registro administrativo
        |
        v
Login administrativo
        |
        v
MainWindow
  |
  +--> Instaladores
  |      +- Buscar instalador
  |      +- Ver
  |      +- Editar
  |      +- Eliminar
  |      \- Instalar
  |
  \--> Gestión de Usuarios
         +- Agregar usuario
         +- Editar usuario
         +- Eliminar usuario
         \- Asignar aplicativos
```

## Historias de usuario

### Historia 1

Como administrador, quiero registrar un ejecutable en la carpeta correcta para que los usuarios encuentren el aplicativo según su alcance real.

Resultado esperado:

- el aplicativo queda visible en el catálogo;
- se clasifica en la carpeta correcta;
- queda disponible para futuras asignaciones.

### Historia 2

Como administrador, quiero crear un usuario y asignarle solo los aplicativos necesarios para controlar el acceso mínimo requerido.

Resultado esperado:

- el usuario queda guardado en la base;
- la cuenta puede iniciar sesión;
- el catálogo visible queda limitado a sus aplicativos asignados.

### Historia 3

Como administrador, quiero editar o retirar un ejecutable cuando ya no debe usarse para mantener el catálogo actualizado.

Resultado esperado:

- el cambio impacta el catálogo de forma inmediata;
- los usuarios no ven instaladores obsoletos;
- la operación queda alineada con la base de datos.

## Registro de administrador

La ruta administrativa permite crear la cuenta privilegiada.

Campos principales del formulario:

- correo corporativo;
- rol;
- contraseña normal;
- contraseña administrativa.

Qué hace el sistema al guardar:

1. valida el dominio corporativo;
2. crea o actualiza el usuario base en `Usuarios`;
3. crea o actualiza el registro en `Administrador`;
4. conserva separadas la contraseña normal y la contraseña administrativa.

## Login administrativo

El acceso administrativo no es igual al login normal. Para entrar como administrador:

1. abre la ruta administrativa;
2. escribe `UsuarioSistema`;
3. escribe la contraseña administrativa;
4. confirma el ingreso.

Si el proceso es correcto:

- se activa el modo administrador;
- aparece `Gestión de Usuarios`;
- el catálogo permite alta, edición y eliminación de instaladores.

## Bosquejo de interfaz administrativa

```text
Menú lateral
|
+- Instaladores
+- Gestión de Usuarios
+- Ayuda
\- Cerrar sesión

Área central
|
+- Vista de Instaladores
|  +- filtros por carpeta
|  +- tarjetas de aplicativos
|  +- acciones de ver, editar, eliminar e instalar
|
\- Vista de Gestión de Usuarios
   +- tabla de usuarios
   +- acciones por fila
   \- panel inferior de asignación
```

## Módulo de instaladores

La pantalla de `Instaladores` es el catálogo principal del sistema.

Cada tarjeta puede mostrar:

- nombre visible;
- ruta registrada;
- ícono del ejecutable;
- botón `Ver`;
- botón `Editar`;
- botón `Eliminar`;
- botón `Instalar`.

## Clasificación por carpetas funcionales

### Desarrollo global

Usa esta carpeta cuando el ejecutable tiene alcance corporativo amplio o puede ser utilizado por varias sedes.

Ejemplos de uso:

- soluciones generales para WEG;
- proyectos que aplican en Medellín, Sabaneta u otras sedes;
- herramientas de soporte transversal.

### Punto local de desarrollo planta

Usa esta carpeta cuando el ejecutable fue creado solo para uso interno de planta en Sabaneta y no debe tratarse como aplicativo corporativo general.

Ejemplos de uso:

- soluciones internas exclusivas de la sede;
- herramientas de apoyo local de operación;
- ejecutables que no deben distribuirse a otras sedes.

## Procedimiento para agregar un ejecutable

1. entra a `Instaladores`;
2. pulsa `Buscar Instalador`;
3. selecciona el archivo `.exe`;
4. asigna nombre visible;
5. agrega descripción si aplica;
6. clasifica la carpeta funcional correcta;
7. guarda el registro.

Recomendaciones:

- usa rutas estables de red o carpetas controladas;
- evita rutas temporales o personales;
- confirma que el archivo abra antes de publicarlo;
- no dupliques el mismo proyecto con nombres ambiguos.

## Procedimiento para editar o eliminar un ejecutable

### Editar

Permite ajustar nombre, descripción y clasificación.

Advertencia:

- si la ruta real cambia fuera del sistema, las asignaciones pueden dejar de coincidir.

### Eliminar

Quita el instalador del catálogo.

Impacto:

- deja de verse en la interfaz;
- deja de estar disponible para nuevas asignaciones;
- un usuario puede conservar una ruta histórica en su JSON si no se limpia después.

## Gestión de Usuarios

Este módulo solo está disponible en modo administrador.

Funciones principales:

- agregar usuario;
- editar usuario;
- eliminar usuario;
- consultar rol;
- asignar aplicativos.

## Procedimiento para agregar un usuario

1. abre `Gestión de Usuarios`;
2. pulsa `Agregar usuario`;
3. completa nombres, apellidos, correo, teléfono, contraseña y rol;
4. guarda el formulario.

Resultado esperado:

- se crea el registro en `Usuarios`;
- el usuario queda listo para login y asignación de aplicativos.

## Procedimiento para editar un usuario

Se puede modificar:

- nombres;
- apellidos;
- correo;
- teléfono;
- contraseña;
- rol.

Si el usuario también es administrador, la actualización puede sincronizar información con la tabla `Administrador`.

## Procedimiento para eliminar un usuario

Al eliminar un usuario:

- se remueve de `Usuarios`;
- se limpian registros asociados de recuperación de contraseña;
- si tenía cuenta administrativa relacionada, también se elimina su registro administrativo.

## Asignación de aplicativos

Cuando se selecciona un usuario en la tabla, el sistema no abre una ventana aparte. La implementación actual despliega un panel inferior dentro de la misma vista.

Ese panel muestra:

- usuario seleccionado;
- cargo o rol;
- listado de aplicativos;
- casillas de selección;
- acción de guardado.

Flujo recomendado:

1. selecciona el usuario correcto;
2. verifica su rol;
3. marca los aplicativos autorizados;
4. guarda la asignación;
5. valida el resultado con una prueba de acceso si el cambio es crítico.

## Relación entre interfaz y base de datos

| Acción administrativa | Impacto principal |
|---|---|
| Registrar administrador | `Usuarios` y `Administrador` |
| Login administrativo | lectura de `Administrador` y `Usuarios` |
| Agregar usuario | `Usuarios` |
| Editar usuario | `Usuarios` y, si aplica, `Administrador` |
| Eliminar usuario | `Usuarios`, `Administrador`, `PasswordRecoveryLog` |
| Agregar instalador | `Instaladores` |
| Editar instalador | `Instaladores` |
| Eliminar instalador | `Instaladores` |
| Guardar asignación | `Usuarios.AplicativosJson` |

## Casos de soporte frecuentes

### Un usuario no ve su aplicativo

Revisa:

1. que el usuario tenga aplicativos asignados;
2. que el ejecutable siga registrado;
3. que la ruta del instalador siga vigente.

### El ejecutable no abre

Revisa:

1. que la ruta exista;
2. que el archivo no haya sido movido;
3. que el usuario tenga permisos sobre la ruta;
4. que no exista bloqueo del sistema operativo.

### No aparece Gestión de Usuarios

Revisa:

1. que el acceso haya sido por login administrativo;
2. que la contraseña usada sea la administrativa;
3. que `ModoAdministrador` haya quedado activo.

## Diagrama de decisión para clasificar proyectos

```text
¿El ejecutable será usado por varias sedes?
        |
        +--> Sí -> Desarrollo global
        |
        \--> No
               |
               v
¿Es solo para operación interna de planta Sabaneta?
               |
               +--> Sí -> Punto local de desarrollo planta
               |
               \--> No -> revisar con desarrollo antes de publicar
```

## Documentos complementarios

- [Visión general del sistema](Docs/Sistema.md)
- [Guía de usuario](Docs/users/User.md)
- [Guía de desarrollo](Docs/Developers/Developer.md)
- [Guía de base de datos](Docs/Developers/BaseDeDatos.md)

## Enlaces de apoyo externo

Estos enlaces no reemplazan la documentación interna del proyecto, pero sí sirven como apoyo para entender tecnologías y conceptos usados por la aplicación:

- [Documentación oficial de WPF en Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/)
- [Documentación oficial de .NET Framework en Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/framework/)
- [Documentación oficial de SQLite](https://sqlite.org/docs.html)

## Checklist operativo del administrador

- validar que la cuenta usada sea administrativa;
- confirmar que el ejecutable existe y abre;
- clasificar bien la carpeta funcional;
- mantener actualizados usuarios y permisos;
- revisar impactos en asignaciones al editar rutas;
- usar el centro de ayuda interno cuando haya dudas.
