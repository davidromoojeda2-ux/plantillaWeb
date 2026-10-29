# Flujo de trabajo completo: desarrollo, GitHub y despliegue

**Repositorio:** `https://github.com/davidromoojeda2-ux/plantillaWeb.git`  
**Worktree de referencia:** `C:\Users\GAMER\.local\share\opencode\worktree\994c49\proyecto en desarrollo`  
**Rama de referencia (ya fusionada):** `proyecto-en-desarrollo`  
**Rama para nuevas tareas:** `feature/mi-nueva-tarea` desde `origin/main`  
**Worktree principal:** `D:\proyecto-Web1\paginaWeb` (rama `main`)  
**Fecha de referencia:** 28/10/2026

> Este documento describe el flujo completo: preparación local, ramas, Pull Request, GitHub Actions, publicación de la imagen en GHCR y despliegue en el VPS.  
> Los comandos de PowerShell se ejecutan en Windows. Los comandos de VPS se ejecutan en Ubuntu/Linux.

> **Estado:** la rama `proyecto-en-desarrollo` ya fue fusionada en `main`. No sigas trabajando sobre una rama ya fusionada: para cada tarea nueva crea una rama `feature/...` desde `origin/main`.

---

## 1. Resumen del flujo

```text
Desarrollo local
      │
      ├── git add / git commit
      ├── git push origin feature/mi-nueva-tarea
      ▼
Pull Request hacia main
      │
      ├── GitHub Actions: validate-code
      ▼
Merge del PR a main
      │
      ├── GitHub Actions: validate-code
      ├── GitHub Actions: build-and-push
      │        └── Imagen en GHCR: ghcr.io/davidromoojeda2-ux/plantillaweb:latest
      └── GitHub Actions: deploy
               ├── Opción A: webhook de Dokploy
               └── Opción B: SSH + docker compose en el VPS
                        │
                        └── Smoke test HTTP
```

**Regla clave:** el despliegue automático solo se activa con un push a `main`.  
Una rama alterna publicada no despliega por sí sola: hay que fusionarla mediante Pull Request.

---

## 2. Requisitos locales

### 2.1. Software necesario

| Herramienta | Versión usada por el proyecto |
|---|---|
| Git | Cualquier versión reciente |
| .NET SDK | 10.0.x |
| Node.js | 22.x |
| pnpm | 12.11.2 |
| Docker Desktop | Opcional para probar la imagen localmente |
| GitHub CLI (`gh`) | Opcional; en esta máquina no está instalado |

### 2.2. Verificar instalaciones

```powershell
git --version
dotnet --version
node --version
npm --version
pnpm --version
docker --version
docker compose version
```

### 2.3. Instalar herramientas en Windows, si faltan

```powershell
winget install Git.Git
winget install Microsoft.DotNet.SDK.10
winget install OpenJS.NodeJS.LTS
winget install Docker.DockerDesktop
```

Instalar pnpm en la versión que usa el proyecto:

```powershell
npm install -g pnpm@12.11.2
pnpm --version
```

> Si `pnpm` no queda disponible, cierra y vuelve a abrir PowerShell.

### 2.4. Herramienta opcional: GitHub CLI

Solo es necesaria si quieres crear PR, configurar secretos o lanzar workflows desde la terminal.

```powershell
winget install --id GitHub.cli
gh --version
gh auth login
```

Si no instalas `gh`, todo el flujo se puede completar desde la web de GitHub.

---

## 3. Rutas y configuración inicial del repositorio

### 3.1. Clonar el repositorio en una máquina nueva

```powershell
git clone https://github.com/davidromoojeda2-ux/plantillaWeb.git "D:\proyecto-Web1\paginaWeb"
cd "D:\proyecto-Web1\paginaWeb"
```

Si el repositorio ya existe, entra al worktree principal:

```powershell
cd "D:\proyecto-Web1\paginaWeb"
git remote -v
git status
```

Configurar identidad de Git, si aún no está configurada:

```powershell
git config --global user.name "Tu Nombre"
git config --global user.email "tu-correo@ejemplo.com"
```

### 3.2. Estructura relevante

```text
paginaWeb/
├── Aplication/
├── Domine/
├── Infrastructure/
├── paginaWeb/                 # Proyecto web ASP.NET Core
│   ├── Styles/tailwind.css
│   ├── wwwroot/
│   └── Views/
├── tests/ContenidoBloques.Tests/
├── deploy/
│   ├── DESPLIEGUE_DOKPLOY.md
│   ├── GUIA_COMANDOS.md
│   └── nginx-portal.conf.example
├── .github/workflows/deploy.yml
├── Dockerfile
├── docker-compose.yml
├── .env.example
└── paginaWeb.slnx
```

### 3.3. Crear el worktree de trabajo

El worktree principal se queda en `main`. Para trabajar en una rama alterna se puede crear un worktree separado. El worktree de este repositorio ya existe; el siguiente comando es el que se usó para crearlo y sirve como referencia:

```powershell
cd "D:\proyecto-Web1\paginaWeb"
git fetch origin --prune
git worktree add -b proyecto-en-desarrollo "C:\Users\GAMER\.local\share\opencode\worktree\994c49\proyecto en desarrollo" origin/main
```

> No ejecutes `git worktree add` de nuevo para la misma ruta. Para una tarea nueva, usa `git switch -c feature/... origin/main` dentro del worktree existente (apartado 5.1) o crea otro worktree con una ruta distinta.

