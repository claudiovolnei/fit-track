# Análise e plano: excluir treino finalizado + alarme/vibração no auto-finish

- **Slug:** `analise-excluir-treino-alarme-vibracao`
- **Status:** proposto (2026-10-26)
- **Data:** 2026-10-26
- **Origem:** dois bugs relatados: (1) "excluir treino não funciona" — admin não consegue excluir treino já finalizado do histórico do atleta; (2) "alarme com vibração não está fazendo o esperado" — ao auto-finalizar um treino esquecido, o front móvel não dispara alarme sonoro nem vibração, e quando dispara, não respeita a duração/cessação prevista.

## 1. Contexto e problema

### 1.1 Excluir treino finalizado não funciona (bug 1)

O endpoint para exclusão de sessão ainda não existe ou não está acessível ao admin. Hoje apenas `CancelAthleteWorkout` permite cancelar uma sessão **rodando** (sem séries). Não há caminho para excluir do histórico um treino já finalizado (`FinishedAt != null`). O usuário reporta que a ação de excluir não produz efeito no histórico.

### 1.2 Alarme/vibração não está fazendo o esperado (bug 2)

O `AutoFinishExpiredWorkoutsHostedService` encerra sessões esquecidas a cada 5 minutos, mas o front móvel (`FitTrack.UI`, Blazor PWA) não recebe sinal visual/sonoro/vibratório. Quando algum mecanismo é acionado, não respeita: (a) duração de até 15 segundos; (b) cessação imediata ao clicar em "Parar"; (c) degradação graciosa quando o navegador não suporta `Audio`/`Vibration`. Há também dúvida sobre como o front detecta que o back encerrou (polling, sinal local, SignalR).

## 2. Objetivo e valor

- **Admin:** pode excluir fisicamente uma sessão já finalizada de um atleta via endpoint dedicado, com proteção `409` se ainda estiver rodando e `404` se não existir/não pertencer.
- **Atleta (móvel):** recebe alarme sonoro + vibração quando o sistema encerra automaticamente um treino esquecido, com duração máxima de 15 s ou cessação imediata ao clicar "Parar"; sem quebra em navegadores sem suporte.
- **Histórico confiável:** o auto-finish deve registrar as `PerformedSets` com a prescrição do personal (não vazio), conforme já especificado no requisito B do documento `alarme-encerramento-admin-historico.md`.

## 3. Atores envolvidos

- **Administrador** — usa o novo endpoint de exclusão.
- **Atleta** — usa a PWA Blazor, recebe alarme/vibração no auto-finish.
- **Sistema (API + hosted service)** — encerra sessões e expõe contrato de exclusão.
- **Personal trainer** — prescreve o treino; não entra no escopo de exclusão (apenas admin).

## 4. Escopo

### 4.1 Incluído

1. Novo endpoint/uso case `DeleteAthleteWorkout` (admin, apenas sessão finalizada) com contrato `DELETE /athletes/{athleteId}/sessions/{sessionId}`.
2. Ajuste no `AutoFinishExpiredWorkouts` para registrar `PerformedSets` com prescrição quando `PlannedWorkoutId` existe (requisito B do doc de referência).
3. Componente de UI no Blazor (`FitTrack.UI`) para alarme (áudio) + vibração (`navigator.vibrate`) + botão "Parar", com ciclo de até 15 s.
4. Detecção do auto-finish no front (polling ou sinal local) para disparar o componente.
5. Degradação graciosa (não quebra se APIs do navegador estiverem indisponíveis).

### 4.2 Não incluído

- Notificação push/e-mail ao atleta (apenas alarme local).
- Alarme quando o atleta encerra manualmente (só auto-finish).
- Alteração da prescrição do personal.
- Exclusão de treino rodando (continua via `CancelAthleteWorkout`).
- Endpoint de exclusão para personal (apenas admin).
- Persistência de preferência de alarme/vibração.
- Soft-delete (exclusão física, decisão D3).
- Nova tabela de banco.

## 5. Sub-requisito 1 — Excluir treino finalizado via admin

### 5.1 Requisitos funcionais

1. Apenas admin (`AdminOnly`).
2. Só exclui sessão com `FinishedAt != null`.
3. Resposta `204 No Content` em sucesso.
4. `404` se atleta/sessão não existirem ou sessão não pertencer ao atleta.
5. `409 session.delete.not_finished` se `FinishedAt == null` (ainda rodando).

### 5.2 Regras de negócio

- Autorização no use case, não apenas no endpoint.
- Verificação de pertencimento: `session.AthleteId == athleteId`.
- Exclusão física (`RemoveAsync`).

### 5.3 Contrato HTTP

