# Alarme ao encerrar, auto-finish com prescrição, exclusão de histórico por admin e responsividade do volume

- **Slug:** `alarme-encerramento-admin-historico`
- **Status:** proposto (2026-10-26)
- **Data:** 2026-10-26
- **Origem:** quatro requisitos do usuário: (A) alarme e vibração no front móvel quando o background service encerra um treino; (B) o `AutoFinishExpiredWorkouts` deve encerrar com os valores prescritos do personal, não vazio; (C) novo endpoint para o administrador excluir um treino JÁ FINALIZADO do histórico de um atleta; (D) corrigir a responsividade do card "Volume feito" em `MyWorkouts.razor`, que estica para baixo em telas estreitas.

## 1. Contexto e problema

### 1.1 Alarme e vibração no encerra automático (requisito A)

O hosted service `AutoFinishExpiredWorkoutsHostedService` roda a cada 5 minutos e encerra sessões esquecidas (rodando há ≥ 2,5 h). Quando isso acontece, o atleta no celular (PWA Blazor em `FitTrack.UI`) não recebe nenhum sinal: não há áudio, nem vibração, nem aviso na tela. O treino some para o histórico sem o atleta perceber que o sistema encerrou.

### 1.2 Auto-encerramento encerra vazio (requisito B)

O `AutoFinishExpiredWorkouts` encerra a sessão com `session.Finish(session.StartedAt + Expiry, null, null)` — sem séries, sem intensidade. Já existe o `FinishWorkout`, que completa a sessão com a prescrição (`PlannedWorkoutId` → `WorkoutSeriesWorkout` → `PrescribedExercises`), registrando uma `PerformedSet` por série prescrita de cada exercício. O auto-finish deveria fazer o mesmo: ao encerrar uma sessão esquecida que veio de um treino prescrito, preencher as `PerformedSets` com a prescrição do personal, como se o atleta tivesse seguido o treino.

### 1.3 Não há exclusão de treino finalizado pelo admin (requisito C)

O `CancelAthleteWorkout` existente cancela apenas sessão **rodando** e sem séries (histórico intocado). Não há caminho para o administrador excluir do histórico um treino já finalizado de um atleta. É necessário um endpoint dedicado, apenas Admin, com contratos claros: `204 No Content`, `404` se não existir ou não pertencer ao atleta, `409` se ainda estiver rodando (só se cancela treino em andamento, não exclui).

### 1.4 Card "Volume feito" estica o layout (requisito D)

Em `MyWorkouts.razor`, o card "Volume feito" do `stat-grid` coloca a linha `Prescrito … · Feito … · média/exercício` dentro de um `<small>` dentro do `stat-value`. Em telas estreitas esse texto estica o card verticalmente, desalinhando o grid de estatísticas. É necessário mover o detalhe para abaixo do valor principal, com `flex-wrap`, fonte reduzida, e impedir que o card estique para baixo.

## 2. Objetivo e valor

- **Atleta (móvel):** fica sabendo, imediatamente, quando o sistema encerrou seu treino esquecido, por meio de alarme sonoro e vibração que duram até 15 segundos ou até clicar em "Parar".
- **Histórico confiável:** o auto-finish registra o que o personal prescreveu (séries, repetições, cargas) em vez de deixar a sessão vazia, preservando a aderência ao plano.
- **Administrador:** pode excluir do histórico um treino finalizado de um atleta, com proteção contra exclusão de treino em andamento (`409`) e inexistente (`404`).
- **Layout móvel:** o card "Volume feito" não quebra o `stat-grid` em telas estreitas; o detalhe fica abaixo do valor, com fonte reduzida e `flex-wrap`.

## 3. Atores envolvidos

- **Atleta** — usa o app móvel/PWA Blazor, recebe o alarme e a vibração ao encerrar automático, visualiza o histórico em `/workouts`.
- **Administrador** — exclui treino já finalizado de um atleta pelo novo endpoint dedicado.
- **Personal trainer** — prescreve o treino (inalterado); não entra no escopo de exclusão deste requisito (apenas admin).
- **Sistema (API + hosted service)** — encerra sessões esquecidas e expõe o endpoint de exclusão.

