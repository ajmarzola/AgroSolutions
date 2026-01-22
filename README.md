# AgroSolutions – Plataforma de Microsserviços para Agricultura Inteligente

## Visão Geral do Projeto

O **AgroSolutions** é uma plataforma distribuída baseada em **microsserviços**, desenvolvida como projeto acadêmico com foco em **arquitetura moderna, escalável e orientada a cloud**.  
O sistema simula um ecossistema de soluções para o agronegócio, cobrindo desde **gestão de propriedades rurais** até **ingestão e análise de dados**, com autenticação centralizada.

O projeto foi concebido para demonstrar, de forma prática, conceitos avançados de:
- Arquitetura de microsserviços
- Conteinerização com Docker
- Orquestração com Kubernetes
- Automação de build e qualidade com CI
- Boas práticas no ecossistema **.NET moderno**

---

## Arquitetura Geral

A solução é composta por **quatro microsserviços independentes**, todos implementados como **ASP.NET Core Web APIs**, executando em containers Docker e orquestrados via Kubernetes.

```
┌──────────────────┐
│ IdentityService  │  ← Autenticação e identidade
└─────────▲────────┘
          │
┌─────────┴────────┐
│ PropertyService  │  ← Gestão de propriedades rurais
└─────────▲────────┘
          │
┌─────────┴────────┐
│ IngestionService │  ← Ingestão de dados (sensores / fontes externas)
└─────────▲────────┘
          │
┌─────────┴────────┐
│ AnalysisService  │  ← Análise de dados e geração de alertas
└──────────────────┘
```

A comunicação entre os serviços ocorre via **HTTP interno no cluster Kubernetes**, utilizando o **DNS de serviços** (`service-name`) fornecido pela própria plataforma.

---

## Descrição dos Microsserviços

### 1. IdentityService (Usuários)
Responsável pela **gestão de identidade e autenticação** do sistema.

**Principais responsabilidades:**
- Cadastro e autenticação de usuários
- Emissão e validação de tokens (JWT)
- Serviço central de identidade consumido pelos demais microsserviços

---

### 2. PropertyService (Propriedades)
Responsável pelo **cadastro e gerenciamento de propriedades rurais**.

**Principais responsabilidades:**
- CRUD de propriedades
- Associação de propriedades a usuários autenticados
- Base para correlação de dados agrícolas

---

### 3. IngestionService (Ingestão de Dados)
Responsável pela **entrada de dados no sistema**, simulando dados de campo ou integração com fontes externas.

**Principais responsabilidades:**
- Receber dados de sensores (ex.: temperatura, umidade, precipitação)
- Persistir e normalizar dados recebidos
- Preparar dados para análise posterior

---

### 4. AnalysisService (Análise e Alertas)
Responsável pela **análise dos dados ingeridos** e geração de informações de valor.

**Principais responsabilidades:**
- Processar dados provenientes do IngestionService
- Aplicar regras de negócio e análises
- Simular geração de alertas e insights

---

## Tecnologias Utilizadas

### Backend / Aplicação
- **.NET 8**
- **ASP.NET Core Web API**
- **C#**
- Health Checks nativos do ASP.NET Core

### Conteinerização e Orquestração
- **Docker** (multi-stage build)
- **Kubernetes**
- **Kustomize** (base + overlays por ambiente)

### DevOps e Qualidade
- **GitHub Actions** (CI)
- Build automatizado
- Execução de testes (estrutura preparada)
- Coleta de cobertura de código (XPlat Code Coverage)

### Infraestrutura Local
- **Docker Desktop com Kubernetes habilitado**

---

## Estrutura do Repositório (Visão Geral)

```
AgroSolutions/
│
├── src/
│   └── services/
│       ├── Usuarios/
│       ├── Propriedades/
│       ├── Ingestao/
│       └── Analise/
│
├── infra/
│   └── k8s/
│       ├── base/
│       └── overlays/
│           ├── local/
│           ├── dev/
│           └── prod/
│
├── build/
│   └── scripts/
│       ├── docker-build.ps1
│       ├── docker-build.sh
│       └── k8s-apply.sh
│
├── .github/
│   └── workflows/
│
└── README.md
```

---

## Como Executar o Projeto Localmente (Avaliação)

> **Ambiente alvo:** Professores e avaliadores utilizando **Docker Desktop (Windows ou macOS)** com Kubernetes habilitado.

### Pré-requisitos
- Docker Desktop instalado
- Kubernetes habilitado no Docker Desktop
- `kubectl` disponível no PATH

---

### Passo 1 – Clonar o repositório
```bash
git clone <url-do-repositorio>
cd AgroSolutions
```

---

### Passo 2 – Build das imagens Docker (ambiente local)
No Windows (PowerShell):

```powershell
.\build\scripts\docker-build.ps1 -Environment local -Registry ghcr.io/agrosolutions
```

Este comando:
- Compila os quatro microsserviços
- Gera imagens Docker com a tag `local`

---

### Passo 3 – Deploy no Kubernetes local
```powershell
kubectl apply -k .\infra\k8s\overlays\local
```

O comando:
- Cria o namespace `agrosolutions-local`
- Sobe todos os serviços simultaneamente
- Aplica ConfigMaps, Deployments e Services

---

### Passo 4 – Verificar status dos serviços
```powershell
kubectl get pods -n agrosolutions-local
kubectl get svc -n agrosolutions-local
```

Todos os Pods devem estar com status **Running** e **Ready**.

---

### Passo 5 – Acessar as APIs (Swagger)
Utilize **port-forward** para acessar localmente:

```powershell
kubectl port-forward -n agrosolutions-local svc/usuarios      8081:80
kubectl port-forward -n agrosolutions-local svc/propriedades 8082:80
kubectl port-forward -n agrosolutions-local svc/ingestao     8083:80
kubectl port-forward -n agrosolutions-local svc/analise      8084:80
```

Acesse no navegador:
- http://localhost:8081/swagger
- http://localhost:8082/swagger
- http://localhost:8083/swagger
- http://localhost:8084/swagger

---

### Passo 6 – Encerrar o ambiente
```powershell
kubectl delete namespace agrosolutions-local
```

---

## Considerações Finais

Este projeto foi estruturado com foco em **boas práticas de arquitetura e DevOps**, priorizando:
- Clareza arquitetural
- Padronização
- Facilidade de avaliação e execução local
- Aderência a cenários reais de mercado

Ele demonstra, de forma prática, como construir, empacotar, orquestrar e executar uma solução moderna baseada em microsserviços no ecossistema .NET.

---

**AgroSolutions – Arquitetura, Cloud e Engenharia de Software aplicadas ao Agronegócio**
