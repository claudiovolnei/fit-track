# Vínculo atleta ↔ personal, roster privado e tela de vínculos

- **Slug:** `vinculo-atleta-personal-privacidade`
- **Status:** implementado (2026-10-21). Fatias A–C concluídas; D concluído nos testes unitários (173 verdes)
  e de tradução (6 verdes). Os testes de integração foram escritos e compilam, mas **não foram
  executados** (Postgres offline no dia).
- **Data:** 2026-10-21
- **Fatias:** A (agregado de vínculo + persistência e migração), B (regras de aplicação nos use cases), C (API + tela de vínculos na UI), D (testes, seed e docs)

> Resumo: hoje qualquer personal enxerga **todos** os atletas e pode ler o histórico de treinos de
> qualquer um deles — não existe a entidade de vínculo entre perfil de atleta e perfil de personal.
> O plano introduz o vínculo (1 personal por atleta, confirmado com o usuário), restringe listagem de
> atletas, histórico de treinos e criação/reatribuição de séries aos alunos do próprio personal
> (admin permanece com acesso total), e dá a admin + personal uma tela única (a própria `/athletes`)
> para pesquisar atletas e vincular/desvincular. Confirmado com o usuário: modelo 1:1 (D1), escopo
> total no vínculo incluindo séries (D2) e esconder atletas sem personal da lista padrão (D3).

## 1. Contexto e problema

Estado atual das operações que tocam atleta:

| Operação | Caminho | Hoje | Regra alvo |
| --- | --- | --- | --- |
| Listar atletas | `GET /athletes` (`WorkoutSeriesEndpoints.cs` L17–21 → `ListAthletes.cs` → `AthleteQueries.cs`) | `PersonalTrainerOnly`; o personal recebe **todos** os atletas — `AthleteQueries.Athletes()` (L91–100) filtra apenas `Roles.User`, sem olhar quem é o treinador | personal: somente seus alunos; admin: todos, com indicativo do personal |
| Histórico do atleta | `GET /athletes/{id}/workouts` (`ListAthleteWorkouts.cs` L25/L35) | só exige `RequirePersonalTrainer` — qualquer personal lê o histórico de qualquer atleta (violação de privacidade) | personal: somente atletas vinculados a ele; admin: todos |
| Criar série para atleta | `POST /series` (`CreateWorkoutSeries.cs` L59–100) | o personal pode criar/reatribuir a série de **qualquer** atleta | somente para atletas vinculados a ele |
| Vínculo atleta ↔ personal | — | **não existe**. O único traço de relação é `WorkoutSeriesAssignment.TrainerId` (o *autor* da série), que nunca funcionou como controle de acesso | agregado próprio, pessoal do domínio Identity |

Perfil de atleta hoje é a conta com `Roles.User` (interpolação em `AthleteQueries.Athletes()`), e o
admin opera exclusivamente pela `/admin/users` (`AdminEndpoints.cs`, `AdminUsers.razor`) — ele hoje
nem alcança a tela `/athletes` (policy `PersonalTrainerOnly`, `NavOptions.cs` L87–89 no registro de
navegação e no `Athletes.razor` L2).

A tela `/athletes` continua sendo a porta de entrada do personal para criar séries por atleta
(`Athletes.razor` L91–125: formulário inline de série + link "Ver treinos"), então é ela que evolui
para tela de vínculos — em vez de inventar uma segunda rota de gestão.

## 2. Decisões (D1–D10)

