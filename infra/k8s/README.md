# Guia de Execução Local – Kubernetes (Docker Desktop / Windows)

Este guia descreve como executar a plataforma **AgroSolutions** localmente usando **Docker Desktop com Kubernetes**, de forma reprodutível e com o mínimo de risco de erro operacional.

> Escopo: **ambiente local** (namespace `agrosolutions-local`).  
> Para `dev`/`prod`, utilize os overlays correspondentes em `infra/k8s/overlays/`.

---

## 1) Pré-requisitos (Windows)

### 1.1 Ferramentas

- **Docker Desktop** (Windows) com:
  - **Kubernetes habilitado** (Settings → Kubernetes → *Enable Kubernetes*)
  - **WSL 2** habilitado (recomendado pelo Docker Desktop)
- **kubectl** (pode vir com o Docker Desktop; opcional instalar via Chocolatey/Winget)
- **Git**
- **PowerShell** (Windows)
- **.NET SDK 10.0.x** *(opcional)*, apenas se você quiser rodar fora de container.

### 1.2 Verificações rápidas

No PowerShell, valide:

```powershell
docker version
kubectl version --client
kubectl config current-context
kubectl get nodes
```

Se `kubectl get nodes` não retornar o node do Docker Desktop, habilite o Kubernetes no Docker Desktop e aguarde o cluster ficar “Running”.

---

## 2) Convenções do repositório (importante)

- Os manifests Kubernetes ficam em: `infra/k8s/`
- O overlay local é: `infra/k8s/overlays/local`
- O namespace local é: **`agrosolutions-local`**
- As imagens usadas no overlay local são reescritas pelo Kustomize para:

```
ghcr.io/agrosolutions/<serviço>:local
```

Por isso, no local, **você precisa buildar essas imagens na sua máquina** antes do deploy.

---

## 3) Build das imagens Docker (Windows / PowerShell)

Na raiz do repositório (onde existe `AgroSolutions.slnx`), execute:

```powershell
pwsh -File .\build\scripts\docker-build.ps1 -Environment local
```

O script:
- compila e builda as imagens dos microsserviços
- aplica a tag `local`
- usa, por padrão, o registry `ghcr.io/agrosolutions`

### 3.1 Verificando se as imagens existem

```powershell
docker images ghcr.io/agrosolutions/analise
docker images ghcr.io/agrosolutions/ingestao
docker images ghcr.io/agrosolutions/propriedades
docker images ghcr.io/agrosolutions/usuarios
```

---

## 4) Deploy no Kubernetes (overlay local)

### 4.1 Aplicar manifests

```powershell
kubectl apply -k infra/k8s/overlays/local
```

### 4.2 Validar namespace e recursos

```powershell
kubectl get ns | findstr agrosolutions-local
kubectl get all -n agrosolutions-local
kubectl get pods -n agrosolutions-local -o wide
```

Aguarde os pods ficarem `Running` e `READY 1/1`.

---

## 5) Como executar o ambiente local por cenário

### Cenário A — Ambiente **já criado** (cluster ok, recursos já existem)

Use quando você já executou anteriormente e quer apenas “subir de novo” ou aplicar pequenas mudanças.

1) (Opcional) Rebuild das imagens caso tenha alterado código:

```powershell
pwsh -File .\build\scripts\docker-build.ps1 -Environment local
```

2) Reaplicar o overlay:

```powershell
kubectl apply -k infra/k8s/overlays/local
```

3) Verificar saúde:

```powershell
kubectl get pods -n agrosolutions-local
kubectl get svc -n agrosolutions-local
```

4) Se algum pod não refletir a nova imagem, force restart do Deployment:

```powershell
kubectl rollout restart deployment -n agrosolutions-local
kubectl rollout status deployment/usuarios -n agrosolutions-local
kubectl rollout status deployment/propriedades -n agrosolutions-local
kubectl rollout status deployment/ingestao -n agrosolutions-local
kubectl rollout status deployment/analise -n agrosolutions-local
```

---

### Cenário B — Ambiente **novo** (primeira execução na máquina)

Use quando o Docker Desktop/Kubernetes acabou de ser instalado, ou você nunca subiu o projeto localmente.

