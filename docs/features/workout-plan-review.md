# Revisão e correção do fluxo de treino — ajuste de carga/repetições, exclusão pelo personal, alarme/vibração e testes pelo app

- **Slug:** `workout-plan-review`
- **Status:** proposto (2026-10-26)
- **Data:** 2026-10-26
- **Origem:** requisito consolidado de quatro falhas reportadas pelo usuário: (1) exclusão de treino pelo personal não funciona no click do botão; (2) alarme e vibração não estão seguindo o comportamento esperado no auto-finish; (3) não é possível colocar valor nas cargas e repetições na finalização/ajuste; (4) necessidade de gerar correções e testar todos os cenários pelo app.
- **Fatia:** entrega única (feature única) que engloba múltiplos cenários relacionados ao treino, alarme/vibração, cargas/repetições e validação pelo app — não são sub-requisitos independentes, mas pontos de falha no mesmo fluxo de uso.

## 1. Contexto e problema

O fluxo de treino (iniciar, executar séries, ajustar carga/repetições, finalizar, cancelar pelo personal, auto-finalizar e histórico) apresenta falhas simultâneas reportadas:

1. **Exclusão/cancelamento pelo personal não funciona.** O personal deve poder cancelar um treino em andamento do atleta (liberar o dia). O botão de exclusão/cancelamento no contexto do personal não está respondendo corretamente, impedindo o uso do endpoint/ação esperado (`CancelAthleteWorkout` / `DeleteAthleteWorkout`). O usuário reporta que "não funciona o click do botão para o personal".
2. **Alarme e vibração não estão seguindo.** Quando o `AutoFinishExpiredWorkoutsHostedService` encerra uma sessão esquecida, o front móvel (`FitTrack.UI`, Blazor PWA) não dispara alarme sonoro nem vibração corretamente, ou não respeita a duração/cessação prevista (até 15 s, cessação ao clicar "Parar", degradação graciosa). O usuário reporta que "alarme e vibração não está seguindo".
3. **Não é possível colocar valor nas cargas e repetições.** Na tela de finalização/ajuste do treino, o atleta não consegue inserir/alterar os valores de carga e de repetições (possivelmente por falha na UI de input, conversão de unidade ou validação do contrato). O usuário reporta que "não está sendo possível colocar valor nas cargas e repetições".
4. **Correção e testes pelo app.** A entrega inclui gerar as correções para os cenários acima e validar pelo aplicativo (PWA / mobile) — não apenas back-end.

Referências de documentação existente que informam este plano:
- `docs/features/analise-excluir-treino-alarme-vibracao.md` — detalha exclusão pelo admin (`DELETE /athletes/{userId}/workouts/{sessionId}`) e alarme/vibração no auto-finish.
- `docs/features/criar-plano.md` — detalha cancelamento pelo personal/admin, auto-finalização, faixa semanal e ajuste de carga/repetições com unidade alternativa.
- `docs/features/alarme-encerramento-admin-historico.md` — referência para registro de `PerformedSets` com prescrição no auto-finish.

## 2. Objetivo e valor

- **Personal:** conseguir cancelar/excluir um treino em andamento do atleta via botão, liberando o dia (regra de um treino por dia). Corrigir falha de clique/ui que impede a ação.
- **Atleta (móvel):** finalizar/ajustar cargas e repetições corretamente, sem bloqueio de input; receber alarme sonoro + vibração quando o treino é auto-finalizado, com cessação imediata ao clicar "Parar" (máx. 15 s) e degradação graciosa; não quebrar o app em navegadores sem suporte.
- **Sistema:** garantir que o `AutoFinishExpiredWorkouts` registre `PerformedSets` com a prescrição quando `PlannedWorkoutId` existir (requisito B do doc de referência) e que a exclusão pelo personal respeite autorização por roster (`RequirePersonalTrainerOrAdmin` + escopo de roster).
- **Qualidade:** todos os cenários corrigidos devem ser validados pelo app (PWA Blazor ou mobile) antes do merge — não apenas por testes automatizados isolados.

## 3. Atores envolvidos

- **Atleta** — executa treino, ajusta carga/repetições na finalização, recebe alarme no auto-finish.
- **Personal trainer** — cancela treino em andamento do atleta da própria roster (botão de exclusão/cancelamento).
- **Administrador** — pode cancelar/excluir em qualquer roster (mesma lógica `RequirePersonalTrainerOrAdmin` / `AdminOnly` conforme contexto).
- **Sistema (API + Hosted Service)** — `AutoFinishExpiredWorkouts`, `CancelAthleteWorkout`, registro de `PerformedSets`, endpoint de exclusão.
- **Team / Tester** — executa validação pelo app (cenários de clique, ajuste de valores, alarme, cessação).

## 4. Escopo

### 4.1 Incluído

1. **Correção do clique/ação de exclusão/cancelamento pelo personal** no contexto do treino (botão no app/Blazor ou endpoint exposto ao personal) — garantir que a ação chegue ao use case (`CancelAthleteWorkout` / `DeleteAthleteWorkout`) com autorização correta (roster + `AdminOnly` quando aplicável).
2. **Correção do ajuste de carga e repetições** na finalização do treino — permitir inserir/alterar valores (possível falha de binding, conversão de unidade kg ↔ barra, ou validação do contrato HTTP/DTO). Reutilizar lógica de `RegisterSet` / ajuste de carga já documentada em `criar-plano.md`.
3. **Correção do alarme/vibração no auto-finish** — componente Blazor (`AutoFinishAlarm.razor` ou equivalente) + JS interop (`Audio`, `navigator.vibrate`); duração máxima 15 s; cessação ao clicar "Parar"; degradação graciosa; detecção do auto-finish no front (polling/recarregamento de `/workouts` ou sinal local — ver decisão D1 do doc de referência).
4. **Registro de `PerformedSets` com prescrição no auto-finish** — `AutoFinishExpiredWorkouts` deve carregar prescrição (`IWorkoutSeriesRepository`) quando `PlannedWorkoutId` existe, montando `PerformedSet`; se não existir, manter vazio (não falhar o job — decisão D5 do doc de referência).
5. **Testes pelo app** — validação manual/integrada dos cenários corrigidos (exclusão pelo personal, ajuste de carga/reps, alarme/vibração, cessação) na PWA/Blazor (`FitTrack.UI`); critérios de aceite verificáveis.

