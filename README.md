# AgroSolutions 🌱

Plataforma de referência para ingestão, análise e monitoramento de dados agrícolas,
desenvolvida como projeto acadêmico e técnico, com foco em **arquitetura de microsserviços**, 
**cloud-native**, **observabilidade** e **boas práticas DevOps**.

---

## 🛠️ Tecnologias Utilizadas

### Backend
- **.NET 8 / ASP.NET Core**
- **JWT Authentication**
- **Entity Framework Core**
- **APIs REST**

### Infraestrutura & Containers
- **Docker**
- **Kubernetes (Docker Desktop – ambiente local)**

### Mensageria
- **RabbitMQ**

### Observabilidade *(em evolução)*
- **Prometheus**
- **Grafana**

### DevOps
- **GitHub Actions**
- **Pipelines CI/CD**
- **Build e versionamento de imagens Docker**

---

## 🧩 Microsserviços

- **Usuários** – Identidade e autenticação
- **Propriedades** – Cadastro de propriedades e talhões
- **Ingestão** – Coleta de dados de sensores (simulados)
- **Análise** – Processamento, métricas e alertas

Cada serviço é independente, containerizado e orquestrado via Kubernetes.

---

## 📊 Funcionalidades Implementadas (MVP)

- ✔ Autenticação do Produtor Rural  
- ✔ Cadastro de Propriedades e Talhões  
- ✔ Ingestão de dados simulados de sensores  
- ✔ Processamento e análise de dados agrícolas  
- ✔ Geração de alertas automáticos  
- ✔ Aplicação containerizada com Docker  
- ✔ Orquestração com Kubernetes local  

---

## 🗺️ Diagrama de Arquitetura (Miro)

O diagrama oficial e atualizado da arquitetura está disponível no Miro:

👉 https://miro.com/app/board/uXjVJQ5da0k=/

Este diagrama representa:
- Separação de responsabilidades por microsserviço
- Fluxo de dados de ingestão → análise
- Camada de observabilidade
- Integração com CI/CD e infraestrutura Kubernetes

---

## 📚 Índice & Tutoriais

Siga os guias abaixo para configurar, executar e monitorar o projeto em seu ambiente local:

1. **🚀 Guia de Execução (Kubernetes)**
   - 📄 [infra/k8s/README.md](infra/k8s/README.md)
   - *Instruções passo-a-passo para subir a stack completa no Docker Desktop.*

2. **📊 Observabilidade (Grafana)**
   - 📄 [infra/observability/grafana/README.md](infra/observability/grafana/README.md)
   - *Como importar dashboards e acompanhar métricas.*

3. **📘 Documentação do Projeto**
   - 📄 [docs/README.md](docs/README.md)
   - *Detalhes arquiteturais e especificações.*

## 📂 Estrutura do Repositório

```
src/             # Código fonte dos microsserviços (APIs)
infra/           # Infraestrutura como Código
  k8s/           # Manifestos Kubernetes (Base + Overlays)
  observability/ # Configs de monitoramento (Grafana/Prometheus)
build/           # Scripts de automação (build, deploy)
docs/            # Documentação técnica
.github/         # Workflows do GitHub Actions
```

---

## 👥 Membros da Equipe – Grupo 21

### 👨‍💻 Anderson Marzola  
- **Matrícula:** RM360850  
- **E-mail:** RM360850@fiap.com.br  
- **Discord:** aj.marzola  
- **GitHub:** https://github.com/ajmarzola  

### 👨‍💻 Rafael Nicoletti  
- **Matrícula:** RM361308  
- **E-mail:** RM361308@fiap.com.br  
- **Discord:** rafaelnicoletti_  
- **GitHub:** https://github.com/RafaelNicoletti  

### 👨‍💻 Valber Martins  
- **Matrícula:** RM360859  
- **E-mail:** RM360859@fiap.com.br  
- **Discord:** valberdev  
- **GitHub:** https://github.com/ValberX21  

---

## 📄 Licença

Projeto de uso educacional.
