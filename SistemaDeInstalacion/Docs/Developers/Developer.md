# Developer

Esta guía técnica está pensada para cualquier desarrollador que tome el proyecto y necesite entender con rapidez cómo está construido, cómo se compila, cómo se prueba y qué puntos debe revisar antes de continuar el desarrollo.

## Explicación principal

`SistemaDeInstalacion` es una aplicación `WPF` sobre `.NET Framework 4.8` con persistencia local en `SQLite`. No existe un backend web separado: la lógica de negocio, la interfaz y el acceso a datos conviven dentro del mismo repositorio.

Esta guía no está orientada al uso funcional diario, sino a entender:

- cómo arranca la aplicación;
- cómo se resuelve la sesión;
- cómo se cargan los módulos;
- cómo se persiste la información;
- cómo se compila, prueba y entrega el sistema.

## Objetivo de esta guía

Con esta guía el desarrollador podrá:

- entender la arquitectura general del proyecto;
- ubicar las carpetas y archivos principales;
- seguir el flujo real de arranque;
- comprender los módulos funcionales;
- revisar la base de datos y sus dependencias;
- compilar y validar cambios sin romper el sistema;
- tener una ruta clara de onboarding técnico.

## Qué debe entender primero el desarrollador

Antes de hacer cambios en el código, es importante tener presentes estas reglas:

1. la aplicación arranca sobre una base `SQLite` local;
2. el modo administrador no depende solo del rol, sino también del flujo de autenticación;
3. buena parte de los permisos funcionales se resuelve desde `SesionUsuario`;
4. los instaladores visibles para usuarios normales dependen de asignaciones por ruta;
5. la ayuda, los logs y el empaquetado forman parte del producto y no deben tratarse como extras.

## Flujo técnico general

```text
Acceso directo
        |
        v
SistemaDeInstalacion.exe
        |
        v
App.xaml / App.xaml.cs
        |
        v
DatabaseInitializer.Initialize()
        |
        v
LoginView
        |
        +--> Login normal
        |        |
        |        v
        |   Sesión operativa
        |
        \--> Login administrativo
                 |
                 v
        Sesión administrativa
                 |
                 v
        MainWindow + módulos internos
```

## Inicio del recorrido técnico

Desde aquí empiezan los puntos donde puedes apoyar la guía con pantallazos técnicos. El orden recomendado es:

1. estructura del proyecto en Visual Studio;
2. punto de entrada `App.xaml.cs`;
3. inicialización de base de datos;
4. login normal;
5. login administrativo;
6. vista principal y navegación;
7. módulo de instaladores;
8. módulo de gestión de usuarios;
9. ayuda y documentación interna;
10. logs;
11. compilación y salida `Release`.

## Paso 1. Abrir el proyecto en Visual Studio

El primer paso para cualquier desarrollador es abrir el proyecto y ubicar su estructura general.

Archivos y carpetas principales:

- `Views/`
- `ViewModels/`
- `Services/`
- `Db/`
- `Models/`
- `Docs/`
- `SistemaDeInstalacion.csproj`

Historia de pantallazo: Estructura del proyecto abierta en Visual Studio.
<!-- Aquí pegas la imagen del Explorador de soluciones con las carpetas principales -->

## Paso 2. Entender la arquitectura general

La solución sigue una estructura WPF clásica apoyada en `ViewModels`, servicios de apoyo y acceso directo a `SQLite`.

```text
Views (XAML)
   |
   v
ViewModels
   |
   +--> Services
   |
   +--> Db
   |
   +--> Models
   |
   v
SQLite (WegInstaladores.db)
```

### Responsabilidad por capas

| Capa | Carpeta | Responsabilidad |
|---|---|---|
| Presentación | `Views/` | vistas, formularios y navegación visual |
| Lógica de presentación | `ViewModels/` | comandos, validaciones y flujo de la interfaz |
| Servicios | `Services/` | sesión, roles, utilidades, logs y ayuda documental |
| Persistencia | `Db/` | inicialización, consultas y operaciones SQLite |
| Dominio | `Models/` | entidades del sistema |
| Recursos | `Images/`, `Fonts/` | activos visuales y tipografías |

