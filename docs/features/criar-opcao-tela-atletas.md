# Opção na tela de atletas para excluir treino pelo endpoint de exclusão

- **Slug:** `criar-opcao-tela-atletas`
- **Status:** proposto (2026-10-26)
- **Data:** 2026-10-26
- **Origem:** requisito de adicionar uma opção de exclusão na tela de histórico do atleta (`/athletes/{userId}/workouts`) para que o treinador (PersonalTrainer ou Admin) possa excluir o treino baseado no endpoint existente `DELETE /athletes/{userId}/workouts/{sessionId}`.

## 1. Contexto e problema

A tela de atletas (`AthleteWorkouts.razor`) exibe o histórico de treinos (`GET /athletes/{userId}/workouts`) e apresenta cards de sessão (`SessionCard.razor`). O endpoint de exclusão `DELETE /athletes/{userId}/workouts/{sessionId}` já existe (`AthleteWorkoutEndpoints.cs`) e exige política `TrainerOrAdmin`, mas a interface atual não expõe nenhuma ação de exclusão para o treinador. O personal precisa de uma opção visível (botão/menu) na tela do atleta para acionar esse endpoint, com confirmação e feedback, para excluir um treino já finalizado.

## 2. Objetivo e valor

- **Treinador (PersonalTrainer/Admin):** consegue excluir um treino finalizado do histórico de um atleta diretamente pela tela `/athletes/{userId}/workouts`, sem precisar de ferramenta externa.
- **Clareza:** a exclusão é explícita, com confirmação, evitando exclusão acidental.
- **Consistência:** reutiliza o endpoint existente (`DELETE /athletes/{userId}/workouts/{sessionId}`) já documentado e testado, sem criar nova API.

## 3. Atores envolvidos

- **Personal Trainer** — visualiza o histórico do atleta (`/athletes/{userId}/workouts`) e aciona a exclusão de treino finalizado.
- **Administrador** — já tem acesso ao endpoint (`TrainerOrAdmin`); a opção na UI também se aplica quando acessa o histórico do atleta.
- **Atleta** — não interage com a exclusão; apenas vê o histórico atualizado após a ação do treinador.
- **Sistema (API)** — expõe `DELETE /athletes/{userId}/workouts/{sessionId}` (`204`, `404`, `409`, `403`).

## 4. Escopo

### 4.1 Incluído

1. Adicionar opção de exclusão (botão/menu) nos cards de treino da tela `/athletes/{userId}/workouts` (`AthleteWorkouts.razor` / `SessionCard.razor`).
2. Confirmar a exclusão via modal ou diálogo (`ConfirmDelete.razor` ou equivalente) antes de chamar a API.
3. Chamar `DELETE /athletes/{userId}/workouts/{sessionId}` via `FitTrackApiClient` e tratar os códigos de status (`204`, `404`, `409`, `403`).
4. Atualizar a lista local (`_workouts`) após a exclusão, com notificação (`ToastService`) de sucesso ou erro.
5. Proteger a ação: só exibir/executar quando o usuário tem permissão (`PersonalTrainer` ou `Admin`) e o treino está finalizado (`FinishedAt != null`). A proteção de `409` já existe no endpoint (sessão rodando).

### 4.2 Não incluído

- Criação de novo endpoint (reutiliza `DELETE /athletes/{userId}/workouts/{sessionId}` existente).
- Exclusão de treino em andamento (o endpoint retorna `409`; a UI deve respeitar, mas não é foco alterar o endpoint).
- Alteração de regras de negócio no domínio ou persistência (a exclusão física já está implementada no use case `DeleteAthleteWorkout`).
- Alteração no histórico do atleta (`/workouts`) — apenas na tela de visualização do treinador (`/athletes/{userId}/workouts`).
- Soft-delete ou auditoria de exclusão (escopo mantido conforme endpoint existente).

## 5. Requisitos funcionais

