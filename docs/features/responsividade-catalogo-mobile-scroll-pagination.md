# Catálogo no mobile: fim do scroll horizontal causado pela `Pagination`

- **Slug:** `responsividade-catalogo-mobile-scroll-pagination`
- **Status:** implementado (2026-10-21)
- **Data:** 2026-10-21
- **Origem:** relato de bug — "no Samsung S23 a tela do catálogo está scrollando para o lado (scroll horizontal) e sumindo o menu inferior no navegador; pode ser com o S8+"
- **Feature anterior relacionada:** `catalogo-mobile-crud-menu-prioridade` (tabbar de 4 abas + painel "Mais", combobox, CRUD de exclusão — já entregues e saudáveis; fora de escopo aqui)

## 1. Contexto e problema

### 1.1 Relato e reprodução

No Samsung S23 o usuário percebe que a tela do catálogo rola horizontalmente
e o menu inferior "some". Diagnóstico executado em emulação real (app local
servido em iframe com viewport controlada; login `trainer@fittrack.local`,
médias CSS: S23 = 360×780, S8+ = 412×915):

| Página | Viewport | `scrollWidth` vs `innerWidth` | Resultado |
| --- | --- | --- | --- |
| `/exercises` | 412 e 360 | 402/350 ≤ largura | **Saudável** — sem overflow |
| `/exercises/manage` | 412×915 | 402 ≤ 412 | **Saudável** |
| `/exercises/manage` | 360×780 | **395 > 360** | **REPRODUZIDO** — página rola para o lado |

Medições complementares em 360×780 em `/exercises/manage`: `section.card`
mede **379px** (16px de borda/gap + 379 > 360) e **146 elementos** estão
esticados. A tabbar permanece `position: fixed` colada na viewport — o
"sumiço" do menu no dispositivo real é a consequência **percebida** do pan
horizontal + zoom/fit de largura do navegador, não um bug da barra.

### 1.2 Causa raiz medida

O componente compartilhado `Pagination`
(`src/FitTrack.UI/Components/Pagination.razor` + `.pagination`/`.pagination-info`
em `src/FitTrack.UI/wwwroot/css/app.css` — estado **antes** da correção; os
blocos hoje ocupam ~2320–2338) tem
**min-content de 341px**:

```css
.pagination {                    /* app.css 2320 */
    display: flex;               /* sem flex-wrap — nunca quebra */
    align-items: center;
    justify-content: center;
    gap: 12px;
}
.pagination-info { min-width: 110px; }  /* largura fixa */
```

Composição da linha: botão "Anterior" (ícone + rótulo) + `.pagination-info`
(110px fixos) + botão "Próxima" (ícone + rótulo) + `gap: 12px` entre os três
= **341px de min-content**. Dentro do card do `manage-grid`, o grid replica o
min-width do item (comportamento de grid: item define a largura da coluna
implícita) e empurra o card para 379px → overflow horizontal da página em
telas com ≤ ~395px de largura de conteúdo.

O painel "Mais" e a tabbar de 4 abas já implementados estão saudáveis nas
duas larguras (sheet mostra Catálogo, Exercícios, Minha conta, Sair para o
personal) — nada a fazer neles.

### 1.3 Amplo impacto (componente compartilhado)

`Pagination` é usado em **8 pontos lógicos em 7 páginas** (16 usos no código —
2 por listagem: branch vazio + branch com dados; `/exercises/manage` tem 4,
pois mantém 2 grids × 2 branches): `/exercises`, `/exercises/manage`,
`/series`, `/athletes`, `/athlete-workouts`, `/my-workouts` e `/admin/users`.
Qualquer correção no CSS do componente vale para todas — o plano de regressão
deve cobrir ao menos uma listagem não-catálogo (ex.: `/series`) para garantir
que o compartilhamento continua saudável.

## 2. Objetivo e valor

- `/exercises` e `/exercises/manage` (e qualquer página com `Pagination`)
  sem rolagem horizontal de página em ~360px de largura — o catálogo fica
  100% utilizável no S23/S8+.
- Menu inferior sempre percebido como fixo no fim da viewport (eliminar o
  pan que fazia o usuário "perder" a tabbar).
- Zero mudança visual no desktop (≥1024px) e em larguras ≥ ~550px (o `clamp`
  satura nos 110px originais — desktop fica bit-a-bit idêntico). Na faixa
  ~360–550px a informação "X–Y de Z" pode reduzir até 88px (imperceptível —
  texto centrado com `text-align: center`).

## 3. Escopo

**Dentro:** CSS do componente `Pagination` (`.pagination`, `.pagination-info`)
e validação de containment do `manage-grid` em mobile.

**Fora (já entregues ou separados):** hinterland do combobox (corrigido na
feature anterior), tabbar de 4 abas + painel "Mais" (implementado), exclusão
CRUD (implementada), qualquer ajuste na sidebar desktop.

## 4. Correção aplicada (2026-10-21)

1. **`.pagination { flex-wrap: wrap; }`** em `app.css` — o min-content do
   componente deixa de ser ditado pela soma da linha (341px, botões + info
   fixa + gaps) e passa a ser o maior item. Em 360px o componente quebra em
   **2 linhas** (altura medida 84px, linhas centradas) em vez de estourar a
   página; em ≥768 a linha única é preservada.
2. **`.pagination-info { min-width: clamp(88px, 20vw, 110px); }`** — **adotado**
   (decisão Q1 do arquiteto, 2026-10-21): a informação encolhe até 88px em
   telas estreitas e satura nos 110px originais em ≥550px (desktop ≥1024px
   bit-a-bit idêntico).
