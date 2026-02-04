# Guia de Execução Local – AgroSolutions 🚜

Este guia detalha como executar a plataforma AgroSolutions localmente utilizando **Docker Desktop (Windows)** com Kubernetes ativado.

---

## ⚙️ Pré-requisitos

1. **Docker Desktop para Windows** instalado.
2. **Kubernetes** habilitado nas configurações do Docker Desktop (`Settings > Kubernetes > Enable Kubernetes`).
3. **WSL 2** configurado (recomendado) ou Terminal com suporte a Bash (Git Bash).
4. **Git** instalado.
5. **Helm** instalado (necessário para o monitoramento).

> **⚠️ Importante**: Execute os comandos a partir da **raiz do repositório**!
> No Windows (PowerShell), prefixe os scripts `.sh` com `bash` se necessário, ou use o WSL.

---

## 🚀 Como Executar

Selecione o cenário abaixo que corresponde à sua situação:

### A) Ambiente Novo (Primeira Execução)
*Para quem acabou de clonar o projeto ou resetou o Docker.*

1. **Instalar Stack de Monitoramento (Prometheus Operator)**:
   ```bash
   helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
   helm repo update
   helm install kps prometheus-community/kube-prometheus-stack --namespace agrosolutions-observability --create-namespace
   ```

2. **Compilar e Gerar Imagens**:
   ```bash
   bash ./build/scripts/docker-build.sh local
   ```

3. **Deploy dos Microsserviços**:
   ```bash
   bash ./build/scripts/k8s-apply.sh local
   ```

4. **Verificar Instalação**:
   ```bash
   kubectl get pods -n agrosolutions-local
   ```
   *Aguarde todos os pods estarem com status `Running`.*

---

### B) Ambiente Já Criado (Retomar Trabalho)
*O ambiente já existe e você quer apenas conferir ou reiniciar.*

1. **Checar Status**:
   ```bash
   kubectl get pods -n agrosolutions-local
   ```

2. **Reiniciar Pods (Troubleshooting simples)**:
   Se necessário reiniciar os serviços para desbloquear algo:
   ```bash
   kubectl rollout restart deployment -n agrosolutions-local
   ```

---

### C) Ambiente Desatualizado (Alterações de Código)
*Você alterou o código C# e precisa testar.*

1. **Rebuild das Imagens**:
   Isso atualizará a tag `:local` no seu Docker registry local.
   ```bash
   bash ./build/scripts/docker-build.sh local
   ```

2. **Atualizar K8s (Se mudou YAMLs)**:
   ```bash
   bash ./build/scripts/k8s-apply.sh local
   ```

3. **Forçar Update nos Pods**:
   Como a tag da imagem não mudou (sempre `:local`), é preciso reiniciar os pods para pegarem o novo binário:
   ```bash
   kubectl rollout restart deployment -n agrosolutions-local
   ```

---

## 🧹 Limpeza (Reset)

Para remover os microsserviços do cluster:
```bash
kubectl delete -k infra/k8s/overlays/local
```

---

## 🌐 Acesso aos Serviços

Utilize `port-forward` para expor as portas para seu `localhost`.

| Serviço | Porta Interna | Comando (Exemplo) | URL |
|---------|---------------|-------------------|-----|
| **Usuários** | 80 | `kubectl port-forward svc/usuarios 8081:80 -n agrosolutions-local` | [http://localhost:8081/swagger](http://localhost:8081/swagger) |
| **Propriedades** | 80 | `kubectl port-forward svc/ingestao 8082:80 -n agrosolutions-local` | [http://localhost:8082/swagger](http://localhost:8082/swagger) |
| **Ingestão** | 80 | `kubectl port-forward svc/ingestao 8083:80 -n agrosolutions-local` | [http://localhost:8083/swagger](http://localhost:8083/swagger) |
| **Ingestão** | 80 | `kubectl port-forward svc/ingestao 8084:80 -n agrosolutions-local` | [http://localhost:8084/swagger](http://localhost:8084/swagger) |
| **Grafana** | 80 (ou 3000) | Consultar documentação específica | [Ver Docs Grafana](../observability/grafana/README.md) |

---

## ℹ️ Troubleshooting Comum

- **Erro `CreateContainerConfigError`**: Ocorre se o K8s não encontrar a imagem. Certifique-se de ter rodado o passo de build.
- **Scripts `.sh` falhando no Windows**: Certifique-se de usar `bash` ou WSL. Check se o arquivo tem quebras de linha padrão Unix (LF).