### 4.2 Não incluído

- Exclusão física de treino já finalizado pelo admin (já documentado separadamente em `analise-excluir-treino-alarme-vibracao.md`; não é o mesmo cenário de "click do botão para o personal", que focou em cancelamento/exclusão de treino rodando).
- Soft-delete (decisão D3 do doc de referência) — mantém exclusão física conforme `CancelWorkout`.
- Notificação push/e-mail (apenas alarme local).
- Alteração do contrato HTTP de carga: a API continua gravando quilos; conversão "barra" permanece no cliente (`criar-plano.md`).
- Novas tabelas de banco (sem migração nova).
- SignalR/integration event (avaliar depois; ver decisão D1).
- Configuração de duração de alarme (fixa em 15 s, decisão D8).

## 5. Sub-requisito A — Correção da exclusão/cancelamento pelo personal

### 5.1 Requisitos funcionais

1. O personal (ou admin) pode cancelar uma sessão **rodando** de um atleta da própria roster via botão/ação no app.
2. A ação remove a sessão, liberando o dia (regra "um treino por dia").
3. Ação recusada se a sessão já estiver finalizada (`409 session.cancel.not_finished` / `session.delete.not_finished`).
4. Ação recusada se o atleta não for da roster do personal (resposta como se não existisse — 404, mesmo padrão `WorkoutSessionAccess`).
5. Autorização no use case, não apenas no endpoint (`RequirePersonalTrainerOrAdmin` + escopo de roster).

### 5.2 Regras de negócio

- Semântica de `CancelWorkout`: só sessão rodando (`FinishedAt == null`). Se a decisão D1 de `criar-plano.md` for "recusar se tiver séries", manter recusa (`409 session.cancel.has_sets`); caso contrário, permitir.
- Exclui sessão fisicamente (`RemoveAsync`).
- Não afeta sessões finalizadas (histórico preservado).

### 5.3 Contrato HTTP — CANCELAMENTO vs EXCLUSÃO (endpoints separados; não mesma rota)

**Cancelamento (personal/admin, só sessão RODANDO):**
```http
DELETE /athletes/{athleteId:guid}/sessions/{sessionId:guid}
```
- **Endpoint:** `WorkoutSeriesEndpoints.cs:26` (`CancelAthleteWorkoutAsync`)
- **Política endpoint:** `.RequireAuthorization(Policies.TrainerOrAdmin)` (`Program.cs` line 125: `RequireRole(Admin, PersonalTrainer)`)
- **Use case:** `CancelAthleteWorkout.cs` (`ICancelAthleteWorkout`) — exige `RequirePersonalTrainerOrAdmin`; verifica roster (`_links.GetByAthleteIdAsync`) se não admin; exige `session.IsRunning`; recusa se `HasSets`; faz `RemoveAsync`; releitura de verificação de concorrência (`GetByIdAsync` segunda vez) antes de remover.
- **Códigos por ator:**
  - **Personal (roster):** `204` (sucesso), `404` (não pertence / não existe — mesmo padrão `WorkoutSessionAccess`), `409 session.cancel.has_sets`, `409 session.cancel.not_running`, `403` (não autorizado, via endpoint)
  - **Admin (qualquer):** `204`, `404`, `409`, `403` (mesma política endpoint, mas use case não exige vínculo de roster)
- **Nota:** não é a mesma rota do `DeleteAthleteWorkout`; semântica diferente (cancelar = rodando; excluir = finalizado).

**Exclusão (personal/admin, só sessão FINALIZADA / histórico):**
```http
DELETE /athletes/{athleteId:guid}/workouts/{sessionId:guid}
```
- **Endpoint:** `AthleteWorkoutEndpoints.cs:16` (`DeleteAthleteWorkoutAsync`)
- **Política endpoint:** `.RequireAuthorization(Policies.TrainerOrAdmin)` (mesma política)
- **Use case:** `DeleteAthleteWorkout.cs` (`IDeleteAthleteWorkout`) — exige `RequirePersonalTrainerOrAdmin`; verifica `session.UserId`; exige `!session.IsRunning` (`FinishedAt != null`); retorna `Results.NoContent()`; faz `RemoveAsync`.
- **Códigos:** `204`, `404` (não existe / não pertence), `409 session.delete.not_finished` (ainda rodando), `403`
- **Distinção:** o `AthleteWorkouts.razor` (página do personal/admin) usa `Api.DeleteAthleteWorkoutAsync`, que bate `/workouts` — adequado só para sessões finalizadas (`!session.IsRunning`). O cancelamento de rodando deve usar `/sessions`; se o botão do personal tentar chamar `/workouts` numa sessão rodando, recebe `409 session.delete.not_finished`.

**Decisão confirmada:** endpoints separados (`/sessions` vs `/workouts`), não mesma rota com políticas diferentes. Cancelamento e exclusão são operações distintas por estado da sessão.

