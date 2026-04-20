# Sistema

`SistemaDeInstalacion` é uma aplicação desktop orientada a centralizar executáveis corporativos, controlar acesso por função e administrar quais usuários podem visualizar ou executar cada aplicativo.

Seu propósito é reduzir a dispersão de instaladores, organizar a operação diária e deixar rotas claras para usuários finais, administradores e desenvolvedores.

## Propósito da aplicação

A aplicação existe para resolver estas necessidades:

1. centralizar executáveis autorizados em um único catálogo;
2. controlar permissões por usuário e por função;
3. separar o acesso administrativo do acesso operacional normal;
4. manter a continuidade funcional e documental do sistema.

## Escopo funcional atual

Hoje o sistema cobre:

- acesso pelo atalho e abertura da aplicação;
- login normal de usuário;
- login administrativo;
- registro de usuário;
- registro de administrador;
- recuperação de senha;
- catálogo de instaladores;
- gestão de usuários;
- atribuição de aplicativos;
- documentação interna por perfil;
- configurações visuais e troca de tema, com a referência funcional centralizada em [User, Configuração](help://users/user#configuracao);
- logs funcionais e de suporte.

## Mapa geral do produto

```text
SistemaDeInstalacion
|
+- Abertura pelo atalho
|  |
|  \- Tela inicial
|      |
|      +-> Registro normal
|      +-> Login normal
|      +-> Registro administrativo
|      \-> Login administrativo
|
+- Operação do usuário
|  +- Catálogo filtrado
|  +- Ajuda
|  \- Execução de aplicativos atribuídos
|
\- Operação administrativa
   +- Catálogo completo
   +- Gestão de usuários
   +- Atribuição de aplicativos
   +- Ajuda
   \- Logs restritos
```

## Perfis de uso

### User

O usuário final abre a aplicação pelo atalho, pode se registrar se ainda não tiver conta, entrar pelo login normal, revisar seus instaladores atribuídos e executar os aplicativos visíveis para sua conta.

### Administradores

O administrador abre a aplicação pelo mesmo atalho, mas opera pela rota administrativa. A partir daí pode se registrar como administrador, entrar com acesso admin, gerenciar usuários, gerenciar instaladores e atribuir aplicativos.

### Developer

O desenvolvedor mantém a aplicação WPF, a lógica, a persistência, o empacotamento, os logs e a documentação técnica. Sua guia é mais técnica e orientada a código.

## Rota funcional recomendada

Para a operação funcional, a ordem correta é esta:

```text
Atalho
        |
        v
Tela inicial
        |
        +--> Registro
        |
        \--> Entrar
                 |
                 +--> Usuário final
                 \--> Administrador
```

Essa ordem é importante porque evita começar diretamente pelos módulos internos sem antes explicar como uma pessoa realmente entra no sistema.

## Configuração

A opção `Configuração` concentra os ajustes visuais e de preferência da aplicação, especialmente as mudanças de tema disponíveis para a experiência de uso.

A referência funcional completa e vigente permanece em [User, Configuração](help://users/user#configuracao).

## Estrutura documental oficial

A documentação se distribui assim:

| Documento canônico | Documento | Uso principal |
|---|---|---|
| `help://sistema` | `Sistema` | visão geral do produto |
| `help://users/user` | `User` | guia operacional para usuários finais |
| `help://administradores/administradores` | `Administradores` | guia operacional para administração |
| `help://developers/developer` | `Developer` | guia técnica de continuidade para desenvolvimento |
| `help://developers/base-de-datos` | `BaseDeDatos` | estrutura e operação da persistência |

## Ponto de partida recomendado

Se você vai operar o sistema:

- comece pelo atalho e pela tela inicial;
- depois revise o registro, caso ainda não tenha conta;
- em seguida revise a rota de login correspondente ao seu perfil.

De acordo com a função:

- consulte [User](help://users/user) se o seu perfil é usuário final;
- consulte [Administradores](help://administradores/administradores) se o seu perfil é administrativo.

Se você vai manter ou evoluir o produto:

- comece por [Developer](help://developers/developer);
- continue com [BaseDeDatos](help://developers/base-de-datos).

## Links complementares

- [Guia de usuário](help://users/user)
- [Guia de administradores](help://administradores/administradores)
- [Guia técnica para desenvolvedores](help://developers/developer)
- [Guia de banco de dados](help://developers/base-de-datos)