1. Na tela `/athletes/{userId}/workouts`, cada card de treino finalizado deve exibir uma opção "Excluir treino" (ou ícone/menu equivalente).
2. Ao clicar na opção, a UI deve solicitar confirmação (ex.: "Excluir treino de [data]? Esta ação não pode ser desfeita.").
3. Confirmado, a UI deve chamar `DELETE /athletes/{userId}/workouts/{sessionId}`.
4. Se a resposta for `204`, o card é removido da lista e uma mensagem de sucesso é exibida (`ToastService`).
5. Se a resposta for `404` (não existe ou não pertence ao atleta), exibir mensagem de erro e não alterar a lista.
6. Se a resposta for `409` (sessão ainda rodando), exibir mensagem de erro informando que não é possível excluir treino em andamento.
7. Se a resposta for `403` (não autorizado), exibir mensagem de erro e não alterar a lista.
8. A opção só aparece para sessões finalizadas (`FinishedAt != null`); para sessões em andamento, a exclusão continua disponível apenas via cancelamento (`CancelAthleteWorkout` / `CancelWorkout`), se aplicável.

## 6. Regras de negócio / invariantes

- A exclusão só é permitida para sessões com `FinishedAt != null` (finalizadas). O endpoint já protege `409` para sessões rodando; a UI deve preferir ocultar a opção quando `IsRunning` é verdadeiro, mas se chamada por engano, respeitar o `409`.
- A autorização é `TrainerOrAdmin` no endpoint; a UI deve condicionar a exibição à política do usuário atual (`RequireAuthorization(Policies.TrainerOrAdmin)` no endpoint, e na UI verificar o papel se necessário, embora a página `/athletes/{userId}/workouts` já exija `PersonalTrainer` ou `Admin`).
- A exclusão é física (`hard delete`), conforme o endpoint existente (`DeleteAthleteWorkout`).
- Nenhum dado de prescrição (`WorkoutSeries`) é alterado; apenas a sessão (`WorkoutSession`) é removida.

## 7. Fluxo principal

1. Treinador (PersonalTrainer/Admin) abre `/athletes/{userId}/workouts`.
2. Na lista de sessões, encontra um treino finalizado.
3. Clica no botão/menu "Excluir treino" no card.
4. UI exibe modal/dialog de confirmação.
5. Treinador confirma.
6. `FitTrackApiClient` envia `DELETE /athletes/{userId}/workouts/{sessionId}`.
7. API retorna `204 No Content`.
8. UI remove o card da lista (`_workouts.Remove(...)` ou recarrega via `LoadAsync`) e exibe toast de sucesso.

## 8. Fluxos alternativos e erros

- **Sessão não existe (`404`)**: toast de erro; lista inalterada.
- **Sessão ainda rodando (`409`)**: toast de erro informando que apenas treinos finalizados podem ser excluídos (treinos em andamento devem ser cancelados via cancelamento, se aplicável).
- **Sem permissão (`403`)**: toast de erro; lista inalterada.
- **Erro de rede**: toast de erro genérico; nenhuma alteração na lista.
- **Cancelamento pelo usuário no modal**: ação interrompida; nenhuma chamada à API.

## 9. Contrato HTTP

Reutiliza o endpoint existente:

- `DELETE /athletes/{userId:guid}/workouts/{sessionId:guid}` → `204 No Content`
  - Chamador: `TrainerOrAdmin` (política existente no endpoint).
  - `204 No Content` — excluído com sucesso.
  - `404` — atleta ou sessão não existem, ou a sessão não pertence ao atleta informado.
  - `409 session.delete.not_finished` — a sessão ainda está rodando.
  - `403` — chamador não é admin.

A UI consome esse contrato via `FitTrackApiClient.DeleteAsync` (ou equivalente).

## 10. Modelo de dados

Nenhuma alteração de esquema ou entidade. O endpoint reutiliza:

- `DeleteAthleteWorkoutCommand(AthleteId, SessionId)` (já existente).
- `IDeleteAthleteWorkout` (use case existente).
- `WorkoutSession` — removida fisicamente via `RemoveAsync` no repositório de persistência (`FitTrack.Adapters.Out.Persistence`).

A UI mantém:
- `AthleteWorkouts.razor` — página; `SessionCard.razor` — componente de card; `FitTrackApiClient` — cliente HTTP.

## 11. Arquivos envolvidos

### API (existente, sem alteração)
- `src/FitTrack.Adapters.In.Api/Endpoints/AthleteWorkoutEndpoints.cs` — endpoint `DELETE /athletes/{userId}/workouts/{sessionId}`.
- `src/FitTrack.Application/Commands/DeleteAthleteWorkoutCommand.cs` (ou equivalente) — comando.
- `src/FitTrack.Application/UseCases/DeleteAthleteWorkout.cs` (ou equivalente) — use case.

### UI (alterações propostas)
- `src/FitTrack.UI/Pages/AthleteWorkouts.razor` — adicionar gatilho de exclusão na lista/card.
- `src/FitTrack.UI/Components/SessionCard.razor` (se usado) — adicionar botão/menu de exclusão com condição `FinishedAt != null`.
- `src/FitTrack.UI/Api/FitTrackApiClient.cs` — verificar se há método de `DELETE` para `/athletes/{userId}/workouts/{sessionId}` (se não existir, adicionar).
- Componente de confirmação (novo ou existente): `ConfirmDelete.razor` ou modal reutilizável.
- `ToastService` — já existente; usado para feedback.

### Testes
- `tests/FitTrack.Unit/` — testes de comportamento de exclusão (se houver lógica nova na UI, não aplicável diretamente; o use case já está coberto).
- `tests/FitTrack.Integration/` — cobertura do endpoint `DELETE` (já existente; não requer alteração, mas pode ser referenciada).
- `tests/FitTrack.UITests/` — testes da nova interação na tela de atletas (ver se o botão aparece, se o modal confirma, se a lista atualiza após `204`).

## 12. Impacto arquitetural

| Projeto | Impacto |
| --- | --- |
| `FitTrack.Domain` | Nenhum; entidades existentes (`WorkoutSession`) reutilizadas. |
| `FitTrack.Application` | Nenhum; `DeleteAthleteWorkout` e `DeleteAthleteWorkoutCommand` já existem. |
| `FitTrack.Adapters.In.Api` | Nenhum; `AthleteWorkoutEndpoints` já expõe o endpoint. |
| `FitTrack.Adapters.Out.Persistence` | Nenhum; `RemoveAsync` já existe. |
| `FitTrack.UI` | Adicionar botão/menu de exclusão nos cards da tela `/athletes/{userId}/workouts`; integração com `FitTrackApiClient`; modal de confirmação; atualização da lista e toast. |
| Testes | UI: interação de exclusão na tela do atleta. Integração: já coberto pelo endpoint existente. |

## 13. Plano de testes

### Unitários
- Não se aplica diretamente a esta feature, pois não há nova lógica de domínio. O use case `DeleteAthleteWorkout` já possui testes unitários (404, 409, sucesso).

### Integração
- Reutilizar testes existentes do endpoint `DELETE /athletes/{userId}/workouts/{sessionId}` (`204`, `404`, `409`, `403`). Nenhuma alteração necessária.

### UI (`tests/FitTrack.UITests`)
- Dado um treinador na página `/athletes/{userId}/workouts`, quando há um treino finalizado, então o botão "Excluir treino" aparece no card.
- Dado o botão visível, quando o treinador clica e confirma no modal, então a API retorna `204`, o card é removido da lista e um toast de sucesso é exibido.
- Dado uma sessão rodando, quando o treinador tenta excluir (se a opção aparecer por engano), então a API retorna `409` e um toast de erro é exibido (lista inalterada).
- Dado uma sessão inexistente (simulada), quando o treinador tenta excluir, então a API retorna `404` e um toast de erro é exibido.

