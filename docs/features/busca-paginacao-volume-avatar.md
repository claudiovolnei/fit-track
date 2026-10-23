# Busca com paginação, volume comparado e avatar por usuário

- **Slug:** `busca-paginacao-volume-avatar`
- **Status:** especificação (pré-implementação)
- **Data:** 2026-10-20
- **Fatias:** A (busca + paginação), B (volume feito × prescrito), C (avatar após login)

## 1. Contexto e problema

### 1.1 Fatia A — listagens e dropdowns sem busca/paginação

As telas de leitura carregam coleções inteiras e algumas buscas são feitas no navegador:

| Tela | Rota | Fonte atual | Situação |
| --- | --- | --- | --- |
| Catálogo | `/exercises` | `GET /exercises` | busca client-side por nome/grupo e chips de grupo derivados da própria lista |
| Gerenciar catálogo | `/exercises/manage` | `GET /muscle-groups`, `GET /exercises` | sem busca e sem paginação nas duas listas |
| Séries | `/series` | `GET /workout-series` | sem busca e sem paginação |
| Atletas | `/athletes` | `GET /athletes` | sem busca e sem paginação |
| Usuários | `/admin/users` | `GET /admin/users` | sem busca e sem paginação; baixa avatar de todos os usuários |
| Meus treinos | `/workouts` | `GET /sessions` | sem busca e sem paginação |
| Treinos do atleta | `/athletes/{userId}/workouts` | `GET /athletes/{userId}/workouts` | sem busca e sem paginação |
| Série (treinos A–E) | `/series/{id}` | `GET /workout-series/{id}` | lista limitada a 5 treinos, sem filtro |

Os endpoints e ports de leitura atuais não aceitam filtro nem paginação:

- `src/FitTrack.Adapters.In.Api/Endpoints/ExerciseEndpoints.cs` (`ListAsync` sem parâmetros)
- `src/FitTrack.Adapters.In.Api/Endpoints/MuscleGroupEndpoints.cs`
- `src/FitTrack.Adapters.In.Api/Endpoints/WorkoutSeriesEndpoints.cs` (`/workout-series`, `/athletes`, `/athletes/{userId}/workouts`)
- `src/FitTrack.Adapters.In.Api/Endpoints/SessionEndpoints.cs`
- `src/FitTrack.Adapters.In.Api/Endpoints/AdminEndpoints.cs`
- Queries em `src/FitTrack.Adapters.Out.Persistence/Queries/` (`ExerciseQueries`, `MuscleGroupQueries`, `WorkoutSeriesQueries`, `AthleteQueries`, `UserQueries`, `UserSessionQueries`)

Dropdowns afetados:

- `src/FitTrack.UI/Components/PrescriptionEditor.razor`: `<select>` simples com o catálogo inteiro (o editor não agrupa por músculo hoje; recebe `Catalog` de `SeriesDetail.razor`).
- `src/FitTrack.UI/Pages/ManageExercises.razor`: seleção de grupo no cadastro de exercício e seleção de grupo por linha.
- `src/FitTrack.UI/Pages/Exercises.razor` e demais telas: filtros existentes são client-side.

### 1.2 Fatia B — volume não compara com a prescrição

`WorkoutSessionDto` (`src/FitTrack.Application/Dtos/WorkoutSessionDto.cs`) tem `WorkoutSeriesWorkoutId` e `PerformedSetDto` tem apenas `ExerciseId`, `LoadKilograms`, `Repetitions`. O volume exibido é sempre o executado:

- `src/FitTrack.UI/Components/SessionCard.razor`: `Σ(LoadKilograms × Repetitions)`.
- `src/FitTrack.UI/Pages/MyWorkouts.razor` e `AthleteWorkouts.razor`: somam o volume de todas as sessões no card "Volume".
- `src/FitTrack.UI/Pages/Today.razor` também exibe volume, mas fora do escopo desta fatia.

A prescrição existe em `WorkoutSeriesWorkout.Exercises` (`PrescribedExercise`: `Sets`, `Repetitions`, `LoadKilograms`) e é lida por `GET /workout-series/{id}` / `GET /workout-series/mine`, mas **não acompanha a sessão**. `LogPerformedSet` já valida que o exercício foi prescrito e copia as repetições da prescrição no momento do registro (`src/FitTrack.Application/UseCases/LogPerformedSet.cs`).

Consequência: o atleta vê quanto levantou, mas não vê quanto isso representa em relação ao que o treinador passou. Como a sessão guarda `WorkoutSeriesWorkoutId`, a comparação é possível sem migração.

### 1.3 Fatia C — avatar não carrega após login (diagnóstico confirmado)

