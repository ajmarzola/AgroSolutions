# Observabilidade – Grafana (AgroSolutions)

Este diretório documenta como utilizar o **Grafana** para visualizar as métricas coletadas no cluster local via **Prometheus** e acompanhar a execução dos microsserviços da plataforma.

> Dashboard versionado no repositório:  
> `infra/observability/grafana/dashboards/agrosolutions-apis-prometheus.json`

---

## 1) Pré-requisitos

Antes de importar dashboards, é necessário que:

1. O ambiente Kubernetes local esteja rodando (Docker Desktop) e os serviços do projeto estejam aplicados:
   - Tutorial: `infra/k8s/README.md`
   - Namespace: `agrosolutions-local`

2. O stack de observabilidade esteja instalado no cluster (**kube-prometheus-stack**), pois ele provê:
   - Prometheus
   - Grafana
   - CRDs (ex.: `ServiceMonitor`)

### 1.1 Instalação do kube-prometheus-stack (Helm)

> Se você já tem o stack instalado, pule para a seção 2.

```powershell
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm repo update

helm install kps prometheus-community/kube-prometheus-stack `
  --namespace agrosolutions-observability `
  --create-namespace
```

**Importante:** os manifests de ServiceMonitor do repositório assumem o label:

- `release: kps`

---

## 2) Garantir que os ServiceMonitors do projeto foram aplicados

No overlay `local`, os manifests de monitoramento já são incluídos:

- `infra/k8s/overlays/local/monitoring/servicemonitors.yaml`
- Patch de labels em Services: `service-labels.patch.yaml`

Valide se existem ServiceMonitors no cluster:

```powershell
kubectl get servicemonitors -n agrosolutions-local
```

Se não aparecer nada, reaplique o overlay local:

```powershell
kubectl apply -k infra/k8s/overlays/local
```

---

## 3) Acessar o Grafana (port-forward)

O Grafana roda no namespace do stack (por padrão, `agrosolutions-observability`).

1) Descobrir o Service do Grafana:

```powershell
kubectl get svc -n agrosolutions-observability | findstr grafana
```

Normalmente o service é algo como `kps-grafana` (o nome pode variar conforme o release).

2) Subir port-forward:

```powershell
kubectl port-forward svc/kps-grafana 3000:80 -n agrosolutions-observability
```

3) Abrir no navegador:

- `http://localhost:3000`

### 3.1 Credenciais (kube-prometheus-stack)

O chart cria um Secret com a senha do admin.

1) Localizar o Secret (nome pode variar, mas costuma conter `grafana`):

```powershell
kubectl get secret -n agrosolutions-observability | findstr grafana
```

2) Ler a senha (exemplo; ajuste o nome do secret conforme o retorno do comando anterior):

```powershell
kubectl get secret kps-grafana -n agrosolutions-observability -o jsonpath="{.data.admin-password}" | % { [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($_)) }
```

Usuário padrão: **admin**.

---

## 4) Garantir a fonte de dados (Prometheus)

No Grafana:

1. Acesse **Connections → Data sources**
2. Verifique se existe um datasource do tipo **Prometheus**
3. Caso não exista, crie um novo datasource Prometheus apontando para o service do Prometheus no namespace `agrosolutions-observability`.

> Dica: no kube-prometheus-stack, normalmente o Prometheus fica exposto como service com nome semelhante a `kps-kube-prometheus-stack-prometheus`.  
> Você pode descobrir com:

```powershell
kubectl get svc -n agrosolutions-observability | findstr prometheus
```

---

## 5) Importar o dashboard do repositório

O dashboard oficial do projeto está em:

- `infra/observability/grafana/dashboards/agrosolutions-apis-prometheus.json`

### 5.1 Importação via UI do Grafana (recomendado)

1) No Grafana, clique em **Dashboards → New → Import**
2) Clique em **Upload JSON file**
3) Selecione o arquivo:
   - `infra/observability/grafana/dashboards/agrosolutions-apis-prometheus.json`
4) No campo **Data source**, selecione o datasource do **Prometheus**
5) Clique em **Import**

### 5.2 Validação rápida do dashboard

- Se os gráficos estiverem vazios, verifique:
  1) Se os pods estão rodando: `kubectl get pods -n agrosolutions-local`
  2) Se o Prometheus está “enxergando” os targets:
     - No Grafana (ou diretamente no Prometheus), verifique **Targets / Service Discovery**
  3) Se os ServiceMonitors existem: `kubectl get servicemonitors -n agrosolutions-local`

---

## 6) Acompanhar as execuções (roteiro de teste)

Para ver “vida” nos gráficos:

1) Suba os serviços localmente (tutorial Kubernetes).
2) Faça port-forward de pelo menos um serviço e acesse o Swagger (isso gera tráfego):

```powershell
kubectl port-forward svc/usuarios 8080:80 -n agrosolutions-local
```

3) No Swagger, execute algumas requisições (ex.: endpoints de health e endpoints de negócio).
4) Volte ao Grafana e ajuste o *time range* para **Last 5 minutes**.

---

## 7) Problemas comuns

### 7.1 “Failed to fetch” ao importar JSON

Causas típicas:
- o port-forward do Grafana caiu
- bloqueio por proxy/VPN corporativa
- sessão expirada

Ações:
- confirme `http://localhost:3000` respondendo
- refaça o port-forward
- tente importar novamente

### 7.2 Gráficos vazios

- Confirme se os **targets** estão UP no Prometheus
- Confirme se os Services do projeto têm labels esperadas (patch `service-labels.patch.yaml`)
- Confirme se os endpoints de métricas estão expostos pelos serviços (conforme instrumentação OpenTelemetry/Prometheus)

---

## 8) Arquivos importantes neste diretório

```
infra/observability/grafana/
├─ dashboards/
│  └─ agrosolutions-apis-prometheus.json
└─ README.md
```
