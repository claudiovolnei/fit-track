# Histórico — sets recolhidos por grupo e comentário sempre presente

- **Slug:** `historico-sets-recolhidos-comentario`
- **Status:** implementado (2026-10-23) — fatia somente de UI/apresentação: nenhum
  contrato HTTP, DTO ou dado persistido muda. **Parecer arquitetural aprovado
  (2026-10-23):** estado no dono da tela confirmado como obrigatório (o
  `_loading` das telas troca o branch inteiro a cada busca/paginação, então
  estado interno do componente morre), agrupamento em classe estática na
  `FitTrack.UI` (precedente `DisplayText.cs`) e testes em dois níveis
  (`FitTrack.Unit` para o helper, bunit para as telas). Provado com os 314
  testes unitários e os 24 testes de tela.
- **Feature anterior relacionada:** `historico-volume-por-exercicio`
  (definiu o card atual do histórico — strip, comparação por exercício e a lista
  `session-sets-list` que esta feature reorganiza; nada do modelo de volume é
  tocado aqui).
- **Decisão do dono (2026-10-23):** a lista de séries do histórico agrupa itens
  consecutivos de exercício/repetições/carga iguais, o grupo recolhe/expande e
  **começa recolhido**; a linha "Sobre o treino" passa a existir sempre, com
  "não informado" quando o atleta não escreveu nada. O relato de sobreposição
  da faixa "Esta semana" com o banner da série na Home ficou **fora de escopo**:
  o dono confirmou hoje (2026-10-23) que era cache do navegador.

## 1. Contexto e problema

O histórico de treinos (`/workouts` em `MyWorkouts.razor` e
`/athletes/{userId}/workouts` em `AthleteWorkouts.razor`) renderiza o card de
cada sessão pelo componente compartilhado
`src/FitTrack.UI/Components/SessionCard.razor`. A lista de séries registradas
(`.session-sets-list` > `.session-set-row`, linhas 77–85) imprime **uma linha
por item de `Session.Sets`**:

```
Abdominal Crunch — 20 kg × 20
Abdominal Crunch — 20 kg × 20
Abdominal Crunch — 20 kg × 20
Abdominal Crunch — 20 kg × 20
```

O mesmo exercício com a mesma execução aparece quatro vezes seguidas, poluindo
o card — o atleta executou "4 séries de 20 repetições a 20 kg", e é isso que a
tela "Hoje" já sabe mostrar: `SeriesExerciseList.razor` (linhas 21–22) renderiza
`<strong>@prescribed.Sets × @prescribed.Repetitions</strong><span>@Load(...)</span>`,
uma linha por exercício com o agregado "sets × reps" e a carga ao lado.

Dois ajustes pedidos no mesmo card:

1. **Agrupar** os sets consecutivos iguais num único cabeçalho recolhível
   (começa recolhido; expandir revela os sets individuais).
2. **Comentário do treino:** o card mostra "Sobre o treino: @Session.Notes"
   apenas quando `!string.IsNullOrWhiteSpace(Session.Notes)`
   (`SessionCard.razor` linhas 49–54). O dono quer a linha sempre presente —
   quando o atleta deixou o textarea "Sobre o treino" do
   `FinishWorkoutModal.razor` vazio, o card mostra
   "Sobre o treino: não informado".

## 2. Objetivo e valor

- **Atleta e personal:** ler o card do histórico de relance — "4 × 20 · 20 kg"
  em vez de quatro linhas iguais — com a cronologia preservada ao expandir.
- **Menos ruído no card:** a área de sets encolhe para um cabeçalho por grupo;
  sessões grandes deixam de virar um bloco de repetições idênticas.
- **Honestidade do comentário:** o card garante que sempre existe a linha
  "Sobre o treino"; o leitor distingue "o atleta não escreveu nada" de
  "o dado sumiu" — já que o modal de finalização deixa o campo vazio.
- **Consistência de padrão:** o recolhível segue exatamente o padrão da casa
  (Hoje), reutilizando o vocabulário visual já existente.

## 3. Atores

- **Atleta** — vê os grupos recolhidos e o comentário em `/workouts`.
- **Personal trainer / Admin** — vê o mesmo card em
  `/athletes/{userId}/workouts`.
- **Sistema (UI)** — agrupa na renderização (leitura pura, em memória); a API
  e o banco não mudam nada.

## 4. Escopo

