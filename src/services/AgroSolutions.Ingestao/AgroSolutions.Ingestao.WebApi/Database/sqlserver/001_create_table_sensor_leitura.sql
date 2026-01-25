/*
  AgroSolutions.Ingestao - SQL Server
  Tabela de leituras de sensores (simuladas) para dashboards no Grafana.

  Observação: este script é idempotente e pode ser aplicado em ambientes dev/local/prod.
*/

IF OBJECT_ID('dbo.SensorLeitura', 'U') IS NULL
BEGIN
  CREATE TABLE dbo.SensorLeitura
  (
    Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SensorLeitura PRIMARY KEY,
    IdPropriedade UNIQUEIDENTIFIER NOT NULL,
    IdTalhao UNIQUEIDENTIFIER NOT NULL,
    Origem VARCHAR(30) NOT NULL,
    DataHoraCapturaUtc DATETIME2(3) NOT NULL,
    UmidadeSoloPercentual DECIMAL(5,2) NULL,
    TemperaturaCelsius DECIMAL(5,2) NULL,
    PrecipitacaoMilimetros DECIMAL(7,2) NULL,
    IdDispositivo VARCHAR(50) NULL,
    CorrelationId VARCHAR(100) NULL
  );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SensorLeitura_Talhao_DataHora' AND object_id = OBJECT_ID('dbo.SensorLeitura'))
BEGIN
  CREATE INDEX IX_SensorLeitura_Talhao_DataHora
  ON dbo.SensorLeitura (IdTalhao, DataHoraCapturaUtc DESC)
  INCLUDE (UmidadeSoloPercentual, TemperaturaCelsius, PrecipitacaoMilimetros, Origem, IdPropriedade);
END
GO
