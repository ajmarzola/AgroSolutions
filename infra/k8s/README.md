# 🚜 Guia de Infraestrutura & Execução – AgroSolutions

Este guia centraliza todas as instruções para executar a plataforma AgroSolutions, desde o ambiente local até ambientes remotos (Dev/Prod).

---

## ⚙️ Pré-requisitos (Local)

1. **Docker Desktop para Windows** (ou Linux/Mac) instalado.
2. **Kubernetes** habilitado (`Settings > Kubernetes > Enable Kubernetes`).
3. **WSL 2** (Windows) ou terminal Bash recomendado.
4. **Git** e **Helm** instalados (Helm opcional se não usar monitoramento avançado).

> **⚠️ Importante**: Execute os comandos a partir da **raiz do repositório**.

---

## 🚀 Execução em Ambiente Local

Utilizamos o Kustomize com overlay `local` para subir a stack completa no seu Docker Desktop.

### 1. Build das Imagens

Antes de subir o cluster, construa as imagens locais para garantir que estão atualizadas.

**Bash/WSL:**
```bash
bash ./build/scripts/docker-build.sh local
```

**PowerShell:**
```powershell
./build/scripts/docker-build.ps1 -Environment local
```

### 2. Aplicar Manifestos

Suba a infraestrutura completa (RabbitMQ, SQL Server, APIs, Workers e Simulador):

**Bash/WSL:**
```bash
bash ./build/scripts/k8s-apply.sh local
```

**Alternativa Manual (funciona em qualquer shell com kubectl):**
```bash
kubectl apply -k infra/k8s/overlays/local
```

### 3. Configurar Credenciais do Simulador

O Simulador roda como um CronJob e precisa se autenticar para enviar dados. Crie ou atualize o segredo com credenciais válidas de um usuário administrador:

```bash
kubectl create secret generic simulador-auth-secret \
  --from-literal=email='admin@agrosolutions.com' \
  --from-literal=password='admin123' \
  --namespace agrosolutions-local \
  --dry-run=client -o yaml | kubectl apply -f -
```

### 4. Verificar Inicialização

Aguarde até que todos os pods estejam com status `Running`. O SQL Server pode levar alguns segundos para inicializar.

```bash
kubectl get pods -n agrosolutions-local -w
```
> **Nota:** É crucial que o `db-init-job` esteja com status `Completed`. Veja a seção de Troubleshooting se ele falhar.

### 5. Validar Execução e Primeiro Alerta

Para confirmar que o fluxo completo (Simulador -> Ingestão -> RabbitMQ -> Análise) está funcionando:

1. **Verificar envio**:
   ```bash
   # Encontre o pod do job mais recente
   kubectl get pods -n agrosolutions-local -l app=ingestao-simulador
   # Veja os logs
   kubectl logs -n agrosolutions-local job/<nome-do-job> --tail=20
   ```
   *Logs esperados: "Login realizado com sucesso", "Leitura enviada".*

2. **Verificar processamento**:
   ```bash
   kubectl logs -n agrosolutions-local -l app=analise -f
   ```
   *Logs esperados: "Regra avaliada", "Alerta gerado".*

---

## 🌐 Acesso aos Serviços (Local)

No ambiente local, os serviços são expostos via **NodePort** (acessíveis via `localhost`).