### 5.4 Fluxo principal

1. Personal abre tela do atleta/histórico.
2. Clica "Cancelar treino do atleta" (não finalizado).
3. API valida rota + autorização + `FinishedAt == null` + pertencimento + (opcional) sem séries.
4. `RemoveAsync` → `204`.
5. Atleta, ao abrir `/today`, não vê mais sessão rodando e pode iniciar.

### 5.5 Critérios de aceite

- Personal cancela rodando do atleta da roster → `204`; sessão removida; atleta pode iniciar outro treino no dia.
- Sessão finalizada → `409` (não cancela histórico).
- Atleta não pertence à roster → `404`.
- Não-personal/não-admin → `403`.

## 6. Sub-requisito B — Correção do ajuste de carga e repetições

### 6.1 Requisitos funcionais

1. Na finalização/ajuste do treino, o atleta pode inserir/alterar carga (kg ou barra) e repetições.
2. O valor prescrito aparece como padrão; pode ser aumentado.
3. Conversão de unidade (`kg ↔ barra`) funciona no cliente, sem alterar contrato HTTP (API grava kg).
4. O input não bloqueia/ignora o valor digitado (falha de binding/validação/UI corrigida).

### 6.2 Regras de negócio

- `RegisterSet` / `PerformedSet` recebem carga em kg; conversão de barra feita no cliente (`criar-plano.md`).
- Se não houver prescrição (`PlannedWorkoutId` ausente ou removido pelo personal), manter comportamento de aceitar livre, sem falhar.

### 6.3 Contrato / modelo (referência)

- Entidades tocadas: `WorkoutSession`, `PerformedSet`, `PerformedExercise`. Nenhuma tabela nova.
- DTO de ajuste: carrega prescrição + valores informados pelo usuário; grava via `RegisterSet` dentro do `Finish`.

### 6.4 Critérios de aceite

- Atleta altera carga e reps na tela de finalização → valores persistidos corretamente.
- Toggle kg/barra funciona; API recebe kg.
- Valor prescrito aparece como padrão; pode ser alterado.
- Não há bloqueio de input após a correção.

## 7. Sub-requisito C — Correção do alarme e vibração no auto-finish

### 7.1 Requisitos funcionais

1. Quando `AutoFinishExpiredWorkoutsHostedService` encerra uma sessão expirada, o front dispara alarme sonoro + vibração.
2. Duração máxima 15 s ou cessação ao clicar "Parar".
3. Uso de `Audio` / `navigator.vibrate` com degradação graciosa (não quebra se indisponível).
4. Componente reutilizável (`FitTrack.UI`), não bloqueante.
5. Só dispara quando o encerramento vem do serviço (auto-finish), não do manual (decisão D6).

### 7.2 Regras de negócio

- Vibração só se `navigator.vibrate` existir.
- Se navegador bloquear autoplay, exibir botão/aviso (decisão D2); não assumir autoplay livre.
- Após "Parar" ou 15 s, cessar imediatamente.
- Detecção no front: polling/recarregamento do histórico (`/workouts`) com sinal local de uma vez (decisão D1); avaliar SignalR depois.
- Componente montado globalmente (`MainLayout.razor` / `App.razor`) ou na página `/workouts` (decisão D7) — confirmar na implementação.

### 7.3 Fluxo principal

1. Hosted service encerra sessão expirada (a cada 5 min, após 2,5 h de abertura — referência `criar-plano.md`).
2. Front detecta auto-finish (polling/recarregamento; comparar `FinishedAt` recente com `StartedAt + Expiry` — decisão D9).
3. Página (`Today`/`MyWorkouts`) detecta auto-finish via polling (`FinishedAt` recente); passa `AlarmActive = true` ao componente montado condicionalmente (`@if`) em `MainLayout.razor`; componente dispara interop `startAutoFinishAlarm` (que inicializa `AudioContext` lazy) e inicia vibração.
4. Usuário clica "Parar" ou aguarda 15 s → cessação.

### 7.4b Contrato do componente de alarme (proposto — arquivo ainda não existe)

**Referência de trigger existente:** `MyWorkouts.razor` / `Today.razor` detectam auto-finish via polling (`LoadAsync`) comparando `FinishedAt` com `StartedAt + 2,5 h`. Quando detectado, deve chamar JS `window.startAutoFinishAlarm()`; ao clicar "Parar" ou após 15 s, chamar `window.stopAutoFinishAlarm()`.

**Props (presentation-only — recebe apenas estado da página):**
- `bool AlarmActive` — só renderiza e dispara interop quando `true`; quando `false`, componente não é montado (ver Ajuste 5 / `@if` na `MainLayout`).
- `TimeSpan? Expiry` — padrão `TimeSpan.FromHours(2.5)`.
- `Action? OnStop` — callback ao clicar "Parar" (invoca `stopAutoFinishAlarm` + encerrar ciclo).
- **NÃO recebe / NÃO chama** `IFinishWorkout` / `IAutoFinishExpiredWorkouts`. A detecção de auto-finish (polling de `FinishedAt` na página `Today`/`MyWorkouts`) fica fora do componente.

**Eventos / interop JS:**
- `JS.InvokeVoidAsync("startAutoFinishAlarm")` → inicia áudio (`AudioContext` via JS `startAutoFinishAlarm`, não no componente) + vibração (pulsos).
- `JS.InvokeVoidAsync("stopAutoFinishAlarm")` → para oscilador (`window._alarmOsc.stop()`), fecha contexto (`_alarmCtx.close()`), cancela vibração (`navigator.vibrate(0)`).
- Degradação: `try/catch` em cada bloco (já implementado em `auto-finish-alarm.js`).