Historia de pantallazo: Carpeta del proyecto organizada por capas.
<!-- Aquí pegas la imagen de la estructura técnica agrupada por carpetas -->

## Paso 3. Revisar el punto de entrada

El arranque empieza en `App.xaml` y `App.xaml.cs`.

Secuencia:

1. se inicia la aplicación;
2. se ejecuta `DatabaseInitializer.Initialize()`;
3. se crean tablas faltantes o se aplican migraciones básicas;
4. se muestra la vista de login;
5. tras autenticación, se abre `MainWindow`;
6. `MainWindow` carga vistas internas según el rol de la sesión.

Fragmento representativo:

```csharp
protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);

    DatabaseInitializer.Initialize();

    var login = new LoginView();
    login.Show();
}
```

Qué significa:

- la inicialización de base ocurre antes de cualquier interacción;
- el sistema depende de SQLite desde el primer arranque;
- un cambio de esquema debe contemplarse siempre antes del login.

Historia de pantallazo: Código de `App.xaml.cs` con el flujo de arranque.
<!-- Aquí pegas la imagen del archivo App.xaml.cs mostrando OnStartup -->

## Paso 4. Entender la inicialización de base de datos

La base activa es `WegInstaladores.db` y su inicialización se gestiona desde `Db/DatabaseInitializer.cs`.

Puntos clave:

- asegura la existencia de tablas;
- agrega columnas faltantes;
- puede migrar bases legacy;
- deja la aplicación lista para el login.

La guía de detalle de persistencia está en:

- [BaseDeDatos](BaseDeDatos.md)

Historia de pantallazo: Clase `DatabaseInitializer` abierta en Visual Studio.
<!-- Aquí pegas la imagen del inicializador de base de datos -->

## Paso 5. Revisar la sesión y la navegación

La sesión vive en `Services/SesionUsuario.cs`. El modo administrador no depende solo del rol, sino también del flujo de autenticación administrativa.

Idea central:

```csharp
public static Usuario UsuarioActual { get; set; }
public static bool ModoAdministrador { get; set; }

public static bool EsAdmin =>
    ModoAdministrador && RolesSistema.EsAdministrador(UsuarioActual?.Rol);
```

Implicación:

- un usuario con rol `ADMINISTRADOR` no verá `Gestión de Usuarios` si no ingresa por el login administrativo.

Historia de pantallazo: Código de `SesionUsuario.cs` con la lógica de `EsAdmin`.
<!-- Aquí pegas la imagen del archivo SesionUsuario.cs -->

## Paso 6. Revisar autenticación de usuarios

### Login normal

Archivos clave:

- `Views/LoginView.xaml`
- `ViewModels/LoginViewModel.cs`
- `Db/UsuariosDbService.cs`

Responsabilidades:

- validar credenciales de usuario normal;
- resolver usuario por correo o alias;
- cargar sesión operativa;
- manejar la opción `Recuérdame`.

Historia de pantallazo: Vista y ViewModel del login normal.
<!-- Aquí pegas la imagen del LoginView y LoginViewModel -->

### Login administrativo

Archivos clave:

- `Views/AdminLoginView.xaml`
- `ViewModels/AdminLoginViewModel.cs`
- `Db/AdministradoresDbService.cs`
- `Services/SesionUsuario.cs`

Responsabilidades:

- validar la contraseña administrativa;
- localizar el usuario base relacionado;
- activar `ModoAdministrador`;
- habilitar la navegación de administración.

Historia de pantallazo: Vista y ViewModel del login administrativo.
<!-- Aquí pegas la imagen del AdminLoginView y AdminLoginViewModel -->

## Paso 7. Revisar el catálogo de instaladores

Archivos clave:

- `Views/InstaladoresView.xaml`
- `ViewModels/InstaladoresViewModel.cs`
- `ViewModels/FormularioInstaladorViewModel.cs`
- `Db/InstaladorDbService.cs`
- `Models/Instalador.cs`

