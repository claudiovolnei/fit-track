# Responsividade do histórico e ajuste por série no exercício

- **Slug:** `responsividade-historico-e-ajuste-exercicio`
- **Status:** proposto (2026-10-24)
- **Data:** 2026-10-24
- **Origem:** relato de uso — volume feito muito alto no histórico; descrição longa de
  exercício quebra a linha do valor `séries × repetições · carga`; tela do exercício
  não deixa claro que dá para ajustar série a série, e não mostra o histórico do
  exercício quando a carga muda.
- **Feature anterior relacionada:** `historico-volume-por-exercicio`,
  `historico-sets-recolhidos-comentario`, `criar-plano` (sub-requisito D: ajuste de
  carga/repetições na finalização).

## 1. Contexto e problema

### 1.1 Histórico (`/workouts`)

A tela de histórico (`MyWorkouts.razor`) + `SessionCard` + `SessionSetList` acumulam
informação vertical:

- o card de resumo da página tem 4 estatísticas, e a de **"Volume feito"** carrega a
  linha `Prescrito … · Feito … · média por exercício` dentro do próprio valor, o que
  estica o card;
- cada `SessionCard` mostra, além do cabeçalho, o `summary-strip`, a lista de séries
  (com grupos recolhíveis) e o bloco `.volume-comparison` + `.volume-exercises`, que
  lista **um item por exercício** da sessão. Em sessões com muitos exercícios o painel
  fica muito alto;
- em `SessionSetList`, o cabeçalho do grupo (`N × reps · carga kg`) e a linha
  individual (`número exercício`) compartilham uma linha com `justify-content:
  space-between`. Quando o nome do exercício é longo, o valor à direita quebra linha
  ou empurra o layout.

### 1.2 Tela do exercício (`/today`)

Na tela Hoje (`Today.razor`) o atleta ajusta a carga e as repetições de um exercício
prescrito por meio de `StartEditing`/`StopEditing`, mas:

- o ajuste é **único por exercício** (`_reps`, `_loads`), aplicado a **todas** as
  séries da prescrição (`prescribed.Sets`). Não há como dizer "série 1 com 18 kg,
  série 2 com 20 kg…";
- a UI não deixa claro que o valor prescrito é o ponto de partida e que pode ser
  **aumentado ou diminuído** série a série;
- não há histórico do exercício quando a carga muda, nem indicação do "último peso"
  para o atleta seguir a progressão;
- o item combina nome, esforço prescrito, pills de séries realizadas e campos de
  ajuste numa estrutura que quebra mal em telas estreitas.

## 2. Objetivo e valor

- **Histórico:** painel mais compacto, com altura controlada mesmo em sessões com
  muitos exercícios, e valor `séries × reps · carga` sempre numa linha estável.
- **Exercício:** permitir ajuste **por série**, com o prescrito como padrão, deixando
  claro que pode mudar; mostrar o histórico do exercício quando a carga muda; manter a
  última atualização como referência para a próxima; organizar carga e série numa
  linha (ou bloco) previsível no mobile.
- **Time:** manter o histórico por série (já suportado pelo storage) e o contrato de
  envio em quilos.

## 3. Atores envolvidos

- **Atleta** — lê o histórico, ajusta o esforço série a série, consulta o histórico
  do exercício.
- **Personal trainer** — prescreve a série/carga de referência (inalterado).
- **Sistema (API)** — persiste as séries realizadas individualmente; fornece o
  histórico recente do exercício.

## 4. Escopo

### 4.1 Incluído

1. Compactação do painel de histórico (volume por exercício colapsável; estatística
   "Volume feito" mais curta).
2. Estabilidade da linha `séries × reps · carga` com nomes longos.
3. Ajuste por série na tela Hoje (per-set reps/carga), com padrão = prescrito.
4. Histórico do exercício quando a carga muda (últimas realizações) e indicação do
   "último peso".
5. Reorganização do item do exercício (carga e série numa linha/bloco).

### 4.2 Não incluído

- Mudança na prescrição do personal (continua uma carga/reps por exercício).
- Novo endpoint de "progressão sugerida" automática — apenas leitura do histórico.
- Alteração do contrato HTTP de carga (continua em kg; conversão kg↔barra fica no
  cliente, já entregue em `criar-plano` D).
- Reescrita do agrupamento de séries no histórico (continua colapsando iguais).

## 5. Sub-requisito A — Responsividade do histórico

### 5.1 Volume feito (card de resumo)

