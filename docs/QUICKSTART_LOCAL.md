# Quickstart Local (Kubernetes)

Este guia descreve como rodar a solução completa localmente utilizando Docker Desktop (Kubernetes) e Kustomize.

## Pré-requisitos

1. **Docker Desktop** instalado e rodando.
2. **Kubernetes** habilitado nas configurações do Docker Desktop.
3. **kubectl** instalado (vem com Docker Desktop).
4. (Opcional) **Kustomize** instalado (ou use `kubectl -k`).

## Passo a Passo

### 1. Aplicar Manifestos

No terminal, na raiz do repositório:

```bash
# Opção 1: Usando kubectl com kustomize integrado (Recomendado)
kubectl apply -k infra/k8s/overlays/local

# Opção 2: Usando kustomize standalone
kustomize build infra/k8s/overlays/local | kubectl apply -f -
```

Isso criará o namespace `agrosolutions-local` e todos os recursos necessários (bancos de dados, RabbitMQ, serviços e CronJobs).

### 2. Aguardar Inicialização

Verifique o status dos pods:

```bash
kubectl get pods -n agrosolutions-local -w
```

Aguarde até que todos estejam com status `Running`. (A inicialização do SQL Server pode levar alguns segundos).

## URLs de Acesso (Swagger/API)

Os serviços estão expostos via **NodePort** (localhost):

| Serviço | Porta | Swagger URL |
|---------|-------|-------------|
| **Usuarios** | 30001 | [http://localhost:30001/swagger](http://localhost:30001/swagger) |
| **Propriedades** | 30002 | [http://localhost:30002/swagger](http://localhost:30002/swagger) |
| **Ingestao** | 30003 | [http://localhost:30003/swagger](http://localhost:30003/swagger) |
| **Analise** | 30004 | [http://localhost:30004/swagger](http://localhost:30004/swagger) |

> **Nota:** O Grafana (Observabilidade) pode ser acessado em [http://localhost:32000](http://localhost:32000) (login: admin/admin).

## Validar Primeiro Alerta

O sistema inclui um **Simulador** rodando como CronJob que envia dados automaticamente a cada minuto.

1. **Verificar Logs do Simulador:**
   ```bash
   # Encontre o pod do job mais recente
   kubectl get pods -n agrosolutions-local -l app=ingestao-simulador
   kubectl logs -n agrosolutions-local job/<nome-do-job>
   ```
   *Você deve ver logs de "Login realizado" e "Leitura enviada".*

2. **Verificar Processamento na Análise:**
   ```bash
   kubectl logs -n agrosolutions-local -l app=analise -f
   ```
   *Procure por logs como "Alerta gerado" ou "Regra avaliada".*

3. **Consultar API de Alertas:**
   Acesse o Swagger da Análise ([http://localhost:30004/swagger](http://localhost:30004/swagger)) e chame o endpoint `GET /api/v1/alertas` (via botão "Try it out").
