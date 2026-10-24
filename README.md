<p align="center">
  <img src="TIProject/wwwroot/images/logo-pc-ucol.png" alt="Protección Civil Universidad de Colima" width="120" />
</p>

# Protección Civil Universitaria — Sistema de gestión de brigadas

Plataforma web para la Brigada Universitaria de Protección Civil de la Universidad de Colima. Centraliza el padrón de brigadistas, las capacitaciones, los boletines informativos y los documentos oficiales, con un panel distinto para cada rol.

## Funcionalidades

- **Sitio público:** inicio con galería, servicios, boletines descargables y catálogo de capacitaciones.
- **Autenticación por rol:** inicio de sesión con número de identificación de 8 dígitos, reCAPTCHA y redirección al panel de cada perfil.
- **5 roles con panel propio:** Administrador, Jefe de Brigada, Brigadista, Auxiliar y Estudiante.
- **Padrón de usuarios:** alta, edición y asignación a una de las 5 brigadas (Primeros Auxilios, Evacuación, Combate de Incendios, Búsqueda y Rescate, Comunicación).
- **Capacitaciones:** creación de cursos con encargado, fecha y lugar, e inscripción de usuarios.
- **Gestor documental:** carga de boletines y documentos por categoría con validación en servidor.

## Seguridad

El proyecto pasó por una auditoría SecDevOps documentada en [`docs/security`](docs/security/README.md) (matriz de activos, árbol de ataque, modelo de amenazas STRIDE con 33 amenazas). Controles implementados a partir de ese análisis:

- Contraseñas con hash PBKDF2 (`PasswordHasher`) y migración automática de contraseñas antiguas en texto plano.
- Autorización por rol con `[Authorize(Roles = ...)]` en cada panel; el rol se toma siempre de la base de datos, nunca del formulario.
- Cookie de sesión `HttpOnly`, `SameSite=Strict` y expiración deslizante de 30 minutos.
- Límite de 5 intentos de inicio de sesión por minuto (rate limiting) y reCAPTCHA.
- Cabeceras de seguridad HTTP (`X-Frame-Options`, `X-Content-Type-Options`, `Referrer-Policy`, `Permissions-Policy`).
- Carga de archivos con lista blanca de extensiones, detección de doble extensión, verificación de *magic bytes* y límite de 20 MB.

## Stack

- C# · ASP.NET Core **Blazor Server** (.NET 8)
- Entity Framework Core 9 con migraciones · SQL Server (LocalDB)
- Bootstrap 5 · CSS propio con modo oscuro · Font Awesome

## Estructura

```
TIProject/
├── Components/Pages/   Páginas Blazor (inicio, login, paneles por rol, usuarios, cursos, documentos)
├── Data/               Entidades (User, Course), DbContext y servicio de documentos
├── Repository/         Repositorios de usuarios y cursos
├── Migrations/         Migraciones de EF Core
└── wwwroot/            CSS, JS, imágenes y archivos subidos
docs/security/          Documentación de seguridad SecDevOps
```

## Cómo ejecutarlo

Requisitos: .NET 8 SDK y SQL Server LocalDB (incluido con Visual Studio).

```bash
cd TIProject
dotnet tool install --global dotnet-ef   # solo la primera vez
dotnet ef database update                # crea la base TIProjectDB
dotnet run
```

Las claves de reCAPTCHA de `appsettings.json` son las claves públicas de prueba de Google. Para un despliegue real hay que reemplazarlas por claves propias y guardarlas fuera del repositorio (variables de entorno o *user secrets*).

## Contexto

Proyecto académico de la Facultad de Telemática, Universidad de Colima.
