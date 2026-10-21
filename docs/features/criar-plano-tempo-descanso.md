# Tempo de descanso por exercício prescrito (plano) e cronômetro de descanso do atleta

- **Slug:** `criar-plano-tempo-descanso`
- **Status:** implementado (2026-10-21)
- **Data:** 2026-10-21
- **Fatia única:** o tempo de descanso passa a fazer parte da prescrição (lado do personal) e é consumido por um cronômetro na tela Hoje (lado do atleta).

## 1. Contexto e problema

### 1.1 O lado do personal: a prescrição não fala de descanso

A prescrição de um exercício é o value object `PrescribedExercise`
(`src/FitTrack.Domain/WorkoutPlanning/PrescribedExercise.cs`), com `Sets`, `Repetitions` e
`LoadKilograms` (nulo para exercício corporal). O mesmo shape atravessa todas as camadas:

| Camada | Artefato | Shape atual |
| --- | --- | --- |
| Domain | `PrescribedExercise` | `ExerciseId, Sets, Repetitions, LoadKilograms` |
| Application (entrada) | `PrescribedExerciseInput` (`Commands/PrescribedExerciseInput.cs`) | idem |
| Application (saída) | `PrescribedExerciseDto` (`Dtos/PrescribedExerciseDto.cs`) | idem + nomes do catálogo |
| Adapter In (contrato HTTP) | `PrescribedExerciseRequest` (`Contracts/WorkoutSeriesRequests.cs`) | idem |
| Adapter Out (persistência) | `WorkoutSeriesWorkoutExerciseRecord` — tabela `workout_series_workout_exercises` | idem + `Position` |
| UI (editor) | `PrescriptionRow` + `PrescriptionEditor.razor` em `src/FitTrack.UI/Components/` | idem |
| UI (leitura) | `PrescribedExerciseResponse` (`src/FitTrack.UI/Api/WorkoutSeriesResponse.cs`) | idem |

Não há campo de tempo de descanso: o personal não consegue dizer "descanse 90s após este exercício",
e o atleta fica sem referência de quanto esperar entre séries.

### 1.2 O lado do atleta: Hoje não tem cronômetro de descanso

A tela `/today` (`src/FitTrack.UI/Pages/Today.razor`) já tem infraestrutura de relógio (um
`System.Timers.Timer` de 1 s que agenda re-render via `InvokeAsync`, contando o tempo **do treino**
a partir de `ElapsedSeconds` medido no servidor). Mas não há cronômetro de descanso por exercício:
nada mostra o tempo prescrito, nada contagem regressiva, nada alerta o atleta de que o descanso
acabou. O fluxo atual é "registrar carga → registrar série", sem pausa guiada.

## 2. Objetivo e valor

- **Personal:** definir, por exercício de cada treino (plano) da série, o tempo de descanso, com
  valor padrão de 60 segundos, em um campo opcional do editor que ele já usa.
- **Atleta:** ver o tempo de descanso de cada exercício na tela Hoje, abrir um modal com contagem
  regressiva, pausar/retomar e ser avisado quando o tempo acabar.

## 3. Escopo

### 3.1 Incluído

1. Campo `RestSeconds` (nome provisório de coluna/campo; ver decisão D2) no value object de
   prescrição, em todos os shapes da tabela 1.1: entrada personal, persistência, leitura, contrato
   HTTP e telas.
2. Padrão **60 segundos** quando o personal não informa o campo (novo valor e valor existente). O
   campo é opcional na borda HTTP: nulo → 60.
3. Faixa permitida **0–600 segundos**: 0 significa "sem descanso" (sem chip/cronômetro para aquele
   exercício). Ver decisão D3.
4. Editor do personal (`SeriesDetail.razor` + `PrescriptionEditor.razor`): campo "Descanso (s)" por
   linha, pré-preenchido com 60.
5. Tela do atleta (`Today.razor`): chip de descanso por exercício prescrito; clique abre um modal com
   contagem regressiva iniciada no valor prescrito; botões pausar/retomar; ao zerar, alerta visível
   no modal até ser dispensado.
6. Migração EF Core: coluna nova na tabela `workout_series_workout_exercises`, não nula, com valor
   padrão 60 para as linhas existentes.

### 3.2 Não incluído

- Persistência do estado do cronômetro (pausado/zerado) na sessão do treino: o estado do descanso é
  efêmero, do cliente; o banco continua guardando apenas o tempo prescrito.