**Problema:** a linha `Prescrito … · Feito … · média por exercício` dentro de
`stat-value` estica o card.

**Proposta:**

1. Mover o detalhe para uma linha muted **abaixo** do valor principal, com
   `flex-wrap` e fonte 0.72rem, em vez de dentro do `<small>` do valor.
2. Encurtar o texto: `Prescrito X · Feito Y · média/exercício` (remover "por").
3. Reduzir o padding do `.stat` em mobile (`@media (max-width: 759px)`: 12px) e o
   gap do `.stat-grid` para 8px.
4. (Opcional, decisão D1) em telas < 420px, empilhar os 4 cards em 1 coluna em vez
   de 2, para que "Volume feito" tenha largura total.

**Arquivos:** `MyWorkouts.razor` (bloco `_summary`), `app.css` (`.stat`, `.stat-grid`,
`.stat-value small`).

### 5.2 Painel alto por exercício (`volume-exercises`)

**Problema:** `.volume-exercises` lista um item por exercício de cada sessão,
aumentando muito a altura.

**Proposta:**

1. Colapsar a lista por padrão atrás de um botão
   `Ver por exercício (N) ▾` dentro de `SessionCard`, expandindo em
   `ExpandedKeys`-like estado local do card (ou reusar `_expandedGroups` com chave
   `volume:{sessionId}`).
2. Quando colapsado, mostrar apenas a linha resumida `Feito (média) X · Prescrito Y ·
   Aumento Z` (já existe em `.volume-comparison`).
3. Quando expandido, renderizar `.volume-exercises` como hoje.
4. Adicionar `max-height`/scroll? Não — colapso é suficiente e mais simples.

**Arquivos:** `SessionCard.razor`, `MyWorkouts.razor` (passar estado de expansão).

### 5.3 Linha `séries × reps · carga` com nome longo

**Problema:** nome longo quebra o valor.

**Proposta (CSS):**

```css
.session-set-row {
    display: flex;
    align-items: center;
    gap: 12px;
}
.session-set-row > span:first-child {
    min-width: 0;
    flex: 1 1 auto;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}
.session-set-row > span:last-child,
.set-group-toggle > span:last-child {
    flex: 0 0 auto;
    white-space: nowrap;
}
```

- Adicionar `title="@ExerciseName"` no nome para tooltip com o nome completo.
- Em telas < 420px, permitir que a linha quebre, mas mantendo o valor numa linha
  própria: `flex-wrap: wrap` com o valor `flex-basis: 100%`? Decisão D2. Recomendação:
  manter `nowrap` no valor e truncar o nome; se o nome for essencial, mostrar tooltip.

**Arquivos:** `SessionSetList.razor`, `app.css`.

### 5.4 Critérios de aceite (A)

1. Em 360×780, o card de "Volume feito" não empurra o `.stat-grid` para além da
   viewport (sem scroll horizontal).
2. Com 1 sessão de 6 exercícios, a altura do `SessionCard` colapsado (volume por
   exercício fechado) é ≤ 1,5× a altura atual sem a lista.
3. Nome de exercício de 40+ caracteres não quebra a linha do valor
   `N × reps · carga kg`; o nome é truncado com reticências e tooltip.
4. Desktop (≥1024px) sem regressão visual do histórico.

## 6. Sub-requisito B — Ajuste por série na tela do exercício

### 6.1 Modelo de dados (mudança de contrato)

Hoje `PerformedExerciseRequest(ExerciseId, Repetitions, LoadKilograms)` + `Sets`
da prescrição. Para suportar variação por série:

**Novo contrato (proposto):**

```csharp
public sealed record PerformedSetRequest(int Repetitions, decimal LoadKilograms);
public sealed record PerformedExerciseRequest(
    Guid ExerciseId,
    IReadOnlyList<PerformedSetRequest> Sets);
```

- O número de entradas em `Sets` deve ser **exatamente** o `prescribed.Sets` (o
  atleta ajusta o esforço de cada série, não a quantidade de séries).
- Validação de reps (1–100) e carga (≥ 0) por série, no use case.
- Domínio: `PerformedExercise` passa a carregar `IReadOnlyList<(Repetitions, Load)>`
  em vez de `Repetitions + Load + Sets`. `WorkoutSession.Complete` registra uma
  `PerformedSet` por entrada (em vez de repetir N vezes o mesmo valor).
- Manter `PerformedExercise.MinSets` para o caso de prescrição vazia (1 série).