**Ciclo de vida (lazy / presentation-only):**
- `AlarmActive == true` (via prop da página) → componente renderiza; dispara `startAutoFinishAlarm` (não no `OnAfterRenderAsync` global, mas no momento em que a condição se torna verdadeira).
- `AudioContext` é inicializado **apenas dentro de `startAutoFinishAlarm`** (JS interop), nunca no `OnAfterRenderAsync` do componente.
- Timer interno (15 s) → `stopAutoFinishAlarm` + `OnStop`; após cessação, página deve passar `AlarmActive = false`.
- `Dispose()` → garantir `stopAutoFinishAlarm` para evitar vazamento de áudio.

**Arquivo JS existente:** `src/FitTrack.UI/wwwroot/js/auto-finish-alarm.js` (funções `startAutoFinishAlarm`, `stopAutoFinishAlarm`, `fitTrack.playRestAlarm`, `fitTrack.unlockRestAlarm`).
**Arquivo que deve integrar:** `FitTrack.UI/Layout/MainLayout.razor` (linha 18: `<script src="/js/auto-finish-alarm.js"></script>` global); componente `AutoFinishAlarm.razor` deve ser montado globalmente, mas **apenas com `@if (AlarmActive)`** (não montado incondicionalmente).

### 7.5 Critérios de aceite (verificáveis pelo app)

- **Cenário auto-finish + alarme:**
  1. Iniciar treino (`/today`); aguardar 2,5 h (ou simular via mock/reload) até auto-finish (`AutoFinishExpiredWorkoutsHostedService`).
  2. Navegar para `/workouts` ou `/today` → componente aparece; som toca; vibração em pulsos.
  3. Clicar "Parar" → cessação imediata (`stopAutoFinishAlarm`); componente desaparece.
- **Cenário 15 s sem interação:**
  1. Deixar alarme ativo sem clicar; após 15 s → cessação automática.
- **Cenário navegador sem suporte:**
  1. Testar em navegador desktop sem `navigator.vibrate` (ou via DevTools desabilitando `AudioContext`) → app não quebra; exibe botão/aviso (se implementado); áudio pode falhar silenciosamente (degradação graciosa).
- **Cenário manual (não dispara):**
  1. Atleta encerra manualmente (`FinishWorkout`) → componente NÃO aparece (apenas quando `FinishedAt` detectado via polling dentro da janela de 2,5 h após `StartedAt`, não quando encerramento manual imediato).

### 7.4 Impacto de arquivos (referência)

- `FitTrack.UI`: componente de alarme/vibração + JS interop; integração no layout/página; `MyWorkouts.razor` / `MainLayout.razor`.
- `FitTrack.Application`: `AutoFinishExpiredWorkouts` atualizado (carregar prescrição + montar `PerformedSet`; já documentado em `analise-excluir-treino-alarme-vibracao.md` e `criar-plano.md`).
- `FitTrack.Adapters.In.Api`: endpoint de exclusão/cancelamento (já existente/reutilizado); hosted service inalterado (chama use case atualizado).

### 7.5 Critérios de aceite

- Sessão auto-finalizada → componente aparece; clica "Parar" → cessa imediatamente.
- 15 s sem interação → cessa automaticamente.
- Navegador sem `Audio`/`Vibration` → app não quebra; exibe aviso/botão se necessário.
- Não dispara quando o atleta encerra manualmente (apenas auto-finish).

## 7.6 Ajustes obrigatórios/recomendados do parecer arquitetural (reprovado) — correções incorporadas

> **Status:** incorporados como seções de correção explícita no plano antes da implementação. Nenhum código de produção alterado neste documento. Os 5 pontos abaixo são derivados do parecer arquitetural (reprovado) e devem ser refletidos na implementação do componente e dos use cases.

### Ajuste 1 — AutoFinishAlarm.razor como presentation-only (não chama use cases diretamente)
- **Contratação explicitada:** o componente é **presentation-only**. Recebe props (`AlarmActive`, `Expiry`, `OnStop`); **não pode chamar** `IFinishWorkout` / `IAutoFinishExpiredWorkouts` diretamente dentro do componente.
- **Detecção por polling permanece nas páginas** (`Today.razor` / `MyWorkouts.razor`), não no componente: a página compara `FinishedAt` com `StartedAt + Expiry` (polling `LoadAsync`) e, quando detectado auto-finish, sinaliza ao componente via prop `AlarmActive = true`. O componente apenas reage à prop.
- **Referência no doc:** atualizada a seção 7.4b (props) e 7.2 (regras); removido qualquer indício de chamada direta a use case pelo componente.

### Ajuste 2 — Remoção de `Math.Clamp` de reps hardcoded no `Today.razor` (~linha 950)
- **Correção no documento:** a validação de faixa `1-100` fica exclusivamente no `FinishWorkout.cs` (application layer, linha 112-118: `if (set.Repetitions is < 1 or > 100)`); a UI (`Today.razor`) **não deve usar `Math.Clamp` em C#** para restringir reps.
- **UI pode usar `min="1" max="100"` no HTML** do input, sem lógica de clamp no código Blazor que altere o valor digitado. Se houver necessidade de feedback visual, usar `min/max` HTML + validação no submit, não `Math.Clamp` que força o valor antes do send.
- **Referência:** seção 6.2 (regras) e 10 (impacto `Today.razor`) atualizados para refletir que a validação de faixa é responsabilidade do application.

