# Série para vários atletas — prescrição coletiva na criação do plano

- **Slug:** `serie-para-varios-atletas`
- **Status:** implementada (2026-10-22) — pendente rodada de integração com Postgres
  (`FITTRACK_TEST_DB`) para os testes pulados.
- **Data:** 2026-10-22
- **Fatia:** uma fatia única — o endpoint `POST /workout-series` passa a aceitar uma
  lista de atletas, criando um vínculo (assignment) por atleta apontando à mesma
  série, sem migração de banco.

## 1. Contexto e problema

O treinador que monta a mesma base de treino para vários atletas precisa repetir
a criação de série uma vez por pessoa. Hoje `POST /workout-series` aceita no
máximo **um** `AthleteId`: para prescrever o mesmo plano a três atletas, ele
passa três vezes pelo fluxo completo (nome único por autor impede nem reusar o
nome), escolhe a sheet do ciclo anterior de cada um, e o resultado são três
séries quase idênticas que ele precisa manter em paralelo — mudou uma carga num
workout, ajusta nas três.

O modelo de dados já está pronto para mais que isso:

- `WorkoutSeries` é agregado **raiz de planejamento** e template reutilizável
  (`WorkoutSeries.cs`: "a same series serves many people without being copied").
- `WorkoutSeriesAssignment` é o vínculo atleta↔série, com index único por
  `UserId` (um vínculo por atleta) e **sem restrição de unicidade em
  `SeriesId`**: vários atletas podem apontar à mesma série hoje, fisicamente.
- O gargalo é o **use case e o contrato HTTP**, que limitam a 1 atleta.

## 2. Objetivo e valor

- **Personal trainer:** montar o plano uma vez e prescrevê-lo a quantos atletas
  da própria roster quiser, em uma só chamada.
- **Personal trainer:** manutenção única — um workout ajustado numa série
  compartilhada chega a todos os atletas vinculados, sem retrabalho.
- **Atleta:** passa a seguir a mesma série que os colegas, com o próprio termo
  de 60 dias e o próprio histórico (assignment individual).
- **Time:** zero migração — a mudança é de contrato-application, sustentada por
  agregados que já existem.

## 3. Atores envolvidos

- **Personal trainer** — cria a série e escolhe os atletas (da própria roster).
- **Administrador** — tem o mesmo poder do personal sobre qualquer roster
  (`IsPersonalTrainer` cobre o ramo trainer no use case atual).
- **Atleta com self-authoring** — continua criando apenas para si mesmo; a lista
  de atletas é ignorada no ramo `selfAuthored` (comportamento atual preservado).
- **Atleta vinculado** — recebe a série por `WorkoutSeriesAssignment` e segue
  treinando por `GET /workout-series/mine` como hoje; nada muda para ele além
  de ser um de vários seguidores da mesma série.

## 4. Escopo

### 4.1 Incluído

1. O comando de criação passa a carregar uma **lista** de atletas além do
   singular atual (compatibilidade de cliente — ver D1).
2. Validar cada atleta (existência + roster do treinador) com as mesmas regras
   e erros de hoje (404 / 403).
3. Para cada atleta: reutilizar o assignment existente via `Reassign` (termo de
   60 dias reinicia, a série do atleta passa a ser a nova) ou criar `Assign`
   novo — exatamente como o fluxo singular faz hoje.
4. Deduplicar atletas repetidos na lista (D4).
5. `CopyPreviousWorkouts` definido para o caso multi-atleta (D3).
6. All-or-nothing na validação: nenhum atleta válido é prescribing se qualquer
   atleta da lista violar regra (D7).

### 4.2 Não incluído

- Migração de banco (o índice único de assignment é em `UserId`; nada muda
  fisicamente — ver Modelo de dados).
- Nova tela/fluxo UI: o repositório Web aqui é esqueleto (sem chamadas a
  workout-series); as telas do treinador vivem no cliente de outro
  repositório. O contrato HTTP e o cliente espelhado em `FitTrack.UI/Api`
  cobrem o necessário.
- Desvincular um atleta de uma série compartilhada sem encerrar o ciclo dele
  (`POST /athletes/{athleteId}/series/end` já cobre a saída individual).
- Compartilhamento de série **entre treinadores** (a autoria continua sendo de
  quem criou; a edição continua só do autor).
- Mudança na semântica de `PUT /workout-series/{id}` (que hoje recebe
  `AthleteId` no corpo mas não o usa no `RenameWorkoutSeriesCommand`) — fora do
  recorte desta fatia.

## 5. Requisitos funcionais

1. **RF1** — `POST /workout-series` aceita `athleteIds` (lista de GUIDs),
   além de manter `athleteId` (singular) como compatibilidade; o valor
   singular, quando presente, é mesclado na lista (D1).
