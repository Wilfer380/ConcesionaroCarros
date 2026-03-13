# Guia Tecnica para Desarrolladores

## 1. Proposito del sistema

`SistemaDeInstalacion` es una aplicacion WPF para la administracion centralizada de instaladores corporativos. El sistema resuelve tres necesidades principales:

- controlar autenticacion de usuarios normales y administradores
- registrar y mantener un catalogo de instaladores corporativos
- asignar permisos de visualizacion y ejecucion de aplicativos por usuario

No es un sistema comercial ni transaccional. El dominio actual del producto es exclusivamente la gestion de instaladores y usuarios del ecosistema interno.

## 2. Alcance funcional actual

El producto cubre estos procesos:

- login normal
- registro normal
- login administrativo
- registro administrativo
- recuperacion local de contrasena
- consulta y ejecucion de instaladores
- administracion de usuarios
- asignacion de aplicativos por persona

## 3. Stack tecnologico

- Framework: `.NET Framework 4.8`
- UI: `WPF`
- Patron de presentacion: `MVVM` liviano
- Base de datos: `SQLite`
- Acceso a datos: `Microsoft.Data.Sqlite`
- Persistencia local de credenciales recordadas: `ProtectedData` del usuario de Windows
- Recursos visuales: `XAML`, `ResourceDictionary` en `App.xaml`, recursos graficos del proyecto

## 4. Solucion y proyecto activos

- Solucion: `SistemaDeInstalacion.sln`
- Proyecto principal: `SistemaDeInstalacion.csproj`
- Base de datos activa: `WegInstaladores.db`

Observacion:

- Todavia existen namespaces internos bajo `ConcesionaroCarros`. Eso no rompe la ejecucion, pero debe considerarse como deuda tecnica pendiente.

## 5. Estructura activa del proyecto

### Vistas principales

- `Views/LoginView.xaml`
- `Views/RegisterView.xaml`
- `Views/AdminLoginView.xaml`
- `Views/AdminRegisterView.xaml`
- `Views/MicrosoftRecoveryView.xaml`
- `Views/InstaladoresView.xaml`
- `Views/GestionUsuarioView.xaml`
- `Views/FormularioInstaladorView.xaml`
- `Views/FormularioUsuarioView.xaml`
- `Views/RecoveryCodePopupView.xaml`

### ViewModels activos

- `MainViewModel`
- `LoginViewModel`
- `RegisterViewModel`
- `AdminLoginViewModel`
- `AdminRegisterViewModel`
- `MicrosoftRecoveryViewModel`
- `InstaladoresViewModel`
- `GestionUsuarioViewModel`
- `FormularioInstaladorViewModel`
- `FormularioUsuarioViewModel`

### Capa de persistencia

- `Db/DatabaseInitializer.cs`
- `Db/UsuariosDbService.cs`
- `Db/AdministradoresDbService.cs`
- `Db/InstaladorDbService.cs`

### Servicios de soporte

- `Services/RolesSistema.cs`
- `Services/SesionUsuario.cs`
- `Services/WindowsProfileService.cs`
- `Services/ModalOverlayScope.cs`

## 6. Flujo de arranque

El arranque del sistema realiza estas acciones:

1. inicializa la base de datos si no existe
2. aplica migraciones de columnas faltantes
3. migra bases heredadas si encuentra nombres antiguos
4. abre el login principal

Responsable principal:

- `Db/DatabaseInitializer.cs`

## 7. Modelo de sesion

La sesion se centraliza en `SesionUsuario`.

El sistema mantiene:

- usuario autenticado actual
- indicador de sesion administrativa (`ModoAdministrador`)

Esto impacta:

- visibilidad de `Gestion de Usuarios`
- permisos de alta, edicion y eliminacion de instaladores
- filtros de aplicativos visibles para usuarios no administradores

## 8. Autenticacion

## Login normal

`LoginViewModel` hace lo siguiente:

