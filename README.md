# GeneralReservationSystem
Sistema genérico para montar aplicaciones web de reservas. (https://generalreservationsystem.onrender.com/)

---

## Tecnologías y Compatibilidad

- **Framework:** .NET 9 (es necesario tener instalado el SDK de .NET 9 para compilar y ejecutar la solución).
- **Frontend:** Blazor WebAssembly con prerenderizado interactivo (`InteractiveWebAssembly`). No es una SPA tradicional, ya que utiliza prerendering para mejorar la experiencia y el SEO.
- **Backend:** ASP.NET Core.
- **Base de datos:** PostgreSQL.
- **Contenedores:** Docker Compose orientado principalmente a desarrollo local, aunque puede adaptarse para producción.

## Estructura de la Solución

La solución contiene los siguientes proyectos principales:
- `GeneralReservationSystem.Server`: API principal y backend.
- `GeneralReservationSystem.Web.Client`: Cliente web Blazor WebAssembly.
- `GeneralReservationSystem.Infrastructure`: Lógica de acceso a datos y migraciones.
- `GeneralReservationSystem.Application`: Lógica de negocio.
- `GeneralReservationSystem.Migration`: Herramientas para migraciones de base de datos.
- `GeneralReservationSystem.Tests`: Pruebas unitarias y de integración.

---

## Configuración del entorno (.env)

Para configurar el sistema, necesitas crear un archivo `.env` en la raíz del proyecto basado en el archivo `.env.example` incluido.

### Configuración Mínima (Desarrollo Local con Docker)

Para ejecutar el sistema localmente con Docker Compose, **solo es necesario establecer el valor de `JWT_SECRET_KEY`**. Los demás valores tienen configuraciones predeterminadas que funcionan automáticamente con el contenedor PostgreSQL incluido en Docker Compose.

```bash
# Copia el archivo de ejemplo
cp .env.example .env

# Edita el archivo .env y genera un JWT_SECRET_KEY seguro
# Puedes generar uno usando:
# - Linux/Mac: openssl rand -base64 64
# - PowerShell: -join ((48..57) + (65..90) + (97..122) | Get-Random -Count 64 | % {[char]$_})
```

**Ejemplo de configuración mínima (.env):**
```env
JWT_SECRET_KEY=8K9mP2vN7xR4tY6wQ3sE5hU8jM1nB4cF7gD9kL2oP5rT8vX1zA4yC6eG9iH2mJ5nQ8sW3uE6xR9tY2bN5cV8fH1kL4oM7pS0zD3gJ6iK9mP2qT5uX8wA1yC4eF7hN0jR3sV6xZ9b
CERT_PASSWORD=YourSecureCertPassword123
```

### Configuración con Base de Datos Externa (Supabase)

Si deseas usar una base de datos externa como Supabase, debes configurar todos los valores de conexión en el archivo `.env`:

```env
SQL_HOST=aws-1-us-east-2.pooler.supabase.com
SQL_PORT=5432
SQL_DB=postgres

DB_OWNER=<Usuario_Postgres_SA>
DB_OWNER_PASSWORD=<Contraseña>

DB_USER=<Usuario_Aplicacion>
DB_PASSWORD=<Contraseña>

JWT_SECRET_KEY=<Tu_Clave_Secreta_JWT_64_caracteres_minimo>
CERT_PASSWORD=<Tu_Password_Certificado> # Usado para el certificado SSL para desarrollo local fuera de Visual Studio
```

Consulta los comentarios en `.env.example` para más detalles sobre cada configuración. Se recomienda usar contraseñas seguras y únicas para cada variable.
Se recomienda también crear un usuario específico para <Usuario_Aplicacion> con menos permisos que el usuario SA, con suficientes permisos para
alterar los datos de la base de datos/schema pero no la estructura.

El usuario SA/DB Owner es utilizado para las migraciones de base de datos, mientras que el usuario de aplicación es utilizado por la API para conectarse a la base de datos en tiempo de ejecución.

---

## Certificados SSL para desarrollo

> **Esta sección aplica para el desarrollo por fuera de Visual Studio, ya que este IDE se encarga automáticamente de generar y confiar en certificados SSL para el desarrollo.**

Para habilitar HTTPS en los servicios, es necesario crear un certificado y confiar en él. Los contenedores esperan encontrar el archivo en `~/.aspnet/https/grs.pfx` y la contraseña en la variable de entorno `CERT_PASSWORD`.

### Windows

Ejecuta los siguientes comandos en PowerShell:

```pwsh
# Crea el certificado
$CREDENTIAL_PLACEHOLDER = "TuContraseñaSegura"
dotnet dev-certs https -ep "$env:USERPROFILE\.aspnet\https\grs.pfx" -p $CREDENTIAL_PLACEHOLDER

# Confía en el certificado
 dotnet dev-certs https --trust
```

Asegúrate de usar la misma contraseña en la variable de entorno `CERT_PASSWORD`.

### Linux

Ejecuta los siguientes comandos en Bash:

```bash
export CERT_PASSWORD="TuContraseñaSegura"
mkdir -p "$HOME/.aspnet/https"
dotnet dev-certs https -ep "$HOME/.aspnet/https/grs.pfx" -p $CERT_PASSWORD
```

Esto generará el certificado en la ruta esperada por los contenedores. No es necesario instalar ni usar `certutil` ni otras librerías externas.

Recuerda que la contraseña debe coincidir con la variable de entorno `CERT_PASSWORD` utilizada en los contenedores.

---

## Ejecución desde la terminal (Bash o PowerShell)

Para iniciar el sistema manualmente desde una terminal Bash o PowerShell, utilice los siguientes comandos desde la raíz del proyecto. Los comandos son idénticos para ambos entornos.

### Modo Desarrollo (Debug)

```bash
docker compose -f docker-compose.yml -f docker-compose.override.yml -f docker-compose.certs.override.yml -f docker-compose.vs.debug.yml build --build-arg BUILD_CONFIGURATION=Debug
docker compose -f docker-compose.yml -f docker-compose.override.yml -f docker-compose.certs.override.yml -f docker-compose.vs.debug.yml up -d
```

### Modo Producción (Release)

```bash
docker compose -f docker-compose.yml -f docker-compose.override.yml -f docker-compose.certs.override.yml -f docker-compose.vs.release.yml build --build-arg BUILD_CONFIGURATION=Release
docker compose -f docker-compose.yml -f docker-compose.override.yml -f docker-compose.certs.override.yml -f docker-compose.vs.release.yml up -d
```

Estos comandos construirán e iniciarán los contenedores en el modo seleccionado. Asegúrese de ejecutar los comandos en el directorio raíz del proyecto y de tener configurados los certificados SSL según las instrucciones previas.

> Si no se requiere una base de datos local (por ejemplo, si se utiliza una conexión a Supabase), se puede omitir a docker-compose.override.yml.

---

## Ejecución y Debugging del Proyecto

### Desarrollo en Visual Studio Code

Para ejecutar y depurar la solución en Visual Studio Code, utilice los tasks definidos en el archivo `.vscode/tasks.json`. Seleccione el entorno según sus necesidades:

- **Desarrollo (Debug):**
    1. Ejecute el task `compose-up-dev` desde el panel de tareas de Visual Studio Code (`Ctrl+Shift+P` → "Run Task" → seleccione `compose-up-dev`). Esto iniciará los contenedores en modo Debug.
    2. Espere a que los contenedores estén en ejecución.
    3. Inicie el perfil de lanzamiento correspondiente para depurar la API o el cliente web desde el panel de ejecución (`Run and Debug`).

- **Producción (Release):**
    1. Ejecute el task `compose-up-rel` para iniciar los contenedores en modo Release.

#### Acceso a la Aplicación Web

Para abrir la aplicación web en el navegador:
1. Instale y abra la extensión **Container Tools** en Visual Studio Code.
2. Diríjase al panel "Containers" (ícono de Docker en la barra lateral).
3. Ubique el contenedor correspondiente al servidor.
4. Haga clic derecho sobre el contenedor y seleccione "Open in Browser" o "Abrir en navegador".

La aplicación web se abrirá en su navegador predeterminado.

### Desarrollo en Visual Studio

Para ejecutar y depurar la solución en Visual Studio, simplemente seleccione el perfil **Docker Compose** en el modo deseado (Debug o Release) y ejecute el proyecto. Visual Studio gestionará automáticamente el ciclo de vida de los contenedores, la depuración y la configuración del certificado SSL para los servicios.

---

## Tests

Para ejecutar los tests, utiliza el siguiente comando en la raíz del proyecto:

```sh
dotnet test GeneralReservationSystem.Tests/GeneralReservationSystem.Tests.csproj
```

Las pruebas se encuentran en el proyecto `GeneralReservationSystem.Tests`.

---

## Migraciones de Base de Datos

El proyecto `GeneralReservationSystem.Migration` permite aplicar y gestionar migraciones de base de datos PostgreSQL mediante argumentos de línea de comandos. El formato general es:

```sh
dotnet run --project GeneralReservationSystem.Migration -- <acción> <connectionString> [migrationName]
```

Acciones disponibles:
- `migrate` : Aplica todas las migraciones pendientes y realiza el seed de datos.
- `revert` : Revierte todas las migraciones que tengan recursos de revert.
- `migrate-one <migrationName>` : Aplica solo la migración especificada.
- `revert-one <migrationName>` : Revierte solo la migración especificada.
- `seed` : Solo realiza el seed de datos.

Actualmente solo existe una migración inicial, pero el sistema está preparado para agregar más migraciones en el futuro. Las migraciones y sus reverts deben estar embebidas como recursos en el ensamblado, bajo las carpetas `Migrations` y `Reverts` respectivamente, con extensión `.pgsql`.

---

## Usuario Administrador por Defecto

Al inicializar la base de datos, el sistema crea automáticamente un usuario administrador si no existe:

- **Usuario:** `admin`
- **Contraseña:** `admin123`
- **Email:** `admin@example.com`

Este proceso se realiza mediante el método `SeedData` en `GeneralReservationSystem.Infrastructure.Database.MigrationsRunner`. El usuario solo se crea si no existe previamente en la tabla de usuarios.

**Advertencia de seguridad:**
- Para entornos de producción, cambia las credenciales por defecto inmediatamente después de la instalación o modifica la lógica de seed para usar credenciales seguras desde variables de entorno.
- No utilices la contraseña por defecto en ambientes públicos o productivos.