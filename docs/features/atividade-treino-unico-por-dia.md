# Atividade: check por exercício, séries com repetição e carga próprias, um treino por dia

- **Slug:** `atividade-treino-unico-por-dia`
- **Status:** implementado (2026-10-21)
- **Data:** 2026-10-21
- **Fatia única:** a tela Hoje vira a "atividade" do atleta — ele marca cada exercício que fez, registra
  cada série com a repetição e a carga que realmente fez, e o dia fecha com um único treino.

## 1. Contexto e problema

A tela `/today` (`src/FitTrack.UI/Pages/Today.razor`) tratava o treino como uma lista de campos de
carga: o atleta informava o peso e o servidor devolvia as repetições da prescrição. Três problemas
apareceram no uso real:

1. **O check do exercício não existe.** O atleta não tem como dizer "já fiz este exercício" sem
   registrar uma carga — e registrar a carga é opcional na prática (exercício de peso corporal,
   exercício pulado por decisão do personal). Sem marcação, não há como saber o que falta.
2. **A repetição é engessada.** A prescrição fixa as repetições (`3 × 12`), mas o atleta costuma fazer
   menos na última série (`2 × 12`). A série registrada ficava com a repetição da prescrição, e o
   histórico mentia sobre o esforço real.
3. **O dia aceita vários treinos.** A regra era "uma sessão por treino por dia": o mesmo atleta
   podia iniciar o treino A e o treino B no mesmo dia, e o histórico mostrava dois treinos.

Fora da atividade, dois problemas de ergonomia afetam toda a aplicação:

4. **Erro como faixa dentro da tela.** Cada página carregava seu próprio bloco `alert--error`
   (`_error`), que empurrava o conteúdo e some ao trocar de tela.
5. **Request sem bloqueio.** Nada impedia o segundo clique enquanto o servidor não respondia, e nada
   dizia ao atleta que a tela estava esperando o servidor.

## 2. Objetivo e valor

- **Atleta:** marcar cada exercício que concluiu (check), registrar cada série com a repetição e a
  carga que fez (mudando só o que precisa), ver o que já registrou em "2x 30kg", e fechar o treino
  sabendo o que falta.
- **Atleta:** um treino por dia — com o treino concluído, o próximo fica para amanhã.
- **Time:** erro sempre no mesmo lugar (toast no canto da tela) e a tela travada enquanto o servidor
  responde.

## 3. Escopo

### 3.1 Incluído

1. Check por exercício prescrito na tela Hoje, com o estado guardado na sessão.
2. Campos de **repetições** e **carga** por exercício, pré-preenchidos com a prescrição: o atleta só
   muda o que realmente foi diferente.
3. Série registrada com a repetição e a carga informadas (a repetição continua caindo para a
   prescrição quando o campo fica vazio).
4. Formato de leitura da série registrada: "2x 30kg" (repetições × carga).
5. Erros de servidor em toast no canto da tela, em todas as páginas.
6. Overlay que bloqueia a tela enquanto há request em voo.
7. Regra de um treino por dia (rodando ou concluído), na API e na tela.
8. Finalização só com todos os exercícios prescritos marcados.

### 3.2 Não incluído

- Alterar a prescrição do treino em andamento (o personal continua sendo o dono da prescrição).
- Remover uma série já registrada (o histórico é fato consumado).
- Reordenar os exercícios da tela Hoje.
- Notificação push / som no fim do treino.
- Tela de estatísticas de aderência (quantos treinos por semana, por exemplo).

## 4. Requisitos funcionais

**Atleta — atividade**

1. Cada exercício prescrito do treino em andamento mostra um check. Marcado, o exercício fica com o
   nome em verde; desmarcado, volta ao normal.