- `src/FitTrack.UI/Layout/NavMenu.razor`: o avatar é buscado apenas em `OnAfterRenderAsync(firstRender)` e somente quando `_avatarUrl is null`. O `NavMenu` já está montado quando o usuário é anônimo; no login SPA (sem reload), `firstRender` é `false` e nenhuma busca acontece.
- `src/FitTrack.UI/Auth/UserAvatarState.cs`: serviço `Scoped` sem identidade do usuário e sem método de limpeza. Pode manter a imagem do usuário anterior.
- `src/FitTrack.UI/Auth/UserSession.cs`: `SignInAsync`/`SignOutAsync` só gravam/limpam o token; não tocam no estado do avatar.
- `src/FitTrack.UI/Pages/Account.razor`: também busca `/me/avatar` apenas no primeiro render e grava no estado.
- `src/FitTrack.UI/Setup/UiServiceCollectionExtensions.cs`: `UserAvatarState` registrado como `Scoped`.
- A imagem em si está correta no backend: `src/FitTrack.Adapters.In.Api/Endpoints/AvatarEndpoints.cs` (`GET /me/avatar`, `GET /users/{userId}/avatar`, `POST/DELETE /me/avatar`).

> Observação de escopo: a **aba mobile** (`tabbar` em `NavMenu.razor`) hoje mostra `AppIcon Name="user"`, não um avatar. Atender ao critério "aba mobile com a imagem do usuário" exige trocar esse ícone por `UserAvatar`. Ver decisão D8.

## 2. Objetivo e valor

- **A:** reduzir tempo de leitura e payload em listas que crescem (catálogo, usuários, histórico), mantendo o comportamento atual quando ninguém pede paginação.
- **B:** transformar o volume em indicador de progresso contra a prescrição, por sessão, por exercício e no total.
- **C:** garantir que o avatar exibido pertence sempre à conta autenticada, sem reload e sem chamadas redundantes.

## 3. Fatia A — busca e paginação

### 3.1 Escopo

- Busca por nome em todas as listagens da tabela 1.1, com _debounce_ e botão/atalho de limpar.
- Paginação server-side nas listas que podem crescer: exercícios, grupos musculares, séries, atletas, usuários, sessões e treinos de um atleta.
- Paginação/filtro client-side apenas onde a coleção é estruturalmente limitada: treinos A–E de uma série (`MaxWorkouts = 5` em `SeriesDetail.razor`) e opções de dropdown já carregadas na tela.
- Dropdowns pesquisáveis por nome.

### 3.2 Regras transversais

1. `search` é opcional, sofre `Trim()`, é comparado sem diferenciar maiúsculas/minúsculas e casa por **contém**.
2. Busca vazia ou só com espaços = sem filtro.
3. Toda mudança de `search` (ou de filtro de grupo) volta para `page = 1`.
4. Ordenação continua fixa por endpoint (não entra `sort` nesta entrega):
   - exercícios por `Name`;
   - grupos por `Name`;
   - séries por `Name`;
   - atletas por `Email`;
   - usuários por `Email` (como hoje);
   - sessões por `WorkoutDate` desc, depois `StartedAt` desc.
5. `totalCount` é calculado com o mesmo filtro, antes de `Skip/Take`.
6. Página além da última devolve `200` com `items: []` e os metadados corretos (não é 404).
7. Acentos não são normalizados nesta entrega (não há extensão `unaccent` no banco). "Supino" encontra "Supino", não "Súpino".
8. `search` limitado a 80 caracteres na borda; acima disso a API responde `400` (`search.too_long`). A UI usa `maxlength="80"`.

### 3.3 Contrato HTTP

#### 3.3.1 Parâmetros de query

Aplicados aos `GET` de coleção já existentes:

| Endpoint | `search` casa com | Filtros adicionais | Paginação |
| --- | --- | --- | --- |
| `GET /exercises` | `Exercise.Name` **ou** `MuscleGroup.Name` (preserva a UX atual da tela) | `muscleGroupId` (Guid opcional) | sim |
| `GET /muscle-groups` | `MuscleGroup.Name` | — | sim |
| `GET /workout-series` | `WorkoutSeries.Name` (apenas séries do personal autenticado) | — | sim |
| `GET /athletes` | `Email` (ver D4) | — | sim |
| `GET /admin/users` | `Name` **ou** `Email` | — | sim |
| `GET /sessions` | nome do treino da série (`WorkoutSeriesWorkout.Name`, via `WorkoutSeriesWorkoutId`) | — | sim |
| `GET /athletes/{userId}/workouts` | idem acima | — | sim |

Parâmetros comuns:

| Nome | Tipo | Regra |
| --- | --- | --- |
| `search` | string? | opcional, `Trim`, máximo 80 |
| `page` | int? | 1-based, `>= 1` |
| `pageSize` | int? | `1..100`; padrão `20` quando a paginação é pedida |
| `muscleGroupId` | Guid? | somente `/exercises`; ignorado se vazio |

Valores inválidos (`page < 1`, `pageSize < 1`, `pageSize > 100`) → `400` com `title = "pagination.invalid"` e `errors` nos campos `page`/`pageSize`. A UI nunca envia valores inválidos.

#### 3.3.2 Resposta paginada