### 4.1 Incluído

1. **Sub-requisito A** — agrupamento de sets consecutivos iguais no card do
   histórico, recolhível, começando recolhido (atleta e personal).
2. **Sub-requisito B** — a linha "Sobre o treino" sempre presente com
   "não informado" quando o atleta não escreveu nada.
3. **Sub-requisito C** — ajustes documentais: README e referência à feature
   anterior.

### 4.2 Não incluído

- **Relato de sobreposição da faixa "Esta semana" (Home) com o banner da
  série:** excluído por decisão do dono em 2026-10-23, que confirmou tratar-se
  de cache do navegador — não há bug de sobreposição a corrigir.
- **Contrato HTTP:** nenhum endpoint muda; `WorkoutSessionResponse.Notes`
  (`string?`) já chega ao cliente.
- **Banco de dados:** nenhuma tabela, coluna ou migração; o agrupamento é
  calculado na renderização a partir de `Session.Sets` que já vem na listagem.
- **"Hoje" (`Today.razor`) e `SeriesExerciseList`:** seguem como estão — servem
  de referência de padrão, não são alterados.
- **Tela do treino em execução:** o card com `IsRunning` recebe as mesmas
  regras de agrupamento (o strip e os sets vêm do mesmo componente), sem
  tratamento especial.

## 5. Sub-requisito A — Grupos de sets recolhidos no card

### 5.1 Requisitos funcionais

1. Os itens de `Session.Sets` são agrupados em **runs consecutivas**: dois sets
   vizinhos entram no mesmo grupo quando têm o mesmo
   `(ExerciseId, Repetitions, LoadKilograms)`; a quebra de qualquer um dos três
   valores abre um grupo novo, preservando a cronologia do treino (D1).
2. O cabeçalho do grupo mostra o **agregado**: `{número de sets} ×
   {Repetitions}` seguido da carga — `4 × 20 · 20 kg` (D2).
3. O grupo é um **recolhível real**: um `<button type="button">` com
   `aria-expanded` (começa `"false"`) e `aria-controls` apontando para o
   contêiner dos sets; o chevron é o `.app-icon` dentro de `.exercise-toggle`,
   que gira quando `aria-expanded="true"` (padrão do `Today.razor` / app.css
   ~linha 1441). O alvo de clique é o cabeçalho inteiro.
4. O grupo **começa recolhido**: sem interação, o card lista um cabeçalho por
   grupo e nenhuma linha individual.
5. Ao expandir, o grupo revela as **linhas individuais de set como hoje**
   (`.session-set-row`: nome do exercício + pill "{carga} kg × {reps}"), cada
   uma identificada pelo índice do set na sessão (D2).
6. **Grupo de um único set** renderiza direto, como a linha atual, sem
   cabeçalho nem toggle — abrir um recolhível para um item só não agrega
   informação (D4).
7. O **estado de expansão** vive no dono da tela (`MyWorkouts.razor` e
   `AthleteWorkouts.razor`), num `HashSet<string>` com chave
   `{session.Id}:{índice do run}`, propagado ao card via componente filho
   `SessionSetList` com `EventCallback<string>` (a chave do grupo tocado) —
   `SessionCard` fica puramente repassador, sem estado; a tela faz add/remove
   espelhando `_expanded` do `Today.razor` (D3, refinado pelo arquiteto: um
   `bool` único não transpõe para N grupos por sessão). É o único caminho que
   sobrevive a busca/paginação: **toda** `LoadAsync` passa por `_loading`,
   trocando o branch do render e destruindo componentes internos — estado
   interno no `SessionSetList` violaria o critério 5.2.5.
8. As telas paginadas (10 por página, com busca) repassam o mesmo conjunto de
   chaves; a chave pela sessão + run garante que a expansão de uma página não
   "vaza" para outra (D3; risco 11.2).

### 5.2 Critérios de aceite

1. Dado um treino com 4 sets consecutivos do mesmo exercício com mesmas
   repetições e carga, quando o card renderiza, então aparece **um cabeçalho**
   `4 × 20 · 20 kg`, recolhido, e **nenhuma** linha individual.
2. Dado o mesmo treino, quando o dono clica no cabeçalho, então
   `aria-expanded` vira `"true"`, o chevron gira e as 4 linhas individuais
   aparecem na ordem registrada, cada uma com seu índice.
