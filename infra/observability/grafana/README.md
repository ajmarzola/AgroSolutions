# Observabilidade – Monitoramento com Grafana 📊

Este guia orienta como acessar o Grafana, importar os dashboards e visualizar as métricas da aplicação AgroSolutions.

---

## ✅ Pré-requisitos

1. **Stack de Observabilidade Instalada**:
   Se você seguiu o [Guia de Execução Kubernetes](../../k8s/README.md), o Prometheus e o Grafana já devem estar instalados via Helm.
   
   Caso contrário, instale agora:
   ```bash
   helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
   helm repo update
   helm install kps prometheus-community/kube-prometheus-stack --namespace agrosolutions-observability --create-namespace
   ```

2. **Serviços Rodando**:
   A aplicação deve estar rodando no namespace `agrosolutions-local` para gerar métricas.

---

## 🚀 Acessando o Grafana

Para acessar o painel, precisamos redirecionar a porta do serviço Kubernetes para sua máquina local.

1. **Realizar Port-Forward**:
   Execute o seguinte comando no terminal:
   ```bash
   # O nome do serviço geralmente é 'kps-grafana' (dado o release name 'kps')
   kubectl port-forward svc/kps-grafana 3000:80 -n agrosolutions-observability
   ```
   *(Mantenha este terminal aberto enquanto usa o Grafana)*

2. **Fazer Login**:
   - Abra o navegador em: [http://localhost:3000](http://localhost:3000)
   - **Usuário**: `admin`
   - **Senha**: `prom-operator`

---

## 📈 Configurando Dashboards

O projeto já possui dashboards pré-configurados. Siga os passos para importá-los:

1. **Localizar os Arquivos JSON**:
   Os arquivos de dashboard estão na pasta:
   `infra/observability/grafana/dashboards/`
   
   - Exemplo: `agrosolutions-apis-prometheus.json`

2. **Importar no Grafana**:
   - No menu lateral esquerdo, clique em **Dashboards** (ícone de quatro quadrados) -> **New** -> **Import**.
   - Clique em **"Upload dashboard JSON file"**.
   - Navegue até a pasta `infra/observability/grafana/dashboards/` no seu repositório clonado.
   - Selecione o arquivo `.json`.
   - Selecione o **DataSource** (geralmente `Prometheus` já configurado automaticamente).
   - Clique em **Import**.

---

## 👁️ Acompanhando a Execução

Após importar, você verá métricas em tempo real.

### O que observar?
- **Requisições por Segundo (RPS)**: Indica o tráfego chegando nas APIs (Ingestão, Análise, etc).
- **Latência**: Tempo de resposta dos serviços.
- **Métricas de Negócio (Simuladas)**:
  - O serviço `Ingestao.Simulador` envia dados constantemente.
  - Verifique se os contadores de "dados recebidos" aumentam no dashboard.

### Troubleshooting
- **Dashboard Vazio?**:
  - Verifique se os pods da aplicação estão rodando (`kubectl get pods -n agrosolutions-local`).
  - Verifique se o `ServiceMonitor` foi aplicado (`kubectl get servicemonitors -n agrosolutions-local`).
  - Aguarde alguns minutos para a coleta de métricas.
