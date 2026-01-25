# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia solution + props (para restore consistente)
COPY Directory.Build.props .
COPY Directory.Build.targets .
COPY src/services/AgroSolutions.Ingestao/AgroSolutions.Ingestao.Simulador/AgroSolutions.Ingestao.Simulador.csproj src/services/AgroSolutions.Ingestao/AgroSolutions.Ingestao.Simulador/

RUN dotnet restore src/services/AgroSolutions.Ingestao/AgroSolutions.Ingestao.Simulador/AgroSolutions.Ingestao.Simulador.csproj

COPY . .
RUN dotnet publish src/services/AgroSolutions.Ingestao/AgroSolutions.Ingestao.Simulador/AgroSolutions.Ingestao.Simulador.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AgroSolutions.Ingestao.Simulador.dll"]
