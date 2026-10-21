# Catálogo no mobile, CRUD completo e menu inferior de 4 opções

- **Slug:** `catalogo-mobile-crud-menu-prioridade`
- **Status:** implementado (2026-10-20)
- **Data:** 2026-10-20
- **Fatias:** A (responsividade do catálogo no mobile + menu inferior permanente), B (excluir grupo e exercício — CRUD completo), C (tabbar de até 4 opções com "Mais" por prioridade)

> Resumo do que saiu e o que divergiu do original: toda a Fatia C e toda a Fatia B foram
> implementadas conforme o plano (com os códigos de 409/404 ajustados ao padrão do projeto —
> ver 4.3 e D10). Na Fatia A, sem ambiente de execução disponível nesta sessão, o diagnóstico
> guiado (3.1) ficou pendente; as correções defensivas (clamp das abas e dropdown do combobox)
> foram aplicadas cobrindo as causas A.1 e A.2, e a validação visual fica para a fase de aceite.
> Descoberta sobre a hipótese A.2 (ver D2): o dropdown do `SearchableSelect` não cobria a
> tabbar — ele ficava **atrás** dela (`z-index: 30` < 60) e perdia as últimas opções quando a
> linha estava perto do rodapé. Corrigido colocando a lista **acima** do menu (`z-index: 130`)
> com `max-height: 240px`, que mantém a tabbar visível em volta.

## 1. Contexto e problema

### 1.1 Fatia A — catálogo quebra no mobile e a tabbar some

O "modo catálogo" tem duas telas:

| Tela | Rota | Papéis |
| --- | --- | --- |
| Catálogo (consulta) | `/exercises` (`src/FitTrack.UI/Pages/Exercises.razor`) | `PersonalTrainer,Admin` |
| Gerenciar catálogo | `/exercises/manage` (`src/FitTrack.UI/Pages/ManageExercises.razor`) | `PersonalTrainer` |

A tabbar (`<nav class="tabbar">` em `src/FitTrack.UI/Layout/NavMenu.razor`, linha 120) renderiza links por bloco `AuthorizeView`, **sem teto**: um usuário com `User` + `PersonalTrainer` + `Admin` chega a **9 abas** (Início, Hoje, Histórico, Séries, Atletas, Catálogo, Usuários, Exercícios, Conta). O CSS (`src/FitTrack.UI/wwwroot/css/app.css`):

```css
.tabbar {                       /* linha 202 */
    position: fixed; inset: auto 0 0;
    display: grid;
    grid-auto-flow: column;
    grid-auto-columns: 1fr;     /* colunas não encolhem abaixo de min-content */
}
.tab-link { min-height: 52px; } /* sem min-width: 0 e sem clamp no <span> do rótulo */
```

Causas prováveis do sintoma "menu inferior sumindo" (confirmar por antes de corrigir — ver 3.1):

1. **Estouro horizontal:** com 7–9 `grid-auto-columns: 1fr`, `1fr` se comporta como `minmax(auto, 1fr)` — colunas param de encolher na largura do rótulo (`Histórico`, `Exercícios`) e o grid passa da viewport; as últimas abas saem da tela.
2. **Sobreposição do combobox:** o dropdown do `SearchableSelect` (`combobox` em `app.css` ~linha 2164, `z-index: 120`) fica **acima** da tabbar (`z-index: 60`), cobrindo o menu enquanto a lista de grupos está aberta em `/exercises/manage`.
3. **Glitch de scroll iOS:** `position: fixed` + `backdrop-filter` pode atrasar a re-renderização da barra durante o scroll em Safari/PWA.

Pontos de responsividade do catálogo que precisam de verificação em viewport ~360×640:

- `/exercises`: `chip-row` de filtros por grupo é `overflow-x: auto` (rolável, ok), mas a lista `entity-row` e a `Pagination` precisam ser conferidas.
- `/exercises/manage`: `manage-grid` é 1 coluna abaixo de 900px e `entity-row` empilha abaixo de 720px — as linhas de edição (nome + grupo + "Ativo" + Salvar) ficam longas; `SearchableSelect` por linha abre dropdown sobre a própria tabbar.

