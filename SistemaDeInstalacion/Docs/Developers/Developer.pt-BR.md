# Developer

## Propósito e escopo

Este documento explica o funcionamento técnico de `SistemaDeInstalacion` seguindo o percurso real do código desde a inicialização até os principais módulos. O foco não é o uso funcional da UI, e sim entender quais classes participam, qual estado compartilham, onde a informação é persistida e quais restrições condicionam qualquer mudança no produto.

O sistema não está dividido em frontend e backend. A solução combina:

- um `launcher` WinForms para instalação e atualização;
- uma aplicação desktop `WPF` para autenticação, administração, documentação, configurações e execução de instaladores;
- persistência local ou compartilhada em `SQLite` por meio de `Microsoft.Data.Sqlite`.

## Stack real e restrições

| Tema | Evidência no repositório | Implicação técnica |
|---|---|---|
| UI principal | `SistemaDeInstalacion.csproj`, `Views/*.xaml` | Aplicação `WPF` sobre `.NET Framework 4.8`. |
| Launcher | `LauncherSistema/Program.cs`, `LauncherSistema/LauncherService.cs` | A distribuição real começa por um executável auxiliar antes de abrir `SistemaDeInstalacion.exe`. |
| Persistência | `Db/DatabaseInitializer.cs`, `Db/*.cs` | Não existe ORM nem API remota; o acesso é SQL direto. |
| Testes | `SistemaDeInstalacion.Tests/*.cs` | Existe cobertura unitária para banco, papéis e modelos; não há testes de UI. |
| Configuração | `App.config` | O caminho do banco compartilhado e várias regras corporativas vêm de `appSettings`. |
| Publicação | `SistemaDeInstalacion.csproj`, `LauncherSistema/LauncherService.cs` | A entrega depende de um compartilhamento UNC corporativo e de `SetupSistema.exe`. |

Restrições importantes confirmadas pelo código:

1. a autenticação administrativa não depende apenas do papel do usuário; ela também exige entrar por `AdminLoginViewModel` para ativar `SesionUsuario.ModoAdministrador`;
2. as permissões de aplicativos não são relacionais; são armazenadas como JSON em `Usuario.AplicativosJson`;
3. o acesso ao `SQLite` é local e direto; o código mitiga bloqueios com `PRAGMA busy_timeout = 5000` e reintentos, não com um modelo transacional forte multiusuário;
4. a aplicação depende de serviços do perfil Windows para resolver nome visível e e-mail do dispositivo (`WindowsProfileService`);
5. a localização da interface se apoia em `.resx`, `LocalizationService`, `LocalizedText` e `TranslateExtension`.

## Mapa do repositório

| Caminho | Responsabilidade principal |
|---|---|
| `SistemaDeInstalacion.sln` | Solução Visual Studio com app WPF, testes e launcher. |
| `SistemaDeInstalacion/` | Aplicação principal. |
| `App.xaml.cs` | Inicialização WPF, handlers globais de exceção, inicialização de tema, idioma e banco. |
| `Db/` | Inicialização de esquema e acesso SQL para usuários, administradores, instaladores e recuperação. |
| `ViewModels/` | Login, navegação do shell, gestão de usuários, instaladores, ajuda, configuração, logs e atualização de idioma. |
| `Services/` | Sessão, papéis, logging, documentação, localização, tema, utilidades Windows e modais. |
| `Docs/` | Documentação embutida renderizada dentro da aplicação. |
| `LauncherSistema/` | Executável que instala, atualiza e depois abre a app principal. |
| `SistemaDeInstalacion.Tests/` | Projeto MSTest para persistência e regras de domínio. |

## Passo 1. Ponto de entrada real: `LauncherSistema`

O ponto de entrada real do produto completo não é `App.xaml.cs`, e sim `LauncherSistema/Program.cs`. `Main()` delega para `LauncherService.Ejecutar(args)`.

`LauncherService` resolve quatro cenários principais antes de abrir a aplicação WPF:

1. detecta uma inicialização pós-instalação ou pós-atualização com `--post-update` ou `--post-install`;
2. compara a versão local com `version.txt` no recurso compartilhado;
3. se `SistemaDeInstalacion.exe` não existir, solicita instalação e executa `SetupSistema.exe`;
4. se houver uma versão mais nova, pode iniciar atualização silenciosa e encerrar o processo atual.

