# Usar la imagen oficial de .NET SDK 6.0 para compilar y ejecutar la aplicación
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /app

# Copiar el archivo del proyecto y restaurar dependencias
COPY *.csproj ./
RUN dotnet restore

# Copiar el resto del proyecto y compilarlo
COPY . ./
RUN dotnet publish -c Release -o out

# Usar la imagen de .NET Runtime para ejecutar la aplicación
FROM mcr.microsoft.com/dotnet/runtime:6.0
WORKDIR /app
COPY --from=build /app/out .

# Configurar el punto de entrada
ENTRYPOINT ["dotnet", "GeneradorFacturasMasivas.dll"]
