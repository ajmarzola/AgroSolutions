# Evidências de Entrega - MVP AgroSolutions

Este documento serve como roteiro para validação da solução AgroSolutions pela banca avaliadora, demonstrando o cumprimento dos requisitos funcionais e técnicos.

## 1. Visão Geral da Arquitetura
A solução foi construída seguindo arquitetura de microsserviços, orquestrada via Kubernetes (ou Docker Compose para dev), com os seguintes componentes principais:
- **AgroSolutions.Usuarios**: Gestão de identidade e autenticação (JWT).
- **AgroSolutions.Propriedades**: Cadastro de propriedades e talhões.
- **AgroSolutions.Ingestao**: Recebimento de dados via API e publicação em fila (RabbitMQ).
- **AgroSolutions.Analise**: Processamento de regras de alertas (Consumer) e persistência.
- **AgroSolutions.Simulador**: Gerador de carga que autentica e envia leituras.
- **Infraestrutura**: SQL Server, RabbitMQ, Obervabilidade (OpenTelemetry/Prometheus/Grafana* - *se habilitado*).

## 2. Roteiro de Teste (Caminho Feliz)

### Pré-requisitos
- Docker & Kubernetes local (Minikube, Kind ou Docker Desktop).
- Ferramenta de requisições HTTP (Postman, curl) ou acesso ao Swagger.

### Passo 1: Inicialização da Infraestrutura
Na pasta raiz do projeto:
```bash
# Aplica os manifestos Kubernetes (Overlay Local)
kubectl apply -k infra/k8s/overlays/local

# Aguarde os pods ficarem Ready
kubectl get pods -n agrosolutions-local
```
*Correção realizada no env do overlay local para garantir injeção correta da senha do SQL Server.*

### Passo 2: Autenticação (Produtor Rural)
1. Acesse o serviço de Usuários (via NodePort ou Port-Forward).
2. **Registro**: `POST /api/usuarios/registrar`
   ```json
   { "email": "produtor@agro.com", "senha": "123", "tipoId": 1 }
   ```
3. **Login**: `POST /api/usuarios/login`
   - Resposta: `{ "token": "eyJhbGciOi..." }`
   - **Guarde este Token JWT**.

### Passo 3: Cadastro de Propriedade e Talhão
Com o Token JWT (Header `Authorization: Bearer <TOKEN>`):
1. **Criar Propriedade**: `POST /api/v1/propriedades`
   ```json
   { "nome": "Fazenda Esperança", "localizacao": "Goiás" }
   ```
   - Retorno: ID da Propriedade (guid).
2. **Criar Talhão**: `POST /api/v1/propriedades/{id}/talhoes`
   ```json
   { "nome": "Talhão Soja 01", "cultura": "Soja", "area": 50.5 }
   ```
   - Retorno: ID do Talhão (guid). **Guarde este ID**.

### Passo 4: Ingestão de Dados e Simulação
O sistema possui um **Simulador** (`AgroSolutions.Ingestao.Simulador`) que roda como container.
- Ele se autentica automaticamente (renova token se expirar).
- Envia leituras de simulação (Chuva/Seca) para a API de Ingestão.
- Validação:
  ```bash
  kubectl logs -l app=ingestao-simulador -n agrosolutions-local
  ```
  Esperado: Logs `[OK] Talhao=... Umidade=...`.

### Passo 5: Motor de Alertas (Regra de 24h)
O serviço de Análise processa mensagens do RabbitMQ.
1. **Regra**: Se a umidade < 30% por 24h (simulado via carga histórica ou frequência de envio), um alerta é gerado.
2. Verifique os logs do serviço de Análise:
   ```bash
   kubectl logs -l app=analise -n agrosolutions-local
   ```
   Esperado nas condições de seca: `ALERTA GERADO: Risco de Seca...`.

## 3. Checklist de Requisitos Atendidos

| Requisito | Status | Evidência no Código |
|-----------|--------|---------------------|
| Login/JWT | ✅ OK | `UsuariosController.cs` (BCrypt + JWT) |
| Cadastro Prop/Talhão | ✅ OK | `PropriedadesController.cs` (Vínculo com OwnerId do Token) |
| Ingestão Segura | ✅ OK | `LeiturasSensoresController.cs` (Valida Token e Propriedade do Talhão) |
| Simulador Autônomo | ✅ OK | `AuthClient.cs` (Renovação de Token e Lógica de Retry) |
| Regra de Alerta 24h | ✅ OK | `MotorDeAlertas.cs` (Consulta leituras das últimas 24h e valida consistência) |
| Infraestrutura | ✅ OK | Kubernetes, RabbitMQ e SQL Server com secrets corrigidos em `local/.env`. |

## 4. Notas de Auditoria
- Foi detectada e corrigida uma inconsistência no nome da variável de ambiente da senha do SQL Server (`password` -> `MSSQL_SA_PASSWORD`) no arquivo `.env` do overlay local, garantindo que o deployment suba corretamente.
- O código do simulador implementa corretamente a lógica de renovação de token expirado antes de falhar requisições.