Quando a paginação é solicitada (presença de `page` **ou** `pageSize`):

```json
{
  "items": [ /* ExerciseDto, MuscleGroupDto, WorkoutSeriesDto, AthleteDto, UserAccountDto, WorkoutSessionDto */ ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 137,
  "totalPages": 7,
  "hasPrevious": false,
  "hasNext": true
}
```

Regras:

- `totalPages = ceil(totalCount / pageSize)`; `0` quando `totalCount = 0`.
- `hasPrevious = page > 1`; `hasNext = page < totalPages`.
- `items` mantém exatamente o shape atual de cada DTO.
- Para `GET /sessions` e `GET /athletes/{userId}/workouts`, o envelope acrescenta `summary` (seção 4.2). Os demais não.

#### 3.3.3 Compatibilidade

**Modo compatível (sem parâmetros de paginação):**

```http
GET /exercises
GET /exercises?search=supino
```

Resposta `200` com o **array JSON atual**, com todos os itens (ou todos os filtrados por `search`), na mesma ordem de hoje. Nenhum cliente existente quebra — incluindo `Home.razor`, `Today.razor`, `SeriesDetail.razor` e os testes de integração que leem `IReadOnlyList<T>`.

**Modo paginado:** qualquer chamada com `page` ou `pageSize` recebe o envelope. A UI nova sempre envia `page`/`pageSize`, portanto usa apenas o modo paginado.

O modo compatível é uma ponte. Pode ser removido em um corte posterior, quando todos os consumidores enviarem paginação. A alternativa de envelope único (quebra de shape para chamadas sem parâmetros) está registrada como decisão D1.

#### 3.3.4 Erros

| Situação | Status | Corpo |
| --- | --- | --- |
| `search` > 80 | `400` | `ProblemDetails`, `title = "search.too_long"` |
| `page`/`pageSize` inválidos | `400` | `ProblemDetails`, `title = "pagination.invalid"` + `errors` |
| endpoint sem autorização | `401`/`403` | inalterado |

Novos códigos devem ser traduzidos em `src/FitTrack.UI/Api/ErrorMessages.cs`.

### 3.4 Fluxo de UI — listagens

Componentes compartilhados propostos em `src/FitTrack.UI/Components/`:

- `SearchField.razor`: `<input type="search">` com `aria-label`, `maxlength="80"`, botão "Limpar" e _debounce_ de 300 ms; `Enter` dispara imediatamente.
- `Pagination.razor`: "Anterior" / "Próxima", indicador "X–Y de Z" com `aria-live="polite"`, botões desabilitados nos limites; rótulos em português.
- Um `PagedResultResponse<T>` no `FitTrackApiClient`, mais métodos `...PageAsync(search, page, pageSize, ...)`; os métodos atuais sem parâmetros continuam para as telas que ainda carregam tudo (Home, Today, editor de prescrição).

Fluxo padrão de cada tela:

1. `OnInitializedAsync` → carrega `page = 1` com `pageSize` da tela.
2. Usuário digita → `_debounce` → volta a `page = 1` e recarrega.
3. Usuário clica em Anterior/Próxima → mantém `search` e recarrega.
4. Erro → mensagem em português via `ErrorMessages.Describe`, sem apagar o termo buscado.
5. Resultado vazio com busca ativa → "Nada encontrado" + ação "Limpar busca"; catálogo realmente vazio mantém o estado vazio atual.

Por tela:

- **`/exercises`**: `pageSize = 20`. Chips de grupo passam a vir de `GET /muscle-groups` (hoje são derivados de `_exercises`, o que quebraria com paginação) e o chip ativo vira `muscleGroupId` no request.
- **`/exercises/manage`**: duas listas independentes com busca e paginação (`pageSize = 20`). Grupos são poucos, mas seguem o mesmo contrato. Após criar/salvar, recarrega a página atual mantendo busca; se o item criado cair fora do filtro/página, exibe confirmação ("Exercício criado") em vez de "sumir" silenciosamente.
- **`/series`**: `pageSize = 12` (grid de cards). Renomear recarrega a página atual.
- **`/athletes`**: `pageSize = 12`. Busca por e-mail (ver D4).
- **`/admin/users`**: `pageSize = 12`. Avatares são baixados apenas para os usuários da página atual (hoje `ReloadAsync` percorre todos).
- **`/workouts`** e **`/athletes/{userId}/workouts`**: `pageSize = 10`. Cards de estatística usam o `summary` do envelope (todas as sessões filtradas, não só a página). Busca por nome do treino (ver D3).
- **`/series/{id}`**: sem paginação server-side (limite estrutural de 5 treinos). Campo de filtro por nome do treino, client-side, sobre `_workouts`. Se o limite mudar, a tela migra para o mesmo contrato das demais.

### 3.5 Fluxo de UI — dropdowns pesquisáveis

