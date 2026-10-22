# Check do exercício no front, modal de finalização com nível do treino e liberação do admin para montar o próprio treino

- **Slug:** `check-local-modal-nivel-e-liberacao-montar-treino`
- **Status:** implementado (2026-10-22)
- **Data:** 2026-10-22
- **Fatias:** três, na mesma entrega — (A) o check e os valores do exercício ficam no front e só o envio final vai ao servidor; (B) a finalização abre um modal com o nível do treino e as observações; (C) o admin libera o atleta a montar o próprio treino, sem personal vinculado.

## 1. Contexto e problema

### 1.1 O check do exercício vai ao servidor a cada clique

Hoje a tela `/today` (`src/FitTrack.UI/Pages/Today.razor`) marca o exercício em duas
interações separadas com o servidor:

| Interação na tela | Chamada | Efeito no servidor |
| --- | --- | --- |
| Clicar no check do exercício (`Today.razor` ~linha 192-197 → `ToggleAsync` ~linha 434) | `PUT /sessions/{id}/exercises/{exerciseId}/completion` | `MarkExerciseCompleted` grava o tick em `completed_exercises` |
| Clicar em "Registrar" (`Today.razor` ~linha 255-259 → `LogSetAsync` ~linha 417) | `POST /sessions/{id}/sets` | `LogPerformedSet` grava a série executada e marca o exercício |

O resultado é uma tela que faz uma requisição por clique — marcar, desmarcar, registrar — e um
atleta que precisa registrar as séries uma a uma para poder fechar o treino. O botão "Registrar"
também é redundante: os campos de repetições e carga já nascem com o que o personal prescreveu
(`Today.razor` ~linhas 532-533), e a única razão de existir é permitir que o atleta registre a
série **de cada vez**.

O pedido é o oposto disso: o check passa a ser **estado local da tela** e o registro some. O
atleta marca o que fez, o que o personal prescreveu continua valendo como padrão e só quando o
atleta **finaliza** o treino tudo é enviado de uma vez.

### 1.2 A finalização não tem nível nem observações

`POST /sessions/{id}/finish` (`SessionEndpoints.cs` linha 26) recebe um corpo vazio
(`FinishWorkoutAsync` na UI manda `new { }`). A sessão (`WorkoutSession`) guarda o relógio, as
séries executadas e os ticks — e nada sobre **como o treino foi**. O histórico
(`src/FitTrack.UI/Components/SessionCard.razor`) mostra duração, número de séries e volume, mas
não dá para saber se o treino foi leve ou intenso, nem o que o atleta sentiu.

O pedido é adicionar, no momento de finalizar, um **modal** com dois campos: um `select` de
**nível do treino** (leve, médio, moderado, intenso, muito intenso) e um campo de texto para
**escrever algo sobre o treino**.

### 1.3 O atleta sem personal não consegue montar o próprio treino

A criação de séries é vedada ao atleta:

- o grupo `/workout-series` da API está sob `Policies.PersonalTrainerOnly`
  (`src/FitTrack.Adapters.In.Api/Endpoints/WorkoutSeriesEndpoints.cs` linha 36-48);
- `CreateWorkoutSeries` exige `RequirePersonalTrainer` e, quando há atleta, exige que o atleta
  esteja na **rota do personal** (`CreateWorkoutSeries.cs` linha 72 e 74-83);
- `AddSeriesWorkout`, `ChangeSeriesWorkout`, `RemoveSeriesWorkout`, `RenameWorkoutSeries` e
  `GetWorkoutSeries` exigem `RequirePersonalTrainer` e que o autor da série (`TrainerId`) seja
  quem chama (`WorkoutSeriesAccess.RequireOwnedAsync`);
- a tela `/series` e `/series/{id}` (`src/FitTrack.UI/Pages/Series.razor` e `SeriesDetail.razor`)
  estão com `[Authorize(Roles = "PersonalTrainer")]`.

Resultado: um atleta que quer montar (ou apenas salvar) o próprio treino depende de um personal
vinculado. O pedido é dar ao **admin** a chave para liberar esse acesso: com a liberação, o atleta
monta e salva a própria série mesmo sem personal vinculado; sem ela, o atleta continua precisando
de um personal e não tem acesso a criar.

## 2. Objetivo e valor

- **Atleta:** marcar o exercício não custa uma requisição; o valor prescrito fica visível e é
  alterável com um clique; o treino fecha com um único envio, que inclui **nível** e
  **observações**.
- **Personal:** recebe um histórico que distingue um treino leve de um intenso e que preserva o
  relato do atleta sobre cada sessão.
- **Admin:** decide, por atleta, quem pode montar o próprio treino — liberando quem já tem um
  plano e quer só ajustá-lo, sem obrigar um personal vinculado.

## 3. Escopo

### 3.1 Incluído

**Fatia A — check e valores no front, envio único no final**

1. O check do exercício vira estado local da tela: marcar, desmarcar e editar repetições/carga
   não geram requisição nenhuma.
2. O botão "Registrar" (e o bloco `set-log` sempre visível) sai da tela. O valor prescrito é o
   valor exibido; o atleta clica para editar repetições e carga quando o que levantou foi diferente.
3. O `POST /sessions/{id}/sets` e o `PUT /sessions/{id}/exercises/{exerciseId}/completion` saem
   da superfície HTTP, junto dos use cases `LogPerformedSet` e `MarkExerciseCompleted`.
4. O `POST /sessions/{id}/finish` passa a receber o exercício concluído com os valores do atleta.

**Fatia B — modal de finalização**

5. "Finalizar treino" abre um modal com o `select` de nível (leve, médio, moderado, intenso, muito
   intenso) e o campo de observações.
6. A sessão guarda o nível e as observações; o histórico (`SessionCard.razor`) passa a exibi-los.

**Fatia C — liberação do admin**

7. A conta do atleta ganha a capacidade `CanAuthorOwnWorkout`, gravada e devolvida pela
   administração (`POST`/`DELETE /admin/users/{userId}/self-authoring`).
8. Com a liberação, o atleta cria a própria série (e a usa como plano ativo) e monta os treinos
   A–E, sem personal vinculado.
9. Sem a liberação, o atleta que tentar criar recebe `403` e a tela não oferece o caminho.

### 3.2 Não incluído

- **Sincronização intermediária:** o estado do treino em andamento continua volátil (decisão D1).
  Recarregar a página no meio do treino perde os checks marcados.