```http
DELETE /athletes/{userId:guid}/workouts/{sessionId:guid}
```

- **Nota arquitetural (arquiteto):** reutiliza endpoint existente `AthleteWorkoutEndpoints.cs`. Política ajustada para `AdminOnly`; rota `/workouts/` mantida (não criar `/sessions/` duplicada).

- **200/204:** `204 No Content` — excluído.
- **404:** sessão não existe, não pertence ao atleta, ou atleta não encontrado.
- **409:** `session.delete.not_finished` — ainda rodando.
- **403:** não admin (decisão D4, do doc de referência).

### 5.4 Fluxo principal

1. Admin abre histórico do atleta.
2. Clica "Excluir treino" em sessão concluída.
3. API valida admin + `FinishedAt != null` + pertencimento.
4. `RemoveAsync` → `204`.
5. Treino some do histórico.

### 5.5 Critérios de aceite

1. Admin exclui finalizada → `204`; sessão removida.
2. Sessão inexistente ou não pertence ao atleta → `404`.
3. Sessão rodando → `409`.
4. Não-admin → `403` (ou `404` por não ter permissão, decisão D4).

## 6. Sub-requisito 2 — Alarme e vibração no auto-finish

### 6.1 Requisitos funcionais

1. Quando `AutoFinishExpiredWorkoutsHostedService` encerra uma sessão, o front dispara alarme sonoro + vibração.
2. Duração máxima de 15 s ou até clique em "Parar".
3. Uso das APIs do navegador (Audio / Vibration / Notification opcional) com degradação graciosa.
4. Componente reutilizável no Blazor (`FitTrack.UI`), não bloqueante.

### 6.2 Regras de negócio

- Só dispara quando o encerramento vem do serviço (auto-finish), não do manual (decisão D6 do doc de referência).
- Vibração só se `navigator.vibrate` existir.
- Se navegador bloquear autoplay, exibir botão/aviso (decisão D2).
- Após "Parar" ou 15 s, áudio e vibração cessam imediatamente.

### 6.3 Fluxo principal

1. Hosted service encerra sessão expirada.
2. Front detecta auto-finish (polling/recarregamento de `/workouts` ou sinal local — decisão D1).
3. Componente montado: toca som, inicia vibração cíclica, exibe "Parar".
4. Usuário clica "Parar" ou aguarda 15 s → cessação.

### 6.4 Arquivos / impacto

- `FitTrack.UI`: componente `AutoFinishAlarm.razor` (ou equivalente) + JS interop; integração em `MyWorkouts.razor` ou `MainLayout.razor` (decisão D7).
- `FitTrack.Application`: `AutoFinishExpiredWorkouts` atualizado (já no doc de referência).
- `FitTrack.Adapters.In.Api`: endpoint `DELETE ...` (novo use case `DeleteAthleteWorkout`).

## 7. Modelo de dados

**Sem nova tabela, sem migração.**

- Sub-requisito 1: exclusão física via `RemoveAsync` (já existe no repositório). Novo comando `DeleteAthleteWorkoutCommand(AthleteId, SessionId)`.
- Sub-requisito 2: nenhuma tabela nova; `WorkoutSession.PlannedWorkoutId`, `Finish`, `RegisterSet` reusados. `PerformedSets` criadas via `RegisterSet` dentro do `Finish` atualizado.
- Entidades/VOs tocados: `WorkoutSession`, `PerformedSet`, `PerformedExercise`; DTO de comando para exclusão.

## 8. Impacto arquitetural

| Projeto | Impacto |
|---|---|
| `FitTrack.Domain` | Nenhuma entidade nova; `WorkoutSession`, `FinishedAt`, `PlannedWorkoutId` reusados. Sem alteração de agregado. |
| `FitTrack.Application` | `AutoFinishExpiredWorkouts` passa a carregar prescrição (`IWorkoutSeriesRepository`) e montar `PerformedSet`; novo use case `DeleteAthleteWorkout` (admin, só finalizada). |
| `FitTrack.Adapters.In.Api` | Novo endpoint `DELETE /athletes/{athleteId}/sessions/{sessionId}` com `AdminOnly`; hosted service inalterado (chama use case atualizado). |
| `FitTrack.Adapters.Out.Persistence` | Consulta de sessão por atleta + verificação `FinishedAt`; `RemoveAsync` já existe. |
| `FitTrack.UI` | Novo componente Blazor de alarme/vibração (JS interop Audio/Vibration); integração no layout/página `/workouts`; `MyWorkouts.razor`. |
| Testes | Unitários (delete 204/404/409; auto-finish com prescrição; alarme para/15s/sem suporte); integração (endpoint + job); UI (card, alarme). |