1) Habilitar Kubernetes no Docker Desktop e validar:

```powershell
kubectl get nodes
```

2) Clonar o repositório e abrir um terminal na raiz.

3) Buildar imagens:

```powershell
pwsh -File .\build\scripts\docker-build.ps1 -Environment local
```

4) Aplicar o overlay local:

```powershell
kubectl apply -k infra/k8s/overlays/local
```

5) Acompanhar pods:

```powershell
kubectl get pods -n agrosolutions-local -w
```

6) Validar endpoints de saúde (opcional via port-forward; ver seção 6).

---

### Cenário C — Ambiente **“desatualizado”** (precisa recriar do zero)

Use quando ocorrerem sintomas como:
- pods presos em `CreateContainerConfigError` / `ImagePullBackOff`
- recursos antigos conflitando com os manifests atuais
- namespace com lixo de deploys anteriores
- mudanças grandes de estrutura de manifests/labels/ports

#### 1) Remover recursos do overlay

```powershell
kubectl delete -k infra/k8s/overlays/local
```

> Se houver erro por “recurso não encontrado”, pode ignorar.

#### 2) Remover o namespace (limpeza total)

```powershell
kubectl delete namespace agrosolutions-local
```

Aguarde o namespace sumir:

```powershell
kubectl get ns | findstr agrosolutions-local
```

#### 3) (Opcional) Limpeza de imagens antigas

Se você suspeitar que está rodando imagem antiga (mesmo com tag `local`):

```powershell
docker image rm ghcr.io/agrosolutions/analise:local -f
docker image rm ghcr.io/agrosolutions/ingestao:local -f
docker image rm ghcr.io/agrosolutions/propriedades:local -f
docker image rm ghcr.io/agrosolutions/usuarios:local -f
```

#### 4) Rebuild e redeploy

```powershell
pwsh -File .\build\scripts\docker-build.ps1 -Environment local
kubectl apply -k infra/k8s/overlays/local
kubectl get pods -n agrosolutions-local -w
```

---

## 6) Acesso aos serviços (Swagger)

Os Services são `ClusterIP`. Para acessar localmente, use `port-forward`.

### 6.1 Usuários (exemplo)

```powershell
kubectl port-forward svc/usuarios 8080:80 -n agrosolutions-local
```

Abra:

- Swagger: `http://localhost:8080/swagger`
- Health: `http://localhost:8080/health/live` e `http://localhost:8080/health/ready`

### 6.2 Demais serviços (sugestão de portas)

Abra **um terminal por serviço**:

```powershell
kubectl port-forward svc/propriedades 8081:80 -n agrosolutions-local
kubectl port-forward svc/ingestao 8082:80 -n agrosolutions-local
kubectl port-forward svc/analise 8083:80 -n agrosolutions-local
```

---

## 7) Troubleshooting (comandos essenciais)

### 7.1 Ver por que o pod não sobe

```powershell
kubectl get pods -n agrosolutions-local
kubectl describe pod <NOME_DO_POD> -n agrosolutions-local
kubectl logs <NOME_DO_POD> -n agrosolutions-local --all-containers=true
```

### 7.2 Problemas comuns e correções rápidas

- **ImagePullBackOff / ErrImagePull**  
  Normalmente indica que a imagem não existe localmente com o nome/tag esperados.
  - Rode novamente o build: `pwsh -File .\build\scripts\docker-build.ps1 -Environment local`
  - Confirme `docker images ghcr.io/agrosolutions/<svc>`

- **CreateContainerConfigError**  
  Frequentemente relacionado a env vars/configmaps:
  - Verifique o ConfigMap do serviço:
    ```powershell
    kubectl get configmap -n agrosolutions-local
    kubectl describe configmap usuarios-config -n agrosolutions-local
    ```
  - Veja eventos do pod: `kubectl describe pod ...`

- **Mudança não refletiu após deploy**  
  Use:
  ```powershell
  kubectl rollout restart deployment -n agrosolutions-local
  ```

---

## 8) Referências no repositório

- Base manifests: `infra/k8s/base/`
- Overlays: `infra/k8s/overlays/`
- Observabilidade (Grafana): `infra/observability/grafana/README.md`

