# Etapa de compilación
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copiar solo el .csproj y restaurar
COPY Sistemadecontrolparqueo.csproj .
RUN dotnet restore

# Copiar todo y publicar SOLO el proyecto (no la solución)
COPY . .
RUN dotnet publish Sistemadecontrolparqueo.csproj -c Release -o out

# Etapa de ejecución
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

EXPOSE 80
ENTRYPOINT ["dotnet", "Sistemadecontrolparqueo.dll"]