# UI de Atletas × Séries × Histórico — usar série existente e rastrear origem

- **Slug:** `slice-ui-atletas-series-historico`
- **Status:** proposto (2026-10-22)
- **Data:** 2026-10-22
- **Fatia:** complemento da fatia anterior (`serie-para-varios-atletas.md`, já
  implementada) — camada de aplicação gráfica e read-side que faltam: (A) vincular
  uma série **existente** a atleta(s) da roster, (B) listar os atletas que seguem
  uma série, (C) expor o nome da série de origem no histórico e (D) as telas
  correspondentes em `FitTrack.UI`.

## 1. Contexto e problema

A fatia anterior deu a `POST /workout-series` a lista `athleteIds`: criar a mesma
série para vários atletas virou uma chamada. Mas o fluxo só cria série **nova**.
Faltam três peças que a tela pede:

1. **Usar série existente.** Em `Athletes.razor` (linhas 139–167 e o handler
   `CreateForAthleteAsync` em 384–394), o "Nova série para este atleta" sempre
   chama `CreateWorkoutSeriesAsync` — uma série nova por atleta. Se o personal
   já tem a base montada (ex.: reutilizar o plano de outro atleta que encerrou
   o ciclo), ele não tem como apontar o atleta a ela: hoje a única via
   indireta é recriar a série com cópia. No backend **não existe** use case de
   assign a série existente — o `Reassign` hoje só acontece dentro de
   `CreateWorkoutSeries` (`CreateWorkoutSeries.cs:166–187`).
2. **Quem segue a série.** `Series.razor` lista e renomeia, mas não mostra os
   atletas vinculados; com séries compartilhadas (a fatia anterior), o personal
   não enxerga o público da série de onde ele está editando.
3. **Origem no histórico.** `SessionCard.razor` (usado por `MyWorkouts.razor:87`
   e `AthleteWorkouts.razor:77`) não mostra de qual série aquele treino veio —
   e o treino veio de uma `WorkoutSeriesWorkout` que pertence a uma série, dado
   que a sessão já carrega (`WorkoutSessionRow.WorkoutSeriesWorkoutId`, usado
   em `UserSessionQueries.cs:90–97`).

## 2. Objetivo e valor

- **Personal trainer:** prescrever a um atleta a série que **já existe** (dele),
  sem criar duplicata nem recopiar ciclos — um atleta entra no plano a qualquer
  momento.
- **Personal trainer:** ver, no card da série, quem está seguindo e em que
  ponto do termo de 60 dias cada um está.
- **Atleta e personal:** no histórico, saber de qual série o treino veio —
  contexto imediato ao avaliar a sessão.
- **Time:** zero migração; tudo se apoia em agregados e projeções que já
  existem (`WorkoutSeriesAssignment`, `UserSessionQueries`).

## 3. Atores envolvidos

- **Personal trainer** — vincula séries dele a atletas da própria roster; vê os
  seguidores das séries dele.
- **Atleta com self-authoring** — pode

  *ser autor* de séries próprias (`CreateWorkoutSeries.cs:62–73`,
  `WorkoutSeriesAccess.RequireAuthorAsync`); a decisão D3 cobre como o assign
  trata esse ramo.
- **Atleta vinculado** — só efeito colateral: recebe (ou têm o link
  reatribuído à) série prescrita; nada muda em `GET /workout-series/mine`.
- **Administrador** — sem poder novo nesta fatia (decisão D2): as séries são
  protegidas por autoria, não por papel.

## 4. Escopo

### 4.1 Incluído

1. **Sub A — Assign a série existente:** novo use case de escrita
   `AssignWorkoutSeries` (port in `IAssignWorkoutSeries`), endpoint
   `POST /workout-series/{seriesId}/athletes`, corpo mesclado
   singular + lista com o mesmo padrão do create
   (`CreateWorkoutSeries.DestinationList`, linhas 202–235). Sem copiar
   workouts: a série já existe e fica como está.
2. **Sub B — Seguidores:** novo read `ListWorkoutSeriesFollowers`
   (port in `IListWorkoutSeriesFollowers`, método em `IQueryWorkoutSeries`),
   endpoint `GET /workout-series/{seriesId}/athletes`.