Responsabilidades:

- listar ejecutables registrados;
- filtrar por carpeta funcional;
- abrir formularios de alta, edición y consulta;
- ejecutar el `.exe` seleccionado;
- restringir visibilidad según aplicativos asignados.

Fragmento representativo:

```csharp
if (!SesionUsuario.EsAdmin)
{
    var asignados = SesionUsuario.UsuarioActual?.ObtenerAplicativosAsignados()
                    ?? new List<string>();

    var rutasAsignadas = new HashSet<string>(
        asignados.Where(r => !string.IsNullOrWhiteSpace(r)),
        StringComparer.OrdinalIgnoreCase);

    todos = todos
        .Where(x => !string.IsNullOrWhiteSpace(x.Ruta) && rutasAsignadas.Contains(x.Ruta))
        .ToList();
}
```

Qué significa:

- el permiso del usuario se basa en la ruta del ejecutable;
- el catálogo de usuario no es libre, sino filtrado por asignación;
- una ruta mal cambiada puede dejar un aplicativo invisible.

Historia de pantallazo: Módulo de instaladores y lógica de filtrado por sesión.
<!-- Aquí pegas la imagen del módulo de instaladores y otra del código si lo consideras necesario -->

## Paso 8. Revisar la gestión de usuarios

Archivos clave:

- `Views/GestionUsuarioView.xaml`
- `Views/FormularioUsuarioView.xaml`
- `ViewModels/GestionUsuarioViewModel.cs`
- `ViewModels/FormularioUsuarioViewModel.cs`
- `Db/UsuariosDbService.cs`
- `Db/AdministradoresDbService.cs`

Responsabilidades:

- crear, editar y eliminar usuarios;
- sincronizar información administrativa cuando aplica;
- asignar aplicativos a través de `AplicativosJson`;
- mantener consistencia entre `Usuarios` y `Administrador`.

Historia de pantallazo: Módulo de gestión de usuarios en ejecución.
<!-- Aquí pegas la imagen de la vista de gestión de usuarios -->

## Paso 9. Revisar recuperación de contraseña

Archivos clave:

- `Views/MicrosoftRecoveryView.xaml`
- `ViewModels/MicrosoftRecoveryViewModel.cs`
- `Db/UsuariosDbService.cs`

Responsabilidades:

- validar la identidad mínima del usuario;
- generar y verificar un código temporal;
- actualizar la contraseña;
- registrar la operación en `PasswordRecoveryLog`.

Historia de pantallazo: Flujo técnico de recuperación de contraseña.
<!-- Aquí pegas la imagen de la vista de recuperación o del código relacionado -->

## Paso 10. Revisar el centro de ayuda interno

Archivos clave:

- `Views/HelpView.xaml`
- `ViewModels/HelpViewModel.cs`
- `Services/DocumentationService.cs`
- `Services/MarkdownDocumentRenderer.cs`

Responsabilidades:

- cargar documentación por perfil;
- navegar entre documentos internos;
- mostrar enlaces complementarios entre guías;
- restringir la visibilidad según rol.

Historia de pantallazo: Vista de ayuda abierta y documentos cargados.
<!-- Aquí pegas la imagen del centro de ayuda dentro de la aplicación -->

## Paso 11. Revisar logs y soporte

El módulo de logs forma parte de la solución funcional y técnica. Permite revisar eventos, errores, latencias y trazabilidad por equipo y fecha.

Archivos de apoyo más relevantes:

- `Services/LogService.cs`
- `Services/LogDashboardService.cs`
- `ViewModels/LogsViewModel.cs`
- `Views/LogsView.xaml`

Puntos clave:

- registra acciones funcionales importantes;
- diferencia niveles como `INFO`, `WARNING`, `ERROR` y `LATENCY`;
- organiza información por equipo y fecha;
- soporta consulta interna desde el sistema.

