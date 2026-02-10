# AgroSolutions.Analise

Microsserviço responsável por **consumir eventos de leituras de sensores**, persistir históricos e **executar motor de alertas** baseados em regras pré-definidas (ex: temperatura crítica).

---

## 🚀 Como Rodar

### Pré-requisitos
- .NET 8 SDK
- RabbitMQ
- SQL Server

### Executar Localmente
Na pasta `src/services/AgroSolutions.Analise`:

```bash
dotnet run --project AgroSolutions.Analise.WebApi
```

A API estará disponível em `http://localhost:5200` (ou porta configurada).

---

## ✅ Como Testar

Os testes unitários cobrem o motor de alertas e o consumidor RabbitMQ.

```bash
dotnet test ../../../tests/AgroSolutions.Analise.WebApi.Tests
```

---

## 🔗 Integração (RabbitMQ)

Este serviço consome mensagens da fila:
- **Queue**: `AgroSolutions.Analise.Leituras`
- **Exchange**: `agrosolutions` (Topic)
- **Routing Key**: `ingestao.leitura_sensor_recebida`

**Verificação**:
1. Envie uma leitura pelo serviço de Ingestão (ou Simulador).
2. Verifique os logs do Analise: `ALERTA GERADO` se a temperatura for > 35 ou < 0.

---

## 📊 Observabilidade

As métricas e logs são exportados via OpenTelemetry.

- **Grafana**: Dashboard `AgroSolutions - Analise` (se configurado).
- **Métricas Chave**: 
  - `analise_alert_processing_duration_seconds`: Tempo/Contagem de processamento de alertas.

---

## 📝 Exemplo de Payload (Evento Consumido)

O serviço espera um JSON no formato de evento publicado pela Ingestão:

```json
{
  "eventType": "LeituraSensorRecebida",
  "eventId": "d290f1ee-6c54-4b01-90e6-d701748f0851",
  "occurredAtUtc": "2024-10-02T10:00:00Z",
  "leitura": {
    "id": 1,
    "idTalhao": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "dataHoraCapturaUtc": "2024-10-02T10:00:00Z",
    "metricas": {
      "umidadeSoloPercentual": 10.5,
      "temperaturaCelsius": 42.0,
      "precipitacaoMilimetros": 0
    }
  }
}
```
