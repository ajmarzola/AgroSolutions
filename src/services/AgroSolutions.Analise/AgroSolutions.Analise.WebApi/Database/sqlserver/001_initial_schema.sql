IF OBJECT_ID('dbo.Leitura', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Leitura (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        IdTalhao UNIQUEIDENTIFIER NOT NULL,
        DataHoraCapturaUtc DATETIME2 NOT NULL,
        TemperaturaCelsius DECIMAL(5,2) NULL,
        UmidadeSoloPercentual DECIMAL(5,2) NULL,
        PrecipitacaoMilimetros DECIMAL(5,2) NULL
    );
    CREATE INDEX IX_Leitura_IdTalhao ON dbo.Leitura(IdTalhao);
END
GO

IF OBJECT_ID('dbo.Alerta', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Alerta (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        IdTalhao UNIQUEIDENTIFIER NOT NULL,
        Mensagem NVARCHAR(500) NOT NULL,
        Nivel VARCHAR(20) NOT NULL,
        DataHoraGeracaoUtc DATETIME2 NOT NULL,
        LeituraId BIGINT NULL
    );
    CREATE INDEX IX_Alerta_IdTalhao ON dbo.Alerta(IdTalhao);
END
GO
