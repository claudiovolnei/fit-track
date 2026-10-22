# Vencer a série atual (e gerar a próxima) + busca no select de vínculo do admin

- **Slug:** `vencer-serie-atual-gerar-nova-e-select-vinculo-busca`
- **Status:** implementado
- **Data:** 2026-10-21
- **Fatias:** A (vencer o ciclo atual e gerar a próxima, personal), B (busca no select de personal da tela de vínculos, admin)

> Resumo: hoje o personal já pode criar uma série nova para um atleta (o que reatribui o termo de 60 dias),
> mas não existe uma ação explícita de **encerrar o ciclo atual** — seja gerando o próximo em um único passo
> (reaproveitando os treinos da série vencida) ou apenas vencendo e deixando o atleta **sem série ativa**,
> para criar uma nova mais à frente. Paralelamente, o select de personal usado pelo admin na tela `/athletes`
> carrega apenas os 100 primeiros personais e não permite buscar, o que quebra a gestão quando há mais de
> 100 contas ou quando o personal desejado não está na primeira página.

## 1. Contexto e problema

### 1.1 Vencer a série atual

O termo de uma série é fixo em 60 dias (`WorkoutSeriesAssignment.TermInDays`).
Quando o termo vence, o atleta deixa de poder iniciar treinos novos (verificado em `StartWorkout.cs` e
em `Today.razor`: alerta "A série venceu…"). O personal hoje tem duas saídas manuais:

1. Criar uma série nova para o atleta em `/athletes` ("Nova série para este atleta"), que chama
   `POST /workout-series` com `AthleteId`: reatribui o termo à nova série. A série antiga fica no
   histórico, mas **não é marcada como vencida explicitamente** — o termo simplesmente passa a apontar
   para a nova série.
2. Renomear/editar a série existente, o que não reinicia o termo nem cria um novo ciclo.

Faltam dois comportamentos:

- **Vencer explicitamente** o ciclo atual, mesmo antes dos 60 dias, para registrar que o personal encerrou o
  planejamento daquele ciclo (e não apenas esperar a data).
- **Gerar a próxima série** reaproveitando os treinos A–E da série vencida, para que o personal não precise
  recriar a prescrição do zero a cada ciclo (progressão de carga, mesmos exercícios).

A tela `/athletes` (personal) já mostra o chip "Série vencida" quando `DaysRemaining == 0`, mas a única ação
disponível é "Nova série para este atleta", que cria uma série em branco.

### 1.2 Select de personal do admin sem busca

Na tela `/athletes`, quando o admin vincula um personal a um atleta, o painel inline carregava os personais
com `Api.GetUsersPageAsync(null, 1, 100, "PersonalTrainer")` e os exibia em um `<select>`. Problemas:

- Só os 100 primeiros personais eram carregados; acima disso o personal desejado não aparecia.
- Não havia campo de busca, então o admin precisava rolar a lista inteira.
- A API já aceitava `search` e `role` em `GET /admin/users`, mas a UI não usava o `search`.

## 2. Objetivo e valor

- **Personal:** encerrar o ciclo atual de um atleta, deixando-o **sem série ativa**, e gerar a próxima série
  quando for a hora, reaproveitando os treinos do ciclo que acabou, sem perder o histórico.
- **Admin:** localizar rapidamente o personal certo ao vincular, mesmo com muitas contas, buscando por
  e-mail/nome.

## 3. Escopo

### 3.1 Incluído

1. Ação "Vencer série atual" no cartão do atleta (visão do personal) para atletas com série vinculada, que
   encerra o ciclo e **deixa o atleta sem série ativa** (a próxima série pode ser criada depois, via
   "Nova série para este atleta").