`.app-main` já reserva `padding-bottom: calc(78px + env(safe-area-inset-bottom))` (linha 156) — o conteúdo não é o problema; o problema é a barra em si.

### 1.2 Fatia B — falta excluir no CRUD do catálogo

Estado atual do catálogo (dados → borda):

| Operação | Domínio | Porta out | Use case | Endpoint | UI |
| --- | --- | --- | --- | --- | --- |
| Grupo: criar | `MuscleGroup` | `IMuscleGroupRepository.SaveAsync` | `CreateMuscleGroup` | `POST /muscle-groups` | `ManageExercises.razor` |
| Grupo: editar | — | idem | `UpdateMuscleGroup` | `PUT /muscle-groups/{id}` | edição inline por linha |
| Grupo: excluir | — | — | — | **não existe** | **não existe** |
| Exercício: criar | `Exercise` | `IExerciseRepository.SaveAsync` | `CreateExercise` | `POST /exercises` | `ManageExercises.razor` |
| Exercício: editar | — | idem | `UpdateExercise` (`IsActive`) | `PUT /exercises/{id}` | edição inline por linha |
| Exercício: excluir | — | — | — | **não existe** | **não existe** |

Ou seja: **criar e editar existem** (exercício e grupo); falta **excluir** nos dois, nas quatro camadas (domínio/porta, aplicação, API, UI).

Referências existentes que restringem a exclusão (registros da persistência):

- `WorkoutSeriesWorkoutExerciseRecord.ExerciseId` (`src/FitTrack.Adapters.Out.Persistence/Records/`) — exercício **prescrito** numa série;
- `PerformedSetRecord.ExerciseId` — exercício **executado** numa sessão;
- `ExerciseRecord.MuscleGroupId` — exercício vinculado ao grupo.

Delete como precedência no projeto: `DELETE /sessions/{id}` (cancelar treino) já faz remoção dura em `IWorkoutSessionRepository.RemoveAsync`.

### 1.3 Fatia C — tabbar sem limite e sem prioridade

A ordem de abas é dada pelos blocos `AuthorizeView` em linha, sem fonte única de verdade. Resultado no mobile:

- excede o espaço da tela (causa A.1);
- o usuário admin/pessoal-que-também-admin não vê indicativo de que existem mais seções;
- duplicação de marcação entre sidebar desktop e tabbar mobile (`NavMenu.razor` mantém duas listas à mão).

## 2. Objetivo e valor

- **A:** catálogo 100% utilizável em ~360px de largura, com o menu inferior sempre presente e legível.
- **B:** CRUD completo do catálogo — treinador cria, edita e **exclui** grupos e exercícios, com recusa clara quando o item está em uso (integridade dos treinos prescritos/executados).
- **C:** tabbar nunca passa de 4 opções; funcionalidades extras ficam num painel "Mais", sempre em prioridade **atleta → personal → admin**.

## 3. Fatia A — responsividade e tabbar permanente

### 3.1 Diagnóstico primeiro (não implementar sem evidência)

Passos de confirmação (Aspire + emulação mobile do navegador, 360×640, DPR 2):

1. Reproduzir com conta `User`+`PersonalTrainer`+`Admin` (seeding cria contas de teste; senão, conceder as três roles em `/admin/users`).
2. Capturar: `/exercises` e `/exercises/manage` com e sem dropdown do `SearchableSelect` aberto, rolando a página.
3. Registrar para cada sintoma qual causa de 1.1 confirma (estouro de grid / overlay do combobox / glitch de scroll) — decisões D1/D2 são fechadas com essa evidência.

### 3.2 Correções de escopo

Correção estrutural: **o teto de 4 abas (Fatia C) elimina a causa A.1**. As correções abaixo cobrem o resto e blindam a regressão:

| Correção | Onde |
| --- | --- |
| `min-width: 0` em `.tab-link` + `overflow: hidden; text-overflow: ellipsis; white-space: nowrap` no `span` do rótulo | `app.css` (tabbar) |
| Dropdown do combobox nunca escala sobre 200px de altura em mobile, com scroll interno | `app.css` (combobox) + verificação em `js/combobox.js` |
| Repasse final de página: `/exercises` e `/exercises/manage` a 360px sem rolagem horizontal e com a tabbar visível nas duas | validação por screenshot |
| Se o glitch de scroll iOS se confirmar: trocar `backdrop-filter` por background opaco na tabbar (PWA) | `app.css` |