3. **Containment do `manage-grid` (Q2): NÃO foi necessário** — descartado na
   verificação pós-fix: com o item 1 o card volta a ≤ viewport (sw 350 ≤ 360;
   cards 318 ≤ 360), sem o fallback no grid.

Nenhuma mudança no `Pagination.razor` (marcação/e `aria-live` permanecem).

## 5. Regras / invariantes

- O `Pagination` nunca pode exceder a largura do container em nenhuma largura
  suportada (min-content do componente ≤ 360px de viewport).
- Em larguras onde a linha única cabe, o layout visual é idêntico ao atual
  (botões centralizados, info no meio, gap 12px).
- O indicador "X–Y de Z" mantém `aria-live="polite"` (`Pagination.razor`,
  linha 10) — quebra de linha interna não pode remover a semântica.
- Desktop (≥1024px) sem regressão: comportamento atual preservado.

## 6. Critérios de aceite (mensuráveis)

6.1. Em 360×780 e 412×915: `document.scrollingElement.scrollWidth <= innerWidth`
     em `/exercises` **e** `/exercises/manage` (overflow horizontal da página = zero).
6.2. Nenhum `section.card` mais largo que a viewport no mobile
     (card ≤ largura de conteúdo; hoje mede 379px > 360).
6.3. O row "X–Y de Z" pode quebrar numeração em linha (mudança sutil de fonte
     do `pagination-info`), mas preserva `aria-live="polite"`.
6.4. Tabbar presente e fixa no fim da viewport nas duas páginas e nas duas
     larguras; painel "Mais" abre e lista os itens por prioridade
     (já coberto pela feature anterior — smoke check).
6.5. Desktop (≥1024px) sem regressão visual do `Pagination`
     (layout idêntico ao atual).
6.6. `/series` (listagem não-catálogo que usa o mesmo componente) sem overflow
     horizontal em 360×780 — o compartilhamento fica saudável.

## 7. Plano de regressão

Verificação em emulação com viewport controlada (mesmo método do diagnóstico),
larguras **360 / 412 / 768 / 1120**:

| Página | Por quê |
| --- | --- |
| `/exercises` | consulta do catálogo; estava saudável — não pode regredir |
| `/exercises/manage` | página que reproduz o bug (360×780) — critérios 6.1/6.2 |
| `/series` | listagem qualquer que usa `Pagination` — valida o compartilhamento (6.6) |

Em cada ponto: `scrollWidth ≤ innerWidth`, card sem estouro, tabbar fixa e
visível, "Mais" funcional. Em 768/1120: layout do `Pagination` idêntico ao
atual (sem quebra de linha onde hoje não há).

`dotnet build` verde e testes unitários verdes (mudança é CSS-only; nenhum
teste de unidade toca CSS). Evidência principal: medições objetivas da
seção 7.1 (screenshot pós-fix capturado durante a validação, não versionado
em `docs/images/`).

### 7.1 Resultado da execução (2026-10-21)

| Ponto | `scrollWidth`/viewport | Overflow | Observação |
| --- | --- | --- | --- |
| `/exercises` 360×780 | 350/360 | não | card 318 |
| `/exercises` 412×915 | 402/412 | não | — |
| `/exercises/manage` 360×780 | **350/360** | não | **bug resolvido** — antes 395/360; cards 318 |
| `/exercises/manage` 412×915 | 402/412 | não | cards 370 |
| `/exercises/manage` 768×900 | 758/768 | não | 1 coluna (cols 1) |
| `/exercises/manage` 1120×800 | 1110/1120 | não | grid 2 colunas, cards 370 |
| `/series` 360/412/768/1120 | 350 / 402 / 758 / 1110 | não | tabbar `none` e sidebar 276px em ≥1024 |
| `/athletes` 360 e 1120 | 350/360, 1110/1120 | não | — |

Tabbar com `bottom-gap 0` em todos os pontos mobile; `Pagination` em 360
quebra em 2 linhas (h 84px, sem estouro); `aria-live="polite"` preservado.
`dotnet build FitTrack.slnx`: 0 erros.

## 8. Impacto arquitetural

- **Único arquivo alterado:** `src/FitTrack.UI/wwwroot/css/app.css`
  (regras `.pagination` e `.pagination-info`).
- Sem mudanças em domínio, portas, use cases, endpoints ou componentes Razor.
- Sem migrações, sem novos pacotes, sem JS interop.

## 9. Riscos e dependências

| Risco | Mitigação |
| --- | --- |
| `flex-wrap` mudar o alinhamento em largura intermediária (~342–400px) onde hoje há linha única apertada | Range onde a quebra ocorre é novo comportamento; validar visualmente em 360/412 |
| Quebra de linha do "X–Y de Z" em 360px alterar a percepção da numeração | Manter `aria-live="polite"` (6.3); clamp defensivo (item 3) se aprovado |
| Grid do `manage-grid` ainda replicar min-width por outra via | Verificar 6.2 após o item 1; só então avaliar `min-width: 0`/`minmax(0,1fr)` no mobile |
| Outras páginas sentirão o `flex-wrap` (componente compartilhado) | Plano de regressão da seção 7 inclui `/series` |

## 10. Decisões em aberto

| # | Pergunta | Estado |
| --- | --- | --- |
| Q1 | Adotar o defensivo `.pagination-info { min-width: clamp(88px, 20vw, 110px) }`? | **Adotado** (arquiteto aprovou em 2026-10-21) |
| Q2 | Se 6.2 ainda falhar após o item 1, adicionar `min-width: 0` no grid mobile? | **Não necessário** — verificação pós-fix: sw 350 ≤ 360 e cards 318 ≤ 360 sem o fallback |
