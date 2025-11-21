FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Sistemadecontrolparqueo.sln .
COPY Sistemadecontrolparqueo/Sistemadecontrolparqueo.csproj ./Sistemadecontrolparqueo/
RUN dotnet restore

COPY . .
WORKDIR /src/Sistemadecontrolparqueo
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Sistemadecontrolparqueo.dll"]