| Serviço | Porta | URL Swagger / Interface |
|---------|-------|-------------------------|
| **Usuários** | 30001 | [http://localhost:30001/swagger](http://localhost:30001/swagger) |
| **Propriedades** | 30002 | [http://localhost:30002/swagger](http://localhost:30002/swagger) |
| **Ingestão** | 30003 | [http://localhost:30003/swagger](http://localhost:30003/swagger) |
| **Análise** | 30004 | [http://localhost:30004/swagger](http://localhost:30004/swagger) |
| **RabbitMQ** | 30006 | [http://localhost:30006](http://localhost:30006) (user / password) |
| **Grafana** | 32000 | [http://localhost:32000](http://localhost:32000) (admin / admin) |

---

## 🌐 Ambientes Dev/Prod

Em ambientes remotos, a infraestrutura segue práticas de **GitOps** para garantir consistência e rastreabilidade.

### Atualização via GitOps
As atualizações de versão de imagem não são feitas manualmente (`kubectl set image`).
1. O cluster possui um agente (ex: ArgoCD ou Flux) monitorando este repositório.
2. Para atualizar, altere a tag da imagem no arquivo `kustomization.yaml` do ambiente desejado (`infra/k8s/overlays/prod` ou `dev`).
3. Commit e Push da alteração.
4. O GitOps detecta a mudança e sincroniza o cluster automaticamente.

### Gerenciamento de Segredos
Segredos (connection strings, chaves de API) **nunca** devem ser commitados no repositório.
- **Configuração**: Eles devem ser injetados via `secretGenerator` em cada overlay localmente (não versionado) ou através de soluções de cofre digital (Vault/SealedSecrets) no cluster.
- **Padrão**: O `kustomization.yaml` deve referenciar segredos que o ambiente espera que já existam os valores.

### URLs Esperadas
As rotas em Dev/Prod dependem da configuração de Ingress e DNS. Abaixo, o padrão esperado:

| Serviço | Dev (exemplo) | Prod (exemplo) |
|---------|---------------|----------------|
| **Swagger/API** | `https://api.dev.agrosolutions.internal/[servico]/swagger` | `https://api.agrosolutions.com/[servico]` |
| **Grafana** | `https://grafana.dev.agrosolutions.internal` | `https://monitor.agrosolutions.com` |
| **Jaeger** | `https://jaeger.dev.agrosolutions.internal` | *(Geralmente restrito a VPN)* |

---

## 🛠 Troubleshooting

### Verificar se o `db-init-job` rodou com sucesso

O `db-init-job` é responsável por rodar as migrations e popular o banco de dados inicialmente. Se ele falhar, as APIs retornarão erro de conexão com banco.

**Passo a passo:**

1. **Checar status do Job:**
   ```bash
   kubectl get jobs -n agrosolutions-local
   ```
   *A coluna `COMPLETIONS` deve estar `1/1`.*

2. **Verificar Logs do Pod do Job:**
   Se estiver `0/1`, descubra o motivo:
   ```bash
   kubectl logs -l job-name=db-init-job -n agrosolutions-local
   ```
   - **Erro "Login failed for user"**: Verifique se a senha na secret `db-secret` bate com a configurada no `sqlserver-deployment`.
   - **Erro "Connection Timeout/Refused"**: O SQL Server ainda não estava pronto quando o job rodou.
     - **Solução**: Delete o job falhado para que o Kubernetes (ou Kustomize) o recrie:
       ```bash
       kubectl delete job db-init-job -n agrosolutions-local
       # Em seguida, reaplique o overlay local
       kubectl apply -k infra/k8s/overlays/local
       ```

### Outros Problemas Comuns

- **Erro `ImagePullBackOff` ou `ErrImagePull`**:
  O Kubernetes não encontrou a imagem localmente. Execute o passo de **Build das Imagens** novamente e certifique-se de que o Docker Desktop está usando o contexto correto.

- **RabbitMQ inacessível**:
  Verifique se o pod do RabbitMQ está `Running` e se a porta 30006 (Management) ou 5672 (AMQP) não estão bloqueadas por firewall ou ocupadas no host.

---

## 🧹 Operações de Limpeza

Se precisar resetar o ambiente:

1. **Hard Reset** (Remove tudo, incluindo volumes/dados):
   ```bash
   kubectl delete -k infra/k8s/overlays/local
   kubectl delete namespace agrosolutions-local
   docker system prune -a --volumes # Cuidado, limpa todo o Docker
   ```

2. **Soft Reset** (Recria pods, mantendo dados):
   ```bash
   kubectl delete -k infra/k8s/overlays/local
   kubectl apply -k infra/k8s/overlays/local
   ```