| # | Decisão |
| --- | --- |
| **D1** | **1 personal por atleta** (índice único em `AthleteId`). Trocar de personal = desvincular e vincular de novo, ou reatribuir ao criar série (`WorkoutSeriesAssignment.Reassign` já existe). Confirmado com o usuário. |
| **D2** | **Escopo total do vínculo**: a regra "personal só vê os treinos dos seus atletas" vale para listar atletas, ler histórico **e** criar/reatribuir séries. O admin ignora o vínculo em todas as ações. Confirmado com o usuário. |
| **D3** | **Lista padrão esconde atletas sem personal.** A lista sem busca do personal mostra só o próprio roster; atleta ainda sem personal aparece apenas no modo de candidatos (busca de vínculo) e para o admin. Confirmado com o usuário. |
| **D4** | **Desvincular não apaga a série.** A prescrição vencida/vigente do atleta permanece (`WorkoutSeriesAssignment` intacto); o novo personal do atleta a substitui ao criar a própria série (reassign em `CreateWorkoutSeries.cs` L81–97). |
| **D5** | **Tela única** `/athletes` para admin e personal, comportamento por papel (3.5). Um agregado a mais no domínio Identity, não um segundo contexto. |
| **D6** | **Nomes**: agregado `TrainerAthleteAssignment` (espelha `WorkoutSeriesAssignment`), tabela `TrainerAthleteAssignments`, rotas `POST/DELETE /athletes/{athleteId}/trainer`, use cases `LinkAthlete`/`UnlinkAthlete`. API não fala "roster" — a UI chama de "alunos". |
| **D7** | **Semantics de conflito**: re-vincular o mesmo personal ao atleta é idempotente (204); atleta já vinculado a **outro** personal é `ConflictException` 409; desvincular atleta sem vínculo é 404; personal de outro aluno é `ForbiddenException` 403 (o middleware em `DomainExceptionMiddleware.cs` L42–46 já mapeia padrões). |
| **D8** | **Backfill na migração**: vínculos derivados de `WorkoutSeriesAssignments` × `WorkoutSeries.TrainerId`. Como a migração `SeriesPerAthlete` garante 1 assignment por atleta, o backfill gera no máximo 1 vínculo por atleta — o índice único de D1 sobrevive. |
| **D9** | O admin escolhe o personal alvo em um select: `GET /admin/users` ganha filtro por papel (`role=PersonalTrainer`), reaproveitando `ListUsers`/`UserQueries` em vez de criar um endpoint novo ou escrever e-mail à mão. |
| **D10** | `DevelopmentSeeder` passa a vincular o atleta semeado (`athlete@fittrack.local`) ao personal semeado (`trainer@fittrack.local`) e semeia um segundo atleta **sem** vínculo (`athlete2@fittrack.local`), mantendo os fluxos de teste existentes verdes e dando às integrações os dois estados (vinculado/não vinculado). |

## 3. Desenho por camada

### 3.1 Domínio e portas (Fatia A)

- Novo `src/FitTrack.Domain/Identity/TrainerAthleteAssignment.cs` — agregado mínimo:
  `Id`, `TrainerId`, `AthleteId`, `LinkedAt`; factory `Assign(id, trainerId, athleteId, linkedAt)`
  com invariantes triviais (ids não vazios), sem transição de estado (desfazer vínculo é remoção no
  repositório, como o cancelamento de sessão).
- Nova porta de saída `src/FitTrack.Application/Ports/Out/ITrainerAthleteAssignmentRepository.cs`:
  `GetByAthleteIdAsync(UserId athlete, CancellationToken)`, `SaveAsync(...)`, `RemoveAsync(...)`.

### 3.2 Persistência (Fatia A)

| Mudança | Arquivo |
| --- | --- |
| Registro `TrainerAthleteAssignmentRecord` (Id, TrainerId, AthleteId, LinkedAt) | `Records/` |
| Configuração: PK Guid, índice **único** em `AthleteId` (D1), FKs para `AspNetUsers` de `TrainerId`/`AthleteId` | `Configuration/TrainerAthleteAssignmentRecordConfiguration.cs` |
| `DbSet` no contexto | `FitTrackDbContext.cs` |
| Migração `TrainerAthleteLink` + backfill (D8): `INSERT … SELECT s.TrainerId, a.UserId FROM WorkoutSeriesAssignments a JOIN WorkoutSeries s ON … WHERE NOT EXISTS (vínculo)` | `Migrations/` |
| Queries: escopo por personal + candidatos. `IQueryAthletes` ganha: `ListAsync(now, query, trainerId)` (roster do personal), filtro `unlinkedOnly` por consulta e `AthleteDto` ganha `PersonalTrainerEmail` (exibição do admin). `ExistsPersonalTrainerAsync(UserId)` para validar o personal alvo (ou reusa de `IQueryUsers` — a verificação é síncrona e barata para o EF) | `Queries/AthleteQueries.cs`, `Application/Ports/Out/IQueryAthletes.cs` |

### 3.3 Aplicação (Fatia B)

