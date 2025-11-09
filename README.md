# GeneralReservationSystem
Sistema genérico para montar aplicaciones web de reservas. (https://generalreservationsystem.onrender.com/)

## Certificados SSL para desarrollo

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
3. Ubique el contenedor correspondiente al frontend/web.
4. Haga clic derecho sobre el contenedor y seleccione "Open in Browser" o "Abrir en navegador".

La aplicación web se abrirá en su navegador predeterminado.

### Desarrollo en Visual Studio

Para ejecutar y depurar la solución en Visual Studio, simplemente seleccione el perfil **Docker Compose** en el modo deseado (Debug o Release) y ejecute el proyecto. Visual Studio gestionará automáticamente el ciclo de vida de los contenedores, la depuración y la configuración del certificado SSL para los servicios.