**Arquivos:** `WorkoutSeriesRequests.cs` (UI), `WorkoutExecutionRequests.cs` (API),
`PerformedExerciseInput.cs`, `FinishWorkoutCommand`, `FinishWorkout.cs`,
`PerformedExercise.cs`, `WorkoutSession.Complete`, mapeamentos e testes.

### 6.2 UI — edição por série

**Comportamento desejado:**

1. O item do exercício mostra a prescrição como baseline:
   `4 × 20 @ 20 kg` (em destaque) + pills das séries realizadas (quando finalizado).
2. Um botão/link "Ajustar" (já existe: `effort-link`) abre um bloco `.set-log`
   com:
   - checkbox de conclusão (já existe);
   - opção **"Manter prescrição"** (padrão): um único par reps/carga aplicado a
     todas as séries (comportamento atual, rápido);
   - opção **"Ajustar por série"**: expande N linhas, uma por série, cada uma com
     reps + carga (com toggle kg/barra por série ou global — decisão D3), pré-preenchidas
     com o prescrito.
   - botão "Pronto" fecha.
3. Quando alguma série difere da prescrição, o item exibe as séries realizadas
   individualmente (`1×18 · 2×20 · 3×20 · 4×22`) em vez de `4 × 20 @ 20 kg`.
4. O payload envia a lista de séries.

**Organização (carga e série numa linha):**

- Linha 1: nome do exercício (esquerda) + prescrição `4 × 20 @ 20 kg` (direita,
  `white-space: nowrap`).
- Linha 2: pills de séries realizadas (wrap).
- Linha 3 (ao editar): bloco de ajuste, com as séries empilhadas, cada uma numa
  linha `Série N: [reps] × [carga] [kg|barra]`.

**CSS:** reusar `.compare-row`, `.compare-exercise`, `.set-pills`, `.set-log`;
adicionar `.set-log__series` (grid 1 coluna em mobile, 2 em ≥760px).

**Arquivos:** `Today.razor`, `app.css`.

### 6.3 Histórico do exercício quando a carga muda

**Problema:** o atleta muda a carga e não vê onde estava.

**Proposta:**

1. Novo endpoint `GET /exercises/{exerciseId}/history?limit=5` (atleta logado) que
   retorna as últimas sessões finalizadas do atleta em que o exercício apareceu,
   com as séries realizadas (reps + carga) e a data. Reusar
   `IUserSessionQueries`/`WorkoutSessionProjection`.
2. Na UI, ao abrir o ajuste de um exercício, se o valor digitado diferir do
   prescrito (para mais ou para menos), mostrar um painel colapsável
   "Histórico deste exercício" listando as últimas realizações:
   `20/10 · 4 × 20 @ 20 kg`, `18/10 · 3 × 18 @ 18 kg`, …
3. Mostrar sempre o "último peso": `Último: 20 kg (20/10)` acima do campo de carga,
   como referência. Se houver histórico, pré-preencher a carga com o último valor
   usado em vez do prescrito (decisão D4). Se não houver, usar o prescrito.
4. O painel é carregado sob demanda (primeira abertura do ajuste) e cacheado em
   memória por exercício para a sessão.

**Arquivos:** novo use case `GetExerciseHistory`, endpoint em `SessionEndpoints` ou
`ExerciseEndpoints`, `FitTrackApiClient`, `Today.razor`.

### 6.4 Critérios de aceite (B)