## 4. Escopo

### 4.1 Incluído

1. Componente de UI no Blazor (`FitTrack.UI`) que dispara alarme (áudio) e vibração (Vibration API) quando o back encerra um treino por auto-finish, com duração de até 15 segundos ou até o usuário clicar em "Parar"; uso das APIs do navegador (Audio/Notification/Vibration) quando disponíveis, com degradação graciosa quando não houver suporte.
2. Alteração do `AutoFinishExpiredWorkouts` (use case de aplicação + hosted service) para, ao finalizar uma sessão esquecida com `PlannedWorkoutId`, registrar as `PerformedSets` com os valores prescritos (`PlannedWorkoutId → WorkoutSeriesWorkout → PrescribedExercises`), como se o atleta tivesse seguido o treino.
3. Novo use case/endpoint dedicado para o administrador excluir uma sessão JÁ FINALIZADA de um atleta (`204 No Content`, `404` se não existir/não pertencer ao atleta, `409` se ainda estiver rodando); apenas Admin.
4. Correção de responsividade do card "Volume feito" em `MyWorkouts.razor` e `app.css` (`.stat`, `.stat-value small`, `.stat-grid`): detalhe abaixo do valor principal, `flex-wrap`, fonte reduzida, card não estica para baixo.

### 4.2 Não incluído

- Notificação push/e-mail ao atleta quando o treino auto-finaliza (apenas alarme local no dispositivo, requisito A).
- Alarme quando o atleta encerra manualmente o treino (apenas quando o background service encerra — decisão D6).
- Alteração da prescrição do personal (continua uma carga/reps por exercício; o auto-finish apenas lê a prescrição existente).
- Exclusão de treino rodando (continua via `CancelAthleteWorkout` / `CancelWorkout`).
- Endpoint de exclusão para personal (requisito C especifica **admin**; decisões D4).
- Persistência de preferência de alarme/vibração (disparo automático sem configuração nesta fatia).
- Soft-delete de histórico (exclusão física, decisão D3).

## 5. Sub-requisito A — Alarme e vibração no front ao encerrar (auto-finish)

### 5.1 Requisitos funcionais

1. Quando o background service encerra um treino (auto-finish), o front móvel/PWA Blazor deve tocar um alarme sonoro e vibrar.
2. O alarme e a vibração duram **até 15 segundos** ou até o usuário clicar em "Parar", o que ocorrer primeiro.
3. Usar APIs do navegador (Audio para som, Vibration para vibração, Notification opcional) quando disponíveis.
4. Componente de UI no Blazor (`FitTrack.UI`), reutilizável e não bloqueante.
5. Em navegador/dispositivo sem suporte a áudio ou vibração, a página não quebra (sem exceção não tratada).

### 5.2 Regras de negócio

- A vibração só é acionada se `navigator.vibrate` existir.
- O áudio não trava a UI; se o navegador bloquear autoplay, exibe notificação/botão para o usuário ativar (decisão D2).
- O alarme dispara apenas quando o encerramento vem do serviço (auto-finish), não do encerramento manual do atleta (decisão D6).
- Após o clique em "Parar" ou os 15 segundos, áudio e vibração cessam imediatamente.

### 5.3 Fluxo principal

1. O `AutoFinishExpiredWorkoutsHostedService` encerra uma sessão esquecida (auto-finish).
2. O front detecta o encerramento automático (ao carregar/recarregar o histórico `/workouts`, decisão D1).
3. O componente de alarme é montado: toca som, inicia vibração cíclica e exibe o botão "Parar".
4. O usuário clica em "Parar" ou aguarda 15 segundos → o alarme e a vibração param.

### 5.4 Arquivos