- **Exercício fora da prescrição:** o atleta deixa de poder registrar um exercício que o personal
  não prescreveu (hoje ele consegue, pelo "Registrar"). O conjunto de exercícios enviados é a
  prescrição vigente.
- **Edição do número de séries:** o atleta ajusta repetições e carga, não quantas séries fez
  (decisão D3).
- **Nível e observações no planejador do personal:** o trainer não escreve a prescrição do treino
  nem vê o relatório de aderência ao descanso.
- **Edição do nível/observações depois de finalizado:** a sessão finalizada é imutável, como já é
  hoje.
- **Filtro do histórico por nível:** o histórico não ganha filtro novo; o nível aparece no cartão.
- **Liberação em lote, por papel ou por série:** a liberação é por conta, no cartão do usuário.
- **Relatório de Intensidade por período, gráfico ou meta de intensidade.**
- **Plano próprio do personal** (o personal continua criando séries para os atletas da rota).

## 4. Requisitos funcionais

### Fatia A — check e valores no front

1. Em `/today`, o check de cada exercício prescrito alterna **sem chamada ao servidor**. A tela
   mantém o estado em memória (`_ticks`) e só desabilita "Finalizar treino" enquanto faltar
   exercício.
2. Ao marcar um exercício, ele passa a valer com os **valores padrão**: repetições e carga
   prescritas e o número de séries prescrito. Se o atleta já editou os valores, valem os editados.
3. O número de séries que a prescrição traz é o que fica registrado; a carga e as repetições são
   as do atleta (padrão = prescrição).
4. O texto da prescrição (`4 × 10 @ 40 kg`) é o valor exibido. Clicar no valor abre a edição de
   repetições e carga **inline**; o botão "Registrar" não existe mais.
5. Ao desmarcar, o exercício sai da lista do que será enviado; os valores editados permanecem
   memorizados para o caso de o atleta remarcar.
6. "Finalizar treino" só fica habilitado com **todos** os exercícios prescritos marcados
   (localmente). O texto de "faltam N exercícios" passa a contar o estado local.
7. Ao finalizar, o cliente envia **uma** requisição com a lista de exercícios concluídos (id,
   repetições, carga).

### Fatia B — modal de finalização

8. Clicar em "Finalizar treino" abre o modal de finalização; o modal traz:
   - `select` "Nível do treino" com as opções **Leve, Médio, Moderado, Intenso, Muito intenso**;
   - `textarea` "Sobre o treino" (opcional).
9. O botão de confirmar fica desabilitado enquanto nenhum nível estiver escolhido. O nível é
   **obrigatório**; as observações são opcionais (decisão D4).
10. Cancelar o modal devolve a tela ao treino em andamento, **sem** enviar nada.
11. Confirmar envia o nível, as observações e os exercícios concluídos numa única requisição; o
    modal fecha e a tela recarrega com o treino concluído.
12. O cartão do histórico passa a mostrar o nível (chip) e as observações (quando houver).

### Fatia C — liberação do admin

13. No cartão de cada conta em `/admin/users`, o admin encontra o controle de liberação de
    "montar o próprio treino", para as contas com o perfil de atleta. Conceder mostra um chip
    "Montagem própria liberada" no cartão; revogar remove o chip.
14. A lista de contas da administração devolve o estado da liberação (`canAuthorOwnWorkout`).
15. Com a liberação, o atleta tem o caminho "Meu plano" na navegação: cria a própria série
    (nome + reaproveitar a folha do ciclo anterior) e monta os treinos A–E com o editor de
    prescrição já existente.
16. A série criada pelo próprio atleta fica **ativa** para ele: o `GET /workout-series/mine` a
    devolve e o treino dela pode ser iniciado em `/today`.
17. Sem a liberação, `POST /workout-series` e as demais escritas de série respondem `403` para o
    atleta, e a tela "Meu plano" explica que ele precisa de um personal ou da liberação.

## 5. Regras de negócio / invariantes

1. **O check é do cliente, a verdade é do servidor.** Enquanto o treino não é finalizado, o
   servidor não sabe o que o atleta marcou. A regra "o treino só fecha com todos os exercícios
   prescritos marcados" deixa de ser lida do servidor e passa a ser **exigida no pedido de
   finalização**: o `FinishWorkout` recusa o pedido quando a lista de exercícios não cobre a prescrição
   (`409 session.finish.exercises.pending`).
2. **Só exercício prescrito entra.** O pedido de finalização é recusado (`400`
   `session.set.exercise.not_prescribed`) quando traz um exercício que não está na prescrição do
   treino da sessão.
3. **Faixas do executado:** repetições entre 1 e 100 (`set.repetitions.out_of_range`), carga ≥ 0
   (`set.load.negative`). São as mesmas faixas da série registrada hoje.
4. **Número de séries vem da prescrição.** Um exercício marcado gera `Sets` séries executadas, com
   as repetições e a carga do atleta. Um exercício de peso corporal vira carga `0`.
5. **O nível é obrigatório e fechado.** Cinco valores, sem texto livre: o valor é um
   `enum` do domínio (`WorkoutIntensity`), com valores numéricos explícitos para o banco.
6. **As observações são texto livre, opcional e curto.** Limite de 500 caracteres
   (`workout_session.notes.too_long`).
7. **A liberação é por conta e por atleta, e vale enquanto estiver ligada.** Ela é lida do banco a
   cada uso — não vive no token — para que a concessão/revogação valha na hora, sem novo login.
8. **A série autoral pertence ao atleta.** `WorkoutSeries.TrainerId` continua sendo o autor; para
   a série do próprio atleta, o autor é o próprio atleta (`TrainerId == AthleteId`). Assim as
   invariantes de autoria (`WorkoutSeriesAccess.RequireOwnedAsync`) continuam valendo sem
   exceção, e a série aparece na lista do próprio atleta pelo mesmo caminho do personal.
9. **Sem liberação, não há criação.** `CreateWorkoutSeries` só abre caminho para o atleta quando
   a liberação está ligada; o personal segue pelo caminho da rota, sem mudança.
10. **O personal não muda de comportamento.** Nenhuma regra do personal é alterada: ele continua
    vendo e editando só as séries que escreveu e os atletas da sua rota.

## 6. Contrato HTTP

### 6.1 Fatia A — finalização com o que foi feito