## 14. Riscos e dependências

- **Endpoint existente (`TrainerOrAdmin`)**: a UI (`AthleteWorkouts.razor`) exige `PersonalTrainer` ou `Admin`, alinhado ao endpoint (`TrainerOrAdmin`). Nenhum conflito de autorização restante após resolução de D1.
- **Estado do card**: a exclusão depende de `FinishedAt != null`. Se a lista local não refletir esse estado corretamente (ex.: cache desatualizado), a UI pode permitir a exclusão de uma sessão rodando (o endpoint protege com `409`, mas a experiência não é ideal).
- **Recarregamento da lista**: após exclusão, a lista pode ser recarregada (`LoadAsync`) ou atualizado localmente. A recarga é mais segura para consistência.
- **Confirmação**: sem modal, o risco de exclusão acidental é alto. A feature inclui modal obrigatório.

## 15. Decisões em aberto

| ID | Decisão | Recomendação |
| --- | --- |
| **D1** | O endpoint `DELETE /athletes/{userId}/workouts/{sessionId}` é `TrainerOrAdmin`. A tela `/athletes/{userId}/workouts` é acessível por `PersonalTrainer` e `Admin`. A opção de exclusão deve aparecer apenas para `Admin`, ou o endpoint deve ser alterado para permitir `PersonalTrainer` também? | Confirmar com o usuário se o personal pode excluir. Se sim, alterar o endpoint (`RequirePersonalTrainerOrAdmin`) ou criar uma política alternativa. Se não, a opção só aparece para `Admin`. |
| **D2** | A exclusão é física (`hard delete`). Deve haver uma mensagem explícita no modal confirmando que a ação é irreversível? | Sim; incluir texto claro no modal de confirmação (ex.: "Esta ação não pode ser desfeita."). |
| **D3** | O card a ser removido é atualizado localmente (`_workouts.Remove`) ou via recarga completa (`LoadAsync`)? | Recarregar (`LoadAsync`) é mais simples e evita inconsistências; atualizar localmente é mais rápido. Recomendação: recarregar para garantir consistência com o servidor. |
| **D4** | A opção de exclusão deve ser visível em todos os cards de treino finalizado ou apenas quando o usuário tem papel de `Admin` (se D1 mantiver `TrainerOrAdmin`)? | Condicionar a visibilidade ao papel do usuário atual, conforme a política do endpoint. |

## 16. Sugestão de próximo passo

1. Confirmar a decisão **D1** (permissão de exclusão: apenas `Admin` ou `PersonalTrainer` também?). Se necessário, ajustar a política do endpoint (`AthleteWorkoutEndpoints.cs`).
2. Revisar o plano (`revisor-plano`) para validar escopo e riscos (especialmente D1).
3. Implementar a alteração na UI (`AthleteWorkouts.razor` / `SessionCard.razor`): adicionar botão/menu, modal de confirmação, chamada ao `FitTrackApiClient`, atualização da lista e toast.
4. Testar a interação na UI (`tests/FitTrack.UITests`) cobrindo sucesso (`204`), erro (`409`, `404`, `403`) e ausência do botão para sessões rodando.

---

**Caminho do arquivo:** `docs/features/criar-opcao-tela-atletas.md`

**Resumo:** Documenta a adição de uma opção de exclusão na tela de atletas (`/athletes/{userId}/workouts`) que reutiliza o endpoint existente `DELETE /athletes/{userId}/workouts/{sessionId}`. Inclui requisitos, escopo, contrato HTTP, arquivos envolvidos (UI), impacto arquitetural (nenhum no domínio/API, apenas UI), testes e decisões em aberto (principalmente D1 sobre a política de autorização do endpoint vs. acesso do personal).
