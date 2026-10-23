# Histórico — volume comparado por exercício

- **Slug:** `historico-volume-por-exercicio`
- **Status:** implementado (2026-10-23) — decisões D1 (ponderação por sets), D2
  (séries com carga 0 fora da média), D3 (`notPerformed` penaliza), D4 (rótulo
  mantido), D5 (`—` + nota), D6 (`0.##` kg) e D7 field do resumo seguidas;
  provado com os 308 testes unitários e 120 testes de integração sobre
  PostgreSQL reais.
- **Data:** 2026-10-22
- **Feature anterior relacionada:** `busca-paginacao-volume-avatar` (apresentou a
  comparação de volume no histórico; o modelo de tonelagem total desta feature
  é substituído nos pontos citados aqui).
- **Decisão do dono (2026-10-22):** o modelo novo é único em toda a aplicação
  (painéis, card de sessão e API), a carga representativa do atleta é a
  **média das séries** e o painel percentual mostra **só o percentual**,
  calculado sobre o prescrito.

## 1. Contexto e problema

O histórico de treinos tem uma faixa de quatro painéis (`.stat-grid` em
`MyWorkouts.razor` e `AthleteWorkouts.razor`), alimentada por
`WorkoutVolumeCalculator`:

1. **Treinos** — quantidade de sessões.
2. **Séries** — quantidade de séries registradas.
3. **Volume feito** — tonelagem total executada (Σ carga × repetições de todas
   as séries), com a tonelagem prescrita embaixo.
4. **Aumento** — delta de tonelagem (feito − prescrito) em kg, com o percentual
   entre parênteses.

O problema está no modelo por trás dos painéis 3 e 4:

1. **A comparação é por sets, não por exercício.** O delta soma tonelagem de
   todas as séries; o personal não enxerga "a carga que eu coloquei contra a
   carga que o atleta usou".
2. **O delta mistura carga e repetições.** Um atleta que fez 12 reps em vez das
   10 prescritas, na mesma carga, aparece como "aumento" — quando na verdade
   não aumentou carga nenhuma.
3. **O percentual vem escondido no painel 4**, dentro do valor em kg, e não é
   "apenas o aumento": acompanha o kg assinado, com a mesma mistura de reps.

O pedido do dono corrige isso: manter treinos e séries; no "volume feito",
comparar **por exercício a carga prescrita com a carga executada** (personal
40 kg → atleta 48 kg); no percentual, mostrar **apenas o aumento**, porque se
calcula sobre o volume comparado.

## 2. Objetivo e valor

- **Personal:** ler no histórico, de relance, quanto o atleta **aumentou de
  carga por exercício** em relação à prescrição — sem repetições sujando o
  número.
- **Atleta:** ver o mesmo comparativo nos próprios painéis e no card de cada
  treino, com o mesmo número que o personal vê.
- **Time:** uma única regra de comparação (leitura pura, sem persistência),
  reutilizada por SQL, UI e testes — como o modelo atual já faz.

## 3. Atores envolvidos

- **Atleta** — vê os painéis em `/workouts` e o comparativo nos seus cards.
- **Personal trainer / Admin** — vê os mesmos painéis em
  `/athletes/{userId}/workouts`.
- **Sistema (API)** — calcula a comparação na leitura
  (`WorkoutVolumeCalculator`); nenhum dado novo é gravado.

## 4. Escopo

### 4.1 Incluído

1. Sub-requisito A — novo modelo de comparação **por exercício**, com carga
   prescrita e carga executada (média das séries), delta e percentual.
2. Sub-requisito B — os 4 painéis do histórico: treinos e séries intactos;
   "Volume feito" passa a exibir o **aumento total em kg** do modelo novo;
   painel de percentual exibe **apenas o percentual**.
3. Sub-requisito C — o card de cada sessão (`SessionCard.razor`) migra para o
   modelo novo, incluindo as linhas por exercício.

### 4.2 Não incluído

- **Home (`Home.razor`) e Hoje (`Today.razor`):** continuam com tonelagem bruta
  (Σ carga × reps). Não há prescrição em jogo ali; é trabalho executado, não
  comparação (D8).
- **Contrato HTTP:** a forma dos endpoints e dos DTOs não muda — os campos
  mudam de semântica e os comentários/afirmações de teste acompanham.
- **Banco de dados:** nenhuma tabela, coluna ou migração; o cálculo continua na
  leitura, a partir de prescrição e séries já persistidas.
- **Unidade "barra"** no comparativo: o modelo continua em kg (a conversão do
  finish já acontece no cliente).

## 5. Sub-requisito A — Modelo de comparação por exercício

### 5.1 Requisitos funcionais

1. Para cada exercício da sessão existe **uma carga prescrita** e **uma carga
   executada** representativas, e não mais tonelagens agregadas por séries.