- Cronômetro automático ao registrar uma série ("registrar → descanso inicia sozinho"): pode vir em
  um corte seguinte (decisão D5).
- Som/vibração no alerta e notificações push da web.
- Histórico de descanso, relatório de aderência ao descanso, tempo de descanso por **série** (em vez
  de por exercício) ou global do treino.
- Alteração de telas do histórico (`/workouts`, `SessionCard`): o descanso prescrito não aparece lá.

## 4. Requisitos funcionais

**Personal**

1. No formulário de prescrição (`PrescriptionEditor.razor`), cada linha ganha um campo "Descanso (s)",
   numérico, com valor inicial 60 quando uma linha nova é criada e com o valor salvo quando um treino
   existente é carregado.
2. O valor enviado é opcional: `restSeconds` ausente ou nulo significa 60. O editor sempre envia o
   valor do campo.
3. O valor é salvo por `POST /workout-series/{seriesId}/workouts` (criar) e
   `PUT /workout-series/{seriesId}/workouts/{workoutId}` (alterar), pelos use cases
   `AddSeriesWorkout` e `ChangeSeriesWorkout`, que já autorizam via `RequirePersonalTrainer` e
  ownership da série.

**Atleta**

4. Em `Today.razor`, cada exercício prescrito exibe o tempo de descanso (ex.: chip "Descanso 60s");
   prescrição com 0 não exibe chip nem cronômetro.
5. Clique no chip abre um modal (um por vez) com contagem regressiva iniciada em `RestSeconds`,
   exibida em `mm:ss`; o relógio do treino (card "Tempo de treino") continua contando em paralelo.
6. O modal tem um único botão pausar/retomar: pausar congela a contagem; retomar continua de onde
   parou. Fechar o modal não reseta o valor salvo da prescrição, mas descarta a contagem corrente
   (reabrir reinicia do valor prescrito); ver decisão D4.
7. Quando a contagem chega a zero, o modal exibe um alerta ("Descanso concluído"), travado até o
   atleta dispensar; o chip do exercício não muda de estado visual — o alerta vive no modal, e o
   modal só se fecha por ação do atleta (fechar, reiniciar). A contagem é uma classe pura com
   relógio injetado (`RestCountdown`), testada em `FitTrack.Unit`; o modal só a desenha e a tica, e a
   página dispensada ignora ticks em trânsito.

## 5. Regras de negócio / invariantes

1. O tempo de descanso pertence à **prescrição** (domínio `WorkoutPlanning`), não à sessão
   (`WorkoutExecution`). Sessões abertas não mudam de significado ao editar a prescrição — o mesmo
   princípio de `ChangeSeriesWorkout` (a sessão guarda o id do treino, não uma cópia).
2. Faixa válida: `0 ≤ RestSeconds ≤ 600`; fora dela, o domínio recusa com `DomainException`
   `workout_series.exercise.prescription.invalid` (o mesmo código já usado para séries, repetições e
   carga inválidas; a tabela de recusas em `tests/FitTrack.TestCases` ganha a linha nova).
3. Padrão do sistema: 60 s (constante `DefaultRestSeconds = 60` no value object). A fronteira HTTP
   normaliza nulo → 60 (a normalize pode morar no mapeamento `PrescribedExerciseInput`).
4. Edições do personal valem para treinos **futuros**: o atleta lê a prescrição vigente via
   `GET /workout-series/mine`; nada é congelado por sessão.

## 6. Contrato HTTP (aditivo)

Nenhum endpoint novo; os contratos de prescrição ganham um campo.

- `POST /workout-series/{seriesId}/workouts` e `PUT /workout-series/{seriesId}/workouts/{workoutId}`
  — requisição `SetSeriesWorkoutRequest` com itens `PrescribedExerciseRequest`:

```json
{ "exerciseId": "…", "sets": 4, "repetitions": 10, "loadKilograms": 40, "restSeconds": 90 }
```

  `restSeconds` é opcional (int nulo). Omitted/null → 60.

- `GET /workout-series/{seriesId}` e `GET /workout-series/mine` — resposta com itens
  `PrescribedExerciseDto` incluindo `restSeconds` **sempre presente** (deriva do padrão quando não
  foi informado):

```json
{ "exerciseId": "…", "exerciseName": "Supino reto", "muscleGroupName": "Peito",
  "sets": 4, "repetitions": 10, "loadKilograms": 40, "restSeconds": 90 }
```

