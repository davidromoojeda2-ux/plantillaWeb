# Guía de Despliegue Automatizado con Dokploy (Opción 2)
## Compilación en GitHub Actions + Ejecución Ligera en VPS

Esta arquitectura aprovecha los recursos gratuitos de los servidores de GitHub (16 GB RAM, 4 vCPUs) para compilar el backend en .NET 10, optimizar el frontend con Tailwind CSS v4 y construir la imagen Docker. Al finalizar, GitHub notifica a **Dokploy** en tu VPS mediante un **Webhook** para que únicamente descargue (`docker pull`) y ejecute la aplicación en segundos, sin consumir CPU ni memoria compilando en tu servidor.

---

## 1. Diagrama del Flujo de Trabajo

```mermaid
flowchart LR
    A[👨‍💻 Desarrollador<br/>git push main] --> B[🐙 GitHub Actions<br/>ubuntu-latest]
    
    subgraph CI["GitHub Actions (Gratuito - 16GB RAM)"]
        B --> C[Validar .NET 10 y Tailwind CSS]
        C --> D[Construir Imagen Docker]
        D --> E[Publicar en GHCR<br/>ghcr.io/.../plantillaweb:latest]
    end

    subgraph CD["VPS Ligero (Dokploy)"]
        E -. Notificación .-> F[🚀 Webhook Dokploy<br/>DOKPLOY_WEBHOOK_URL]
        F --> G[Descargar Imagen<br/>docker pull]
        G --> H[Reiniciar Contenedor<br/>Zero-Downtime]
        H --> I[🌐 Portal en Producción<br/>HTTPS con Let's Encrypt]
    end
```

---

## 2. Paso 1: Instalar Dokploy en tu Servidor VPS

Conéctate a tu VPS mediante SSH y ejecuta el instalador oficial de Dokploy (requiere Ubuntu 22.04 o 24.04):

```bash
curl -sSL https://dokploy.com/setup.sh | sh
```

El script instalará automáticamente:
- Docker y Docker Compose (si no están instalados).
- Traefik (proxy inverso con SSL automático).
- El panel de administración web de Dokploy.

Al finalizar, verás un mensaje indicando que Dokploy está listo en el puerto `3000`.

---

## 3. Paso 2: Configuración Inicial de Dokploy

1. Abre tu navegador web y entra a: `http://TU_IP_VPS:3000`
2. Registra tu cuenta de administrador con tu correo y contraseña.
3. En la barra lateral izquierda, entra en **"Projects"** (Proyectos) y haz clic en **"Create Project"**.
   - Nombre sugerido: `Portal Noticias`

---

## 4. Paso 3: Crear el Servicio de la Aplicación

Dentro del proyecto creado:
1. Haz clic en **"Create Service"** y selecciona **"Docker"** (o **"Docker Image"**).
2. Asigna un nombre al servicio: `web`
3. En la configuración del servicio (proveedor: ej. github/docker)
   - **Image Name / Docker Image:**  
     `ghcr.io/davidromoojeda2-ux/plantillaweb:latest`
En 
   - **Domain** agregar: `tu-dominio.com` (En esta sección se pone el dominio)
   - **Container Port:** `8080`

### 💡 Acceso a la Imagen en GitHub Container Registry (GHCR)

Tienes dos alternativas para que Dokploy pueda descargar tu imagen:

#### Alternativa A: Hacer el paquete público en GitHub (Más simple, recomendada Ojo:debe ser creada la imagen para que sea visible en packages y publicarla)
1. En GitHub, ve a tu perfil ➔ pestaña **"Packages"**.
2. Haz clic en el paquete **`plantillaweb`**.
3. En el menú lateral derecho, entra a **"Package settings"**.
4. Al final de la página (Danger Zone), pulsa **"Change package visibility"** y cámbialo a **Public**.
5. *¡Listo! Dokploy podrá descargar la imagen sin configurar usuarios ni contraseñas.*

#### Alternativa B: Usar credenciales privadas en Dokploy
1. Si prefieres mantener el paquete privado, en Dokploy ve a **Settings** ➔ **Registries** ➔ **Add Registry**.
2. Completa los datos:
   - **Registry URL:** `ghcr.io`
   - **Username:** `davidromoojeda2-ux`
   - **Password / Token:** Un **Personal Access Token (PAT)** de GitHub con el permiso `read:packages`.

---

## 5. Paso 4: Configurar Variables de Entorno en Dokploy

En la ficha de tu servicio `web` en Dokploy, ve a la pestaña **"Environment"** y agrega las variables requeridas por el proyecto:

