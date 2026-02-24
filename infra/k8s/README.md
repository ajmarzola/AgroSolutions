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

### ⚠️ PASSO OBRIGATÓRIO: CONFIGURAÇÃO DE SEGREDOS

Antes de aplicar os manifestos, você **DEVE** configurar as variáveis de ambiente sensíveis.

1.  Navegue até a pasta do overlay desejado (ex: `infra/k8s/overlays/local`).
2.  Copie o arquivo `.env.example` para `.env` (ou crie um novo).
    ```bash
    cp infra/k8s/overlays/local/.env.example infra/k8s/overlays/local/.env
    ```
3.  Preencha o arquivo `.env` com seus valores.

#### Checklist de Chaves
| Chave | Descrição | Exemplo Seguro (Dev) |
|-------|-----------|----------------------|
| `Jwt__Key` | Chave para assinatura de tokens JWT (min 32 chars) | `super_secret_key_at_least_32_chars_long_12345` |
| `MSSQL_SA_PASSWORD` | Senha de admin do SQL Server | `Password123!` (Exige: maiúscula, minúscula, número, especial) |
| `RABBITMQ_DEFAULT_USER` | Usuário padrão RabbitMQ | `user` |
| `RABBITMQ_DEFAULT_PASS` | Senha padrão RabbitMQ | `password` |

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

O **Simulador** agora roda como um **Deployment contínuo** e já vem configurado para utilizar o usuário **Admin** padrão (`admin@agrosolutions.com` / `Admin123!`), que é criado automaticamente na inicialização do banco de dados.

> **Nota:** Não é mais necessário criar segredos manualmente para o simulador, a menos que você deseje alterar o usuário utilizado editando o manifesto do deployment.

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
   # Veja os logs do Deployment do Simulador
   kubectl logs -n agrosolutions-local -l app=ingestao-simulador --tail=20 -f
   ```
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
| **Grafana** | 30000 | [http://localhost:30000](http://localhost:30000) (admin / admin) |

---

## ✅ Smoke Test (Teste de Sanidade)

### Como Validar o MVP
Siga este roteiro para garantir que todos os componentes estão integrados:

1.  **Login**:
    *   Acesse o Swagger do serviço de **Usuários** ([http://localhost:30001/swagger](http://localhost:30001/swagger)).
    *   Use o endpoint `/api/usuarios` (POST) para criar um usuário (ou use admin se existir).
    *   Faça login (`/api/auth/login`) e copie o `access_token` da resposta.

2.  **Criar Propriedade**:
    *   Acesse o Swagger de **Propriedades** ([http://localhost:30002/swagger](http://localhost:30002/swagger)).
    *   Clique em **Authorize** e cole o token `Bearer <token>`.
    *   Crie uma propriedade via `POST /api/propriedades`. Copie o ID da propriedade criada.

3.  **Iniciar Simulador**:
    *   Se o cronjob ainda não rodou, dispare um job manual para teste imediato:
    ```bash
    kubectl create job --from=cronjob/ingestao-simulador manual-test-job -n agrosolutions-local
    ```

4.  **Checar Logs no Loki (Opcional) ou via Kubectl**:
    *   Verifique se a Ingestão recebeu os dados:
    ```bash
    kubectl logs -l app=ingestao-worker -n agrosolutions-local --tail=50
    ```
    *   Verifique se a Análise processou:
    ```bash
    kubectl logs -l app=analise -n agrosolutions-local --tail=50
    ```
    *   *Logs esperados: "Mensagem recebida", "Regra processada", "Alerta gerado".*

5.  **Ver Alerta no Grafana**:
    *   Acesse o Grafana em [http://localhost:32000](http://localhost:32000) (admin / admin).
    *   Navegue até **Dashboards** -> **AgroSolutions Alerts**.
    *   Verifique os gráficos de temperatura/umidade e a tabela de alertas recentes.

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
