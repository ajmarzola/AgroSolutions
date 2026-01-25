# Grafana Dashboards – AgroSolutions

Este diretório contém dashboards Grafana utilizados no ambiente local
(Kubernetes + Prometheus).

## Dashboards disponíveis

- `agrosolutions-apis-prometheus.json`
  - Métricas RED das APIs (Rate, Errors, Duration)
  - Saúde dos serviços
  - Uso de CPU e memória por namespace

## Como importar

1. Acesse o Grafana (http://localhost:3000)
2. Dashboards ? New ? Import
3. Cole o JSON ou selecione o arquivo
4. Escolha o datasource Prometheus
