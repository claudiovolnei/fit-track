# Criar plano — pacote de ajustes do fluxo de treino

- **Slug:** `criar-plano`
- **Status:** proposto (2026-10-22)
- **Data:** 2026-10-22
- **Fatia:** quatro sub-requisitos entregues juntos por tocarem o mesmo fluxo
  (sessão em andamento, tela Hoje, Home do atleta): cancelamento pelo personal,
  auto-finalização, faixa semanal na Home e ajuste de carga/repetições com unidade
  alternativa.

## 1. Contexto e problema

O fluxo atual fecha o treino de três formas: o atleta finaliza (com nível e
observações), o atleta cancela (só se o treino estiver rodando e sem séries) ou o
treino fica rodando para sempre. Quatro lacunas aparecem no uso:

1. **O personal não tem mão no treino em andamento.** Se o atleta iniciou o
   treino errado, ou o personal quer liberar o dia para outro treino, não há
   caminho: o atleta precisa cancelar sozinho, e só se não registrou nada.
2. **Um treino esquecido bloqueia o dia indefinidamente.** A regra "um treino por
   dia" recusa qualquer início enquanto houver sessão rodando; um atleta que
   esqueceu de finalizar perde o dia inteiro (e os dias seguintes, até alguém
   intervir).
3. **A Home do atleta não mostra a semana.** Os cards resumem totais, mas não há
   visão rápida de "quais dias da semana eu treinei", que é o feedback de
   aderência mais imediato.
4. **O ajuste de carga na finalização não permite unidade alternativa.** O atleta
   pode mudar as repetições e a carga em kg, mas muitos pensam em "barras" (a
   barra olímpica e anilhas) e não em quilos; e o padrão é o valor prescrito, mas
   a tela não deixa claro que pode ser aumentado.

## 2. Objetivo e valor

- **Personal:** liberar o dia de um atleta cancelando um treino em andamento, sem
  depender do atleta.
- **Atleta:** nunca ficar bloqueado por um treino esquecido — após 2,5 h ele se
  fecha sozinho.
- **Atleta:** ver de relance, na Home, os 7 dias da semana com círculo cheio nos
  dias em que concluiu um treino.
- **Atleta:** finalizar com o valor prescrito como ponto de partida, podendo
  subir as repetições ou a carga, e alternar a carga entre kg e barras.
- **Time:** manter a regra de um treino por dia e a integridade do histórico
  (sessão cancelada some; sessão auto-finalizada vira histórico).

## 3. Atores envolvidos

- **Atleta** — segue a série, inicia e finaliza o treino, ajusta o esforço.
- **Personal trainer** — cancela treino em andamento de atleta da própria roster.
- **Administrador** — pode cancelar em qualquer roster (mesma lógica de
  `RequirePersonalTrainerOrAdmin`).
- **Sistema (API)** — auto-finaliza sessões esquecidas por job em segundo plano.

## 4. Escopo

### 4.1 Incluído

1. Cancelamento de treino em andamento por personal/admin, liberando o dia.
2. Auto-finalização de sessão rodando após 2,5 h de abertura.
3. Faixa semanal (7 dias) na Home do atleta, com círculo vazio/cheio.
4. Toggle de unidade de carga (kg ↔ barra) na tela Hoje, com o prescrito como
   padrão, e possibilidade de aumentar repetições/carga.

### 4.2 Não incluído

- Cancelamento de treino **já finalizado** (o histórico é fato consumado; cancelar
  só remove sessão rodando).
- Auto-cancelamento (a sessão auto-finalizada vira histórico, não some).
- Notificação push/e-mail ao atleta quando o personal cancela ou quando o treino
  auto-finaliza.
- Relatório de aderência semanal além dos 7 círculos.
- Alteração do contrato HTTP de carga: a API continua gravando quilos; a unidade
  "barra" é convertida no cliente.
- Novo endpoint para a faixa semanal (a Home reusa a listagem de sessões).

## 5. Sub-requisito A — Cancelamento pelo personal

### 5.1 Requisitos funcionais

1. O personal (ou admin) pode cancelar uma sessão **rodando** de um atleta da
   própria roster.
2. O cancelamento remove a sessão, de modo que o atleta possa iniciar outro treino
   no mesmo dia (a regra "um treino por dia" deixa de ver a sessão removida).
3. O cancelamento é recusado se a sessão não estiver rodando (já finalizada).
4. O cancelamento é recusado se o atleta não for da roster do personal (resposta
   como se a sessão não existisse — 404, mesmo padrão de
   `WorkoutSessionAccess`).