2. A carga executada do exercício é a **média aritmética das cargas das séries
   registradas** daquele exercício na sessão (decisão do dono).
3. A carga prescrita do exercício é a média **ponderada por sets** das cargas
   das linhas de prescrição daquele exercício (D1).
4. O delta do exercício é a **diferença de carga** (`executada − prescrita`),
   em kg de carga; nenhum valor é multiplicado por séries ou repetições.
5. O delta da sessão é a **soma dos deltas por exercício**:
   `personal 40 kg → atleta 48 kg` soma `+8 kg`; dois exercícios assim somam
   `+16 kg` (o "10 kg aumentados" do pedido é a soma dos deltas dos
   exercícios do treino).
6. O percentual da sessão é `DeltaPercent = delta ÷ prescrito × 100`
   (fórmula na 5.3); o painel e o card nunca recalculam percentual na tela —
   sempre consomem o valor já arredondado da API.

### 5.2 Statuses (inalterados)

Os quatro status continuam, agora com a semântica de carga:

| Status | Condição | Entra no delta? |
| --- | --- | --- |
| `matched` | Prescrito com carga e atleta registrou séries do exercício | Sim |
| `notPerformed` | Prescrito com carga, atleta não registrou séries do exercício | Sim, com executada = 0 (penaliza o faltoso, como hoje) |
| `bodyweight` | Prescrição do exercício não tem carga (todas as linhas sem carga) | Não (não há base de comparação) |
| `notPrescribed` | Atleta registrou séries de exercício fora da prescrição | Não (não há base de comparação) |

### 5.3 Fórmulas (propostas)

Por exercício `e` da sessão:

- `cargaFeita(e)` = média simples das cargas das séries de `e` (D2: séries com
  carga 0 entram na média? ver decisão).
- `cargaPrescrita(e)` = `Σ(sets × carga) / Σ(sets)` linhas de `e` com carga.
- `Δ(e)` = `cargaFeita(e) − cargaPrescrita(e)`.

Por sessão:

- `PrescribedKilograms` = `Σ cargaPrescrita(e)` sobre os exercícios
  `matched` + `notPerformed`.
- `PerformedKilograms` = `Σ cargaFeita(e)` sobre os exercícios `matched`.
- `DeltaKilograms` = `PerformedKilograms − PrescribedKilograms`
  (equivalente a `Σ Δ(e)` dos `matched` menos a prescrição dos `notPerformed`).
- `DeltaPercent` = `DeltaKilograms / PrescribedKilograms × 100`, com 2 casas
  (`AwayFromZero`, padrão atual); `null` quando `PrescribedKilograms <= 0`.

Exemplo (prescrição: A 3×10 @ 40 kg, B 3×8 @ 30 kg; atleta fez A 48 kg nas 3
séries e B 32 kg nas 3 séries):

- A: `48 − 40 = +8`; B: `32 − 30 = +2`; delta da sessão = **+10 kg** (exemplo
  do pedido).
- `PrescribedKilograms` = 70; `PerformedKilograms` = 80; `DeltaPercent`
  = `10 / 70 × 100` = **+14,29 %**.

### 5.4 Resumo do histórico (`Summarize`)

- Agrega o que a sessão já calculou: `Σ session.Volume?.PerformedKilograms`,
  `Σ session.Volume?.PrescribedKilograms`, delta e percentual com as mesmas
  fórmulas da sessão.
- Sessões sem prescrição comparável (`session.Volume == null`) continuam
  contando em **Treinos** e **Séries** e em `SessionsWithoutPrescription`,
  mas não entram no delta (igual ao comportamento atual do resumo).
- O termo "volume feito" dos DTOs passa a significar "soma das cargas médias
  executadas" — conforme o modelo novo. Sem mudança de nome de campo.

### 5.5 Critérios de aceite

1. Dada uma sessão com exercício prescrito a 40 kg e séries registradas a 48 kg,
   quando a API calcula o volume, então o exercício tem
   `PerformedKilograms = 48`, `PrescribedKilograms = 40`, `DeltaKilograms = 8`
   e `DeltaPercent = 20`.
2. Dada uma sessão com duas cargas diferentes entre as séries do mesmo
   exercício (ex.: 40 e 48), quando a API calcula, então a carga executada é a
   média (44).
3. Dados dois exercícios com deltas +8 e +2, quando a API calcula a sessão,
   então `DeltaKilograms = 10` e `DeltaPercent = 14,29` (70 prescrito).
4. Dado um exercício `notPerformed`, quando a API calcula, então o delta da
   sessão desconta a carga prescrita dele.
5. Dado um exercício `notPrescribed` ou `bodyweight`, quando a API calcula,
   então nenhum valor dele entra no delta ou no percentual.