- **`PrescriptionEditor.razor`**: substituir o `<select>` por um combobox (`role="combobox"`, `aria-expanded`, `aria-activedescendant`, navegação por setas, `Enter` seleciona, `Esc` fecha). O texto digitado filtra `Catalog` por `Name` (sem diferenciar caixa) e exibe "Nome — Grupo muscular" para desambiguar. A opção selecionada permanece selecionada mesmo quando o filtro deixa de casá-la; o valor não é limpo ao digitar.
- **`ManageExercises.razor`**: os dois selects de grupo (cadastro e linha) ganham o mesmo componente, filtrando `_groups` por nome.
- **Fonte dos dados:** o editor continua recebendo o catálogo completo já carregado por `SeriesDetail.razor`; o filtro do dropdown é client-side, sem request por tecla. `Today.razor` também depende do catálogo completo e não muda nesta entrega.
- **Sem resultado:** opção desabilitada "Nenhum exercício encontrado" e manutenção do valor atual.
- **Acessibilidade:** label associada, foco visível, anúncio do resultado selecionado.
- **Formulário:** o `Enter` que escolhe a opção é interceptado no navegador (`wwwroot/js/combobox.js`) para que o `EditForm` ao redor não seja enviado antes de o Blazor tratar a tecla.

> O catálogo atual é pequeno e já é carregado integralmente em `SeriesDetail.razor`. Se o catálogo crescer a ponto de justificar type-ahead server-side, o componente deve consumir `GET /exercises?search=&pageSize=20`; ver decisão D2.

### 3.6 Critérios de aceite — Fatia A

1. `/exercises`, `/exercises/manage` (grupos e exercícios), `/series`, `/athletes`, `/admin/users`, `/workouts` e `/athletes/{userId}/workouts` exibem campo de busca por nome com _debounce_ e ação de limpar.
2. Todas essas telas exibem paginação com Anterior/Próxima e "X–Y de Z"; com 0 resultados, o indicador mostra "0 de 0" e o estado vazio em português.
3. Buscar e paginar funcionam na primeira carga e após mudar a busca; mudar a busca volta para a página 1.
4. `GET /exercises`, `/muscle-groups`, `/workout-series`, `/athletes`, `/admin/users`, `/sessions` e `/athletes/{userId}/workouts`, **sem parâmetros**, devolvem o mesmo array e a mesma ordem de antes (modo compatível).
5. Com `page`/`pageSize`, a resposta é o envelope de 3.3.2, com `totalCount` correto e `items` da página.
6. `search` é case-insensitive; `search` vazio não filtra; página além do fim devolve lista vazia, não erro.
7. O dropdown de exercício do editor de prescrição e os selects de grupo de `/exercises/manage` filtram por nome ao digitar, com teclado e ARIA.
8. Erros e estados vazios continuam em português; labels e `aria-label` existentes são preservados.
9. `/admin/users` baixa avatares somente dos usuários da página atual.
10. Nenhuma tela dispara request por tecla: busca só após o _debounce_ (300 ms) ou `Enter`.

## 4. Fatia B — volume feito × prescrito

### 4.1 Definições

Para cada exercício do treino:

- **Prescrito:** `Sets × Repetitions × LoadKilograms` da prescrição atual (`WorkoutSeriesWorkoutExerciseRecord`). `LoadKilograms = null` (corporal) conta como `0` no cálculo e recebe status próprio.
- **Feito:** `Σ(LoadKilograms × Repetitions)` das séries executadas daquele exercício na sessão.
- **Delta:** `Feito − Prescrito` (pode ser negativo).
- **Delta percentual:** `Delta / Prescrito × 100` quando `Prescrito > 0`; caso contrário `null` (a UI mostra só o absoluto).
- **Total da sessão:** soma dos itens acima para a união de exercícios prescritos e executados.
- **Total do histórico:** soma de todas as sessões no escopo do filtro (não apenas a página), mais a contagem de sessões sem prescrição.

A comparação é sempre **por exercício** e depois agregada. Não se compara a sessão inteira diretamente, porque prescrição e execução podem ter quantidades diferentes de séries.

### 4.2 Contrato de resposta (aditivo)

`WorkoutSessionDto` ganha uma propriedade anulável `Volume`:

```json
{
  "id": "…",
  "workoutDate": "2026-10-20",
  "startedAt": "…",
  "finishedAt": "…",
  "durationSeconds": 3120,
  "workoutSeriesWorkoutId": "…",
  "sets": [ { "exerciseId": "…", "loadKilograms": 60, "repetitions": 10 } ],
  "volume": {
    "performedKilograms": 800,
    "prescribedKilograms": 600,
    "deltaKilograms": 200,
    "deltaPercent": 33.33,
    "exercises": [
      {
        "exerciseId": "…",
        "exerciseName": "Supino reto",
        "performedKilograms": 800,
        "prescribedKilograms": 600,
        "deltaKilograms": 200,
        "deltaPercent": 33.33,
        "status": "matched"
      },
      {
        "exerciseId": "…",
        "exerciseName": "Prancha",
        "performedKilograms": 0,
        "prescribedKilograms": 0,
        "deltaKilograms": 0,
        "deltaPercent": null,
        "status": "bodyweight"
      }
    ]
  }
}
```