Arquivos-chave:

- `LauncherSistema/Program.cs`
- `LauncherSistema/LauncherService.cs`

## Passo 2. Inicialização WPF e bootstrap

Quando `SistemaDeInstalacion.exe` é aberto, o fluxo entra em `App.OnStartup()`.

Sequência real em `App.xaml.cs`:

1. registra `DispatcherUnhandledException` e `AppDomain.CurrentDomain.UnhandledException`;
2. inicializa `LocalizationService`;
3. registra `GlobalCopyContextService`;
4. escreve o evento `Inicio de aplicación` em `LogService.Info`;
5. inicializa o banco com `DatabaseInitializer.Initialize()` e mede latência;
6. mostra `LoginView`.

Isso significa que banco, logs e localização são pré-requisitos de qualquer tela funcional.

## Passo 3. Inicialização do banco de dados

`Db/DatabaseInitializer.cs` é o bootstrapper de persistência. Ele faz muito mais do que criar `WegInstaladores.db`.

Responsabilidades confirmadas:

1. resolver `CurrentDbPath` a partir de `CC_SHARED_DATABASE_PATH`;
2. criar o diretório contêiner, se necessário;
3. migrar arquivos legacy quando o banco atual ainda não existe;
4. criar as tabelas vigentes (`Usuarios`, `Instaladores`, `Administrador`, `PasswordRecoveryLog`);
5. adicionar colunas faltantes com `ALTER TABLE` por meio de `EnsureColumnExists()`;
6. migrar dados de `Administradores` para `Administrador`;
7. remover tabelas antigas;
8. normalizar valores herdados, como papéis e pastas padrão.

