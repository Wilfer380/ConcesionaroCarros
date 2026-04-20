# Administradores

Este guia explica o percurso funcional do administrador dentro do sistema. Diferentemente do usuário final, o administrador pode entrar por duas rotas distintas:

- pelo login normal, quando precisa apenas operar como usuário padrão;
- pelo login administrativo, quando precisa usar funções de gestão.

O objetivo deste guia é deixar claras as duas rotas desde o início, diferenciando o que o administrador compartilha com o usuário normal e o que pertence somente ao modo administrativo.

## Explicação principal

O administrador nem sempre trabalha em modo administrativo. Também pode abrir a aplicação, registrar-se, entrar e operar como um usuário normal se entrar pela rota padrão.

Isso significa que existem dois cenários diferentes:

1. **Administrador pelo login normal**  
   O administrador entra como qualquer usuário final. Nesse modo pode ver os aplicativos atribuídos, consultar ajuda e encerrar a sessão, mas não acessa módulos de gestão.

2. **Administrador pelo login administrativo**  
   O administrador entra pela rota especial de administração. Nesse modo, módulos como `Gestão de Usuários`, administração de instaladores, atribuição de aplicativos e, quando permitido, o módulo de logs ficam habilitados.

## Objetivo deste guia

Com este guia o administrador poderá:

- entender quando usar o login normal e quando usar o login admin;
- seguir corretamente o processo a partir do atalho;
- registrar-se se ainda não existir uma conta administrativa;
- entrar pela rota correta de acordo com a tarefa;
- gerenciar instaladores;
- gerenciar usuários;
- atribuir aplicativos;
- consultar ajuda e suporte;
- validar as alterações funcionais realizadas.

## O que o administrador deve entender primeiro

Antes do percurso visual, o administrador deve ter claras estas regras:

1. o acesso sempre começa a partir do mesmo atalho principal do sistema;
2. entrar pelo login normal não ativa permissões administrativas, mesmo que a pessoa tenha papel administrativo no sistema;
3. se a conta administrativa ainda não existir, primeiro deve ser feito o registro pela rota administrativa;
4. somente depois de um login admin correto os módulos de gestão são habilitados;
5. toda alteração administrativa afeta outros usuários e deve ser revisada com cuidado.

## Fluxo administrativo geral

```text
Atalho
        |
        v
Tela inicial
        |
        +--> Login normal
        |        |
        |        v
        |   Operação como usuário padrão
        |
        +--> Registro administrativo
        |        |
        |        v
        |   Retorno ao login admin
        |
        \--> Login administrativo
                 |
                 v
        Vista principal admin
                 |
                 +--> Instaladores
                 +--> Gestão de Usuários
                 +--> Ajuda
                 \--> Logs conforme permissões
```

## Passo 1. Abrir a aplicação pelo atalho

O administrador inicia a partir do mesmo atalho principal do sistema. A partir dessa tela pode decidir se continuará pela rota de usuário padrão ou pela rota administrativa.

![Acesso pelo atalho para administradores](image-1.png)

## Passo 2. Diferença entre login normal e login admin

Esta é uma das regras mais importantes do sistema:

- o login normal é para operação padrão, onde os usuários entram como usuários operacionais normais, ainda que a mesma pessoa também tenha papel administrativo;
- o login administrativo ativa permissões que não existem na rota de usuários normais;
- um usuário com papel administrativo não obtém acesso completo se entrar pelo login normal;
- para ver a vista principal admin e as demais funções de administração, a pessoa deve entrar pela rota admin.

## Passo 2.1. Acesso pelo login de usuário

Se o administrador entrar pela janela normal, o fluxo de acesso, registro, recuperação e entrada é o mesmo do guia de usuário.

### Resultado do login normal para um administrador

Depois de entrar pelo login normal:

- verá a mesma vista geral do usuário padrão;
- não verá módulos de gestão;
- não verá o módulo de logs;
- sua experiência será equivalente à descrita no guia `User`.