`status` (string estável, traduzida na UI):

| Valor | Significado |
| --- | --- |
| `matched` | prescrito com carga e executado |
| `bodyweight` | prescrição sem carga (`LoadKilograms = null`) |
| `notPerformed` | prescrito, nenhuma série executada |
| `notPrescribed` | executado, mas fora da prescrição atual (dado legado/ajuste manual) |

`volume` é `null` quando não há referência de prescrição: `workoutSeriesWorkoutId == null` ou o treino não existe mais. Nesses casos a UI mostra apenas o volume feito (comportamento atual).

Resumo no envelope de histórico (`/sessions` e `/athletes/{userId}/workouts` no modo paginado):

```json
{
  "items": [ /* … */ ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 57,
  "totalPages": 6,
  "summary": {
    "sessionCount": 57,
    "setCount": 420,
    "performedKilograms": 50000,
    "prescribedKilograms": 45000,
    "deltaKilograms": 5000,
    "deltaPercent": 11.11,
    "sessionsWithoutPrescription": 3
  }
}
```

`volume` também aparece no modo compatível (array), pois é apenas um campo novo no DTO e clientes antigos ignoram propriedades desconhecidas. Respostas de comando (`POST /sessions`, `POST /sessions/{id}/sets`, `POST /sessions/{id}/finish`) mantêm `volume = null` para não pagar a consulta de prescrição fora da leitura.

### 4.3 Regras e casos de borda

1. Sessão sem `WorkoutSeriesWorkoutId` (histórico legado): `volume = null`; UI mostra "Volume feito X · sem prescrição para comparar".
2. Treino removido da série (sem FK por design, `WorkoutSeriesWorkoutId` órfão): `volume = null`, sem erro.
3. Prescrição alterada depois da sessão: o comparativo usa a **prescrição atual**, não um snapshot histórico. Não há snapshot no modelo e não haverá migração nesta entrega (D5).
4. Prescrição corporal (`LoadKilograms = null`): entra com prescrito `0`, status `bodyweight`; o percentual fica `null` se o prescrito total for `0`.
5. Prescrito `0` explícito (carga zero permitida): mesmo tratamento do corporal para percentual; `deltaPercent = null`.
6. Exercício prescrito e não executado: `performed = 0`, delta negativo, status `notPerformed`.
7. Exercício executado e não prescrito: hoje `LogPerformedSet` recusa (`session.set.exercise.not_prescribed`), então só aparece em dados legados/ajuste manual. Entra no feito e no delta com status `notPrescribed`, para não esconder o número.
8. Sessão em andamento (`FinishedAt = null`): o volume é parcial. A UI rotula "Em andamento — parcial" e não compara percentual como se fosse resultado final (D10).
9. Sessões de hoje iniciadas e não finalizadas continuam na lista; nada muda em `StartWorkout`/`FinishWorkout`.
10. PRs e registro existentes não mudam de significado: a alteração é só de leitura/apresentação.
11. Arredondamento: volumes em `decimal`. A API devolve o valor bruto; a UI formata com `DisplayText.Volume` (kg até 999, toneladas a partir de 1000).
12. `deltaPercent` é devolvido com duas casas e formatado com no máximo uma casa na UI.

### 4.4 Fluxo de UI e textos

> **Nota (2026-10-22):** o modelo de comparação desta seção (tonelagem:
> feito versus prescrito em Σ carga × reps) foi substituído pela feature
> `historico-volume-por-exercicio`. Hoje cada exercício compara a carga média
> executada com a carga prescrita por set, o delta é a soma das diferenças de
> carga (ex.: prescrito 40 kg → feito 48 kg = +8 kg) e o painel "Aumento"
> mostra apenas o percentual. Os exemplos abaixo e o critério 4 (feito
> 2.400 kg / prescrito 2.000 kg) descrevem o modelo anterior.

- **`SessionCard.razor`** (usado por `/workouts` e `/athletes/{userId}/workouts`):
  - Com prescrição: `feito 1,5 t · prescrito 1,2 t · +300 kg (+25%)`, com cor de sucesso para delta positivo e de alerta para negativo.
  - Sem prescrição: `feito 1,5 t · sem prescrição para comparar`.
  - Em andamento: prefixo `parcial`.
  - Detalhe por exercício abaixo das séries, com uma linha por item de `volume.exercises`: `Supino reto — feito 800 kg · prescrito 600 kg · +200 kg (+33%)`; `Rosca — não executado · prescrito 200 kg`; `Prancha — prescrição sem carga · feito 0 kg`; `Exercício X — não prescrito · feito 100 kg`.
  - Séries individuais e seus `kg × reps` continuam iguais.
