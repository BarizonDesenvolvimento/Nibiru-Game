# Guia: conectar a Unity ao Claude Code via MCP

Atualizado em 19/10/2026. Unity 6.3 LTS (6000.3.26f1), Windows 11.

O MCP é a "ponte" que deixa o Claude Code conversar com o editor da Unity aberto:
criar objetos na cena, ler o Console, rodar o Play Mode, montar prefabs etc.

## Comparação das opções

| | Unity CLI (`unity mcp`) — oficial | `com.coplaydev.unity-mcp` — comunidade | MCP dentro do AI Assistant (`com.unity.ai.assistant`) — oficial antigo |
|---|---|---|---|
| Quem mantém | Unity Technologies | CoplayDev + comunidade (código aberto, MIT) | Unity Technologies |
| Situação | Ativo; é o substituto oficial | Muito ativo (v10.3.0 em 04/10/2026, ~15 mil estrelas) | **Descontinuado** (a Unity mandou migrar para a CLI) |
| Custo | Grátis, só precisa de conta Unity | Grátis | Precisa de assinatura ou trial do Unity AI e projeto ligado ao Unity Cloud |
| O que instalar | Unity CLI (já instalada nesta máquina) + pacote Unity Pipeline no projeto | Pacote via Git URL + Python 3.10+ + `uv` (o `uv` não está instalado aqui) | Pacote AI Assistant |
| Versões da Unity | 6.0 ou mais nova | 2021.3 até 6.x | 6.0 ou mais nova |
| Peças móveis | 2 (CLI + pacote) | 3 (pacote C# + servidor Python + uv), com versões que precisam bater | 2 |
| Ponto fraco | CLI ainda em beta (1.0.0-beta.13), poucos exemplos da comunidade | Atualizações frequentes, às vezes com mudanças que quebram; depende do Python | Vai deixar de existir |

### Escolha: Unity CLI (oficial)

- É da própria Unity e acompanha as versões do editor, então tende a quebrar menos a cada atualização.
- Não depende de Python nem de `uv`. O Python desta máquina é o 3.14, muito recente, e isso costuma causar problemas com servidores em Python.
- Já está instalada e logada na sua conta (`unity auth status` confirma).
- É grátis e não exige assinatura do Unity AI.
- O MCP antigo dentro do AI Assistant foi descontinuado, então não vale a pena começar por ele.

**Plano B:** se a CLI der problema (ela ainda está em beta), use o CoplayDev. As instruções estão no fim deste guia.

---

## Passo a passo (Unity CLI)

Os comandos são rodados no **PowerShell**, dentro da pasta do projeto
(`C:\Projetos\Jogos\Nibiru\Nibiru`). O Claude Code pode rodá-los por você; peça.

### Passo 1 — Abrir o projeto na Unity

**No editor:** abra o projeto Nibiru pelo Unity Hub e deixe a janela aberta.
A ponte só funciona com o editor aberto.

Recomendado: vá em **Edit > Preferences > General > Interaction Mode** e escolha
**No Throttling**. Sem isso, a Unity "desacelera" quando a janela não está em
foco, e os comandos do Claude demoram para responder.

### Passo 2 — Atualizar a Unity CLI

```powershell
unity self-update
unity --version
```

**No editor:** nada a fazer.

### Passo 3 — Instalar o pacote Unity Pipeline no projeto

```powershell
unity pipeline install
```

**No editor:**
1. Volte para a janela da Unity. Ela vai importar o pacote e recompilar os scripts
   (aparece uma barra de progresso e um ícone girando no canto inferior direito).
   Espere terminar.
2. Abra **Window > General > Console** e confira se não há erros em vermelho.
3. Opcional: em **Window > Package Manager**, aba **In Project**, o pacote
   *Unity Pipeline* deve aparecer na lista.

Confirme no PowerShell:

```powershell
unity pipeline list
```

As colunas **Pipeline** e **Servidor acessível** devem mostrar `true`.

Esse passo altera `Packages/manifest.json` e `Packages/packages-lock.json`. Faça um
commit depois ("Adiciona pacote Unity Pipeline").

### Passo 4 — Registrar o servidor MCP no Claude Code

```powershell
unity mcp configure claude-code --project-path "C:\Projetos\Jogos\Nibiru\Nibiru"
```

Por baixo, isso roda `claude mcp add` e cria um servidor chamado
`unity-editor-mcp` para o seu usuário (vale para todas as sessões do Claude Code).

**No editor:** nada a fazer. Se aparecer um pedido de autorização de conexão na
Unity, clique em aceitar.

### Passo 5 — (Opcional) Instalar a skill da Unity CLI

```powershell
unity skill install claude-code
```

Isso ensina o Claude a usar os comandos da CLI. Outra opção, mais completa, é o
plugin oficial da Unity: no app Claude, **Plugins > Browse**, procure "Unity" e
instale.

### Passo 6 — Reiniciar o Claude Code e testar

1. Feche esta sessão e abra uma **nova sessão** no app Claude (servidores MCP só
   carregam quando a sessão começa).
2. Confira no PowerShell:
   ```powershell
   claude mcp list
   ```
   `unity-editor-mcp` deve aparecer como conectado.
3. Teste pedindo ao Claude: *"Liste os objetos da cena aberta na Unity."*

**No editor:** a Unity precisa estar aberta. Se o Claude criar algo na cena, você
vai ver o objeto aparecer na janela **Hierarchy**.

---

## Problemas comuns

| Sintoma | O que fazer |
|---|---|
| `Servidor acessível: false` | Clique na janela da Unity para ela terminar de compilar e rode `unity pipeline list` de novo. Veja o Console. |
| Claude não vê ferramentas da Unity | Abra uma sessão nova. Rode `claude mcp list`. |
| Respostas lentas | Ajuste **Interaction Mode** para **No Throttling** (Passo 1). |
| Erro depois de atualizar a Unity | `unity pipeline install --force` |
| Diagnóstico geral | `unity doctor` |

---

## Plano B: CoplayDev (`com.coplaydev.unity-mcp`)

Só use se a Unity CLI não funcionar. Não instale os dois ao mesmo tempo.

1. Instale o `uv` (gerenciador de Python): `winget install astral-sh.uv`.
2. **No editor:** **Window > Package Manager > + > Add package from git URL** e cole:
   `https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main`
   (para fixar uma versão, troque `#main` por uma tag, por exemplo `#v10.3.0`).
3. **No editor:** **Window > MCP for Unity**, clique em **Configure All Detected
   Clients** (ou configure só o Claude Code) e confira se o status fica verde.
4. Abra uma nova sessão do Claude Code e teste.

## Fontes

- Unity CLI substitui o MCP do editor: https://docs.unity.com/en-us/unity-cli/replace-mcp-server-unity-cli
- Instalar a Unity CLI: https://docs.unity.com/en-us/unity-cli/use-unity-cli
- Pacote Unity Pipeline: https://docs.unity.com/en-us/unity-cli/unity-pipeline/unity-pipeline-package
- Plugin da Unity para Claude Code: https://docs.unity.com/en-us/ai/unity-plugin/claude-code
- MCP antigo do AI Assistant: https://docs.unity3d.com/Packages/com.unity.ai.assistant@2.7/manual/integration/unity-mcp-get-started.html
- CoplayDev unity-mcp: https://github.com/CoplayDev/unity-mcp