- Novo componente Blazor de alarme/vibração (ex.: `AutoFinishAlarm.razor`) + JS interop para Audio/Vibration/Notification.
- Integração no layout (`MainLayout.razor` ou `App.razor`, decisão D7) e na página `/workouts` (`MyWorkouts.razor`) para detectar o auto-finish.

### 5.5 Critérios de aceite (A)

1. Dado um treino auto-finalizado, quando o atleta abre o histórico, então aparece o alarme com botão "Parar".
2. Dado o alarme tocando, quando o usuário clica "Parar", então o áudio e a vibração cessam imediatamente.
3. Dado o usuário não interagir, então o alarme e a vibração param após 15 segundos.
4. Em navegador sem suporte a Vibration/Audio, a página renderiza sem exceções não tratadas.

## 6. Sub-requisito B — Auto-encerramento com valores prescritos (background service)

### 6.1 Requisitos funcionais

1. Ao finalizar uma sessão esquecida, o `AutoFinishExpiredWorkouts` deve registrar as séries com os valores prescritos da série de treino.
2. O caminho de dados é `PlannedWorkoutId → WorkoutSeriesWorkout → PrescribedExercises`.
3. Preencher as `PerformedSets` com a prescrição do personal (uma `PerformedSet` por série prescrita de cada exercício prescrito), como se o atleta tivesse seguido o treino prescrito.
4. Manter a intensidade nula (o sistema não adivinha o esforço do atleta).

### 6.2 Regras de negócio

- Só se aplica a sessões com `PlannedWorkoutId` preenchido (vieram de um plano prescrito do personal).
- Se a prescrição do workout não existir mais (workout removido pelo personal), encerrar vazio (comportamento atual) e logar aviso — não falhar o job (decisão D5).
- Idempotente: sessão já finalizada não é tocada (`session.finish.already_finished` capturado, como hoje).
- O `FinishedAt` continua sendo `StartedAt + 2,5 h` (duração limitada ao limiar, conforme `criar-plano` D4).

### 6.3 Fluxo principal

1. O hosted service lista sessões rodando com `StartedAt <= UtcNow - 2,5 h`.
2. Para cada sessão expirada com `PlannedWorkoutId`, carrega o workout e seus `PrescribedExercises`.
3. Para cada exercício prescrito, registra uma `PerformedSet` por série prescrita, com carga e repetições da prescrição.
4. Finaliza a sessão com `FinishedAt = StartedAt + Expiry` e intensidade nula.

### 6.4 Arquivos

- `FitTrack.Application/UseCases/AutoFinishExpiredWorkouts.cs` (passar a receber `IWorkoutSeriesRepository` e montar os `PerformedSet` da prescrição).
- `FitTrack.Application/Ports/In/IAutoFinishExpiredWorkouts.cs` (assinatura inalterada: `ExecuteAsync` retorna `int`).
- `FitTrack.Adapters.In.Api/Program.cs` (registro do use case — já injetado; hosted service inalterado).
- Domínio reusado: `WorkoutSession.PlannedWorkoutId`, `WorkoutSession.Finish`, `WorkoutSession.RegisterSet`, `PerformedSet`, `PerformedExercise`.
- Testes unitários e de integração do uso case.

### 6.5 Critérios de aceite (B)

1. Dada uma sessão rodando esquecida com `PlannedWorkoutId` e prescrição de N exercícios, quando o job executa, então a sessão é finalizada com uma `PerformedSet` por série prescrita de cada exercício (carga e repetições prescritas).
2. Dada uma sessão sem `PlannedWorkoutId`, quando o job executa, então é finalizada vazia (comportamento atual preservado).
3. Dada uma sessão já finalizada, quando o job executa, então não é alterada (idempotente).
4. A intensidade da sessão auto-finalizada continua nula.

## 7. Sub-requisito C — Admin exclui treino finalizado do histórico (novo endpoint)

### 7.1 Requisitos funcionais

