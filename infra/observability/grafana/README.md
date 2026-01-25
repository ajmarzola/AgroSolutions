# Observabilidade – Grafana (AgroSolutions)

Este diretório contém a documentação para utilização do **Grafana** como ferramenta
de visualização das métricas coletadas pelo Prometheus.

---

## 🧩 Stack Utilizada

- **Prometheus Operator (kube-prometheus-stack)**
- **Prometheus**
- **Grafana**
- **ServiceMonitor (Kubernetes CRD)**

---

## ✅ Pré-requisitos

Antes de prosseguir, é necessário ter o stack de observabilidade instalado no cluster.

Exemplo via Helm:

```bash
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm repo update

helm install kps prometheus-community/kube-prometheus-stack   --namespace agrosolutions-observability   --create-namespace
```

> O label `release: kps` é utilizado pelos ServiceMonitors do projeto.

---

## 📡 Integração com os Serviços

Os microsserviços expõem métricas via endpoint:

```
/metrics
```

Cada Service Kubernetes recebe o label:

```yaml
monitoring: enabled
```

Os ServiceMonitors selecionam automaticamente esses serviços.

---

## 📊 Dashboards

Após acessar o Grafana (porta padrão 3000):

- Importar dashboards customizados (JSON)
- Utilizar o datasource Prometheus configurado automaticamente

Credenciais padrão (local):
- Usuário: `admin`
- Senha: `prom-operator`

---

## 🔍 Validação

Verificar se os targets estão ativos:

- Grafana → Explore → Prometheus
- Prometheus UI → Status → Targets

---

## ℹ️ Observações

- Esta configuração é voltada para **ambiente local**
- Para produção, recomenda-se:
  - Autenticação no Grafana
  - Persistência de dados
  - TLS e RBAC refinado
