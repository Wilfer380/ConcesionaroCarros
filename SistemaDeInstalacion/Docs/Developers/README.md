# Guia Técnica para Desarrolladores

## 1. Que es este proyecto

`SistemaDeInstalacion` es una aplicacion de escritorio `WPF` sobre `.NET Framework 4.8` orientada a un problema muy concreto:

- autenticar usuarios normales y administradores
- administrar un catálogo de instaladores corporativos
- permitir que cada usuario vea y ejecute solo los aplicativos que tiene asignados

No es un sistema web, no usa servicios remotos como backend principal y no depende de un servidor de aplicaciones para operar su flujo base. El centro del sistema es una base `SQLite` local llamada `WegInstaladores.db`.

La app trabaja con tres conceptos de negocio muy importantes:

1. `Usuarios`
2. `Instaladores`
3. `Administrador`

Si otro desarrollador toma este proyecto, casi cualquier cambio importante termina pasando por esos tres ejes.

## 2. Objetivo de esta documentación

Esta guía esta escrita para que un desarrollador nuevo pueda:

- abrir el proyecto correctamente
- compilarlo y ejecutarlo
- entender como se inicializa la app
- entender como se guardan usuarios, administradores e instaladores
- saber como se registra un nuevo `.exe` dentro del sistema
- saber que archivos hay que empaquetar al distribuir la aplicacion
- continuar el proyecto sin romper permisos, sesiones o persistencia

La idea no es documentar una arquitectura ideal, sino explicar la arquitectura real que hoy existe en el repositorio.

## 3. Stack técnico real

- lenguaje: `C#`
- UI: `WPF` + `XAML`
- framework: `.NET Framework 4.8`
- patron general: `MVVM` liviano
- base de datos: `SQLite`
- acceso a datos: `Microsoft.Data.Sqlite`
- serialización de aplicativos asignados: `JavaScriptSerializer`
- persistencia local de credenciales recordadas: `ProtectedData`
- recursos visuales: `Images/`, `Fonts/`, `ResourceDictionary`

Paquetes declarados en [SistemaDeInstalacion.csproj](c:/Users/wandica/source/repos/ConcesionaroCarros/SistemaDeInstalacion/SistemaDeInstalacion.csproj):

```xml
<PackageReference Include="Microsoft.Data.Sqlite">
  <Version>10.0.2</Version>
</PackageReference>
<PackageReference Include="PDFsharp">
  <Version>6.2.4</Version>
</PackageReference>
<PackageReference Include="QuestPDF">
  <Version>2025.12.4</Version>
</PackageReference>
```

Observación importante:

- `PDFsharp` y `QuestPDF` estan referenciados
- hoy no forman el flujo principal visible de autenticación, usuarios o instaladores
- antes de quitarlos hay que validar si existen planes de uso, ramas alternas o funcionalidades futuras

## 4. Como abrir el proyecto

En esta carpeta no existe solución `.sln`. El archivo real de entrada es:

- [SistemaDeInstalacion.csproj](c:/Users/wandica/source/repos/ConcesionaroCarros/SistemaDeInstalacion/SistemaDeInstalacion.csproj)

Proceso recomendado:

1. abrir `Visual Studio 2022`
2. elegir `Open a project or solution`
3. abrir `SistemaDeInstalacion.csproj`
4. esperar restauración de `NuGet`

Requisitos recomendados del equipo:

- Windows 10 o superior
- Visual Studio 2022
- workload `.NET desktop development`
- soporte para `.NET Framework 4.8`

## 5. Como compilar y ejecutar

### 5.1 Configuraciones declaradas

En el proyecto existen dos configuraciones:

```xml
<PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' ">
  <OutputPath>bin\Debug\</OutputPath>
</PropertyGroup>

<PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' ">
  <OutputPath>bin\Release\</OutputPath>
</PropertyGroup>
```

### 5.2 Compilación recomendada

La forma mas confiable de compilar este proyecto es desde Visual Studio:

1. seleccionar `Debug` o `Release`
2. usar `Build > Build Solution`
3. ejecutar con `F5` o `Ctrl+F5`

La salida queda en:

- `bin/Debug/`
- `bin/Release/`

### 5.3 Ejecución fuera de Visual Studio

Para ejecutar la app ya compilada:

1. entrar a `bin/Release/` o `bin/Debug/`
2. abrir `SistemaDeInstalacion.exe`
3. confirmar que junto al `.exe` existan sus dependencias y carpetas necesarias

## 6. Que archivos son importantes para el ejecutable final

Esta app no se distribuye como un solo archivo autocontenido. Para empaquetarla correctamente hay que conservar el contenido funcional del directorio de salida.

Minimo esperado junto al ejecutable:

- `SistemaDeInstalacion.exe`
- `SistemaDeInstalacion.exe.config`
- `Microsoft.Data.Sqlite.dll`
- `SQLitePCLRaw.*`
- `PdfSharp.*`
- `QuestPDF.dll`
- carpeta `runtimes/`
- carpeta `Fonts/`

La carpeta `Fonts/` no es opcional. `FontResolver.cs` la usa asi:

```csharp
string ruta = Path.Combine(
    AppDomain.CurrentDomain.BaseDirectory,
    "Fonts",
    "Roboto-Regular.ttf");

_fontData = File.ReadAllBytes(ruta);
```

Si `Fonts/Roboto-Regular.ttf` no viaja con el ejecutable, cualquier flujo que use ese resolvedor puede fallar.

## 7. Estructura del proyecto

Mapa rapido:

- `App.xaml`, `App.xaml.cs`
  - arranque de la aplicación
- `MainWindow.xaml`, `MainWindow.xaml.cs`
  - shell principal luego del login
- `Views/`
  - pantallas `XAML`
- `ViewModels/`
  - lógica de presentación y comandos
- `Db/`
  - acceso a `SQLite` e inicialización de esquema
- `Models/`
  - entidades del dominio en memoria
- `Services/`
  - sesión, roles, overlay modal, perfil de Windows
- `Commands/`
  - `RelayCommand`
- `Docs/`
  - documentación funcional y técnica

Archivos mas importantes para entender el sistema:

- [App.xaml.cs](c:/Users/wandica/source/repos/ConcesionaroCarros/SistemaDeInstalacion/App.xaml.cs)
- [MainWindow.xaml.cs](c:/Users/wandica/source/repos/ConcesionaroCarros/SistemaDeInstalacion/MainWindow.xaml.cs)
- [Db/DatabaseInitializer.cs](c:/Users/wandica/source/repos/ConcesionaroCarros/SistemaDeInstalacion/Db/DatabaseInitializer.cs)
- [ViewModels/LoginViewModel.cs](c:/Users/wandica/source/repos/ConcesionaroCarros/SistemaDeInstalacion/ViewModels/LoginViewModel.cs)
- [ViewModels/AdminLoginViewModel.cs](c:/Users/wandica/source/repos/ConcesionaroCarros/SistemaDeInstalacion/ViewModels/AdminLoginViewModel.cs)
- [ViewModels/InstaladoresViewModel.cs](c:/Users/wandica/source/repos/ConcesionaroCarros/SistemaDeInstalacion/ViewModels/InstaladoresViewModel.cs)
- [ViewModels/GestionUsuarioViewModel.cs](c:/Users/wandica/source/repos/ConcesionaroCarros/SistemaDeInstalacion/ViewModels/GestionUsuarioViewModel.cs)
- [Db/UsuariosDbService.cs](c:/Users/wandica/source/repos/ConcesionaroCarros/SistemaDeInstalacion/Db/UsuariosDbService.cs)
- [Db/InstaladorDbService.cs](c:/Users/wandica/source/repos/ConcesionaroCarros/SistemaDeInstalacion/Db/InstaladorDbService.cs)
- [Db/AdministradoresDbService.cs](c:/Users/wandica/source/repos/ConcesionaroCarros/SistemaDeInstalacion/Db/AdministradoresDbService.cs)

## 8. Flujo de arranque real

La aplicación arranca en [App.xaml.cs](c:/Users/wandica/source/repos/ConcesionaroCarros/SistemaDeInstalacion/App.xaml.cs):

```csharp
protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);

    DatabaseInitializer.Initialize();

    var login = new LoginView();
    login.Show();
}
```

Eso quiere decir:

1. primero se asegura la base de datos
2. luego se abre el login normal

En [MainWindow.xaml.cs](c:/Users/wandica/source/repos/ConcesionaroCarros/SistemaDeInstalacion/MainWindow.xaml.cs) se vuelve a llamar al inicializador:

```csharp
public MainWindow()
{
    InitializeComponent();
    DatabaseInitializer.Initialize();
    DataContext = new MainViewModel();
}
```

Esto no rompe porque `DatabaseInitializer.Initialize()` es idempotente, pero si es una deuda técnica:

- la base se inicializa dos veces
- cualquier cambio futuro en inicialización debe considerar ambas entradas

## 9. Base de datos y persistencia

La base actual es:

- `WegInstaladores.db`

La cadena de conexión se construye por código:

```csharp
private const string CurrentDbPath = "WegInstaladores.db";
public static string ConnectionString => $"Data Source={CurrentDbPath}";
```

Implicación muy importante:

- la base se crea en el directorio desde el que se ejecuta la app
- si ejecutas desde `bin/Debug`, la base vive ahi
- si copias el ejecutable a otra carpeta, la base se crea en esa otra carpeta

### 9.1 Que hace `DatabaseInitializer`

`DatabaseInitializer` hace cuatro tareas principales:

1. migrar archivos de base legacy si existen
2. crear tablas faltantes
3. agregar columnas faltantes
4. normalizar datos historicos

Fragmento real:

```csharp
CREATE TABLE IF NOT EXISTS Usuarios (...);
CREATE TABLE IF NOT EXISTS Instaladores (...);
CREATE TABLE IF NOT EXISTS Administrador (...);
CREATE TABLE IF NOT EXISTS PasswordRecoveryLog (...);
```

También asegura columnas por codigo:

```csharp
EnsureColumnExists(connection, "Usuarios", "FotoPerfil", "TEXT");
EnsureColumnExists(connection, "Usuarios", "AplicativosJson", "TEXT DEFAULT '[]'");

EnsureColumnExists(connection, "Instaladores", "Nombre", "TEXT");
EnsureColumnExists(connection, "Instaladores", "Descripcion", "TEXT");
EnsureColumnExists(connection, "Instaladores", "Carpeta", "TEXT");
```

### 9.2 Bases legacy que todavia migra

Si no existe `WegInstaladores.db`, intenta copiar una de estas:

- `WegInstallerSystems.db`
- `installer_systems.db`
- `carros.db`

Eso esta implementado asi:

```csharp
private static readonly string[] LegacyDbPaths =
{
    "WegInstallerSystems.db",
    "installer_systems.db",
    "carros.db"
};
```

### 9.3 Tablas principales

La explicación completa del esquema esta en [BaseDeDatos.md](c:/Users/wandica/source/repos/ConcesionaroCarros/SistemaDeInstalacion/Docs/Developers/BaseDeDatos.md), pero a nivel practico estas son las tablas clave:

- `Usuarios`
- `Instaladores`
- `Administrador`
- `PasswordRecoveryLog`

## 10. Sesion y autorizacion

El estado global de sesion esta en [SesionUsuario.cs](c:/Users/wandica/source/repos/ConcesionaroCarros/SistemaDeInstalacion/Services/SesionUsuario.cs):

```csharp
public static Usuario UsuarioActual { get; set; }
public static bool ModoAdministrador { get; set; }

public static bool EsAdmin =>
    ModoAdministrador && RolesSistema.EsAdministrador(UsuarioActual?.Rol);
```

Esto tiene una consecuencia clave:

- no basta con tener rol `ADMINISTRADOR`
- también se necesita haber entrado por el flujo administrativo

Es decir:

- si alguien tiene rol `ADMINISTRADOR` pero entra por login normal, no se considera admin pleno en UI
- si entra por login admin, `ModoAdministrador = true` y ahi si obtiene permisos de gestión

## 11. Login normal

El login normal vive en [LoginViewModel.cs](c:/Users/wandica/source/repos/ConcesionaroCarros/SistemaDeInstalacion/ViewModels/LoginViewModel.cs).

Permite entrar con:

- correo completo
- alias del correo
- usuario del equipo Windows

Resolución del correo real:

```csharp
var correoLogin =
    string.Equals(usuarioIngreso, usuarioPc, StringComparison.OrdinalIgnoreCase) &&
    !string.IsNullOrWhiteSpace(correoPrincipalDispositivo)
        ? _db.ObtenerCorreoPorUsuarioLogin(correoPrincipalDispositivo, usuarioPc, nombreVisible)
        : _db.ObtenerCorreoPorUsuarioLogin(usuarioIngreso, usuarioPc, nombreVisible);
```