1. Dado um exercício prescrito com 4 × 20 @ 20 kg, quando o atleta abre o ajuste,
   então as 4 séries aparecem com 20 reps e 20 kg por padrão (opção "Manter
   prescrição" ativa).
2. Dado o atleta escolhendo "Ajustar por série" e mudando a série 1 para 18 kg e a
   série 4 para 22 kg, então o payload enviado tem 4 entradas:
   `[18, 20, 20, 22]` kg (com reps respectivas).
3. Dado o atleta finalizando com variação, o histórico exibe
   `1×18 · 2×20 · 3×20 · 4×22` em vez de `4 × 20 @ 20 kg`.
4. Dado o atleta mudando a carga (para mais ou para menos), o painel "Histórico
   deste exercício" aparece com as últimas realizações; o "Último peso" é mostrado.
5. Sem histórico anterior, o campo inicia com o prescrito e não mostra histórico.
6. A carga continua sendo enviada em kg (conversão kg↔barra no cliente, já
   entregue).

## 7. Modelo de dados

- **Domínio:** `PerformedExercise` deixa de ter `Repetitions/Load/Sets` únicos e
  passa a carregar `IReadOnlyList<PerformedSetDetail>` (reps + carga). `Complete`
  registra um `PerformedSet` por entrada. Sem migração de banco (o storage já é por
  set).
- **API:** `PerformedExerciseRequest` muda para lista de sets. Breaking change
  interno (cliente e servidor no mesmo deploy).
- **UI:** `PerformedExerciseRequest` (em `WorkoutSeriesRequests.cs`) acompanha.
- **Queries:** novo `GetExerciseHistory` reusando `WorkoutSessionProjection`.

## 8. Impacto arquitetural

| Projeto | Impacto |
| --- | --- |
| `FitTrack.Domain` | `PerformedExercise` muda de shape; `WorkoutSession.Complete` registra sets variados. |
| `FitTrack.Application` | `PerformedExerciseInput` vira lista de sets; validação por set; novo use case `GetExerciseHistory`; `FinishWorkout.BuildPerformedExercises` adapta. |
| `FitTrack.Adapters.In.Api` | `PerformedExerciseRequest` (contrato) muda; novo endpoint de histórico do exercício. |
| `FitTrack.Adapters.Out.Persistence` | Query de histórico por exercício (reuso do projection). |
| `FitTrack.UI` | `Today.razor` (edição por série + histórico); `MyWorkouts.razor`/`SessionCard`/`SessionSetList` (compactação); `app.css`. |
| Testes | Unitários (BuildPerformedExercises com sets variados), integração (finish com sets variados; histórico do exercício), UI (colapso, truncamento). |

## 9. Plano de testes

### Unitários
- `FinishWorkout`: prescrição 4×20; envio com sets `[18,20,20,22]` registra 4
  sets com cargas respectivas; envio com set fora de range (reps 0 ou 101) lança
  `set.repetitions.out_of_range`; exercício não prescrito lança
  `session.set.exercise.not_prescribed`.
- `SessionSetGroups.Group`: sets variados não colapsam (cada um vira linha).

### Integração
- Finish com sets variados persiste 4 `PerformedSetRecord` com cargas distintas;
  `GET /sessions` retorna as 4 séries; agrupamento no histórico mostra
  `1×18 · 2×20 · 3×20 · 4×22`.
- `GET /exercises/{id}/history` retorna as últimas sessões do atleta com o exercício.

### UI
- Histórico: card colapsado com 6 exercícios; nome longo não quebra o valor.
- Hoje: edição por série envia lista correta; histórico aparece ao mudar carga.

## 10. Riscos e dependências

- **Breaking change de contrato:** cliente e servidor precisam ser deployados juntos;
  versões antigas do cliente falham ao finalizar. Mitigar com deploy atômico (já é
  o padrão).
- **Altura do painel:** colapsar por exercício esconde informação; garantir que o
  resumo (`Feito/Puscrito/Aumento`) permaneça visível.
- **Performance do histórico do exercício:** limitar a 5 sessões; índice em
  `performed_sets(exercise_id)` pode ser necessário (decisão D5).
- **Default "último peso":** pode confundir se o atleta regrediu de carga; sempre
  mostrar a data e permitir voltar ao prescrito (botão "Usar prescrito").

## 11. Decisões em aberto

| ID | Decisão | Recomendação |
| --- | --- | --- |
| **D1** | Empilhar os 4 cards em 1 coluna em < 420px? | Não por padrão; manter 2 colunas com padding reduzido. Reavaliar com medição. |
| **D2** | Nome longo: truncar sempre ou quebrar em telas < 420px? | Truncar sempre + tooltip; quebra pode desalinhar o valor. |
| **D3** | Toggle kg/barra por série ou global no bloco de ajuste? | Global por exercício (reusar `_loadUnits` por exercício), aplicado a todas as séries. |
| **D4** | Pré-preencher carga com o último valor usado ou com o prescrito? | Último valor usado se existir histórico; botão "Usar prescrito" restaura. |
| **D5** | Criar índice em `performed_sets(exercise_id, session_id)`? | Avaliar volume; começar sem índice, medir. |
| **D6** | Colapsar volume por exercício por padrão ou expandido? | Colapsado por padrão; o resumo permanece visível. |

## 12. Sugestão de próximo passo

1. Arquiteto: validar o novo contrato `PerformedExerciseRequest` (lista de sets) e o
   endpoint de histórico do exercício.
2. Implementação: começar pelo sub-requisito A (CSS + colapso, baixo risco), depois
   o contrato de sets (domínio + API + UI) e por fim o histórico do exercício.
3. Testador: cobrir os critérios de aceite de cada sub-requisito antes do merge.