3. **Sub C — Série de origem no histórico:** `WorkoutSessionDto` /
   `WorkoutSessionResponse` ganham `WorkoutSeriesName` (nullable), resolvido
   em lote no `UserSessionQueries.ComposeAsync` (sem N+1, padrão
   `LoadExerciseNamesAsync`, linhas 118–138).
4. **Sub D — UI:**
   - `Athletes.razor`: no inline-form atual, abas "Nova" ⇄ "Usar existente"
     com select (busca paginada via `GetWorkoutSeriesPageAsync`,
     `FitTrackApiClient.cs:108–115`) das séries do próprio personal; salvar
     chama o endpoint de assign.
   - `Series.razor`: por card, seção "Atletas seguindo" alimentada pelo read
     de seguidores.
   - `SessionCard.razor`: chip com o nome da série de origem (quando há).

### 4.2 Não incluído

- Migração de banco (o índice único de assignments continua em `UserId`;
  N atletas na mesma série já é suportado fisicamente).
- Desvincular atleta sem encerrar ciclo (`POST /athletes/{athleteId}/series/end`
  cobre a saída individual, incluindo a própria UI, já chamada no card).
- Compartilhamento entre treinadores — a autoria continua sendo de quem criou.
- Renomear/remover assignments em lote ou dashboards agregados.
- Mudança na semântica de `PUT /workout-series/{id}`.

## 5. Requisitos funcionais

### Sub A — Vincular série existente

1. **RF1** — `POST /workout-series/{seriesId}/athletes` aceita `athleteIds`
   (lista) e mantém `athleteId` (singular), mesclados na mesma ordem e com a
   mesma deduplicação do create: a primeira ocorrência define a posição e
   `Guid.Empty` conta como ausente (decisão D1, espelho de
   `CreateWorkoutSeries.DestinationList`).
2. **RF2** — Apenas o **autor** da série pode chamar: `WorkoutSeriesAccess.RequireOwnedAsync`
   responde 404 para série desconhecida e 403 para série de outro autor
   (`WorkoutSeriesAccess.cs:48–63`).
3. **RF3** — Para cada atleta: existência (`NotFoundException` → 404) e roster
   do chamador (`ForbiddenException` → 403, sem revelar de quem é a roster) —
   mesmas regras e mensagens do create (`CreateWorkoutSeries.cs:93–113`).
4. **RF4** — Para cada atleta com assignment existente (aberto ou fechado):
   `Reassign(seriesId, autor, now)` — aponta à série escolhida e reinicia o
   termo de 60 dias (`WorkoutSeriesAssignment.cs:88–95`); sem assignment:
   `Assign` com termo iniciado agora (`Assign`, linhas 59–85).
5. **RF5** — All-or-nothing na validação: nenhum atleta é vinculado se
   qualquer um falhar; o erro reportado é o do primeiro problema, na ordem da
   lista (padrão D7 da fatia anterior).
6. **RF6** — Sem cópia de workouts e sem mudança na série em si (nem nome,
   nem `AthleteId` legado): o assign só toca assignments.
7. **RF7** — O agregado não é alterado: a série pode ficar com
   `AthleteId` legado nulo ou preenchido, conforme o que já estava lá; o reuso
   do linked legado não é reavaliado.

### Sub B — Seguidores da série

8. **RF8** — `GET /workout-series/{seriesId}/athletes` devolve, para a série,
   os atletas com assignment apontando a ela: `userId`, e-mail/nome do atleta,
   estado do link (`IsOpen`), `expiresAt` e `daysRemaining`.
9. **RF9** — `daysRemaining` é calculado com
   `WorkoutSeriesAssignment.DaysRemainingUntil(expiresAt, now)`
   (`WorkoutSeriesAssignment.cs:118–119`), o mesmo helpers que a projeção SQL
   já usa (`WorkoutSeriesQueries.cs:116`, `AthleteQueries.cs:129`) — nenhuma
   regra de termo duplicada.