Validación final:

```csharp
var usuario = _db.Login(correoLogin, Password);
```

Las credenciales recordadas se guardan en:

- `%AppData%\ConcesionaroCarros\login.remember`

Y se cifran con:

```csharp
var cifrado = ProtectedData.Protect(datos, Entropy, DataProtectionScope.CurrentUser);
```

Implicación:

- ese archivo solo sirve para el mismo usuario Windows
- no es reutilizable en otra cuenta del sistema operativo

## 12. Login administrativo

El flujo admin esta separado del flujo normal.

Reglas prácticas:

- valida `Administrador.UsuarioSistema`
- valida `PasswordAdminHash`
- resuelve el usuario base por `Correo`
- activa `SesionUsuario.ModoAdministrador`

Esto existe para separar:

- contraseña normal de uso diario
- contraseña administrativa de gestión

## 13. Registro de usuarios y administradores

### 13.1 Registro normal

El registro normal:

- exige correo `@weg.net`
- deriva nombre desde Windows o desde el alias del correo
- crea el usuario base en `Usuarios`
- guarda la contraseña como hash `SHA-256`

### 13.2 Registro administrativo

El registro administrativo crea o sincroniza dos cosas:

1. usuario base en `Usuarios`
2. credencial admin en `Administrador`

Fragmento representativo:

```csharp
usuariosDb.RegistrarYRetornarId(nuevo, PasswordNormal);

adminsDb.GuardarOActualizar(new Administrador
{
    Nombres = nombres,
    Apellidos = apellidos,
    Correo = Correo,
    UsuarioSistema = usuarioSistemaLogin,
    Rol = rolSeleccionado
}, PasswordAdmin);
```

Eso significa que el admin tiene dos credenciales distintas:

- `PasswordNormal`
- `PasswordAdmin`

## 14. Como se guardan las contraseñas

En [UsuariosDbService.cs](c:/Users/wandica/source/repos/ConcesionaroCarros/SistemaDeInstalacion/Db/UsuariosDbService.cs) se usa `SHA-256` y luego base64:

```csharp
using (var sha = SHA256.Create())
{
    var bytes = Encoding.UTF8.GetBytes(password ?? string.Empty);
    var hash = sha.ComputeHash(bytes);
    return Convert.ToBase64String(hash);
}
```

Esto aplica tanto al usuario normal como al administrativo, aunque cada uno se guarda en tablas y columnas distintas.

## 15. Módulo de instaladores

Este es uno de los módulos mas importantes del proyecto.

Responsabilidades del módulo:

- registrar ejecutables `.exe`
- listarlos por carpeta funcional
- permitir que un admin los cree, edite o elimine
- permitir que un usuario los ejecute si estan asignados

El flujo principal esta en [InstaladoresViewModel.cs](c:/Users/wandica/source/repos/ConcesionaroCarros/SistemaDeInstalacion/ViewModels/InstaladoresViewModel.cs).

Carpetas funcionales actuales:

- `Punto local de desarrollo planta`
- `Desarrollo global`

## 16. Como agregar un nuevo instalador al sistema

Esta parte conviene documentarla paso a paso porque es una operación funcional clave.

### 16.1 Regla técnica actual

La aplicación no empaqueta otros instaladores dentro de si misma ni extrae archivos comprimidos. Lo que hace es:

1. registrar la ruta de un `.exe`
2. mostrarlo en la UI
3. ejecutarlo cuando el usuario hace clic

La selección del archivo se hace con `OpenFileDialog` y solo acepta `.exe`:

```csharp
OpenFileDialog dlg = new OpenFileDialog();
dlg.Filter = "Ejecutables (*.exe)|*.exe";
```

### 16.2 Proceso recomendado para registrar un instalador

1. compilar o conseguir el `.exe` que se quiere distribuir
2. ubicarlo en una ruta estable
3. abrir la app con una sesión administrativa
4. entrar al módulo de instaladores
5. usar `Nuevo Instalador`
6. buscar el archivo `.exe`
7. completar `Nombre`, `Descripcion` y `Carpeta`
8. guardar

### 16.3 Que significa "ruta estable"

La ruta del ejecutable es critica porque hoy funciona como identificador operativo. El guardado se hace asi:

```csharp
INSERT INTO Instaladores
(Ruta, Nombre, Descripcion, Carpeta, FechaRegistro)
VALUES ($ruta, $nombre, $descripcion, $carpeta, $fecha);
```

Y la actualización usa:

```csharp
UPDATE Instaladores
SET Nombre = $nombre,
    Descripcion = $descripcion,
    Carpeta = $carpeta
WHERE Ruta = $ruta
```

Esto implica:

- la `Ruta` no es solo un dato visual
- la `Ruta` se usa como clave operativa
- si el ejecutable se mueve de carpeta, la app puede dejar de encontrarlo
- si el ejecutable cambia de nombre o ubicación, hay que corregir la base o volver a registrarlo

### 16.4 Donde conviene guardar los `.exe`

Como recomendación de continuidad del proyecto:

- usar carpetas de red o carpetas locales controladas
- evitar rutas temporales del escritorio de un usuario
- evitar rutas dentro de `Downloads`
- evitar rutas que cambien en cada despliegue manual

Buenas opciones:

- una carpeta compartida corporativa
- una carpeta local estandar por ambiente
- una ruta documentada que no cambie entre equipos administradores

## 17. Como se ejecuta un instalador desde la app

La ejecución real del `.exe` se hace así:

```csharp
Process.Start(new ProcessStartInfo
{
    FileName = inst.Ruta,
    UseShellExecute = true
});
```

Y antes verifica:

```csharp
if (inst != null && File.Exists(inst.Ruta))
```

Puntos importantes:

- si el archivo no existe en esa ruta, no se ejecuta
- la app depende del sistema operativo para lanzar el proceso
- `UseShellExecute = true` permite respetar comportamiento normal de Windows
- si aparece UAC y el usuario cancela, el codigo `1223` se trata como cancelación esperada

Esto significa que el sistema **no instala silenciosamente** por si mismo. Solo dispara el ejecutable registrado.

## 18. Como asignar instaladores a usuarios

La relación usuario-instalador no esta normalizada en otra tabla. Se guarda en `Usuarios.AplicativosJson`.

En [Models/Usuario.cs](c:/Users/wandica/source/repos/ConcesionaroCarros/SistemaDeInstalacion/Models/Usuario.cs):

```csharp
public void EstablecerAplicativosAsignados(IEnumerable<string> rutas)
{
    var serializer = new JavaScriptSerializer();
    var lista = (rutas ?? Enumerable.Empty<string>())
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Distinct()
        .ToList();

    AplicativosJson = serializer.Serialize(lista);
}
```

Y el guardado en base se hace asi:

```csharp
UPDATE Usuarios
SET AplicativosJson = $apps
WHERE Id = $id;
```

Eso quiere decir:

- el permiso de un usuario se basa en una lista de rutas
- si la ruta del `.exe` cambia, el permiso puede quedar roto
- no hay integridad referencial real entre `Usuarios` e `Instaladores`

## 19. Como se filtran los instaladores visibles por usuario

Cuando la sesión no es admin, el sistema filtra por rutas asignadas:

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

Conclusión funcional:

- admin ve todo el catálogo
- usuario normal ve solo las rutas que estan en su `AplicativosJson`

## 20. Proceso recomendado para empaquetar la aplicación

Como esta app es de escritorio clásica y usa archivos externos, la forma segura de empaquetarla es por carpeta de salida.

### 20.1 Opción recomendada

1. compilar en `Release`
2. entrar a `bin/Release/`
3. tomar el contenido funcional completo
4. validar que el ejecutable abra en una carpeta limpia
5. distribuir esa carpeta o usarla como base para un instalador corporativo

### 20.2 Que no se debe olvidar al empaquetar

- `SistemaDeInstalacion.exe`
- `SistemaDeInstalacion.exe.config`
- `Fonts/`
- `runtimes/`
- dlls de `SQLite`
- dlls de `PDFsharp`
- dlls de `QuestPDF`

### 20.3 Si se quiere hacer un instalador corporativo

Si en el futuro se usa `Inno Setup`, `WiX`, `Advanced Installer` o similar, la lógica recomendada sería:

1. copiar el contenido de `bin/Release/`
2. preservar estructura de carpetas
3. definir carpeta de instalación fija
4. verificar permisos de lectura/escritura sobre la carpeta final
5. probar que la base `WegInstaladores.db` pueda crearse ahi