Entrar al worktree:

```powershell
cd "C:\Users\GAMER\.local\share\opencode\worktree\994c49\proyecto en desarrollo"
git status
git branch --show-current
```

Ver todos los worktrees:

```powershell
git worktree list
```

Renombrar o mover un worktree:

```powershell
git worktree move "C:\ruta\actual\del\worktree" "C:\ruta\nueva\del\worktree"
```

Eliminar un worktree que ya no se usa:

```powershell
git worktree remove "C:\ruta\del\worktree"
git worktree prune
```

> **Importante:** una misma rama no debería estar abierta en dos worktrees al mismo tiempo.  
> Las rutas con espacios deben ir siempre entre comillas.  
> En Windows, `git worktree move` puede fallar con `Permission denied` si OpenCode, un IDE o un proceso tienen abierta la carpeta. Cierra esos procesos y vuelve a intentarlo.

### 3.4. Abrir el worktree correcto en el IDE

La terminal y el IDE son contextos distintos. Si la terminal está en `...\proyecto en desarrollo` pero el IDE muestra `paginaWeb main`, el workspace abierto sigue siendo el worktree principal `D:\proyecto-Web1\paginaWeb`.

Abrir la carpeta correcta en el IDE:

1. `File` → `Open Folder...`
2. Seleccionar `C:\Users\GAMER\.local\share\opencode\worktree\994c49\proyecto en desarrollo`
3. Abrir en una ventana nueva.

O desde PowerShell, si tienes el comando `code` disponible:

```powershell
code "C:\Users\GAMER\.local\share\opencode\worktree\994c49\proyecto en desarrollo"
```

Verificar que el worktree y la rama sean los correctos:

```powershell
git rev-parse --show-toplevel
git branch --show-current
git worktree list
```

Resultado esperado:

```text
C:/Users/GAMER/.local/share/opencode/worktree/994c49/proyecto en desarrollo
proyecto-en-desarrollo
```

Y en `git worktree list`:

```text
D:/proyecto-Web1/paginaWeb                                                  6b896dd [main]
C:/Users/GAMER/.local/share/opencode/worktree/994c49/proyecto en desarrollo 257fef8 [proyecto-en-desarrollo]
```

> Si ya creaste una rama nueva, `git branch --show-current` mostrará esa rama (`feature/...`), no `proyecto-en-desarrollo`.  
> El `cd` de la terminal no cambia la carpeta abierta en el IDE. El indicador de rama del IDE refleja el workspace, no la ruta de la terminal.  
> No ejecutes `git switch main` dentro del worktree alterno: `main` ya está abierto en el worktree principal y Git lo impedirá.

---

## 4. Configuración local de la aplicación

### 4.1. Restaurar dependencias .NET

```powershell
cd "C:\Users\GAMER\.local\share\opencode\worktree\994c49\proyecto en desarrollo"
dotnet restore paginaWeb.slnx
```

### 4.2. Configurar secretos locales con User Secrets

Los secretos locales no se suben al repositorio.

```powershell
dotnet user-secrets set "MongoDB:ConnectionString" "mongodb+srv://<usuario>:<contraseña>@<cluster>.mongodb.net/portal_noticias?retryWrites=true&w=majority" --project paginaWeb/paginaWeb.csproj
dotnet user-secrets set "MongoDB:DatabaseName" "portal_noticias" --project paginaWeb/paginaWeb.csproj
```

Si usas Cloudflare R2:

```powershell
dotnet user-secrets set "CloudflareR2:AccountId" "TU_ACCOUNT_ID" --project paginaWeb/paginaWeb.csproj
dotnet user-secrets set "CloudflareR2:AccessKeyId" "TU_ACCESS_KEY" --project paginaWeb/paginaWeb.csproj
dotnet user-secrets set "CloudflareR2:SecretAccessKey" "TU_SECRET_KEY" --project paginaWeb/paginaWeb.csproj
dotnet user-secrets set "CloudflareR2:BucketName" "portal-noticias" --project paginaWeb/paginaWeb.csproj
dotnet user-secrets set "CloudflareR2:PublicDomain" "https://pub-tu-dominio.r2.dev" --project paginaWeb/paginaWeb.csproj
```

Verificar secretos configurados:

```powershell
dotnet user-secrets list --project paginaWeb/paginaWeb.csproj
```

### 4.3. Instalar dependencias frontend y compilar Tailwind CSS

```powershell
cd "C:\Users\GAMER\.local\share\opencode\worktree\994c49\proyecto en desarrollo\paginaWeb"
pnpm install --frozen-lockfile
pnpm run build:css
```

Durante desarrollo, para recompilar el CSS automáticamente:

```powershell
pnpm run watch:css
```

### 4.4. Compilar la solución

```powershell
cd "C:\Users\GAMER\.local\share\opencode\worktree\994c49\proyecto en desarrollo"
dotnet build paginaWeb.slnx -c Release --no-restore -p:SkipTailwind=true
```

### 4.5. Ejecutar pruebas

```powershell
dotnet test tests/ContenidoBloques.Tests/ContenidoBloques.Tests.csproj -c Release
```

> El workflow actual de GitHub Actions compila y valida estilos, pero no ejecuta esta prueba automáticamente. Ejecutarla localmente antes del PR es recomendable.