- **Cards de estatística** em `MyWorkouts.razor` e `AthleteWorkouts.razor`: "Volume" passa a "Volume feito", com subtexto "Prescrito 1,2 t · +25%" e um card "Aumento" com o delta absoluto/percentual. O total vem de `summary`.
- **Sinais:** `+` para positivo, `−` para negativo, `0` para neutro. Nunca mostrar `+0 kg` como ganho.
- **Português:** "feito", "prescrito", "aumento", "parcial", "sem prescrição", "não executado", "não prescrito", "prescrição sem carga".

### 4.5 Critérios de aceite — Fatia B

1. No histórico do atleta e na visão do personal, cada sessão com prescrição exibe feito, prescrito, delta absoluto e percentual.
2. O detalhe por exercício mostra a comparação para cada exercício prescrito e, quando houver dado legado, para o executado fora da prescrição, com os status de 4.2.
3. O total do histórico exibe feito, prescrito, delta e percentual, calculados sobre todas as sessões filtradas (não só a página).
4. Prescrição 4×10×50 kg + 4 séries executadas a 60 kg×10 → feito 2.400 kg, prescrito 2.000 kg, +400 kg (+20%).
5. Exercício prescrito e não executado aparece com delta negativo explícito.
6. Sessão sem `WorkoutSeriesWorkoutId` e sessão cujo treino foi removido continuam legíveis, sem delta e sem erro.
7. Exercício corporal não gera percentual; mostra "prescrição sem carga".
8. Sessão em andamento é marcada como parcial.
9. `GET /sessions` sem parâmetros continua devolvendo array e passa a incluir `volume` em cada item; clientes antigos continuam desserializando.
10. `POST /sessions`, `POST /sessions/{id}/sets` e `POST /sessions/{id}/finish` não mudam de contrato.

## 5. Fatia C — avatar por usuário após login

### 5.1 Causa confirmada

1. `NavMenu.OnAfterRenderAsync` só executa a busca em `firstRender`, mas o menu já foi renderizado anônimo; no login SPA `firstRender` é `false`.
2. `UserAvatarState` não tem identidade do usuário e não é limpo em `SignInAsync`/`SignOutAsync`.
3. `Account.razor` repete o mesmo gate de `firstRender`.
4. Não há reação à mudança de `AuthenticationState`; `UserAvatarState.Changed` só reage a upload/remoção feitos na própria tela.

### 5.2 Correção proposta

1. **`UserAvatarState`** passa a guardar:
   - `string? UserKey` (claim `sub`/`NameIdentifier` do usuário dono da imagem);
   - `string? AvatarUrl`;
   - `bool Loaded` (indica que a busca já foi feita, inclusive quando o resultado foi "sem avatar").
   - Métodos `Set(userKey, url)`, `Clear()` e evento `Changed`.
2. **`UserSession.SignInAsync` e `SignOutAsync`** chamam `UserAvatarState.Clear()`. O `UserSession` passa a depender do estado (serviço `Scoped`, mesma lifetime).
3. **`NavMenu`** assina `AuthenticationStateProvider.AuthenticationStateChanged` além de `UserAvatarState.Changed`:
   - estado anônimo → não busca;
   - autenticado e (`!Loaded` ou `UserKey` diferente do usuário atual) → busca `GET /me/avatar` uma vez e grava com a chave do usuário;
   - se a resposta for `404`, grava `null` com `Loaded = true` (não repete a chamada).
4. **`Account.razor`** lê primeiro de `UserAvatarState`; só chama `/me/avatar` quando o estado não tem a chave do usuário atual. Upload/remoção continuam chamando `Set`/`Clear` e refletem imediatamente no menu.
5. **Guarda de corrida:** a resposta assíncrona só é aplicada se a `UserKey` ainda for a do usuário que iniciou a busca (evita login A → logout → login B exibir A).
6. **Sem chamada por navegação:** `Loaded` + `UserKey` no serviço `Scoped` garantem uma busca por usuário autenticado; navegar entre páginas não refaz a chamada.
7. **Fallback:** `UserAvatar` já mostra iniciais quando `AvatarUrl` é nulo; nenhuma mudança necessária.
8. **Aba mobile:** trocar `AppIcon Name="user"` do link `/account` na `tabbar` por `UserAvatar` (tamanho ~22), mantendo `aria-label="Conta"` e o estado ativo do `NavLink` (D8).

### 5.3 Fluxos

- **Login (SPA, sem reload):** `Login.SubmitAsync` → `Session.SignInAsync` limpa o estado → `NotifyChanged` → `NavMenu` reage ao novo `AuthenticationState` → busca o avatar do novo usuário → menu, topbar e tela de conta exibem a imagem correta.
- **Logout:** `NavMenu.SignOutAsync` → `Session.SignOutAsync` limpa o token e o estado → menu anônimo; nenhum avatar antigo permanece.
- **Troca de usuário:** logout + login de B nunca mostra a imagem de A (chave + limpeza + guarda de corrida).
- **Sem avatar:** `404` → estado `Loaded` com `null` → iniciais corretas, sem request repetido.
- **Upload/remoção em `/account`:** `Set(dataUrl)`/`Set(null)` → `Changed` → menu e topbar atualizam no mesmo render.
- **Reload da página:** estado `Scoped` recriado; primeira carga autenticada busca uma vez.