### 20.4 Punto crítico de despliegue

Como la base se crea junto al ejecutable, el directorio final necesita permiso de escritura. Si se despliega en una carpeta muy restringida, la app puede fallar al inicializar `SQLite`.

## 21. Proceso recomendado para publicar un nuevo ejecutable de negocio

Si el equipo necesita subir una nueva versión de un aplicativo interno, el proceso sano seráa:

1. generar el nuevo `.exe`
2. publicarlo en la ruta final estable
3. comprobar manualmente que abre fuera de la app
4. entrar con un admin a `SistemaDeInstalacion`
5. registrar o actualizar el instalador
6. asignarlo a usuarios o roles operativos
7. validar con una cuenta no admin
8. validar con una cuenta admin

Si el `.exe` reemplaza a otro anterior:

- si se conserva la misma ruta, el impacto es menor
- si cambia la ruta, hay que revisar asignaciones en `AplicativosJson`

## 22. Consultas SQL utiles para soporte técnico

Ver usuarios:

```sql
SELECT Id, Nombres, Apellidos, Correo, Rol, AplicativosJson
FROM Usuarios
ORDER BY Id DESC;
```

Ver administradores:

```sql
SELECT Id, Correo, UsuarioSistema, Rol, FechaRegistro
FROM Administrador
ORDER BY Id DESC;
```

Ver instaladores:

```sql
SELECT Id, Ruta, Nombre, Descripcion, Carpeta, FechaRegistro
FROM Instaladores
ORDER BY Id DESC;
```

Ver recuperaciones:

```sql
SELECT Id, UsuarioId, CorreoUsuario, CorreoAdministrador, FechaRecuperacion
FROM PasswordRecoveryLog
ORDER BY Id DESC;
```

## 23. Archivos que se deben revisar antes de tocar algo importante

Si vas a tocar autenticación:

- [ViewModels/LoginViewModel.cs](c:/Users/wandica/source/repos/ConcesionaroCarros/SistemaDeInstalacion/ViewModels/LoginViewModel.cs)
- [ViewModels/AdminLoginViewModel.cs](c:/Users/wandica/source/repos/ConcesionaroCarros/SistemaDeInstalacion/ViewModels/AdminLoginViewModel.cs)
- [Db/UsuariosDbService.cs](c:/Users/wandica/source/repos/ConcesionaroCarros/SistemaDeInstalacion/Db/UsuariosDbService.cs)
- [Db/AdministradoresDbService.cs](c:/Users/wandica/source/repos/ConcesionaroCarros/SistemaDeInstalacion/Db/AdministradoresDbService.cs)
- [Services/SesionUsuario.cs](c:/Users/wandica/source/repos/ConcesionaroCarros/SistemaDeInstalacion/Services/SesionUsuario.cs)

Si vas a tocar instaladores:

- [ViewModels/InstaladoresViewModel.cs](c:/Users/wandica/source/repos/ConcesionaroCarros/SistemaDeInstalacion/ViewModels/InstaladoresViewModel.cs)
- [ViewModels/FormularioInstaladorViewModel.cs](c:/Users/wandica/source/repos/ConcesionaroCarros/SistemaDeInstalacion/ViewModels/FormularioInstaladorViewModel.cs)
- [Db/InstaladorDbService.cs](c:/Users/wandica/source/repos/ConcesionaroCarros/SistemaDeInstalacion/Db/InstaladorDbService.cs)
- [Models/Instalador.cs](c:/Users/wandica/source/repos/ConcesionaroCarros/SistemaDeInstalacion/Models/Instalador.cs)

Si vas a tocar usuarios y permisos:

- [ViewModels/GestionUsuarioViewModel.cs](c:/Users/wandica/source/repos/ConcesionaroCarros/SistemaDeInstalacion/ViewModels/GestionUsuarioViewModel.cs)
- [ViewModels/FormularioUsuarioViewModel.cs](c:/Users/wandica/source/repos/ConcesionaroCarros/SistemaDeInstalacion/ViewModels/FormularioUsuarioViewModel.cs)
- [Models/Usuario.cs](c:/Users/wandica/source/repos/ConcesionaroCarros/SistemaDeInstalacion/Models/Usuario.cs)
- [Services/RolesSistema.cs](c:/Users/wandica/source/repos/ConcesionaroCarros/SistemaDeInstalacion/Services/RolesSistema.cs)