## 9. Riscos e dependências

- **Detecção do auto-finish no front (D1):** polling do `/workouts` pode ser atrasado; SignalR ainda não implementado. **Suposição:** usaremos polling ao carregar/recarregar o histórico, com sinal local para dispara uma vez.
- **Autoplay de áudio (D2):** navegadores móveis bloqueiam áudio sem interação; usar notificação como gatilho + botão de ativação se bloqueado.
- **Suporte à Vibration API:** nem todos os dispositivos/navegadores suportam; degradar graciosamente.
- **Exclusão física:** irreversível; confirmar na UI (D3).
- **Concorrência:** atleta finalizando enquanto admin exclui; usar estado/versão (D4).
- **Prescrição removida (D5):** se o workout da série foi removido pelo personal, encerrar vazio (comportamento atual) e logar aviso — não falhar o job.
- **Implantação atômica:** novo endpoint e mudança do job devem ir juntos.

## 10. Decisões em aberto (tabela ID/Decisão/Recomendação)

| ID | Decisão | Recomendação / Suposição |
|---|---|---|
| **D1** | Como o front detecta que o back encerrou o treino (auto-finish)? | Polling do histórico ao carregar `/workouts` + sinal local de uma vez; avaliar SignalR depois. **Suposição atual: polling + estado global.** |
| **D2** | Áudio toca automaticamente ou exige interação do usuário? | Notificação do navegador + tentativa de áudio; se bloqueado, exibir botão "Ativar alarme". **Não assume autoplay livre.** |
| **D3** | Exclusão do admin é física (hard delete) ou soft-delete? | Hard delete (mesma semântica de `CancelWorkout` que remove); adicionar confirmação na UI. |
| **D4** | Admin pode excluir treinador de qualquer atleta ou só de sua roster? | Admin: qualquer atleta (`AdminOnly`). Personal: não entra neste requisito (apenas admin, conforme especificado). |
| **D5** | O que fazer quando a prescrição do workout não existe mais? | Encerrar vazio (comportamento atual) e logar aviso; não falhar o job. |
| **D6** | O alarme dispara também quando o atleta encerra manualmente? | Não; só quando o background service encerra (auto-finish). |
| **D7** | Onde montar o componente de alarme no Blazor? | `MainLayout.razor` ou `App.razor`, visível em todas as páginas, ativado por estado global. |
| **D8** | Duração do alarme fixa em 15 s ou configurável? | Fixa em 15 s (requisito); não configurar nesta fatia. |
| **D9** | Como o componente detecta que é auto-finish e não encerramento manual? | Usar flag/sinal local disparado apenas pelo hosted service (não pelo `FinishWorkout` do atleta). **Suposição: o front consulta o histórico e compara `FinishedAt` recente com `StartedAt + Expiry`; se dentro da janela, considera auto-finish.** |

## 11. Critérios de aceite (resumo)

- **Admin / Exclusão:** `DELETE /athletes/{id}/sessions/{id}` retorna `204` para finalizada; `404` para inexistente/não pertencente; `409` para rodando; `403` para não-admin.
- **Auto-finish:** sessão esquecida com `PlannedWorkoutId` finaliza com `PerformedSets` prescritas (não vazia); sem `PlannedWorkoutId` mantém vazio; idempotente.
- **Alarme/vibração:** ao abrir histórico com treino auto-finalizado, componente aparece; clica "Parar" → cessa imediatamente; 15 s sem interação → cessa; navegadores sem suporte não quebram.

## 12. Sugestão de próximo passo

1. **Arquiteto / Revisão de plano (`revisor-plano`):** validar o contrato `DELETE ...`, a mudança no `AutoFinishExpiredWorkouts`, e as decisões D1, D2, D4, D5, D9.
2. **Implementação sugerida (ordem):** D (CSS/estrutura UI, já documentado no doc de referência, baixo risco) → C (endpoint admin + delete use case) → B (auto-finish com prescrição) → A (componente alarme + vibração).
3. **Testador:** cobrir critérios de aceite de exclusão, auto-finish e alarme antes do merge.

---

**Decisões em aberto / perguntas ao usuário:**
- Confirma-se a detecção via polling + sinal local (D1/D9) ou deseja SignalR/integration event?
- Confirma-se `hard delete` (D3) sem soft-delete?
- A vibração deve ser cíclica contínua até "Parar"/15 s, ou pulsos periódicos?
- O componente de alarme deve ser montado globalmente (`MainLayout`) ou apenas na página `/workouts`?
- Há restrição de rosters/niveis de admin além de `AdminOnly`?

**Caminho do arquivo criado:** `docs/features/analise-excluir-treino-alarme-vibracao.md`
