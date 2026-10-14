# Guía Rápida de Comandos: .NET Secrets, Git y Servidor Ubuntu

---

## 1. Comandos de User Secrets (.NET)

Los User Secrets permiten guardar cadenas de conexión o claves sensibles en tu máquina de desarrollo sin subirlas al repositorio.

> **Importante:** Ejecuta estos comandos desde la raíz del proyecto o apuntando al proyecto web con `--project paginaWeb/paginaWeb.csproj`.

### Ver los secretos configurados actualmente
```powershell
dotnet user-secrets list --project paginaWeb/paginaWeb.csproj
```

### Configurar la cadena de conexión de MongoDB Atlas
```powershell
dotnet user-secrets set "MongoDB:ConnectionString" "mongodb+srv://<usuario>:<contraseña>@<cluster>.mongodb.net/portal_noticias?retryWrites=true&w=majority" --project paginaWeb/paginaWeb.csproj
```

### Configurar el nombre de la base de datos
```powershell
dotnet user-secrets set "MongoDB:DatabaseName" "portal_noticias" --project paginaWeb/paginaWeb.csproj
```

### Configurar credenciales de Cloudflare R2 (si aplica)
```powershell
dotnet user-secrets set "CloudflareR2:AccountId" "TU_ACCOUNT_ID" --project paginaWeb/paginaWeb.csproj
dotnet user-secrets set "CloudflareR2:AccessKeyId" "TU_ACCESS_KEY" --project paginaWeb/paginaWeb.csproj
dotnet user-secrets set "CloudflareR2:SecretAccessKey" "TU_SECRET_KEY" --project paginaWeb/paginaWeb.csproj
dotnet user-secrets set "CloudflareR2:BucketName" "portal-noticias" --project paginaWeb/paginaWeb.csproj
dotnet user-secrets set "CloudflareR2:PublicDomain" "https://pub-ejemplo.r2.dev" --project paginaWeb/paginaWeb.csproj
```

### Borrar un secreto específico
```powershell
dotnet user-secrets remove "MongoDB:ConnectionString" --project paginaWeb/paginaWeb.csproj
```

### Borrar todos los secretos del proyecto
```powershell
dotnet user-secrets clear --project paginaWeb/paginaWeb.csproj
```

---

## 2. Comandos Git para tu Flujo de Trabajo

### Flujo Diario de Desarrollo

```bash
# 1. Ver qué archivos has modificado o están pendientes
git status

# 2. Agregar todos los cambios realizados
git add .

# O agregar solo un archivo específico
git add ruta/al/archivo.cs

# 3. Guardar el commit con un mensaje descriptivo
git commit -m "feat: implementar nueva funcionalidad"

# 4. Enviar los cambios a la rama principal en GitHub
git push origin main
```

### Sincronización y Actualización

```bash
# Descargar e integrar los últimos cambios del repositorio remoto
git pull origin main

# Ver el historial de commits recientes (modo compacto)
git log --oneline -n 10

# Ver las diferencias exactas de lo que modificaste antes de hacer commit
git diff
```

### Deshacer Cambios y Correcciones

```bash
# Descartar cambios no guardados en un archivo (volver al último commit)
git restore nombre-archivo.cs

# Descartar todos los cambios no confirmados en el proyecto
git restore .

# Quitar archivos de la lista de preparación (unstage) sin perder lo editado
git restore --staged .

# Modificar el mensaje del último commit realizado
git commit --amend -m "nuevo mensaje para el commit"
```

---

## 3. Comandos Esenciales para Servidor Ubuntu (Linux)

### Navegación y Exploración de Carpetas

| Comando | Función |
| :--- | :--- |
| `pwd` | Muestra la ruta de la carpeta donde estás ubicado (*Print Working Directory*) |
| `ls -la` | Lista todos los archivos y carpetas, incluyendo ocultos (`.env`), con permisos y tamaño |
| `cd /var/www/portal` | Entra a la ruta especificada |
| `cd ..` | Sube un nivel hacia la carpeta superior |
| `cd ~` | Va directo a la carpeta personal del usuario actual (`/home/usuario` o `/root`) |

### Crear, Mover, Copiar y Eliminar

| Comando | Función |
| :--- | :--- |
| `mkdir -p /var/www/portal/uploads` | Crea una carpeta (con `-p` crea también las carpetas intermedias si no existen) |
| `cp .env.example .env` | Copia un archivo a un nuevo destino |
| `mv archivo.txt nuevo_nombre.txt` | Renombra o mueve un archivo de ubicación |
| `rm archivo.txt` | Elimina un archivo permanentemente |
| `rm -rf carpeta/` | Elimina una carpeta y todo su contenido de forma recursiva (**usar con cuidado**) |

### Crear y Editar Archivos con `nano`

`nano` es el editor de terminal más intuitivo y estándar en Ubuntu.

```bash
# Abrir o crear un archivo:
nano .env

# Si requiere permisos de administrador:
sudo nano /etc/nginx/sites-available/portal
```

**Atajos dentro de `nano`:**
- **Guardar cambios:** Presiona `Ctrl + O`, luego presiona `Enter`.
- **Salir del editor:** Presiona `Ctrl + X` (si hay cambios no guardados, te preguntará `Y` para sí o `N` para descartar).
- **Buscar texto:** Presiona `Ctrl + W`, escribe la palabra y pulsa `Enter`.
- **Pegar texto:** Clic derecho con el ratón o `Ctrl + Shift + V` según tu terminal.

### Ver Contenido de Archivos Rápidamente

```bash
# Ver todo el contenido de un archivo en pantalla
cat .env

# Ver las últimas 50 líneas de un archivo (ideal para logs)
tail -n 50 /var/log/nginx/error.log

# Ver un archivo de log en tiempo real mientras se escribe
tail -f /var/log/nginx/access.log
```

### Permisos de Archivos y Carpetas

```bash
# Asignar la propiedad de una carpeta al usuario actual
sudo chown -R $USER:$USER /var/www/portal

# Dar permisos de lectura y escritura a la carpeta de uploads para Docker/Nginx
sudo chmod -R 775 /var/www/portal/uploads
```

### Comandos de Docker en Ubuntu

```bash
# Ver contenedores en ejecución
docker ps

# Ver todos los contenedores (incluyendo los detenidos o con error)
docker ps -a

# Ver los logs en tiempo real de la aplicación
docker compose logs -f web

# Reiniciar el contenedor de la aplicación
docker compose restart web

# Detener los servicios
docker compose down

# Levantar los servicios en segundo plano con las variables del .env
docker compose up -d
```

### Comandos de Nginx en Ubuntu

```bash
# Verificar que la sintaxis de configuración de Nginx esté correcta (sin errores)
sudo nginx -t

# Recargar la configuración sin detener el tráfico
sudo systemctl reload nginx

# Ver el estado del servicio Nginx
sudo systemctl status nginx
```
