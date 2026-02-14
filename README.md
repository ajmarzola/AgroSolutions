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

## � Quickstart Executável (Caminho Feliz)

Para rodar o projeto localmente e validar o fluxo completo:

### 1. Subir Infraestrutura
> **Nota**: Antes de executar, configure os segredos conforme instruído no [Guia de Infraestrutura](infra/k8s/README.md#⚠-passo-obrigatório-configuração-de-segredos).

```bash
kubectl apply -k infra/k8s/overlays/local
```

### 2. Autenticação (Registrar e Obter Token)

**Passo 2.1: Registrar Usuário (Necessário na primeira execução)**
**POST** `http://localhost:30001/api/usuarios/registrar`
```json
{
  "nome": "Admin",
  "email": "admin@agrosolutions.com",
  "senha": "admin",
  "tipoId": 1
}
```

**Passo 2.2: Login**
**POST** `http://localhost:30001/api/usuarios/login`
```json
{
  "email": "admin@agrosolutions.com",
  "password": "admin"
}
```
*Copie o token `eyJ...` retornado.*

### 3. Criar Recursos com o Token
Use o Header `Authorization: Bearer <SEU_TOKEN>` nas requisições abaixo.

**Criar Propriedade:**
**POST** `http://localhost:30002/api/v1/propriedades`
```json
{
  "nome": "Fazenda Modelo",
  "localizacao": "SP"
}
```
*Copie o `id` da resposta.*

**Criar Talhão:**
Substitua `{id}` pelo ID da propriedade criada.
**POST** `http://localhost:30002/api/v1/propriedades/{id}/talhoes`
```json
{
  "nome": "Talhão 1",
  "cultura": "Soja",
  "area": 100
}
```

### 4. Verificar Simulador e Alertas

1. **Simulador:** 
   Verifique se está rodando: `kubectl get pods -n agrosolutions-local -l app=ingestao-simulador` (Status deve ser `Running`).

2. **Grafana:**
   Exponha o serviço:
   ```bash
   kubectl port-forward svc/grafana 3000:80 -n agrosolutions-local
   ```
   Acesse `http://localhost:3000` (User/Pass: `admin`/`admin`).
   Veja o dashboard "AgroSolutions Monitor" com os alertas gerados.

### 5. Executar Script de Testes Automatizados (QA Validation)

Para validar a integridade de todo o fluxo (Infra, Auth, Ingestão, Mensageria e Banco de Dados), execute o script de validação:

**Pré-requisitos:** Python 3 instalado.

```bash
# Instalar dependências (se necessário)
pip install requests pyodbc

# Executar script de validação v2
python tests/qa_validation_v2.py
```

O script realizará:
1. Verificação de conexão SQL e autenticação.
2. Criação de usuários, propriedades e talhões.
3. Envio de leituras de sensores (Ingestão).
4. Validação do processamento assíncrono (RabbitMQ -> Analise -> DB).
5. Verificação de geração de Alertas de negócio.

---

## 🔧 Troubleshooting

| Erro / Sintoma | Ação Recomendada |
| :--- | :--- |
| **Pod CrashLoopBackOff** | `kubectl logs <nome-pod> -n agrosolutions-local` para ver detalhes. |
| **Probes (NotReady)** | Aguarde a inicialização completa (especialmente SQL Server). Aumente `initialDelaySeconds` se persistir. |
| **Erro Conexão SQL** | Verifique se o pod SQL Server está `Running`. Confirme a connection string nos Secrets. |
| **Erro 401 Unauthorized** | Token expirou. Gere um novo no `/login`. |

---

## �🗺️ Diagrama de Arquitetura (Miro)

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
