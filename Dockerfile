# Etapa de compilación
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar solución y .csproj para restore eficiente
COPY *.sln .
COPY Sistemadecontrolparqueo/*.csproj ./Sistemadecontrolparqueo/
RUN dotnet restore

# Copiar todo el código fuente
COPY . .

# Compilar y publicar
WORKDIR /src/Sistemadecontrolparqueo
RUN dotnet publish -c Release -o /app/publish --no-restore

# Etapa de ejecución (más ligera)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "Sistemadecontrolparqueo.dll"]