3. Dados sets 20 kg, depois 30 kg, depois 20 kg do mesmo exercício, quando o
   card renderiza, então existem três grupos na ordem registrada (cronologia
   preservada).
4. Dado um set único de um exercício, quando o card renderiza, então a linha
   aparece direto, sem cabeçalho clicável nem `aria-expanded`.
5. Dado um grupo expandido, quando o atleta busca ou pagina o histórico, então
   as outras sessões continuam recolhidas e o grupo da mesma sessão/posição
   permanece como estava naquela página.
6. Dado o mesmo histórico aberto pelo personal em
   `/athletes/{userId}/workouts`, então o agrupamento e o recolhível se
   comportam igual.

## 6. Sub-requisito B — "Sobre o treino" sempre presente

### 6.1 Requisitos funcionais

1. O card do histórico mantém a linha `<strong>Sobre o treino:</strong>
   …` para **toda** sessão, removendo a condição
   `!string.IsNullOrWhiteSpace(Session.Notes)`.
2. Quando o atleta não escreveu nada no final (`Notes` `null`, vazia ou só de
   espaços — critério `string.IsNullOrWhiteSpace`, D6), o texto mostrado é
   **"não informado"**.
3. Quando há texto, o valor cheio de `Session.Notes` aparece como hoje.

### 6.2 Critérios de aceite

1. Dada uma sessão sem `Notes`, quando o card renderiza, então o card mostra
   "Sobre o treino: não informado".
2. Dada uma sessão com `Notes = "pernas cansadas"`, quando o card renderiza,
   então a linha mostra "Sobre o treino: pernas cansadas".
3. A mudança vale nos dois históricos
   (atleta `/workouts` e personal `/athletes/{userId}/workouts`).

## 7. Sub-requisito C — Documentação

### 7.1 Requisitos funcionais

1. `README.md` — a linha do estado atual que descreve o histórico (seção
   "Current state", parágrafo do fechamento da sessão) menciona hoje "a nota
   que o atleta escreveu": ajustar para a linha sempre presente com "não
   informado" e citar o agrupamento recolhido.
2. `docs/features/historico-volume-por-exercicio.md` — anotar que a lista
   `session-sets-list` descrita no card da sessão (seção 7 daquela feature)
   passa a ser agrupada por esta feature.

### 7.2 Critérios de aceite

1. Dado o README hoje, quando a feature é documentada, então os dois pontos do
   item 7.1 estão alinhados ao comportamento novo.

## 8. DTOs / contrato (inalterado)

- `WorkoutSessionResponse` já carrega `Notes` (`string?`,
  `src/FitTrack.Application/Dtos/WorkoutSessionDto.cs` linha 26) e `Sets`
  (com `ExerciseId`, `Repetitions`, `LoadKilograms`) — nada muda na API,
  nos DTOs ou no JSON.
- O agregado do cabeçalho (`{n} × {reps}` + carga) é calculado na renderização;
  nenhum campo novo é exposto.

## 9. Impacto arquitetural

| Projeto | Impacto |
| --- | --- |
| `FitTrack.UI` | `SessionCard.razor`: bloco de notas (6.1) e troca do `foreach` de sets (linhas 77–85) pelo novo `SessionSetList.razor` (novo componente, repasse puro sem estado). `SessionSetGroups.cs` (novo): static helper de runs consecutivas com o record `SetRun` aninhado — precedente `DisplayText.cs`, nada de apresentação vaza para Application. `MyWorkouts.razor`/`AthleteWorkouts.razor`: estado `_expandedGroups` (HashSet) e repasse. `app.css`: cabeçalho de grupo reutilizando `.exercise-toggle`/`.session-set-row` (D5) e, no máximo, uma classe nova para o cabeçalho. |
| `FitTrack.Application` | Nenhum — DTOs já carregam tudo; nenhum caso de uso é tocado. |
| `FitTrack.Domain` | Nenhum — nenhum agregado muda. |
| `FitTrack.Adapters.Out.Persistence` | Nenhum — a listagem já devolve os sets completos. |
| Testes | Novos testes de tela em `tests/FitTrack.UITests` (bunit + `FakeApi`/`Sheet`), seção 10. |

## 10. Plano de testes

O projeto `tests/FitTrack.UITests` (bunit 2.12.5 + xunit, base `ScreenTest`/
`FakeApi`) cobre as telas (`Today`, `MeuPlano`, `AdminUsers`, e agora o
histórico), com o agrupamento em dois níveis (arquiteto):