1. Novo endpoint dedicado para o administrador excluir uma sessão JÁ FINALIZADA (histórico) de um atleta.
2. Apenas **Admin** (política `AdminOnly`; decisão D4 sobre personal).
3. Resposta `204 No Content` em caso de sucesso.
4. `404` se a sessão não existir ou não pertencer ao atleta informado.
5. `409` se a sessão ainda estiver rodando (não pode excluir treino em andamento — só cancelar via `CancelAthleteWorkout`/`CancelWorkout`).

### 7.2 Regras de negócio

- Só permite exclusão de sessão com `FinishedAt != null` (finalizada).
- Sessão rodando (`IsRunning`) → `409 session.delete.not_finished`.
- Autorização no use case, não apenas no endpoint (política `AdminOnly` + verificação de pertencimento `AthleteId`).
- Exclusão física da sessão (`RemoveAsync` já existe no repositório).

### 7.3 Contrato HTTP (proposto)

- `DELETE /athletes/{athleteId:guid}/sessions/{sessionId:guid}` → `204 No Content`
  - Chamador: `Admin` (política `AdminOnly`).
  - `204 No Content` — excluída com sucesso.
  - `404` — atleta ou sessão não existem, ou a sessão não pertence ao atleta.
  - `409 session.delete.not_finished` — a sessão ainda está rodando.
  - `403` — chamador não é admin (decisão D4).

Fluxo alternativo (D2): reusar `DELETE /sessions/{id}` e deixar o use case decidir pelo papel do chamador. A recomendação é o endpoint dedicado, por clareza de escopo e auditoria (mesma linha de `CancelAthleteWorkout`).

### 7.4 Fluxo principal

1. Admin abre o histórico do atleta.
2. Clica "Excluir treino" em uma sessão concluída.
3. API exclui a sessão → `204 No Content`.
4. O treino some do histórico do atleta.

### 7.5 Arquivos

- Novo comando DTO `DeleteAthleteWorkoutCommand(AthleteId, SessionId)` e novo use case `DeleteAthleteWorkout` (ou comando dentro de `FitTrack.Application/UseCases`).
- Novo endpoint em `FitTrack.Adapters.In.Api/Endpoints` (ex.: `AthleteWorkoutEndpoints` ou `SessionEndpoints`), com política `AdminOnly`.
- `FitTrack.Adapters.Out.Persistence`: consulta de sessão por atleta + verificação de `FinishedAt`; `RemoveAsync`.
- UI: gatilho de exclusão na tela do atleta (local a definir, opcional nesta fatia).

### 7.6 Critérios de aceite (C)

1. Dado um admin, quando exclui uma sessão finalizada de um atleta, então a API responde `204` e a sessão deixa de existir.
2. Dada uma sessão inexistente (ou que não pertence ao atleta), quando o admin tenta excluir, então a API responde `404`.
3. Dada uma sessão ainda rodando, quando o admin tenta excluir, então a API responde `409 session.delete.not_finished`.
4. Dado um usuário sem papel de admin, quando tenta excluir, então a API responde `403` (ou `404` por não ter permissão — decisão D4).

## 8. Sub-requisito D — Responsividade do card "Volume feito"

### 8.1 Requisitos funcionais

1. O card "Volume feito" no `MyWorkouts.razor` não deve esticar verticalmente em telas estreitas.
2. O detalhe `Prescrito … · Feito … · média/exercício` deve ficar **abaixo** do valor principal (fora do `<small>` do `stat-value`).
3. Usar `flex-wrap`, fonte reduzida, e o card não estica para baixo.

### 8.2 Regras de negócio

- O valor principal (`@DisplayText.SignedLoad(_summary.DeltaKilograms)` ou `—`) permanece em destaque no `stat-value`.
- A linha de detalhe (`Prescrito X · Feito Y · média/exercício` ou `sem prescrição para comparar`) vira uma linha muted abaixo do valor, com `flex-wrap` e fonte `0.72rem`.
- Sem regressão visual em desktop (≥1024px).

### 8.3 Arquivos

