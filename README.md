# AgroSolutions 🌱

Plataforma de referência para ingestão, análise e monitoramento de dados agrícolas,
desenvolvida como projeto acadêmico e técnico, com foco em **arquitetura de microsserviços**, 
**cloud-native**, **observabilidade** e **boas práticas DevOps**.

---

## 🚀 Tecnologias Principais

- **.NET 10** (Web APIs)
- **Docker**
- **Kubernetes (Kustomize)**
- **GitHub Actions (CI/CD)**
- **Prometheus + Grafana (Observabilidade)**
- **OpenTelemetry**
- **SQL Server / Dados simulados**

---

## 🧩 Microsserviços

- **Usuários** – Identidade e autenticação
- **Propriedades** – Cadastro de propriedades e talhões
- **Ingestão** – Coleta de dados de sensores (simulados)
- **Análise** – Processamento, métricas e alertas

Cada serviço é independente, containerizado e orquestrado via Kubernetes.

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

## 🐳 Execução Local

A execução local com Docker + Kubernetes (Docker Desktop) está documentada em:

📄 `infra/k8s/README.md`

---

## 📊 Observabilidade

A stack de observabilidade local utiliza:

- Prometheus (via Prometheus Operator)
- Grafana (dashboards customizados)
- OpenTelemetry nos serviços

Documentação detalhada:

📄 `infra/observability/grafana/README.md`

---

## 📚 Documentação

Índice central de documentação:

📄 `docs/README.md`

## 📂 Estrutura do Repositório (resumo)


```
src/
  services/
infra/
  k8s/
  observability/
docs/
.github/
```

---

## 👥 Equipe

Projeto desenvolvido no contexto acadêmico FIAP – Tech Challenge / Hackathon.

---

## 📄 Licença

Projeto de uso educacional.
