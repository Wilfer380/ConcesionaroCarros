# BaseDeDatos

Este guia documenta a camada real de persistência de `SistemaDeInstalacion` de acordo com o código atual em `Db/`, `ViewModels/`, `App.config` e `SistemaDeInstalacion.Tests/`.

Ele complementa:

- [Developer](help://developers/developer)
- [Administradores](help://administradores/administradores)

## Stack real

- provedor: `Microsoft.Data.Sqlite` `10.0.2` em `SistemaDeInstalacion.csproj`;
- motor: `SQLite` sobre arquivo local/compartilhado;
- runtime: `.NET Framework 4.8`;
- acesso a dados: SQL manual com `SqliteConnection`, `SqliteCommand` e consultas parametrizadas;
- ORM / migrador: não existe.

## Arquivo e configuração efetiva

O caminho ativo é resolvido em `Db/DatabaseInitializer.cs`:

- arquivo padrão: `WegInstaladores.db`;
- chave de configuração: `CC_SHARED_DATABASE_PATH`;
- se a chave estiver vazia, o banco cai em `AppDomain.CurrentDomain.BaseDirectory`;
- se a chave tiver variáveis de ambiente, elas são expandidas com `Environment.ExpandEnvironmentVariables()`;
- se a chave for relativa, ela se torna absoluta usando o diretório de execução como base;
- se a chave for absoluta, é usada como está.

A string de conexão real é apenas:

```text
Data Source={CurrentDbPath}
```

Não existe configuração de nível de connection string para `journal_mode`, `foreign_keys`, `cache` ou outros parâmetros avançados do SQLite.

Nota técnica: a preferência visual (`ThemePreference`) não é persistida em `SQLite`. Ela vive como configuração local por perfil Windows em `Properties/Settings.settings`, é resolvida em tempo de execução por `ThemeManager` e fica fora do modelo relacional. A referência funcional principal para configuração e troca de tema está em [Configuração](help://users/user#configuracao).

## Bootstrap real

`App.xaml.cs` executa `DatabaseInitializer.Initialize()` em `OnStartup()` antes de abrir `LoginView`.

Fluxo real:

```text
App.OnStartup()
  -> DatabaseInitializer.Initialize()
     -> cria diretório do banco se necessário
     -> se WegInstaladores.db não existir, copia um banco legacy se encontrar
     -> abre SQLite
     -> cria tabelas atuais com CREATE TABLE IF NOT EXISTS
     -> adiciona colunas faltantes com ALTER TABLE
     -> migra dados de Administradores -> Administrador
     -> remove tabelas legacy
     -> normaliza dados herdados
```

## Migrações e evolução manual de esquema

Não existe versionamento formal de esquema. A migração é programática e idempotente dentro de `DatabaseInitializer.Initialize()`.

### Arquivos legacy que ainda são absorvidos

Se `CurrentDbPath` não existir, o bootstrap tenta copiar um destes arquivos a partir do mesmo diretório:

- `WegInstallerSystems.db`
- `installer_systems.db`
- `carros.db`

Depois disso, tenta excluir o arquivo legacy migrado em modo best-effort.

### Estratégia de evolução

A aplicação realiza alterações manuais com `ALTER TABLE` por meio de `EnsureColumnExists()` e `PRAGMA table_info()`.

Colunas adicionadas por compatibilidade:

- `Usuarios.FotoPerfil`
- `Usuarios.AplicativosJson`
- `Instaladores.Nombre`
- `Instaladores.Descripcion`
- `Instaladores.Carpeta`
- todas as colunas atuais de `Administrador`
- todas as colunas atuais de `PasswordRecoveryLog`

Ponto importante: `Instaladores` é criado primeiro com uma estrutura mínima e depois completado por alterações posteriores. Na prática, o esquema atual depende do bootstrap + alterações, não apenas do `CREATE TABLE` inicial.

### Tabelas legacy removidas

O bootstrap executa:

```sql
DROP TABLE IF EXISTS Carros;
DROP TABLE IF EXISTS Clientes;
DROP TABLE IF EXISTS Empleados;
DROP TABLE IF EXISTS Administradores;
```

Se a tabela legacy `Administradores` existir, suas linhas são primeiro copiadas para `Administrador` usando `INSERT OR IGNORE ... SELECT ...` antes da remoção.

### Normalização automática de dados

Na inicialização são aplicadas as seguintes correções:

- `Usuarios.AplicativosJson` vazio ou `NULL` passa a `'[]'`;
- `Usuarios.Rol = CLIENTE` migra para `VENTAS`;
- `Usuarios.Rol = INGENIERO` migra para `INGENIERIA`;
- `Usuarios.Rol = ADMIN` migra para `ADMINISTRADOR`;
- `Instaladores.Carpeta` vazia ou `NULL` migra para `Desarrollo global`.

## Pragmas e concorrência

Apenas um pragma é configurado explicitamente em código:

```sql
PRAGMA busy_timeout = 5000;
```

Ele é aplicado ao abrir conexão em:

- `Db/UsuariosDbService.cs`
- `Db/AdministradoresDbService.cs`

Ele não é aplicado em `Db/InstaladorDbService.cs`.

Além disso, `UsuariosDbService` implementa reintentos manuais para `SqliteErrorCode == 5` com até três tentativas e esperas incrementais.

`AdministradoresDbService` não implementa reintentos manuais.

`InstaladorDbService` não implementa nem `busy_timeout` nem reintentos explícitos.

## Tabelas reais e papéis

### `Usuarios`

Definição atual:

```sql
CREATE TABLE IF NOT EXISTS Usuarios (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Nombres TEXT NOT NULL,
    Apellidos TEXT NOT NULL,
    Correo TEXT NOT NULL UNIQUE,
    Telefono TEXT,
    PasswordHash TEXT NOT NULL,
    Rol TEXT NOT NULL,
    FechaRegistro TEXT NOT NULL,
    FotoPerfil TEXT,
    AplicativosJson TEXT DEFAULT '[]'
);
```

Papel real:

- identidade principal do sistema;
- autenticação normal;
- papel funcional da UI;
- armazenamento do caminho da foto de perfil;
- armazenamento dos aplicativos atribuídos como JSON de rotas.

### `Instaladores`

Definição funcional após o bootstrap:

```sql
CREATE TABLE IF NOT EXISTS Instaladores (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Ruta TEXT,
    Carpeta TEXT,
    FechaRegistro TEXT,
    Nombre TEXT,
    Descripcion TEXT
);
```

Papel real:

- catálogo de executáveis lançados a partir da app;
- classificação por pasta, como `Desarrollo global` e `Punto local de desarrollo planta`;
- fonte de atribuição de aplicativos quando o usuário não é admin.

### `Administrador`

```sql
CREATE TABLE IF NOT EXISTS Administrador (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Nombres TEXT NOT NULL,
    Apellidos TEXT,
    Correo TEXT NOT NULL UNIQUE,
    UsuarioSistema TEXT NOT NULL,
    Rol TEXT NOT NULL,
    PasswordAdminHash TEXT NOT NULL,
    FechaRegistro TEXT NOT NULL
);
```

Papel real:

- credencial privilegiada separada para o login admin;
- resolução do usuário administrativo por `UsuarioSistema`;
- ponte para o usuário base por meio de `Correo`.

## Camada de serviços sobre SQLite

Serviços principais:

- `UsuariosDbService`: registro, login, consultas e salvamento de atribuições;
- `AdministradoresDbService`: registro admin e login admin;
- `InstaladorDbService`: criação, atualização, exclusão e listagem de instaladores.

Esses serviços constroem o contrato real com os ViewModels WPF. Não existe camada de repositório nem abstração ORM acima deles.

## Recomendações operacionais

- se você modificar o esquema, revise `DatabaseInitializer`, os serviços impactados e os testes em conjunto;
- se adicionar uma nova coluna, preserve o comportamento idempotente do bootstrap;
- se alterar o caminho do banco compartilhado, revise também o fluxo do launcher e da atualização;
- se localizar mensagens de erro da persistência, lembre que os dados já armazenados não são traduzidos.

## Links de referência

- [Developer](help://developers/developer)
- [SQLite Documentation](https://sqlite.org/docs.html)
- [PRAGMA busy_timeout](https://sqlite.org/pragma.html#pragma_busy_timeout)
- [Microsoft.Data.Sqlite](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/)
