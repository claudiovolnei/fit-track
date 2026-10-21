# Deploy free: Render (API) + Neon (PostgreSQL) + Cloudflare (front)

O Fly.io encerrou o uso gratuito. A API passa a rodar no **Render** (plano free, Docker) e o banco
no **Neon** (plano free). O front continua no Cloudflare Workers, sem mudança de hospedagem.

## O que muda e o que não muda

| Item | Antes | Depois |
| --- | --- | --- |
| API | Fly.io `fittrack-api-claudiovolnei` (gru) | Render `fittrack-api-claudiovolnei` (Ohio), `render.yaml` |
| Banco | Segredo do Fly | Neon free (0,5 GB), connection string no Render |
| Front | Cloudflare Workers | Igual; muda só a variável `API_BASE_URL` |
| Disco `/data` (DataProtection) | Volume do Fly | **Não existe** no free: chaves em memória |
| Migrações | `Database__MigrateOnStartup=true` | Igual |
| Porta | 8080 | 10000 (`ASPNETCORE_URLS`, definido no `render.yaml`) |

O Render free dorme após 15 minutos sem tráfego e acorda em ~1 minuto. Os avatares já ficam no
PostgreSQL, então o filesystem efêmero não perde dado. As chaves de DataProtection só afetam tokens
do Identity; o reset de senha do admin gera e consome o token na mesma requisição, então a perda é
inócua.

## 1. Neon

1. Crie um projeto no Neon na região **AWS us-east-1** (a mais próxima de Ohio) e um banco
   `fittrack`.
2. Copie a connection string **pooled** (host com `-pooler`) e acrescente `SSL Mode=Require`.
   Para o EF, use um pool pequeno:

   ```text
   Host=ep-xxxx-pooler.us-east-1.aws.neon.tech;Database=fittrack;Username=neondb_owner;Password=...;SSL Mode=Require;Maximum Pool Size=10
   ```

3. Para o `pg_restore`, use a connection string **direta** (sem `-pooler`).

## 2. Migrar os dados

Descubra primeiro se o banco atual é um Postgres do Fly ou um serviço externo (Neon, Supabase...).
`fly secrets list` mostra apenas os nomes dos segredos.

```powershell
fly secrets list --app fittrack-api-claudiovolnei
```

Se o banco já for externo, pule para o passo 3 e mantenha a mesma connection string.

Se for um Postgres do Fly (ou outro banco a ser aposentado), congele as escritas antes do dump:

```powershell
# requer aprovação; reversível com "fly scale count 1"
fly scale count 0 --app fittrack-api-claudiovolnei

$env:OLD_DB = "postgresql://usuario:senha@host:5432/fittrack"
$env:NEW_DB = "postgresql://usuario:senha@ep-xxxx.us-east-1.aws.neon.tech/fittrack?sslmode=require"

pg_dump --no-owner --no-privileges --format=custom --file=fittrack.dump --dbname=$env:OLD_DB
pg_restore --no-owner --no-privileges --dbname=$env:NEW_DB fittrack.dump
```

`pg_dump`/`pg_restore` precisam vir do PostgreSQL client (no Windows:
`winget install PostgreSQL.PostgreSQL`). Use uma versão igual ou mais nova que a do servidor de
origem. Se o banco antigo for um app Postgres do Fly, exponha-o antes com
`fly proxy 15432:5432 --app <app-do-banco>` e use `localhost:15432` como host. Guarde o
`fittrack.dump` até validar o corte.

## 3. Render

O `render.yaml` na raiz descreve o serviço. No dashboard: **New > Blueprint**, selecione o
repositório e aplique. Ou crie manualmente um **Web Service** com:

- Runtime: Docker, Dockerfile na raiz
- Plan: Free, Region: Ohio
- Health check: `/healthz`
- Branch: `main`, Auto-Deploy: sim

Variáveis de ambiente (as duas primeiras são segredos, marcadas `sync: false` no blueprint):