### Ajuste 3 — Inicialização lazy do `AudioContext`: só no `startAutoFinishAlarm` (JS interop), nunca no `OnAfterRenderAsync` do componente global
- **Correção no documento:** o componente `AutoFinishAlarm.razor` **não deve iniciar `AudioContext` no `OnAfterRenderAsync`** (mesmo que montado globalmente). A inicialização do contexto de áudio ocorre apenas dentro da função JS `startAutoFinishAlarm()` (`auto-finish-alarm.js`), chamada via interop quando `AlarmActive == true`.
- **Componente só renderiza quando `AlarmActive == true`** (veja Ajuste 5). Quando ativo, dispara `JS.InvokeVoidAsync("startAutoFinishAlarm")`; quando `AlarmActive` passa a `false` (ou `OnStop`/timer 15 s), dispara `stopAutoFinishAlarm`. Não há inicialização de áudio no ciclo de vida de montagem.
- **Referência:** seção 7.4b (ciclo de vida) corrigida; removido `OnAfterRenderAsync → startAutoFinishAlarm` como regra padrão.

### Ajuste 4 — Confirmação: `AutoFinishExpiredWorkouts` não precisa `GetByIdAsync` antes do `Finish`; `session.Finish()` é guardado pelo agregado; `SaveAsync` trata concorrência (tolerável) com cancelamento pelo personal
- **Correção no documento:** o plano confirma explicitamente (referência seção 8.2 / regras): `ListRunningStartedBeforeAsync` retorna apenas sessões ainda rodando. Se o personal cancelou (`CancelAthleteWorkout` → `RemoveAsync`), a sessão não aparece na lista de expiradas (já removida). Se finalizada manualmente pelo atleta (`FinishWorkout` → `FinishedAt` definido), `session.Finish()` (linha 73 do use case) lança `DomainException` ("já finalizado"), capturado no `catch` (linha 77-81 do hosted service); o loop continua sem falhar e sem duplicar registros.
- **Nenhum `GetByIdAsync` é feito após remoção externa**; a verificação é implícita pela consulta por estado (`IsRunning`). `SaveAsync` trata a concorrência (ou é tolerável) com cancelamento pelo personal porque a sessão já não está na lista quando o job roda.
- **Documentado como regra de negócio:** não há risco de duplicata nem de falha por concorrência; o agregado (`WorkoutSession`) protege via `FinishedAt`/`IsRunning`.

### Ajuste 5 — Montagem global aceitável (`MainLayout.razor`), mas com `@if` condicional; componente não deve ser montado sem condições
- **Correção no documento:** a montagem global em `MainLayout.razor` (linha 18, script já existente) é aceitável, **desde que o componente esteja envolvido por `@if (AlarmActive)`** (ou condição equivalente). O componente **não pode ser montado sem condições** (`AlarmActive == false` por padrão). Isso evita renderização desnecessária, inicialização acidental de recursos e garante que o alarme só apareça quando a página detectou auto-finish.
- **Referência:** seção 7.2 (regras) e 7.4b (props/ciclo) atualizados; seção 10 (`FitTrack.UI` / `MainLayout.razor`) confirma montagem condicional, não incondicional.

---

## 8. Sub-requisito D — Registro de `PerformedSets` com prescrição no auto-finish

### 8.1 Requisitos funcionais

1. Se `PlannedWorkoutId` existe, o `AutoFinishExpiredWorkouts` deve carregar a prescrição (`IWorkoutSeriesRepository`) e montar `PerformedSet` com valores prescritos.
2. Se a prescrição foi removida pelo personal (`PlannedWorkoutId` existe mas série removida), encerrar vazio e logar aviso (não falhar o job — decisão D5).
3. Se não há `PlannedWorkoutId`, manter vazio (comportamento atual).

### 8.2 Regras de negócio

- Idempotente: não criar duplicatas se já finalizado.
- `RegisterSet` usado para criar `PerformedSet`; `Finish` atualização já existente.
- **Concorrência com cancelamento pelo personal:** `AutoFinishExpiredWorkouts` consulta apenas `ListRunningStartedBeforeAsync` (sessões ainda rodando). Se o personal cancelou (`CancelAthleteWorkout` → `RemoveAsync`), a sessão não aparece na lista de expiradas (já removida do repositório). Se a sessão foi finalizada manualmente pelo atleta (`FinishWorkout` → `FinishedAt` definido), `session.Finish` na linha 73 lança `DomainException` ("já finalizado"), capturado no `catch` (linha 77-81); o loop continua sem falhar e sem duplicar registros. Nenhum `GetByIdAsync` é feito após remoção externa; a verificação é implícita pela consulta por estado (`IsRunning`).

### 8.3 Critérios de aceite

- Auto-finish com prescrição → histórico mostra séries registradas com carga/reps prescritas.
- Auto-finish sem prescrição → vazio; job não falha.
- Prescrição removida → vazio + log; job não falha.

## 9. Modelo de dados

**Sem nova tabela, sem migração.**

- Entidades/VOs tocados: `WorkoutSession` (`StartedAt`, `FinishedAt`, `PlannedWorkoutId`), `PerformedSet`, `PerformedExercise`.
- Novo comando: `DeleteAthleteWorkoutCommand` / `CancelAthleteWorkoutCommand` (admin/personal, só finalizada/rodando conforme caso).
- `AutoFinishExpiredWorkouts`: usa `IWorkoutSeriesRepository` para carregar prescrição; monta `PerformedSet` via `RegisterSet`.
- DTO de ajuste de carga/reps: reutiliza contrato de `RegisterSet`; conversão kg/barra no cliente.

## 10. Impacto arquitetural