- recibe usuario y contrasena
- resuelve el correo real desde el alias digitado
- permite login por usuario del dispositivo o por alias del correo
- valida hash SHA-256 contra `Usuarios.PasswordHash`
- abre `MainWindow`
- guarda credenciales solo si `Recordarme` esta activo

### Credenciales recordadas

Se almacenan localmente con `ProtectedData` del usuario de Windows. Actualmente las rutas usadas son:

- `AppData\Roaming\ConcesionaroCarros\login.remember`
- `AppData\Roaming\ConcesionaroCarros\login.admin.remember`

Observacion tecnica:

- aunque el proyecto ya fue renombrado, estas rutas aun conservan el nombre historico del directorio. Si se decide normalizarlo, debe hacerse con una migracion de archivos recordados para no perder la experiencia del usuario.

## Login administrativo

`AdminLoginViewModel`:

- valida la existencia del usuario en `Administrador`
- valida la contrasena administrativa
- busca el usuario base asociado en `Usuarios`
- fuerza `SesionUsuario.ModoAdministrador = true`
- abre `MainWindow`

## Registro normal

`RegisterViewModel`:

- exige correo `@weg.net`
- toma el usuario del dispositivo si el correo corresponde al equipo actual
- si el correo no es del equipo, construye el login desde la parte anterior a `@`
- genera nombres desde el perfil de Windows o desde el alias del correo
- registra en `Usuarios`
- devuelve al login con datos precargados

## Registro administrativo

`AdminRegisterViewModel`:

- exige correo `@weg.net`
- registra o actualiza el usuario base en `Usuarios`
- registra o actualiza el administrador en `Administrador`
- guarda dos credenciales distintas:
  - contrasena normal
  - contrasena administrativa
- devuelve al login administrativo con datos precargados

## Recuperacion de contrasena

`MicrosoftRecoveryViewModel`:

- valida formato del correo
- exige confirmacion `No soy un robot`
- verifica existencia del usuario en `Usuarios`
- genera codigo temporal local de 6 digitos
- habilita el paso de cambio de contrasena
- actualiza `Usuarios.PasswordHash`
- registra auditoria en `PasswordRecoveryLog`
- retorna al login con la nueva contrasena cargada

## 9. Navegacion principal

`MainViewModel` controla la vista cargada dentro de `MainWindow`.

Estados actuales:

- `Instaladores`
- `Usuarios`

Comandos principales:

- `ShowInstaladoresCommand`
- `ShowGestionUsuariosCommand`
- `CerrarSesionCommand`

### Nombre mostrado en cabecera

La cabecera lateral intenta mostrar el nombre mas natural posible segun la sesion:

- si el correo de sesion coincide con el correo principal del equipo, usa el nombre visible de Windows
- en caso contrario usa el alias del correo
- si no puede resolverlo, usa nombres y apellidos almacenados en base de datos

## 10. Modulo de instaladores

`InstaladoresViewModel` es el centro funcional del producto.

Responsabilidades:

- cargar instaladores por carpeta funcional
- filtrar instaladores segun aplicativos asignados al usuario
- abrir formularios de alta, edicion y solo lectura
- ejecutar instaladores con `Process.Start`
- eliminar registros de instaladores

### Carpetas funcionales

- `Punto local de desarrollo planta`
- `Desarrollo global`

### Regla de permisos

- usuario normal: solo ve rutas incluidas en `Usuarios.AplicativosJson`
- administrador: ve el catalogo completo y puede gestionarlo

## 11. Modulo de gestion de usuarios

`GestionUsuarioViewModel` concentra la administracion de usuarios del sistema.

Responsabilidades:

- cargar usuarios desde `Usuarios`
- abrir formulario de alta
- abrir formulario de edicion
- eliminar usuarios
- mantener sincronizacion con `Administrador` cuando el rol corresponde
- mostrar panel de asignacion inferior
- guardar `AplicativosJson`