5. O cancelamento do personal não apaga sessões finalizadas nem toca no
   histórico.
6. O atleta continua podendo cancelar o próprio treino vazio, como hoje.

### 5.2 Regras de negócio

- A remoção segue a semântica de `CancelWorkout`: só sessão rodando. A decisão de
  permitir cancelar mesmo com séries registradas fica em aberto (D1).
- A autorização é do use case (`RequirePersonalTrainerOrAdmin` + escopo de
  roster), não só do endpoint.
- A liberação do dia é efeito natural da remoção: `StartWorkout` consulta
  `ListByDateAsync` e não encontra mais a sessão.

### 5.3 Contrato HTTP (proposto)

Novo endpoint, para não sobrecarregar a semântica de "sessão própria":

- `DELETE /athletes/{athleteId}/sessions/{sessionId}` → `204 No Content`
  - `PersonalTrainer` (própria roster) ou `Admin`.
  - `404` se o atleta ou a sessão não existirem, ou o atleta não for da roster.
  - `409 session.cancel.not_running` se a sessão já estiver finalizada.
  - `409 session.cancel.has_sets` se a sessão tiver séries e a regra D1 for
    "recusar" (ver decisões em aberto).

Alternativa (D2): reusar `DELETE /sessions/{id}` e deixar o use case decidir pelo
papel do chamador. A recomendação é o endpoint dedicado, por clareza de escopo e
auditoria.

### 5.4 Fluxo principal

1. Personal abre a tela do atleta (ou o histórico) e vê "Treino em andamento".
2. Clica "Cancelar treino do atleta".
3. API remove a sessão rodando.
4. O atleta, ao abrir `/today`, vê o treino como "A fazer" e pode iniciá-lo de
   novo no mesmo dia.

### 5.5 Critérios de aceite

1. Dado um personal vinculado ao atleta, quando cancela uma sessão rodando
   daquele atleta, então a API responde `204` e a sessão deixa de existir.
2. Dado o cancelamento acima, quando o atleta inicia outro treino no mesmo dia,
   então a API aceita (não retorna `session.start.already_started_today`).
3. Dado um personal **não** vinculado ao atleta, quando tenta cancelar, então a
   API responde `404`.
4. Dado uma sessão já finalizada, quando o personal tenta cancelar, então a API
   responde `409 session.cancel.not_running`.
5. O atleta não consegue cancelar a sessão de outro atleta (permanece `404`).

## 6. Sub-requisito B — Auto-finalização após 2,5 h

### 6.1 Requisitos funcionais

1. Uma sessão rodando há **2,5 h** (150 minutos) ou mais é finalizada
   automaticamente pelo servidor.
2. A auto-finalização usa o relógio do servidor (`IClock`), não o do cliente.
3. Após a auto-finalização, a sessão aparece no histórico como finalizada, com a
   duração registrada.
4. O atleta que tentar finalizar depois recebe `409 session.finish.already_finished`.
5. A auto-finalização não exige ação do atleta e não depende do navegador estar
   aberto.

### 6.2 Regras de negócio

- O limiar (2,5 h) é uma constante da aplicação (recomendação: em
  `FinishWorkout` ou em um serviço dedicado, valor `2.5` horas).
- A sessão auto-finalizada é **finalizada**, não cancelada: ela permanece no
  histórico e conta para a regra "um treino por dia" daquele dia.
- A auto-finalização não registra séries que existiam só na tela do atleta (o
  servidor não as conhece). O resultado pode ser uma sessão finalizada sem séries
  (D6).
- Concorrência: se o atleta finalizar no mesmo instante, uma das duas operações
  perde com `session.finish.already_finished`; a outra vence.

### 6.3 Mecanismo (proposto)

Serviço em segundo plano no adapter de entrada (hosted service), executando a
cada 5 minutos (D3):

1. Lista sessões rodando com `StartedAt <= UtcNow - 2.5h`.
2. Para cada uma, chama o fechamento com `FinishedAt = StartedAt + 2.5h`
   (duração limitada a 2,5 h) **ou** `FinishedAt = UtcNow` (duração real) —
   decisão D4.
3. Grava com a intensidade definida em D5 (nula ou padrão).

Alternativa (D3): verificação preguiçosa em `StartWorkout` e nas leituras. A
recomendação é o job em segundo plano, porque a tela do atleta pode estar fechada.

### 6.4 Contrato HTTP