`POST /sessions/{sessionId}/finish` — **corpo novo** (antes era vazio):

```json
{
  "intensity": "moderado",
  "notes": "Senti o ombro no último treino.",
  "exercises": [
    { "exerciseId": "…", "repetitions": 10, "loadKilograms": 40 },
    { "exerciseId": "…", "repetitions": 8, "loadKilograms": 0 }
  ]
}
```

| Campo | Tipo | Regra |
| --- | --- | --- |
| `intensity` | string enum | obrigatório: `leve` \| `medio` \| `moderado` \| `intenso` \| `muitoIntenso` |
| `notes` | string? | opcional, no máximo 500 caracteres |
| `exercises` | array | obrigatório e não vazio; um item por exercício prescrito concluído |

Resposta `200` com o `WorkoutSessionDto` (agora com `intensity` e `notes`).

Erros:

| Situação | Resposta |
| --- | --- |
| `intensity` ausente ou fora da lista | `400` `session.finish.intensity.invalid` |
| `notes` com mais de 500 caracteres | `400` `workout_session.notes.too_long` |
| `exercises` vazio ou faltando exercício prescrito | `409` `session.finish.exercises.pending` |
| exercício fora da prescrição | `400` `session.set.exercise.not_prescribed` |
| repetições fora de 1–100 | `400` `set.repetitions.out_of_range` |
| carga negativa | `400` `set.load.negative` |
| treino já finalizado | `409` `session.finish.already_finished` |

### 6.2 Fatia B — leitura da sessão

`GET /sessions` e `GET /sessions/{id}` (o read model `WorkoutSessionDto` / `TodayWorkoutDto`)
ganham:

```json
{ "id": "…", "intensity": "intenso", "notes": "…", "…": "…" }
```

`intensity` é `null` nas sessões antigas (que não têm o dado) e `notes` é `null` quando o atleta
não escreveu nada.

### 6.3 Fatia C — liberação e criação

| Endpoint | Regra | Resposta |
| --- | --- | --- |
| `POST /admin/users/{userId}/self-authoring` | `Admin` | `204` (idempotente) |
| `DELETE /admin/users/{userId}/self-authoring` | `Admin` | `204` (idempotente) |
| `GET /admin/users` | `Admin` | cada conta ganha `canAuthorOwnWorkout` (bool) |
| `POST /workout-series` | `PersonalTrainer` **ou** atleta com liberação | `201` com a série |
| `POST/PUT/DELETE /workout-series/{seriesId}[/workouts/{workoutId}]` | autor da série **e** (personal **ou** atleta com liberação) | como hoje |
| `GET /workout-series` | `PersonalTrainer` **ou** atleta com liberação | lista só das séries do próprio autor |
| `GET /workout-series/{seriesId}` | autor da série **e** (personal **ou** atleta com liberação) | `403` para quem não pode montar séries; `404` para a série de outro autor |

Para o atleta, `POST /workout-series` ignora o `athleteId` do corpo: a série é sempre criada
para o próprio atleta, com ele como autor.

### 6.4 Endpoints removidos

| Endpoint | Motivo |
| --- | --- |
| `POST /sessions/{sessionId}/sets` | o registro por série foi para o front; tudo vai no final |
| `PUT /sessions/{sessionId}/exercises/{exerciseId}/completion` | o check é estado local até o final |

Ambos são removidos **junto** com os use cases `LogPerformedSet`/`ILogPerformedSet` e
`MarkExerciseCompleted`/`IMarkExerciseCompleted`. A API tem um único cliente (o app Blazor), então
não há consumidor externo quebrado (decisão D2).

## 7. Fluxo principal

**Atleta treinando (fatias A e B)**

1. Atleta entra em `/today`, inicia o treino A → `POST /sessions` (o servidor cria a sessão e o
   relógio corre no servidor).
2. A tela mostra cada exercício prescrito com o valor do personal (`4 × 10 @ 40 kg`) e o check
   **desmarcado**. Nenhuma requisição acontece ainda.
3. Atleta marca o check do primeiro exercício → o check muda na hora, o valor continua o da
   prescrição. Nenhuma requisição acontece.
4. Atleta fez 8 repetições em vez de 10 → clica no valor → o campo abre inline → digita 8 → o
   valor passa a ser `4 × 8 @ 40 kg`. Nenhuma requisição acontece.
5. Atleta marca os outros exercícios. O rodapé avisa "faltam 2 exercícios" e "Finalizar treino"
   fica desabilitado.
6. Atleta clica em "Finalizar treino" → abre o modal: `select` de nível (vazio) + "Sobre o
   treino". O botão de confirmar está desabilitado.
7. Atleta escolhe "Intenso", escreve "Senti o ombro no último treino." e confirma.
8. O cliente envia **uma** requisição: `POST /sessions/{id}/finish` com `{ intensity, notes,
   exercises }` — os exercícios marcados com as repetições e a carga que ele ajustou.
9. O servidor recarrega a prescrição, valida a cobertura, registra `Sets` séries por exercício
   com os valores do atleta, fecha a sessão com o nível e as observações, e devolve a sessão.
10. A tela recarrega: o treino aparece como concluído, com o resumo de séries e volume (agora
    contando as séries que o atleta registrou ao finalizar).

**Atleta montando o próprio treino (fatia C)**

11. Admin entra em `/admin/users`, acha a conta do atleta e clica em "Permitir montar o próprio
    treino" → `POST /admin/users/{userId}/self-authoring` → o cartão passa a mostrar o chip
    "Montagem própria liberada".
