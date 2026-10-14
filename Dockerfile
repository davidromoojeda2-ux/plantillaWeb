# ==============================================================================
# Etapa 1: Compilación de Frontend (Tailwind CSS v4 con pnpm)
# ==============================================================================
FROM node:22-alpine AS frontend
WORKDIR /src/frontend

# Habilitar pnpm
RUN npm install -g pnpm

# Copiar manifiestos de dependencias para aprovechar caché
COPY paginaWeb/package.json paginaWeb/pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile

# Copiar estilos y vistas para que el motor de Tailwind analice las clases CSS
COPY paginaWeb/Styles ./Styles
COPY paginaWeb/Views ./Views
COPY paginaWeb/Areas ./Areas
COPY paginaWeb/ViewComponents ./ViewComponents
COPY paginaWeb/wwwroot ./wwwroot

# Compilar CSS minificado para producción
RUN pnpm run build:css

# ==============================================================================
# Etapa 2: Compilación y Publicación de .NET 10
# ==============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar archivos de proyecto y restaurar dependencias NuGet (cacheable)
COPY ["Domine/Domine.csproj", "Domine/"]
COPY ["Aplication/Aplication.csproj", "Aplication/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]
COPY ["paginaWeb/paginaWeb.csproj", "paginaWeb/"]

RUN dotnet restore "paginaWeb/paginaWeb.csproj"

# Copiar el resto del código fuente
COPY Domine/ Domine/
COPY Aplication/ Aplication/
COPY Infrastructure/ Infrastructure/
COPY paginaWeb/ paginaWeb/

# Inyectar el CSS compilado en la etapa frontend
COPY --from=frontend /src/frontend/wwwroot/css/tailwind.css ./paginaWeb/wwwroot/css/tailwind.css

# Publicar los binarios de la aplicación optimizados para producción
RUN dotnet publish "paginaWeb/paginaWeb.csproj" \
    -c Release \
    -o /app/publish \
    -p:SkipTailwind=true \
    --no-restore

# ==============================================================================
# Etapa 3: Runtime de Producción
# ==============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Puerto estándar de ASP.NET Core en .NET 8/9/10
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080

# Copiar la aplicación publicada
COPY --from=build /app/publish .

# Crear directorio para almacenamiento de imágenes local si no se usa S3/R2
RUN mkdir -p /app/wwwroot/uploads

ENTRYPOINT ["dotnet", "paginaWeb.dll"]