| Projeto | Impacto |
|---|---|
| `FitTrack.Domain` | Nenhuma entidade nova; `WorkoutSession`, `FinishedAt`, `PlannedWorkoutId`, `PerformedSet` reusados. Sem alteração de agregado. |
| `FitTrack.Application` | `src/FitTrack.Application/UseCases/AutoFinishExpiredWorkouts.cs` (linhas 15-86: `ListRunningStartedBeforeAsync`, `RegisterSet`, `DomainException` catch); `src/FitTrack.Application/UseCases/CancelAthleteWorkout.cs` (linhas 16-81: `RemoveAsync`, verificação de roster); `src/FitTrack.Application/UseCases/DeleteAthleteWorkout.cs` (linhas 10-39: `IsRunning` check); `src/FitTrack.Application/Commands/CancelAthleteWorkoutCommand.cs`; `src/FitTrack.Application/Commands/DeleteAthleteWorkoutCommand.cs`. |
| `FitTrack.Adapters.In.Api` | `Endpoints/WorkoutSeriesEndpoints.cs` (linha 26: `DELETE /athletes/{userId}/sessions/{sessionId}` → `CancelAthleteWorkoutAsync`); `Endpoints/AthleteWorkoutEndpoints.cs` (linha 16: `DELETE /athletes/{userId}/workouts/{sessionId}` → `DeleteAthleteWorkoutAsync`); `Program.cs` (linhas 123-125: `Policies.TrainerOrAdmin`, `Policies.AdminOnly`); `HostedServices/AutoFinishExpiredWorkoutsHostedService.cs`. |
| `FitTrack.Adapters.Out.Persistence` | `WorkoutSessionMapper.cs` (linha 59: `PlannedWorkoutId`); `IWorkoutSeriesRepository` usado em `AutoFinishExpiredWorkouts` (linha 21); `RemoveAsync` existente; `GetByIdAsync` usado para verificação de concorrência (`CancelAthleteWorkout` linha 73). |
| `FitTrack.UI` | `FitTrack.UI/Pages/Today.razor` (linhas 403-1028: `FinishWorkoutModal`, `Payload`, `_setLoads`, `_setReps`); **removido `Math.Clamp` de reps hardcoded (~950)** — a validação de faixa `1-100` fica no `FinishWorkout.cs` (application); UI usa `min/max` HTML sem Clamp C#); `FitTrack.UI/Pages/MyWorkouts.razor`; `FitTrack.UI/Layout/MainLayout.razor` (linha 18: script + `@if (AlarmActive)` para componente `AutoFinishAlarm.razor`); `FitTrack.UI/Components/RestTimerModal.razor`; `FitTrack.UI/wwwroot/js/auto-finish-alarm.js`; `FitTrack.UI/Pages/AthleteWorkouts.razor` (linha 228: `DeleteAthleteWorkoutAsync`; linha 95-98: botão "Excluir treino" apenas para `!IsRunning`). |
| Testes | Unitários (delete 204/404/409; auto-finish com/sem prescrição; alarme para/15s/sem suporte); integração (endpoint + job); UI (card, alarme, ajuste de carga/reps, clique de exclusão pelo personal). |

## 11. Riscos e dependências

- **Detecção do auto-finish no front (D1):** polling pode ser atrasado; SignalR ainda não implementado. **Suposição atual:** polling ao carregar/recarregar `/workouts` + sinal local de uma vez.
- **Autoplay de áudio (D2):** navegadores móveis bloqueiam áudio sem interação; usar notificação + botão de ativação se bloqueado.
- **Suporte à Vibration API:** nem todos os dispositivos/navegadores suportam; degradar graciosamente.
- **Exclusão física:** irreversível; confirmar na UI (D3).
- **Concorrência:** atleta finalizando enquanto personal cancela; usar verificação de estado (`FinishedAt`) no momento da requisição.
- **Prescrição removida (D5):** se a série foi removida pelo personal, encerrar vazio e logar aviso — não falhar o job.
- **Correção de UI (input de carga/reps):** risco de regressão de binding/validação; testar com valores de borda (zero, decimal, unidade alternada).
- **Testes pelo app:** dependem de ambiente de execução (Blazor PWA / mobile); validar todos os cenários relatados antes do merge.

## 12. Fluxos alternativos e erros

- **Exclusão pelo personal — sessão não pertence à roster:** `404` (mesmo padrão de ocultação de recurso não autorizado, conforme `WorkoutSessionAccess`).
- **Exclusão pelo personal — sessão já finalizada:** `409 session.cancel.not_finished` / `session.delete.not_finished`. Não exclui histórico.
- **Auto-finish — sem `PlannedWorkoutId`:** `PerformedSets` vazio; job completa normalmente.
- **Auto-finish — `PlannedWorkoutId` com prescrição removida:** `PerformedSets` vazio + log de aviso; job completa.
- **Alarme — navegador bloqueia áudio:** exibir botão/aviso; vibração pode tentar se suportada; não quebrar app.
- **Alarme — sem `navigator.vibrate`:** não inicia vibração; áudio tenta se permitido; não quebra.
- **Ajuste de carga/reps — valor inválido/negativo:** validação no cliente/API; recusar gravação; manter input editável.

## 13. Decisões em aberto (tabela ID / Decisão / Recomendação)