- Erros: `restSeconds < 0` ou `> 600` → `400` `ProblemDetails` com
  `title = "workout_series.exercise.prescription.invalid"` (tradução nova não é necessária: o código
  já existe em `src/FitTrack.UI/Api/ErrorMessages.cs` — conferir o texto traduzido cobre "descanso").

## 7. Fluxo principal

**Personal configurando**

1. Personal abre `/series/{id}`; cada linha do editor (`PrescriptionEditor`) carrega "Descanso (s)"
   com o valor salvo (60 se nunca configurado).
2. Personal ajusta o valor (ex. 90) e clica "Salvar treino" → `PUT …/workouts/{workoutId}`.
3. `ChangeSeriesWorkout` mapeia via `ToPrescribed`, `PrescribedExercise` valida a faixa, o agregado
   `WorkoutSeries.ChangeWorkout` persiste pela cadeia existente
   (`WorkoutSeriesMapper` grava a coluna nova) e o DTO devolvido traz `restSeconds: 90`.

**Atleta usando o cronômetro**

4. Atleta entra em `/today`; o mesmo treino aparece com o chip "Descanso 90s" em cada exercício.
5. Terminou a série do exercício, toca no chip → abre o modal com "01:30" e o botão de pausar/retomar
   (estado inicial: contando).
6. O tick de 1 s decrementa o contador no contexto do Blazor; "01:29", "01:28"…
7. Atleta pausa → contagem congela ("00:44"), botão vira "retomar"; retoma do mesmo valor.
8. Ao chegar a `00:00`, o modal troca para o estado de alerta ("Descanso concluído…") e para de
   contagem; o atleta dispensa o alerta (fecha ou reinicia o descanso para a próxima série, vendo
   decisão D6).

## 8. Fluxos alternativos e erros

| Situação | Comportamento |
| --- | --- |
| Personal envia `restSeconds: 0` | Salvo; atleta não vê chip nem cronômetro para aquele exercício. |
| Personal envia `restSeconds: -1` ou `9999` | `400` `workout_series.exercise.prescription.invalid`; a tela de editor mostra a mensagem em português e preserva o que o personal digitou. |
| Personal omite `restSeconds` | Prescrição salva com 60 s. |
| Treino antigo (linha criada antes da migração) | A coluna nasce com default 60; o atleta lê 60 s. |
| Atleta fecha o modal com contagem em andamento | Contagem descartada; rechamada reinicia do valor prescrito. |
| Atleta navega para away/dispose da página durante contagem | Tick em trânsito é ignorado (mesmo guard de `_disposed` de `Today.razor`); nenhuma exceção "erro inesperado". |
| Atleta abre o modal para o exercício A e depois para B | Um modal por vez; abrir B fecha A. |
| Série vencida (`IsExpired`) e o treino já iniciado | Cronômetro de descanso segue usável; só **iniciar** treino novo é bloqueado (comportamento existente). |

## 9. Modelo de dados

Tabela `workout_series_workout_exercises` (chave composta
`workout_series_workout_id + exercise_id`) ganha uma coluna:

- `RestSeconds` — `integer`, `NOT NULL`, valor padrão `60` (EF: `.HasDefaultValue(60)`), sem
  restrição de faixa no banco (a faixa é invariante do domínio; isto segue o padrão atual de `Sets`).
  O nome da coluna segue a convenção da tabela (PascalCase, como `Sets` e `LoadKilograms`); o
  `.HasSentinel(int.MinValue)` no modelo é o que impede o default de engolir o 0 — "sem descanso" é
  um valor legítimo e precisa ser gravado como tal.

Migração EF Core `20261021150319_PrescriptionRestSeconds` em
`src/FitTrack.Adapters.Out.Persistence/Migrations/`; linhas existentes são preenchidas com 60 pelo
default na criação da coluna. Snapshot atualizado. Sem dado tabelado novo em outras tabelas;
`workout_sessions` e `performed_sets` não mudam.

### Value objects / dtos tocados

- `PrescribedExercise` — propriedade `RestSeconds` + constante `DefaultRestSeconds`.
- `PrescribedExerciseInput`, `PrescribedExerciseDto`, `PrescribedExerciseRequest`,
  `PrescribedExerciseResponse` (UI), `PrescribedExerciseResponse` (contracts de integração).
