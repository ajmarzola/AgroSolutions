# AgroSolutions.Propriedades

Microsserviço responsável pelo **cadastro de Propriedades, Talhões e Dispositivos IoT**. É a fonte da verdade para a estrutura física monitorada.

---

## 🚀 Como Rodar

### Pré-requisitos
- .NET 10 SDK
- SQL Server

### Executar Localmente
Na pasta `src/services/AgroSolutions.Propriedades`:

```bash
dotnet run --project AgroSolutions.Propriedades.WebApi
```

---

## 🛠️ Endpoints Principais

- `GET /api/propriedades`: Listar propriedades.
- `POST /api/propriedades`: Cadastrar nova propriedade.
- `POST /api/propriedades/{id}/talhoes`: Adicionar talhão.

Serviço utilizado para validar IDs (PropriedadeId, TalhaoId) recebidos na Ingestão.