### 4.6. Ejecutar la aplicación en local

```powershell
cd "C:\Users\GAMER\.local\share\opencode\worktree\994c49\proyecto en desarrollo"
dotnet run --project paginaWeb/paginaWeb.csproj
```

Con recarga automática:

```powershell
dotnet watch --project paginaWeb/paginaWeb.csproj
```

Abrir en el navegador la URL que indique la consola, normalmente:

```text
http://localhost:5000
```

---

## 5. Flujo diario con Git

> En los ejemplos, `feature/mi-nueva-tarea` es un nombre de ejemplo. Sustitúyelo por el nombre real de tu rama.

### 5.1. Crear una rama nueva para cada tarea

La rama `proyecto-en-desarrollo` ya fue fusionada. No sigas trabajando sobre una rama ya fusionada. Para cada tarea nueva, crea una rama desde `origin/main`.

Reutilizando el worktree de desarrollo:

```powershell
cd "C:\Users\GAMER\.local\share\opencode\worktree\994c49\proyecto en desarrollo"
git rev-parse --show-toplevel
git status
git fetch origin --prune
git switch -c feature/mi-nueva-tarea origin/main
git branch --show-current
```

Qué hace:

- `git fetch origin --prune`: actualiza las referencias remotas.
- `git switch -c feature/mi-nueva-tarea`: crea y cambia a una rama nueva.
- `origin/main`: indica que la rama nueva debe partir de la última versión de `main`.

Si prefieres una carpeta separada para cada tarea, crea otro worktree desde el worktree principal:

```powershell
cd "D:\proyecto-Web1\paginaWeb"
git fetch origin --prune
git worktree add -b feature/mi-nueva-tarea "C:\ruta\worktrees\feature-mi-nueva-tarea" origin/main
```

> No uses `git switch main` dentro del worktree alterno: `main` ya está abierto en `D:\proyecto-Web1\paginaWeb` y Git lo impedirá.  
> Si tienes cambios sin commit, commitéalos o guárdalos con `git stash` antes de cambiar de rama.  
> Si la rama ya existe, usa `git switch feature/mi-nueva-tarea` en lugar de `git switch -c`.  
> Para actualizar una rama existente con los cambios de `main`, usa `git merge origin/main` o `git rebase origin/main`.

### 5.2. Actualizar la base antes de trabajar

```powershell
cd "C:\Users\GAMER\.local\share\opencode\worktree\994c49\proyecto en desarrollo"
git fetch origin --prune
git status
git log --oneline --decorate -5
```

Comprobar si la rama está detrás de `main`:

```powershell
git rev-list --left-right --count origin/main...HEAD
```

Lectura del resultado:

```text
0    9
│    └── commits que tu rama tiene por delante de origin/main
└─────── commits de origin/main que te faltan
```

Si el primer número es mayor que `0`, actualiza la rama antes del PR:

```powershell
git rebase origin/main
```

Si hay conflictos, resuélvelos, agrega los archivos y continúa:

```powershell
git add .
git rebase --continue
```

> No uses `git rebase` sobre una rama compartida por otras personas sin coordinarlo. Para una rama personal de feature es el flujo habitual.

### 5.3. Revisar cambios

```powershell
git status
git diff
git diff --staged
```

### 5.4. Crear el commit

```powershell
git add .
git commit -m "feat: descripción clara del cambio"
```

Buenas prácticas para el mensaje:

```text
feat: nueva funcionalidad
fix: corrección de error
refactor: reorganización sin cambiar comportamiento
docs: documentación
chore: mantenimiento o dependencias
test: pruebas
```

### 5.5. Publicar la rama

```powershell
git push -u origin feature/mi-nueva-tarea
```

Verificar la rama remota:

```powershell
git branch -vv
git ls-remote --heads origin
```

### 5.6. Crear el Pull Request

Desde la web:

```text
https://github.com/davidromoojeda2-ux/plantillaWeb/pull/new/feature/mi-nueva-tarea
```

O comparar contra `main`:

```text
https://github.com/davidromoojeda2-ux/plantillaWeb/compare/main...feature/mi-nueva-tarea?expand=1
```

Con GitHub CLI, si está instalado:

```powershell
gh pr create --base main --head feature/mi-nueva-tarea --title "feat: descripción del cambio" --body "Descripción detallada del cambio."
gh pr checks --watch
gh pr view --web
```

### 5.7. Fusionar el Pull Request

Desde la web de GitHub:

1. Abrir el PR.
2. Revisar la pestaña **Files changed**.
3. Esperar a que **validate-code** termine en verde.
4. Pulsar **Merge pull request**.
5. Confirmar el merge.

Con GitHub CLI:

```powershell
gh pr merge --merge
```

> El merge a `main` es el evento que dispara la construcción de la imagen Docker y el despliegue.

También puedes integrar sin PR desde el worktree principal. Es el flujo que se usó para fusionar `proyecto-en-desarrollo`:

```powershell
cd "D:\proyecto-Web1\paginaWeb"
git switch main
git fetch origin --prune
git pull --ff-only origin main
git merge --no-ff feature/mi-nueva-tarea -m "merge: integra feature/mi-nueva-tarea en main"
git push origin main
git log --oneline --graph --decorate -5
```

Ejemplo histórico con la rama ya fusionada:

```powershell
git merge --no-ff proyecto-en-desarrollo -m "merge: integra proyecto-en-desarrollo en main"
git push origin main
```

> `--no-ff` crea un commit de merge y conserva el historial de la rama.  
> El `git push origin main` es el que dispara GitHub Actions, la imagen Docker y el despliegue.  
> No ejecutes `git switch main` dentro del worktree alterno: hazlo en `D:\proyecto-Web1\paginaWeb`.

### 5.8. Actualizar `main` después del merge

```powershell
cd "D:\proyecto-Web1\paginaWeb"
git switch main
git pull --ff-only origin main
git log --oneline -5
```

### 5.9. Limpiar la rama y el worktree

Si vas a reutilizar el worktree de desarrollo, **no lo elimines**. Basta con cambiar a una rama nueva desde `origin/main` siguiendo el apartado 5.1. Elimina el worktree solo si quieres una carpeta nueva por tarea.

Primero salir del worktree y eliminarlo:

```powershell
cd "D:\proyecto-Web1\paginaWeb"
git worktree remove "C:\Users\GAMER\.local\share\opencode\worktree\994c49\proyecto en desarrollo"
```

Si el worktree tiene cambios sin commit, Git pedirá confirmación. No uses `--force` salvo que estés seguro:

```powershell
git worktree remove --force "C:\Users\GAMER\.local\share\opencode\worktree\994c49\proyecto en desarrollo"
```

Eliminar la rama local ya fusionada:

```powershell
git branch -d feature/mi-nueva-tarea
```

Eliminar la rama remota:

```powershell
git push origin --delete feature/mi-nueva-tarea
```

Limpiar referencias de worktrees que ya no existen:

```powershell
git worktree prune
```

---

## 6. Renombrar una rama ya publicada

Si necesitas cambiar el nombre de una rama local y remota:

```powershell
git branch -m nombre-nuevo
git push -u origin nombre-nuevo
git push origin --delete nombre-anterior
```

Ejemplo histórico aplicado a este repositorio (la rama ya fue fusionada):

```powershell
git branch -m proyecto-en-desarrollo
git push -u origin proyecto-en-desarrollo
git push origin --delete refactor/bloques-articulo
```

Verificar:

```powershell
git branch -vv
git ls-remote --heads origin
```

> Antes de borrar la rama remota anterior, comprueba que no tenga un Pull Request abierto.

---

## 7. GitHub Actions: qué ocurre en cada evento

Workflow:

```text
.github/workflows/deploy.yml
```

### 7.1. Disparadores

| Evento | Qué ejecuta |
|---|---|
| Pull Request hacia `main` | `validate-code` |
| Push a `main` | `validate-code` → `build-and-push` → `deploy` |
| `workflow_dispatch` manual | Depende de la rama seleccionada; para desplegar debe ser `main` |

### 7.2. Job `validate-code`

Se ejecuta en `ubuntu-latest` y hace:

```text
pnpm install --frozen-lockfile
pnpm run build:css
dotnet restore paginaWeb.slnx
dotnet build paginaWeb.slnx -c Release --no-restore -p:SkipTailwind=true
```

### 7.3. Job `build-and-push`

Solo se ejecuta si:

```yaml
if: github.ref == 'refs/heads/main'
```

Hace:

1. Configura Docker Buildx.
2. Inicia sesión en `ghcr.io` con `GITHUB_TOKEN`.
3. Construye la imagen usando `./Dockerfile`.
4. Publica la imagen con dos etiquetas:
   - `latest`
   - `sha-<commit-corto>`

Imagen resultante:

```text
ghcr.io/davidromoojeda2-ux/plantillaweb:latest
ghcr.io/davidromoojeda2-ux/plantillaweb:sha-<commit-corto>
```

### 7.4. Job `deploy`

Solo se ejecuta en `main`. Tiene dos modos:

1. **Dokploy:** si existe el secreto `DOKPLOY_WEBHOOK_URL`, llama al webhook.
2. **SSH directo:** si no hay webhook y existe `SSH_HOST`, copia `docker-compose.yml` y `.env.example` a `/opt/portal-noticias`, descarga la imagen, levanta los contenedores y ejecuta un smoke test.

Si no configuras ninguno de los dos, el job puede terminar en verde sin desplegar nada. Configura al menos una opción.

### 7.5. Ver el workflow en GitHub

```text
https://github.com/davidromoojeda2-ux/plantillaWeb/actions/workflows/deploy.yml
```

Con GitHub CLI, si está instalado:

```powershell
gh run list --workflow=deploy.yml --limit 10
gh run watch <run-id>
gh run view <run-id> --log
```

### 7.6. Ejecutar el workflow manualmente

Desde la web:

1. Abrir **Actions**.
2. Seleccionar **CI/CD Pipeline - Validacion, Docker y Despliegue VPS**.
3. Pulsar **Run workflow**.
4. Seleccionar la rama `main`.
5. Ejecutar.

Con GitHub CLI:

```powershell
gh workflow run deploy.yml --ref main
gh run list --workflow=deploy.yml --limit 5
```

> Si ejecutas el workflow manualmente desde una rama distinta de `main`, los jobs de build y deploy se omiten por la condición del workflow.

---

## 8. Configurar los secretos de GitHub

Ruta web:

```text
https://github.com/davidromoojeda2-ux/plantillaWeb/settings/secrets/actions
```

### 8.1. Secretos necesarios