### 3.3 Critérios de aceite

1. Em 360×640 (o mobile mais apertado), `/exercises` e `/exercises/manage` não geram rolagem horizontal de página.
2. A tabbar permanece visível durante toques no `SearchableSelect`, rolagem e busca do catálogo; com o dropdown aberto o menu continua visível (pode ser encoberto parcialmente, mas nunca some).
3. Rótulos de abas nunca vazam de suas colunas (clamp ativo).
4. Evidências (screenshots antes/depois) anexadas em `docs/images/`.

## 4. Fatia B — excluir exercício e grupo (CRUD completo)

### 4.1 Regras de exclusão (integridade primeiro)

| Caso | Regra |
| --- | --- |
| Exercício prescrito em série **ou** presente em sessão finalizada/aberta | **409** — não exclui |
| Exercício sem nenhuma dessas referências | remover permanentemente (delete duro, como `CancelWorkout`) |
| Grupo com exercícios (ativos ou inativos) | **409** — não exclui |
| Grupo sem exercícios | remover permanentemente |
| Id inexistente | **404** |
| Excluir exercício **não apaga o histórico de execução** — por isso a recusa (D3) | — |
| Soft delete (`IsActive = false`) permanece como alternativa no fluxo: desativar já existe no editar | opcional, não substitui a exclusão |

### 4.2 Aplicação (use cases + portas)

- Novos use cases seguindo o padrão atual:
  - `src/FitTrack.Application/UseCases/DeleteExercise.cs` + porta in `IDeleteExercise`
    `Task ExecuteAsync(DeleteExerciseCommand command, CancellationToken ct)`
  - `src/FitTrack.Application/UseCases/DeleteMuscleGroup.cs` + porta in `IDeleteMuscleGroup`
- Nova porta out **`ICheckCatalogUsage`** (`src/FitTrack.Application/Ports/Out/`), implementada na persistência (`src/FitTrack.Adapters.Out.Persistence/Queries/CatalogUsageQueries.cs`):
  ```csharp
  Task<bool> ExerciseReferencedAsync(ExerciseId id, CancellationToken ct);
  Task<bool> MuscleGroupHasExercisesAsync(MuscleGroupId id, CancellationToken ct);
  ```
  A checagem cruza bounded contexts (planning/execution), por isso não cabe em `IQueryWorkoutSeries` nem em `IQueryUserSessions` isolados.
- `IExerciseRepository` e `IMuscleGroupRepository` ganham `Task RemoveAsync(…, CancellationToken ct)`; implementação no adapter de persistência remove o record (o `UnitOfWork` existente persiste).

### 4.3 Contrato HTTP (`ExerciseEndpoints`/`MuscleGroupEndpoints`)

| Endpoint | Autorização | 204 | 404 | 409 |
| --- | --- | --- | --- | --- |
| `DELETE /exercises/{id:guid}` | `Policies.PersonalTrainerOnly` | removido | `not_found` (padrão do middleware) | `exercise.in_use` |
| `DELETE /muscle-groups/{id:guid}` | `Policies.PersonalTrainerOnly` | removido | `not_found` (padrão do middleware) | `muscle_group.in_use` |

`409` é um ProblemDetails com `title =` código da regra e `detail` em inglês, produzido por
`ConflictException` + `DomainExceptionMiddleware` (D10). A UI traduz `exercise.in_use` /
`muscle_group.in_use` em `ErrorMessages.Describe`.

### 4.4 UI (`ManageExercises.razor`)

- Cada linha de grupo e de exercício ganha dois botões de ação: **Salvar** (já existe) e **Excluir** (novo, estilo `chip--danger`).
- Confirmação em **dois toques na própria linha** (D5): 1º toque muda o rótulo para "Confirmar exclusão?"; 2º toque executa; clique fora/outra ação volta ao estado normal.
- Depois de excluir: recarrega a página corrente (`ReloadAsync`), ajustando `_page` se a página ficou vazia; `_notice` reporta o resultado.
- Erros 409 aparecem via `_error` com a mesma mensagem amigável do backend ("Exercício em uso…").
- `/exercises` (consulta) continua **só leitura** — exclusão fica na tela de gerência (D6).