- **Unidade do helper** (`tests/FitTrack.Unit/Components/SessionSetGroupsTests.cs`,
  precedente `NavOptionsTests` que testa a `FitTrack.UI`; o helper é uma classe
  estática pública como `NavOptions`/`DisplayText`): matriz de runs —
  4 sets iguais → 1 run; quebra de carga 20/30/20 → três grupos de um set;
  set único → run simples; mesmo exercício separado no treino → dois runs
  distintos; repetição diferente → quebra mesmo com exercício e carga iguais;
  sessão vazia → nenhuma run. `LoadKilograms` do set executado é `decimal`
  não-nulo no fio (peso corporal é um valor, não um nulo), então não há caso
  de carga nula a tratar.
- **Tela** (`tests/FitTrack.UITests/MyWorkoutsScreenTests.cs`, novo, com as
  fixtures de `Sheet` estendidas — `ChestWorkout`, `History` e `Roster`):
  cabeçalho `4 × 20 · 20 kg` recolhido; clique expande com `aria-expanded="true"`
  e revela as 4 linhas com o ordinal do set; quebra de carga (20/20/30/20/20)
  → dois cabeçalhos e a linha do meio direto (D4); set único → linha sem
  toggle; comentário sem `Notes` → "não informado" e com texto → texto cheio;
  expandir um grupo e re-render por busca (API fake que responde por query) →
  o grupo da sessão persiste, os outros recolhidos. Casos do personal
  (AthleteWorkouts) no mesmo arquivo. Os testes do `SessionCard` em
  `MeuPlanoScreenTests.cs` passaram a exigir "não informado" no lugar de
  "linha ausente" (6.2.1).

## 11. Riscos

1. **Grupos de 1 elemento:** um toggle para um set único é um clique que não
   agregaria nada e uma `aria-expanded` vazia de sentido. Mitigação (D4):
   renderizar direto, sem cabeçalho clicável — a linha do card continua
   idêntica à de hoje nesse caso.
2. **Busca/paginação re-renderizando a página:** `LoadAsync` substitui
   `_sessions` inteiro a cada busca ou página. O estado de expansão usar
   `session.Id + chave do run` evita vazamento entre sessões; sessões que saíram
   de tela deixam chaves órfãs inofensivas (nenhuma busca as referencia). Chave
   por **índice do run** na sessão (e não por conteúdo) porque o mesmo
   `(exercício, reps, carga)` pode reaparecer separadamente no mesmo treino —
   dois runs distintos precisam de chaves distintas (D1/D3).
3. **Acessibilidade:** o cabeçalho tem de ser um `<button type="button">` real
   (não `div` com `@onclick`), focável por teclado, com `aria-expanded` que
   controla o CSS do chevron e `aria-controls` apontando para um `id` estável do
   contêiner (gerado uma vez por componente, como `_listId` em
   `SeriesExerciseList.razor`). Todo o cabeçalho é o alvo de clique.
4. **Volume do DOM em sessões grandes:** o DOM default encolhe (grupos
   recolhidos renderizam um cabeçalho e nenhuma linha), mas uma sessão com
   dezenas de alternâncias de carga gera dezenas de grupos. A paginação de 10
   sessões por página limita o pior caso; virtualização fica fora desta fatia.
5. **Padrão de recolhível consistente:** reutilizar `.exercise-toggle`
   (chevron gira com `aria-expanded`) e `.session-set-row` (linhas) mantém o
   vocabulário do Today; a única peça nova é o estilo do cabeçalho do grupo,
   que deve conversar com `.summary-strip` e `.volume-comparison` (fonte
   `0.8x`, `--text-soft`, bordas `--border-soft`) em vez de criar uma família
   nova (D5).

## 12. Referências a alterar (checklist)

1. `src/FitTrack.UI/Components/SessionCard.razor` — linha de notas (49–54)
   sempre presente; lista de sets (77–85) delegada ao `SessionSetList`.
2. `src/FitTrack.UI/Components/SessionSetList.razor` — **novo componente**:
   repasse puro de `ExpandedKeys`/toggles; renderiza o wrapper
   `.session-sets-list` (a lista do card continua existindo no DOM, agora com a
   SessionSetList dentro), cabeçalhos recolhíveis e linhas `.session-set-row`.