- `FitTrack.UI/Pages/MyWorkouts.razor` (bloco `_summary`, article "Volume feito": mover o `<small>` para uma linha separada abaixo do `stat-value`).
- `FitTrack.UI/wwwroot/css/app.css` (`.stat`, `.stat-value small`, `.stat-grid`): reduzir padding/gap em mobile, permitir `flex-wrap`, fonte reduzida no detalhe.

### 8.4 Critérios de aceite (D)

1. Em 360×780, o card de "Volume feito" não empurra o `.stat-grid` para além da viewport (sem scroll horizontal indesejado).
2. O detalhe fica numa linha muted abaixo do valor principal, com `flex-wrap` e fonte reduzida.
3. Desktop (≥1024px) sem regressão visual do histórico.

## 9. Modelo de dados

Não há novas tabelas nem migrações de banco nesta fatia.

- **Sub A:** nenhuma tabela nova; componente de UI + JS interop (Audio/Vibration/Notification). Não altera o domínio.
- **Sub B:** nenhuma tabela nova; `PerformedSets` são criadas via `WorkoutSession.RegisterSet` (já existente) dentro de `Finish`. `WorkoutSession.PlannedWorkoutId` já existe. O `AutoFinishExpiredWorkouts` precisará carregar a prescrição via `IWorkoutSeriesRepository` (já usado em `FinishWorkout`; hoje o use case do auto-finish injeta apenas `IWorkoutSessionRepository` + `IClock`).
- **Sub C:** nenhuma tabela nova; exclusão física via `RemoveAsync` (já existe). Novo comando `DeleteAthleteWorkoutCommand(AthleteId, SessionId)`.
- **Sub D:** nenhuma mudança de dados; apenas CSS/razor.

Value objects/DTOs tocados:
- `WorkoutSession` — `PlannedWorkoutId`, `Finish`, `RegisterSet` já existem; nenhuma alteração de agregado.
- Novo comando/DTO para exclusão de admin.
- UI: novo componente de alarme; `MyWorkouts.razor`.

## 10. Impacto arquitetural (tabela por projeto)

| Projeto | Impacto |
| --- | --- |
| `FitTrack.Domain` | Nenhuma entidade nova; `WorkoutSession.PlannedWorkoutId`, `Finish` e `RegisterSet` são reusados. Sem alteração de agregado. |
| `FitTrack.Application` | `AutoFinishExpiredWorkouts` passa a receber `IWorkoutSeriesRepository` e montar `PerformedSet` a partir da prescrição (`PlannedWorkoutId → Workout → PrescribedExercises`); novo use case/comando `DeleteAthleteWorkout` (admin, só sessão finalizada). |
| `FitTrack.Adapters.In.Api` | Novo endpoint `DELETE /athletes/{athleteId}/sessions/{sessionId}` com política `AdminOnly`; hosted service inalterado (chama o use case atualizado); registro do novo use case. |
| `FitTrack.Adapters.Out.Persistence` | Consulta de sessão por atleta + verificação de `FinishedAt`; `RemoveAsync` já existe. |
| `FitTrack.UI` | Novo componente Blazor de alarme/vibração (JS interop Audio/Vibration/Notification); `MyWorkouts.razor` (card "Volume feito"); montagem do componente no layout (decisão D7). |
| Testes | Unitários (auto-finish com prescrição; exclusão admin 204/404/409; componente alarme), integração (endpoint delete; job com prescrição), UI (alarme para/para após 15s; card não estica). |

## 11. Plano de testes (unitários, integração, UI)

### Unitários (`tests/FitTrack.Unit`)

- `AutoFinishExpiredWorkouts`: sessão com prescrição registra uma `PerformedSet` por série prescrita; sessão sem `PlannedWorkoutId` encerra vazia; sessão já finalizada não é tocada (idempotência); prescrição ausente não falha o job.
- `DeleteAthleteWorkout`: admin exclui finalizada → sucesso; sessão não encontrada/não pertence ao atleta → 404; sessão rodando → 409; não-admin → 403/404.
- Componente alarme: inicia ao detectar auto-finish; para no clique "Parar"; para após 15 s sem interação; sem suporte a APIs não lança exceção.