### 4.5 Testes

| Camada | Arquivo | Casos |
| --- | --- | --- |
| Unit | `tests/FitTrack.Unit/...DeleteExercise...` | recusa quando referenciado (serie prescrito / set executado), recusa de grupo com exercício, caminho feliz, 404 semântico (id inexistente) |
| Integration | `tests/FitTrack.Integration/CatalogFlowTests.cs` | `DELETE` 204 (sem referências), 409 (com prescrição registrada), 409 (grupo em uso), 404; `GET` depois do 204 confirma remoção |
| Fakes | `InMemoryExerciseRepository`/`InMemoryMuscleGroupRepository` + implementação in-memory de `ICheckCatalogUsage` | extensões mínimas |

## 5. Fatia C — tabbar de até 4 opções com "Mais"

### 5.1 Fonte única de navegação

Novo arquivo `src/FitTrack.UI/Layout/NavOptions.cs` (estático e puro, testável):

```csharp
public sealed record NavItem(string Key, string Href, string Label, string Icon,
                             NavGroup Group, string[] Roles);

public enum NavGroup { Athlete = 0, Trainer = 1, Admin = 2 }

public static class NavOptions
{
    /// Itens na ordem de prioridade dos grupos: Atleta → Personal → Admin.
    public static TabResult Tabs(IReadOnlyCollection<string> enabledRoles, bool authenticated);
    /// Tabs visíveis (máx. 4) + overflow ("Mais"), com grupos na ordem dos enum.
}
```

Itens (priority = valor do `NavGroup`):

| Key | Grupo | Roles |
| --- | --- | --- |
| `home` | — (fixo) | sempre |
| `today` | Athlete | `User` |
| `workouts` | Athlete | `User` |
| `series` | Trainer | `PersonalTrainer` |
| `athletes` | Trainer | `PersonalTrainer` |
| `catalog` | Trainer | `PersonalTrainer` |
| `admin-users` | Admin | `Admin` |
| `admin-exercises` | Admin | `Admin`+`PersonalTrainer` (rota já é `PersonalTrainer,Admin`) |
| `account` | — (estrutural, sempre por último) | autenticado |
| `login` / `register` | — | anônimo |

### 5.2 Regras do menu inferior

1. **Máximo de 4 opções** (ações estruturais contam).
2. Slot 1 é sempre **Início**.
3. Os slots seguintes recebem funcionalidades **em ordem de prioridade: atleta → personal → admin**.
4. A **última** opção é **"Mais"** quando ainda resta qualquer funcionalidade fora do teto; painel lista o restante em grupos Atleta → Personal → Admin, com **Conta no fim** (+ Sair).
5. Se tudo couber em ≤4, não há "Mais" (ex.: usuário comum: Início, Hoje, Histórico, Conta).
6. Anônimo: Início, Entrar, Criar conta (sem "Mais").
7. Troca de rota fecha o "Mais"; painel é um sheet nativo-Blazor (overlay + lista), acessível (`aria-expanded`, `Esc` fecha, foco movido no aberto).

Tabela por perfil (conforme o seeding: `trainer@` tem apenas `PersonalTrainer`, sem o papel `User`):

| Perfil | Tabs (≤4) | Painel "Mais" |
| --- | --- | --- |
| Anônimo | Início, Entrar, Criar conta | — |
| `User` | Início, Hoje, Histórico, Conta | — |
| `Admin` só | Início, Usuários, Exercícios, Conta | — |
| `PersonalTrainer` só | Início, Séries, Atletas, **Mais** | Catálogo, Exercícios, Conta, Sair |
| `PersonalTrainer` + `Admin` | Início, Séries, Atletas, **Mais** | Catálogo, Usuários, Exercícios, Conta, Sair |
| `User` + `PersonalTrainer` + `Admin` | Início, Hoje, Histórico, **Mais** | Séries, Atletas, Catálogo, Usuários, Exercícios, Conta, Sair |

