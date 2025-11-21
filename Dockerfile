# Etapa de construcción
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore "Sistemadecontrolparqueo.csproj"
RUN dotnet publish "Sistemadecontrolparqueo.csproj" -c Release -o /app/out

# Etapa de ejecución
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

# Puerto (Railway lo sobrescribe, pero es buena práctica)
EXPOSE 80

# ✅ Este comando debe coincidir con `startCommand` en railway.json
ENTRYPOINT ["dotnet", "Sistemadecontrolparqueo.dll"]