### Panel de asignacion

Usa dos modelos auxiliares:

- `RolAsignacionItem`
- `AplicativoAsignacionItem`

El rol se muestra como referencia visual. La persistencia de aplicativos se hace en `Usuarios.AplicativosJson`.

## 12. Formularios modales y overlay

Los formularios:

- `FormularioUsuarioView`
- `FormularioInstaladorView`

se abren como ventanas modales reales. Mientras estan activos, `ModalOverlayScope` crea una capa translúcida sobre la ventana principal para:

- desactivar visualmente el fondo
- mantener foco en el formulario activo
- evitar interacciones accidentales con la vista principal

## 13. Roles del sistema

Los roles definidos en `RolesSistema` son:

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

`RolesSistema.AsignablesSinAdmin` se usa en el panel de asignacion de usuarios.

## 14. Dependencias y requisitos para seguir desarrollando

Para trabajar sobre el proyecto se debe contar con:

- Windows
- Visual Studio con soporte para `.NET Framework 4.8`
- restauracion correcta de paquetes NuGet
- acceso de escritura al directorio del proyecto

Paquetes usados por el proyecto:

- `Microsoft.Data.Sqlite`
- `PDFsharp`
- `QuestPDF`

## 15. Base de datos

La base se documenta con mas detalle en:

- `Docs/Developers/BaseDeDatos.md`

Resumen rapido:

- archivo actual: `WegInstaladores.db`
- tablas activas: `Usuarios`, `Instaladores`, `Administrador`, `PasswordRecoveryLog`
- migracion automatica desde nombres de base heredados
- eliminacion de tablas legacy despues de migrar

## 16. Convenciones y puntos de mantenimiento

### Recursos visuales

- usar rutas relativas de recurso del proyecto (`/Images/...`)
- evitar volver a introducir rutas absolutas de desarrollo

### Persistencia de usuarios

- cualquier cambio de rol debe revisar el impacto en `Administrador`
- cualquier cambio de login debe validar las reglas de resolucion por usuario de dispositivo y alias de correo

### Instaladores

- la ruta almacenada es la ruta real del ejecutable
- si la ruta deja de existir, el aplicativo seguira registrado pero no podra ejecutarse
- la carpeta funcional se normaliza automaticamente cuando llega vacia

### Recuperacion de contrasena

- el flujo es local y no depende de un backend externo
- el campo `ValidadoMicrosoft` en `PasswordRecoveryLog` permanece por compatibilidad historica, aunque el flujo actual no autentica contra Microsoft

## 17. Riesgos tecnicos actuales

- el namespace interno sigue usando `ConcesionaroCarros`
- las rutas de credenciales recordadas tambien conservan ese nombre historico
- la asignacion de aplicativos se guarda en JSON dentro de `Usuarios`, lo cual es practico pero menos normalizado que una tabla relacional

## 18. Recomendaciones para otro desarrollador que tome el proyecto

1. abrir primero `SistemaDeInstalacion.sln`
2. revisar `DatabaseInitializer` antes de tocar persistencia
3. revisar `LoginViewModel`, `AdminLoginViewModel` y `SesionUsuario` antes de tocar seguridad
4. revisar `InstaladoresViewModel` y `GestionUsuarioViewModel` antes de tocar permisos
5. validar cualquier cambio visual en:
   - login normal
   - login admin
   - registro normal
   - registro admin
   - formularios modales
6. no eliminar compatibilidad legacy de base o credenciales sin una migracion controlada

## 19. Seguimiento recomendado

Si el proyecto sigue creciendo, conviene planificar estas mejoras:

- renombrar namespaces internos al nombre real del producto
- mover la logica de autenticacion a servicios dedicados
- introducir logging estructurado
- reemplazar `AplicativosJson` por una tabla relacional de asignacion
- separar mas claramente UI, reglas de negocio y acceso a datos