Para o detalhe completo da persistência, consulte [BaseDeDatos](help://developers/base-de-datos).

## Passo 4. Login normal

`LoginViewModel` implementa o fluxo de autenticação operacional comum.

Se você precisar da narrativa funcional desta tela, consulte [Passo a passo para entrar](help://users/user#passo-a-passo-para-entrar).

Arquivos-chave:

- `ViewModels/LoginViewModel.cs`
- `Db/UsuariosDbService.cs`
- `Views/LoginView.xaml`

Fluxo real:

1. o usuário informa alias ou e-mail em `Usuario`;
2. `UsuariosDbService.ObtenerCorreoPorUsuarioLogin()` resolve o e-mail real usando e-mail completo, alias antes de `@` ou mapeamento do dispositivo;
3. `UsuariosDbService.Login()` compara `Correo` e `PasswordHash` usando `SHA-256` em Base64;
4. se autenticar corretamente, `AbrirSesionUsuario()` atribui `SesionUsuario.UsuarioActual` e força `SesionUsuario.ModoAdministrador = false`;
5. `MainWindow` é aberto.

## Passo 5. Login administrativo

`AdminLoginViewModel` define o fluxo privilegiado. Esse é o ponto que realmente habilita o modo administrador no shell principal.

Arquivos-chave:

- `ViewModels/AdminLoginViewModel.cs`
- `Db/AdministradoresDbService.cs`
- `Db/UsuariosDbService.cs`
- `Services/SesionUsuario.cs`

Fluxo real:

1. valida que exista um registro administrativo por `UsuarioSistema`;
2. autentica contra a tabela `Administrador` usando a senha administrativa;
3. resolve o usuário base em `Usuarios` por meio do e-mail do administrador;
4. garante que o papel do usuário base seja `ADMINISTRADOR`;
5. atribui `SesionUsuario.UsuarioActual = usuarioNormal` e `SesionUsuario.ModoAdministrador = true`;
6. abre `MainWindow`.

Se você precisar da rota funcional correspondente, consulte [Passo 4. Entrar pelo login administrativo](help://administradores/administradores#passo-4-entrar-pelo-login-administrativo).

## Passo 6. Sessão, shell e modo administrador

O estado de sessão vive em `Services/SesionUsuario.cs` e é consumido principalmente por `MainViewModel`.

Estado compartilhado relevante:

```csharp
public static Usuario UsuarioActual { get; set; }
public static bool ModoAdministrador { get; set; }

public static bool EsAdmin =>
    ModoAdministrador && RolesSistema.EsAdministrador(UsuarioActual?.Rol);
```

Isso define uma regra central do sistema: o papel isolado não basta; é necessário ter entrado pela rota administrativa.

`MainViewModel` monta a navegação interna:

- `MostrarInstaladores()` carrega `InstaladoresView` como vista padrão;
- `ShowGestionUsuariosCommand` só funciona se `EsAdmin` for `true`;
- `ShowLogsCommand` expõe o centro de logs com uma restrição específica de suporte/desenvolvimento; o detalhe canônico fica em [Passo 12. Logs e diagnóstico de suporte](help://developers/developer#passo-12-logs-e-diagnostico-de-suporte);
- `ShowAyudaCommand` carrega `HelpViewModel(EsAdministrador)` e filtra a documentação por perfil e idioma;
- `ShowSettingsCommand` carrega `SettingsView`;
- `CerrarSesionCommand` limpa a sessão, retorna a `LoginView` e fecha a janela atual.

Para o lado funcional do shell, consulte [Vista principal do usuário](help://users/user#vista-principal-do-usuario) e [Passo 6. Vista principal do administrador](help://administradores/administradores#passo-6-vista-principal-do-administrador).

## Passo 7. Módulo de instaladores

Este módulo mistura catálogo, permissões e execução de processos externos.

Arquivos-chave:

- `ViewModels/InstaladoresViewModel.cs`
- `ViewModels/FormularioInstaladorViewModel.cs`
- `Db/InstaladorDbService.cs`
- `Models/Instalador.cs`
- `Views/InstaladoresView.xaml`

Responsabilidades reais:

1. carregar instaladores a partir de `InstaladorDbService.ObtenerTodos()`;
2. normalizar pastas em categorias local e global;
3. se a sessão não for admin, filtrar pelas rotas armazenadas em `SesionUsuario.UsuarioActual.ObtenerAplicativosAsignados()`;
4. permitir criar, editar e excluir apenas em modo admin;
5. executar o arquivo registrado usando o start de processos do Windows.

Para a rota funcional, consulte [Passo 7. Gestão de instaladores](help://administradores/administradores#passo-7-gestao-de-instaladores) e [Como executar um aplicativo atribuído](help://users/user#como-executar-um-aplicativo-atribuido).

## Passo 8. Gestão de usuários e atribuição

`GestionUsuarioViewModel` controla a lista de usuários e a atribuição de aplicativos.

Arquivos-chave:

- `ViewModels/GestionUsuarioViewModel.cs`
- `ViewModels/FormularioUsuarioViewModel.cs`
- `Db/UsuariosDbService.cs`
- `Views/GestionUsuarioView.xaml`
- `Views/FormularioUsuarioView.xaml`

Responsabilidades técnicas:

1. carregar todos os usuários a partir do serviço de banco;
2. abrir os formulários modais de criação e edição;
3. excluir usuários selecionados;
4. abrir o painel de atribuição;
5. atribuir aplicativos serializando as rotas selecionadas em `AplicativosJson`.

Para o percurso funcional, consulte [Passo 8. Gestão de usuários](help://administradores/administradores#passo-8-gestao-de-usuarios) e [Passo 9. Atribuição de aplicativos](help://administradores/administradores#passo-9-atribuicao-de-aplicativos).

## Passo 9. Registro e recuperação

Os fluxos de registro e recuperação estão separados em ViewModels diferentes:

- `RegisterViewModel` para o registro normal;
- `AdminRegisterViewModel` para o registro administrativo;
- `MicrosoftRecoveryViewModel` e `RecoveryCodePopupView` para recuperação de senha.

Esses fluxos combinam regras de validação, verificações em banco e feedback localizado.

Referências funcionais:

- [Registro de usuário](help://users/user#registro-de-usuario)
- [Passo 3. Registro administrativo](help://administradores/administradores#passo-3-registro-administrativo)
- [Recuperação de senha](help://users/user#recuperacao-de-senha)

## Passo 10. Ajuda e documentação integrada

O módulo de Ajuda carrega documentos markdown a partir de `Docs/` e os renderiza dentro da aplicação WPF.

Arquivos-chave:

- `Services/DocumentationService.cs`
- `Services/MarkdownDocumentRenderer.cs`
- `ViewModels/HelpViewModel.cs`
- `Views/HelpView.xaml`
- `Views/HelpView.xaml.cs`

Comportamentos importantes:

1. a documentação é filtrada por perfil;
2. o serviço resolve o arquivo localizado conforme a cultura selecionada (`.en.md`, `.pt-BR.md` ou documento base em espanhol);
3. links internos como `help://users/user#...` são resolvidos dentro do visualizador embutido;
4. quando o idioma muda, a ajuda recarrega as seções e tenta manter o documento e a âncora selecionados.

Referências funcionais:

- [Como usar a guia Ajuda](help://users/user#como-usar-a-guia-ajuda)
- [Passo 10. Uso da guia Ajuda](help://administradores/administradores#passo-10-uso-da-guia-ajuda)

## Passo 11. Localização e tema

A stack atual de localização se apoia em:

- `Properties/Resources.resx`
- `Properties/Resources.en.resx`
- `Properties/Resources.pt-BR.resx`
- `Services/LocalizationService.cs`
- `Services/LocalizedText.cs`
- `Markup/TranslateExtension.cs`
- `ViewModels/ILocalizableViewModel.cs`

`LocalizationService` altera `Thread.CurrentCulture`, `Thread.CurrentUICulture`, `Resources.Culture`, atualiza os ViewModels abertos e notifica a UI.

A troca de tema é tratada por `ThemeManager`, `SettingsViewModel` e os dicionários XAML de tema.

A referência funcional principal da configuração está em [Configuração](help://users/user#configuracao).

## Passo 12. Logs e diagnóstico de suporte

O fluxo atual das métricas de logs está dividido entre `LogService` (escrita), `LogDashboardService` (agregação), `LogsViewModel` (filtros, refresh e drill-down) e `LogsView.xaml` + `LogsView.xaml.cs` (renderização e hover).

Arquivos-chave:

- `Services/LogService.cs`
- `Services/LogDashboardService.cs`
- `Services/AppLogEntry.cs`
- `ViewModels/LogsViewModel.cs`
- `Views/LogsView.xaml`
- `Views/LogsView.xaml.cs`

Restrições developer-only confirmadas pelo código:

- `MainViewModel.PuedeVerLogs` só abre o centro de logs quando `SesionUsuario.EsAdmin` é `true` e o e-mail atual está em `AllowedLogViewerEmails` (`wandica@weg.net`, `maicolj@weg.net`);
- `LogsViewModel` aplica um contexto inicial enviesado para developer apenas quando `LogService.ResolveCurrentAuditUserName()` resolve para `wandica` ou `maicolj`.

Na prática, essas métricas são diagnósticos para desenvolvimento/suporte, não uma tela pensada para usuários finais nem para administradores genéricos.

Fluxo técnico dos dados:

1. `LogService.Write()` persiste 8 colunas separadas por tab: timestamp, nível, máquina, usuário, source, `DurationMs`, mensagem e details.
2. O root primário é derivado de `CC_SHARED_DATABASE_PATH`; se a escrita falhar, o logging cai para `%LocalAppData%\SistemaDeInstalacion\LogsFallback`.
3. Os arquivos ficam em `Logs/<Machine>/<yyyy-MM-dd>/events.log`.
4. `LogDashboardService` lê todos os roots retornados por `LogService.GetReadableLogsDirectories()`, parseia os `.log` em `AppLogEntry` e extrai tokens semânticos de `Details` usando pares `key=value|...`.
5. As métricas semânticas não vêm de `SQLite`; vêm desses detalhes de log, com tokens como `event=validation`, `accepted=false`, `signal=heartbeat`, `dependency=...`, `state=degraded` e `interval_minutes=...`.
6. Logs antigos em espanhol continuam suportados pelo mapeamento de compatibilidade `evento=` -> `event=`.

Métricas implementadas hoje:

1. `GetDashboardSnapshot()` carrega `baseEntries` por máquina + janela temporal e depois aplica filtros de severidade/source/usuário/busca para gerar `filteredEntries`.
2. Os cards superiores saem de `LogDashboardSummary`: eventos visíveis, erros, advertências e latência P95. A latência média também é calculada e reutilizada nas seções narrativas.
3. `BuildExecutiveStatus()` calcula cobertura observável sobre cinco sinais base: `health`, `heartbeat`, `dependencies`, `session` e `validation`.
4. `BuildStatusSections()` monta as seções visíveis `incidents`, `validations`, `latency`, `activity` e `health`, cada uma com fatos, segmentos de timeline e trend cells.
5. `BuildDistribution()` gera os blocos top-6 por source, usuário e máquina a partir de `filteredEntries`.
6. `BuildLatencyDistribution()` agrupa entradas com `DurationMs` em `< 250 ms`, `250-499 ms`, `500-999 ms`, `1-2 s` e `>= 2 s`.
7. `BuildCriticalEvents()` expõe os últimos 8 eventos críticos visíveis (`ERROR`, `WARNING`, `VALIDATION` rejeitada e sinais `HEALTH` degradados/não saudáveis).
8. `InstrumentationStatus` é deliberadamente honesto: quando falta sinal, o dashboard mostra o gap de instrumentação em vez de inventar uptime.

Interação UI / ViewModel / service:

1. `LogsViewModel` monta `LogDashboardQuery`, pede snapshots e aplica os resultados em coleções bindáveis consumidas por `LogsView.xaml`.
2. O modo real-time força a janela `2h`, assina `LogService.LogWritten`, aplica debounce de `150 ms` e mantém auto-refresh a cada `2 s`.
3. Os blocos métricos de source, usuário e máquina são clicáveis e aplicam filtros contextuais; os blocos de latência são apenas hover informativo.
4. `LogsView.xaml.cs` gerencia sessões de hover persistente e delega a composição do hover card para `PreviewMetricHover()`, `PreviewFactHover()` e `PreviewNarrativeSegment()`.
5. O drill-down de incidentes e de narrativa só reorganiza a visão filtrada atual; não altera os logs persistidos.

Observação do estado atual: `LogDashboardSnapshot` ainda expõe `ErrorSeries` e `WarningSeries`, mas o XAML atual já não faz bind direto dessas coleções. O dashboard visível hoje prioriza `StatusSections`, distribuições, hover contextual, timeline de incidentes e a grade filtrada.

Entradas históricas de log continuam sendo dados imutáveis; mudar o idioma da UI não reescreve mensagens já persistidas.

## Build, release e fluxo operacional

Para o fluxo de desenvolvimento, a convenção atual de branches é:

```text
ProgramTranslation -> Homologation -> Produccion
```

Isso significa:

- `ProgramTranslation`: branch ativa de desenvolvimento;
- `Homologation`: branch de validação;
- `Produccion`: branch de liberação.

A aplicação WPF deve ser desenvolvida em `ProgramTranslation`, depois revisada e promovida para `Homologation`, e somente após validação promovida para `Produccion`.

## Recomendações técnicas

- não trabalhe diretamente em `Produccion`;
- evite alterar regras de persistência sem revisar `DatabaseInitializer` e os serviços de banco em conjunto;
- ao localizar uma tela, atualize tanto o XAML quanto as mensagens do ViewModel;
- ao localizar a documentação, crie arquivos markdown por idioma em vez de misturar vários idiomas em um mesmo arquivo;
- trate launcher, aplicação WPF, documentação e banco compartilhado como um único produto coordenado.

## Links de referência

- [WPF data binding](https://learn.microsoft.com/dotnet/desktop/wpf/data/)
- [FlowDocument](https://learn.microsoft.com/dotnet/api/system.windows.documents.flowdocument)
- [Hyperlink em WPF](https://learn.microsoft.com/dotnet/api/system.windows.documents.hyperlink)
- [Microsoft.Data.Sqlite](https://learn.microsoft.com/dotnet/standard/data/sqlite/)
- [SQLite `PRAGMA busy_timeout`](https://www.sqlite.org/pragma.html#pragma_busy_timeout)
- [MSTest](https://learn.microsoft.com/dotnet/core/testing/unit-testing-mstest)