| Variável | Valor |
| --- | --- |
| `ConnectionStrings__fittrack` | connection string pooled do Neon |
| `Jwt__SigningKey` | 32+ caracteres aleatórios |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ASPNETCORE_URLS` | `http://+:10000` |
| `Database__MigrateOnStartup` | `true` |
| `Cors__AllowedOrigins__0` | `https://fit-track.claudiovolneisilva.workers.dev` |
| `Cors__AllowedOrigins__1` | `https://fit-track-treinos.com.br` |
| `Cors__AllowedOrigins__2` | `https://www.fit-track-treinos.com.br` |

Gere a chave:

```powershell
[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
```

Reutilizar a chave do Fly evita logoff no corte; como o access token dura 30 minutos, gerar uma nova
apenas força um novo login em até meia hora.

Não defina `DataProtection__KeysPath`: no free não há disco persistente.

Após o deploy, valide:

```powershell
curl https://fittrack-api-claudiovolnei.onrender.com/healthz
```

### Domínio próprio da API (opcional, grátis)

O Render free suporta domínio próprio com TLS gerenciado. Em **Settings > Custom Domains**, adicione
`api.fit-track-treinos.com.br` e crie no Cloudflare o CNAME para
`fittrack-api-claudiovolnei.onrender.com` com o proxy **desligado** (DNS only), para o Render emitir o
certificado. Depois use `https://api.fit-track-treinos.com.br` como `API_BASE_URL`.

## 4. Front no Cloudflare

Em **Workers > fit-track > Settings > Build > Build variables**, defina:

```text
API_BASE_URL = https://fittrack-api-claudiovolnei.onrender.com
```

(o ou o domínio próprio da API) e dispare um novo deploy. O `scripts/pages-build.sh` grava essa URL
no `appsettings.json` antes do publish, então o valor do dashboard vence o default do script.

## 5. Validação do corte

1. `GET /healthz` responde 200 (a primeira chamada pode levar ~60 s se o serviço estiver dormindo).
2. Login com uma conta real; `GET /athletes` e `GET /workout-series/mine` respondem.
3. Criar uma série, abrir o editor e iniciar um treino em `/today`.
4. Confirmar no Neon que as tabelas e os dados chegaram (`__EFMigrationsHistory` incluída).
5. Conferir os logs do Render por erro de conexão/SSL.

## 6. Rollback

O app do Fly continua existindo (parado) e o dump está guardado:

```powershell
fly scale count 1 --app fittrack-api-claudiovolnei
```

e reverta `API_BASE_URL` no Cloudflare para a URL do Fly. Faça isso somente se o Neon/Render falhar.

## 7. Limites e cuidados do free

- **Sleep:** 15 min ociosos → ~60 s para acordar. Para um app de treino, a primeira ação depois de
  uma pausa pode demorar. Se incomodar, um ping externo (UptimeRobot, a cada 10 min) mantém o
  serviço acordado; 750 h/mês cobrem os até 744 h de um mês, então continua dentro do free.
- **Neon:** 0,5 GB e 100 CU-horas/mês. Os avatares ficam em `bytea`; se o banco crescer, mover os
  avatares para o R2 free (10 GB) antes de estourar.
- **Banco free do Render:** não usar. Ele expira em 30 dias.
- **Build:** cada deploy consome minutos de build; o `Dockerfile` já separa o restore do NuGet.
- **Chaves do Identity:** reinícios invalidam tokens pendentes de confirmação/reset. O fluxo atual
  não depende deles entre requisições.

## 8. Depois de validar (limpeza)

1. Remover `fly.toml` e as referências ao Fly em `.opencode/agents/producao.md` e
   `scripts/pages-build.sh` (default do `API_BASE_URL`).
2. Atualizar a tabela de alvos de produção para Render + Neon + Cloudflare.
3. Destruir o app do Fly e o Postgres antigo **manualmente**, depois de alguns dias de rollback
   disponível. Os agentes têm `destroy/delete` bloqueado por política.