- `PrescribedExerciseMapping.ToPrescribed` — normaliza nulo → 60.
- `WorkoutSeriesWorkoutExerciseRecord`, `WorkoutSeriesMapper` (gravação e leitura),
  `WorkoutSeriesQueries` e `UserSessionQueries` (projeção da leitura, linhas ~167 e ~158).

## 10. Fluxo de telas

### 10.1 Editor do personal

- `PrescriptionEditor.razor`: campo `number` por linha, `min="0"`, `max="600"`, `step="1"`,
  `inputmode="numeric"`, label "Descanso (s)". O editor mostra o **valor efetivo** (60 quando o
  personal não mexeu), não um placeholder eterno — o `placeholder` previsto na especificação inicial
  foi descartado por isso.
- `PrescriptionRow` ganha `int RestSeconds { get; set; } = 60;`.
- `SeriesDetail.razor` carrega `RestSeconds` ao hidratar `WorkoutRow` e envia no
  `PrescribedExerciseRequest`. Mensagem de erro traduzida no bloco de `_error` existente.

### 10.2 Tela Hoje (atleta)

- Chip de descanso dentro da linha de cada exercício prescrito (nos dois estados do card: "a fazer"
  e "em andamento"), texto "⏱ 90s" com `aria-label="Descanso prescrito de {exercício}: 90 segundos"`.
- Componente novo `src/FitTrack.UI/Components/RestTimerModal.razor` (overlay do design system):
  título "Descanso", exercício, contagem `mm:ss`, botão pausar/retomar único com troca de rótulo,
  alerta de conclusão com botão "Reiniciar descanso".
- A regra da contagem mora em `src/FitTrack.UI/Components/RestCountdown.cs` — classe pura com
  relógio injetado, testada em `tests/FitTrack.Unit/Application/RestCountdownTests.cs`. O estado
  fica **no modal** (uma instância por abertura): fechar descarta a contagem e nada cruza HTTP.

## 11. Critérios de aceite

**Prescrição (personal)**

1. Dado um personal criando um treino sem preencher "Descanso (s)", quando salva, então a prescrição
   fica com 60 s e `GET /workout-series/{id}` devolve `restSeconds: 60`.
2. Dado um personal alterando o campo para 90 e salvando, quando a resposta do `PUT` volta, então
   `restSeconds: 90` e a tela recarregada mostra 90.
3. Dado um personal enviando `restSeconds: -1` (ou acima de 600), quando salva, então a API responde
   `400` com `workout_series.exercise.prescription.invalid` e a UI exibe mensagem em português.
4. Dado uma prescrição salva com 0, quando o treino aparece em `/today`, então nenhum chip de
   descanso é exibido para aquele exercício.

**Cronômetro (atleta)**

5. Dado um exercício prescrito com descanso de 60 s, quando o atleta abre `/today`, então o chip de
   descanso exibe "60s"; ao clicar, o modal abre com "01:00" e a contagem desce 1 s por segundo.
6. Quando o atleta pausa, a contagem congela e o botão muda para retomar; retomar continua de onde
   parou (sem resetar ao valor cheio).
7. Quando a contagem chega a 00:00, um alerta "Descanso concluído" aparece no modal e permanece até
   o atleta dispensar.
8. Ao fechar o modal e reabrir o mesmo exercício, a contagem reinicia do valor prescrito (60 s ou o
   personalizado), não do valor interrompido.
9. O cronômetro de descanso não interfere no relógio do treino: o "Tempo de treino" continua contando
   e a finalização/cancelamento do treino não muda de contrato.
10. Deixar a página com o modal aberto não gera exceção (ticks pós-dispose são ignorados).

**Persistência**

11. A migração preenche as linhas antigas com 60; nenhuma linha lê nulo ou erro para prescrições
    criadas antes do deploy.

## 12. Impacto arquitetural

| Projeto | Impacto |
| --- | --- |
| `FitTrack.Domain` | `PrescribedExercise` ganha a propriedade e a validação de faixa (nenhuma dependência nova). |
| `FitTrack.Application` | `PrescribedExerciseInput`/`PrescribedExerciseDto`, `PrescribedExerciseMapping`; nenhum use case muda de assinatura (o campo viaja dentro do input). |
| `FitTrack.Adapters.Out.Persistence` | Record, configuration (`HasDefaultValue`), mapper, queries de leitura, migração. |
| `FitTrack.Adapters.In.Api` | Contratos `PrescribedExerciseRequest`; endpoints reutilizam `Prescription(...)` sem lógica nova. |
| `FitTrack.UI` | `PrescriptionRow`, `PrescriptionEditor.razor`, `SeriesDetail.razor`, `WorkoutSeriesResponse`, `Today.razor`, componente novo `RestTimerModal.razor`, estilos. |
| Testes | Contracts de integração (`tests/FitTrack.Integration/Contracts/WorkoutSeriesResponse.cs`) e fakes unitários ganham o campo. |