### 5.4 Critérios de aceite — Fatia C

1. Login sem reload faz o menu, a topbar e `/account` exibirem a imagem do usuário autenticado.
2. Logout limpa o avatar; login seguinte nunca mostra a imagem do usuário anterior.
3. Usuário sem avatar vê as iniciais corretas do próprio e-mail em todas as superfícies.
4. Upload e remoção em `/account` refletem imediatamente no menu/topbar, sem reload.
5. Navegar entre telas não dispara nova chamada a `/me/avatar` (verificável no DevTools).
6. Trocar de usuário rapidamente (login A, logout, login B) não deixa a imagem de A em cache.
7. A aba mobile de conta exibe o avatar do usuário (se D8 for aprovado) e mantém `aria-label`.
8. Nenhuma mudança em `/me/avatar` ou `/users/{userId}/avatar` no backend.

## 6. Impactos por camada

| Camada | Impacto |
| --- | --- |
| **Domain** | Nenhum. Volume é leitura; nenhuma invariante muda. |
| **Application** | Novos DTOs de paginação (`PagedResult<T>`, `PageRequest`/critérios) e de volume (`WorkoutVolumeDto`, `ExerciseVolumeDto`, `WorkoutVolumeSummaryDto`). Assinaturas dos use cases de listagem e dos ports `IQuery*` passam a receber critério de busca/paginação. Cálculo do volume em um serviço/mapper de leitura testável, não no agregado. |
| **Persistence** | `ExerciseQueries`, `MuscleGroupQueries`, `AthleteQueries`, `UserQueries`, `WorkoutSeriesQueries` e `UserSessionQueries` ganham `Where` + `Count` + `OrderBy` + `Skip/Take`. Para volume e busca de sessão, um join em lote por `WorkoutSeriesWorkoutId` para os ids da página (evitar N+1). Sem migração. |
| **API** | Parâmetros de query nos endpoints existentes; envelope condicional; validação de `page`/`pageSize`/`search`; `volume` e `summary` aditivos. OpenAPI reflete os novos parâmetros. |
| **UI** | Componentes `SearchField`/`Pagination`/combobox; `PagedResultResponse<T>`; métodos paginados no `FitTrackApiClient`; alteração de 7 páginas; `SessionCard`; `UserAvatarState`/`UserSession`/`NavMenu`/`Account`. |
| **Testes** | Fakes de query precisam aceitar critérios; testes de integração de listagem precisam cobrir os dois modos; testes de tradução SQL para os novos filtros; testes puros do cálculo de volume; teste de unidade do estado do avatar (se o projeto UI for referenciável) + validação manual do fluxo SPA. |

## 7. Fora de escopo

- Novas entidades, tabelas ou migrações (não há snapshot de prescrição).
- Busca por texto livre em séries de exercícios ou por data no histórico.
- Ordenação configurável, filtros múltiplos ou facetas além de `muscleGroupId`.
- Type-ahead server-side nos dropdowns (o catálogo segue carregado por inteiro).
- Alterar o volume ou o layout de `Today.razor` e do dashboard `Home.razor`.
- Paginação de `/workout-series/{id}` (limite de 5 treinos).
- Refresh token, expiração de sessão ou mudanças no JWT.
- Upload/redimensionamento/validação de imagem (já implementados).
- PRs (não há conceito de PR no código atual).

## 8. Plano de testes

### Unitários (`tests/FitTrack.Unit`)

- `PagingRequest`/normalização: página padrão, `search` vazio, `pageSize` no limite.
- Cálculo de volume:
  - prescrito 4×10×50 e feito 4×60×10 → +400 kg / +20%;
  - exercício não executado → delta negativo;
  - prescrição corporal → status `bodyweight`, percentual nulo;
  - prescrito 0 → percentual nulo;
  - sessão sem `WorkoutSeriesWorkoutId` → `volume = null`;
  - união de exercícios prescritos/executados sem duplicar.
- `UserAvatarState`: `Set`/`Clear`, troca de `UserKey`, `Loaded`, disparo de `Changed`.
- Atualizar fakes (`InMemoryExerciseQueries`, `InMemoryUserSessionQueries`, etc.) para os novos critérios.

### Integração (`tests/FitTrack.Integration`)

- `GET /exercises?search=...&page=1&pageSize=...` → envelope, `totalCount`, ordem e filtro.
- `GET /exercises` sem parâmetros → array (compatibilidade), incluindo quando só há `search`.
- Demais endpoints: um teste de paginação e um de busca por endpoint (podem ser theory).
- Página além do fim → `200` com `items` vazio.
- `pageSize=0/101` → `400`.
- `QueryTranslationTests`: filtros `search` e o join de prescrição traduzem para SQL (Npgsql).
- Volume: sessão criada via `SeriesArrange` com prescrição conhecida e séries registradas; conferir `volume` e `summary`.
- Volume sem prescrição: sessão legada (inserida sem `WorkoutSeriesWorkoutId`) → `volume = null`.
- Regressão: `SessionFlowTests` e `CatalogFlowTests` continuam lendo arrays sem parâmetros.
- Avatar: os testes de backend existentes permanecem; o fluxo SPA é validado manualmente (não há bUnit no repositório).

