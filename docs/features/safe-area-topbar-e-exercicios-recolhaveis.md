# Safe area no iPhone 15 + exercícios da série recolhíveis

- **Slug:** `safe-area-topbar-e-exercicios-recolhaveis`
- **Status:** implementado (2026-10-23)
- **Data:** 2026-10-23
- **Origem:** relato de bug + pedido de melhoria — "o menu bar superior está sobrepondo o menu superior do iPhone 15 (horário/sinal)"; "colocar algo para expandir e mostrar os exercícios daquela série para não ficar com muito scroll, mantendo todos recolhidos em todas as telas que usam esse componente"

## 1. Problema A — topbar sob a status bar do iPhone 15

### 1.1 Diagnóstico

O PWA já declara em `src/FitTrack.Web/wwwroot/index.html`:

```html
<meta name="viewport" content="width=device-width, initial-scale=1.0, viewport-fit=cover" />
<meta name="apple-mobile-web-app-capable" content="yes" />
<meta name="apple-mobile-web-app-status-bar-style" content="black-translucent" />
```

Com `viewport-fit=cover` + `black-translucent`, no modo standalone a status bar do
iOS (hora, sinal, bateria — ilha dinâmica do iPhone 15) é desenhada **por cima** da
página, sem reservar faixa própria. Cabe à página respeitar a safe area superior.

No CSS (`src/FitTrack.UI/wwwroot/css/app.css`), a tabbar inferior e o rodapé da
`.app-main` **já** respeitam `env(safe-area-inset-bottom)` (linhas ~160 e ~210).
A `.topbar`, porém, não:

```css
/* app.css ~178 (mobile) */
.topbar {
    position: fixed;
    inset: 0 0 auto;
    height: var(--topbar-height);          /* 62px, linha ~34 */
    padding: 0 16px;                        /* sem padding-top = sem safe area */
}

.app-main {
    padding-top: var(--topbar-height);     /* também não compensa a inserção */
}
```

Resultado: marca "fittrack" e avatar/sair ficam sob a hora e a bateria.

### 1.2 Plano de correção

1. Em `app.css`, `.topbar` (mobile):
   - `height: calc(var(--topbar-height) + env(safe-area-inset-top));`
   - `padding-top: env(safe-area-inset-top);` (mantém o conteúdo centralizado
     verticalmente no restante da faixa).
2. Em `app.css`, `.app-main` (mobile):
   - `padding-top: calc(var(--topbar-height) + env(safe-area-inset-top));`
   - Assim o conteúdo nunca aparece sob a topbar nem reaparece um "buraco" no scroll.
3. Verificar se há elementos fixos ancorados no topo além da topbar (`sheet-overlay`,
   toasts, FABs — os fixos existentes já usam apenas `safe-area-inset-bottom`);
   nenhum deles fica no topo, então não precisam de ajuste.
4. Desktop (≥1024px) não muda: `.topbar` está `display: none` e o padding da
   `.app-main` é zerado nesse breakpoint (linhas ~381–397).
5. `env()` degrada a 0 em navegadores sem safe area (Chrome/Android, desktop) —
   sem necessidade de `@supports` nem de media query por dispositivo.

### 1.3 Critérios de aceite (A)

- iPhone (standalone): hora/sinal/bateria nunca sobrepõem a marca nem o botão sair;
  margem visível entre a status bar e o conteúdo da topbar.
- O primeiro card da página não fica parcialmente escondido atrás da topbar.
- Desktop e Android: layout idêntico ao atual (env() = 0).

## 2. Problema B — exercícios da série sempre abertos (scroll longo)

### 2.1 Diagnóstico

`src/FitTrack.UI/Pages/Today.razor` (linha ~109) renderiza a lista completa de
exercícios de **cada** treino (A–E) da série, sempre visível, dentro do
`workout-card`:

```razor
<ul class="exercise-list">
    @foreach (var prescribed in workout.Exercises)
    {
        <li class="exercise-row"> … nome, grupo, séries × reps, carga, timer de descanso … </li>
    }
</ul>
```

Com uma série cheia (até 5 treinos × vários exercícios), a tela "Hoje" exige muito
scroll antes do card do treino do dia. O `workout-card` também é usado em
`SeriesDetail.razor` (edição, com `PrescriptionEditor`), então a aderência do
colapso deve viver em um **componente compartilhado**, não inline na página —
é esse o requisito "todas as telas que usam esse componente".

### 2.2 Plano: componente `SeriesExerciseList` (nome provisório)

Novo componente em `src/FitTrack.UI/Components/` com fronteira leve:

- **Parâmetros:** `Exercises` (a lista prescrita já carregada) e `Expanded`
  (bool, padrão `false` = todos recolhidos). A página continua buscando os
  dados — o componente só deixa de exibir a lista quando recolhida.
- **Gatilho:** no `workout-head`, ao lado/abaixo do contador já existente
  ("N exercícios"), um botão com chevron (`AppIcon name="chevron-down"`,
  rotacionando quando aberto), `aria-expanded` e rótulo "Mostrar/Esconder
  exercícios" — o card cabeceira inteiro clicável é evitado para não conflitar
  com o botão "Iniciar treino".
- **Transição:** esconder a `<ul>` quando recolhido (sem render é suficiente,
  Blazor re-renderiza). Evitar animação de altura JS nesta fatia.
- **Reuso:** `Today.razor` passa a usá-lo. `SeriesDetail.razor` segue com o
  `PrescriptionEditor` no corpo, mas o cabeçalho do card ganha o mesmo controle
  de expandir/recolher (o editor de prescrição é caro visualmente com 5 treinos
  abertos), com o estado inicial recolhido também.

