# Guia de Execução Local – AgroSolutions 🚜

Este guia detalha como executar a plataforma AgroSolutions localmente utilizando **Docker Desktop (Windows)** com Kubernetes ativado.

---

## ⚙️ Pré-requisitos

1. **Docker Desktop para Windows** instalado.
2. **Kubernetes** habilitado nas configurações do Docker Desktop (`Settings > Kubernetes > Enable Kubernetes`).
3. **WSL 2** configurado (recomendado) ou Terminal com suporte a Bash (Git Bash).
4. **Git** instalado.
5. **Helm** instalado (somente se for usar o stack de monitoramento).

> **⚠️ Importante**: Execute os comandos a partir da **raiz do repositório**.
> No Windows (PowerShell), prefixe scripts `.sh` com `bash` se necessário, ou use o WSL.

---

## ✅ Serviços contemplados

- AgroSolutions.Analise.WebApi
- AgroSolutions.Ingestao.WebApi
- AgroSolutions.Propriedades.WebApi
- AgroSolutions.Usuarios.WebApi
- AgroSolutions.Ingestao.Simulador (CronJob)

---

## 🚀 Cenário A — Ambiente Novo (Instalação do zero)

1) **(Opcional) Instalar stack de monitoramento**:

```bash
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm repo update
helm install kps prometheus-community/kube-prometheus-stack --namespace agrosolutions-observability --create-namespace
```

2) **Build das imagens (inclui Simulador)**:

**Bash/WSL**
```bash
bash ./build/scripts/docker-build.sh local
```

**PowerShell**
```powershell
./build/scripts/docker-build.ps1 -Environment local
```

3) **Aplicar o K8s local**:

```bash
bash ./build/scripts/k8s-apply.sh local
```

4) **Verificar recursos**:

```bash
kubectl get pods -n agrosolutions-local
kubectl get deployments -n agrosolutions-local
kubectl get cronjobs -n agrosolutions-local
```

> Aguarde todos os pods ficarem `Running`. O simulador roda via CronJob a cada 5 minutos.

> **Observação (local):** no overlay local, o serviço de Ingestão usa repositório em memória e RabbitMQ desabilitado para facilitar o bootstrap. Por isso o deployment de Ingestão roda com 1 réplica.

---

## 🚀 Cenário B — Ambiente Existente (dia a dia)

1) **Checar status**:
```bash
kubectl get pods -n agrosolutions-local
```

2) **Reaplicar manifests (se necessário)**:
```bash
kubectl apply -k infra/k8s/overlays/local
```

3) **Reiniciar deployments (quando precisa refletir mudanças de imagem)**:
```bash
kubectl rollout restart deployment -n agrosolutions-local
```

---

## 🚀 Cenário C — Reset de Ambiente (sem apagar volumes persistentes)

1) **Remover recursos da stack**:
```bash
kubectl delete -k infra/k8s/overlays/local
```

2) **Aplicar novamente**:
```bash
kubectl apply -k infra/k8s/overlays/local
```

> Esse reset não remove volumes persistentes (caso existam). Ele apenas recria workloads e services.

---

## 🚀 Cenário D — Hard Reset (apagar containers, imagens e volumes)

1) **Remover recursos do cluster**:
```bash
kubectl delete -k infra/k8s/overlays/local
kubectl delete namespace agrosolutions-local --ignore-not-found
```

2) **Limpar Docker Desktop** (cuidado: remove imagens/volumes locais):

```bash
docker system prune -a --volumes
```

3) **Recriar tudo**: volte ao **Cenário A**.

---

## 🌐 Acesso aos serviços (port-forward)

| Serviço | Porta Interna | Comando (Exemplo) | URL |
|---------|---------------|-------------------|-----|
| **Usuários** | 80 | `kubectl port-forward svc/usuarios 8081:80 -n agrosolutions-local` | [http://localhost:8081/swagger](http://localhost:8081/swagger) |
| **Propriedades** | 80 | `kubectl port-forward svc/propriedades 8082:80 -n agrosolutions-local` | [http://localhost:8082/swagger](http://localhost:8082/swagger) |
| **Ingestão** | 80 | `kubectl port-forward svc/ingestao 8083:80 -n agrosolutions-local` | [http://localhost:8083/swagger](http://localhost:8083/swagger) |
| **Análise** | 80 | `kubectl port-forward svc/analise 8084:80 -n agrosolutions-local` | [http://localhost:8084/swagger](http://localhost:8084/swagger) |
| **Grafana** | 80 (ou 3000) | Consultar documentação específica | [Ver Docs Grafana](../observability/grafana/README.md) |

---

## 🤖 Simulador (execução manual)

Para forçar uma execução fora do agendamento:

```bash
kubectl create job --from=cronjob/ingestao-simulador simulador-manual -n agrosolutions-local
```

Ver logs do job:

```bash
kubectl logs job/simulador-manual -n agrosolutions-local
```

> O simulador aceita `TALHOES` como GUIDs ou números. Números são convertidos para GUIDs determinísticos.
> Para customizar o ID da propriedade, ajuste `ID_PROPRIEDADE` no ConfigMap.

---

## ℹ️ Troubleshooting comum

- **Erro `CreateContainerConfigError`**: o K8s não encontrou a imagem. Refaça o build das imagens.
- **Scripts `.sh` falhando no Windows**: use `bash` ou WSL. Verifique se os arquivos estão com LF.