- `CurrentUserExtensions.cs`: novo `RequirePersonalTrainerOrAdmin(this ICurrentUser, string action)` —
  a tela de vínculos exige um dos dois papéis, e os use cases relevantes mudam de strict-para-pessoal
  para híbrido sem abrir mão do erro uniforme ( preamble do arquivo, L5–8).
- Novas portas de entrada `ILinkAthlete` / `IUnlinkAthlete` + comandos `LinkAthleteCommand(AthleteId,
  PersonalTrainerId?)` e `UnlinkAthleteCommand(AthleteId)` (o `PersonalTrainerId` é nulo para o
  personal, que só pode vincular a si mesmo; obrigatório para o admin — D6).
- Novos usos `LinkAthlete.cs` / `UnlinkAthlete.cs` com a tabela de erros (D7):

| Código | HTTP | Quando |
| --- | --- | --- |
| `not_found` | 404 (`NotFoundException`) | atleta não existe / não tem perfil de atleta; personal alvo não existe ou não é `PersonalTrainer`; desvincular atleta sem vínculo |
| `roster.busy` | 409 (`ConflictException`) | atleta já vinculado a outro personal |
| `forbidden` | 403 (`ForbiddenException`) | personal tenta vincular (com trainerId alheio) ou desvincular aluno de outro |
| `roster.trainer.required` | 400 (`DomainException`) | admin vincula sem informar o personal |
| 204 + idempotência | 204 | re-vincular o mesmo personal |

> Nota pós-implementação: os casos 403/404 usam os códigos genéricos do middleware
> (`forbidden`/`not_found`), alinhados ao padrão do resto da API — em vez dos códigos específicos
> `roster.not_own`/`roster.not_linked` imaginados antes do código. O contrato efetivo é este da tabela.

- Alterações em use cases existentes:
  - `ListAthletes.cs`: muda para `RequirePersonalTrainerOrAdmin`; persiste consulta `unlinkedOnly`
    (candidatos) e, quando personal (admin não) o filtro de escopo `trainerId = _currentUser.Id` (D3).
  - `ListAthleteWorkouts.cs`: admin passa direto; personal consulta o vínculo
    (`ITrainerAthleteAssignmentRepository.GetByAthleteIdAsync`) e só segue com
    `link.TrainerId == _currentUser.Id` — `ForbiddenException` caso contrário (sem achado, sem a
    revelação de que "outro personal" existe, mensagem genérica).
  - `CreateWorkoutSeries.cs`: na frente de `AthleteId`, mesma checagem de vínculo (D2), bloqueando
    série nova **e** reassign para atleta de outro personal.

### 3.4 API e contratos (Fatia C)

- `Policies.cs`: nova constante `TrainerOrAdmin`; Program.cs L113–115 registra com
  `RequireRole(Roles.Admin, Roles.PersonalTrainer)`.
- `WorkoutSeriesEndpoints.cs`:
  - grupo `/athletes` passa a `Policies.TrainerOrAdmin`;
  - `GET /athletes` ganha `bool? candidates` (modo de busca de candidato);
  - `POST /athletes/{athleteId:guid}/trainer` com `LinkTrainerRequest(PersonalTrainerId?)` → 204;
  - `DELETE /athletes/{athleteId:guid}/trainer` → 204.
- `AdminEndpoints.cs`: `GET /admin/users` ganha `role` opcional (D9), apenas o filtro de papel — sem
  mudanças nos contratos de resposta (`UserAccountResponse` já existe).
- Contratos em `Contracts/`: `LinkTrainerRequest`.

### 3.5 UI (Fatia C)

| Comportamento por papel em `/athletes` | Personal | Admin |
| --- | --- | --- |
| Lista padrão (sem busca) | somente o próprio roster (com série/dias restantes, como hoje) | todos os atletas + chip `Personal: X` ou `Sem personal` |
| Busca | com termo, alterna para candidatos: atletas **sem personal** (D3) + card com botão "Vincular a mim"; quem tem personal não aparece | busca em todos os atletas; no candidato, select de personal (populado via `GET /admin/users?role=PersonalTrainer`, D9) + "Vincular" |
| Desvincular | botão de perigo no próprio aluno, com confirmação | em qualquer cartão com vínculo |
| Criar série / ver treinos | mantém o que existe (só para o roster) | não aparece (gestão de série continua do personal) |

