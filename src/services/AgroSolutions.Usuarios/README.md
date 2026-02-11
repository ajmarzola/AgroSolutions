# AgroSolutions.Usuarios

Microsserviço responsável pela **gestão de usuários, autenticação e autorização**. Utilitza JWT para proteção dos demais serviços, incluindo autenticação do Simulador (Machine-to-Machine).

---

## 🚀 Como Rodar

### Pré-requisitos
- .NET 10 SDK
- SQL Server (Entity Framework Core)

### Executar Localmente
Na pasta `src/services/AgroSolutions.Usuarios`:

```bash
dotnet run --project AgroSolutions.Usuarios.WebApi
```

A API estará disponível em `http://localhost:5001`.

---

## ✅ Como Testar

Os testes validação regras de registro (email único) e login.

```bash
dotnet test ../../../tests/AgroSolutions.Usuarios.WebApi.Tests
```

---

## 🔐 Autenticação

Para acessar endpoints protegidos em outros serviços, obtenha um token via endpoint `/login`.

**Fluxo**:
1. `POST /api/usuarios/registrar`: Criar usuário.
2. `POST /api/usuarios/login`: Receber token `eyJhbGciOi...`.
3. Use o header `Authorization: Bearer <token>` nas requisições.

---

## 📝 Payloads Importantes

**Registrar Usuário**:
```json
{
  "email": "produtor@exemplo.com",
  "senha": "SenhaForte123",
  "tipoId": 1
}
```

**Login**:
```json
{
  "email": "produtor@exemplo.com",
  "password": "SenhaForte123"
}
```