6. Dada uma sessão sem prescrição real (`prescription == null`), quando a API
   calcula, então `Volume` é `null`, como hoje; dada uma prescrição toda sem
   carga (`bodyweight`), então o delta é zero e `DeltaPercent` é `null`.

## 6. Sub-requisito B — Painéis do histórico

### 6.1 Requisitos funcionais

Aplica-se a `/workouts` (atleta) e `/athletes/{userId}/workouts`
(personal/admin) — os dois usam a mesma marcação `.stat-grid`:

1. Painel **Treinos** — inalterado (`_summary.SessionCount`).
2. Painel **Séries** — inalterado (`_summary.SetCount`).
3. Painel **Volume feito** — valor principal vira o **aumento total do modelo
   novo**: `+10 kg` (exemplo), com classes `is-up`/`is-down` conforme o sinal.
   Linha secundária: `Prescrito {prescrito} kg · Feito {feito} kg (média por
   exercício)` (D4).
   Sem prescrição comparável: `—` com a nota explícita (D5).
4. Painel **Aumento** — valor **apenas percentual**: `+14,3 %`, com classes
   `is-up`/`is-down`; quando `DeltaPercent == null`, exibe `—` no lugar do
   número, notando "sem base de comparação" (D5).
5. O painel 4 deixa de exibir o kg assinado (o kg do aumento vive no painel 3).

### 6.2 Critérios de aceite

1. Dado um histórico com prescrições comparáveis, quando o atleta abre
   `/workouts`, então o painel "Volume feito" mostra o aumento total em kg do
   modelo novo e o painel "Aumento" mostra só o percentual.
2. O mesmo resumo aparece idêntico no histórico do personal do atleta.
3. Dado um histórico sem nenhuma prescrição comparável, quando os painéis
   abrem, então treinos e séries mostram as contagens e os painéis 3 e 4
   mostram "—" com as notas.
4. Nenhum painel recalcula percentual na tela: ambos consomem
   `WorkoutVolumeSummaryResponse`.

## 7. Sub-requisito C — Card da sessão (SessionCard)

### 7.1 Requisitos funcionais

1. O bloco de comparação da sessão usa os valores do modelo novo:
   `Feito {soma cargas médias} · Prescrito {soma cargas} · Aumento {delta}
   ({percent})`.
2. A linha por exercício mostra a carga, não a tonelagem:
   `Supino · prescrito 40 kg · feito 48 kg (média) · +8 (+20 %)`.
3. Os textos dos estados alternativos continuam:
   `não executado · prescrito 40 kg`, `não prescrito`,
   `prescrição sem carga · feito {carga média executada}`.
4. O strip de resumo do card (séries · volume · duração) continua com a
   tonelagem bruta das séries: é trabalho executado, não comparação.
5. A sessão sem prescrição (`Volume == null`) continua mostrando o volume
   bruto com "sem prescrição para comparar".

### 7.2 Critérios de aceite

1. Dada a sessão do exemplo (A 40→48, B 30→32), quando o card renderiza, então
   o bloco mostra `+10 kg (+14,29 %)` e as linhas por exercício mostram as
   cargas prescritas/executadas por exercício.
2. A tonelagem bruta do strip não muda com a feature.

## 8. Semântica dos DTOs (forma inalterada)

`WorkoutVolumeDto` / `ExerciseVolumeDto` / `WorkoutVolumeSummaryDto` e o
espelho `WorkoutVolumeResponse` da integração mantêm os campos. Os comentários
passam a descrever carga média (executada) e carga ponderada por sets
(prescrita), e o delta somado por exercício — sem renomear campos nem quebrar
o JSON.

## 9. Impacto arquitetural

| Projeto | Impacto |
| --- | --- |
| `FitTrack.Application` | `WorkoutVolumeCalculator.Calculate` e `Summarize` reescritos; comentários dos DTOs. Leitura pura, como hoje. |
| `FitTrack.Adapters.Out.Persistence` | `UserSessionQueries` continua só chamando o calculator; prescrição e séries já chegam prontas. |
| `FitTrack.UI` | Painéis em `MyWorkouts.razor`/`AthleteWorkouts.razor`; `SessionCard.razor`; formatador de carga em `DisplayText.cs` (D6). |
| `FitTrack.Domain` | Nenhum (nenhum agregado é tocado). |
| Testes | Afirmações do calculator e de fluxo de sessão reescritas. |

## 10. Plano de testes

### Unitários (`tests/FitTrack.Unit`)

- `WorkoutVolumeCalculatorTests`: nova aritmética — 5.5 como lista de casos;
  inclui média de séries, ponderada por sets com linhas múltiplas, deltas
  somados, percentual de 2 casas, `null` sem base, resumo agregando sessões
  com e sem prescrição.