## 13. Plano de testes

### Unitários (`tests/FitTrack.Unit`)

- `ValueObjectTests`/`WorkoutSeriesTests`: faixa válida, `DefaultRestSeconds = 60`, recusas fora de
  0–600 (`workout_series.exercise.prescription.invalid`), normalização nulo → 60 no mapeamento da
  borda.
- `WorkoutSeriesManagementTests`/`SeriesRefusalTests` (fakes): salvar treino com descanso informado e
  omitido; recusa fora da faixa entra na tabela `tests/FitTrack.TestCases/SeriesRefusals.cs`.
- Atualizar `InMemoryWorkoutSeriesQueries` para carregar o campo.

### Integração (`tests/FitTrack.Integration`)

- `SeriesFlowTests`/`Prescriptions.cs`: criar treino sem `restSeconds` → resposta com 60; com 90 →
  resposta 90; `PUT` alterando para 120 → 120; fora da faixa → 400.
- `SessionFlowTests`/`VolumeFlowTests`: regressão — os shapes de sessão não mudam; `GET
  /workout-series/mine` passa a carregar `restSeconds` (é o que o atleta consome).
- Testes de tradução SQL (`QueryTranslationTests`), se a coluna entrar em algum filtro — não entra;
  continuar sem filtro novo.

### Validação manual

1. `dotnet run --project src/FitTrack.AppHost`; entrar como `trainer@fittrack.local`.
2. `/series/{id}`: montar treino, conferir campo "Descanso (s)" em 60 por padrão; trocar um para 90 e
   salvar; recarregar e conferir persistência.
3. Entrar como `athlete@fittrack.local`, `/today`: chip 90s no exercício configurado, 60s nos demais;
   abrir modal, pausar/retomar, esperar zerar e conferir o alerta; `restSeconds: 0` não mostra chip.

## 14. Riscos e dependências

- Migração aditiva, risco baixo; ainda assim, executar contra o banco local antes do commit
  (`dotnet ef database update`), porque o AppHost aplica migrações no startup em Development.
- A contagem do descanso é do cliente: recarregar a página recomeça o descanso do valor cheio
  (estado efêmero, aceito pelo escopo). Um relógio redondo por exemplo de 1 segundo de deriva é
  possível; nada que o time considere crítico para descanso de treino.
- `[Authorize]` e políticas não mudam: a feature não cria rota nova.

## 15. Decisões em aberto

| ID | Decisão | Recomendação |
| --- | --- | --- |
| **D1** | Nome público do campo (`restSeconds` é o candidato) | `restSeconds`, alinhado ao estilo `loadKilograms` (nome completo, snake case camelCase do JSON). |
| **D2** | Coluna `RestSeconds` com default no banco vs. campo opcional (nulo = 60) projetado na leitura | Coluna `NOT NULL` com `DEFAULT 60`: a leitura não precisa normalizar e o dado é explícito. |
| **D3** | Faixa permitida: 0–600 com 0 = "sem descanso", ou mínimo 10 | 0–600: dá flexibilidade ("sem descanso") e evita recuperar o atributo depois; a UI pode orientar "0 = sem descanso". |
| **D4** | Fechar o modal: descarta a contagem ou mantém rodando em segundo plano? | Descarta e reinicia do valor prescrito ao reabrir (simples e previsível); manter rodando em segundo plano adiciona estado que ninguém pediu nesta fatia. |
| **D5** | Iniciar o cronômetro automaticamente ao registrar uma série do exercício | Fora de escopo agora; fica um follow-up bom, dependendo do feedback dos atletas. |
| **D6** | Zerou: alerta permanece + opções "dispensar" e "recomeçar" | Alerta com o botão "Reiniciar descanso" para a próxima série (o clique dispensa e reinicia); dispensar sem reiniciar leva de volta ao chip. |
| **D7** | Notificação sonora quando zerar | Não nesta fatia (sem JS interop de áudio); o alerta visual cobre o pedido. |