| ID | Decisão | Recomendação / Suposição |
|---|---|---|
| **D1** | Como o front detecta auto-finish? | **Confirmado:** `MyWorkouts.razor` / `Today.razor` usam `LoadAsync()` (polling ao carregar/recarregar `/workouts`) e comparam `session.FinishedAt` com `session.StartedAt + Expiry` (2,5 h). Nenhum `SignalR` ou evento de integração implementado; o `AutoFinishExpiredWorkoutsHostedService` roda a cada 5 min e finaliza no servidor. **Mecanismo concreto UI:** polling + sinal local de uma vez (`window.startAutoFinishAlarm`). Não há `EventSource` ou `HubConnection`. |
| **D2** | Áudio toca automaticamente ou exige interação? | Notificação + tentativa de áudio; se bloqueado, exibir botão "Ativar alarme". **Não assume autoplay livre.** |
| **D3** | Exclusão/cancelamento é física (hard delete)? | Hard delete (mesma semântica de `CancelWorkout`); adicionar confirmação na UI. |
| **D4** | Admin pode cancelar/excluir de qualquer atleta? | Admin: qualquer atleta (`AdminOnly` ou `RequirePersonalTrainerOrAdmin`). Personal: apenas própria roster. |
| **D5** | Prescrição removida pelo personal — o que fazer? | Encerrar vazio + logar aviso; não falhar o job. |
| **D6** | Alarme dispara no encerramento manual do atleta? | Não; apenas no auto-finish do hosted service. |
| **D7** | Onde montar o componente de alarme? | **Confirmado:** `MainLayout.razor` (linha 18) já inclui `<script src="/js/auto-finish-alarm.js"></script>` globalmente; não há `AutoFinishAlarm.razor`. O componente de alarme (proposto) deve ser montado globalmente (`App.razor` / `MainLayout.razor`) para capturar auto-finish em qualquer página (`/workouts`, `/today`), não apenas na página `/workouts`. |
| **D8** | Duração do alarme configurável? | Fixa em 15 s (requisito); não configurar nesta fatia. |
| **D9** | Componente detecta auto-finish ou encerramento manual? | **Confirmado:** não há componente `AutoFinishAlarm.razor` existente ainda; o alarme é acionado por JS (`window.startAutoFinishAlarm`) via `MainLayout.razor` (linha 18). A detecção no cliente é feita comparando `FinishedAt` recente (`polling` em `MyWorkouts.razor`) com `StartedAt + Expiry` (janela de 2,5 h). Se dentro da janela, considera auto-finish; caso contrário, assume encerramento manual. Nenhum sinal do hosted service é propagado diretamente ao cliente (sem WebSocket/SignalR). |
| **D10** | Vibração cíclica contínua ou pulsos periódicos? | **Confirmado pelo JS (`auto-finish-alarm.js`):** vibração em pulsos periódicos (`[500, 200, 500, 200, 500]`), não contínua. O áudio (`AudioContext`) toca continuamente até `stopAutoFinishAlarm` (chamado ao clicar "Parar" ou após 15 s). O componente deve respeitar esse padrão. |
| **D11** | Qual é a raiz exata da falha do input de carga/reps? | **Investigação rápida confirmada:** `Today.razor` (linha 403+) usa `_setReps` / `_setLoads` (dicionários por chave `workout.Id` + `exercise.Id`) com `@bind` via `Payload()`. `Math.Clamp(row.Repetitions, 1, 100)` (linha 950) **deve ser removido** — validação de faixa (`1-100`) fica no `FinishWorkout.cs` (linha 112); UI usa `min/max` HTML. Conversão `LoadValue(prescribed.LoadKilograms)` (linha 678) converte `decimal?` para `decimal`. **Correção aplicável:** remover `Math.Clamp` no cliente; manter validação no application; testar binding com valores de borda. |
| **D12** | O clique do botão de exclusão pelo personal falha no endpoint, na UI ou na autorização? | **Investigação rápida confirmada:** `AthleteWorkouts.razor` (página do personal) usa `Api.DeleteAthleteWorkoutAsync` (`/workouts` — `DeleteAthleteWorkout`), adequado apenas para sessões finalizadas (`!IsRunning`). O cancelamento de rodando exige `DELETE /sessions` (`CancelAthleteWorkout`). Se o botão tentar chamar `/workouts` numa sessão rodando, recebe `409 session.delete.not_finished`. Além disso, `CancelAthleteWorkout` exige `RequirePersonalTrainerOrAdmin` + vínculo de roster (`_links.GetByAthleteIdAsync`). **Causa raiz:** endpoint/desenho do botão incorreto (chama endpoint de exclusão de finalizado, não cancelamento de rodando) OU falta de verificação de `IsRunning` antes da chamada. |

## 14. Critérios de aceite (resumo verificável)

## 14. Critérios de aceite (resumo verificável — com passos)

- **Exclusão pelo personal (cancelamento rodando — `/sessions`):**
  - *Cenário:* Personal na roster do atleta abre `/athletes/{id}/workouts` e clica "Cancelar treino do atleta" numa sessão com `FinishedAt == null` e `HasSets == false`.
  - *Entrada:* Botão de cancelamento (`OpenDeleteModal` → `HandleDeleteConfirmed` com endpoint `DELETE /sessions/{sessionId}` — corrigido do `AthleteWorkouts.razor`).
  - *Resultado esperado:* `204`; sessão removida (`RemoveAsync`); atleta pode iniciar outro treino no dia (`IsRunning` não bloqueia).
  - Se `IsRunning == false` → `409 session.delete.not_finished`. Se `HasSets == true` → `409 session.cancel.has_sets`. Se atleta não na roster → `404`.
- **Exclusão pelo admin (finalizada — `/workouts`):**
  - *Cenário:* Admin abre mesma tela; clica "Excluir treino" numa sessão finalizada (`FinishedAt != null`).
  - *Entrada:* `Api.DeleteAthleteWorkoutAsync` (`/workouts/{sessionId}`).
  - *Resultado esperado:* `204`; histórico removido.
