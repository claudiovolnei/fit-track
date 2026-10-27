# PWA nativo: Auto-finish alarm — uso de recursos nativos offline

- **Slug:** `pwa-native-resources-auto-finish`
- **Status:** proposto (2026-10-26)
- **Data:** 2026-10-26
- **Origem:** bug "Auto-finish-alarm.js:1 Uncaught SyntaxError: Unexpected token '<'" — ao abrir a PWA o JS retorna HTML invés do script. Requisito extra: componentes usem APIs nativas (AudioContext / Vibração / Notificação) com fallback gracioso.

## 1. Contexto e problema

### 1.1 Erro de carga do JS na PWA

O `MainLayout.razor` carrega `<script src="/js/auto-finish-alarm.js">`. Na PWA, o service worker (`service-worker.published.js`) cacheia assets, mas `auto-finish-alarm.js` retorna `index.html` (`<!DOCTYPE...`) no lugar do script, gerando `Uncaught SyntaxError: Unexpected token '<'` no parser do browser.

Razão provável: a estratégia de fetch (`onFetch`) faz cache match aleatoriamente e, em cache quebrado/corrupto, pode retornar `index.html` como fallback para qualquer GET que não matches direto. A linha `cache.match(event.request)` deve ser preciso mas um stato mal instalado ou rebuild incorreto pode causar mismatch. Necessário prevenir fallback de index.html para URLs de destino .js (como /js/auto-finish-alarm.js).

### 1.2 Requisito adicional: recursos nativos

O `auto-finish-alarm.js` já usa `navigator.vibrate()` e `window.AudioContext`, mas a comunicação Cross-Interactive (Blazor → JS) é feita via string (startAutoFinishAlarm/stopAutoFinishAlarm), inadequada para notificações push do SO. O requisito pede que, quando o PWA está baixado, o componente use APIs nativas do SO (`AudioContext`, `Vibration`, `Notification` API) com degrade gracioso se APIs não estiverem disponíveis.

## 2. Objetivo e valor

- **Correção:** `auto-finish-alarm.js` sempre serve em PWA (exceto quando ainda não instalada).
- **Funcionalidade:** o alarm usa APIs nativas do SO para vibração + áudio sem rede, com degrade gracioso se APIs indisponíveis.
- **Notificação:** usar `Notification` API para alertar o atleta em dispositivos Android/iOS sincronizados com o SO push.
- **Feedback visual:** aviso no overlay do `MainLayout` antes da interação, adequado a tela mobile.

## 3. Atores envolvidos

- **Usuário** — abre a PWA no celular, vê aviso "Treino expirado" com som/vibração/notificação nativa.
- **Front (Blazor PWA)** — `MainLayout` + `AutoFinishAlarm` component.
- **Service Worker** — garante caching correto de todos os arquivos staticos incl. JS.

## 4. Escopo

### 4.1 Incluído

1. Corrigir o service worker `service-worker.published.js` para garantir match précise de `auto-finish-alarm.js` (não caí na fallback de index.html para GETs estáticos).
2. Refinar `auto-finish-alarm.js`: estrutura robusta com try/catch em cada chamada API nativa, fallback digno (Sound → Notification → nothing).
3. Aumentar `AutoFinishAlarm.razor`: integrar `Notification` API + visão acima do app; botão "Parar" cessa everything.
4. Garantia que JS seja cacheavel na First Offline Cache (Fresh Install); extrairmos fatores de correção se saição de préc.
5. Testes:unitário do JS `auto-finish-alarm.js` (lógica); integração do pub projetas com um cenário de cache corrompido.

### 4.2 Não incluído

- Service worker para novas páginas; rota atual mantida.
- Push notification server-side (backend); restringe-se ao front-side native.
- Mudança de prescrição dos treinos; apenas usuário atletas on-line no tread actual.
- Perfil de luminosidade e skins para as telas; mantém-se cores existentes.

## 5. Contratos HTTP / Frontend

Nenhum; تغيير na parte de API. Focus only mudança no front-side.

## 6. Modelo de dados