Rótulos do painel: **Treino** (atleta) → **Gestão** (personal) → **Administração** (admin) — a
mesma prioridade do requisito, com os nomes das seções que a sidebar já usa.

### 5.3 Implementação

- `NavMenu.razor` monta `_tabs`/`_overflow` a partir de `NavOptions.Tabs(roles, authenticated)`; blocos `AuthorizeView` da tabbar são substituídos pelo resultado (uma lista; `NavLink` com `Match` adequado).
- **Sidebar desktop continua igual visualmente**, agora gerada do mesmo registro (decisão D7). Reuso não pode alterar agrupamento/desktop atual.
- Sem novo pacote (sem sheet JS interop): overlay div + lista, estilo consistente com `combobox`.
- Ícone "Mais": `AppIcon` existente (ex.: `library`/`layers`); rótulo "Mais".

### 5.4 Critérios de aceite

1. Nenhum perfil exibe mais de 4 `NavLink`s na tabbar (tabela 5.2) e nunca há rolagem horizontal dela.
2. "Mais" abre, lista somente itens disponíveis ao perfil em questão, na ordem dos grupos, com Conta no fim; fecha ao navegar ou tocar fora.
3. Tabs ativas destacam como hoje (`NavLink` `active`).
4. Estado do painel é por composição local; nenhuma mudança de rota mantém painel aberto.
5. Testes unitários de `NavOptions.Tabs` cobrem a matriz de perfis da tabela 5.2 (ordem de prioridade + contagem).

## 6. Decisões

| # | Pergunta | Decisão |
| --- | --- | --- |
| D1 | Qual é a causa real do "menu sumindo" no modo catálogo? | Fechada **após** o diagnóstico da Fatia A (dossiê com screenshots); correções 3.2 aplicadas somente às causas confirmadas |
| D2 | Dropdown sobre a tabbar é comportamento aceitável? | Não nas telas de catálogo: dropdown do combobox limitado a `max-height` com scroll para nunca cobrir integralmente a tabbar |
| D3 | Exclusão dura ou soft delete? | **Delete duro com recusa** quando referenciado (mesma semântica de `CancelWorkout`); `IsActive=false` continua disponível no editar |
| D4 | Onde mora a checagem de uso? | Porta out `ICheckCatalogUsage` na aplicação, implementação única na persistência (cruza planning+execution) |
| D5 | Como confirmar exclusão na UI? | Dois toques na própria linha, sem dialog nativo, sem JS interop |
| D6 | Excluir aparece em `/exercises` (consulta)? | Não — gerência exclusivamente em `/exercises/manage` |
| D7 | Reusar o registro na sidebar desktop? | Sim — mesmo registro alimenta as seções Visão geral/Treino/Gestão/Administração da sidebar, sem mudança visual |
| D8 | "Exercícios" do admin vs. "Catálogo" do personal | `/exercises` aparece no grupo Admin e também fica visível para `PersonalTrainer` (a rota já autoriza ambos); key única evita duplicação — Catálogo de gestão continua sendo `/exercises/manage` |
| D9 | Sheet "Mais" | Componente puro do NavMenu (overlay + lista), fecha por rota/clique fora; acessível (`aria-expanded`, `role="dialog"`) |
| D10 | Como o 409 chega à borda? | Nova `ConflictException(code, message)` em `FitTrack.Application/Exceptions`, mapeada no `DomainExceptionMiddleware` para 409 com `title = code` |

## 7. Verificação e ordem de entrega

Sequência recomendada (a Fatia C destrói a causa estrutural de A antes do acabamento):

1. **A1** — diagnóstico guiado (3.1) com evidências anexadas.
2. **C** — `NavOptions` + testes + tabbar/sheet; regressão visual desktop.
3. **A2** — acabamento de CSS da Fatia A (3.2) revalidando fullscreen mobile nas telas de catálogo.
4. **B** — exclusão nas 4 camadas + testes unit/integration + README e `docs/` atualizados ao fechar (padrão `readme-update`).

Validação transversal em cada fatia: `dotnet build`/`dotnet test` verdes, navegação manual em 360×640 nas duas telas do catálogo, e contagem/ordem de abas batendo com a tabela 5.2.
