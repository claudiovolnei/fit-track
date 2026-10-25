# Criar novo plano — drop set/pirâmide, alarme de descanso e logout por expiração de token

- **Slug:** `criar-novo-plano`
- **Status:** proposto (2026-10-24)
- **Data:** 2026-10-24
- **Fatia:** três sub-requisitos independentes, agrupados por tocarem a experiência do atleta:
  (A) formas rápidas para preencher séries do tipo drop set e pirâmide na tela Hoje;
  (B) alarme (som/vibração) ao final do cronômetro de descanso; e
  (C) logout automático quando o token de acesso expirar.

## 1. Contexto e problema

### 1.1 Sub-requisito A — séries drop set e pirâmide

A tela Hoje já permite variar carga e repetições por série: ao expandir um exercício, o atleta digita
`reps` e `kg` para cada uma das `N` séries prescritas, e o envio final grava uma `PerformedSet` por
série. O que falta é uma forma rápida de montar padrões comuns de treino de força:

- **Drop set:** várias séries consecutivas com carga decrescente (e, em geral, repetições crescentes
  ou iguais), sem pausa longa entre elas.
- **Pirâmide:** carga crescente (e repetições decrescentes) ou decrescente (e repetições crescentes)
  ao longo das séries.

Hoje o atleta precisa digitar cada valor à mão, o que é lento e propenso a erro, principalmente no
celular. O personal prescreve "4 séries, pirâmide 12-10-8-6", mas a UI não tem um atalho para esse
padrão.

### 1.2 Sub-requisito B — alarme no final do descanso

O cronômetro de descanso (`RestCountdown` + `RestTimerModal`) já conta regressivamente e exibe o
alerta visual "Descanso concluído" ao chegar a zero. O plano anterior descartou som/vibração (D7)
por simplicidade. Na prática, o atleta pode estar olhando para outro lado ou com o aparelho apoiado;
um aviso apenas visual é fácil de perder, e o descanso acaba virando "quanto tempo se passou mesmo?".

### 1.3 Sub-requisito C — expiração de token/acesso

O cliente Blazor guarda o JWT no `localStorage` e o `TokenAuthenticationStateProvider` decodifica o
payload para montar a identidade. Nenhum ponto do cliente verifica a claim `exp` nem reage a um
`401` da API de forma centralizada:

- Um token expirado continua "logado" na UI até a próxima chamada que falhe; a falha aparece como
  erro genérico em vez de redirecionar para `/login`.
- O `AuthTokenHandler` anexa o token expirado a todas as requisições, desperdiçando chamadas e
  poluindo a experiência com erros.
- Não há refresh token: quando o acesso expira, a única ação correta é deslogar e pedir novo login.

## 2. Objetivo e valor

- **Atleta:** montar drop set e pirâmide com um toque, ajustando só o ponto de partida; menos digitação,
  menos erro.
- **Atleta:** ser avisado por som e/ou vibração quando o descanso acabar, mesmo sem olhar para a tela.
- **Atleta/sistema:** sair da sessão automaticamente quando o token expirar, em vez de acumular erros 401.
- **Time:** manter o contrato HTTP e o modelo de dados inalterados no sub-requisito A (templates são
  estado de tela); adicionar apenas um helper de áudio no sub-requisito B; tratar expiração no cliente
  no sub-requisito C, sem mudar a API.

## 3. Atores envolvidos

- **Atleta** — usa os atalhos de série, o cronômetro de descanso e a sessão autenticada.
- **Personal trainer / administrador** — não usam os atalhos de série do atleta; podem ser afetados
  pelo logout por expiração como qualquer usuário autenticado.
- **Sistema (UI)** — executa os templates, toca o alarme e limpa a sessão expirada.

## 4. Escopo

### 4.1 Incluído

A. Atalhos (templates) na tela Hoje para preencher as linhas de carga/repetições de um exercício
   prescrito segundo os padrões drop set e pirâmide, com base nos valores já digitados ou prescritos.