**Sem nova tabela, sem migração.** Apenas update do JS-front e ajustes scoped.

- `WorkoutSession`: já contém todo histórico necessário.
- Entidades tocadas: `WorkoutSession`, `PerformedSet` (existente); tudo reutilizado.

## 7. Impacto

| Projeto | Impacto |
|---|---|
| `FitTrack.UI` | Novo componente `AutoFinishAlarm.razor` estendido (already exists + augmentation); `MainLayout.razor` já referencia `auto-finish-alarm.js`. |
| `FitTrack.Web` (wwwroot) | Service worker aprimorado para filtrar como' index.html no fallback somente para URLs de navegação; LRU cache preventively descripts clones corrompidos. |
| Testes | Unitários do JS logic; eye do PWA servindo correctamente o asset. |

## 8. Riscos e dependências

- **Cache corrupt share:** o STEP enhance para rewrite a tentativa `cache.match(req)` veja se context > 'GET', evitando match arbitrariedade.
- **Audio autoplay blocking:** browsers mobile block AudioContext sem interação; precarre o callback 'Parar' instantaneamente; fallback à notificação visual.
- **Vibration API support:** nem todos rod support; já degrade gracioso.
- **Notification API:** Chrome Android suporta; Safari iOS require HTTPS + permissão no SO.

## 9. Decisões em aberto

| ID | Decisão | Suposição |
|---|---|---|
| D1 | Fallback index.html protege indices? Somente para mode='navigate'; noutras cam apagens de static. | Sim. O look může ser exact response para cam statice previamente cached. |
| D2 | Notification callback força interação primária? | Usamos Notification com permissão Asked; se Bloqu, fallback vibraco. |
| D3 | Quando PWA's Answers novamente Server (non F'=full de prec), occurs re-caching do SW? | Sim: I assert que QUALITY reinstallacao regenera QUALITY do cache novo. |
| D4 | Aonde apreсим von em tela toda; ativada por param global? | \(MainLayout \) com READ=@{ @inject bool AlarmActive } na homepage history page. |

## 10. Critérios de aceite

1. **Carregamento:** Abrir a PWA corretamente -> `auto-finish-alarm.js` carrega sem SyntaxError '<'; todos os assets estáticos são servidos do cache após primeira instalação.

2. **Native resources:** 
   - Ao alarmar, tentar `navigator.vibrate([500,200,500,200,500])`; se suportado.
   - Tentar `new (window.AudioContext || window.webkitAudioContext)()` e iniciar oscilador; se suportado.
   - Se `Notification.permission === 'granted'`, usar Notification API para alerta mobile push; se não estiver concedida, pedir permissão `Notification.requestPermission()`; se concedida, usar Notification API; se negada, não usar Notification.
   - Se nenhum recurso nativo suportado, não falhar; degradar graciosamente.

3. **Degrade gracioso:** Em navegador/dispositivo sem suporte a Vibration/Audio/Notification, o alarme visual aparece sem exceções não tratadas.

4. **Botão Parar:** cessa vibração, áudio e notificações imediatamente (chama `stopAutoFinishAlarm`). Atualiza estado do `AutoFinishAlarm.razor` (`AlarmActive=false`).

5. **Offline:** PWA funciona standalone após instalação; cache deve incluir o script JS.

6. **Suporte a Service Worker:** O service worker published devolve index.html apenas para requests de navegação (`event.request.mode === 'navigate'`), não para GETs estáticos (incluindo `.js`, `.css`, `.woff`, etc.).

7. **Validação de cache:** Durante testes, simular cache corrompido em service-worker.published.js e confirmar que a correção garante match preciso de `auto-finish-alarm.js` pelo navegador.

## 11. Sugestões

1. **Implementação sequencial** menor primeiro ("service worker menos bridage") -> JS aprimorado -> AutoFinishAlarm.razor augmented.
2. **Validar** com o testador provocando a caixa 'corrompido' região de cache (simular com mismatch).
3. **Arquiteto** se necessario valida dependência do SW + aviso de código.
