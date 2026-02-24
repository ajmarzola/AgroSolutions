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
- **AgroSolutions.Ingestao.Simulador** – Serviço (Deployment) que roda continuamente gerando leituras simuladas de sensores (Autentica com Admin -> Gera Token -> Post em Ingestao).

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

### 2. Autenticação (Usuário Admin Padrão)

O sistema já inicia com um usuário administrador pré-configurado via **Seed Database**. O **Simulador** utiliza este usuário automaticamente para gerar dados.

**Credenciais Padrão:**
- **Email:** `admin@agrosolutions.com`
- **Senha:** `Admin123!`

> **Nota:** Se desejar criar novos usuários, utilize os endpoints da API de Usuários.

**Passo 2.1: Login (Manual - Opcional)**
**POST** `http://localhost:30001/api/usuarios/login`
```json
{
  "email": "admin@agrosolutions.com",
  "password": "Admin123!"
}
```
*Copie o token `eyJ...` retornado se for realizar chamadas manuais.*

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

## 📖 Referência de API

Abaixo, a documentação simplificada dos principais endpoints para integração. Para detalhes completos dos esquemas e modelos de dados, consulte o Swagger de cada serviço (disponível via Ingress ou Port Forward).

### 1. API de Usuários (AgroSolutions.Usuarios)
**Responsabilidade:** Gerenciar o ciclo de vida dos usuários (produtores rurais, administradores) e prover autenticação segura via tokens JWT para acesso aos demais serviços.

| Método | Endpoint | Descrição |
| :--- | :--- | :--- |
| **POST** | `/api/usuarios/registrar` | Cadastra um novo usuário no sistema. |
| **POST** | `/api/usuarios/login` | Autentica o usuário e retorna o token JWT (Bearer). |
| **GET** | `/api/usuarios` | Lista todos os usuários (Requer Auth). |
| **DELETE** | `/api/usuarios/{id}` | Remove um usuário pelo ID (Requer Auth). |

**Exemplo de Payload (Login):**
```json
{
  "email": "produtor@agro.com",
  "password": "senha_segura"
}
```

**Teste Rápido (cURL):**
```bash
curl -X POST http://localhost/api/usuarios/login \
  -H "Content-Type: application/json" \
  -d '{"email":"produtor@agro.com", "password":"senha_segura"}'
```

---

### 2. API de Propriedades (AgroSolutions.Propriedades)
**Responsabilidade:** Permitir que produtores cadastrem suas fazendas e subdivisões (talhões), criando a estrutura hierárquica necessária para associar os dispositivos IoT.

| Método | Endpoint | Descrição |
| :--- | :--- | :--- |
| **GET** | `/api/v1/Propriedades` | Lista propriedades do usuário logado. |
| **POST** | `/api/v1/Propriedades` | Cria uma nova propriedade. |
| **GET** | `/api/v1/Propriedades/{id}/talhoes` | Lista talhões de uma propriedade. |
| **POST** | `/api/v1/Propriedades/{id}/talhoes` | Cria um talhão dentro de uma propriedade. |
| **GET** | `/api/v1/Propriedades/talhoes/{id}` | Detalhes de um talhão específico. |

**Exemplo de Payload (Criar Propriedade):**
```json
{
  "nome": "Fazenda Esperança",
  "localizacao": "Mato Grosso"
}
```

**Teste Rápido (cURL):**
```bash
curl -X GET http://localhost/api/v1/Propriedades \
  -H "Authorization: Bearer <SEU_TOKEN>"
```

---

### 3. API de Ingestão (AgroSolutions.Ingestao)
**Responsabilidade:** Receber dados brutos (telemetria) dos sensores IoT instalados nos talhões, realizar validações de integridade e segurança (propriedade do talhão) e publicar os eventos para processamento assíncrono.

| Método | Endpoint | Descrição |
| :--- | :--- | :--- |
| **POST** | `/api/v1/leituras-sensores` | Recebe uma leitura de sensor IoT. |
| **GET** | `/api/v1/leituras-sensores` | Consulta histórico de leituras (filtros por talhão/data). |

**Exemplo de Payload (Nova Leitura):**
```json
{
  "idPropriedade": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "idTalhao": "9f2d1e56-2244-4221-a0c3-123456789abc",
  "origem": "Sensor-01",
  "dataHoraCapturaUtc": "2024-02-23T12:00:00Z",
  "metricas": {
    "umidadeSoloPercentual": 45.5,
    "temperaturaCelsius": 28.0,
    "precipitacaoMilimetros": 0
  }
}
```

**Teste Rápido (cURL):**
```bash
curl -X POST http://localhost/api/v1/leituras-sensores \
  -H "Authorization: Bearer <SEU_TOKEN>" \
  -H "Content-Type: application/json" \
  -d '...'
```

---

### 4. API de Análise (AgroSolutions.Analise)
**Responsabilidade:** Processar os dados recebidos, aplicar regras de negócio para gerar alertas (ex: seca extrema, chuva excessiva) e expor endpoints de consulta para dashboards analíticos.

| Método | Endpoint | Descrição |
| :--- | :--- | :--- |
| **GET** | `/api/v1/analise/leituras` | Consulta leituras processadas e armazenadas para análise. |
| **GET** | `/api/v1/analise/alertas` | Consulta alertas gerados pelo motor de regras. |

**Exemplo de Payload (Resposta Alerta):**
```json
[
  {
    "id": "...",
    "mensagem": "Alerta Crítico: Umidade abaixo de 20%",
    "dataHoraGeracao": "2024-02-23T12:05:00Z",
    "nivelSeveridade": "Critico"
  }
]
```

**Teste Rápido (cURL):**
```bash
curl -X GET "http://localhost/api/v1/analise/alertas?idTalhao=<GUID_TALHAO>" \
  -H "Authorization: Bearer <SEU_TOKEN>"
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