Historia de pantallazo: Vista de logs y archivos de soporte relacionados.
<!-- Aquí pegas la imagen de la vista de logs y, si quieres, del código del servicio -->

## Paso 12. Compilar y ejecutar el proyecto

Actualmente el repositorio versiona:

- `SistemaDeInstalacion.csproj`
- `SistemaDeInstalacion.Tests/SistemaDeInstalacion.Tests.csproj`

No existe una solución `.sln` versionada en el repositorio. El flujo recomendado es:

1. abrir `SistemaDeInstalacion.csproj` en Visual Studio;
2. crear una solución local;
3. agregar `SistemaDeInstalacion.Tests.csproj`;
4. restaurar paquetes `NuGet`;
5. compilar en `Debug` o `Release`.

Requisitos de desarrollo:

- `Windows 10` o superior;
- `Visual Studio 2022`;
- carga de trabajo `.NET desktop development`;
- `.NET Framework 4.8 Developer Pack`;
- restauración de paquetes `NuGet`;
- permisos de lectura y escritura sobre la carpeta del proyecto.

Salidas principales:

- `bin/Debug/SistemaDeInstalacion.exe`
- `bin/Release/SistemaDeInstalacion.exe`

Artefactos esperados junto al ejecutable:

- `SistemaDeInstalacion.exe.config`
- `Microsoft.Data.Sqlite.dll`
- `QuestPDF.dll`
- dependencias `PDFsharp`
- carpeta `runtimes/`
- carpeta `Fonts/`
- carpeta `Docs/` para el centro de ayuda

Historia de pantallazo: Compilación en Visual Studio y salida en `bin/Release`.
<!-- Aquí pegas la imagen del build exitoso y la carpeta de salida -->

## Paso 13. Entender el empaquetado actual

La entrega operativa actual se basa en una carpeta de `Release` completa y en el proceso de empaquetado con Inno Setup que ya hace parte del flujo funcional del proyecto.

Bosquejo de entrega:

```text
Entrega/
|
+- SistemaDeInstalacion.exe
+- SistemaDeInstalacion.exe.config
+- Microsoft.Data.Sqlite.dll
+- QuestPDF.dll
+- PDFsharp*.dll
+- runtimes/
+- Fonts/
+- Docs/
\- WegInstaladores.db   (si se distribuye una base inicial)
```

Historia de pantallazo: Estructura final de entrega o publicación.
<!-- Aquí pegas la imagen de la carpeta de entrega o del instalador -->

## Configuración y parámetros relevantes

`App.config` contiene claves como:

- `CC_CORPORATE_EMAIL_DOMAIN`
- `CC_REQUIRE_MICROSOFT_EMAIL_VALIDATION`
- `CC_AZURE_TENANT_ID`
- `CC_AZURE_CLIENT_ID`
- `CC_AZURE_CLIENT_SECRET`

Estado funcional actual:

- el dominio corporativo activo es `weg.net`;
- la validación Microsoft está desactivada;
- la recuperación de contraseña funciona de manera local;
- los parámetros Azure están preparados para una evolución futura, no para el flujo actual.

## Asignación de aplicativos

Hoy la relación usuario-aplicativo no está normalizada en una tabla intermedia. Se guarda en `Usuarios.AplicativosJson`.

Fragmento representativo:

```csharp
var serializer = new JavaScriptSerializer();
AplicativosJson = serializer.Serialize(lista);
```

Implicaciones:

- la asignación depende de rutas físicas;
- si una ruta cambia, el permiso puede quedar obsoleto;
- un refactor futuro debería considerar una tabla relacional.

## Seguridad actual

Las contraseñas no se almacenan en texto plano. El sistema usa `SHA-256` y guarda el resultado en base64.

Ejemplo:

```csharp
using (var sha = SHA256.Create())
{
    var bytes = Encoding.UTF8.GetBytes(password ?? string.Empty);
    var hash = sha.ComputeHash(bytes);
    return Convert.ToBase64String(hash);
}
```

Lectura técnica:

- mejora el almacenamiento frente a texto plano;
- sigue siendo una estrategia limitada para estándares modernos;
- una evolución futura debería migrar a `PBKDF2`, `bcrypt` o `Argon2`.

## Pruebas unitarias

Existe un proyecto real de pruebas:

- `SistemaDeInstalacion.Tests`

Cobertura funcional actual:

- inicialización de la base;
- migración de bases heredadas;
- reglas de usuarios;
- login y hashing;
- servicios de administradores;
- servicios de instaladores;
- validación de roles.

Ejecución recomendada:

1. compila la solución local;
2. abre `Prueba > Explorador de pruebas` en Visual Studio;
3. ejecuta la suite completa;
4. revisa especialmente pruebas ligadas a SQLite y migraciones.

Historia de pantallazo: Explorador de pruebas y ejecución de tests.
<!-- Aquí pegas la imagen del Explorador de pruebas -->

## Historias técnicas de continuidad

### Historia 1

Como desarrollador, quiero entender el arranque y la sesión para poder cambiar la navegación sin romper el acceso de usuarios y administradores.

Archivos a revisar:

- `App.xaml.cs`
- `MainViewModel.cs`
- `SesionUsuario.cs`

### Historia 2

Como desarrollador, quiero modificar el catálogo de instaladores para agregar nuevas capacidades sin romper la asignación por usuario.

Archivos a revisar:

- `InstaladorDbService.cs`
- `InstaladoresViewModel.cs`
- `FormularioInstaladorViewModel.cs`

### Historia 3

Como desarrollador, quiero cambiar la persistencia de permisos para migrar de JSON a una relación normalizada.

Archivos a revisar:

- `UsuariosDbService.cs`
- `DatabaseInitializer.cs`
- `Docs/Developers/BaseDeDatos.md`
- pruebas de `SistemaDeInstalacion.Tests`

## Riesgos técnicos actuales

- el namespace histórico aún se llama `ConcesionaroCarros`;
- la asignación de aplicativos depende de rutas físicas;
- la base se crea en el directorio de ejecución;
- no hay solución `.sln` versionada;
- el empaquetado no está formalizado con instalador corporativo;
- parte de la navegación mezcla MVVM con apertura directa de ventanas.

## Orden recomendado para onboarding técnico

1. `App.xaml.cs`
2. `Db/DatabaseInitializer.cs`
3. `Services/SesionUsuario.cs`
4. `ViewModels/MainViewModel.cs`
5. `ViewModels/LoginViewModel.cs`
6. `ViewModels/AdminLoginViewModel.cs`
7. `ViewModels/InstaladoresViewModel.cs`
8. `ViewModels/GestionUsuarioViewModel.cs`
9. `Docs/Developers/BaseDeDatos.md`
10. `SistemaDeInstalacion.Tests/`

## Enlaces complementarios

- [Visión general del sistema](../Sistema.md)
- [Guía de usuarios](../users/User.md)
- [Guía de administradores](../Administradores/Administradores.md)
- [Guía de base de datos](BaseDeDatos.md)

## Enlaces de apoyo externo

Para ampliar el trabajo técnico sobre esta solución:

- [Documentación oficial de WPF en Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/)
- [Documentación de data binding en WPF](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/data/)
- [Documentación oficial de .NET Framework](https://learn.microsoft.com/en-us/dotnet/framework/)
- [Documentación oficial de MSTest](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-mstest-intro)
- [Documentación oficial de Microsoft.Data.Sqlite](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/)
- [Documentación oficial de QuestPDF](https://www.questpdf.com/)
- [Documentación oficial de PDFsharp](https://docs.pdfsharp.net/)

## Checklist antes de entregar cambios

- compilar en `Debug` o `Release`;
- validar login normal;
- validar login administrativo;
- validar centro de ayuda por perfil;
- validar alta, edición y baja de instaladores;
- validar alta, edición y baja de usuarios;
- validar asignación de aplicativos;
- revisar impacto de base y migraciones;
- ejecutar pruebas si el entorno lo permite.