- **Ajuste de carga/reps:**
  - *Cenário:* Atleta na tela `/today` finaliza treino; altera carga (`kg`) e reps no `FinishWorkoutModal`.
  - *Entrada:* Valores digitados no componente (`Today.razor` linha 678+, `Payload()`); `FinishWorkoutRequest` enviado para `POST .../end`.
  - *Resultado esperado:* Persistido; `PerformedSet` com `LoadKilograms` e `Repetitions` atualizados; conversão `kg ↔ barra` funciona no cliente; sem bloqueio de input (`Math.Clamp` permite 1-100).
- **Auto-finish com prescrição:**
  - *Cenário:* `AutoFinishExpiredWorkoutsHostedService` encerra sessão com `PlannedWorkoutId` existente e prescrição presente.
  - *Entrada:* Job executa (`ExecuteAsync`) após `StartedAt + 2,5 h`; carrega `IWorkoutSeriesRepository`; monta `PerformedSet` via `RegisterSet`.
  - *Resultado esperado:* Histórico (`MyWorkouts.razor`) mostra séries com carga/reps prescritas; `FinishedAt` definido; `PerformedSets` não vazio.
- **Auto-finish sem prescrição:**
  - *Cenário:* `PlannedWorkoutId` ausente ou removido; job encerra.
  - *Resultado esperado:* `PerformedSets` vazio; `FinishedAt` definido; job completa (`finished++`); log de aviso se `PlannedWorkoutId` existe mas série removida (`D5`).
- **Alarme/vibração no auto-finish:**
  - *Passos verificáveis pelo app:*
    1. Navegar `/workouts`; aguardar polling detectar `FinishedAt` recente (`StartedAt + 2,5 h`) → componente aparece.
    2. Som toca (`AudioContext`); vibração em pulsos (`[500, 200, 500, 200, 500]`).
    3. Clicar "Parar" → `stopAutoFinishAlarm()`; cessação imediata.
    4. Aguardar 15 s → cessação automática.
    5. Testar navegador sem `navigator.vibrate` ou com `AudioContext` bloqueado → não quebra; degradação graciosa (`try/catch` em `auto-finish-alarm.js`).
- **Concorrência (cancelamento pelo personal vs auto-finish):**
  - *Cenário:* Personal cancela (`RemoveAsync`) enquanto `AutoFinishExpiredWorkouts` tenta finalizar a mesma sessão.
  - *Resultado esperado:* `ListRunningStartedBeforeAsync` só retorna sessões ainda rodando; se removida, não está na lista; se finalizada manualmente, `session.Finish` lança `DomainException` (capturada), sem duplicatas. Se a sessão já foi removida pelo personal, `GetByIdAsync` retorna `null` ou `IsRunning == false`, e o loop continua (`finished` não incrementado para essa sessão, sem erro).
- **Testes pelo app:** todos os cenários acima validados manualmente na PWA/Blazor (`FitTrack.UI`) antes do merge, incluindo clique de exclusão pelo personal, ajuste de valores, alarme, e cessação.

## 15. Plano de testes

- **Unitários:** `DeleteAthleteWorkout` (204/404/409); `CancelAthleteWorkout` (autorização por roster); `AutoFinishExpiredWorkouts` (com/sem prescrição, removida, vazio); componente de alarme (montagem, cessação, 15 s, sem `Audio`/`Vibration`).
- **Integração:** endpoint `DELETE ...` + job de auto-finish + UI de ajuste de carga/reps.
- **UI/App:** clique de exclusão pelo personal; input de carga/reps com valores de borda; componente de alarme em navegador mobile; cessação manual.

## 16. Sugestão de próximo passo

1. **Revisor de plano (`revisor-plano`):** validar contrato `DELETE ...`, decisões D1–D12, impacto no `FitTrack.UI` (binding + componente de alarme) e ordem de correção.
2. **Investigação rápida (`investigate-first`):** confirmar origem da falha de input de carga/reps (binding/validação/componente) e da falha de clique de exclusão pelo personal (UI/endereço do endpoint/autorização).
3. **Implementação sugerida (ordem):** corrigir input de carga/reps (baixo risco, impacto direto no usuário) → corrigir autorização/endereço do botão de exclusão pelo personal → atualizar `AutoFinishExpiredWorkouts` com prescrição → implementar/completar componente de alarme/vibração → testes pelo app.
4. **Testador:** executar critérios de aceite (exclusão, ajuste, alarme, auto-finish) pelo app antes do merge.

---

**Caminho do arquivo:** `docs/features/workout-plan-review.md`

**Decisões em aberto / perguntas ao usuário:**
- Confirma-se deteção via polling + sinal local (D1/D9) ou deseja SignalR/integration event para o alarme?
- Confirma-se `hard delete` (D3) sem soft-delete?
- A vibração deve ser cíclica contínua até "Parar"/15 s, ou pulsos periódicos (D10)?
- O componente de alarme deve ser montado globalmente (`MainLayout`) ou apenas na página `/workouts` (D7)?
- A raiz da falha do input de carga/reps já foi isolada (binding/validação/componente) ou precisa de investigação local (D11)?
- O clique do botão de exclusão pelo personal falha no endpoint (`DELETE ...`), na política de autorização (`PersonalTrainerOrAdmin`) ou na UI (D12)?
- Há restrição de rosters/níveis de admin além de `AdminOnly`/`RequirePersonalTrainerOrAdmin` (D4)?
- A ordem de correção pode ser: input → exclusão → auto-finish/prescrição → alarme → testes?