Nenhum endpoint novo. A sessão auto-finalizada é lida pelos endpoints existentes
(`GET /sessions`, `GET /athletes/{userId}/workouts`) com `finishedAt` preenchido
e `intensity` conforme D5.

### 6.5 Fluxo principal

1. Atleta inicia treino às 10:00 e fecha o navegador sem finalizar.
2. Às 12:30 (2,5 h depois) o job encontra a sessão rodando.
3. O job finaliza a sessão com `FinishedAt` conforme D4.
4. Às 13:00 o atleta abre o app: o treino aparece como "Concluído" e o dia
   segue contando como treinado.

### 6.6 Critérios de aceite

1. Dada uma sessão rodando iniciada há 2,5 h ou mais, quando o job executa,
   então a sessão passa a ter `finishedAt` preenchido.
2. Dada uma sessão auto-finalizada, quando o atleta tenta finalizá-la pela tela,
   então a API responde `409 session.finish.already_finished`.
3. Dada uma sessão rodando há menos de 2,5 h, quando o job executa, então ela
   permanece rodando.
4. O job não finaliza sessões já finalizadas (idempotente).

## 7. Sub-requisito C — Faixa semanal na Home

### 7.1 Requisitos funcionais

1. Na Home do atleta (ramo `else` de `Home.razor`), exibir uma faixa com
   **7 dias** da semana, cada um representado pela **primeira letra** do dia da
   semana.
2. Cada dia é um círculo: **vazio** se o atleta não concluiu treino naquele dia,
   **cheio** se concluiu ao menos um.
3. Apenas sessões **finalizadas** preenchem o círculo; sessão rodando não conta.
4. A faixa reflete a semana atual (segunda a domingo) — ou os últimos 7 dias
   (D7).
5. Ao finalizar um treino, a Home recarregada mostra o dia de hoje cheio.
6. A faixa não aparece para personal/admin (ramo trainer da Home).

### 7.2 Regras de negócio

- O dado vem da listagem de sessões que a Home já carrega (`GetSessionsAsync`);
  não é necessário endpoint novo.
- "Concluiu treino" = sessão com `FinishedAt != null`, agrupada por
  `WorkoutDate`.
- A primeira letra usa o locale pt-BR (D, S, T, Q, Q, S, S — duplicatas
  aceitas, D8).
- O dia de "hoje" usa `WorkoutDate` (data do servidor), consistente com a sessão;
  a exibição da semana segue a mesma data.

### 7.3 Fluxo de telas

- Componente novo ou bloco inline em `Home.razor`, acima dos cards de estatística
  do atleta.
- 7 círculos em linha, cada um com a letra do dia e `aria-label` com a data
  completa (ex.: "Segunda-feira, 20 de outubro — treinou").
- Estilo: círculo vazio com borda; cheio com cor de sucesso.

### 7.4 Critérios de aceite

1. Dado um atleta com sessão finalizada na segunda-feira, quando abre a Home,
   então o círculo da segunda está cheio e os demais vazios (salvo outros dias).
2. Dado um atleta sem sessões na semana, quando abre a Home, então os 7 círculos
   estão vazios.
3. Dado um atleta com sessão rodando hoje (não finalizada), quando abre a Home,
   então o círculo de hoje está vazio.
4. O personal não vê a faixa semanal.
5. Após finalizar um treino e voltar à Home, o círculo do dia está cheio.

## 8. Sub-requisito D — Ajuste de carga/repetições na finalização

### 8.1 Requisitos funcionais

1. Na tela Hoje, cada exercício prescrito exibe as repetições e a carga
   **prescritas** como valor inicial dos campos (já é o comportamento atual).
2. O atleta pode alterar as repetições e a carga antes de finalizar; o valor
   enviado é o que ele digitou (ou o prescrito, se não mudou).
3. O atleta pode **aumentar** as repetições e a carga em relação ao prescrito
   (D10 define se pode diminuir).
4. A carga pode ser expressa em **kg** ou em **barra(s)**, por um toggle por
   exercício; ao alternar, o valor é convertido (D9) e o campo/step se adaptam.
5. O padrão é kg; o prescrito (em kg) é convertido para barras quando o atleta
   escolhe "barra".
6. Os valores são enviados à API em quilos (contrato inalterado); a conversão
   ocorre no cliente.
7. As séries registradas/exibidas no histórico continuam em kg.

### 8.2 Regras de negócio

- A prescrição continua sendo do personal e não muda; o atleta só ajusta o que
  fez naquele dia.