- `FitTrackApiClient.cs`: `LinkTrainerAsync(Guid athleteId, Guid? trainerId)`,
  `UnlinkTrainerAsync(Guid athleteId)`, parâmetro `candidates` e filtro `role` na lista de usuários
  administrativos que já existe.
- `Athletes.razor`: `@attribute [Authorize(Roles = "PersonalTrainer,Admin")]`, modos por papel
  (tabela acima), estado de candidatos com verbond de vínculo por atleta, confirmação na alteração de
  vínculo, chip de personal no card do admin.
- `NavOptions.cs` L87–89: item "athletes" passa a `Roles = [PersonalTrainer, Admin]` (o teto de
  abas mobile se mantém pelo registro); `NavOptionsTests.cs` atualizado.
- `ErrorMessages.cs`: entradas pt-br para `roster.busy` e `roster.trainer.required`; os 403/404 caem
  nas mensagens genéricas do fallback.

### 3.6 Seed (Fatia C/D)

`DevelopmentSeeder.cs`: após criar as contas (L33–35), vincular `AthleteEmail → TrainerEmail` e criar
`athlete2@fittrack.local` sem vínculo (D10). O seed segue escrevendo direto no contexto/repositorios
(pelo registro, "no authenticated caller").

## 4. Fatias executáveis

### Fatia A — agregado e persistência
1. Agregado `TrainerAthleteAssignment` + unit test de domínio (`tests/FitTrack.Unit/Domain/`).
2. Porta `ITrainerAthleteAssignmentRepository` + adaptador em `Repositories/`.
3. Record + configuração (índice único) + DbSet no contexto + migração com backfill (D8).
4. Queries: escopo por personal, `PersonalTrainerEmail` no DTO e modo candidato (contratos
   atualizados com compatibilidade: default é o comportamento de roster, de modo que a integração
   existente não quebre na fatia separada).

### Fatia B — regras de aplicação
1. `RequirePersonalTrainerOrAdmin` em `CurrentUserExtensions.cs`.
2. `LinkAthlete` / `UnlinkAthlete` + portas de entrada + comandos.
3. Escopo de `ListAthletes` (D3), guarda do histórico `ListAthleteWorkouts` e do
   `CreateWorkoutSeries` (D2).

### Fatia C — API e tela
1. `Policies.TrainerOrAdmin` ativação em Program.cs e no grupo `/athletes`.
2. Rotas de vínculo + `candidates` + filtro `role` no `/admin/users` (D9).
3. Cliente HTTP e `Athletes.razor` por papel (3.5) + navegação (NavOptions) + `ErrorMessages`.
4. Seed D10.

### Fatia D — testes e docs
1. Unit: `InMemoryTrainerAthleteRepository` + testes de `LinkAthlete`/`UnlinkAthlete` (tabela de
   erros de D7), escopo de `ListAthletes`, privacidade de `ListAthleteWorkouts`, guarda de
   `CreateWorkoutSeries`, `NavOptionsTests`.
2. Integration: `RosterFlowTests` (admin vincula/desvincula qualquer um; personal vincula a si;
   conflito 409 D7; personal não lê histórico de atleta de outro personal — 403; personal de outro
   aluno falha 403). Ajustar `SeriesArrange.cs` e `Prescriptions.cs` para vincular antes de criar
   série (D10 cuida de parte disso no seed).
3. Testes de tradução de query (QueryTranslation Tests) para o filtro de vínculo.
4. README: fluxo de atletas/personal + tabela de rotas (skill `readme-update`).

## 5. Critérios de aceite

