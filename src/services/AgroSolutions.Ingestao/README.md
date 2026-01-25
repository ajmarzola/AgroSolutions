# AgroSolutions.Ingestao

Microsserviço responsável por **receber e persistir leituras de sensores de campo (simuladas)** — umidade do solo, temperatura e precipitação — associadas a um **talhão**.  
O objetivo do serviço é alimentar **dashboards no Grafana** e publicar eventos para o serviço **AgroSolutions.Analise** processar regras, métricas e alertas.

---

## Objetivos do desenho (por que esta implementação)

### Grafana como protagonista
Para a demonstração (vídeo/entrega), o dashboard do Grafana é o foco. Por isso, o serviço prioriza:

- **consultas por intervalo de tempo** (série temporal)
- **agregação por janelas** (ex.: 5 minutos) para reduzir o número de pontos no painel
- **estrutura simples** para evolução incremental

### Banco de dados: SQL Server
Escolhemos **SQL Server** como armazenamento das leituras por:

- **baixa fricção** para o time (SQL já é conhecido)
- datasource do Grafana para SQL Server é estável e atende bem o MVP
- permite evoluir para particionamento/otimizações sem trocar todo o stack

Script de criação da tabela e índices:

- `AgroSolutions.Ingestao.WebApi/Database/sqlserver/001_create_table_sensor_leitura.sql`

### Mensageria: RabbitMQ
Escolhemos **RabbitMQ** para desacoplar ingestão e análise:

- ingestão recebe/persiste rapidamente
- publica evento `LeituraSensorRecebida` em exchange topic
- o serviço de análise consome e executa regras/alertas

Configuração (appsettings):
- `RabbitMq.Exchange` (default: `agrosolutions`)
- `RabbitMq.RoutingKeyLeituraRecebida` (default: `ingestao.leitura_sensor_recebida`)

### Autenticação/JWT
A autenticação (JWT) será adicionada posteriormente pelo serviço **AgroSolutions.Usuarios**.  
Nesta etapa, o Ingestão está preparado para incorporar `[Authorize]` e validar claims/scopes, sem mudar o contrato dos endpoints.

---

## Endpoints

### POST `/api/v1/leituras-sensores`
Recebe uma leitura de sensor (simulada).

- JSON com **propriedades em português** (Contrato do time)
- validação: ao menos uma métrica deve ser informada

Exemplo de payload: ver `AgroSolutions.Ingestao.WebApi.http`.

### GET `/api/v1/leituras-sensores`
Consulta leituras por talhão e intervalo (UTC).

Query params:
- `idTalhao` (Guid)
- `deUtc` / `ateUtc` (DateTime em UTC)
- `agruparMinutos` (opcional): quando informado, retorna **série agregada** por bucket.

---


## Testes (simulador de dados + CronJob)

Para facilitar os testes do fluxo **Ingestão → SQL Server → Grafana** (e também a publicação no RabbitMQ), disponibilizamos um **Console App** que gera leituras aleatórias e envia para a API.

### 1) Executar o simulador localmente (dotnet run)

Pré-requisito: API de ingestão acessível (por exemplo, via `kubectl port-forward` ou Docker).

Exemplo com API em `http://localhost:8080`:

```bash
dotnet run --project src/services/AgroSolutions.Ingestao/AgroSolutions.Ingestao.Simulador/AgroSolutions.Ingestao.Simulador.csproj -- \
  --baseUrl=http://localhost:8080 \
  --talhoes=1,2,3 \
  --intervalo=2 \
  --total=10
```

Variáveis de ambiente suportadas (alternativa aos argumentos):
- `INGESTAO_BASE_URL` (default: `http://localhost:8080`)
- `TALHOES` (default: `1`)
- `INTERVALO_SECONDS` (default: `5`)
- `TOTAL_POR_TALHAO` (default: `12`)
- `FONTE` (default: `simulador`)
- `ID_DISPOSITIVO` (default: `SIM-001`)
- `BEARER_TOKEN` (opcional; quando JWT estiver ativo)

Exemplo:

```bash
set INGESTAO_BASE_URL=http://localhost:8080
set TALHOES=1,2,3
set INTERVALO_SECONDS=2
set TOTAL_POR_TALHAO=10
dotnet run --project src/services/AgroSolutions.Ingestao/AgroSolutions.Ingestao.Simulador/AgroSolutions.Ingestao.Simulador.csproj
```

### 2) Executar o simulador como container (Docker)

Build:

```bash
./build/scripts/docker-build.sh local ghcr.io/agrosolutions local
```

(ou no Windows PowerShell)

```powershell
./build/scripts/docker-build.ps1 -Environment local -Registry ghcr.io/agrosolutions -Tag local
```

Run (exemplo):

```bash
docker run --rm \
  -e INGESTAO_BASE_URL=http://host.docker.internal:8080 \
  -e TALHOES=1,2,3 \
  -e INTERVALO_SECONDS=2 \
  -e TOTAL_POR_TALHAO=10 \
  ghcr.io/agrosolutions/ingestao-simulador:local
```

### 3) Executar no Kubernetes via CronJob

O CronJob está definido em:

- `infra/k8s/base/ingestao/cronjob-simulador.yaml`

Ele é aplicado automaticamente quando você aplica o overlay (local/dev/prod), pois está referenciado no `kustomization.yaml` da base de ingestão.

Aplicar (exemplo local):

```bash
kubectl apply -k infra/k8s/overlays/local
```

Verificar execuções:

```bash
kubectl get cronjob -n agrosolutions-local
kubectl get jobs -n agrosolutions-local
kubectl get pods -n agrosolutions-local -l app.kubernetes.io/name=ingestao-simulador
kubectl logs -n agrosolutions-local job/<nome-do-job>
```

Ajustes rápidos (para demo):
- `spec.schedule` no CronJob (ex.: `*/1 * * * *` para rodar a cada 1 minuto)
- `TALHOES`, `TOTAL_POR_TALHAO` e `INTERVALO_SECONDS` no manifest para controlar volume de pontos no Grafana.

## Observabilidade

- `/metrics` (Prometheus scraping)
- `/health/live` e `/health/ready`

---

## Próximos passos (planejados)

1. Adicionar autenticação JWT (integração com AgroSolutions.Usuarios)
2. Adicionar **Worker de simulação** (Console App + CronJob no Kubernetes)
3. Melhorar idempotência/eventId (Outbox simplificada, se necessário)
4. Criar views específicas para Grafana (agregações, min/max, etc.)