10. **RF10** — Apenas o **autor** da série vê os seguidores: série
    desconhecida → 404; série de outro autor → **404 também** ("como se não
    existisse"), o mesmo padrão de leitura de `GetWorkoutSeries`
    (`GetWorkoutSeries.cs:9-12`) — a leitura não revela que existe um autor
    alheio (corrijo aqui premissa do rascunho, que dizia 403). Admin não
    passa (D2).
11. **RF11** — Lista vazia → `200` com `[]` (D5), para simplificar o cliente
    (o card da tela apenas não renderiza a seção).

### Sub C — Série de origem no histórico

12. **RF12** — `WorkoutSessionDto` (e `WorkoutSessionResponse` do cliente)
    ganham `WorkoutSeriesName` (nullable): nome da série
    `WorkoutSeriesWorkoutId` → `workout_series_workouts` → `workout_series`.
13. **RF13** — O nome é resolvido em **um batch** por página de sessões,
    join/junção mais dicionário como `LoadExerciseNamesAsync`
    (`UserSessionQueries.cs:118–138`); sem consulta por linha (N+1).
14. **RF14** — Sessão sem `WorkoutSeriesWorkoutId`, ou com referência órfã
    (workout ou série apagados) → `WorkoutSeriesName = null`, sem erro — o
    mesmo tratamento que `ComposeAsync` já dá à prescrição
    (`UserSessionQueries.cs:103–107`).
15. **RF15** — `SessionCard.razor` exibe o nome da série como chip/badge
    quando presente e omite o chip quando nulo (D6).

### Sub D — UI

16. **RF16** — Em `Athletes.razor`, o inline-form "Nova série para este
    atleta" passa a ter as opções "Nova" (comportamento atual, preservado) e
    "Usar existente": select das séries **do próprio personal** carregadas por
    `GET /workout-series` paginado com busca.
17. **RF17** — Salvar em "Usar existente" chama o endpoint de assign com
    `athleteId` do atleta da linha e navega à tela da série
    (`/series/{id}`), igual ao fluxo atual (`Athletes.razor:384–394`).
18. **RF18** — Em `Series.razor`, por card, a seção "Atletas seguindo" lista
    os nomes e um chip de estado por atleta ("seguindo — N dias restantes" /
    "ciclo encerrado"), do read de seguidores; carregada ao expandir o card
    (um request por card aberto, não por lista, D-riscos).

## 6. Regras de negócio / invariantes

- **A série é template; o vínculo é individual.** Vincular série existente
  nunca altera a série: quem muda são os `WorkoutSeriesAssignment` — o mesmo
  princípio documentado na fatia anterior (seção 6).
- **Um assignment por atleta.** O índice único por `UserId` se mantém: assign
  a série existente reusa o vínculo (`Reassign`) em vez de criar o segundo.
- **Termo por atleta.** `Reassign` reinicia os 60 dias do **próprio atleta**;
  atletas da mesma série podem ter termos distintos.
- **Autoria manda nas mutações.** Assign só da mão do autor da série
  (`WorkoutSeries.TrainerId == chamador`), e o roster manda nos destinos —
  a mesma dupla de regras do create.
- **Sem cópia implícita.** Assign a série existente não tem parâmetro de
  cópia (D4): pessoas novas recebem a série como está.
- **O histórico é fato.** O nome da série no histórico vem do vinculo que a
  sessão já registrou; se a série for renomeada depois, as sessões antigas
  mostram o nome do **instante da leitura** (a resolução é por join, não por
  snapshot) — suposto implícito que o par comportamento/DTO nome-e-série
  reflete o último nome. (Verificado: nenhuma regra exige snapshot.)

## 7. Contrato HTTP (proposto)

### `POST /workout-series/{seriesId}/athletes`

```http
POST /workout-series/{seriesId}/athletes
{
  "athleteIds": ["<guid-1>", "<guid-2>"],   // opcional
  "athleteId": "<guid-1>"                   // legado, mesclado como no create
}
```

- **204 No Content** — atletas vinculados (assign assimétrico: resposta é o
  estado da série, não um DTO novo; a tela recarrega os dados).
- **404** — série desconhecida ou algum atleta da lista inexistente.
- **403** — série de outro autor, ou algum atleta fora da roster do chamador
  (mesma mensagem do create, sem revelar de quem é a roster).
- **400** — nenhum atleta informado (lista única vazia e singular ausente).

### `GET /workout-series/{seriesId}/athletes`

- **200** com `WorkoutSeriesFollowerResponse[]`:

```json
[
  {
    "userId": "<guid>",
    "email": "atleta@fittrack.app",
    "name": "Ana",
    "isOpen": true,
    "expiresAt": "2026-11-01T00:00:00Z",
    "daysRemaining": 9
  }
]
```

- **404** — série desconhecida e, com o mesmo tratamento ("como se não
  existisse", padrão de `GetWorkoutSeries`), a série de outro autor. A
  mutação de assign é que nomeia o erro (`403`) — a leitura nomeia apenas a
  ausência (`404`).

### Compatibilidade

- `GET /sessions` e `GET /athletes/{userId}/workouts` (e os espelhos) ganham o
  campo **opcional** `workoutSeriesName` (nullable) em
  `WorkoutSessionResponse`; clientes antigos ignoram o campo novo (RFC 7159,
  contrato aditivo).

## 8. Fluxos

### Fluxo principal — Sub A (usar série existente)

1. Personal abre `/Athletes`, clica "Nova série para este atleta" e escolhe a
   aba "Usar existente".
2. A tela carrega as séries dele (`GET /workout-series` paginado com busca) e
   o personal seleciona uma.
3. Ao salvar, `POST /workout-series/{seriesId}/athletes` com o `athleteId` do
   atleta.
4. O use case: carrega a série (`RequireOwnedAsync`), valida o atleta
   (existência + roster), resolve o assignment — `Reassign` (termo reinicia)
   ou `Assign` (termo novo) — e salva.
5. Resposta **204**; a tela navega a `/series/{id}` para o personal composto
   ( igual ao fluxo atual: `Athletes.razor:392–393`).
6. O atleta abre `/workout-series/mine` e vê a série prescrita, com o próprio
   termo; a próxima sessão registra `WorkoutSeriesWorkoutId` da série e o
   histórico passa a exibir o nome.

### Fluxo principal — Sub B (seguidores)

1. Personal abre `/Series` e expande o card da série.
2. `GET /workout-series/{seriesId}/athletes` (uma chamada por card aberto).
3. Cada seguidor aparece com o chip de estado ("seguindo — N dias restantes"
   ou "ciclo encerrado" para `IsOpen = false` ou termo expirado).

### Fluxo principal — Sub C (origem no histórico)

1. Atleta (ou personal no histórico dele) abre a listagem de sessões.
2. `UserSessionQueries.ComposeAsync` resolve, em lote, os nomes das séries dos
   workouts referenciados.
3. `SessionCard.razor` mostra o chip com o nome; sessões soltas (sem série)
   seguem exatamente como hoje, sem chip.

### Fluxos alternativos e erros

- **Atleta já segue outra série:** `Reassign` desacopla a anterior
  (implicitamente) e aponta à nova — a série antiga fica sem aquele seguindo;
  o histórico dele permanece (a composição por sessão não é tocada).
- **Atleta de outra roster:** 403, série e demais atletas intactos (RF5).
- **Série de outro autor:** 403 no **assign** (mutação sobre o alheio) e 404
  nos **followers** (leitura — "como se não existisse", padrão de
  `GetWorkoutSeries`; ver RF10 e a correção registrada ali).
- **Lista com duplicado:** deduplicado silenciosamente (mesma regra do create,
  D1).
- **Lista vazia (só singular ausente):** 400 — assign sem destino é sem efeito
  e melhor recusado (o create aceita porque cria série de todo jeito; assign
  sem destino não teria o que fazer).
- **Followers de série sem seguidores:** 200 com `[]`.
- **Sessão órfã:** `WorkoutSeriesName = null` e histórico normal (RF14).

## 9. Critérios de aceite

1. **A1** — Dado o personal com duas sérias (uma e por dois atletas), quando
   usa "Usar existente" para um terceiro atleta da roster, então o assign cria
   o vínculo (200/204) e o atleta o vê em `/workout-series/mine` na **mesma
   série** (mesma `SeriesId`).
2. **A2** — Dado o atleta já seguindo outra série, quando o personal vincula a
   existente, então o assignment reutilizado aponta à nova e o termo reinicia
   (60 dias a contar de agora).
3. **A3** — Dado um personal tentando vincular a série de outro autor, então
   a API responde `403` mesmo com o atleta na roster dele.
4. **A4** — Dado um atleta fora da roster do chamador, então a API responde
   `403` **antes** de salvar qualquer vínculo (all-or-nothing: com dois
   atletas, um válido e um alheio, nenhum é vinculado).
5. **A5** — Dado um atleta inexistente na lista, então a API responde `404`.
6. **A6** — Dada a série com dois seguidores (um ativo, um com ciclo
   encerrado), quando o autor abre o card em `/Series`, então a seção mostra os
   dois, com os chips corretos e dias restantes coerentes com o termo.
7. **A7** — Dadas as mesmas séries acessadas por um **não autor** (mesmo
   admin), então assign e followers respondem `403`/`404` conforme descrito.
8. **A8** — Dada uma sessão referenciando workout de série, quando o histórico
   é consultado, então `workoutSeriesName` traz o nome atual da série; para
   sessão sem referência (ou órfã), traz `null` e a tela omite o chip.
9. **A9** — Dado o fluxo "Nova" inalterado, quando o personal cria série nova
   para um atleta, então o comportamento é idêntico ao de hoje (regressão:
   nada de `405`/`409` no endpoint novo).

## 10. Modelo de dados

- **Nenhuma tabela nova, nenhuma migração.**
  - `workout_series_assignments` — a mesma tabela; assign existente reusa o
    vínculo e a série-shared continua permitindo N linhas por `SeriesId`.
- Ports / DTOs tocados (nenhum agregado novo):
  - Novos comandos: `AssignWorkoutSeriesCommand(Guid SeriesId, Guid? AthleteId,
    IReadOnlyList<Guid>? AthleteIds)` — singular + lista mesclados, como
    `CreateWorkoutSeriesCommand`.
  - Novos read-side DTOs: `WorkoutSeriesFollowerDto` (userId, email, name,
    isOpen, expiresAt, daysRemaining); `WorkoutSessionDto` ganha
    `WorkoutSeriesName?`.
  - API: `AssignWorkoutSeriesRequest` (espelho do create com
    `AthleteIds`), `WorkoutSeriesFollowerResponse`; `WorkoutSessionResponse`
    ganha `WorkoutSeriesName`. Espelhos em `FitTrack.UI/Api/`
    (`WorkoutSeriesRequests.cs` e o arquivo de sessões).

## 11. Impacto arquitetural

| Projeto | Impacto |
| --- | --- |
| `FitTrack.Domain` | Nenhum. O agregado `WorkoutSeriesAssignment` já provê `Assign`/`Reassign`/`DaysRemainingUntil` — reusados tal como estão. |
| `FitTrain.Application` | Novo port in `IAssignWorkoutSeries` + use case `AssignWorkoutSeries` (reuso do padrão de validação de `CreateWorkoutSeries`); novo port in `IListWorkoutSeriesFollowers` + use case; `IQueryWorkoutSeries` ganha `ListFollowersAsync(seriesId, now, ct)`. |
| `FitTrack.Adapters.In.Api` | `POST /workout-series/{seriesId}/athletes` e `GET /workout-series/{seriesId}/athletes` em `WorkoutSeriesEndpoints.cs` (o grupo `series`, linhas 39–49). |
| `FitTrack.Adapters.Out.Persistence` | `WorkoutSeriesQueries.ListFollowersAsync` — join `workout_series_assignments` × usuários (padrão de join com nome em `AthleteQueries.cs`); `UserSessionQueries` ganha o batch de nomes de séries (padrão `LoadExerciseNamesAsync`). Nenhuma migração. |
| `FitTrack.UI` | Cliente (`FitTrackApiClient.AssignSeriesAsync`, `ListSeriesFollowersAsync`; request/response novos); `Athletes.razor` (abas no inline-form + select), `Series.razor` (seção seguidores), `SessionCard.razor` (chip de série). |
| Testes | Casos unitários e de integração para os três subs (ver plano). |

## 12. Plano de testes

### Unitários (`tests/FitTrack.Unit`)

- `AssignWorkoutSeries`, autor feliz: assignment criado/reusado apontando à
  série; termo de 60 dias por atleta; série intocada (nome, workouts,
  `AthleteId` de hoje).
- A série existente com **vários** atletas no mesmo `seriesId` (reuso direto do
  modelo da fatia anterior) — mesmos assert de assign/reassign por atleta.
- Destino de outra roster → `ForbiddenException` e nada salvo (all-or-nothing).
- Destino inexistente → `NotFoundException`.
- Série de outro autor → `ForbiddenException` via `RequireOwnedAsync`.
- Singular + lista mesclados e deduplicados (espelho do
  `DestinationList` do create).
- `ListWorkoutSeriesFollowers`: seguidores ordenados, `daysRemaining` conforme
  `WorkoutSeriesAssignment.DaysRemainingUntil`, `isOpen` do link, autor ok /
  não autor recusado.
- Fakes: `InMemoryWorkoutSeriesQueries`
  (`tests/FitTrack.Unit/Fakes/InMemoryWorkoutSeriesQueries.cs`) implementa o
  novo método (a projeção sobre os fakes de assignments).

### Integração (`tests/FitTrack.Integration`)

- Assign a vários atletas ao mesmo `seriesId`: `GET /workout-series/mine` de
  cada atleta devolve a mesma série (estende a série de testes da fatia
  anterior — `WorkoutSeriesManagementTests`, `SeriesFlowTests`,
  `SeriesRefusalTests`).
- `403` para não autor; `404` para atleta alheio (roster/innexistência
  indistinguível na resposta de 403).
- `GET /workout-series/{id}/athletes` reflete os vínculos (após assign e após
  `POST /athletes/{athleteId}/series/end` — o seguidor sai da lista ou aparece
  como ciclo fechado; D6 define o chip).
- Histórico (`GET /sessions`, `GET /athletes/{userId}/workouts`) traz
  `workoutSeriesName` para sessão ligada a série e `null` para sessão órfã/
  avulsa.
- Regressão: `POST /workout-series` (com e sem `athleteId`/`athleteIds`)
  inalterado; `PUT` e endpoints de workouts intocados.

### UI (`tests/FitTrack.UITests`)

- **Nesta fatia, a cobertura de UI ficou fora**: os testes da entrega são os
  unitários/integração do backend mais a regressão completa já verde. Os
  casos de contrato no espelho do cliente (`AssignSeriesAsync` serializa
  singular + `athleteIds`; `WorkoutSessionResponse` deserializa
  `workoutSeriesName`; `SeriesFollowerResponse` deserializa os campos do
  read) e os de tela (abas, chips, navegação a `/series/{id}`) entram com a
  fatia de testes de tela — devidamente registrados na seção 14.
- Decisão de implementação registrada: o select de séries do "Usar
  existente" usa a leitura simples (`GET /workout-series`, array plano) em
  vez da paginada com busca — a roster é pequena; trocar quando o catálogo
  do autor crescer.

## 13. Riscos e dependências

- **N+1 de seguidores:** um request por card em `/Series` é aceitável, mas a
  seção deve carregar **ao expandir o card**, não em lote para todas as séries
  da página (D-impacto listado no RF18). Monitorar se a página passar a
  renderizar todos os card abertos por padrão.
- **Compat do contrato de sessões:** campo novo opcional; clientes antigos
  ignoram. Cuidado apenas com testes de snapshot/desserialização estrita em
  outros consumidores.
- **Assign sem validação de roster = vazamento:** a validação é o PII do
  assign; pular a checagem de roster permitiria a um personal "prescrever" a qualquer
  usuário. O use case deve replicar as mesmas guardas do create, testadas
  (RF3/RF5).
- **`Reassign` reinicia termo de quem já treinava outra série:** é a semântica
  vigente do fluxo singular/create; documentado aqui para não surpreender (o
  atleta não é notificado nesta fatia).
- **Nome da série no histórico:** a resolução é por join no momento
  da leitura — renomear a série muda o rótulo de sessões antigas (não há
  snapshot). Aceito; revisar se a exigência de histórico imutável surgir.
- **Gravação em laço sem unit-of-work:** como no create, cada
  assignment é salvo com o seu `SaveAsync`; a pré-validação all-or-nothing
  cobre os erros declarados (404/403), mas uma falha de runtime a meio do
  laço deixaria estado parcial. Sem padrão de transação por use case no
  projeto, herdamos o comportamento das demais gravações multi-save.
- **Batch de nomes de série roda por `ComposeAsync`:** a mesma query extra
  executa para a página e para o sumário; aceitável no padrão de
  `LoadExerciseNamesAsync`, sem N+1.
- **Un-assign deliberadamente deferido:** não existe hoje "desvincular sem
  fechar". `Close()` mantém o registro vivo de propósito — é a fonte de
  `CopyPreviousWorkouts` — e `Reassign` sobrescreve sem histórico. Um DELETE
  físico destruiria o rastro que cópias futuras precisam; se surge a
  necessidade real, modelar como `Detach` no agregado, não como DELETE.
- **Dependência fatia anterior:** os 2 endpoints e o modelo multi-atleta
  já implementados (`CreateWorkoutSeries.cs`) são pré-condição; nada desta
  fatia funciona sem eles.

## 14. Decisões em aberto

| ID | Decisão | Recomendação |
| --- | --- | --- |
| **D1** | Corpo do assign: singular + lista mesclados, ou só lista? | Mesclar, espelhando o create (`DestinationList`): um único padrão nos dois endpoints e compat com callers que usam o singular. |
| **D2** | Admin pode assign/ver followers de série de outro autor? | Não — assign e followers protegidos por **autoria** (`RequireOwnedAsync`), não por papel. A regra vigente é uma só: **autoria manda na série, roster manda nos destinos** — para todos os papéis. Correção de premissa: em `EndWorkoutSeries` o admin **não** dá bypass — o ramo trainer dele também exige o vínculo de roster (`EndWorkoutSeries.cs:47-56`); lá o gate é roster, aqui é autoria da série + roster dos destinos. Decisões coerentes entre si. |
| **D3** | Atleta liberado (self-authoring) pode se atribuir série própria? | Sim — o autor da própria série pode `Assign` a si mesmo via o mesmo endpoint (idempotente em reusar o próprio link: `Reassign` reinicia o próprio termo; sem assignment porque o self-authored já o tem). A lista é ignorada, como no create. |
| **D4** | Assign aceita `copyPreviousWorkouts`? | Não — não há de onde copiar: a série já existe e fica como está. O parâmetro nem entra no request. |
| **D5** | Followers vazio: `204` ou `200` com `[]`? | `200` com `[]` — resposta sempre lista (simplicidade do cliente e dos fakes). |
| **D6** | O chip de estado em `Series.razor` mostra ou omite o seguidor com ciclo encerrado? | Mostrar, como "ciclo encerrado" (não omitir): é informação real do read (`isOpen = false`), e o personal decide se reativa/ignora. |
| **D7** | `GET /workout-series/{seriesId}/athletes` também empaginado (`ListQuery`)? | Não nesta fatia: a roster limita o tamanho na prática (mesma justificativa de D5 da fatia anterior); adicionar paginação depois, se cards de séries de rosters grandes precisarem. |
| **D8** | `POST` de assign responde `204` (sem corpo) ou `200` com o DTO da série? | `204` — a tela recarrega os dados que precisa (lista de séries/seguidores); a série em si não mudou, não há DTO novo a devolver. |

## 15. Sugestão de próximo passo

1. **Arquiteto:** validar D1–D8, em especial D2 (autor-only, sem bypass de
   admin), D3 (self-authoring) e o formato do comando (singular+lista).
2. **Implementação:** começar pelo use case `AssignWorkoutSeries` + testes
   unitários (reusa quase integralmente o padrão de `CreateWorkoutSeries`),
   depois os endpoints, o read de seguidores, o campo no histórico e, por
   último, as três telas.
3. **Testador:** cobrir A1–A9 antes do merge, incluindo a regressão do
   contrato de `POST /workout-series` e de sessões.