- [ ] Personal autenticado em `GET /athletes` recebe apenas os atletas vinculados a ele (mesmo que existam outros atletas no banco).
- [ ] Personal lê `GET /athletes/{id}/workouts` do próprio aluno (200) e recebe 403 para atleta vinculado a outro personal ou sem personal.
- [ ] Admin lê histórico e o roster completo de qualquer atleta (200).
- [ ] `POST /athletes/{id}/trainer`: personal sem corpo de trainer vincula a si mesmo (204; re-pedido também 204); atleta de outro personal → 409; admin informando trainer id de personal válido → 204; personal informando trainerId de terceiros → 403.
- [ ] `DELETE /athletes/{id}/trainer`: personal remove só o próprio aluno (204), aluno de outro → 403, sem vínculo → 404; admin remove de qualquer atleta (204).
- [ ] Desvincular não apaga a série atribuída ao atleta (D4) — a série segue com o mesmo prazo; o novo personal a troca ao criar a própria.
- [ ] Personal não consegue criar/reatribuir série para atleta não vinculado a ele (403), com texto pt-br claro na UI (`ErrorMessages`).
- [ ] Migração `TrainerAthleteLink` roda sobre banco com dados vigentes e gera os vínculos do backfill (D8), mantendo o índice único.
- [ ] Admin vê, em `/athletes`, chip "Personal: X"/"Sem personal" e consegue vincular escolhendo o personal no select (D9), filtrado por papel.
- [ ] Atleta com vínculo semeado acessa Hoje/Histórico normalmente; os fluxos atuais de teste seguem verdes após o seed D10.

## 6. Estado da implementação (2026-10-21)

- **Fatia A**: agregado `TrainerAthleteAssignment`/`TrainerAthleteAssignmentId` em `Domain/Identity`,
  porta `ITrainerAthleteAssignmentRepository`, record + configuração (índice único em `AthleteId`,
  índice em `TrainerId`), `DbSet` e DI em `OutAdapterRegistration`. Duas migrações:
  `20261021150319_PrescriptionRestSeconds` (drift preexistente do plano de treino, isolado) e
  `20261021151019_TrainerAthleteLink` (tabela + backfill SQL; colunas PascalCase entre aspas, padrão
  do projeto). Snapshot regenerado.
- **Fatia B**: `RequirePersonalTrainerOrAdmin`; `LinkAthlete`/`UnlinkAthlete` com a tabela D7;
  `ListAthletes` role-scoped (roster do personal, candidatos, tudo para o admin);
  `ListAthleteWorkouts` e `CreateWorkoutSeries` com a guarda de vínculo (admin ignora, D2). O
  `AthleteDto`/`AthleteResponse` ganharam `PersonalTrainerEmail`.
- **Fatia C**: `Policies.TrainerOrAdmin` + grupo `/athletes`; rotas `candidates` e
  `POST/DELETE /athletes/{id}/trainer`; `GET /admin/users?role=` (D9); cliente HTTP e
  `Athletes.razor` reescrita por papel (roster/busca-candidatos para o personal, tudo + select de
  personal para o admin); `NavOptions` com `[PersonalTrainer, Admin]`; seed D10
  (`athlete@fittrack.local` → `trainer@fittrack.local`, `athlete2@fittrack.local` sem vínculo).
- **Fatia D**: fake `InMemoryTrainerAthleteAssignmentRepository` (+ `SeedLink`), queries falsas com
  rosto de vínculo, **unit 173/173 verdes** (15 novos em `LinkAthleteTests`), integração
  compila com `RosterFlowTests` (8 cenários sobre Postgres) e os arranjos existentes criam o
  vínculo dentro de `AthleteHandle.CreateAsync`; `QueryTranslationTests` cobre roster/candidatos
  (6 verdes, sem banco). **Integrações ainda não executadas** — Postgres estava offline.

## 7. Riscos e observações

- **Quebras conhecidas nas integrações**: quem cria séries por `SeriesArrange`/`Prescriptions` sem
  vínculo passa a falhar com 403 — a ordem das fatias B → D concentra o ajuste, e o seed (D10) protege
  o caminho principal. Cobrir em `RosterFlowTests` antes de mexer nos fluxos antigos.
- **Papéis compostos**: conta `Admin + PersonalTrainer` enxerga a própria roster quando atua como
  personal e tudo quando atua como admin — a decisão "admin ignora vínculo" (D2) resolve a dupla
  habilidade a favor do admin; a UI segue o papel mais forte na exibição (padrão `DisplayText.Roles`
  já usado em `NavMenu.razor` L231).
- **Índice único na migração**: o backfill de D8 precisa rodar antes da criação do índice único ou
  usar `DISTINCT ON (UserId)` no insert — decidir na migração conforme volume (backfill Migration
  style do projeto: código SQL gerado no `Up`).
- **Fora de escopo**: N:N entre personal e atleta; auto-atendimento do atleta escolhendo personal
  (convite/aprovação); notificações; apagar séries ao desvincular.
