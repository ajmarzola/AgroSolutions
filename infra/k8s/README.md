# Guia de Execução Local – AgroSolutions

Este guia descreve como executar a plataforma AgroSolutions localmente utilizando
**Docker Desktop + Kubernetes**, de forma reproduzível e padronizada.

---

## ✅ Pré-requisitos

- Docker Desktop (com Kubernetes habilitado)
- kubectl
- Git
- .NET SDK 10.0.x (opcional, para desenvolvimento fora do container)

---

## 📦 Build das Imagens Docker

Na raiz do repositório:

```bash
./docker-build.sh
```

Este script:
- Compila todos os microsserviços
- Gera imagens Docker locais
- Usa tags `local`

---

## ☸️ Deploy no Kubernetes (Local)

Aplicar o overlay local:

```bash
kubectl apply -k infra/k8s/overlays/local
```

Verificar pods:

```bash
kubectl get pods -n agrosolutions-local
```

---

## 🌐 Acesso aos Serviços

Exemplo com port-forward:

```bash
kubectl port-forward svc/usuarios 8080:80 -n agrosolutions-local
```

Swagger disponível em:

```
http://localhost:8080/swagger
```

Repita para os demais serviços conforme necessário.

---

## 🧹 Limpeza do Ambiente

Remover recursos do cluster:

```bash
kubectl delete -k infra/k8s/overlays/local
```

---

## 🛠️ Troubleshooting

- Pods em `CreateContainerConfigError`:
  - Verifique variáveis de ambiente
  - Confira se a imagem Docker foi criada localmente

- Pods não iniciam:
  - `kubectl describe pod <nome>`
  - `kubectl logs <pod>`

---

## ℹ️ Observações

Este ambiente é **exclusivamente para desenvolvimento local**.
Não utilizar em produção.