B. Alarme sonoro e/ou vibração ao zerar o `RestCountdown`, com opção de ativar/desativar e respeito
   às políticas de autoplay do navegador.
C. Verificação da expiração do JWT no cliente e logout automático, além de reação centralizada a 401.

### 4.2 Não incluído

- Novo campo de "tipo de série" no domínio, na prescrição ou na persistência (A é template de tela).
- Persistência do estado do template escolhido (A é efêmero, como o ajuste de carga).
- Som customizável, volume, notificações push ou alarme em segundo plano (B cobre apenas o modal aberto).
- Refresh token ou renovação silenciosa de sessão (C desloga e pede novo login).
- Alteração de endpoints ou contratos HTTP.

## 5. Sub-requisito A — formas para drop set e pirâmide

### 5.1 Requisitos funcionais

1. Na tela Hoje, quando um exercício estiver em edição (`StartEditing`), além dos campos por série,
   exibir botões de atalho: "Drop set", "Pirâmide crescente", "Pirâmide decrescente" e "Limpar/normal".
2. O atalho preenche `_setReps` e `_setLoads` daquele exercício com um padrão calculado a partir do
   valor atual da primeira série (ou do prescrito, se ainda não editado).
3. O atleta pode ajustar manualmente qualquer série depois de aplicar o atalho; o atalho não bloqueia
   edição.
4. O padrão não altera o número de séries prescritas (`row.Sets`); apenas distribui reps/carga.
5. "Limpar/normal" volta todas as séries ao valor prescrito (reps e carga iguais).

### 5.2 Regras de negócio / algoritmos propostos

- **Drop set:** manter repetições iguais à primeira série; carga decresce em passos configuráveis
  (decisão D1: percentual, ex. -10%, ou absoluto, ex. -2,5 kg ou -1 barra conforme a unidade ativa).
- **Pirâmide crescente:** carga cresce por série (ex. +2,5 kg ou +1 barra) e repetições decrescem
  (ex. -2 reps por série, mínimo 1).
- **Pirâmide decrescente:** carga decresce e repetições crescem.
- Quando a unidade ativa for "barra", os passos são convertidos para kg antes de gravar em `_setLoads`
  (contrato em kg, como hoje).
- Os valores resultantes respeitam as faixas da API (reps 1–100, carga >= 0); valores fora são
  limitados (clamp) e o atleta é avisado por toast se houver ajuste.

### 5.3 Fluxo de telas

1. Atleta toca "Ajustar" no exercício.
2. Os campos por série aparecem com o prescrito.
3. Atleta toca "Pirâmide crescente".
4. Os campos são preenchidos: série 1 com carga base, série 2 com carga base + passo e reps -2, etc.
5. Atleta pode editar individualmente.
6. Ao finalizar, o payload já existente envia os valores por série em kg.

### 5.4 Critérios de aceite

1. Dado um exercício prescrito com 4 séries de 10 reps @ 40 kg, ao aplicar "Pirâmide crescente"
   com passo padrão, então as cargas/reps das 4 séries seguem o padrão definido e o envio resulta
   nessas cargas em kg.
2. Dado um atalho aplicado, ao editar manualmente uma série, o valor editado é preservado.
3. Dado "Limpar/normal", todas as séries voltam ao prescrito.
4. O atalho não cria estado no servidor e não altera a prescrição.

### 5.5 Impacto

- `FitTrack.UI`: `Today.razor` (botões e método de preenchimento), possivelmente um helper puro
  `SetSeriesTemplates.cs` testável.
- Sem mudança em Domain/Application/API/Persistence.

## 6. Sub-requisito B — alarme no final do descanso

### 6.1 Requisitos funcionais

1. Quando `RestCountdown.Finished` tornar-se verdadeiro, o modal dispara um alarme: som (beep curto)
   e, se disponível, vibração do dispositivo.
