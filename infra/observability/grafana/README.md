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

**Via NodePort:**

- **Ingestão**: [http://localhost:30003/metrics](http://localhost:30003/metrics)
- **Análise**: [http://localhost:30004/metrics](http://localhost:30004/metrics)
- **Propriedades**: [http://localhost:30002/metrics](http://localhost:30002/metrics)
- **Usuários**: [http://localhost:30001/metrics](http://localhost:30001/metrics)

Se houver saída de métricas, o endpoint está OK.

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
### Monitoramento de Negócio (Talhões)

O dashboard **Monitoramento de Talhões** (`infra/k8s/base/observability/grafana/dashboards/monitoramento-talhoes.json`) exibe métricas de negócio cruzando informações:
- **Umidade do Solo**: Dados de telemetria.
- **Status**: Classificação de risco.
- **Propriedade e Dono**: Cruzamento de dados entre bancos `AgroSolutionsPropriedades` e `AgroSolutionsUsuarios`.

Este dashboard utiliza o DataSource `SQLServer` e realiza queries cross-database.
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

---

## ?? Rastreamento Distribu�do (Distributed Tracing)

O rastreamento distribu�do permite acompanhar o ciclo de vida de uma requisi��o que perpassa diversos servi�os. Utilizamos **OpenTelemetry** para instrumenta��o e **Jaeger** para visualiza��o.

### 1. Acessando a Interface do Jaeger

O Jaeger UI � o local onde voc� pode visualizar os traces.

Para acessar localmente, fa�a o port-forward do servi�o do Jaeger (que roda no namespace grosolutions-local):

\\\ash
kubectl port-forward svc/jaeger-collector 16686:16686 -n agrosolutions-local
\\\

Em seguida, acesse no navegador: **[http://localhost:16686](http://localhost:16686)**

### 2. Gerando e Visualizando Traces

1.  **Gere Tr�fego**: Utilize o Simulador ou fa�a chamadas aos endpoints da Ingestao.WebApi.
2.  **Busque no Jaeger**:
    *   No menu esquerdo 'Service', selecione \AgroSolutions.Ingestao.WebApi\ (ou outro servi�o).
    *   Clique em **Find Traces**.
    *   Voc� ver� uma lista de requisi��es. Clique em uma para ver o detalhe.
3.  **Trace Distribu�do**: Se a requisi��o envolver m�ltiplos servi�os, voc� ver� as 'spans' de cada servi�o aninhadas, permitindo identificar gargalos de lat�ncia.

### 3. Valida��o

Certifique-se que o servi�o \jaeger\ est� rodando:

\\\ash
kubectl get pods -l app=jaeger -n agrosolutions-local
\\\

Se n�o houver tra�os, verifique se a vari�vel de ambiente \OpenTelemetry__Enabled\ est� como 'true' e se o endpoint \http://jaeger-collector:4317\ est� acess�vel pelos pods.


---

## 🛠️ Automação e Dashboard-as-Code

A infraestrutura do Grafana neste projeto foi automatizada para carregar Data Sources e Dashboards via código.

### 🔐 Credenciais de Acesso (Admin)

Caso utilize a implantação customizada (via manifestos em `infra/k8s`):
- **Usuário**: `admin`
- **Senha**: `admin`

Para recuperar as credenciais via Secret:
```bash
kubectl get secret grafana-admin-credentials -o jsonpath="{.data.admin-user}" | base64 --decode
kubectl get secret grafana-admin-credentials -o jsonpath="{.data.admin-password}" | base64 --decode
```

### ➕ Gerenciamento de Dashboards

Os dashboards são carregados automaticamente através de ConfigMaps monitorados por um Sidecar.

**Para adicionar um novo dashboard:**
1. Adicione o arquivo JSON do dashboard na pasta: `infra/k8s/base/observability/grafana/dashboards/`.
2. O Kustomize (`infra/k8s/base/observability/grafana/kustomization.yaml`) está configurado para gerar um ConfigMap com a label `grafana_dashboard: "1"` para os arquivos nesta pasta. *Nota: Se adicionar novos arquivos, lembre-se de listá-los no kustomization.yaml*.
3. Aplique a configuração: `kubectl apply -k infra/k8s/base/observability`
4. O Grafana detectará a mudança e disponibilizará o dashboard imediatamente na pasta "AgroSolutions" (sem necessidade de restart).
