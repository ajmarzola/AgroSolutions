# ?? AgroSolutions – Plataforma de Agricultura de Precisão (Hackathon FIAP 8NETT)

## ?? Visão Geral

A **AgroSolutions** é uma plataforma de **Agricultura 4.0** desenvolvida como MVP para o **Hackathon FIAP – 8NETT**, com o objetivo de modernizar a tomada de decisão no campo por meio de **IoT, microsserviços, mensageria, observabilidade e análise de dados**.

A solução permite que produtores rurais acompanhem dados simulados de sensores agrícolas (umidade do solo, temperatura e precipitação), visualizem históricos, recebam alertas automáticos e gerenciem suas propriedades e talhões de forma centralizada.

---

## ?? Objetivos do Projeto

- Implementar uma **arquitetura baseada em microsserviços**
- Simular **ingestão de dados de sensores agrícolas**
- Processar dados e gerar **alertas automáticos**
- Disponibilizar dados para **dashboards de monitoramento**
- Demonstrar **orquestração com Kubernetes**
- Aplicar **boas práticas de arquitetura, segurança e CI/CD**
- Preparar a base para **observabilidade com Prometheus + Grafana**

---

## ?? Arquitetura da Solução

A solução foi projetada seguindo os princípios de **microsserviços desacoplados**, cada um com responsabilidade bem definida.

### Microsserviços

| Serviço (Projeto .NET) | Responsabilidade |
|------|------------------|
| **AgroSolutions.Usuarios** | Autenticação e autorização de produtores rurais (JWT) |
| **AgroSolutions.Propriedades** | Cadastro de propriedades, talhões e culturas |
| **AgroSolutions.Ingestao** | Recebimento e validação de dados simulados de sensores |
| **AgroSolutions.Analise** | Processamento dos dados e geração de alertas |
| **Mensageria (RabbitMQ)** | Comunicação assíncrona entre serviços |

---

## Estrutura de Diretórios

A estrutura do repositório está organizada da seguinte forma:

```
AgroSolutions/
|-- .github/                # Configurações de Workflow do GitHub Actions
|-- build/                  # Scripts e configurações de build
|-- docs/                   # Documentação adicional do projeto
|-- infra/                  # Configurações de infraestrutura
|   `-- k8s/                # Manifestos Kubernetes
|-- src/                    # Código fonte dos serviços
|   `-- services/
|       |-- AgroSolutions.Analise/       # Serviço de Análise e Alertas
|       |-- AgroSolutions.Ingestao/      # Serviço de Ingestão de Dados
|       |-- AgroSolutions.Propriedades/  # Serviço de Propriedades e Talhões
|       `-- AgroSolutions.Usuarios/      # Serviço de Identidade (Auth)
|-- AgroSolutions.slnx      # Solução principal (.NET)
`-- README.md
```

---

## ??? Tecnologias Utilizadas

### Backend
- **.NET 8 / ASP.NET Core**
- **JWT Authentication**
- **Entity Framework Core**
- **REST APIs**

### Infraestrutura
- **Docker**
- **Kubernetes (Docker Desktop / Local Cluster)**

### Mensageria
- **RabbitMQ**

### Observabilidade (em andamento)
- **Prometheus**
- **Grafana**

### DevOps
- **GitHub**
- **Pipelines CI/CD**

---

## ?? Funcionalidades Implementadas (MVP)

? Autenticação do Produtor Rural  
? Cadastro de Propriedades e Talhões  
? Ingestão de dados simulados de sensores  
? Processamento e análise de dados  
? Geração de alertas automáticos  
? Deploy em containers Docker  
? Orquestração com Kubernetes local  

---

## ?? Membros da Equipe – Grupo 21

### ????? Anderson Marzola  
- Matrícula: RM360850  
- E-mail: RM360850@fiap.com.br  
- Discord: aj.marzola  
- GitHub: https://github.com/ajmarzola  

### ????? Rafael Nicoletti  
- Matrícula: RM361308  
- E-mail: RM361308@fiap.com.br  
- Discord: rafaelnicoletti_  
- GitHub: https://github.com/RafaelNicoletti  

### ????? Valber Martins  
- Matrícula: RM360859  
- E-mail: RM360859@fiap.com.br  
- Discord: valberdev  
- GitHub: https://github.com/ValberX21  

---

## ?? Considerações Finais

A AgroSolutions representa um **MVP sólido de agricultura de precisão**, aplicando conceitos modernos de arquitetura de software, cloud-native e DevOps, com foco em escalabilidade, observabilidade e boas práticas.

**FIAP – Hackathon 8NETT | Grupo 21**
