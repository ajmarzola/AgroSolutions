using AgroSolutions.Analise.WebApi.Domain;
using AgroSolutions.Analise.WebApi.Infrastructure.Repositorios;

namespace AgroSolutions.Analise.WebApi.Services;

public interface IMotorDeAlertas
{
    Task<List<Alerta>> AvaliarLeituraAsync(Leitura leitura);
}

public class MotorDeAlertas : IMotorDeAlertas
{
    private readonly IAnaliseRepositorio _repositorio;

    public MotorDeAlertas(IAnaliseRepositorio repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<List<Alerta>> AvaliarLeituraAsync(Leitura leitura)
    {
        var alertas = new List<Alerta>();

        if (leitura.TemperaturaCelsius.HasValue)
        {
            if (leitura.TemperaturaCelsius > 35)
                alertas.Add(CriarAlerta(leitura, "Temperatura Crítica (> 35°C)", "Critical"));
            else if (leitura.TemperaturaCelsius < 0)
                alertas.Add(CriarAlerta(leitura, "Risco de Geada (< 0°C)", "Warning"));
        }

        if (leitura.UmidadeSoloPercentual.HasValue)
        {
            if (leitura.UmidadeSoloPercentual < 20)
                alertas.Add(CriarAlerta(leitura, "Seca Extrema (Umidade < 20%)", "Critical"));
            
            // Nova regra de negócio: Risco de Seca (Umidade < 30% nas últimas 24h)
            var ultimasLeituras = await _repositorio.GetLeiturasUltimas24HorasAsync(leitura.IdTalhao);
            if (ultimasLeituras.Any() && ultimasLeituras.All(l => l.UmidadeSoloPercentual < 30))
            {
                 alertas.Add(CriarAlerta(leitura, "Risco de Seca: Umidade abaixo de 30% por 24h", "Warning"));
            }
        }

        return alertas;
    }

    private Alerta CriarAlerta(Leitura l, string msg, string nivel)
    {
        return new Alerta
        {
            IdTalhao = l.IdTalhao,
            Mensagem = msg,
            Nivel = nivel,
            DataHoraGeracaoUtc = DateTime.UtcNow,
            LeituraId = l.Id
        };
    }
}
