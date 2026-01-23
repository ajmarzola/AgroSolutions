# 🌱 AgroSolutions – Plataforma de Agricultura de Precisão  
### Hackathon FIAP – 8NETT

---

## 📌 Visão Geral

A **AgroSolutions** é uma plataforma de **Agricultura de Precisão (Agro 4.0)** desenvolvida como **MVP** para o **Hackathon FIAP – 8NETT**.  
O projeto tem como objetivo apoiar produtores rurais na **tomada de decisão baseada em dados**, utilizando conceitos modernos de **IoT, microsserviços, mensageria, containers, Kubernetes e observabilidade**.

A solução simula a coleta de dados de sensores agrícolas — como **umidade do solo, temperatura e precipitação** — permitindo sua análise, visualização histórica e a geração automática de **alertas inteligentes** para cada talhão.

---

## 🎯 Objetivos do Projeto

- Modernizar a gestão agrícola com **dados em tempo (quase) real**
- Implementar uma **arquitetura de microsserviços desacoplados**
- Simular a **ingestão de dados de sensores agrícolas**
- Processar dados e gerar **alertas automáticos**
- Disponibilizar dados para **dashboards de monitoramento**
- Demonstrar **orquestração com Kubernetes**
- Aplicar **boas práticas de arquitetura, segurança e CI/CD**
- Preparar o ambiente para **observabilidade com Prometheus e Grafana**

---

## 🧩 Arquitetura da Solução

A arquitetura foi desenhada seguindo princípios de **cloud-native architecture**, com serviços independentes, comunicação assíncrona e fácil escalabilidade.

### Microsserviços (.NET)

| Serviço | Projeto | Responsabilidade |
|-------|--------|------------------|
| Usuários | `AgroSolutions.Usuarios` | Autenticação e autorização de produtores (JWT) |
| Propriedades | `AgroSolutions.Propriedades` | Cadastro de propriedades, talhões e culturas |
| Ingestão | `AgroSolutions.Ingestao` | Recebimento e validação de dados simulados de sensores |
| Análise | `AgroSolutions.Analise` | Processamento dos dados e geração de alertas |
| Mensageria | RabbitMQ | Comunicação assíncrona entre serviços |

---

## 📂 Estrutura de Diretórios

A estrutura do repositório foi organizada para facilitar manutenção, deploy e entendimento arquitetural:

```
AgroSolutions/
│
├── .github/                # Workflows do GitHub Actions (CI/CD)
├── build/                  # Scripts e configurações de build
├── docs/                   # Documentação e diagramas
├── infra/
│   └── k8s/                # Manifestos Kubernetes
│
├── src/
│   └── services/
│       ├── AgroSolutions.Analise/        # Serviço de Análise e Alertas
│       ├── AgroSolutions.Ingestao/       # Serviço de Ingestão de Dados
│       ├── AgroSolutions.Propriedades/   # Serviço de Propriedades e Talhões
│       └── AgroSolutions.Usuarios/       # Serviço de Usuários (Auth)
│
├── AgroSolutions.slnx       # Solução principal (.NET)
└── README.md
```

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

## 📊 Funcionalidades Implementadas (MVP)

- ✔ Autenticação do Produtor Rural  
- ✔ Cadastro de Propriedades e Talhões  
- ✔ Ingestão de dados simulados de sensores  
- ✔ Processamento e análise de dados agrícolas  
- ✔ Geração de alertas automáticos  
- ✔ Aplicação containerizada com Docker  
- ✔ Orquestração com Kubernetes local  

---

## 🚀 Execução do Projeto (Resumo)

**Pré-requisitos**
- Docker Desktop
- Kubernetes habilitado
- kubectl configurado
- .NET SDK 8

**Fluxo geral**
1. Build das imagens Docker dos serviços
2. Aplicação dos manifests Kubernetes
3. Comunicação entre serviços via RabbitMQ
4. Serviços disponíveis no cluster local

---

## 👥 Membros da Equipe – Grupo 21

### 👨‍💻 Anderson Marzola  
- **Matrícula:** RM360850  
- **E-mail:** RM360850@fiap.com.br  
- **Discord:** aj.marzola  
- **GitHub:** https://github.com/ajmarzola  

---

### 👨‍💻 Rafael Nicoletti  
- **Matrícula:** RM361308  
- **E-mail:** RM361308@fiap.com.br  
- **Discord:** rafaelnicoletti_  
- **GitHub:** https://github.com/RafaelNicoletti  

---

### 👨‍💻 Valber Martins  
- **Matrícula:** RM360859  
- **E-mail:** RM360859@fiap.com.br  
- **Discord:** valberdev  
- **GitHub:** https://github.com/ValberX21  

---

## 🌾 Considerações Finais

A **AgroSolutions** entrega um **MVP funcional e arquiteturalmente consistente**, aplicando conceitos modernos de engenharia de software, cloud, containers e DevOps.  
O projeto está preparado para evolução, incluindo dashboards avançados, observabilidade completa e integração com dados externos.

**FIAP – Hackathon 8NETT | Grupo 21**
