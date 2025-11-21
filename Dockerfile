# Etapa de compilación
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copiar y restaurar
COPY *.csproj .
RUN dotnet restore

# Copiar todo y publicar
COPY . .
RUN dotnet publish -c Release -o out

# Etapa de ejecución
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

# Exponer puerto (recomendado por Railway)
EXPOSE 80

# Iniciar app
ENTRYPOINT ["dotnet", "Sistemadecontrolparqueo.dll"]