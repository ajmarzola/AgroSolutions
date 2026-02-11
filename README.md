# AgroSolutions 🌱

Plataforma de referência para **agricultura de precisão**, com ingestão e análise de dados de sensores, monitoramento e alertas. O projeto aplica **arquitetura de microsserviços**, práticas **cloud‑native**, **observabilidade** e **DevOps** para apoiar a modernização da tomada de decisão no campo.

---

## 🎯 Objetivos do Projeto (Hackathon 8NETT)

- Modernizar a cooperativa AgroSolutions com **agricultura 4.0**.
- Coletar dados de sensores (IoT) em **tempo real** e armazenar histórico.
- Disponibilizar **dashboards** de monitoramento e **alertas** para o produtor rural.
- Aumentar produtividade, reduzir desperdícios e promover sustentabilidade.

---

## 🛠️ Tecnologias Utilizadas

### Backend
- **.NET 10 / ASP.NET Core**
- **APIs REST**
- **Dapper + SQL Server** (Ingestão)

### Infraestrutura & Containers
- **Docker**
- **Kubernetes (Docker Desktop – ambiente local)**
- **Helm** (stack de observabilidade)

### Mensageria
- **RabbitMQ**

### Observabilidade
- **OpenTelemetry**
- **Prometheus**
- **Grafana**

### DevOps
- **GitHub Actions** (workflows)
- **Build e versionamento de imagens Docker**

---

## 🧩 Microsserviços

- **AgroSolutions.Usuarios.WebApi** – Gestão de usuários e autenticação.
- **AgroSolutions.Propriedades.WebApi** – Cadastro de propriedades e talhões.
- **AgroSolutions.Ingestao.WebApi** – Recepção de leituras de sensores e persistência.
- **AgroSolutions.Analise.WebApi** – Processamento e análise de dados.
- **AgroSolutions.Ingestao.Simulador** – Console app para gerar leituras simuladas (Fluxo: Login em Usuarios -> Token JWT -> Post em Ingestao).

Cada serviço é independente, containerizado e orquestrado via Kubernetes.

---

## ✅ Funcionalidades Implementadas

- Ingestão de leituras de sensores via API.
- Simulador de dados para geração de leituras.
- Consultas e agregações de leituras.
- Observabilidade com métricas Prometheus e dashboards Grafana.
- Deploy local via Kubernetes (Kustomize + Helm).

---

## 🗺️ Diagrama de Arquitetura (Miro)

O diagrama da arquitetura está disponível no Miro:

👉 https://miro.com/app/board/uXjVJQ5da0k=/

---

## 📚 Documentação principal

1) **Execução Local (Kubernetes)**
- [infra/k8s/README.md](infra/k8s/README.md)

2) **Observabilidade (Grafana/Prometheus)**
- [infra/observability/grafana/README.md](infra/observability/grafana/README.md)

3) **Documentação do Projeto**
- [docs/README.md](docs/README.md)

4) **Requisitos do Hackathon (Markdown)**
- [docs/REQUISITOS_HACKATHON.md](docs/REQUISITOS_HACKATHON.md)

---

## 📂 Estrutura do Repositório

```
src/             # Código fonte dos microsserviços
tests/           # Testes automatizados
infra/           # Infraestrutura como código
  k8s/           # Manifestos Kubernetes (Base + Overlays)
  observability/ # Dashboards e monitoramento
build/           # Scripts de automação (build/deploy)
docs/            # Documentação técnica
.github/         # Workflows de CI/CD
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
