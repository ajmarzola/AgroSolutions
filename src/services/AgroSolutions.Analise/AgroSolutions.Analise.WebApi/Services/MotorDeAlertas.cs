using AgroSolutions.Analise.WebApi.Domain;

namespace AgroSolutions.Analise.WebApi.Services;

public interface IMotorDeAlertas
{
    List<Alerta> AvaliarLeitura(Leitura leitura);
}

public class MotorDeAlertas : IMotorDeAlertas
{
    public List<Alerta> AvaliarLeitura(Leitura leitura)
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