- Faixa de repetições: 1–100 (já validada pela API, `set.repetitions.out_of_range`).
- Carga em kg: `>= 0` (0 = peso corporal), passo 0,5.
- Carga em barra: número de barras (D11 define inteiro ou decimal), convertido
  para kg por `barras × PesoDaBarra` antes do envio.
- O toggle não cria estado no servidor; é estado de tela.

### 8.3 Fluxo de telas

1. Atleta toca no exercício para ajustar (como hoje, `StartEditing`).
2. Os campos reps e carga aparecem com os valores prescritos.
3. Um toggle "kg / barra" ao lado do campo de carga alterna a unidade.
4. Ao escolher "barra", o campo passa a mostrar `carga_prescrita / PesoDaBarra`
   (arredondado) e o step vira 1 (ou 0,5, D11).
5. Ao finalizar, o payload converte de volta para kg.
6. Se o atleta não tocou no toggle, envia em kg (comportamento atual).

### 8.4 Critérios de aceite

1. Dado um exercício prescrito com 3 × 12 @ 40 kg, quando o atleta abre o ajuste,
   então reps = 12 e carga = 40 kg.
2. Dado o atleta mudando só a carga para 45 kg e finalizando, então a série
   gravada tem 12 reps e 45 kg.
3. Dado o atleta mudando as reps para 10, então a série gravada tem 10 reps.
4. Dado o toggle em "barra" com peso da barra 20 kg, então 40 kg aparece como
   2 barras e o envio resulta em 40 kg.
5. Dado o atleta escolhendo "barra" e digitando 2,5 barras, então o envio resulta
   em 50 kg (se D11 permitir decimal) — caso contrário, o campo recusa decimal.
6. O histórico exibe a carga em kg, independentemente da unidade usada no ajuste.

## 9. Modelo de dados

- **Sub A:** nenhuma tabela nova; a sessão é removida (já existe `RemoveAsync`).
- **Sub B:** nenhuma tabela nova; `FinishedAt` e `Intensity` já existem. Pode ser
  necessário índice em `StartedAt`/`FinishedAt` para o job (D12).
- **Sub C:** nenhuma mudança.
- **Sub D:** nenhuma mudança (a API continua em kg).

Value objects/DTOs tocados:

- `WorkoutSession` — já suporta `Finish` com intensidade nula; o job pode usá-lo.
- `CancelWorkout` / novo `CancelAthleteWorkout` — autorização e escopo.
- `StartWorkout` — pode precisar de guarda contra sessões expiradas (D13).
- `Home.razor` — faixa semanal.
- `Today.razor` — toggle de unidade e conversão.

## 10. Impacto arquitetural

| Projeto | Impacto |
| --- | --- |
| `FitTrack.Domain` | Constante do limiar 2,5 h (recomendado no use case/serviço, não no agregado). Nenhuma dependência nova. |
| `FitTrack.Application` | `CancelWorkout` estendido ou novo `CancelAthleteWorkout`; novo serviço de auto-finalização; `IClock` já existe. |
| `FitTrack.Adapters.In.Api` | Endpoint `DELETE /athletes/{id}/sessions/{sessionId}`; registro do hosted service; políticas existentes. |
| `FitTrack.Adapters.Out.Persistence` | Consulta de sessões rodando antigas para o job; possivelmente índice novo. |
| `FitTrack.UI` | `Home.razor` (faixa semanal); `Today.razor` (toggle de unidade); gatilho de cancelamento pelo personal (local a definir, D2). |
| Testes | Casos novos nos testes unitários/integração/UI para os quatro sub-requisitos. |

## 11. Plano de testes

### Unitários (`tests/FitTrack.Unit`)

- `CancelWorkout`/`CancelAthleteWorkout`: personal da roster cancela; fora da
  roster é 404; sessão finalizada é 409; admin cancela qualquer.
- Serviço de auto-finalização: sessão com 2,5 h+ é finalizada; abaixo não; já
  finalizada não é tocada; concorrência tratada.
- `Home`/faixa: 7 dias, letras corretas, cheio só com sessão finalizada.
- `Today`: valores padrão prescritos; toggle converte kg↔barra; envio em kg.

### Integração (`tests/FitTrack.Integration`)

- `SessionFlowTests`: cancelamento pelo personal libera o dia (novo treino aceito).
- Auto-finalização via job ou endpoint de teste com relógio controlado.
- `GET /athletes/{id}/workouts` e `GET /sessions` refletem sessão auto-finalizada.

### UI (`tests/FitTrack.UITests`)