2. O check marca o exercício como feito e **não** apaga as séries já registradas dele.
3. Registrar uma série marca o exercício como feito: quem lê o check não precisa de outra ação.
4. Cada exercício tem dois campos numéricos: **reps** e **kg**, preenchidos com a prescrição
   (repetições e carga). O atleta pode mudar os dois; mudar só um é o caso comum.
5. As séries registradas aparecem como etiquetas no formato "2x 30kg" (repetições × carga). Carga zero
   aparece como "2x peso corporal".
6. O botão "Finalizar treino" só fica habilitado com todos os exercícios prescritos marcados; a tela
   diz quantos faltam ("Faltam 2 exercícios para marcar antes de finalizar").
7. O check só existe enquanto o treino está em andamento.

**Atleta — um treino por dia**

8. Com um treino no dia (rodando ou concluído), os outros treinos da série mostram o aviso "Você já
   treinou hoje. Só dá para fazer um treino por dia — o próximo fica para amanhã." e não mostram o
   botão de iniciar.
9. A API recusa o segundo treino do dia com `409 session.start.already_started_today`.
10. Iniciar de novo **o mesmo** treino do dia devolve a sessão existente (idempotência preservada).

**Time — ergonomia**

11. Qualquer erro de API aparece como toast no canto da tela, em vez da faixa dentro da página.
12. Enquanto houver request em voo, a tela fica bloqueada por um overlay com spinner.

## 5. Regras de negócio / invariantes

1. **O check pertence à sessão** (`WorkoutExecution`), não à prescrição: a prescrição é do personal e
   não muda com o que o atleta fez.
2. **A marcação é do exercício, não do treino**: o agregado guarda o conjunto de exercícios marcados
   (`WorkoutSession.CompletedExercises`).
3. **Registrar uma série marca o exercício**: performing um exercício é o próprio fato de fazê-lo.
   Invariante do agregado (`WorkoutSession.RegisterSet`).
4. **Desmarcar não apaga séries**: o que o atleta levantou é fato; o check responde "o exercício da
   prescrição está concluído".
5. **Finalizar exige todos os exercícios da prescrição marcados.** A regra mora no use case
   `FinishWorkout`, e não no agregado: a prescrição vive no contexto de planejamento e a sessão
   guarda só o id do treino (mesmo princípio de `LogPerformedSet`, que já checa a prescrição no
   use case).
6. **Um treino por dia**: a regra é do use case `StartWorkout` e vale para o dia inteiro — um treino
   rodando **ou** concluído bloqueia o próximo. Antes da regra existia a unicidade
   (usuário, dia, treino), que permitia dois treinos no mesmo dia.
7. **A expiração da série vem antes da regra do dia**: quem tentou com a série vencida recebe
   `workout_series.assignment.expired` (o aviso mais específico), e não o conflito do dia.
8. **As repetições do atleta são limitadas a 1–100**, a mesma faixa da prescrição.

## 6. Contrato HTTP

### 6.1 Novos endpoints

`PUT /sessions/{sessionId}/exercises/{exerciseId}/completion`

```json
{ "completed": true }
```

Idempotente: `true` marca, `false` desmarca. Resposta `200` com a sessão atualizada. O exercício
precisa estar na prescrição do treino da sessão — fora disso, `400`
`session.set.exercise.not_prescribed`.

### 6.2 Contratos alterados

- `POST /sessions/{sessionId}/sets` — `LogPerformedSetRequest` ganha `repetitions` (int opcional):
  `{ "exerciseId": "…", "loadKilograms": 65, "repetitions": 2 }`. Omitido/null → as repetições da
  prescrição.
- `GET /workout-series/mine` — o treino de hoje (`TodayWorkoutDto`) ganha `completedExerciseIds`
  (lista de ids), a lista dos exercícios marcados.
- `GET /sessions` e `GET /athletes/{userId}/workouts` — `WorkoutSessionDto` ganha
  `completedExerciseIds`.

### 6.3 Códigos de recusa novos