2. **RF2** — A lista, após deduplicação, pode ter de 0 até N atletas; com 0
   atletas o comportamento é o de hoje (série sem vínculo imediato).
3. **RF3** — Para **cada** atleta da lista, o use case valida existência
   (`NotFoundException` → 404, como hoje) e roster do treinador
   (`ForbiddenException` → 403, como hoje), sem revelar de quem é a roster.
4. **RF4** — A série é criada **uma única vez** com `WorkoutSeries.Create`
   (autor = treinador chamador) e os assignments são criados/reaproveitados
   **por atleta**, todos apontando ao mesmo `SeriesId`.
5. **RF5** — Para atleta com assignment existente (fechado ou não), o use case
   chama `Reassign` (substitui a série e reinicia o termo de 60 dias), como o
   fluxo singular faz; para atleta sem assignment, cria `Assign` com termo
   iniciado na criação.
6. **RF6** — Atletas duplicados na lista são deduplicados (primeira ocorrência
   define a posição, ver D4; a ordem resultante alimenta D3).
7. **RF7** — `CopyPreviousWorkouts = true` copia os workouts da sheet do ciclo
   anterior **uma vez** para a série, conforme a fonte definida em D3.
8. **RF8** — O nome da série continua único por autor
   (`workout_series.name.duplicate`); a criação em lote não muda a regra.
9. **RF9** — O ramo self-authored (`!IsPersonalTrainer`) continua: atleta
   liberado cria para si mesmo, lista e singular ignorados
   (`RequireAuthorAsync` + `athleteId = _currentUser.Id`), como hoje.
10. **RF10** — O campo legado `WorkoutSeries.AthleteId` é preenchido quando
    houver **exatamente 1** atleta e fica `null` quando houver 2+ (D2).

## 6. Regras de negócio / invariantes

- **A série é template, o vínculo é individual.** `WorkoutSeries` continua com
  no máximo um `AthleteId` legado (D2); a pertença real do atleta vive em
  `WorkoutSeriesAssignment`. N atletas podem seguir a mesma `SeriesId`.
- **Um assignment por atleta.** O index único por `UserId` garante
  1 vínculo por atleta; o reassign reutiliza o vínculo em vez de criar um
  segundo — mesma regra do fluxo singular atual.
- **Termo de 60 dias por atleta.** Cada assignment tem o próprio
  `AssigndAt`/termo; atletas da mesma série podem ter termos iniciados no
  mesmo instante da criação (o `IClock` é consultado uma vez por atleta ou uma
  vez para o lote — implementação decide, sem efeito observável).
- **Roster manda.** Um atleta de outra roster ou sem treinador é recusado com
  403, como hoje; a resposta não distingue os dois casos.
- **Atomicidade da validação.** A lista inteira é validada **antes** de salvar
  a série (all-or-nothing, D7): ou todos os atletas são válidos e todos
  recebem a série, ou nada é salvo e o erro é reportado (404/403 do primeiro
  atleta problemático, em ordem determinística).
- **Sem endpoint novo e sem consulta nova por atleta repetida:** o use case
  pode resolver existência + roster e assignments em lote (port de saída
  estendido ou chamadas existentes em laço — ver Impacto arquitetural).

## 7. Contrato HTTP (proposto)

Nenhum endpoint novo. Mudança no corpo de `POST /workout-series`:

```http
POST /workout-series
{
  "name": "Adaptação - Base A",
  "athleteIds": ["<guid-1>", "<guid-2>", "<guid-3>"],   // novo, opcional
  "athleteId": "<guid-1>",                              // legado, opcional, mantido
  "copyPreviousWorkouts": true
}
```

- `athleteIds` e `athleteId` são mesclados: `athleteId` presente entra na
  lista como primeiro elemento (D1). Só `athleteId` → comportamento idêntico
  ao de hoje (inclusive resposta DTO com `athleteId` preenchido — RF10).
- **201 Created** com `Location: /workout-series/{id}` e `WorkoutSeriesDto`
  como hoje (o DTO continua um por série; a lista de seguidores não entra na
  resposta — a visão "quem segue esta série" fica para o cliente resolver por
  roster/assignments, fora do escopo).
- **400** `workout_series.name.duplicate` — nome já usado pelo autor.
- **404** — algum `athleteIds[i]` não existe como usuário/atleta.
- **403** — algum `athleteIds[i]` fora da roster do treinador chamador.
- O `PUT /workout-series/{id}` permanece inalterado (o `AthleteId` que hoje
  carrega no corpo é ignorado pelo use case e segue ignorado).

## 8. Fluxo principal

1. Treinador monta o plano na tela do cliente e escolhe 3 atletas da roster.
2. Cliente envia `POST /workout-series` com `name` + `athleteIds` (3 GUIDs) +
   `copyPreviousWorkouts`.