- Home do atleta renderiza 7 círculos; cheio conforme sessões.
- Today renderiza toggle e converte a carga no payload.

## 12. Riscos e dependências

- **Cancelamento pelo personal apaga a sessão:** se ela tiver séries, há perda de
  histórico (mitigar com a regra D1).
- **Auto-finalização fecha sessão vazia:** o atleta pode perder o treino do dia se
  não tiver enviado as marcações (o fluxo atual envia tudo no finalizar). Mitigar
  documentando e, se desejado, auto-cancelar em vez de finalizar (D6).
- **Job em segundo plano:** em ambiente com múltiplas instâncias, evitar
  processamento duplicado (idempotência + filtro por estado).
- **Fuso horário:** a semana da Home e o limiar de 2,5 h usam data/hora do
  servidor; o cliente não é fonte da verdade.
- **Peso da barra:** assumir 20 kg pode não servir a todos (halteres, barras
  curtas). Tornar configurável (D9).
- **Índice no banco:** a consulta do job pode exigir índice em
  `workout_sessions(started_at, finished_at)`.

## 13. Decisões em aberto

| ID | Decisão | Recomendação |
| --- | --- | --- |
| **D1** | Personal pode cancelar sessão **com séries** registradas? | Recusar (manter a regra atual): preserva o histórico; se precisar liberar o dia com séries, o personal auto-finaliza ou o atleta finaliza. |
| **D2** | Onde o personal aciona o cancelamento? | No histórico do atleta (`/athletes/{id}/workouts`) ou em um painel "treino em andamento"; endpoint dedicado `DELETE /athletes/{id}/sessions/{sessionId}`. |
| **D3** | Mecanismo da auto-finalização: job em segundo plano ou verificação preguiçosa? | Job em segundo plano a cada 5 min, mais guarda em `StartWorkout` para bloquear início após 2,5 h. |
| **D4** | `FinishedAt` da auto-finalização: `StartedAt + 2,5 h` (duração limitada) ou `UtcNow` (duração real)? | `StartedAt + 2,5 h`: a duração reflete o limite, não o atraso do job. |
| **D5** | Intensidade da sessão auto-finalizada: nula ou padrão (ex.: "moderado")? | Nula: a intensidade é percepção do atleta; o sistema não deve inventá-la. Requer ajuste em `FinishWorkout` (hoje exige intensidade). |
| **D6** | Auto-finalização deve registrar as séries já salvas (nenhuma, no fluxo atual) ou cancelar? | Finalizar com o que existe (vazio) e documentar; considerar auto-cancelar se a sessão não tiver séries. |
| **D7** | A semana da Home é a semana civil (seg–dom) ou os últimos 7 dias? | Semana civil (seg–dom), alinhada à noção de "esta semana" já usada no card. |
| **D8** | Letras duplicadas (S, Q, S) são aceitáveis? | Sim, conforme pedido; adicionar `title`/`aria-label` com o nome completo do dia. |
| **D9** | Peso da barra fixo em 20 kg ou configurável? | Configurável por personal/atleta (padrão 20 kg), para suportar halteres e barras curtas. |
| **D10** | O atleta pode apenas aumentar reps/carga ou também diminuir? | Permitir qualquer valor válido (0+ kg, 1–100 reps): a realidade pode ser menos que o prescrito. |
| **D11** | Carga em "barra" aceita decimal (2,5 barras) ou apenas inteiro? | Decimal, para anilhas parciais; converter com a mesma precisão do kg. |
| **D12** | Criar índice em `workout_sessions(started_at, finished_at)` para o job? | Sim, se o volume de sessões crescer; avaliar no plano de migração. |
| **D13** | `StartWorkout` deve auto-finalizar sessões expiradas antes de recusar o início? | Sim, como guarda: se a sessão rodando tem 2,5 h+, finaliza-a e permite o novo início. |
| **D14** | Admin cancela sessão de qualquer atleta ou só da roster? | Qualquer (admin vê e age em toda roster), consistente com `RequirePersonalTrainerOrAdmin`. |

## 14. Sugestão de próximo passo

1. Arquiteto: validar os contratos propostos (endpoint de cancelamento pelo
   personal, job de auto-finalização, conversão de unidade no cliente) e as
   decisões D1, D4, D5, D9.
2. Implementação: começar pelo sub-requisito A (menor impacto, destrava o
   personal), depois B (job), C (UI) e D (UI).
3. Testador: cobrir os critérios de aceite de cada sub-requisito antes do merge.