| Código | HTTP | Quando |
| --- | --- | --- |
| `session.start.already_started_today` | 409 | Segundo treino do mesmo dia. |
| `session.finish.exercises.pending` | 409 | Finalização com exercício pendente. |
| `session.exercise.after_finish` | 400 | Marcação em treino já finalizado. |
| `set.repetitions.out_of_range` | 400 | Repetições fora de 1–100. |

Traduções em `src/FitTrack.UI/Api/ErrorMessages.cs`.

## 7. Fluxo principal

**Atleta treinando**

1. Atleta entra em `/today`; a série aparece com os treinos A–E.
2. "Iniciar treino" no treino A → `POST /sessions` → a sessão nasce e o cronômetro corre.
3. O exercício mostra os campos (reps e kg já com a prescrição) e o check vazio.
4. O atleta faz o exercício e toca em **Registrar** → `POST /sessions/{id}/sets` com a carga e (se
   mudou) a repetição → a etiqueta "2x 30kg" aparece e o exercício fica marcado.
5. Se o exercício foi feito sem carga, o atleta toca no check → `PUT …/completion` → o exercício fica
   marcado sem série registrada.
6. Repetir o passo 4 para os demais exercícios; o contador "Faltam N exercícios" cai a cada um.
7. "Finalizar treino" → `POST /sessions/{id}/finish` → o treino fecha e a duração congela.

**Atleta no dia seguinte**

8. O treino do dia anterior aparece como "Concluído"; o próximo treino da série continua "A fazer" e
   pode ser iniciado normalmente (a regra vale por dia).

## 8. Fluxos alternativos e erros

| Situação | Comportamento |
| --- | --- |
| Atleta marca o check e depois desmarca | O exercício volta a pendente; as séries registradas continuam. |
| Atleta registra uma série | O check é marcado sozinho e a etiqueta da série aparece. |
| Atleta tenta finalizar com exercício pendente | O botão está desabilitado e a tela diz quantos faltam; pela API, `409 session.finish.exercises.pending`. |
| Atleta tenta iniciar o segundo treino do dia | A tela mostra o aviso e esconde o botão; pela API, `409 session.start.already_started_today`. |
| Atleta inicia de novo o mesmo treino do dia | A sessão existente volta (`200`), sem criar outra. |
| Série vencida e tentativa de iniciar | `400 workout_series.assignment.expired` (o aviso mais específico vem antes do conflito do dia). |
| Treino em andamento com a série vencida | Continua usável: marcar, registrar e finalizar. |
| Personal remove um exercício do treino no meio | O exercício que saiu da prescrição não é mais exigido para finalizar. |
| Repetições fora de 1–100 | `400 set.repetitions.out_of_range`; a tela recarrega e mostra o toast. |
| Qualquer erro de API | Toast no canto da tela, com a tradução do código em português. |

## 9. Modelo de dados

Tabela nova `completed_exercises`:

- `WorkoutSessionId` — `uuid`, parte da chave primária, FK para `workout_sessions` (cascade delete).
- `ExerciseId` — `uuid`, parte da chave primária.

Uma linha por exercício marcado na sessão. A chave composta impede duas linhas do mesmo exercício; o
agregado garante a unicidade (o `Tick` só acrescenta se ainda não está na lista).

Migração EF Core `20261022002033_SessionExerciseTicks` em
`src/FitTrack.Adapters.Out.Persistence/Migrations/`. Snapshot atualizado. Nenhuma coluna nova nas
outras tabelas; `workout_sessions` e `performed_sets` não mudam.

### Value objects / dtos tocados

- `WorkoutSession` — `CompletedExercises`, `IsExerciseCompleted`, `MarkExerciseCompleted`,
  `UnmarkExerciseCompleted`; `RegisterSet` marca o exercício.
- `TodayWorkoutDto` e `WorkoutSessionDto` — `CompletedExerciseIds`.
- `WorkoutSessionMapper` — lê e grava as marcações; `WorkoutSessionQueries.LoadTodayAsync` e
  `WorkoutSessionProjection` carregam as marcações na leitura.