2. O alarme toca uma única vez por ciclo de contagem; reiniciar o descanso rearma o alarme.
3. O atleta pode ativar/desativar o alarme (preferência persistida no `localStorage`); padrão ativo.
4. Se o navegador bloquear autoplay, o alarme não quebra o fluxo: o alerta visual continua e um
   aviso discreto informa que o som foi bloqueado.
5. O alarme não interfere no relógio do treino nem na finalização.

### 6.2 Implementação proposta

- Adicionar um helper JS em `wwwroot` (ex.: `rest-alarm.js`) que usa a Web Audio API para gerar um
  beep e `navigator.vibrate` quando suportado.
- `RestTimerModal.razor` chama `IJSRuntime.InvokeVoidAsync("fitTrack.playRestAlarm")` ao detectar
  a transição para `Finished`, guardado por flag `_alarmPlayed`.
- Preferência de ativação lida/gravada via `localStorage` (reaproveitar `IJSRuntime`), com estado
  inicial carregado em `OnInitializedAsync`.
- O helper é tolerante a falhas (try/catch no JS e no C#); falha de JS não derruba o modal.

### 6.3 Critérios de aceite

1. Dado o descanso chegando a zero com alarme ativo, então um beep é emitido e o dispositivo vibra
   (quando suportado).
2. Dado o alarme desativado, então nenhum som/vibração é emitido e o alerta visual continua.
3. Dado bloqueio de autoplay, então o fluxo não lança exceção e o alerta visual permanece.
4. Reiniciar o descanso permite novo alarme ao zerar novamente.

### 6.4 Impacto

- `FitTrack.UI`: `RestTimerModal.razor`, novo `wwwroot/rest-alarm.js`, possivelmente um serviço
  `IRestAlarm` para testabilidade.
- `FitTrack.UITests`: bUnit com fake JS (sem som real).

## 7. Sub-requisito C — logout por expiração de token

### 7.1 Requisitos funcionais

1. Ao carregar a aplicação ou ao mudar o estado de autenticação, se o token armazenado estiver
   expirado (`exp` no passado), o cliente limpa o token e notifica estado anônimo.
2. O `AuthTokenHandler`, ao receber `401 Unauthorized` da API, limpa o token, notifica o
   `TokenAuthenticationStateProvider` e redireciona para `/login` (preservando a URL de retorno quando
   possível).
3. Chamadas concorrentes que recebam 401 não disparam múltiplos redirects; usar um gate/semáforo.
4. O redirecionamento não ocorre em páginas públicas (`/login`, `/register`).
5. O logout por expiração não apaga dados locais não relacionados (ex.: preferência do alarme).

### 7.2 Implementação proposta

- `JwtReader` (ou um helper `TokenExpiry`) lê a claim `exp` e expõe `IsExpired(token, now)`.
- `TokenAuthenticationStateProvider.GetAuthenticationStateAsync` retorna anônimo se o token estiver
   expirado e dispara limpeza assíncrona do store (sem deadlock).
- `AuthTokenHandler.SendAsync`: após `base.SendAsync`, se `response.StatusCode == Unauthorized`,
   invoca um `ISessionTerminator` (novo serviço) que limpa o store, notifica auth state e navega
   para `/login` via `NavigationManager`.
- O `NavigationManager` é injetado no handler/terminator; cuidado com SSR/prerender (só atuar no
   browser).
- Opcional: timer leve no `MainLayout` ou `TokenAuthenticationStateProvider` que reavalia a expiração
   a cada minuto e desloga proativamente (decisão D2).

### 7.3 Contrato HTTP

Nenhum endpoint novo. A API continua retornando 401 com o bearer inválido/expirado; o cliente passa
a reagir de forma centralizada.

### 7.4 Critérios de aceite

1. Dado um token expirado no `localStorage`, ao abrir a aplicação, então o usuário aparece como
   não autenticado e é redirecionado para `/login` ao acessar rota protegida.
2. Dado uma chamada à API retornando 401, então o token é removido e o usuário é redirecionado para
   `/login`.
3. Dado múltiplas chamadas simultâneas retornando 401, então ocorre um único redirect.
4. Dado que o usuário está em `/login`, então um 401 não causa loop de redirecionamento.

### 7.5 Impacto

- `FitTrack.UI`: `JwtReader`/novo helper, `TokenAuthenticationStateProvider`, `AuthTokenHandler`,
  novo `SessionTerminator`, registro em `UiServiceCollectionExtensions`.
- `FitTrack.UITests`: testes com token expirado e fake handler.

## 8. Modelo de dados

- Sub A: nenhuma mudança (templates de tela).
- Sub B: nenhuma mudança (preferência no `localStorage`).
- Sub C: nenhuma mudança (estado no `localStorage` + memória).

## 9. Impacto arquitetural

| Projeto | Impacto |
| --- | --- |
| `FitTrack.Domain` | Nenhum. |
| `FitTrack.Application` | Nenhum. |
| `FitTrack.Adapters.In.Api` | Nenhum (apenas respostas 401 existentes). |
| `FitTrack.Adapters.Out.Persistence` | Nenhum. |
| `FitTrack.UI` | `Today.razor` (templates de série), `RestTimerModal.razor` + helper JS (alarme), auth (expiração/logout). |
| `FitTrack.UITests` | Novos testes para templates, alarme (fake JS) e expiração. |

## 10. Plano de testes

### Unitários (`tests/FitTrack.Unit`)

- Helper de templates de série: drop set/pirâmide geram sequências esperadas; clamp de faixas.
- `TokenExpiry`: token expirado/não expirado; token malformado tratado como expirado.
- `RestCountdown`: alarme dispara uma vez (comportamento do modal coberto por bUnit).

### UI (`tests/FitTrack.UITests`)

- Today: aplicar atalho preenche campos; edição manual preservada; limpar volta ao prescrito.
- RestTimerModal: alarme chamado ao zerar (fake JS); não chamado quando desativado.
- Auth: token expirado resulta em estado anônimo; 401 centralizado dispara logout único.

## 11. Riscos e dependências

- **A (templates):** definição de "passo" pode não servir a todos (halteres, anilhas). Tornar
  configurável (D1) ou usar passos relativos.
- **B (alarme):** políticas de autoplay podem bloquear som até interação do usuário; o modal já
  exige clique para abrir, o que costuma liberar áudio.
- **C (logout):** redirecionamento durante prerender/SSR pode falhar; restringir a execução no
  browser.
- **C (logout):** limpar token em 401 pode deslogar por falhas transitórias de rede se a API
  responder 401 indevidamente; mitigar tratando apenas 401, não 403/5xx.

## 12. Decisões em aberto

| ID | Decisão | Recomendação |
| --- | --- | --- |
| D1 | Passo do drop set/pirâmide: percentual, absoluto em kg ou em barras? | Passo absoluto na unidade ativa (kg ou barra), com padrão 2,5 kg / 1 barra, porque o atleta pensa na unidade que está usando. |
| D2 | Logout proativo por timer no cliente ou apenas reativo a 401/exp? | Reativo (401 + exp na carga) para evitar timers extras; adicionar timer leve apenas se o feedback pedir. |
| D3 | Alarme padrão ativo ou desativado? | Ativo, com toggle visível no modal, para atender o pedido sem surpresa excessiva. |
| D4 | Vibração sempre que suportada ou apenas quando o som estiver ativo? | Vincular à mesma preferência do alarme, para um único controle. |
| D5 | Templates de série alteram repetições no drop set ou mantêm? | Manter repetições da primeira série no drop set (clássico) e variar carga; permitir edição manual. |

## 13. Sugestão de próximo passo

1. Arquiteto: validar os algoritmos de template (D1, D5), a estratégia de áudio (D3, D4) e o
   fluxo de logout (D2).
2. Implementação: começar por C (menor risco, melhora a experiência geral), depois B (UI isolada),
   depois A (mais interação com a tela Hoje).
3. Testador: cobrir os critérios de aceite de cada sub-requisito antes do merge.