3. API/execução do use case:
   a. Valida nome (único por autor).
   b. Deduplica a lista (3 atletas distintos aqui).
   c. Para cada atleta: existência + roster (em lote ou em laço).
   d. Cria a série com autor = treinador e `AthleteId = null` (2+ atletas — D2).
   e. `CopyPreviousWorkouts = true`: copia os workouts da sheet do ciclo
      anterior da fonte definida em D3 (primeira atleta com assignment
      existente, na ordem da lista deduplicada).
   f. Para cada atleta: reusa assignment (`Reassign`) ou cria (`Assign`),
      todos apontando à mesma `SeriesId`; salva.
4. Resposta **201** com o DTO da série.
5. Cada atleta abre `/workout-series/mine`, vê a própria série (a mesma
   `SeriesId`) e treina normalmente; o histórico dele segue por assignment.

### Fluxos alternativos e erros

- **1 atleta na lista:** igual ao today — série criada com `AthleteId`
  preenchido (RF10) e um assignment.
- **Lista vazia/ausente:** série sem vínculo imediato, como o today sem
  `AthleteId`.
- **Atleta inexistente na posição i:** 404 antes de salvar qualquer coisa
  (all-or-nothing, D7).
- **Atleta fora da roster (posição i):** 403, mesmo padrão sem revelar de quem
  é a roster (D7).
- **Atleta duplicado:** silenciosamente deduplicado (D4); sem erro.
- **Self-authored com lista:** `RequireAuthorAsync` valida a liberação; a
  lista é ignorada, série criada para o próprio atleta (RF9).
- **Assignment existente e fechado:** ainda reutilizado por `Reassign`, que
  substitui a série e reinicia o termo — mesma semântica do today.

## 9. Modelo de dados

- **Nenhuma tabela nova, nenhuma migração.**
  - `workout_series` — índice/campo existentes; `AthleteId` legado continua
    nullable (passa a ser `null` para 2+ atletas, D2).
  - `workout_series_assignments` — index único permanece **em `UserId`**
    (1 vínculo por atleta); nada impede N linhas com o mesmo `SeriesId`, que
    é exatamente o efeito desejado.
- Portas/DTOs tocados (sem novo agregado):
  - `CreateWorkoutSeriesCommand` — ganha `AthleteIds` (lista), mantém
    `AthleteId` para compat.
  - `CreateWorkoutSeriesRequest` (API) e o espelho cliente em
    `FitTrack.UI/Api/WorkoutSeriesRequests.cs` — ganham `AthleteIds`.
  - `IWorkoutSeriesAssignmentRepository` — pode ganhar lookup em lote
    (`GetByUserIdsAsync`) para evitar N consultas; a versão single
    (`GetByUserIdAsync`) continua para o caminho de 1 atleta.

## 10. Impacto arquitetural

| Projeto | Impacto |
| --- | --- |
| `FitTrack.Domain` | Nenhum agregado novo nem mudança de assinatura obrigatória; `WorkoutSeries.Create` segue com `athleteId` único (D2 define o valor). |
| `FitTrack.Application` | `CreateWorkoutSeriesCommand` + `CreateWorkoutSeries` reescritos no ramo trainer para laço de atletas; possível método em lote em `IWorkoutSeriesAssignmentRepository` (port out). |
| `FitTrack.Adapters.In.Api` | `CreateWorkoutSeriesRequest` com `AthleteIds`; mapeamento no endpoint monta o comando mesclando singular + lista. |
| `FitTrack.Adapters.Out.Persistence` | Se adotado lookup em lote, implementação do novo método no repositório de assignments (consulta por lista de `UserId`s). Nenhuma migração. |
| `FitTrack.UI` | Espelho do request no cliente (`FitTrack.UI/Api/WorkoutSeriesRequests.cs`); sem telas neste repositório. |
| Testes | Casos novos unitários/integração (ver Plano de testes). |

## 11. Plano de testes

### Unitários (`tests/FitTrack.Unit`)

- `CreateWorkoutSeries` com lista de N atletas: N assignments criados
  apontando ao mesmo `SeriesId`; um por atleta; termo de 60 dias por atleta.
- Lista com duplicado: deduplicado, 1 assignment para o atleta repetido.
- Lista com 1 atleta: `WorkoutSeries.AthleteId` preenchido (RF10).
- Lista com 2+ atletas: `WorkoutSeries.AthleteId` nulo (RF10/D2).
- All-or-nothing: primeiro atleta inválido (inexistente → 404; de outra
  roster → 403) e nenhuma série/assignment salvo.
- Assignment existente por atleta: `Reassign` — mesma série substituída e
  termo reiniciado, um reassign por vínculo.