| Secreto | Obligatorio | Uso |
|---|---|---|
| `DOKPLOY_WEBHOOK_URL` | Solo si usas Dokploy | URL del webhook de despliegue |
| `SSH_HOST` | Solo si usas SSH directo | IP o dominio del VPS |
| `SSH_USER` | Solo si usas SSH directo | Usuario SSH |
| `SSH_PRIVATE_KEY` | Solo si usas SSH directo | Clave privada SSH completa |
| `SSH_PORT` | Opcional | Puerto SSH; por defecto `22` |
| `GITHUB_TOKEN` | Automático | Lo inyecta GitHub Actions; no se crea a mano |

### 8.2. Configurar secretos desde la web

Para cada secreto:

1. **Settings** → **Secrets and variables** → **Actions**.
2. **New repository secret**.
3. Escribir el nombre exacto.
4. Pegar el valor.
5. **Add secret**.

### 8.3. Configurar secretos con GitHub CLI

Si instalaste `gh` y autenticaste:

```powershell
gh secret set DOKPLOY_WEBHOOK_URL --body "https://tu-dokploy.example.com/api/deploy?token=..."
gh secret set SSH_HOST --body "203.0.113.10"
gh secret set SSH_USER --body "deploy"
gh secret set SSH_PORT --body "22"
Get-Content "$HOME\.ssh\gh_actions_deploy" -Raw | gh secret set SSH_PRIVATE_KEY
gh secret list
```

### 8.4. Crear una clave SSH para GitHub Actions

En local:

```powershell
ssh-keygen -t ed25519 -C "github-actions-plantillaweb" -f "$HOME\.ssh\gh_actions_deploy"
```

Copiar la clave pública al VPS:

```powershell
Get-Content "$HOME\.ssh\gh_actions_deploy.pub" | ssh <usuario>@<host> "mkdir -p ~/.ssh && cat >> ~/.ssh/authorized_keys && chmod 700 ~/.ssh && chmod 600 ~/.ssh/authorized_keys"
```

Guardar la clave **privada** en el secreto `SSH_PRIVATE_KEY`:

```powershell
Get-Content "$HOME\.ssh\gh_actions_deploy" -Raw
```

Copia todo el contenido, incluidas las líneas:

```text
-----BEGIN OPENSSH PRIVATE KEY-----
...
-----END OPENSSH PRIVATE KEY-----
```

> Nunca subas la clave privada al repositorio ni la pegues en un archivo versionado.

---

## 9. Opción A: despliegue con Dokploy (recomendada)

Dokploy recibe la imagen ya construida desde GitHub y solo la descarga y reinicia el contenedor. Es la opción más liviana para el VPS.

### 9.1. Instalar Dokploy en el VPS

Conéctate por SSH:

```bash
ssh <usuario>@<host>
```

Instalar Dokploy en Ubuntu 22.04/24.04:

```bash
curl -sSL https://dokploy.com/setup.sh | sh
```

Al finalizar, abrir el panel:

```text
http://<IP_DEL_VPS>:3000
```

Crear la cuenta de administrador.

### 9.2. Crear el proyecto y el servicio

1. **Projects** → **Create Project**.
2. Nombre sugerido: `Portal Noticias`.
3. Dentro del proyecto: **Create Service** → **Docker** o **Docker Image**.
4. Nombre del servicio: `web`.
5. Imagen Docker:

```text
ghcr.io/davidromoojeda2-ux/plantillaweb:latest
```

6. Puerto del contenedor:

```text
8080
```

### 9.3. Dar acceso a la imagen de GHCR

**Opción A: paquete público**

1. GitHub → tu perfil → **Packages**.
2. Abrir el paquete `plantillaweb`.
3. **Package settings** → **Change package visibility** → **Public**.

**Opción B: paquete privado**

1. Crear un Personal Access Token (PAT) con permiso `read:packages`.
2. En Dokploy: **Settings** → **Registries** → **Add Registry**.
3. Registry URL:

```text
ghcr.io
```

4. Username:

```text
davidromoojeda2-ux
```

5. Password: el PAT con `read:packages`.

### 9.4. Variables de entorno en Dokploy

En el servicio `web`, pestaña **Environment**:

```env
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080

MongoDB__ConnectionString=mongodb+srv://<usuario>:<contraseña>@<cluster>.mongodb.net/?retryWrites=true&w=majority
MongoDB__DatabaseName=portal_noticias

Storage__Provider=Local
LocalStorage__BaseUrl=/uploads
```

#### ¿Es obligatorio configurar S3 o Cloudflare R2?
**No, es circunstancial (100% opcional).**  
El backend (`ImageService.cs`) cuenta con detección automática y contingencia (*fallback*):
1. Al subir una imagen, el servicio verifica disponibilidad con `S3Storage.IsAvailableAsync()`.
2. Si no hay credenciales configuradas para S3 o Cloudflare R2, el método retorna `false` de inmediato sin demoras ni errores.
3. El sistema conmuta automáticamente a **almacenamiento local** y guarda las imágenes en el volumen del VPS.

> Solo si en el futuro decides externalizar las fotos a la nube (para no ocupar espacio en el disco del VPS), agregas en **Environment** las variables de Cloudflare R2:
> ```env
> Storage__Provider=CloudflareR2
> CloudflareR2__AccountId=<account-id>
> CloudflareR2__AccessKeyId=<access-key>
> CloudflareR2__SecretAccessKey=<secret-key>
> CloudflareR2__BucketName=portal-noticias
> CloudflareR2__PublicDomain=https://pub-tu-dominio.r2.dev
> ```