### Integração (`tests/FitTrack.Integration`)

- Job de auto-finish persiste `PerformedSet` com cargas/reps prescritas; `GET /sessions` reflete as séries.
- `DELETE /athletes/{id}/sessions/{sessionId}` → 204; sessão removida do histórico; 409 se rodando; 404 se inexistente.

### UI (`tests/FitTrack.UITests`)

- Alarme aparece ao abrir histórico com treino auto-finalizado; botão "Parar" encerra áudio/vibração; para após 15 s.
- `MyWorkouts.razor`: card "Volume feito" não estica em 360×780; detalhe abaixo do valor; desktop sem regressão.

## 12. Riscos e dependências

- **Detecção do auto-finish no front:** o front precisa saber que o back encerrou o treino (polling do histórico, SignalR ou sinal local). Decisão em aberto (D1).
- **Autoplay de áudio:** navegadores móveis bloqueiam áudio sem interação do usuário; a notificação pode ser o gatilho. Decisão D2.
- **Suporte à Vibration API:** nem todos os navegadores suportam `navigator.vibrate`; degradar graciosamente (requisito A.5).
- **Exclusão física de histórico:** remove dados irreversivelmente; considerar confirmação na UI (decisão D3).
- **Concorrência:** atleta finalizando enquanto o admin exclui; usar estado/versão para evitar perda (decisão D4).
- **Prescrição removida:** se o workout da série foi removido pelo personal, não há prescrição para recriar; manter encerramento vazio (decisão D5).
- **Implantação atômica:** novo contrato de endpoint e mudança de comportamento do job devem ir juntos (já é o padrão do time).

## 13. Decisões em aberto (tabela ID/Decisão/Recomendação)

| ID | Decisão | Recomendação |
| --- | --- | --- |
| **D1** | Como o front detecta que o back encerrou o treino (auto-finish)? | Polling do histórico ao carregar `/workouts` + sinal local para disparar o alarme uma vez; avaliar SignalR depois. |
| **D2** | Áudio toca automaticamente ou exige interação do usuário? | Notificação do navegador + tentativa de áudio; se o navegador bloquear, mostrar botão "Ouvir alarme". |
| **D3** | Exclusão do admin é física (hard delete) ou soft-delete? | Hard delete (mesma semântica de `CancelWorkout` que remove); adicionar confirmação na UI. |
| **D4** | Admin pode excluir treino de qualquer atleta ou só de sua roster? | Admin: qualquer atleta (política `AdminOnly`). Personal: não entra neste requisito (apenas admin, conforme especificado). |
| **D5** | O que fazer quando a prescrição do workout não existe mais? | Encerrar vazio (comportamento atual) e logar aviso; não falhar o job. |
| **D6** | O alarme dispara também quando o atleta encerra manualmente? | Não; só quando o background service encerra (auto-finish). |
| **D7** | Onde montar o componente de alarme no Blazor? | `MainLayout.razor` ou `App.razor`, visível em todas as páginas, ativado por estado global. |
| **D8** | Duração do alarme fixa em 15 s ou configurável? | Fixa em 15 s (requisito); não configurar nesta fatia. |

## 14. Sugestão de próximo passo

1. Arquiteto: validar o novo endpoint de exclusão de admin (política `AdminOnly`, contrato 204/404/409), a mudança no `AutoFinishExpiredWorkouts` (carregar prescrição) e as decisões D1, D2, D4, D5.
2. Revisor de plano (`revisor-plano`): revisar este documento para clareza, completude, viabilidade e riscos antes da implementação.
3. Implementação: começar pelo D (CSS, baixo risco), depois C (endpoint admin), B (auto-finish com prescrição) e por fim A (componente de alarme UI).
4. Testador: cobrir os critérios de aceite de cada sub-requisito antes do merge.
