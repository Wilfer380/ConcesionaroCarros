# Documentación Técnica para Desarrolladores

## 1. Introducción del proyecto

### Nombre del proyecto

`SistemaDeInstalacion`

### Tipo de aplicación

Aplicación de escritorio desarrollada en `C#` con `WPF` sobre `.NET Framework 4.8`, usando una organización de capas inspirada en el patron `MVVM`.

### Proposito del sistema

El sistema fue construido para administrar un catálogo de instaladores corporativos y controlar, por usuario y por rol, que aplicativos puede ver y ejecutar cada persona dentro de la organización.

### Problema que resuelve

Antes de unificar este flujo, la distribucion de instaladores podia depender de rutas manuales, accesos dispersos o permisos no controlados. El sistema centraliza ese proceso y permite:

- registrar instaladores en un catálogo único
- separar aplicativos por carpeta funcional
- asignar aplicativos a usuarios especificos
- ofrecer acceso administrativo separado del acceso operativo normal
- recuperar contraseñas de forma local sin depender de soporte manual inmediato

### Alcance actual del software

El alcance vigente cubre:

- login normal
- registro normal
- login administrativo
- registro administrativo
- recuperación local de contraseña
- administración del catálogo de instaladores
- gestión de usuarios
- asignación de aplicativos por usuario

No es un sistema comercial ni transaccional. El dominio actual es exclusivamente la gestión de instaladores empresariales y accesos de usuarios.

## 2. Tecnologías utilizadas

| Tecnología | Uso |
|---|---|
| C# | Lenguaje principal |
| .NET Framework 4.8 | Runtime de la aplicacion |
| WPF | Interfaz grafica de escritorio |
| MVVM | Organizacion de la logica de presentacion |
| SQLite | Persistencia local |
| Microsoft.Data.Sqlite | Acceso a datos SQLite |
| NuGet | Gestion de paquetes |
| Visual Studio 2022 | IDE recomendado |
| PDFsharp | Dependencia incluida en el proyecto |
| QuestPDF | Dependencia incluida en el proyecto |

## 3. Arquitectura del sistema

La aplicación sigue una estructura de capas simple y clara.

```text
SistemaDeInstalacion/
|
|-- Commands/
|   |-- RelayCommand.cs
|
|-- Converters/
|   |-- BooleanNegationConverter.cs
|   |-- InverseBooleanConverter.cs
|   |-- InverseBooleanToVisibilityConverter.cs
|   |-- ColumnsByWidthConverter .cs
|
|-- Db/
|   |-- DatabaseInitializer.cs
|   |-- UsuariosDbService.cs
|   |-- AdministradoresDbService.cs
|   |-- InstaladorDbService.cs
|
|-- Docs/
|   |-- README.md
|   |-- users/
|   |   |-- README.md
|   |-- Developers/
|       |-- README.md
|       |-- BaseDeDatos.md
|
|-- Images/
|-- Fonts/
|
|-- Models/
|   |-- Usuario.cs
|   |-- Administrador.cs
|   |-- Instalador.cs
|
|-- Services/
|   |-- RolesSistema.cs
|   |-- SesionUsuario.cs
|   |-- WindowsProfileService.cs
|   |-- ModalOverlayScope.cs
|
|-- ViewModels/
|   |-- MainViewModel.cs
|   |-- LoginViewModel.cs
|   |-- RegisterViewModel.cs
|   |-- AdminLoginViewModel.cs
|   |-- AdminRegisterViewModel.cs
|   |-- MicrosoftRecoveryViewModel.cs
|   |-- InstaladoresViewModel.cs
|   |-- GestionUsuarioViewModel.cs
|   |-- FormularioInstaladorViewModel.cs
|   |-- FormularioUsuarioViewModel.cs
|
|-- Views/
|   |-- MainWindow.xaml
|   |-- LoginView.xaml
|   |-- RegisterView.xaml
|   |-- AdminLoginView.xaml
|   |-- AdminRegisterView.xaml
|   |-- MicrosoftRecoveryView.xaml
|   |-- InstaladoresView.xaml
|   |-- GestionUsuarioView.xaml
|   |-- FormularioInstaladorView.xaml
|   |-- FormularioUsuarioView.xaml
|   |-- RecoveryCodePopupView.xaml
|
|-- Properties/
|-- App.xaml
|-- App.config
|-- MainWindow.xaml
|-- SistemaDeInstalacion.csproj
```

### Responsabilidad por carpeta