Guardar los cambios.

### 9.5. Volumen para persistencia de imágenes y estructura de `uploads`

Si utilizas almacenamiento en el servidor (opción por defecto):

#### 1. Crear la carpeta y asignar permisos en el VPS (SSH)
Antes de mapear en Dokploy, crea la carpeta en el disco del servidor para evitar problemas de permisos:
```bash
sudo mkdir -p /opt/portal-noticias/uploads
sudo chown -R $USER:$USER /opt/portal-noticias/uploads
sudo chmod -R 775 /opt/portal-noticias/uploads
```

#### 2. Configurar el volumen en Dokploy
1. En el servicio `web`, entrar a la pestaña **Volumes**.
2. Añadir un volumen (*Bind Mount*):
   * **Host Path:** `/opt/portal-noticias/uploads`  
     *(Es la carpeta física real en el disco duro del VPS. Como los contenedores Docker son efímeros y se destruyen en cada despliegue, esta ruta garantiza que las fotos **nunca se pierdan** al actualizar la aplicación).*
   * **Mount Path:** `/app/wwwroot/uploads`  
     *(Es la ruta interna del contenedor donde ASP.NET Core maneja los estáticos de `wwwroot`. Dokploy crea un puente directo: todo lo que .NET escribe aquí se guarda en el Host Path del VPS).*
3. Guardar cambios.

#### 3. ¿Cómo organiza la aplicación las imágenes dentro de `uploads`?
El servicio `ImageService.cs` optimiza cada imagen subida:
* **Conversión a WebP:** Compresión con calidad 80 y eliminación de metadatos EXIF por privacidad.
* **Variantes responsivas:** Genera resoluciones automáticas de 400px (móvil), 800px (tablet) y 1600px (desktop).
* **Estructura en disco:**
  ```text
  /opt/portal-noticias/uploads/
  └── images/
      └── {AÑO}/
          └── {MES}/
              ├── {nombre-limpio}-{hash8}-400.webp
              ├── {nombre-limpio}-{hash8}-800.webp
              └── {nombre-limpio}-{hash8}-1600.webp
  ```
* **Acceso público:** Gracias a `LocalStorage__BaseUrl=/uploads`, las fotos quedan disponibles en la web bajo la URL:  
  `https://tu-dominio.com/uploads/images/{AÑO}/{MES}/{archivo}.webp`

### 9.6. Dominio y HTTPS

1. En el proveedor de dominio, crear un registro DNS tipo `A`:
   - Host: `@` o `noticias`
   - Valor: IP pública del VPS
2. En Dokploy, pestaña **Domains**:
   - Host: `tu-dominio.com`
   - Path: `/`
   - Container Port: `8080`
   - Certificate: `Let's Encrypt`
3. Crear el dominio y esperar la emisión del certificado.

### 9.7. Activar el webhook

1. En Dokploy, abrir el servicio `web`.
2. Buscar **Deploy Webhook** o **Webhook URL**.
3. Copiar la URL.
4. En GitHub, crear el secreto:

```text
DOKPLOY_WEBHOOK_URL
```

5. Pegar la URL del webhook.

### 9.8. Desplegar

Desde el worktree:

```powershell
cd "C:\Users\GAMER\.local\share\opencode\worktree\994c49\proyecto en desarrollo"
git status
git add .
git commit -m "feat: cambio listo para producción"
git push origin feature/mi-nueva-tarea
```

Crear el PR, fusionarlo a `main` y esperar a que GitHub Actions termine.

Después del merge, el flujo automático es:

```text
push a main
  → validate-code
  → build-and-push
  → GHCR: ghcr.io/davidromoojeda2-ux/plantillaweb:latest
  → webhook Dokploy
  → docker pull + restart
  → smoke test
```

### 9.9. Verificar el despliegue

En Dokploy:

1. Abrir el servicio `web`.
2. Pestaña **Logs**.
3. Revisar que el contenedor haya iniciado sin errores.
4. Abrir el dominio y comprobar la aplicación.

Desde el VPS:

```bash
curl -I https://tu-dominio.com/
docker ps
docker logs --tail=100 <nombre-del-contenedor>
```

---

## 10. Opción B: despliegue por SSH directo (fallback)

Esta opción la usa el workflow cuando `DOKPLOY_WEBHOOK_URL` está vacío y `SSH_HOST` tiene valor.

### 10.1. Preparar el VPS

Conéctate:

```bash
ssh <usuario>@<host>
```

Instalar Docker y Docker Compose, si no están:

```bash
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker "$USER"
newgrp docker
docker --version
docker compose version
```

Crear la carpeta de despliegue:

```bash
sudo mkdir -p /opt/portal-noticias/uploads
sudo chown -R "$USER":"$USER" /opt/portal-noticias
cd /opt/portal-noticias
```

Crear el archivo `.env`:

```bash
cp .env.example .env
nano .env
chmod 600 .env
```

Contenido mínimo recomendado:

```env
DOCKER_IMAGE=ghcr.io/davidromoojeda2-ux/plantillaweb:latest
HOST_PORT=5000

MONGO_CONNECTION_STRING=mongodb+srv://<usuario>:<contraseña>@<cluster>.mongodb.net/portal_noticias?retryWrites=true&w=majority
MONGO_DB=portal_noticias

STORAGE_PROVIDER=Local
R2_ACCOUNT_ID=
R2_ACCESS_KEY_ID=
R2_SECRET_ACCESS_KEY=
R2_BUCKET_NAME=portal-noticias
R2_PUBLIC_DOMAIN=
```

> El `docker-compose.yml` actual conecta `Storage__Provider`, `LocalStorage__BaseUrl` y las variables de Cloudflare R2. Si vas a usar AWS S3/MinIO, revisa y ajusta el mapeo de variables antes de desplegar.

### 10.2. Probar el despliegue manual en el VPS

```bash
cd /opt/portal-noticias
docker login ghcr.io -u davidromoojeda2-ux
docker compose pull web
docker compose up -d --remove-orphans
docker compose ps
curl -I http://127.0.0.1:5000/
docker compose logs --tail=100 web
```

### 10.3. Configurar Nginx

Copiar la plantilla:

```bash
sudo cp /opt/portal-noticias/deploy/nginx-portal.conf.example /etc/nginx/sites-available/portal-noticias
sudo nano /etc/nginx/sites-available/portal-noticias
```

Ajustar `server_name` y el puerto si cambiaste `HOST_PORT`.

Habilitar el sitio:

```bash
sudo ln -s /etc/nginx/sites-available/portal-noticias /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

### 10.4. Configurar HTTPS con Let's Encrypt

```bash
sudo apt update
sudo apt install -y certbot python3-certbot-nginx
sudo certbot --nginx -d tu-dominio.com -d www.tu-dominio.com
```

Comprobar renovación:

```bash
sudo certbot renew --dry-run
```

### 10.5. Configurar secretos SSH en GitHub

Crear el secreto `SSH_HOST`, `SSH_USER`, `SSH_PRIVATE_KEY` y, si aplica, `SSH_PORT`.

El workflow se encargará de:

1. Copiar `docker-compose.yml` y `.env.example` a `/opt/portal-noticias`.
2. Crear `.env` si no existe.
3. Autenticarse en GHCR.
4. Ejecutar `docker compose pull web`.
5. Ejecutar `docker compose up -d --remove-orphans`.
6. Hacer un smoke test contra `http://127.0.0.1:$HOST_PORT/`.
7. Limpiar imágenes antiguas con `docker image prune -f`.

### 10.6. Verificar el smoke test

El workflow acepta como respuesta válida:

```text
200, 301 o 302
```

Si falla, revisa:

```bash
cd /opt/portal-noticias
docker compose ps
docker compose logs --tail=100 web
docker compose config
curl -v http://127.0.0.1:5000/
```

---

## 11. Comandos completos: de local a producción

### 11.1. Camino rápido para una tarea nueva

```powershell
# 1. Entrar al worktree
cd "C:\Users\GAMER\.local\share\opencode\worktree\994c49\proyecto en desarrollo"

# 2. Verificar que estás en el worktree correcto
git rev-parse --show-toplevel
git branch --show-current

# 3. Actualizar referencias y crear la rama de la tarea desde main
git fetch origin --prune
git switch -c feature/mi-nueva-tarea origin/main

# 4. Compilar y probar localmente
git status
git rev-list --left-right --count origin/main...HEAD
cd paginaWeb
pnpm install --frozen-lockfile
pnpm run build:css
cd ..
dotnet restore paginaWeb.slnx
dotnet build paginaWeb.slnx -c Release --no-restore -p:SkipTailwind=true
dotnet test tests/ContenidoBloques.Tests/ContenidoBloques.Tests.csproj -c Release

# 5. Publicar la rama
git push -u origin feature/mi-nueva-tarea

# 6. Crear el PR desde la web
# https://github.com/davidromoojeda2-ux/plantillaWeb/pull/new/feature/mi-nueva-tarea
```

### 11.2. Integrar y desplegar

Si fusionas con un Pull Request, actualiza `main` local después del merge:

```powershell
cd "D:\proyecto-Web1\paginaWeb"
git switch main
git pull --ff-only origin main
```

Si haces el merge localmente sin PR, integra la rama y publica `main`:

```powershell
cd "D:\proyecto-Web1\paginaWeb"
git switch main
git fetch origin --prune
git pull --ff-only origin main
git merge --no-ff feature/mi-nueva-tarea -m "merge: integra feature/mi-nueva-tarea en main"
git push origin main
```

Comprobar que el workflow se disparó:

```text
https://github.com/davidromoojeda2-ux/plantillaWeb/actions/workflows/deploy.yml
```

Si usas GitHub CLI:

```powershell
gh run list --workflow=deploy.yml --limit 5
```

### 11.3. Reejecutar un despliegue manualmente

```powershell
gh workflow run deploy.yml --ref main
```

O desde la web: **Actions** → workflow → **Run workflow** → rama `main`.

---

## 12. Verificación y diagnóstico

### 12.1. Verificar el estado de Git

```powershell
git rev-parse --show-toplevel
git branch --show-current
git status
git branch -vv
git worktree list
git log --oneline --graph --decorate -10
```