## 9. Validação manual

Pré-requisito: `dotnet run --project src/FitTrack.AppHost` (web em `http://localhost:5090`, API em `http://localhost:5080`), com os usuários semeados do README.

**Fatia A**

1. Entrar como `trainer@fittrack.local` / `Trainer123$`.
2. `/exercises`: digitar parte de um nome, conferir _debounce_, limpar, trocar chip de grupo, navegar entre páginas e conferir "X–Y de Z".
3. `/exercises/manage`: buscar grupo e exercício, paginar, criar um exercício e conferir que a lista recarrega mantendo o filtro.
4. `/series`: buscar e paginar; renomear uma série e conferir que a página se mantém.
5. `/athletes`: buscar por e-mail e paginar.
6. Abrir uma série, adicionar um treino e digitar no dropdown de exercício; conferir filtro, teclado (setas/Enter/Esc) e seleção preservada.
7. Sair e entrar como `admin@fittrack.local` / `Admin123$`; `/admin/users`: buscar, paginar e conferir que só os avatares da página são baixados (DevTools → Network).
8. Entrar como `athlete@fittrack.local` / `Athlete123$`; `/workouts`: buscar pelo nome do treino, paginar e conferir os cards de estatística.

**Fatia B**

1. Como personal, criar/atribuir uma série com prescrição conhecida (ex.: 4×10 a 50 kg).
2. Como atleta, iniciar o treino, registrar 4 séries a 60 kg, finalizar.
3. `/workouts`: conferir `feito 2,4 t · prescrito 2 t · +400 kg (+20%)` e o detalhe por exercício.
4. Como personal, abrir `/athletes/{id}/workouts`: mesmo resultado e card de aumento.
5. Repetir com um exercício de prescrição corporal: sem percentual.
6. Verificar uma sessão antiga sem `WorkoutSeriesWorkoutId` (se existir na base): sem delta, sem erro.
7. Abrir uma sessão em andamento: rótulo "parcial".

**Fatia C**

1. Como usuário A com avatar, entrar; conferir menu, topbar e `/account`.
2. Sair e entrar como usuário B sem avatar; conferir iniciais de B e ausência da imagem de A.
3. Em `/account` de B, enviar uma foto; conferir atualização imediata no menu/topbar; remover e conferir as iniciais.
4. Navegar entre 5 telas e conferir no DevTools que não houve nova chamada a `/me/avatar`.
5. Recarregar a página autenticado e conferir que o avatar aparece.
6. Em viewport mobile, conferir a aba de conta com o avatar (se D8 aprovado).

## 10. Decisões em aberto

| ID | Decisão | Recomendação |
| --- | --- | --- |
| **D1** | Envelope único vs. modo compatível (array sem parâmetros) | Modo compatível nesta entrega; remover depois que todos os consumidores enviarem `page`/`pageSize`. |
| **D2** | Paginação/filtro server-side também nos dropdowns ou client-side sobre o catálogo carregado | Client-side agora (catálogo pequeno, uma chamada por tela); type-ahead server-side se o catálogo crescer. |
| **D3** | O que `search` significa no histórico (`/sessions`, `/athletes/{userId}/workouts`) | Nome do treino da série via `WorkoutSeriesWorkoutId`; sessões sem vínculo ficam fora quando há busca. |
| **D4** | Busca de atleta por e-mail (DTO não tem `Name`) vs. expor `Name` em `AthleteDto` | Buscar por e-mail agora; expor `Name` em corte separado, se o produto pedir. |
| **D5** | Comparar com a prescrição atual vs. congelar a prescrição na sessão | Atual agora; snapshot exigiria nova tabela/migração e fica fora de escopo. |
| **D6** | Carga prescrita nula (corporal) e prescrita zero no percentual | Tratar como 0 no volume e `deltaPercent = null`; status `bodyweight`. |
| **D7** | `summary` dentro do envelope de histórico vs. endpoint dedicado | Dentro do envelope; evita segunda chamada e mantém o total coerente com o filtro. |
| **D8** | Adicionar avatar à aba mobile e onde centralizar a busca do avatar | Adicionar `UserAvatar` na tabbar; busca centralizada no `NavMenu` reagindo a `AuthenticationStateChanged`, com estado por usuário. |
| **D9** | Tamanhos de página | 20 para listas, 12 para grids de cards, 10 para sessões; constantes por tela. |
| **D10** | Comparar volume de sessão em andamento | Exibir, mas rotulado "parcial", sem tratar como resultado final. |