- `Models`: estructuras de datos que representan entidades persistidas o usadas por la UI.
- `Views`: definición visual en XAML y eventos de UI puntuales.
- `ViewModels`: logica de presentación y coordinación de flujos.
- `Db`: acceso a datos SQLite y migraciones.
- `Services`: reglas compartidas del dominio, sesión, roles y utilidades de entorno.
- `Commands`: implementación de comandos WPF (`RelayCommand`).
- `Converters`: adaptadores de datos para XAML.
- `Images` y `Fonts`: recursos visuales del producto.
- `Docs`: documentación funcional y técnica.

## 4. Patron MVVM aplicado al proyecto

El sistema usa MVVM de forma práctica:

- la `View` define la interfaz visual
- el `ViewModel` expone propiedades, comandos y flujo de trabajo
- el `Model` representa los datos del dominio
- los `DbService` y `Services` encapsulan persistencia y reglas auxiliares

Ejemplo de flujo:

```text
Usuario
   ↓
View (WPF/XAML)
   ↓ DataBinding / Commands
ViewModel
   ↓
DbService / Services
   ↓
SQLite
```

### Ejemplo concreto

En el login normal:

- `LoginView.xaml` define los campos y botones
- `LoginViewModel` resuelve usuario, valida contraseña y abre la sesión
- `UsuariosDbService` consulta la tabla `Usuarios`

## 5. Flujo general de funcionamiento del sistema

### Arranque

Punto de entrada:

- `App.xaml`
- `App.xaml.cs`

Secuencia de inicio:

1. `App.OnStartup(...)`
2. `DatabaseInitializer.Initialize()`
3. apertura de `LoginView`

### Flujo funcional resumido

```text
Inicio de la aplicación
   ↓
Inicialización de base de datos
   ↓
Login principal
   ↓
Validación de credenciales
   ↓
SesionUsuario
   ↓
MainWindow
   ↓
Instaladores / Gestión de Usuarios
```

## 6. Autenticación y sesión

## Login normal

`LoginViewModel` realiza estas tareas:

- recibe `Usuario` y `Password`
- resuelve el correo real desde el alias digitado
- soporta login por usuario del equipo o por alias del correo
- valida contraseña en `Usuarios`
- abre `MainWindow`
- guarda credenciales recordadas si el usuario activa `Recordarme`

## Registro normal

`RegisterViewModel`:

- exige correo corporativo `@weg.net`
- identifica si el correo pertenece al equipo actual
- genera el nombre visible desde Windows o desde el alias del correo
- registra el usuario en `Usuarios`
- vuelve al login con datos precargados

## Login administrativo

`AdminLoginViewModel`:

- valida `UsuarioSistema` en `Administrador`
- valida la contraseña administrativa
- resuelve el usuario base en `Usuarios`
- marca `SesionUsuario.ModoAdministrador = true`
- abre `MainWindow`

## Registro administrativo

`AdminRegisterViewModel`:

- exige correo `@weg.net`
- registra o actualiza la cuenta base en `Usuarios`
- registra o actualiza la cuenta de `Administrador`
- maneja dos credenciales:
  - contraseña normal
  - contraseña administrativa

## Recuperacion de contrasena

`MicrosoftRecoveryViewModel`:

- valida el correo
- exige confirmación `No soy un robot`
- genera un código temporal local de 6 digitos
- habilita el paso de cambio de contraseña
- actualiza `Usuarios.PasswordHash`
- inserta auditoria en `PasswordRecoveryLog`

## Modelo de sesión

`SesionUsuario` centraliza:

- usuario autenticado actual
- modo de sesión administrativa

Esto condiciona:

- visibilidad de `GestionUsuarioView`
- permisos de alta, edición y eliminación de instaladores
- filtros de aplicativos visibles para usuarios normales

## 7. Vistas y módulos principales

### MainWindow

Contiene el shell principal de la aplicación:

- panel lateral
- nombre del usuario autenticado
- navegación a `Instalador`
- navegación a `Gestion de Usuarios` cuando aplica

### Módulo Instaladores

`InstaladoresViewModel`:

- carga instaladores por carpeta
- filtra instaladores para usuarios no administradores
- abre formularios modales de alta y edición
- ejecuta instaladores via `Process.Start`
- elimina registros del catálogo

Carpetas funcionales actuales:

- `Punto local de desarrollo planta`
- `Desarrollo global`

### Módulo Gestión de Usuarios

`GestionUsuarioViewModel`:

- carga usuarios desde `Usuarios`
- abre formulario de alta de usuario
- abre formulario de edición
- elimina usuarios
- sincroniza `Administrador` cuando el rol aplica
- muestra y guarda el panel inferior de asignación de aplicativos

## 8. Base de datos

Motor actual:

- `SQLite`

Archivo actual:

- `WegInstaladores.db`

### Tablas activas

- `Usuarios`
- `Instaladores`
- `Administrador`
- `PasswordRecoveryLog`

### Claves primarias

- todas las tablas usan `Id INTEGER PRIMARY KEY AUTOINCREMENT`

### Claves foraneas

Actualmente no hay claves foraneas físicas declaradas en el esquema. Las relaciones son lógicas y se resuelven por código.

### Relaciones lógicas

- `Administrador.Correo` se relaciona con `Usuarios.Correo`
- `PasswordRecoveryLog.UsuarioId` se relaciona logicamente con `Usuarios.Id`
- `Usuarios.AplicativosJson` contiene rutas de `Instaladores.Ruta`

### Tablas detalladas

Ver:

- `Docs/Developers/BaseDeDatos.md`

## 9. Configuración del sistema

Archivo principal de configuración:

- `App.config`

Claves activas relevantes:

- `CC_CORPORATE_EMAIL_DOMAIN`
- `CC_REQUIRE_MICROSOFT_EMAIL_VALIDATION`
- `CC_AZURE_TENANT_ID`
- `CC_AZURE_CLIENT_ID`
- `CC_AZURE_CLIENT_SECRET`

Uso actual:

- el dominio corporativo activo es `weg.net`
- la validación Microsoft esta desactivada en pruebas (`false`)

Observación:

- actualmente el flujo de recuperación funciona de manera local
- las claves Azure permanecen para futuras integraciones empresariales si se desea reactivar esa validación

## 10. Instalacion del proyecto en entorno de desarrollo

## Requisitos

- Windows
- Visual Studio 2022 o superior con soporte para `.NET Framework 4.8`
- restauración de paquetes NuGet habilitada
- acceso de escritura al directorio del proyecto

## Apertura recomendada

Abrir:

- `SistemaDeInstalacion.sln`

## Compilación recomendada

En este proyecto la forma mas estable de trabajo es Visual Studio o MSBuild para .NET Framework.

### Opción Visual Studio

- `Restore NuGet Packages`
- `Build Solution`
- `Start`

### Opción consola

```powershell
nuget restore SistemaDeInstalacion.sln
msbuild SistemaDeInstalacion.sln /t:Build /p:Configuration=Debug
```

## Ejecución en desarrollo

La aplicación arranca desde Visual Studio o ejecutando el binario generado en:

```text
SistemaDeInstalacion\bin\Debug\SistemaDeInstalacion.exe
```

## Proyecto de pruebas

Existe un proyecto adicional:

- `SistemaDeInstalacion.Tests`

Ese proyecto ejecuta pruebas manuales/automatizadas simples sobre base de datos, roles y modelos.

## 11. Dependencias y paquetes NuGet

Paquetes definidos actualmente en el proyecto principal:

- `Microsoft.Data.Sqlite`
- `PDFsharp`
- `QuestPDF`

Adicionalmente, el proyecto de pruebas referencia DLLs copiadas desde `bin\Debug` para validar servicios del sistema.

## 12. Empaquetado y distribución

### Estado actual del repositorio

Actualmente el repositorio no incluye un proyecto formal de instalación tipo:

- MSI
- Setup Project de Visual Studio
- ClickOnce

### Forma de salida actual

La salida principal es el ejecutable compilado desde el proyecto WPF.

Ejemplo de ubicación:

```text
bin\Release\SistemaDeInstalacion.exe
```

### Publicación

Como es un proyecto `.NET Framework` clasico, la publicación se realiza normalmente por:

- compilación `Release`
- empaquetado externo
- copia controlada de binarios y dependencias

Si en el futuro se necesita un instalador formal, conviene agregar un proyecto de instalación dedicado y documentarlo aparte.

## 13. Puntos de entrada del sistema

Los puntos de entrada principales son:

- `App.xaml`
- `App.xaml.cs`
- `MainWindow.xaml`
- `MainViewModel.cs`

Puntos de entrada funcionales secundarios:

- `LoginView.xaml`
- `AdminLoginView.xaml`
- `RegisterView.xaml`
- `AdminRegisterView.xaml`

## 14. Manejo de errores y logs

### Manejo actual de errores

El sistema maneja errores principalmente por:

- `try/catch` en ViewModels y DbServices
- mensajes al usuario por `MessageBox`
- reintentos simples en operaciones SQLite cuando la base esta bloqueada

### Logs actuales

No existe aun un subsistema formal de archivos log generales.

La única trazabilidad persistente implementada de forma estructurada es:

- `PasswordRecoveryLog`

### Implicacion

Para soporte y evolución futura, seria recomendable incorporar un logger centralizado para:

- errores de acceso a base
- fallos al ejecutar instaladores
- errores de autenticación
- errores de formularios modales

## 15. Seguridad

Medidas actuales implementadas:

- hashing SHA-256 para contraseñas de usuario y administrador
- almacenamiento protegido local con `ProtectedData` para credenciales recordadas
- validación de dominio corporativo en registro (`@weg.net`)
- separación entre contrasena normal y contrasena administrativa
- validación explicita del flujo de recuperación de acceso

Observaciones:

- SHA-256 funciona, pero en una evolución futura seria mas robusto usar un esquema de hashing con sal y algoritmo dedicado como PBKDF2, bcrypt o Argon2
- las credenciales recordadas dependen del usuario actual de Windows y no deben copiarse entre perfiles

## 16. Buenas prácticas para extender el proyecto

### Agregar una nueva vista

1. crear la vista en `Views/`
2. crear su `ViewModel` en `ViewModels/`
3. exponer propiedades y comandos necesarios
4. enlazarla desde `MainViewModel` o desde el modulo que corresponda
5. agregarla al `.csproj` si no queda incluida automáticamente

### Agregar un nuevo módulo de negocio

1. definir el modelo en `Models/`
2. crear su servicio o acceso a datos en `Db/` o `Services/`
3. crear ViewModel y vista correspondiente
4. integrar navegación y permisos
5. documentar cambios en `Docs/Developers`

### Agregar un nuevo rol

1. incluirlo en `Services/RolesSistema.cs`
2. revisar formularios de usuario y panel de asignación
3. revisar cualquier regla que dependa de `EsAdministrador(...)`
4. validar impacto sobre login, gestión de usuarios y filtros de instaladores

### Agregar nuevos instaladores o nuevas carpetas

1. revisar constantes de carpeta en `FormularioInstaladorViewModel` e `InstaladoresViewModel`
2. validar el impacto visual en `InstaladoresView`
3. revisar cualquier filtro por carpeta

## 17. Ejemplos de flujo de datos

### Ejemplo 1. Login normal

```text
LoginView
   ↓ binding
LoginViewModel
   ↓
UsuariosDbService
   ↓
Tabla Usuarios
   ↓
SesionUsuario
   ↓
MainWindow
```

### Ejemplo 2. Asignación de aplicativos

```text
GestionUsuarioView
   ↓ seleccion de usuario
GestionUsuarioViewModel
   ↓
InstaladorDbService + UsuariosDbService
   ↓
Tabla Instaladores + Tabla Usuarios
   ↓
AplicativosJson actualizado
```

### Ejemplo 3. Registro administrativo

```text
AdminRegisterView
   ↓
AdminRegisterViewModel
   ↓
UsuariosDbService
   ↓
AdministradoresDbService
   ↓
Tablas Usuarios + Administrador
```

## 18. Diagrama conceptual de arquitectura

```text
+-----------------------+
|      Views (WPF)      |
+-----------------------+
            |
            v
+-----------------------+
|   ViewModels (MVVM)   |
+-----------------------+
      |            |
      v            v
+-------------+  +------------------+
|   Services   |  |    Db Services   |
+-------------+  +------------------+
            \      /
             \    /
              v  v
         +-------------+
         |   SQLite    |
         +-------------+
```

## 19. Consideraciones para continuidad del proyecto

Antes de hacer cambios grandes, otro desarrollador deberia revisar en este orden:

1. `App.xaml.cs` y `DatabaseInitializer.cs`
2. `LoginViewModel.cs` y `AdminLoginViewModel.cs`
3. `MainViewModel.cs` y `SesionUsuario.cs`
4. `InstaladoresViewModel.cs`
5. `GestionUsuarioViewModel.cs`
6. `Docs/Developers/BaseDeDatos.md`

## 20. Deuda técnica identificada

- namespaces internos aun con `ConcesionaroCarros`
- rutas de credenciales recordadas aun con ese nombre historico
- `AplicativosJson` como almacenamiento JSON en vez de tabla relacional
- ausencia de logging estructurado general
- compatibilidad legacy en la migracion de base de datos que convendra retirar cuando ya no existan instalaciones antiguas