- Fake `InMemoryUserSessionQueries` continua delegando ao calculator — só
  validar contra os dados que ele constrói.

### Integração (`tests/FitTrack.Integration`)

- `SessionFlowTests`: os valores `2600/2000` de volume viram os valores do
  modelo novo (carga média × status), com os dados reais do teste.
- `ListEndpointsTests`: segue validando `Volume` não-nulo quando há
  prescrição.

## 11. Riscos

- **Unidade "kg" com significado novo:** painéis e cards deixam de mostrar
  tonelagem prescrita/executada; o número fica muito menor e a leitura mental
  antiga ("quanto carreguei no total") muda. Mitigado com a linha secundária
  "média por exercício" nos painéis e no card.
- **A média esconde a progressão interna:** séries 40 → 48 → 48 têm média
  45,33, menor que o pico 48 (decisão do dono: média). A nota "(média)" nos
  painéis e no card evita interpretação errada.
- **Múltiplas linhas de prescrição por exercício** (carga crescente entre as
  linhas, caso raro): a média ponderada por sets resume uma carga "por série";
  com anilhas por lado (ex.: 20 kg de anilhas + barra) o prescrito deixaria de
  representar a intenção fracionada — avaliar com prescrições reais (D1).

## 12. Referências a alterar (checklist)

Todo o modelo novo nasce e vive nestes pontos — nenhum outro lugar calcula ou
exibe a comparação:

1. `src/FitTrack.Application/UseCases/WorkoutVolumeCalculator.cs` — reescrita
   de `Calculate` e `Summarize` para o modelo por exercício (5.3/5.4).
2. `src/FitTrack.Application/Dtos/WorkoutVolumeDto.cs` — comentários de
   semântica nos três registros.
3. `src/FitTrack.UI/Pages/MyWorkouts.razor` — painéis 3 e 4 (6.1).
4. `src/FitTrack.UI/Pages/AthleteWorkouts.razor` — painéis 3 e 4 (6.1).
5. `src/FitTrack.UI/Components/SessionCard.razor` — bloco de comparação e
   linhas por exercício (7.1).
6. `src/FitTrack.UI/Components/DisplayText.cs` — formatador de carga
   (`0.##` kg), sem converter para toneladas (D6).
7. `src/FitTrack.Adapters.Out.Persistence/Queries/UserSessionQueries.cs` —
   apenas revisão: já delega ao calculator.
8. `tests/FitTrack.Unit/Application/WorkoutVolumeCalculatorTests.cs` — novos
   casos da seção 5.5.
9. `tests/FitTrack.Integration/SessionFlowTests.cs` — novos valores de volume.
10. `tests/FitTrack.Integration/Contracts/WorkoutVolumeResponse.cs` —
    revisar comentários de semântica.
11. `docs/features/busca-paginacao-volume-avatar.md` — seções que descrevem o
    modelo de tonelagem: anotar que foram substituídos por esta feature.
12. `README.md` — se citar o comparativo de volume, alinhar a descrição.

## 13. Decisões em aberto

| ID | Decisão | Recomendação |
| --- | --- | --- |
| **D1** | Carga prescrita com várias linhas por exercício: média ponderada por sets ou média simples? | Ponderada por sets: é a carga "por série", comparável à média das séries do atleta. |
| **D2** | Séries com carga 0 (peso corporal) entram na média do exercício? | Não entram; exercício feito só com 0 é tratado como sem carga executada. |
| **D3** | `notPerformed` entra no delta (executada = 0)? | Sim, como hoje (5.2): prescrito e não feito desconta. |
| **D4** | Painel "Volume feito" mantém o rótulo com nota "(média por exercício)"? | Sim: mantém o vocabulário atual na UI; nota esclarece a unidade nova. |
| **D5** | Texto quando não há base de comparação nos painéis 3 e 4. | `—` com "sem prescrição para comparar" (painel 3) e "sem base de comparação" (painel 4). |
| **D6** | Formato: `0.##` kg nas cargas (sem virar tonelada nos painéis)? | Sim: cargas médias são valores baixos; "1,2 t" confundiria. `Volume()` continua na tonelagem bruta (Home, Hoje, strip do card). |
| **D7** | `SessionsWithoutPrescription` no resumo continua contando? | Sim, campo já exposto e usado para honestidade dos números. |
| **D8** | Home/Hoje migram também? | Não nesta fatia: são tonelagem bruta sem prescrição; segue feature à parte se desejar. |

## 14. Sugestão de próximo passo

Avaliar a arquitetura da entrega (o cálculo é leitura pura e centralizado no
calculator — nenhum agregado muda) e, aprovada, implementar nesta ordem:
calculator → testes unitários → UI (painéis, depois card) → testes de
integração → docs/referências.
