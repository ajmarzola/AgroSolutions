# AgroSolutions 🌱

Plataforma de referência para ingestão, análise e monitoramento de dados agrícolas, desenvolvida como projeto acadêmico e técnico, com foco em **arquitetura de microsserviços**, **cloud-native**, **observabilidade** e **boas práticas DevOps**.

---

## Índice (Tutorial – execução local)

1. **Infra / Kubernetes (Docker Desktop – Windows)**  
   ➜ `infra/k8s/README.md`  
   Guia completo para:
   - build das imagens (PowerShell),
   - deploy via Kustomize (overlay `local`),
   - execução em cenários de ambiente **já criado**, **novo** e **desatualizado**.

2. **Observabilidade / Grafana (Dashboards de monitoramento)**  
   ➜ `infra/observability/grafana/README.md`  
   Guia completo para:
   - acesso ao Grafana no cluster,
   - configuração da fonte de dados (Prometheus),
   - importação do dashboard do repositório,
   - validação das métricas durante a execução.

---

## Visão geral da solução

A solução é composta por microsserviços independentes, containerizados e orquestrados em Kubernetes (Kustomize), incluindo uma stack de observabilidade baseada em Prometheus + Grafana.

### Microsserviços

- **Usuários** – Identidade e autenticação do produtor rural
- **Propriedades** – Cadastro de propriedades e talhões
- **Ingestão** – Recebimento de dados de sensores (simulados)
- **Análise** – Processamento, métricas e alertas simples

---

## Tecnologias utilizadas

- **.NET 10** (Web APIs)
- **Docker** (build das imagens localmente)
- **Kubernetes (Docker Desktop) + Kustomize** (base + overlays `local`, `dev`, `prod`)
- **GitHub Actions** (pipelines CI)
- **Prometheus + Grafana** (observabilidade)
- **OpenTelemetry** (instrumentação)
- **Health checks** (`/health/live` e `/health/ready`)
- **SQL Server / Dados simulados** (conforme evolução do MVP)

---

## Estrutura do repositório

```
AgroSolutions-anderson-monitoramento/
├─ src/
│  └─ services/
│     ├─ AgroSolutions.Analise/
│     ├─ AgroSolutions.Ingestao/
│     ├─ AgroSolutions.Propriedades/
│     └─ AgroSolutions.Usuarios/
├─ infra/
│  ├─ k8s/
│  │  ├─ base/                 # manifests por serviço (Deployment/Service/ConfigMap)
│  │  └─ overlays/
│  │     ├─ local/             # namespace agrosolutions-local + imagens tag "local"
│  │     ├─ dev/
│  │     └─ prod/
│  └─ observability/
│     └─ grafana/
│        ├─ dashboards/        # dashboards JSON versionados
│        └─ README.md
├─ build/
│  └─ scripts/
│     ├─ docker-build.ps1      # build das imagens (Windows / PowerShell)
│     └─ docker-build.sh       # build das imagens (bash)
├─ docs/
│  └─ HACKATHON 8NETT.pdf
└─ .github/workflows/          # CI por serviço
```

---

## Diagrama de Arquitetura (Miro)

O diagrama da arquitetura (referência do time) está disponível no Miro:

👉 https://miro.com/app/board/uXjVJQ5da0k=/

---

## Observações importantes (local)

- O tutorial oficial para executar localmente fica em `infra/k8s/README.md`.
- Em Docker Desktop (Windows), a forma recomendada é usar o script **PowerShell**:
  - `build/scripts/docker-build.ps1`

---

## Licença

Projeto de uso educacional.