```env
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080

# ─── URL Pública (robots.txt y sitemap.xml) ───────────────────────────────────
Site__BaseUrl=https://tu-dominio.com

# ─── Base de Datos MongoDB Atlas ──────────────────────────────────────────────
MongoDB__ConnectionString=mongodb+srv://davidromoojeda2_db_user:TU_PASSWORD@cluster0.cqouh1v.mongodb.net/?retryWrites=true&w=majority
MongoDB__DatabaseName=portal_noticias

# ─── Almacenamiento de Archivos ───────────────────────────────────────────────
# Opciones: Local | CloudflareR2
Storage__Provider=Local
LocalStorage__BaseUrl=/uploads

# Solo si usas Cloudflare R2:
CloudflareR2__AccountId=
CloudflareR2__AccessKeyId=
CloudflareR2__SecretAccessKey=
CloudflareR2__BucketName=portal-noticias
CloudflareR2__PublicDomain=https://pub-tu-dominio.r2.dev
```

Haz clic en **"Save"**.

---

## 6. Paso 5: Persistencia de Archivos e Imágenes (Volumen)

Si utilizas almacenamiento `Local` para las imágenes que suban los editores:
1. En la ficha de tu servicio `web` en Dokploy, ve a la pestaña **"Volumes"**.
2. Añade un nuevo volumen:
   - **Type:** `Bind Mount` o `Volume`
   - **Host Path:** `/opt/portal-noticias/uploads` (o el nombre que elijas)
   - **Mount Path (en el contenedor):** `/app/wwwroot/uploads`
3. Haz clic en **"Save"**.

> Con esto, aunque el contenedor se actualice con una nueva versión, las fotos y archivos nunca se perderán.

---

## 7. Paso 6: Configurar Dominio y Certificado SSL (HTTPS)

1. En el panel de tu proveedor de dominio (Cloudflare, Namecheap, GoDaddy, etc.):
   - Crea un registro DNS tipo **A**:
     - **Host:** `@` (o un subdominio como `noticias`)
     - **Valor/Destino:** La dirección IP pública de tu VPS.
2. En Dokploy, entra en la pestaña **"Domains"** del servicio `web`:
   - **Host:** `tu-dominio.com` (o `noticias.tu-dominio.com`)
   - **Path:** `/`
   - **Container Port:** `8080`
   - **Certificate:** Selecciona **"Let's Encrypt"**
3. Haz clic en **"Create"**.
   - Traefik configurará automáticamente el tráfico y emitirá el certificado SSL con renovación desatendida.

---

## 8. Paso 7: Activar el Despliegue Automático (Webhook)

1. En la ficha del servicio `web` en Dokploy, busca la sección **"Deployments"** o **"General"**.
2. Localiza el campo **"Deploy Webhook"** o **"Webhook URL"** y copia la dirección generada.  
   *(Tendrá un formato similar a: `http://TU_IP:3000/api/deploy?token=xxxxxxxxxxxx` o con tu dominio de Dokploy).*
3. En tu repositorio de GitHub:
   - Ve a **Settings** ➔ **Secrets and variables** ➔ **Actions**.
   - Haz clic en **"New repository secret"**.
   - **Name:** `DOKPLOY_WEBHOOK_URL`
   - **Secret:** Pega la URL del webhook que copiaste de Dokploy.
   - Haz clic en **"Add secret"**.

---

## 9. ¡Listo! ¿Cómo se realiza el despliegue a partir de ahora?

Desde tu máquina local, tu flujo diario se resume en:

```bash
git add .
git commit -m "feat: nueva funcionalidad lista para producción"
git push origin main
```

### ¿Qué sucederá automáticamente?
1. GitHub Actions compila el backend y frontend en sus servidores.
2. Construye la imagen Docker optimizada y la sube a GitHub Packages (`ghcr.io`).
3. GitHub Actions llama al Webhook de Dokploy.
4. Dokploy descarga la imagen (`docker pull`) y reinicia el servicio en 3 segundos sin interrupciones perceptibles.

---

## 10. Monitoreo y Mantenimiento desde Dokploy

- **Ver Logs en vivo:** Pestaña **"Logs"** de Dokploy para auditar peticiones o errores en tiempo real.
- **Rendimiento:** Pestaña **"Metrics"** para consultar el uso de CPU, RAM y red del contenedor.
- **Rollback instantáneo:** Si una versión falla, puedes volver a una etiqueta previa desde la interfaz web con un solo clic.
