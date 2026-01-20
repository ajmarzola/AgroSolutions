# syntax=docker/dockerfile:1.6

ARG DOTNET_VERSION=8.0

# =========================
# 1) Build
# =========================
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

# Opcional: se usar feed privado, copie NuGet.config
# COPY NuGet.config ./

# Copie arquivos de solution/props primeiro (se existirem)
COPY Directory.Build.props ./
COPY Directory.Build.targets ./
COPY *.sln ./

# Copie apenas os csproj para maximizar cache do restore
COPY src/services/AgroSolutions.Analise/AgroSolutions.Analise.WebApi/*.csproj src/services/AgroSolutions.Analise/AgroSolutions.Analise.WebApi/

# COPY src/services/IdentityService/IdentityService.Application/*.csproj src/services/IdentityService/IdentityService.Application/
# COPY src/services/IdentityService/IdentityService.Domain/*.csproj src/services/IdentityService/IdentityService.Domain/
# COPY src/services/IdentityService/IdentityService.Infrastructure/*.csproj src/services/IdentityService/IdentityService.Infrastructure/
# (repita o padrão por serviço, ou use um script/templating para gerar)

RUN dotnet restore src/services/AgroSolutions.Analise/AgroSolutions.Analise.WebApi/AgroSolutions.Analise.WebApi.csproj

# Agora copie o restante do código
COPY . .

# Publicação (self-contained = false para manter imagem menor)
RUN dotnet publish src/services/AgroSolutions.Analise/AgroSolutions.Analise.WebApi/AgroSolutions.Analise.WebApi.csproj \
    -c Release -o /app/publish \
    /p:UseAppHost=false

# =========================
# 2) Runtime (imagem final)
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-alpine AS runtime
WORKDIR /app

# Cria usuário/grupo não-root
RUN addgroup -S appgrp && adduser -S appusr -G appgrp
USER appusr

# Porta padrão (ajuste se necessário)
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish ./

# Health endpoint típico: /health
ENTRYPOINT ["dotnet", "AgroSolutions.Analise.WebApi.dll"]