[Abrir o guia de usuário a partir do atalho](help://users/user#passo-1-abrir-a-aplicacao)

## Rota 2. Administrador operando pelo login administrativo

Esta é a rota que o administrador deve usar quando precisa fazer alterações funcionais dentro do sistema. Para continuar com o processo, primeiro deve entrar pelo login admin e depois seguir com registro ou entrada, se necessário.

## Passo 3. Registro administrativo

Se a conta administrativa ainda não existir, primeiro deve ser criada a partir da rota admin usando a opção de registro.

### Passo 3.1. Abrir a opção Registrar-se no login admin

Na tela de login administrativo, clique na opção `Registrar-se`.

![Acesso ao registro administrativo](image-4.png)

### Passo 3.2. Completar o formulário de registro administrativo

No registro administrativo devem ser preenchidos:

- e-mail corporativo;
- função administrativa;
- senha normal;
- senha administrativa.

Este registro não é igual ao normal. Aqui o sistema cria ou atualiza o usuário base e também registra os dados administrativos necessários para operar em modo admin.

### Passo 3.3. Salvar o registro administrativo

Clique no botão de registro e aguarde a confirmação.

Se o processo for correto:

- o usuário base será criado ou atualizado;
- a conta administrativa será registrada;
- o sistema retornará ao login admin com os dados prontos para entrar.

![Confirmação do registro administrativo](image-5.png)

## Passo 4. Entrar pelo login administrativo

Depois de abrir o atalho e, se necessário, concluir o registro, o próximo passo é entrar pelo login admin.

### Passo 4.1. Abrir o login administrativo

Na tela inicial, entre na opção de acesso administrativo.

### Passo 4.2. Informar o usuário administrativo

Nesse login você deve usar o `UsuarioSistema` ou identificador administrativo definido para a conta.

### Passo 4.3. Informar a senha administrativa

Digite a senha administrativa correspondente. Essa senha não é validada da mesma forma que a senha do login normal.

### Passo 4.4. Confirmar o acesso

Clique no botão de acesso. Se tudo estiver correto, o sistema abrirá a vista principal com o modo administrador ativo.

![Login administrativo correto](image-6.png)

## Passo 5. Validações do login administrativo

O sistema diferencia melhor os erros do login admin.

### Quando o usuário administrativo não existe

O sistema indicará que o usuário administrativo não está registrado.

### Quando a senha administrativa está incorreta

O sistema indicará que a senha administrativa está incorreta.

![Erros de validação do login administrativo](image-7.png)

## Passo 6. Vista principal do administrador

Quando o login admin é correto, a aplicação entra em modo administrador.

Normalmente o administrador verá:

- painel lateral;
- módulo `Instaladores`;
- módulo `Gestão de Usuários`;
- opção `Ajuda`;
- opção `Configuração` para revisar ou alterar o tema visual;
- opção `Logs` somente se o perfil cumprir a validação especial de suporte;
- opção `Encerrar sessão`.

![Vista principal do administrador](image-8.png)

## Passo 7. Gestão de instaladores

A partir deste módulo é controlado o catálogo de executáveis do sistema.

[Abrir o guia de usuário a partir de "O que acontece quando o login é correto"](help://users/user#o-que-acontece-quando-o-login-e-correto)

### Como adicionar um instalador

1. entre no módulo `Buscar Instaladores`;
2. abra o formulário de novo instalador;
3. selecione o executável;
4. complete nome, descrição e categoria;
5. salve o registro.

![Módulo de instaladores](image-9.png)

![Formulário de novo instalador](image-10.png)

![Confirmação de instalador salvo](image-10.png)

### Como editar ou excluir um instalador

Para editar:

1. localize o cartão correto;
2. clique no ícone do lápis, que é `Editar`;
3. atualize as informações;
4. salve as alterações.

Para excluir:

1. localize novamente o cartão correto;
2. clique no ícone da lixeira, que é `Excluir`;
3. confirme a ação e remova o registro por completo.

![Editar ou excluir instalador](image-11.png)

### Como instalar um aplicativo registrado

Quando o instalador já está registrado, o administrador também pode testar o processo de instalação a partir do cartão correspondente.

1. localize o cartão do aplicativo que deseja instalar;
2. clique no botão `Instalar`;
3. aguarde até que o Windows abra a janela de execução ou instalação;
4. confirme a execução se o Windows solicitar;
5. continue o assistente de instalação até o final;
6. valide que a aplicação foi aberta corretamente ao terminar.

[Abrir o processo de instalação no guia de usuário](help://users/user#como-executar-um-aplicativo-atribuido)

## Passo 8. Gestão de usuários

Este módulo está disponível somente em modo administrador. A partir dele são administradas contas e permissões.

![Módulo de gestão de usuários](image-12.png)

### Como adicionar um usuário

1. abra `Gestão de Usuários`;
2. clique na opção para adicionar um usuário;
3. complete nomes, sobrenomes, e-mail, telefone, senha e função;
4. salve as informações.

![Formulário de novo usuário](image-13.png)

### Como editar ou excluir um usuário

Para editar:

1. localize o registro;
2. clique em `Editar`;
3. atualize as informações;
4. salve.

Para excluir:

1. localize o registro;
2. clique em `Excluir`;
3. confirme a ação.

![Editar ou excluir usuário](image-14.png)

## Passo 9. Atribuição de aplicativos

Dentro de `Gestão de Usuários`, o administrador pode atribuir ou retirar aplicativos de uma pessoa.

O fluxo geral é:

1. selecionar o usuário;
2. abrir o painel de atribuição;
3. marcar ou desmarcar aplicativos;
4. salvar a atribuição.

![Painel de atribuição de aplicativos](image-15.png)

## Passo 10. Uso da guia Ajuda

A guia `Ajuda` permite ao administrador consultar a documentação funcional dentro do sistema, sem sair da aplicação.

A partir desta vista o administrador normalmente pode:

- navegar entre pastas de documentação;
- abrir guias administrativas e funcionais;
- revisar informações complementares;
- localizar o canal de suporte disponível.

### Passo 10.1. Abrir a guia Ajuda

No painel lateral, clique na opção `Ajuda`.

![Acesso à ajuda administrativa](image-16.png)

### Passo 10.2. Revisar as pastas e os documentos disponíveis

Na vista de ajuda, o administrador verá as pastas e os documentos disponíveis para o perfil.

### Passo 10.3. Abrir um documento e consultar seu conteúdo

Selecione o documento necessário e revise a informação no painel principal.

![Conteúdo da ajuda administrativa](image-18.png)

## Passo 11. Configuração

A tela `Configuração` permite ao administrador ajustar as preferências visuais da aplicação sem sair do sistema.

A referência funcional canônica desta tela permanece em [User, Configuração](help://users/user#configuracao), porque o percurso e a troca de tema são compartilhados entre perfis.

## Passo 12. Encerrar sessão

Quando o administrador terminar:

1. deve retornar ao painel lateral;
2. clicar em `Encerrar sessão`;
3. confirmar o encerramento se necessário;
4. deixar o sistema na tela inicial.

## O que revisar depois de uma alteração importante

Depois de adicionar, editar, excluir ou atribuir, valide:

- que o usuário apareça corretamente;
- que o instalador continue visível no catálogo;
- que o usuário possa entrar;
- que o catálogo filtrado corresponda à atribuição realizada.

## Casos frequentes de suporte

### O usuário não vê seu aplicativo

Revise:

1. se o instalador existe;
2. se o usuário tem atribuições;
3. se o caminho do executável continua válido.

### O executável não abre

Revise:

1. se o arquivo existe no caminho;
2. se não foi movido;
3. se o Windows não está bloqueando;
4. se o usuário tem permissões sobre aquela localização.

### Gestão de Usuários não aparece

Revise:

1. se o acesso foi feito pelo login admin;
2. se a senha usada foi a administrativa;
3. se o modo administrador está ativo.

## Boas práticas para administração

- use nomes claros nos instaladores;
- valide caminhos antes de salvar;
- evite contas duplicadas;
- confirme atribuições depois de cada alteração;
- diferencie sempre login normal de login admin.

## Contato e apoio

Se surgir uma dúvida funcional ou técnica, utilize o centro de ajuda interno do sistema e o canal de suporte definido pela organização.