2. A criação de série para o atleta ganhou a opção de **reaproveitar a folha do ciclo encerrado** ("Reaproveitar
   os treinos de {série}"), marcada por padrão quando existe um ciclo encerrado.
3. Endpoint/use case de vencer (`POST /athletes/{athleteId}/series/end`), que marca a atribuição atual como
   encerrada e deixa o atleta sem série ativa.
4. Select de personal do admin trocado por um **combobox com busca** (`SearchableSelect`), alimentado pela
   lista completa de personais (sem o corte de 100), filtrando por nome ou e-mail, com o estado
   "Nenhum personal encontrado".

### 3.2 Não incluído

- Endpoint dedicado de "vencer e gerar nova" em um único passo: o personal vence e depois cria a próxima
  série pelo formulário que já existe, com a folha reaproveitada. A composição é o fluxo natural do produto
  (vencer é uma decisão; a próxima série nasce quando o personal quiser).
- Histórico comparativo entre ciclos (relatório de evolução entre séries): as sessões já guardam a série
  referenciada; a comparação fica para um corte futuro.
- Clonagem parcial de treinos (escolher quais treinos A–E copiar): a clonagem é total ou nula.
- Mudanças no termo de 60 dias ou na regra de expiração.

## 4. Requisitos funcionais

**Personal**

1. No cartão do atleta (`Athletes.razor`, visão personal), quando `SeriesId is not null`, aparece o botão
   "Vencer série atual" (além de "Nova série para este atleta").
2. "Vencer série atual" pede confirmação (o botão vira "Confirmar?") e, ao confirmar, encerra o ciclo: o
   atleta fica **sem série ativa** (a tela passa a mostrar "Sem série vinculada" e a "Ciclo anterior: {nome}").
   A série antiga e as sessões já registradas permanecem intactas.
3. No formulário de "Nova série para este atleta", quando o atleta tem um ciclo encerrado, aparece a opção
   "Reaproveitar os treinos de {série}" (marcada por padrão). Ao confirmar, a API cria a nova série já com
   os treinos do ciclo encerrado (nomes + exercícios prescritos, incluindo `RestSeconds`), com ids novos, e
   reatribui o termo de 60 dias.
4. Se o ciclo já estiver encerrado, a ação não falha: vencer de novo não muda nada.
5. O personal só pode executar as ações para atletas do próprio roster; caso contrário, 403.

**Admin**

6. No painel de vinculação (`Athletes.razor`, visão admin), o `<select>` de personal vira um combobox com
   busca: digitar filtra por nome ou e-mail.
7. Se a busca não retornar personais, o combobox exibe "Nenhum personal encontrado" e o botão "Vincular"
   permanece desabilitado.
8. O carregamento inicial traz **todos** os personais (sem paginação), de modo que nenhum personal fique
   fora de alcance por causa do tamanho da lista.

## 5. Regras de negócio / invariantes

1. **Vencer é idempotente e não destrutivo:** encerrar um ciclo já encerrado não muda nada. A série antiga e
   as sessões já registradas não são apagadas nem reescritas (mesmo princípio de `ChangeSeriesWorkout`: a
   sessão guarda o id do treino, não uma cópia).
2. **Uma atribuição por atleta:** a atribuição continua sendo uma só por atleta (índice único em `UserId`).
   Ao criar a próxima série, a atribuição é reaproveitada (`Reassign`), o que reabre o ciclo.
3. **Ciclo encerrado é "sem série ativa":** o atleta não vê série alguma em `/today` e não consegue iniciar
   treinos; a leitura do roster mostra a série do ciclo anterior pelo nome, para o personal reconhecer de onde
   veio a folha.
4. **Clonagem total ou nula:** quando "Reaproveitar os treinos" está marcado, cada treino da série de origem
   é copiado (nome + exercícios prescritos, incluindo `RestSeconds`) para a nova série via `AddWorkout`, com
   ids novos. Quando desmarcado, a nova série nasce vazia, como hoje.
5. **Nome único por personal:** o nome da nova série segue a regra existente (`WorkoutSeriesName` +
   `NameTakenAsync`). Nome repetido responde `400 workout_series.name.duplicate` (mesmo código de
   `POST /workout-series`).
6. **Autoria:** a nova série é criada pelo personal autenticado (`TrainerId = _currentUser.Id`).
7. **Roster:** as ações são recusadas com 403 para atleta não vinculado ao personal ou vinculado a outro
   (mesma checagem de `CreateWorkoutSeries`).
8. **Treino em andamento:** encerrar o ciclo com um treino aberto no dia é recusado com 409
   `workout_series.assignment.running` — o treino em andamento é a única forma de o atleta finalizá-lo, e a
   série é o caminho até ele.
9. **Termo:** a nova atribuição reinicia o termo em 60 dias a partir de `now` (`Reassign`).

## 6. Contrato HTTP

- `POST /athletes/{athleteId:guid}/series/end` (policy `PersonalTrainerOnly`) → `204`: encerra o ciclo atual e
  deixa o atleta sem série ativa.
  Erros: `403` forbidden (não é do roster), `404` not_found (atleta sem série atual), `409`
  `workout_series.assignment.running` (treino em andamento hoje).

- `POST /workout-series` ganha o campo `copyPreviousWorkouts` (default `false`) no corpo
  `CreateWorkoutSeriesRequest(Name, AthleteId, CopyPreviousWorkouts)`: quando marcado, a série nasce com os
  treinos do ciclo que o atleta está deixando (ou do ciclo encerrado) e a atribuição reabre com o termo de 60 dias.
  Erros: os mesmos de hoje, mais `403` para atleta de outro roster e `400 workout_series.name.duplicate`.

- `GET /admin/users` já aceita `search` + `role`; a UI passa a chamar sem paginação
  (`?role=PersonalTrainer`) e filtra no cliente, com o combobox do app.

## 7. Fluxo principal

**Personal vencendo o ciclo**

1. Personal abre `/athletes`; no cartão do atleta com série vinculada vê "Vencer série atual".
2. Clica → o botão vira "Confirmar?" (confirmação em dois toques, o mesmo padrão de `ManageExercises`).
3. Confirma → `POST /athletes/{id}/series/end`.
4. O use case `EndWorkoutSeries`:
   a. Exige personal (`RequirePersonalTrainer`).
   b. Valida roster (403 se não for o próprio aluno).
   c. Carrega a atribuição atual (404 se não existir).
   d. Se já estiver encerrada, não faz nada (idempotente).
   e. Se o atleta tem treino em andamento hoje, recusa com 409.
   f. Marca o ciclo como encerrado (`Close(now)`) e salva.
5. A tela recarrega; o cartão passa a mostrar "Sem série vinculada" e "Ciclo anterior: {nome}"; o atleta vê
   "Nenhuma série vinculada" em `/today` e pode receber uma nova série depois via "Nova série para este atleta",
   com a folha reaproveitada.

**Personal gerando a próxima série**

1. No cartão do atleta sem série ativa, o personal abre "Nova série para este atleta".
2. O formulário mostra "Reaproveitar os treinos de {série}" (marcado por padrão).
3. Confirma → `POST /workout-series` com `copyPreviousWorkouts: true`.
4. O use case `CreateWorkoutSeries` cria a série, copia os treinos do ciclo de origem e reatribui o termo de
   60 dias; a tela abre a nova série para o personal ajustar as cargas.

**Admin buscando personal**

1. Admin clica "Vincular personal" no cartão do atleta.
2. O painel inline exibe o combobox com busca; a lista traz todos os personais.
3. Admin digita parte do nome/e-mail → a lista filtra na hora; o primeiro resultado fica selecionado.
4. Admin escolhe e clica "Vincular" → `POST /athletes/{id}/trainer` (fluxo existente).

## 8. Fluxos alternativos e erros

| Situação | Comportamento |
| --- | --- |
| Atleta sem série atual | O botão "Vencer série atual" não aparece; o formulário de nova série não oferece reaproveitar. |
| Ciclo já encerrado | "Vencer série atual" não muda nada; "Nova série para este atleta" reaproveita a folha do ciclo encerrado. |
| Personal de outro aluno | 403 `forbidden`; a UI exibe a mensagem genérica. |
| Nome duplicado | 400 `workout_series.name.duplicate`; a UI preserva o texto digitado. |
| Treino em andamento hoje | 409 `workout_series.assignment.running`; a UI traduz o motivo. |
| Clonagem com 5 treinos | A nova série nasce com 5 treinos (limite `MaxWorkouts`); o personal pode remover depois. |
| Busca de personal sem resultados | Combobox com "Nenhum personal encontrado"; botão "Vincular" desabilitado. |
| Atleta acessando `/today` durante o encerramento | A leitura usa a atribuição vigente; após o encerramento, o atleta passa a ver "Nenhuma série vinculada". |

## 9. Modelo de dados

Uma coluna nova, uma migração:

- `workout_series_assignments.closed_at timestamptz NULL` — o instante em que o personal encerrou o ciclo antes
  do termo. `NULL` significa que o atleta ainda segue a série. O índice único em `UserId` continua valendo: a
  atribuição é reaproveitada (reaberta) quando a próxima série é criada.
- `WorkoutSeriesAssignment` ganha `ClosedAt`, `IsOpen` e `Close(now)` (idempotente); `Reassign` reabre o ciclo
  (zera `ClosedAt`).
- `WorkoutSeries` não ganha construtor de cópia: a clonagem é feita no use case via `AddWorkout` com ids novos
  (mantém o agregado encapsulado).
- `WorkoutSeriesWorkout` e `PrescribedExercise` (incluindo `RestSeconds`) são copiados por valor, sem
  referência compartilhada (imutáveis na prática).

Migração: `20261021222919_SeriesCycleEnd` (adiciona `ClosedAt`).

## 10. Fluxo de telas

### 10.1 Personal — `/athletes`

- No cartão do atleta (visão personal, `CandidatesMode == false`), quando `SeriesId is not null`, aparece o
  botão "Vencer série atual" (`btn-secondary btn-sm`) ao lado de "Nova série para este atleta".
- O botão pede confirmação em dois toques: o rótulo vira "Confirmar?" até a ação acontecer.
- No cartão de um atleta sem série ativa, o chip "Ciclo anterior: {nome}" mostra de onde veio a folha.
- No formulário de "Nova série para este atleta", quando o atleta tem ciclo encerrado, aparece a opção
  "Reaproveitar os treinos de {série}" (marcada por padrão).

### 10.2 Admin — `/athletes`

- No painel inline de vinculação (`_linkingFor == athlete.UserId`), o `<select>` de personal vira o
  `SearchableSelect` do app: busca por nome ou e-mail, com o estado "Nenhum personal encontrado".
- A lista de personais vem sem paginação (`GET /admin/users?role=PersonalTrainer`), para que nenhum personal
  fique fora de alcance.

## 11. Critérios de aceite

1. Personal com atleta vinculado vê o botão "Vencer série atual".
2. "Vencer série atual" encerra o ciclo (204) e o cartão passa a mostrar "Sem série vinculada" e o nome do
   ciclo anterior; o atleta vê "Nenhuma série vinculada" em `/today`; a série antiga segue acessível no
   histórico.
3. Com "Reaproveitar os treinos" marcado, a nova série contém os mesmos treinos (nomes + prescrição,
   incluindo `RestSeconds`) da série de origem, com ids novos.
4. Com a opção desmarcada, a nova série nasce sem treinos.
5. Ciclo já encerrado: "Vencer série atual" não falha nem muda nada.
6. Personal de outro aluno recebe 403 ao tentar vencer.
7. Atleta sem série atual não vê o botão de vencer.
8. Treino em andamento hoje impede o encerramento (409 `workout_series.assignment.running`).
9. Nome duplicado retorna 400 `workout_series.name.duplicate` e a UI preserva o texto digitado.
10. Admin, ao vincular, busca o personal por nome ou e-mail; a lista traz todos os personais.
11. Busca sem resultados mostra "Nenhum personal encontrado" e o botão "Vincular" desabilitado.

## 12. Impacto arquitetural

| Projeto | Impacto |
| --- | --- |
| `FitTrack.Domain` | `WorkoutSeriesAssignment` ganha `ClosedAt`, `IsOpen` e `Close(now)` idempotente; `Reassign` reabre o ciclo. |
| `FitTrack.Application` | Novo use case `EndWorkoutSeries` + porta de entrada + comando `EndWorkoutSeriesCommand(AthleteId)`; `CreateWorkoutSeries` ganha `CopyPreviousWorkouts`; `IQueryUserSessions` ganha `HasRunningSessionAsync`; guard compartilhado `SeriesCycleGuard`. |
| `FitTrack.Adapters.Out.Persistence` | Coluna `ClosedAt` no registro, na configuração, no mapper e na migração; leituras de série do atleta e do roster passam a ignorar ciclos encerrados; `UserSessionQueries` implementa a consulta de treino em andamento. |
| `FitTrack.Adapters.In.Api` | Novo endpoint `POST /athletes/{athleteId}/series/end` (policy `PersonalTrainerOnly`); contrato `CreateWorkoutSeriesRequest` para a criação com reaproveitamento. |
| `FitTrack.UI` | `Athletes.razor` (personal: botão de vencer com confirmação, chip do ciclo anterior, opção de reaproveitar; admin: `SearchableSelect` no lugar do `<select>`); `FitTrackApiClient` com `EndSeriesAsync` e `GetUsersByRoleAsync`; `ErrorMessages` com o motivo do 409. |
| Testes | `SeriesCycleTests` (unit) e `SeriesCycleFlowTests` (integração). |

## 13. Plano de testes

### Unitários (`tests/FitTrack.Unit`)

- `SeriesCycleTests`: vencer encerra o ciclo e o atleta fica sem série; roster (403 para atleta de outro/sem
  vínculo); 404 sem série atual; 409 com treino em andamento; idempotência; criação reaproveitando a folha do
  ciclo encerrado; criação sem reaproveitar nasce vazia.

### Integração (`tests/FitTrack.Integration`)

- `SeriesCycleFlowTests`: vencer deixa o atleta sem série (204, `/workout-series/mine` vazio, roster com o
  ciclo anterior); a série seguinte nasce com a mesma folha; 403 para personal de outro roster e para o
  administrador.

### Validação manual

1. `dotnet run --project src/FitTrack.AppHost`; entrar como `trainer@fittrack.local`.
2. `/athletes`: atleta com série ativa → "Vencer série atual" → "Confirmar?" → o cartão passa a mostrar
   "Sem série vinculada" e "Ciclo anterior".
3. "Nova série para este atleta" → "Reaproveitar os treinos de {série}" marcado → criar → a nova série já
   vem com A–E.
4. Entrar como `admin@fittrack.local` em `/athletes`: "Vincular personal" → buscar por e-mail no combobox.

## 14. Riscos e dependências

- **Ciclo encerrado e treino aberto:** encerrar com um treino em andamento é recusado; se o personal insistir,
  o caminho é esperar o atleta finalizar.
- **Clonagem e limite de treinos:** se a série de origem tiver 5 treinos, a cópia atinge `MaxWorkouts`; o
  personal não poderá adicionar mais sem remover. Comportamento aceitável e documentado.
- **Concorrência:** dois encerramentos simultâneos para o mesmo atleta são idempotentes; duas criações de série
  simultâneas podem criar duas séries — a última reatribuição vence. Risco baixo (ação manual).
- **Lista de personais sem paginação:** o admin carrega todos os personais do papel; com um número muito alto de
  contas, a lista fica grande. Aceitável agora; uma busca no servidor é o corte futuro se doer.
- **Fora de escopo:** relatório comparativo entre ciclos, clonagem parcial, paginação no select, endpoint único
  de "vencer e gerar nova".

## 15. Decisões

| ID | Decisão | Motivo |
| --- | --- | --- |
| **D1** | Vencer antes do termo vencer é permitido? | Sim: o personal encerra o ciclo antecipadamente; a regra é idempotente e não destrutiva. |
| **D2** | O ciclo encerrado apaga a atribuição ou a marca? | Marca (`closed_at`): a atribuição continua sendo uma por atleta e guarda de qual série veio a folha. A leitura é que trata o ciclo encerrado como "sem série ativa". |
| **D3** | "Vencer e gerar nova" é endpoint dedicado? | Não: vencer e criar são dois momentos diferentes do trabalho do personal; a criação reaproveita a folha pelo formulário que já existe. |
| **D4** | Admin também pode encerrar ciclo? | Não: gestão de série é do personal; o admin só vincula personais. |
| **D5** | Busca do personal: no servidor ou no cliente? | No cliente, sobre a lista completa de personais: o `SearchableSelect` do app já filtra por nome e e-mail, e nenhum personal fica de fora por paginação. |