- `CopyPreviousWorkouts` com múltiplos atletas: fonte e efeito conforme D3.
- Self-authored com lista: série criada para o próprio atleta, lista ignorada.
- Nome duplicado continua recusado no lote.

### Integração (`tests/FitTrack.Integration`)

- `POST /workout-series` com `athleteIds` (N>1): 201, `GET /workout-series/mine`
  de cada atleta devolve a mesma série; histórico/sessões de cada atleta
  seguem funcionando.
- `POST /workout-series` com só `athleteId`: resposta igual à de hoje
  (regressão de contrato).
- Atleta de outra roster na lista: 403 e série não criada (nenhum atleta
  recebe).
- Assignment pré-existente de um dos atletas: reassign persistido, sem
  segundo vínculo para o mesmo `UserId`.

### UI (`tests/FitTrack.UITests`)

- Limitado neste repositório (SPA esqueleto); cobrir o contrato do cliente
  espelhado (`FitTrack.UI/Api`) — request serializa `athleteIds` junto ao
  `athleteId`. Casos de tela ficam no repositório do cliente.

## 12. Riscos e dependências

- **Leituras que presumem 1 atleta por série.** Consultas e DTOs carregam
  `AthleteId` (linha singular). Com 2+ atletas e `AthleteId` nulo (D2), telas
  do treinador que filtram por "minha série do atleta X" via `WorkoutSeries.AthleteId`
  precisam passar a considerar assignments. Mitigação: listar séries do
  treinador por `TrainerId` (como hoje) e resolver pertença por roster.
- **`Reassign` em massa reinicia o termo de atletas que já treinavam outra
  série.** É a semântica atual do singular; ao generalizar o comportamento é
  o mesmo, só que para mais de uma pessoa por chamada.
- **`CopyPreviousWorkouts` ambíguo no lote** (D3): sheets de ciclos anteriores
  podem divergir por atleta; copiar "a do primeiro" é determinístico mas
  arbitrário — documentar e decidir.
- **Consulta por atleta em laço:** N chamadas de roster/assignments por
  request. Para N típico de roster (unidades) é aceitável; o lookup em lote é
  otimização, não requisito.
- **Compat de clientes:** clientes que enviam só `athleteId` não devem
  perceber mudança (D1 mescla e mantém comportamento).

## 13. Decisões em aberto

| ID | Decisão | Recomendação |
| --- | --- | --- |
| **D1** | Contrato: manter `athleteId` singular + `/athleteIds`, ou substituir por lista sozinha? | Aceitar ambos, mesclando o singular (primeiro da lista, deduplicado): zero quebra para clientes atuais; descontinuar o singular em versão futura. |
| **D2** | `WorkoutSeries.AthleteId` com 2+ atletas: `null` ou "primeiro atleta"? | `null` — o campo legado volta a significar "série de um único atleta"; preencher com o primeiro é information loss que confunde leitura/DTO. Risco: telas que leem só esse campo. |
| **D3** | `CopyPreviousWorkouts` com vários atletas: copia de quem? | Copiar da sheet do **primeiro atleta da lista deduplicada que tiver assignment existente** (ordem recebida, determinística), uma única vez; sem assignment em nenhum → sem cópia (mesmo comportamento do singular sem histórico). |
| **D4** | Atleta duplicado na lista: deduplicar ou recusar (400)? | Deduplicar silenciosamente (primeira ocorrência define posição): a intenção do treinador é inequívoca e o erro seria barulho. |
| **D5** | Limite no tamanho da lista? | Nenhum limite rígido; a roster do treinador já limita na prática. Revisar se telas passarem a prescrever para rosters grandes inteiras. |
| **D6** | Lookup de assignments/extensão em lote no port? | Adicionar `GetByUserIdsAsync` em `IWorkoutSeriesAssignmentRepository` já nesta fatia (evita N consultas e serve ao teste unitário com fake). |
| **D7** | Falha em um atleta: all-or-nothing ou prescrever aos válidos? | All-or-nothing: validação completa antes do primeiro `SaveAsync`; recusa com o erro do primeiro atleta problemático em ordem de lista. Prescrever parcialmente esconde falha do treinador. |

## 14. Sugestão de próximo passo

1. Arquiteto: validar D1–D7, em especial D2 (legado `AthleteId`) e D3 (fonte
   da cópia), e a extensão em lote do port de assignments (D6).
2. Implementação: comando + use case (laço com validação prévia), depois o
   contrato HTTP e o espelho do cliente (`FitTrack.UI/Api`), por último o
   lookup em lote no repositório.
3. Testador: cobrir os critérios de aceite do fluxo principal + regressão do
   contrato com `athleteId` singular antes do merge.
