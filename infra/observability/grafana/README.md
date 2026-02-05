# Observabilidade – Monitoramento com Grafana 📊

Este guia orienta como configurar o monitoramento local das 4 WebAPIs da AgroSolutions via Prometheus + Grafana (stack kube‑prometheus‑stack), além de validar se as métricas estão chegando corretamente.

---

## ✅ Pré‑requisitos

1) **Stack de Observabilidade instalada (Prometheus + Grafana)**

Se você seguiu o [Guia de Execução Kubernetes](../../k8s/README.md), o stack já deve estar instalado. Caso contrário:

```bash
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm repo update
helm install kps prometheus-community/kube-prometheus-stack --namespace agrosolutions-observability --create-namespace
```

2) **APIs rodando no namespace local**

As 4 APIs devem estar em execução no namespace `agrosolutions-local`:

```bash
kubectl get pods -n agrosolutions-local
```

---

## ✅ Verificação de exposição de métricas

As APIs expõem métricas em `/metrics` (OpenTelemetry/Prometheus). Para validar rapidamente:

```bash
kubectl port-forward svc/ingestao 8083:80 -n agrosolutions-local
```

Em outra janela:

```bash
curl http://localhost:8083/metrics
```

Repita para **analise**, **propriedades** e **usuarios** (alterando a porta local). Se houver saída de métricas, o endpoint está OK.

---

## 🚀 Cenário A — Ambiente Novo (subir o stack do zero)

1) **Instale o stack de observabilidade** (se ainda não estiver):

```bash
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm repo update
helm install kps prometheus-community/kube-prometheus-stack --namespace agrosolutions-observability --create-namespace
```

2) **Aplique os ServiceMonitors** (já incluídos no overlay local):

```bash
kubectl apply -k infra/k8s/overlays/local
```

3) **Acesse o Grafana**:

```bash
kubectl port-forward svc/kps-grafana 3000:80 -n agrosolutions-observability
```

URL: [http://localhost:3000](http://localhost:3000)

Credenciais padrão:
- Usuário: `admin`
- Senha: `prom-operator`

4) **Importe o dashboard padrão**:

- Arquivo: `infra/observability/grafana/dashboards/agrosolutions-apis-prometheus.json`
- Grafana → **Dashboards** → **New** → **Import** → **Upload dashboard JSON file**
- Selecione o DataSource `Prometheus` e clique em **Import**.

---

## 🚀 Cenário B — Ambiente Existente (acesso e validação)

1) **Acesse o Grafana**:

```bash
kubectl port-forward svc/kps-grafana 3000:80 -n agrosolutions-observability
```

2) **Verifique se as 4 APIs estão sendo trackeadas**:

- No Grafana, abra o dashboard **AgroSolutions — APIs (Prometheus)**.
- No painel **UP (Targets)**, devem aparecer 4 séries: `analise`, `ingestao`, `propriedades`, `usuarios`.

3) **Validação via Prometheus (opcional)**:

```bash
kubectl port-forward svc/kps-kube-prometheus-stack-prometheus 9090:9090 -n agrosolutions-observability
```

Abra [http://localhost:9090](http://localhost:9090) e execute a query:

```
up{namespace="agrosolutions-local"}
```

Se vierem 4 targets com valor `1`, o scrape está OK.

---

## 🚀 Cenário C — Reset de métricas (sem perder dashboards)

Use quando quiser “limpar” a série de métricas coletadas sem apagar configurações do Grafana.

1) **Reiniciar Prometheus e Grafana** (limpa caches e reabre conexões):

```bash
kubectl rollout restart deployment/kps-grafana -n agrosolutions-observability
kubectl rollout restart statefulset/kps-kube-prometheus-stack-prometheus -n agrosolutions-observability
```

2) **Limpar dados do Prometheus (opcional)** — mantém dashboards do Grafana:

```bash
kubectl delete pvc -n agrosolutions-observability -l app.kubernetes.io/name=prometheus
kubectl rollout restart statefulset/kps-kube-prometheus-stack-prometheus -n agrosolutions-observability
```

> Isso zera o histórico de métricas, mas preserva os dashboards (Grafana tem PVC separado).

---

## 🚀 Cenário D — Hard Reset (remover e recriar todo o stack)

1) **Remover o stack de observabilidade**:

```bash
helm uninstall kps -n agrosolutions-observability
```

2) **Remover PVCs de Grafana e Prometheus**:

```bash
kubectl delete pvc -n agrosolutions-observability --all
```

3) **Recriar tudo**: volte ao **Cenário A**.

---

## ✅ Dependências e pontos de verificação

- **Prometheus** (kube‑prometheus‑stack) deve estar ativo no namespace `agrosolutions-observability`.
- **ServiceMonitor** aplicado no cluster:

```bash
kubectl get servicemonitors -n agrosolutions-observability
```

- **URLs locais principais**:
  - Grafana: [http://localhost:3000](http://localhost:3000)
  - Prometheus: [http://localhost:9090](http://localhost:9090)

---

## ℹ️ Troubleshooting rápido

- **Dashboard vazio**: verifique se as APIs estão rodando e expondo `/metrics`.
- **Targets DOWN**: confirme o label `monitoring: enabled` nos Services e o ServiceMonitor aplicado.
- **Sem dados de latência/RPS**: gere tráfego nas APIs (ex.: simulador ou chamadas via Swagger).
