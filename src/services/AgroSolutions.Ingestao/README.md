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
O simulador realiza autenticação completa:
1. Realiza login no serviço **AgroSolutions.Usuarios** (endpoint `/login`) para obter um token JWT.
2. Utiliza esse token para enviar leituras ao serviço de Ingestão (`POST /api/v1/leituras-sensores` com header `Authorization: Bearer <token>`).

O serviço de Ingestão valida o token e as claims de acesso antes de aceitar a leitura.

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


## Modo Demo (Simulação Temporal)

O simulador foi atualizado para suportar um modo de execução interativo para demonstrações.

### Comportamento
1. Ao iniciar, o simulador aguarda o pressionamento da tecla **ENTER** (comando "Start").
2. Após o início, executa por **10 minutos**, gerando dados a cada **1 minuto**.
3. Gera dados para **todos os talhões** configurados na variável `TALHOES`.
4. Os dados gerados incluem novas métricas simuladas:
   - **Umidade do Solo**: 15% a 40%
   - **Temperatura**: 18°C a 35°C
   - **Nível de Nitrogênio**: 20 a 50 mg/kg (apenas log/meta)
   - **Status do Sensor**: 95% Ativo / 5% Falha

### Execução

```bash
dotnet run --project src/services/AgroSolutions.Ingestao/AgroSolutions.Ingestao.Simulador/AgroSolutions.Ingestao.Simulador.csproj
# Pressione ENTER quando solicitado
```

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

### 3) Executar no Kubernetes via Deployment

O Simulador agora roda como um **Deployment** contínuo, definido em:

- `infra/k8s/base/ingestao/deployment-simulador.yaml`

Ele é aplicado automaticamente quando você aplica o overlay (local/dev/prod), subindo uma réplica que gera dados constantemente.

Aplicar (exemplo local):

```bash
kubectl apply -k infra/k8s/overlays/local
```

Verificar execução:

```bash
kubectl get pods -n agrosolutions-local -l app=ingestao-simulador
kubectl logs -n agrosolutions-local -l app=ingestao-simulador -f
```

Ajustes rápidos (para demo):
- Edite o `deployment-simulador.yaml` ou use `kubectl set env` para alterar variáveis como `TALHOES`, `TOTAL_POR_TALHAO` e `INTERVALO_SECONDS` se desejar controlar o volume de dados.
- **Novidade**: Se a variável `TALHOES` não for informada (ou vazia), o simulador buscará automaticamente todos os talhões cadastrados no serviço **AgroSolutions.Propriedades** (via endpoint `/api/v1/Propriedades/admin/simulacao/talhoes`) e gerará dados para eles.

## Observabilidade

- `/metrics` (Prometheus scraping)
- `/health/live` e `/health/ready`

---

## Próximos passos / Backlog

1. [x] Adicionar autenticação JWT (integração com AgroSolutions.Usuarios)
2. [x] Adicionar **Worker de simulação** (Console App + Deployment no Kubernetes)
3. Criar views específicas para Grafana (agregações, min/max, etc.)
4. Melhorar idempotência/eventId (Outbox simplificada, se necessário)