- `LogPerformedSetCommand` — `Repetitions` opcional.

## 10. Fluxo de telas

### 10.1 Tela Hoje

- `Today.razor` (`src/FitTrack.UI/Pages/Today.razor`): check por exercício, campos de reps e kg,
  etiquetas "2x 30kg", contador de pendências e botão de finalizar condicional.
- Estado local: `_ticks` (o check, espelhando o servidor), `_reps` e `_loads` (o que o atleta
  digitou). O servidor é a verdade: cada ação relê a folha, então o check nunca diverge do banco.

### 10.2 Toasts e overlay

- `src/FitTrack.UI/Notifications/ToastService.cs` — o hub das mensagens: `Info`, `Success`,
  `Warning`, `Error`, com expiração automática (5 s; 9 s para erro) e `TimeProvider` injetável.
- `src/FitTrack.UI/Components/ToastHost.razor` — desenha a pilha de toasts no canto (acima da barra de
  abas no mobile) e remove as mensagens vencidas no próprio tick.
- `src/FitTrack.UI/Notifications/RequestActivity.cs` — conta os requests em voo.
- `src/FitTrack.UI/Api/RequestActivityHandler.cs` — entra no pipeline do `HttpClient` e conta cada
  request.
- `src/FitTrack.UI/Components/LoadingOverlay.razor` — o overlay que bloqueia a tela.

> **Detalhe que custou uma hora:** o `RequestActivity` é **singleton**, não scoped. O handler do
> pipeline HTTP resolve o serviço do próprio escopo, e só a instância da raiz é a mesma que o overlay
> observa — com scoped, os dois viam instâncias diferentes e o overlay nunca aparecia.

## 11. Critérios de aceite

**Check**

1. Dado um treino em andamento, quando o atleta marca o check de um exercício, então o exercício fica
   marcado (nome em verde) e o servidor guarda a marcação.
2. Quando o atleta desmarca, o exercício volta a pendente e as séries registradas continuam.
3. Quando o atleta registra uma série, o exercício fica marcado sem ação extra.

**Repetição e carga**

4. Os campos reps e kg já vêm preenchidos com a prescrição.
5. Quando o atleta muda só a carga e registra, a série gravada tem a carga nova e as repetições da
   prescrição.
6. Quando o atleta muda as repetições, a série gravada tem as repetições novas (ex.: "2x 27kg").
7. As séries registradas aparecem no formato "2x 30kg".

**Um treino por dia**

8. Dado um treino concluído hoje, quando o atleta abre `/today`, então os outros treinos mostram o
   aviso do dia e não mostram o botão de iniciar.
9. Pela API, iniciar outro treino no mesmo dia responde `409 session.start.already_started_today`.
10. Iniciar o mesmo treino de novo devolve a sessão existente, sem criar outra.

**Finalização**

11. Com exercício pendente, o botão "Finalizar treino" está desabilitado e a tela diz quantos faltam.
12. Pela API, finalizar com pendência responde `409 session.finish.exercises.pending`.
13. Com todos marcados, finalizar fecha o treino.

**Ergonomia**

14. Um erro de API aparece como toast no canto da tela, em qualquer página.
15. Durante um request, a tela fica coberta pelo overlay com spinner.
16. Depois do request, o overlay some e a tela volta ao normal.

## 12. Impacto arquitetural