12. Atleta entra no app: o menu ganha "Meu plano" → `/meu-plano`.
13. Atleta clica em "Criar série", dá um nome (e marca "reaproveitar os treinos do ciclo
    anterior", se quiser manter a folha) → `POST /workout-series` → `201` com a série criada e já
    atribuída a ele.
14. Atleta abre a série → o editor de prescrição de `/series/{id}` (o mesmo do personal) → monta
    o treino A com os exercícios, séries, repetições, carga e descanso → "Salvar treino" →
    `POST /workout-series/{seriesId}/workouts`.
15. Atleta vai em `/today`: a série própria é a que aparece (o `GET /workout-series/mine` a
    devolve), com o banner "Montado por você" no lugar de "Personal …", e o treino pode ser
    iniciado.

## 8. Fluxos alternativos e erros

| Situação | Comportamento |
| --- | --- |
| Atleta marca o check e recarrega a página | Os checks se perdem (estado local). O treino continua "em andamento" no servidor; o atleta marca de novo. Ver decisão D1. |
| Atleta desmarca um exercício | Ele sai da lista do que será enviado; os valores editados ficam memorizados. |
| Atleta marca todos e o modal é cancelado | Nada é enviado; o treino continua em andamento e o check continua marcado. |
| Atleta escolhe um nível fora da lista (payload forjado) | `400 session.finish.intensity.invalid`. |
| Atleta escreve mais de 500 caracteres | `400 session.finish.notes.too_long`; o texto digitado é preservado na tela. |
| Atleta finaliza sem ter marcado todos os exercícios | `409 session.finish.exercises.pending`; a tela avisa quantos faltam. |
| O personal remove um exercício da prescrição com o treino em andamento | O exercício enviado não está mais na prescrição → `400 session.set.exercise.not_prescribed`. Ver decisão D6. |
| Atleta sem liberação chama `POST /workout-series` | `403` `forbidden`; a mensagem traduzida explica que é preciso um personal ou a liberação. |
| Admin revoga a liberação com o atleta na tela "Meu plano" | A próxima chamada da tela é `403`; a tela mostra o estado bloqueado. |
| Atleta com liberação tem também personal na rota | A série do personal é a ativa (`GET /workout-series/mine` devolve a atribuída); a série própria continua na lista "Meu plano", editável. Ver decisão D7. |
| Atleta com liberação cria a primeira série | Não há ciclo anterior: o "reaproveitar a folha" fica desmarcado e sem efeito. |
| Sessão antiga (sem nível) no histórico | O cartão mostra apenas a duração — sem chip de nível e sem observações. |
| Admin concede a liberação a uma conta sem perfil de atleta | A liberação fica gravada e passa a valer quando a conta receber o perfil de atleta. |

## 9. Modelo de dados

### 9.1 `workout_sessions` (fatia B)

Duas colunas novas, aditivas:

- `Intensity` — `integer`, **nula** (as sessões antigas não têm o dado). Gravada como o valor
  numérico do `enum WorkoutIntensity` (valores explícitos 1..5, para o ordinal não depender da
  ordem de declaração).
- `Notes` — `text`, **nula**, no máximo 500 caracteres (a faixa é invariante do domínio).

Migração EF Core `…_SessionIntensityAndNotes` em
`src/FitTrack.Adapters.Out.Persistence/Migrations/`; snapshot atualizado. Nenhuma tabela nova.

### 9.2 `app_users` (fatia C)

- `CanAuthorOwnWorkout` — `boolean`, `NOT NULL`, default `false`. A coluna fica na tabela do
  Identity porque a liberação é um atributo da **conta** (mesmo lugar do nome e do avatar), e não
  da série nem da rota.

Migração EF Core `…_AthleteSelfAuthoring` (ou a mesma migração da fatia B — ver decisão D8).

### 9.3 Value objects e read models tocados

- **Novo** `src/FitTrack.Domain/WorkoutExecution/WorkoutIntensity.cs` — `enum` com
  `Leve = 1, Medio = 2, Moderado = 3, Intenso = 4, MuitoIntenso = 5`.
- **Novo** `src/FitTrack.Domain/WorkoutExecution/WorkoutNotes.cs` — value object do texto livre,
  com `MaxLength = 500` (o `null` vira texto vazio).
- `WorkoutSession` — ganha `Intensity` (`WorkoutIntensity?`) e `Notes` (`WorkoutNotes?`); o
  `Finish` passa a receber os dois.
- `WorkoutSessionRecord`, `WorkoutSessionMapper`, `WorkoutSessionProjection` — leem e gravam as
  duas colunas.
- `WorkoutSessionDto`, `TodayWorkoutDto` — ganham `Intensity` e `Notes`.
- `FinishWorkoutCommand` — ganha `Intensity`, `Notes` e a lista de exercícios concluídos.
- `UserAccountDto` — ganha `CanAuthorOwnWorkout`.
- `MyWorkoutSeriesDto` — ganha `IsSelfAuthored` (a série foi escrita pelo próprio atleta), para
  o banner do `/today` não dizer "Personal" quando o autor é o próprio atleta.

### 9.4 Port novo

- **Novo** `src/FitTrack.Application/Ports/Out/IAccountPermissions.cs`:
  `Task<bool> CanAuthorOwnWorkoutAsync(UserId userId, CancellationToken)`. Implementado pelo
  adaptador de persistência lendo `AppUser.CanAuthorOwnWorkout`. É lido do banco a cada uso (não
  vive no token), para a concessão e a revogação valerem na hora.

## 10. Fluxo de telas

### 10.1 `/today` — o treino em andamento

- O bloco `set-log` (inputs de reps/kg + botão "Registrar") sai; no lugar, a linha do exercício
  mostra a prescrição como texto e um botão/`<span>` clicável que abre a edição inline das
  repetições e da carga.
- O check continua no mesmo lugar (à esquerda do nome), agora sem `ToggleAsync` — o `@bind:after`
  some e o estado passa a ser só local.
- O contador "faltam N exercícios" passa a ler o estado local (`_ticks`), não o `CompletedExerciseIds`
  do servidor (que agora só se preenche no final).
- Botão "Finalizar treino" → abre o modal.
- O banner da série: quando `IsSelfAuthored`, a linha do personal vira "Montado por você" e o
  avatar do personal some.

### 10.2 Modal de finalização (novo)

- `src/FitTrack.UI/Components/FinishWorkoutModal.razor`, no mesmo estilo do
  `RestTimerModal.razor`: overlay, título "Finalizar treino", `select` "Nível do treino"
  (opções: Leve, Médio, Moderado, Intenso, Muito intenso), `textarea` "Sobre o treino"
  (placeholder "Como foi o treino?"), botão "Finalizar" (desabilitado sem nível) e "Cancelar".
- O `select` começa vazio; as opções são as cinco, na ordem da lista.

### 10.3 `/admin/users`

- No cartão da conta, um botão "Permitir montar o próprio treino" (ou "Revogar montagem própria"),
  só para contas com o perfil de atleta; abaixo dos perfis, o chip "Montagem própria liberada"
  quando a liberação está ligada.

### 10.4 "Meu plano" do atleta (novo)

- `src/FitTrack.UI/Pages/MeuPlano.razor`, em `/meu-plano`: lista as séries do próprio atleta
  (mesmo endpoint e mesmo editor de `/series`), botão "Criar série" (modal com nome + "reaproveitar
  a folha do ciclo anterior") e, quando a API recusa (`403`), o estado bloqueado: "Para montar o
  próprio treino você precisa de um personal vinculado ou da liberação do administrador".
- `src/FitTrack.UI/Pages/Series.razor` e `SeriesDetail.razor`: o `[Authorize(Roles =
  "PersonalTrainer")]` vira `[Authorize]` e o texto passa a falar de "suas séries" (personal) ou
  "meus planos" (atleta).

### 10.5 Navegação

- `src/FitTrack.UI/Layout/NavOptions.cs`: novo item "Meu plano" (`/meu-plano`), grupo
  `NavGroup.Athlete`, `NavAuth.Roles` com `User`. A aba do mobile respeita o teto de 4: com o item
  novo, o excedente cai no painel "Mais" (decisão D9).

## 11. Critérios de aceite

**Fatia A — check e valores no front**

1. Dado um treino em andamento, quando o atleta marca o check de um exercício, então nenhuma
   requisição HTTP é disparada (verificável na aba Network) e o check aparece marcado.
2. Dado um exercício prescrito como `4 × 10 @ 40 kg`, quando o atleta marca o check sem editar,
   então o valor exibido continua `4 × 10 @ 40 kg`.
3. Dado o mesmo exercício, quando o atleta clica no valor, então os campos de repetições e carga
   abrem inline, e o botão "Registrar" não existe na tela.
4. Dado o exercício com as repetições ajustadas para 8, quando o atleta finaliza o treino, então a
   requisição de finalização leva `repetitions: 8` e `loadKilograms: 40`.
5. Dado um treino com 5 exercícios prescritos e 4 marcados, quando o atleta olha a tela, então
   "Finalizar treino" está desabilitado e o aviso diz "faltam 1 exercício".
6. Dado um treino com todos os exercícios marcados, quando o atleta finaliza, então **uma** única
   requisição é feita (`POST /sessions/{id}/finish`) e o histórico mostra `Sets` séries executadas
   por exercício, com as repetições e a carga enviadas.
7. Dado `POST /sessions/{id}/sets` e `PUT /sessions/{id}/exercises/{exerciseId}/completion`, quando
   qualquer cliente os chama, então a API responde `404` (rota removida).
8. Dado um pedido de finalização que não cobre todos os exercícios prescritos, quando o servidor
   valida, então a resposta é `409` com `session.finish.exercises.pending`.

**Fatia B — modal de finalização**

9. Dado um treino com todos os exercícios marcados, quando o atleta clica em "Finalizar treino",
   então abre um modal com o `select` de nível (5 opções) e o campo "Sobre o treino".
10. Dado o modal aberto sem nível escolhido, quando o atleta olha o botão de confirmar, então ele
    está desabilitado.
11. Dado o modal com "Intenso" e uma observação, quando o atleta confirma, então a requisição leva
    `intensity: "intenso"` e o texto, e o cartão do histórico mostra o chip do nível e a
    observação.
12. Dado um `intensity` inválido no payload, quando o servidor valida, então a resposta é `400`
    com `session.finish.intensity.invalid` e a mensagem aparece em português na tela.
13. Dado uma sessão antiga no histórico, quando o cartão abre, então não há chip de nível nem
    observação.

**Fatia C — liberação do admin**

14. Dado o admin na tela de usuários, quando ele concede a liberação a um atleta, então o cartão
    mostra o chip "Montagem própria liberada" e `GET /admin/users` devolve
    `canAuthorOwnWorkout: true`.
15. Dado um atleta **sem** liberação, quando ele chama `POST /workout-series`, então a resposta é
    `403`.
16. Dado um atleta **com** liberação e sem personal na rota, quando ele cria uma série pela tela
    "Meu plano", então a série é criada, atribuída a ele e aparece em `/today`.
17. Dado um atleta **com** liberação, quando ele monta um treino na série própria, então a
    prescrição salva igual à do personal (exercício, séries, repetições, carga, descanso).
18. Dado um personal, quando ele cria uma série para um atleta da rota, então nada muda: a
    liberação do atleta não interfere.
19. Dado o admin revogando a liberação, quando o atleta recarrega "Meu plano", então a tela mostra
    o estado bloqueado.

**Persistência**

20. As sessões antigas continuam legíveis: `intensity` nulo e `notes` nulo, sem erro de leitura.
21. As contas existentes nascem com `can_author_own_workout = false`: nenhum atleta ganha acesso
    por causa da migração.

## 12. Impacto arquitetural

| Projeto | Impacto |
| --- | --- |
| `FitTrack.Domain` | Novo `WorkoutIntensity` (enum) e `WorkoutNotes` (VO); `WorkoutSession` ganha os dois estados e o `Finish` novo. Nenhuma dependência nova. |
| `FitTrack.Application` | `FinishWorkout` reescrito (cobre a prescrição, aplica as séries, fecha com nível e observações); `LogPerformedSet` e `MarkExerciseCompleted` removidos; `CreateWorkoutSeries` ganha o caminho do atleta; `AddSeriesWorkout`, `ChangeSeriesWorkout`, `RemoveSeriesWorkout`, `RenameWorkoutSeries`, `GetWorkoutSeries`, `ListWorkoutSeries` trocam `RequirePersonalTrainer` pelo guard compartilhado; `EndWorkoutSeries` aceita o próprio atleta; novo port `IAccountPermissions`; DTOs e comandos ajustados. |
| `FitTrack.Adapters.Out.Persistence` | `AppUser` + `WorkoutSessionRecord` com as colunas novas; mappers, projection e queries; configuração EF; duas migrações. |
| `FitTrack.Adapters.In.Api` | `SessionEndpoints` perde dois routes e o `finish` ganha corpo; `WorkoutSeriesEndpoints` relaxa a policy; `AdminEndpoints` ganha a liberação; contratos novos (`FinishWorkoutRequest`). |
| `FitTrack.UI` | `Today.razor` (check local, edição inline, modal), `FinishWorkoutModal.razor` novo, `SessionCard.razor` (nível + observações), `AdminUsers.razor` (liberação), `MeuPlano.razor` novo, `Series.razor`/`SeriesDetail.razor` (acesso do atleta), `NavOptions.cs` (item "Meu plano"), `ErrorMessages.cs` (textos novos). |
| Testes | Contratos de integração (`WorkoutSessionResponse`, `UserAccountResponse`) e fakes unitários (`InMemoryWorkoutSeriesQueries`, `InMemoryUserQueries`) ganham os campos novos; suites de sessão e de série ajustadas. |

## 13. Plano de testes

### Unitários (`tests/FitTrack.Unit`)

- `WorkoutSessionTests`: finalizar com nível e observações; recusa de observações acima de 500
  (`workout_session.notes.too_long`); sessão sem nível (legado) continua válida.
- `WorkoutIntensity`: os cinco valores e o valor numérico explícito de cada um.
- `FinishWorkout` (fake de sessão + série): cobre a prescrição → `409`; exercício fora da
  prescrição → `400`; repetições/carga fora da faixa; sucesso → a sessão carrega as séries
  (`Sets` por exercício), o nível e as observações.
- `CreateWorkoutSeries`: atleta **sem** liberação → `ForbiddenException`; atleta **com** liberação →
  série com `TrainerId == AthleteId == o próprio atleta` e atribuição criada; personal → caminho
  atual intacto; personal com atleta de outra rota → `ForbiddenException` (recusa atual mantida).
- `AddSeriesWorkout` / `ChangeSeriesWorkout` / `RemoveSeriesWorkout` / `RenameWorkoutSeries`:
  atleta com liberação e série própria → permitido; atleta sem liberação → `ForbiddenException`;
  série de outro autor → `ForbiddenException`.
- `EndWorkoutSeries`: atleta com liberação fecha o próprio ciclo; personal fecha o do atleta da
  rota (comportamento atual).
- `ListUsers`/`InMemoryUserQueries`: o `UserAccountDto` devolve `CanAuthorOwnWorkout`.
- `NavOptionsTests`: o item "Meu plano" entra para o perfil de atleta e o teto de 4 abas do mobile
  continua valendo.

### Integração (`tests/FitTrack.Integration`)

- `SessionFlowTests`: o `POST /sessions/{id}/finish` com corpo completo → `200` com o nível e as
  observações; com `intensity` inválido → `400`; com `exercises` incompleta → `409`; com exercício
  fora da prescrição → `400`; as rotas removidas → `404`.
- `VolumeFlowTests`: o volume do histórico passa a contar as séries do exercício marcado com os
  valores enviados.
- `RosterFlowTests` / `SeriesFlowTests` (regressão): o personal continua criando, editando e
  removendo séries e treinos como antes.
- Novo `SelfAuthoringFlowTests`: admin concede → o atleta cria a série → monta o treino → o
  `/workout-series/mine` devolve a série própria; revogar → `403`.
- `ListEndpointsTests`: `GET /admin/users` devolve `canAuthorOwnWorkout`.

### Validação manual

1. `dotnet run --project src/FitTrack.AppHost`; entrar como `trainer@fittrack.local`, criar uma
   série para `athlete@fittrack.local` com um treino de 2 exercícios (4 × 10 @ 40 kg).
2. Entrar como `athlete@fittrack.local` → `/today` → iniciar → marcar um check (olhar o Network:
   nenhuma requisição) → clicar no valor e mudar as repetições → marcar o segundo → "Finalizar
   treino" → modal → escolher o nível, escrever a observação → confirmar → conferir o histórico
   (chip do nível, observação, 8 séries executadas, volume 8 × 4 × 40).
3. Entrar como `admin@fittrack.local` → `/admin/users` → liberar a montagem própria do atleta →
   entrar como o atleta → "Meu plano" → criar a série → montar o treino A → `/today` → o treino
   da série própria aparece → iniciar e finalizar.
4. Revogar a liberação no admin → recarregar "Meu plano" → estado bloqueado; `POST
   /workout-series` devolve `403`.

## 14. Riscos e dependências

- **Perda de estado no meio do treino:** o check vive no front, então recarregar a página (ou
  fechar a aba) perde o que foi marcado. É a consequência direta do pedido; a mitigação é o
 atleta marcar de novo. Ver decisão D1.
- **Quebra de contrato HTTP:** dois endpoints saem e o `finish` ganha corpo. Não há consumidor
  além do app Blazor; o deploy tem de acontecer com app e API no mesmo momento (o `render.yaml` faz
  API e cliente juntos).
- **Migrações:** duas colunas novas e uma coluna nova na tabela do Identity — aditivas, com
  default seguro (`false`). Rodar `dotnet ef database update` contra o banco local antes do
  commit (o AppHost aplica migrações no startup em Development).
- **O volume do histórico muda de forma:** antes contava as séries que o atleta registrou uma a
  uma; agora conta as séries da prescrição do exercício marcado. Comparativos antigos e novos
  ficam em bases diferentes (decisão D3).
- **Convivência personal × série própria:** o atleta pode ter as duas coisas; a série atribuída é
  a que "Hoje" mostra (decisão D7).
- **`[Authorize]` das páginas de série:** ao relaxar de `PersonalTrainer` para autenticado, a
  linha de defesa passa a ser o use case. Cada use case tocado tem que declarar a regra — é o
  mesmo padrão já usado em `/athletes` e `/workout-series/mine`.

## 15. Decisões em aberto

| ID | Decisão | Recomendação |
| --- | --- | --- |
| **D1** | O estado do treino em andamento (checks e valores editados) fica só no front, ou o cliente sincroniza em background? | Só no front, como pedido. O custo é perder os checks ao recarregar a página; a alternativa (guardar no `localStorage`) é um corte seguinte, se o time se incomoda. |
| **D2** | Remover `POST /sessions/{id}/sets` e `PUT /sessions/{id}/exercises/{exerciseId}/completion`, ou deixá-los vivos e sem uso? | Remover: o único cliente é o app Blazor, e endpoint morto vira dívida. Se houver consumidor externo, manter como deprecated. |
| **D3** | O número de séries executadas por exercício marcado vem da prescrição (`Sets`), ou o atleta informa quantas fez? | Vem da prescrição. O pedido fala em ajustar os valores, e as repetições/carga são os campos que já existem na tela. A série registrada à mão fica para um corte seguinte. |
| **D4** | O nível é obrigatório e as observações opcionais? | Nível obrigatório (lista fechada, um clique), observações opcionais com limite de 500 caracteres. |
| **D5** | Onde a liberação do admin fica gravada? | Coluna `CanAuthorOwnWorkout` em `app_users`: é um atributo da conta, ao lado do nome e do avatar, e vale para qualquer série futura da conta. |
| **D6** | O personal remove um exercício da prescrição enquanto o treino está em andamento: o que acontece com o check marcado no front? | O envio é recusado com `session.set.exercise.not_prescribed` e a tela recarrega com a prescrição vigente (o exercício some do treino em andamento). Alternativa: ignorar o exercício removido no pedido — mais amigável, porém esconde o conflito. |
| **D7** | Atleta com liberação e com personal na rota: qual plano "Hoje" mostra? | O atribuído (`GET /workout-series/mine`) — que é o do personal, enquanto o personal não encerrar o ciclo. A série própria continua editável em "Meu plano". **Revisto na implementação:** o índice único de `workout_series_assignments.user_id` garante **uma série por atleta**, e o `Reassign` de `CreateWorkoutSeries` já troca o plano. Então a série autoral **substitui** a do personal na atribuição: o atleta passa a seguir a própria, e a série do personal continua na lista do personal (com o autor intacto). Ver seção 17. |
| **D8** | Uma migração ou duas? | **Uma só** (`20261022133659_WorkoutIntensityAndSelfAuthoring`): as duas colunas da sessão e a coluna da conta saem juntas, porque o modelo do EF só conhece o estado final. Ver seção 17. |
| **D9** | "Meu plano" entra como aba do mobile ou fica no painel "Mais"? | Pelo teto de 4 abas, o excedente vai para o "Mais" — o registro de navegação decide sozinho, sem regra nova. |
| **D10** | A tela "Meu plano" é nova ou o `/series` do personal é compartilhado? | Nova (`/meu-plano`), com o editor de `/series/{id}` reaproveitado. O `/series` do personal continua voltado para o personal. |
| **D11** | O `select` de nível começa vazio ou com um valor padrão? | Vazio, com o confirmar desabilitado — o nível é uma informação que só o atleta tem. |
| **D12** | O nível e as observações entram no volume/relatórios do personal? | Não nesta fatia. O `AthleteWorkouts` (relatório do personal) mostra o cartão da sessão, que já passa a exibir o nível e a observação. |

## 16. README a atualizar na entrega

- **Stack / Current state**: o check do exercício e o registro por série viram estado local com
  envio único no final; a finalização passa a registrar nível e observações; a administração
  ganha a liberação de "montar o próprio treino".
- **HTTP surface**: remover `POST /sessions/{id}/sets` e `PUT
  /sessions/{id}/exercises/{exerciseId}/completion`; documentar o corpo novo de `POST
  /sessions/{id}/finish`; documentar `POST/DELETE /admin/users/{userId}/self-authoring`; o grupo
  `/workout-series` deixa de ser exclusivo do personal.
- **Bounded contexts / Domain model**: `workout_sessions` ganha `Intensity` e `Notes`; `app_users`
  ganha `CanAuthorOwnWorkout`; o diagrama `erDiagram` é atualizado.
- **Screens**: a captura de "Hoje" muda (sem botão "Registrar", com o modal de finalização).

## 17. O que a revisão de arquitetura mudou no plano

A avaliação do `arquiteto` (contra o código real, não contra o plano) encontrou três pontos que o
plano original não via. A implementação saiu assim:

### 17.1 A série autoral substitui a do personal (achado C1)

O plano original (D7 e requisito 16) supunha que as duas séries coexistissem. O banco diz o
contrário: `workout_series_assignments.user_id` tem **índice único** — uma série por atleta — e o
`Reassign` de `CreateWorkoutSeries` já troca o plano do atleta quando um personal cria a próxima
série.

A regra que o repositório já carrega ("uma série por atleta") é a mesma que o pedido pede: com a
liberação, o atleta monta a própria série, e ela passa a ser o plano ativo. A série do personal não
é apagada — continua na lista do personal, com o autor intacto — só deixa de ser o plano seguido.

O `CopyPreviousWorkouts` (o "reaproveitar a folha") é o caminho de quem já tem um treino e quer
só salvá-lo: a prescrição do personal vem junto e o atleta passa a ajustá-la.

A tela "Meu plano" diz isso antes de qualquer ação: "A série nova passa a ser o seu plano atual".

### 17.2 O check local sobrevive ao reload (achado C2b)

O `Today.razor` original sobrescrevia os ticks a cada `ReloadAsync` — e o `catch` do
`RunAsync` recarrega a tela depois de uma recusa. Com o check no front, um `409` do final apaga
o trabalho do atleta.

A implementação amarra os ticks locais à **sessão** (`_tickSession`): quando a sessão muda, a
marca começa de novo; enquanto ela é a mesma, um reload não mexe no que o atleta marcou. Um
treino **finalizado** mostra o que o servidor gravou (a verdade passa a ser do servidor).

### 17.3 O guard compartilhado tem nome e dono (achado A1)

O plano dizia "guard compartilhado" sem nomear. Ele virou
`WorkoutSeriesAccess.RequireAuthorAsync(currentUser, permissions, action, ct)`: personal passa
direto; atleta passa quando o administrador liberou; os demais recebem `403`. O mesmo guard é
usado pelos sete use cases da série (`CreateWorkoutSeries`, `ListWorkoutSeries`,
`GetWorkoutSeries`, `RenameWorkoutSeries`, `AddSeriesWorkout`, `ChangeSeriesWorkout`,
`RemoveSeriesWorkout`) e por `EndWorkoutSeries`, que também ganha o caminho do próprio atleta.

`WorkoutSeriesAccess.RequireOwnedAsync` deixou de dizer "A personal trainer only manages…": a
mensagem agora é "Only the author of a workout series can manage it", porque o autor pode ser o
próprio atleta.

### 17.4 Outros ajustes

| Achado | O que mudou |
| --- | --- |
| **C2** (escrita da liberação) | A escrita fica no adaptador de entrada, como o resto da administração: `AdminEndpoints` grava em `AppUser` pelo `UserManager`. `IAccountPermissions` é só leitura. |
| **A2** (grupo `cycles`) | O grupo `/athletes/{athleteId}/series` também foi relaxado para autenticado — o `EndWorkoutSeries` declara a própria regra. |
| **A3** (`TrainerName`) | `MyWorkoutSeriesDto` ganha `IsSelfAuthored`; a tela `/today` mostra "Montado por você" e não busca o avatar de ninguém. |
| **M3** (`WorkoutNotes`) | O `null` fica **fora** do value object (`WorkoutNotes?`): a coluna segue nula quando o atleta não escreveu nada, e as sessões antigas leem nulo. |
| **M4** (contrato do nível) | O adaptador traduz a string do HTTP (`leve`, `medio`, `moderado`, `intenso`, `muitoIntenso`) com um `switch` explícito: palavra desconhecida → `400 session.finish.intensity.invalid`. |
| **M5** (sessão sem prescrição) | Uma sessão sem plano fecha com o que o atleta enviou; as duas regras de prescrição só valem quando há prescrição. |
| **M6** (exercício removido) | A tela monta o envio a partir da **prescrição vigente**, então um exercício removido pelo personal simplesmente não vai no pedido — sem recusa e sem perder o treino. A regra `session.set.exercise.not_prescribed` continua no servidor, para o payload forjado. |
| **B13** (configuração do EF) | `AppUserConfiguration` novo: sem ele a coluna `can_author_own_workout` não sairia na migração. |
| **B14** (projeção) | `WorkoutSessionRow` carrega `Intensity` e `Notes`, e o `WorkoutSessionProjection` (usado pelas duas leituras) os repassa. |
| **A4** (`TodayWorkoutDto`) | Ganhou `Intensity` e `Notes` também: o cartão de "Hoje" mostra o nível do treino concluído, e o read model é o mesmo nas duas leituras. |

## 18. O que a revisão do diff mudou no plano

A revisão do diff (contra o código já escrito) encontrou dois defeitos que quebrariam a feature em
produção, e alguns ajustes de coerência:

| Achado | O que estava errado | O que mudou |
| --- | --- | --- |
| **CRÍTICO** | O `enum` do domínio (`WorkoutIntensity`) ia direto para o read model e o `System.Text.Json` o serializava como **número** — enquanto o cliente e o contrato o querem como **palavra**. A tela "Hoje" e o histórico desceriam com um erro de desserialização. | O read model passa a carregar `string?` e o vocabulário mora em `Application/Mapping/WorkoutIntensityMapping.cs`, com `ToContract` (domínio → palavra) e `FromContract` (palavra → domínio). O contrato, o cliente e o teste agora falam a mesma língua. |
| **CRÍTICO** | O modal escrevia no próprio `[Parameter]`, e o pai lia um campo que **nada preenchia** — as observações nunca saíam do navegador. Pior: o relógio de 1 s da tela re-renderiza a cada segundo e podia **zerar** o select e o texto que o atleta acabara de digitar. | O modal virou controlado: `Intensity`/`IntensityChanged` e `Notes`/`NotesChanged`, e a tela usa `@bind-Intensity`/`@bind-Notes`. O valor vive com a tela, e o re-render do relógio não mexe nele. |
| **ALTO** | O exercício de peso corporal aparecia como "0 kg" (o `null` da prescrição virava `0m` no estado local). | A linha do exercício guarda a carga prescrita (`PrescribedLoad`), e a ausência de carga continua "peso corporal". |
| **ALTO** | Dois testes de integração não passavam: um contava um catálogo com 8 exercícios com `Assert.Single`, e o outro esperava `404` onde a API responde `403` (o guard da liberação vem antes da leitura). | O primeiro usa o primeiro exercício do catálogo; o segundo espera `403` — e o plano (§6.3) foi corrigido para dizer `403` para quem não pode montar séries. |
| **MÉDIO** | O nível obrigatório vivia só no use case; a repetição fora de 1–100 não era validada no caminho sem prescrição; a liberação valia para qualquer conta, inclusive de administrador. | `WorkoutSession.Complete` exige o nível (o agregado carrega a invariante); a faixa das repetições é validada nos dois caminhos; `RequireAuthorAsync` exige o **perfil de atleta** além da liberação (`ICurrentUser.IsAthlete`). |
| **BAIXO** | Coluna `can_author_own_workout` fora do padrão das outras colunas da tabela do Identity; `RequirePersonalTrainer` e `UnmarkExerciseCompleted` sem nenhum chamador; "Meu plano" paginava só 50 séries. | A coluna chama `CanAuthorOwnWorkout`, como as outras da tabela; o código morto saiu; "Meu plano" lê a lista inteira. |

## 19. O que a suíte cobre (e o que ela não cobre)

`dotnet test` na entrega: **269 testes unitários** e **11 de integração** verdes (os 91 de integração
que precisam de PostgreSQL são pulados sem `FITTRACK_TEST_DB`).

Coberto por teste:

- o nível e as observações fazendo o ida e volta do mapper e da projeção (`SessionIntensityPersistenceTests`);
- a cobertura e a pertinência da prescrição, as faixas de repetição e carga, o nível obrigatório e
  a nota longa (`WorkoutSeriesExecutionTests`);
- a liberação: o atleta sem liberação é recusado, o liberado cria e compõe a própria série, a
  revogação vale na hora, e a série do atleta é o plano que ele segue (`WorkoutSeriesManagementTests`,
  `SeriesCycleTests`, `ListUsersTests`);
- o vocabulário do nível nos dois sentidos (`WorkoutIntensityMappingTests`).

**Cobertura de tela** (novo projeto `tests/FitTrack.UITests`, bUnit sobre uma API falsa): o check
não custa requisição (critério 1), o valor da prescrição permanece até o atleta clicar (2), a
edição inline abre com os valores prescritos (3), "Finalizar treino" fica desabilitado enquanto
falta exercício e a mensagem "Faltam N exercícios" aparece (5), o modal pergunta o nível e a nota
e só fecha com o nível escolhido (9, 10), o modal oferece as cinco opções (9), a observação
chega ao servidor (11) e a marca sobrevive a um final recusado (o achado C2b do arquiteto). O
cartão do histórico mostra o nível e a nota (13, 19) e a tela "Meu plano" tem o estado bloqueado
quando a API recusa a criação. São 16 testes no projeto novo.

A suíte de integração ganhou um conserto de higiene do ambiente: `SeriesRefusalTests` era a única
classe que precisa de PostgreSQL e não tinha o atributo que a pula, então ela falhava em vermelho
por falta de banco em vez de pular.