Si vas a tocar persistencia:

- [Db/DatabaseInitializer.cs](c:/Users/wandica/source/repos/ConcesionaroCarros/SistemaDeInstalacion/Db/DatabaseInitializer.cs)
- [Db/UsuariosDbService.cs](c:/Users/wandica/source/repos/ConcesionaroCarros/SistemaDeInstalacion/Db/UsuariosDbService.cs)
- [Db/InstaladorDbService.cs](c:/Users/wandica/source/repos/ConcesionaroCarros/SistemaDeInstalacion/Db/InstaladorDbService.cs)
- [Docs/Developers/BaseDeDatos.md](c:/Users/wandica/source/repos/ConcesionaroCarros/SistemaDeInstalacion/Docs/Developers/BaseDeDatos.md)

## 24. Checklist tecnico minimo antes de entregar cambios

- la app abre desde cero
- `WegInstaladores.db` se crea correctamente
- login normal sigue funcionando
- login admin sigue funcionando
- un admin puede registrar instaladores
- un usuario no admin solo ve lo asignado
- la ejecucion de un `.exe` real sigue funcionando
- la base migra correctamente desde una copia vieja si aplica
- el paquete de salida incluye `Fonts/` y `runtimes/`

## 25. Tests automatizados

El repositorio ahora incluye un proyecto de pruebas en:

- `SistemaDeInstalacion.Tests/`

Cobertura actual:

- inicialización de la base
- actualización de una base con esquema legacy
- serialización de `AplicativosJson`
- reglas de `RolesSistema`
- registro y login de usuarios
- resolución de correo por alias
- registro, login y sincronización de administradores
- guardado, actualizacion y eliminación de instaladores

Comando de ejecución:

```powershell
dotnet run --project SistemaDeInstalacion.Tests/SistemaDeInstalacion.Tests.csproj
```

Nota importante para el desarrollador:

- este runner compila contra las DLL locales que ya produce la app en `bin/Debug/`
- si esas DLL no existen todavia, primero hay que compilar la aplicación principal

Salida esperada:

```text
[OK] ...
Resultado: 9/9 tests aprobados.
```

Si algún test falla:

- revisar primero si la base anterior quedo bloqueada por otro proceso
- validar que `bin/Debug/` siga teniendo las dependencias de `SQLite`
- revisar si el cambio afecto persistencia, roles, login o rutas de instaladores

## 26. Riesgos y deuda tecnica actual

- el namespace sigue siendo `ConcesionaroCarros` aunque el producto se llame `SistemaDeInstalacion`
- `MainWindow` reinicializa la base aunque `App` ya lo hizo
- la asignación de aplicativos depende de `Ruta`, no de un identificador estable
- no hay versionado formal de migraciones
- la base vive junto al ejecutable, lo que vuelve sensible el permiso de escritura de la carpeta final
- el proyecto mezcla `MVVM` con apertura directa de ventanas desde `ViewModel`

## 27. Si yo tuviera que continuar este proyecto

El orden recomendado seria:

1. no tocar primero la UI, sino entender `DatabaseInitializer`, `SesionUsuario`, `UsuariosDbService` e `InstaladorDbService`
2. validar manualmente el flujo completo con una base nueva
3. documentar cualquier cambio de esquema tambien en [BaseDeDatos.md](c:/Users/wandica/source/repos/ConcesionaroCarros/SistemaDeInstalacion/Docs/Developers/BaseDeDatos.md)
4. evitar cambiar la semantica de `Ruta` sin plan de migracion
5. si se va a profesionalizar el producto, el primer gran refactor deberia ser separar la relacion `usuario -> instalador` a una tabla relacional

## 28. Documentos relacionados

- [Docs/README.md](c:/Users/wandica/source/repos/ConcesionaroCarros/SistemaDeInstalacion/Docs/README.md)
- [Docs/Developers/BaseDeDatos.md](c:/Users/wandica/source/repos/ConcesionaroCarros/SistemaDeInstalacion/Docs/Developers/BaseDeDatos.md)
- [Docs/users/README.md](c:/Users/wandica/source/repos/ConcesionaroCarros/SistemaDeInstalacion/Docs/users/README.md)