| Projeto | Impacto |
| --- | --- |
| `FitTrack.Domain` | `WorkoutSession` ganha a coleção de exercícios marcados e as operações de marcar/desmarcar. Nenhuma dependência nova. |
| `FitTrack.Application` | `MarkExerciseCompleted` (use case e porta de entrada), `StartWorkout` e `FinishWorkout` com as regras novas, `LogPerformedSet` com repetições opcionais, `IWorkoutSessionRepository.ListByDateAsync`. |
| `FitTrack.Adapters.Out.Persistence` | Record `CompletedExerciseRecord`, configuration, mapper, repositório, queries de leitura e migração. |
| `FitTrack.Adapters.In.Api` | Contratos `SetExerciseCompletionRequest` e `LogPerformedSetRequest`; endpoint `PUT /sessions/{id}/exercises/{exerciseId}/completion`; registro do use case novo no composition root. |
| `FitTrack.UI` | `ToastService`, `RequestActivity`, `RequestActivityHandler`, `ToastHost`, `LoadingOverlay`, `Today.razor` reescrito, erro das demais páginas migrado para toast, `MainLayout` com os dois componentes, CSS dos toasts/overlay/check. |
| Testes | Fakes unitários (`InMemoryWorkoutSessionRepository`), casos novos em `WorkoutSeriesExecutionTests`, `WorkoutSessionTests`, `ToastServiceTests`, `RequestActivityTests` e `SessionFlowTests` (integração). |

## 13. Plano de testes

### Unitários (`tests/FitTrack.Unit`)

- `WorkoutSessionTests`: registrar série marca o exercício; marcar duas vezes não duplica; desmarcar
  mantém as séries; marcar após finalizar é recusado (`session.exercise.after_finish`).
- `WorkoutSeriesExecutionTests`: um treino por dia (rodando e concluído) devolve
  `session.start.already_started_today`; finalizar com pendência devolve
  `session.finish.exercises.pending`; repetições do atleta são usadas; repetições fora da faixa são
  recusadas; marcar/desmarcar pelo use case.
- `ToastServiceTests`: mensagem entra, sai por expiração (`TimeProvider` falso), `Dismiss` e `Clear`.
- `RequestActivityTests`: o contador segura o overlay até o último request; um `Dispose` duplo não
  abre a tela com request em voo.

### Integração (`tests/FitTrack.Integration`)

- `SessionFlowTests`: ticks abrem e fecham o treino; finalizar com pendência é `409`; um treino por
  dia é `409`; registrar com repetições do atleta.

## 14. Riscos e dependências

- Migração aditiva (tabela nova), risco baixo; o AppHost aplica as migrações no startup em
  Development.
- A regra de um treino por dia não ganha índice único no banco: o banco pode conter mais de um
  treino no mesmo dia (dados anteriores à regra) e um índice unique falharia a migração. A regra é do
  use case; a corrida de dois "iniciar" simultâneos é improbável (a tela fica bloqueada durante o
  request) e o índice existente (usuário, dia, treino) continua garantindo a idempotência.
- O check é do cliente espelhado do servidor: cada ação relê a folha, então um request falhado não
  deixa o check marcado na tela.

## 15. Decisões em aberto

| ID | Decisão | Recomendação |
| --- | --- | --- |
| **D1** | O check marca sozinho ao registrar uma série? | Sim: evita duas ações para o mesmo fato e mantém o histórico coerente. |
| **D2** | Desmarcar apaga as séries registradas? | Não: o que o atleta levantou é fato; o check responde só sobre o exercício da prescrição. |
| **D3** | Repetição do atleta pode ser maior que a da prescrição? | Sim, dentro de 1–100: houve ajuste do personal entre a prescrição e o treino. |
| **D4** | Um treino por dia vale para o dia civil (UTC) ou o dia local do atleta? | Hoje é o dia UTC do servidor (`WorkoutDate`), o mesmo critério já usado pela sessão. Migrar para o fuso do atleta é uma fatia própria. |
| **D5** | Índice único no banco para a regra do dia? | Não agora: quebraria a migração em bancos com dias de treino duplo. Avaliar quando a regra puder limpar o histórico. |
| **D6** | Toast de sucesso também? | Sim, quando houver ação cujo sucesso vale um aviso (hoje o feedback de sucesso continua nas telas). |