3. `src/FitTrack.UI/Components/SessionSetGroups.cs` — **novo**: helper
   `public static` de runs consecutivas + record `SetRun` aninhado
   (precedente `DisplayText.cs`, mesmo projeto).
4. `src/FitTrack.UI/Pages/MyWorkouts.razor` — `_expandedGroups` (HashSet) e
   repasse ao card (D3).
5. `src/FitTrack.UI/Pages/AthleteWorkouts.razor` — idem.
6. `src/FitTrack.UI/Components/SeriesExerciseList.razor` — referência do
   padrão de parâmetros/EventCallback (sem alteração).
7. `src/FitTrack.UI/Pages/Today.razor` — referência do dono do estado
   (`_expanded`, ~linha 390; sem alteração).
8. `src/FitTrack.UI/wwwroot/css/app.css` — nova classe do cabeçalho de grupo
   (`.set-group-toggle`, e o aggregate como lado direito da linha), reutilizando
   `.exercise-toggle` (~1441), `.session-set-row` (~1790) — ellipsis do nome
   vem do seletor `> span:first-child` existente — e o vocabulário de
   `.summary-strip` (~1587) / `.volume-comparison` (~2477).
9. `tests/FitTrack.Unit/Components/SessionSetGroupsTests.cs` — **novo**:
   matriz do agrupamento (10).
10. `tests/FitTrack.UITests/Sheet.cs` — fixture de `WorkoutSessionResponse`
    do histórico; `tests/FitTrack.UITests/MyWorkoutsScreenTests.cs` — **novo**,
    cobrindo AthleteWorkouts (10).
11. `src/FitTrack.Application/Dtos/WorkoutSessionDto.cs` — só conferência:
    `Notes string?` (linha 26) já atende.
12. `README.md` — parágrafo do fechamento da sessão na seção "Current state"
    (hoje na linha ~254): nota sempre presente + agrupamento recolhido
    (**durante a implementação**, para o README nunca descrever estado futuro).
13. `docs/features/historico-volume-por-exercicio.md` — nota apontando que a
    lista do card agora vem agrupada (esta feature).

## 13. Decisões em aberto

| ID | Decisão | Recomendação |
| --- | --- | --- |
| **D1** | Chave do agrupamento: runs consecutivas por `(ExerciseId, Repetitions, LoadKilograms)`? | Sim — preserva a cronologia do treino (o dono já indicou); agrupar globalmente por exercício reordenaria os sets e esconderia alternâncias reais de carga. |
| **D2** | O que a expansão revela e o que o cabeçalho agrega. | Cabeçalho `4 × 20 · 20 kg`; expansão mostra as linhas individuais como hoje, cada uma com o índice do set na sessão. |
| **D3** | Onde vive o estado de expansão. | No dono da tela (`MyWorkouts`/`AthleteWorkouts`), `HashSet<string>` com chave `{session.Id}:{índice do run}`, propagação por `EventCallback<string>` (a chave do grupo tocado) — refinado pelo arquiteto: um `bool` único não transpõe para N grupos por sessão, e o estado interno do componente **violaria** o critério 5.2.5 (o `_loading` das telas troca o branch do render a cada busca/paginação, destruindo componentes internos; "the screen owns it, so the state survives the redraws"). |
| **D4** | Grupo de 1 elemento mostra direto, sem toggle? | Sim: linha direta, sem cabeçalho clicável nem `aria-expanded` — um toggle de item único não agrega informação e seria ruído. |
| **D5** | Reutilizar `.exercise-toggle`/`.session-set-row` ou classes novas? | Reutilizar as duas; criar no máximo **uma** classe nova para o cabeçalho do grupo, com o mesmo vocabulário visual do card. |
| **D6** | Critério de "não informado": `IsNullOrWhiteSpace`? | Sim — `null`, vazia ou só espaços exibem "não informado", alinhado ao que o modal de finalização grava quando o campo fica vazio. |

## 14. Sugestão de próximo passo

Avaliar a arquitetura da entrega (apresentação pura: nenhum caso de uso, DTO,
agregado ou adapter mudam; a única superfície é `FitTrack.UI` mais a folha de
estilo e os testes de tela) e, aprovada, implementar nesta ordem:
`SessionSetList.razor` → `SessionCard.razor` (notas + sets) → telas donas do
estado → `app.css` → testes em `FitTrack.UITests` → README e nota na feature
anterior.