Si `git rev-parse --show-toplevel` devuelve `D:\proyecto-Web1\paginaWeb`, estás en el worktree principal, no en el worktree de desarrollo. Si el IDE muestra `main` pero la terminal está en `...\proyecto en desarrollo`, el workspace abierto en el IDE es el principal: abre la carpeta del worktree como se explica en el apartado 3.4.

### 12.2. Verificar que la rama está lista para el PR

```powershell
git fetch origin --prune
git rev-list --left-right --count origin/main...HEAD
```

Interpretación:

```text
0    9   → lista para PR (0 detrás, 9 adelante)
2    5   → hay 2 commits de main que faltan; hacer rebase
```

### 12.3. Verificar la imagen en GHCR

```powershell
docker pull ghcr.io/davidromoojeda2-ux/plantillaweb:latest
docker image inspect ghcr.io/davidromoojeda2-ux/plantillaweb:latest
```

En GitHub:

```text
https://github.com/davidromoojeda2-ux?tab=packages
```

### 12.4. Diagnóstico de GitHub Actions

| Síntoma | Causa probable | Solución |
|---|---|---|
| `build-and-push` se omite | El workflow se ejecutó en un PR o en una rama distinta de `main` | Fusionar a `main` o lanzar el workflow sobre `main` |
| `deploy` se omite | Misma causa | Ejecutar sobre `main` |
| `deploy` termina sin desplegar | No hay `DOKPLOY_WEBHOOK_URL` ni `SSH_HOST` | Configurar al menos una opción |
| Error al descargar la imagen | Paquete GHCR privado sin credenciales | Hacer público el paquete o configurar PAT/registry |
| Smoke test falla | Contenedor no inicia, puerto o variables incorrectas | Revisar `docker compose logs web` y `.env` |
| Error de conexión a MongoDB | Cadena de conexión o IP no permitida en Atlas | Revisar `MongoDB__ConnectionString` y Network Access |
| `pnpm install` falla | Lockfile o versión de pnpm distinta | Usar `pnpm@12.11.2` y `--frozen-lockfile` |
| `dotnet build` falla | SDK incorrecto o CSS no compilado | Instalar .NET 10 y ejecutar `pnpm run build:css` |
| El IDE muestra `main` aunque la terminal esté en el worktree | El workspace abierto es `D:\proyecto-Web1\paginaWeb` | Abrir la carpeta del worktree (`File → Open Folder...` o `code "C:\...\proyecto en desarrollo"`) |
| `git switch main` falla en el worktree alterno | `main` ya está abierta en el worktree principal | No cambies a `main`; crea una rama nueva con `git switch -c feature/... origin/main` |

### 12.5. Comandos útiles en el VPS

```bash
cd /opt/portal-noticias
docker compose ps
docker compose logs -f web
docker compose pull web
docker compose up -d --remove-orphans
docker compose restart web
docker compose down
curl -I http://127.0.0.1:5000/
sudo nginx -t
sudo systemctl reload nginx
```

---

## 13. Checklist antes de fusionar a `main`

- [ ] El worktree está limpio: `git status`.
- [ ] La rama está actualizada contra `origin/main`: `git rev-list --left-right --count origin/main...HEAD` muestra `0` a la izquierda.
- [ ] `pnpm run build:css` funciona.
- [ ] `dotnet build paginaWeb.slnx -c Release --no-restore -p:SkipTailwind=true` funciona.
- [ ] Las pruebas pasan.
- [ ] El PR tiene descripción y revisión.
- [ ] Los secretos de GitHub están configurados si se va a desplegar.
- [ ] El VPS tiene Docker/Dokploy y el dominio listo.
- [ ] Después del merge, el workflow de GitHub Actions terminó en verde.
- [ ] El smoke test del despliegue respondió `200`, `301` o `302`.
- [ ] La aplicación responde en el dominio de producción.

---

## 14. Estado actual de este repositorio

| Elemento | Valor |
|---|---|
| Repositorio | `https://github.com/davidromoojeda2-ux/plantillaWeb.git` |
| Worktree principal | `D:\proyecto-Web1\paginaWeb` |
| Rama principal | `main` |
| Commit de `main` | `6b896dd` |
| Worktree alterno | `C:\Users\GAMER\.local\share\opencode\worktree\994c49\proyecto en desarrollo` |
| Rama alterna local | `proyecto-en-desarrollo` (ya fusionada) |
| Rama alterna remota | `origin/proyecto-en-desarrollo` |
| Commit de la rama alterna | `257fef8` |
| Comparación con `main` | `0` adelante, `3` detrás (la rama ya está fusionada) |
| PR de la rama alterna | Ya fusionado en `main` |
| Nueva tarea | `git switch -c feature/mi-nueva-tarea origin/main` |
| Workflow | `.github/workflows/deploy.yml` |
| Despliegue automático | Solo con push/merge a `main` |

---

## 15. Notas de seguridad

- No commitear `.env`, claves privadas, cadenas de conexión ni tokens.
- Usar **User Secrets** en local y **GitHub Secrets** en CI/CD.
- Mantener la clave privada SSH solo en el secreto `SSH_PRIVATE_KEY`.
- Si el paquete GHCR es privado, usar un PAT con permiso mínimo `read:packages`.
- Revisar los permisos del workflow: el actual usa `contents: read` y `packages: write`.
- No usar `git worktree remove --force` si hay cambios sin commit que aún necesitas.
- Antes de borrar una rama remota, verificar que no tenga un PR abierto.

---

**Fin del documento.**