### 2.3 Decisão de estado

- O estado de expansão vive na página (dicionário `workoutId → bool`) para
  sobreviver a re-renders do ciclo de ações (`StartAsync`, `_busy`).
- Alternativa aceita, se preferir o componente dono do estado: `[Parameter]`
  `ExpandedInicial` + `EventCallback<bool> ExpandedChanged` (two-way binding),
  mantendo a página atualizada. Escolher durante a implementação; a fronteira
  externa não muda.
- Não persistir em storage: o padrão é sempre recolhido ao abrir a tela,
  sem "lembrar" da sessão anterior (simplicidade > conveniência aqui;
  revisar se houver retorno de usuários pedindo persistência).

### 2.4 Critérios de aceite (B)

- Ao abrir "Hoje", nenhum treino mostra os exercícios: só cabeçalho
  (label A–E, nome, contador "N exercícios", chip de status) e a ação
  ("Iniciar treino") sempre visíveis fora da lista. O timer de descanso
  continua existindo apenas dentro das `exercise-row` — recolhido, não aparece.
- Expandir um treino revela nome, grupo muscular, séries × reps, carga e
  botão de descanso exatamente como hoje (sem mudança visual na linha).
- Vários treinos podem estar abertos ao mesmo tempo (recolher é individual).
- SeriesDetail: mesma mecânica para a prescrição de cada treino; salvar/remover
  continuam acessíveis com o card recolhido.
- Scroll da tela "Hoje" com série completa fica limitado a, no máximo,
  5 cabeçalhos de card com a lista fechada.

## 3. Achado do ciclo de validação — auto-zoom do iOS nos inputs (scroll lateral percebido)

A primeira varredura (viewport 393×852, iPhone 15) não encontrou overflow de
página alguma na largura do dispositivo em nenhum estado medido (páginas do
personal e do atleta, exercícios expandidos, prescrição expandida, treino em
andamento, `set-log` aberto) — `scrollWidth` sempre igual à `clientWidth`, e
nenhum elemento com `right > viewport` além dos chips em carrossel interno de
`/exercises` (scroll próprio, por design).

O sintoma "está passando um pouco da tela" no iPhone veio de outra fonte,
medida em runtime: **todos os campos de texto** ficavam em `font-size: 0.95rem`
(15,2px) — abaixo dos **16px** que o iOS Safari exige. Ao focar qualquer campo
(e-mail, senha de login e de conta, número/reps do `set-log`), o dispositivo
aplica **auto-zoom** (~1,05×–1,2×) e a página passa a puxar lateralmente a
"um pouco" do tamanho da tela, exatamente como relatado.

**Correção:** a regra compartilhada de inputs em `app.css` sobe de
`font-size: 0.95rem` para `1rem` (16px — aumento de 0,8px, imperceptível),
mantendo todo campo acima do limiar do zoom automático em qualquer tela.

### 3.1 Achado adicional do ciclo — `aria-expanded` booleano

A mesma varredura expôs um defeito no componente recolhível: ligar
`aria-expanded="@algumBool"` em Razor faz o Blazor emitir o atributo
**vazio** (`aria-expanded=""`) quando `true` e **omitido** quando `false` —
o leitor de tela nunca vê o estado, e o CSS do chevron
(`[aria-expanded="true"]`) não combina com o valor vazio: a seta não girava.
`SeriesExerciseList` e o toggle do `SeriesDetail` agora emitem a string de
forma explícita (`@(aberto ? "true" : "false")`).

## 4. Fatiamento

| Passo | Arquivo | Entrega |
| --- | --- | --- |
| 1 | `src/FitTrack.UI/wwwroot/css/app.css` | safe area da `.topbar` + `.app-main` (A) |
| 2 | `src/FitTrack.UI/Components/SeriesExerciseList.razor` (novo) + CSS (`.exercise-toggle` etc.) | componente recolhível |
| 3 | `src/FitTrack.UI/Pages/Today.razor` | usa o componente (B) |
| 4 | `src/FitTrack.UI/Pages/SeriesDetail.razor` | mesmo recolhimento para a prescrição (B) |
| 5 | Docs: este arquivo → status `implementado` | registro |

Fora de escopo: animação de altura suave, persistência do estado aberto,
mudança nos endpoints (nenhum dado novo é necessário).

## 5. Verificação

- Emulador/DevTools: dimensionar viewport do iPhone 15 (393×852) e conferir
  `getComputedStyle(document.querySelector('.topbar')).height` ≥
  `62px + inset` via painel de safe area.
- Rodar o app e percorrer: Hoje (série completa), SeriesDetail (treinos A–E),
  AthleteWorkouts/MyWorkouts (cards de histórico — sem lista prescrita, sem mudança).
- Varredura de overflow por página e estado no iframe 393×852
  (`scrollWidth == clientWidth`, nenhum elemento com `right > innerWidth`),
  para personal e atleta, treino fechado/em andamento e listas expandidas —
  feita no ciclo 2026-10-23 (sem overflow; ver seção 3).
- `[...document.querySelectorAll('input:not([type=checkbox]):not([type=radio])')]`
  todos com `getComputedStyle(el).fontSize >= 16px` nas telas de login, conta,
  catálogo, Hoje (set-log) e SeriesDetail.
- Todos os `.exercise-toggle` com `getAttribute('aria-expanded')` entre
  `"true"`/`"false"` (nunca `""` ou `null`) e chevron girando quando aberto.
- `dotnet build` da solution + suítes existentes (UI não possui testes de snapshot
  para esses componentes; verificação é manual/guiada no dispositivo).
