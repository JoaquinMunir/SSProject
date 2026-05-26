# Documentación SecDevOps por Etapas — TIProject

**Proyecto:** TIProject — Sistema de Gestión de Brigadas y Capacitaciones  
**Fecha:** 2026-05-25  
**Framework:** SecDevOps (Security + DevOps integrado)  
**Clasificación:** Confidencial — Uso Interno  
**Versión:** 1.0  

---

## Introducción

SecDevOps integra las prácticas de seguridad en cada etapa del ciclo de desarrollo DevOps. Este documento describe los controles de seguridad aplicados, hallazgos y artefactos generados en cada etapa del ciclo de vida del software para TIProject.

```
CICLO SECDEVOPS

  ┌─────────┐    ┌─────────┐    ┌─────────┐    ┌─────────┐
  │  PLAN   │───►│  CODE   │───►│  BUILD  │───►│  TEST   │
  └─────────┘    └─────────┘    └─────────┘    └─────────┘
       ▲                                              │
       │                                              ▼
  ┌─────────┐    ┌─────────┐    ┌─────────┐    ┌─────────┐
  │ MONITOR │◄───│ OPERATE │◄───│ DEPLOY  │◄───│ RELEASE │
  └─────────┘    └─────────┘    └─────────┘    └─────────┘
  
  En cada etapa: Security Gate + Artefactos de Seguridad
```

---

## ETAPA 1 — PLAN (Planificación)

### 1.1 Objetivo
Identificar requisitos de seguridad, activos críticos y amenazas antes de escribir código.

### 1.2 Actividades de Seguridad Realizadas

| Actividad | Resultado | Artefacto |
|-----------|-----------|-----------|
| Relevamiento de activos de información | 25 activos identificados en 4 categorías | `01-matriz-activos-informacion.md` |
| Modelado de amenazas STRIDE | 33 amenazas identificadas, 13 críticas | `04-stride-threat-model.md` |
| Árbol de ataque | 12 vectores principales con riesgo y esfuerzo | `03-arbol-de-ataque.md` |
| Definición de arquitectura de seguridad | Arquitectura AS-IS y TO-BE documentadas | `02-arquitectura-seguridad.md` |
| Análisis de roles y permisos | Mapa de acceso por rol definido | Sección 6, doc. arquitectura |

### 1.3 Requisitos de Seguridad Identificados

#### RSE-001 — Autenticación
- **Descripción:** El sistema DEBE usar un framework de autenticación estándar
- **Criterio de aceptación:** Integración de ASP.NET Core Identity con tokens de sesión opacos
- **Fuente:** S-01, S-02, S-03 (STRIDE)
- **Prioridad:** Crítica

#### RSE-002 — Almacenamiento de Contraseñas
- **Descripción:** Las contraseñas DEBEN almacenarse con hashing criptográfico
- **Criterio de aceptación:** PBKDF2 o bcrypt; ninguna contraseña en texto plano en BD
- **Fuente:** I-02, DA-01 (Asset Matrix)
- **Prioridad:** Crítica

#### RSE-003 — Control de Acceso
- **Descripción:** Todas las rutas protegidas DEBEN verificar identidad y rol
- **Criterio de aceptación:** `[Authorize]` en todas las páginas; rutas de admin con rol explícito
- **Fuente:** E-01, E-02, E-03 (STRIDE)
- **Prioridad:** Crítica

#### RSE-004 — Gestión Segura de Secretos
- **Descripción:** Ningún secreto (conn. strings, API keys) en código fuente
- **Criterio de aceptación:** `dotnet user-secrets` en dev; env variables en producción
- **Fuente:** I-04, IA-01 (Asset Matrix)
- **Prioridad:** Alta

#### RSE-005 — Validación de Archivos Subidos
- **Descripción:** El sistema DEBE validar tipo MIME y extensión de archivos subidos
- **Criterio de aceptación:** Whitelist de extensiones permitidas + verificación MIME real
- **Fuente:** E-05, AT-06 (Attack Tree)
- **Prioridad:** Alta

#### RSE-006 — Headers de Seguridad HTTP
- **Descripción:** La aplicación DEBE enviar headers de seguridad estándar
- **Criterio de aceptación:** CSP, X-Frame-Options, X-Content-Type-Options presentes
- **Fuente:** I-09, V-10 (Security Architecture)
- **Prioridad:** Media

#### RSE-007 — Auditoría y Trazabilidad
- **Descripción:** Todas las acciones sensibles DEBEN registrarse con usuario y timestamp
- **Criterio de aceptación:** Tabla AuditLog con: UserId, Action, Entity, Timestamp, IP
- **Fuente:** R-01, R-02, R-03 (STRIDE)
- **Prioridad:** Alta

#### RSE-008 — Protección contra Fuerza Bruta
- **Descripción:** El endpoint de login DEBE tener rate limiting y lockout de cuentas
- **Criterio de aceptación:** 5 intentos fallidos → bloqueo 15 min; rate limit 10 req/min/IP
- **Fuente:** AT-05, D-02 (Attack Tree / STRIDE)
- **Prioridad:** Alta

### 1.4 Criterios de Aceptación de Seguridad (Definition of Done)

```
Ninguna historia de usuario se considera COMPLETA sin:
□ No introduce nuevas vulnerabilidades críticas o altas (verificado por SAST)
□ Cambios de autenticación/autorización tienen test unitario
□ No hay secretos en código fuente
□ Manejo de errores no expone stack traces
□ Inputs de usuario son validados/sanitizados
```

---

## ETAPA 2 — CODE (Desarrollo)

### 2.1 Objetivo
Asegurar que el código fuente se desarrolla siguiendo prácticas de codificación segura.

### 2.2 Guías de Codificación Segura para TIProject

#### 2.2.1 Autenticación y Sesión
```csharp
// ❌ MAL — Actual en Login.razor
string? savedPassword = await JS.InvokeAsync<string>("getCookie", "savePassword");
Console.WriteLine($"Email: {email}, Password: {password}"); // NUNCA hacer esto

// ✅ BIEN — Con ASP.NET Core Identity
var result = await _signInManager.PasswordSignInAsync(email, password, 
    isPersistent: false, lockoutOnFailure: true);
if (result.Succeeded)
{
    // Redirigir basado en claims, NO en query params
    var user = await _userManager.FindByEmailAsync(email);
    var roles = await _userManager.GetRolesAsync(user);
}
```

#### 2.2.2 Autorización en Componentes Blazor
```csharp
// ❌ MAL — Sin protección (página actual)
@page "/dashboard_admin"
// Cualquiera puede acceder

// ✅ BIEN — Con autorización por rol
@page "/dashboard_admin"
@attribute [Authorize(Roles = "admin")]
// Si no tiene rol admin → redirige a login automáticamente
```

#### 2.2.3 Obtener Usuario Autenticado (sin confiar en URL)
```csharp
// ❌ MAL — Actual: userId desde query param
@inject NavigationManager Nav
int userId = int.Parse(Nav.ToAbsoluteUri(Nav.Uri).Query.TrimStart('?'));

// ✅ BIEN — Desde ClaimsPrincipal
@inject AuthenticationStateProvider AuthStateProvider
var authState = await AuthStateProvider.GetAuthenticationStateAsync();
var user = authState.User;
var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
var role = user.FindFirstValue(ClaimTypes.Role);
```

#### 2.2.4 Validación de Archivos
```csharp
// ❌ MAL — Sin validación (actual en Documents.cs)
var path = Path.Combine("wwwroot/uploads", file.FileName);
await file.CopyToAsync(stream);

// ✅ BIEN — Con validación MIME y whitelist
private static readonly HashSet<string> AllowedExtensions = 
    new() { ".pdf", ".png", ".jpg", ".jpeg", ".docx" };

private static readonly Dictionary<string, byte[]> MagicNumbers = new()
{
    { ".pdf", new byte[] { 0x25, 0x50, 0x44, 0x46 } }, // %PDF
    { ".png", new byte[] { 0x89, 0x50, 0x4E, 0x47 } }, // PNG
    { ".jpg", new byte[] { 0xFF, 0xD8, 0xFF } }          // JPEG
};

public bool IsFileValid(IBrowserFile file)
{
    var ext = Path.GetExtension(file.Name).ToLowerInvariant();
    if (!AllowedExtensions.Contains(ext)) return false;
    // Leer primeros bytes para verificar magic number
    // ...
    return true;
}

// Almacenar con GUID, no con nombre original
var safeFileName = $"{Guid.NewGuid()}{ext}";
```

#### 2.2.5 Gestión de Secretos
```bash
# Desarrollo — usar user-secrets
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=...;Password=..."

# appsettings.json — solo configuración no sensible
{
  "Logging": { ... },
  "AllowedHosts": "*"
  # NO incluir ConnectionStrings aquí
}
```

#### 2.2.6 Prevención de IDOR (Insecure Direct Object Reference)
```csharp
// ❌ MAL — Cualquier usuario puede editar cualquier ID
@page "/edit/{Id:int}"
await _repoUsers.Update(Id, model);

// ✅ BIEN — Verificar que el solicitante tiene permisos
var requesterId = user.FindFirstValue(ClaimTypes.NameIdentifier);
var isAdmin = user.IsInRole("admin");

if (!isAdmin && requesterId != Id.ToString())
{
    Nav.NavigateTo("/unauthorized");
    return;
}
```

### 2.3 Revisión de Código — Checklist de Seguridad

```
Para cada Pull Request, verificar:

AUTENTICACIÓN Y SESIÓN
□ No hay contraseñas o tokens hardcoded en el código
□ No se usan console.log() con datos sensibles
□ No se pasan credenciales por URL o query params
□ Las cookies tienen HttpOnly, Secure, SameSite=Strict

AUTORIZACIÓN
□ Cada nueva página/endpoint tiene su [Authorize] correspondiente
□ El rol se obtiene de ClaimsPrincipal, no de input del usuario
□ Se verifica IDOR en operaciones sobre recursos de otros usuarios

VALIDACIÓN DE ENTRADA
□ Inputs validados con DataAnnotations y validación server-side
□ Archivos validados por extensión Y magic number MIME
□ Sin concatenación de strings para construir queries SQL (usar EF Core)

GESTIÓN DE ERRORES
□ No se exponen stack traces al usuario
□ Errores logeados internamente, mensaje genérico al cliente
□ Sin información sensible en mensajes de error

LOGGING
□ Acciones sensibles registradas en AuditLog
□ Sin datos PII en logs de aplicación
□ Intentos de login (exitosos y fallidos) registrados
```

### 2.4 Herramientas de Análisis Estático (SAST)

| Herramienta | Tipo | Integración | Objetivo |
|-------------|------|-------------|----------|
| **Roslyn Analyzers** | SAST C# | Built-in .NET | Vulnerabilidades en código C# |
| **Microsoft.CodeAnalysis.NetAnalyzers** | SAST | NuGet package | Reglas de seguridad .NET |
| **SonarQube Community** | SAST | CLI / IDE | Análisis profundo de calidad y seguridad |
| **OWASP Dependency-Check** | SCA | CLI | Dependencias con CVE conocidos |

```xml
<!-- Agregar a TIProject.csproj para análisis estático -->
<ItemGroup>
  <PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="8.*" 
                    PrivateAssets="all" />
</ItemGroup>
```

---

## ETAPA 3 — BUILD (Construcción)

### 3.1 Objetivo
Integrar controles de seguridad en el pipeline de compilación.

### 3.2 Pipeline de Build Seguro

```yaml
# Ejemplo: GitHub Actions / Azure DevOps pipeline

stages:
  - stage: SecurityChecks
    jobs:
      - job: DependencyScanning
        steps:
          # Escaneo de vulnerabilidades en dependencias NuGet
          - script: |
              dotnet tool install --global dotnet-outdated-tool
              dotnet outdated --fail-on-updates
            displayName: 'Check outdated packages'
          
          - script: |
              # OWASP Dependency Check
              dependency-check --project "TIProject" --scan . \
                --format "HTML" --out "reports/dependency-check"
            displayName: 'OWASP Dependency Check'

      - job: StaticAnalysis
        steps:
          - script: |
              dotnet build --configuration Release /p:TreatWarningsAsErrors=true
            displayName: 'Build with warnings as errors'
          
          - script: |
              dotnet tool install --global security-scan
              security-scan TIProject.sln
            displayName: 'Security scan'

      - job: SecretScanning
        steps:
          - script: |
              # Detectar secretos hardcoded
              trufflehog filesystem . --only-verified
            displayName: 'Secret scanning'
```

### 3.3 Verificaciones de Seguridad en Build

| Check | Herramienta | Acción si falla |
|-------|-------------|-----------------|
| Dependencias con CVE crítico | OWASP Dep-Check | Bloquear build |
| Secretos en código | TruffleHog / git-secrets | Bloquear build |
| Análisis estático (SAST) | Roslyn + SonarQube | Bloquear si es crítico |
| Paquetes desactualizados | dotnet-outdated | Advertencia |
| Licencias de dependencias | dotnet-license-tool | Bloquear si no permitida |

### 3.4 Dependencias Actuales — Análisis de Riesgo

| Paquete | Versión | CVE Conocidos | Riesgo | Acción |
|---------|---------|--------------|--------|--------|
| Microsoft.EntityFrameworkCore.SqlServer | 9.0.9 | Verificar NVD | Bajo | Mantener actualizado |
| Microsoft.EntityFrameworkCore.Tools | 9.0.9 | Verificar NVD | Bajo | Solo dev dependency |
| Microsoft.EntityFrameworkCore.InMemory | 9.0.9 | N/A | Bajo | Solo pruebas |
| xunit | 2.9.2 | N/A | Bajo | Solo pruebas |
| Bootstrap (wwwroot) | Verificar versión | Potencial | Medio | Actualizar a v5.3+ |

```bash
# Comando para auditar dependencias .NET
dotnet list package --vulnerable --include-transitive

# Actualizar paquetes con vulnerabilidades
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version latest
```

---

## ETAPA 4 — TEST (Pruebas)

### 4.1 Objetivo
Verificar que los controles de seguridad funcionan correctamente mediante pruebas automatizadas y manuales.

### 4.2 Pruebas de Seguridad Existentes (Estado Actual)

El proyecto tiene pruebas unitarias en `TIProject.Tests/` que cubren:
- Validaciones de modelo de Usuario (`UserValidationTests.cs`)
- Validaciones de modelo de Curso (`CourseValidationTests.cs`)
- Operaciones de repositorio (`RepoUsersTests.cs`, `RepoCoursesTests.cs`)

**Gap:** No existen pruebas de seguridad específicas.

### 4.3 Pruebas de Seguridad a Implementar

#### 4.3.1 Pruebas Unitarias de Seguridad

```csharp
// TIProject.Tests/Security/AuthorizationTests.cs

public class AuthorizationTests
{
    [Fact]
    public async Task DashboardAdmin_RequiresAdminRole()
    {
        // Arrange — usuario sin rol admin
        var client = CreateClientWithRole("student");
        
        // Act
        var response = await client.GetAsync("/dashboard_admin");
        
        // Assert — debe redirigir a login o devolver 403
        Assert.True(response.StatusCode == HttpStatusCode.Redirect || 
                    response.StatusCode == HttpStatusCode.Forbidden);
    }
    
    [Fact]
    public async Task EditUser_PreventIDOR_NonAdminUser()
    {
        // Arrange — usuario autenticado como student intenta editar otro usuario
        var client = CreateClientWithRole("student", userId: 5);
        
        // Act
        var response = await client.GetAsync("/edit/1"); // ID de otro usuario
        
        // Assert — debe denegar acceso
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }
    
    [Fact]
    public void PasswordHasher_NeverStoresPlainText()
    {
        // Arrange
        var hasher = new PasswordHasher<AppUser>();
        var user = new AppUser();
        const string plainPassword = "TestPass123!";
        
        // Act
        var hash = hasher.HashPassword(user, plainPassword);
        
        // Assert
        Assert.NotEqual(plainPassword, hash);
        Assert.True(hash.Length > 50); // PBKDF2 produces long hashes
    }
    
    [Fact]
    public async Task LoginEndpoint_RateLimit_BlocksAfterNAttempts()
    {
        var client = CreateUnauthenticatedClient();
        
        // Act — 6 intentos fallidos
        for (int i = 0; i < 6; i++)
        {
            await client.PostAsync("/login", InvalidCredentials());
        }
        var response = await client.PostAsync("/login", InvalidCredentials());
        
        // Assert — 7mo intento debe ser bloqueado
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }
    
    [Theory]
    [InlineData("/dashboard_admin")]
    [InlineData("/add")]
    [InlineData("/addCourse")]
    [InlineData("/usersView")]
    public async Task ProtectedRoutes_RequireAuthentication(string route)
    {
        var client = CreateUnauthenticatedClient();
        var response = await client.GetAsync(route);
        
        // Unauthenticated request must redirect to login
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/login", response.Headers.Location?.ToString() ?? "");
    }
}
```

#### 4.3.2 Pruebas de Validación de Archivos

```csharp
// TIProject.Tests/Security/FileUploadSecurityTests.cs

public class FileUploadSecurityTests
{
    [Theory]
    [InlineData("malware.exe")]
    [InlineData("shell.aspx")]
    [InlineData("script.php")]
    [InlineData("payload.bat")]
    public void FileValidator_RejectsExecutableFiles(string filename)
    {
        var validator = new FileUploadValidator();
        var result = validator.ValidateExtension(filename);
        Assert.False(result.IsValid);
    }
    
    [Theory]
    [InlineData("documento.pdf")]
    [InlineData("imagen.png")]
    [InlineData("foto.jpg")]
    public void FileValidator_AcceptsAllowedFiles(string filename)
    {
        var validator = new FileUploadValidator();
        var result = validator.ValidateExtension(filename);
        Assert.True(result.IsValid);
    }
    
    [Fact]
    public void FileValidator_DetectsContentTypeMismatch()
    {
        // Archivo con extensión .pdf pero magic bytes de .exe
        var fakeFile = CreateFileWithMagicBytes("fake.pdf", new byte[] { 0x4D, 0x5A }); // MZ header (EXE)
        var validator = new FileUploadValidator();
        var result = validator.ValidateMimeType(fakeFile);
        Assert.False(result.IsValid);
    }
}
```

#### 4.3.3 Pruebas DAST (Dynamic Application Security Testing)

| Herramienta | Tipo de Prueba | Objetivo |
|-------------|----------------|---------|
| **OWASP ZAP** | DAST Automatizado | Escaneo de vulnerabilidades web en runtime |
| **Burp Suite Community** | Prueba manual | Interceptación y manipulación de requests |
| **Nikto** | DAST Scanner | Configuraciones inseguras del servidor |

```bash
# OWASP ZAP — Escaneo automatizado
docker run -t owasp/zap2docker-stable zap-baseline.py \
  -t https://localhost:7242 \
  -r reports/zap-report.html \
  --hook=/zap/auth_hook.py

# Nikto — Escaneo de servidor
nikto -h https://localhost:7242 -output reports/nikto-report.txt
```

### 4.4 Casos de Prueba de Penetración (Pentest Manual)

| TC-ID | Caso de Prueba | Pasos | Resultado Esperado | Resultado Actual |
|-------|----------------|-------|-------------------|-----------------|
| PT-01 | Acceso directo a /dashboard_admin | GET /dashboard_admin sin cookies | Redirect a /login | ❌ Acceso libre |
| PT-02 | Escalación via ?role=0 | Login como student, cambiar ?role=0 en URL | Acceso denegado | ❌ Acceso como admin |
| PT-03 | Lectura de cookie de sesión | DevTools → Application → Cookies | Cookie opaca (no legible) | ❌ Credenciales en plain-text |
| PT-04 | Fuerza bruta sin bloqueo | 100 intentos de login automatizados | Bloqueo tras 5 intentos | ❌ Sin bloqueo |
| PT-05 | Upload de archivo .aspx | Subir web shell con extensión .aspx | Rechazo con error 400 | ❌ Acepta cualquier archivo |
| PT-06 | IDOR en /edit/{id} | Autenticado como user 5, GET /edit/1 | Acceso denegado (403) | ❌ Acceso libre |
| PT-07 | Credenciales en console.log | Login y abrir DevTools → Console | Sin datos sensibles en consola | ❌ Password visible |
| PT-08 | SQL Injection básico | Email: `' OR '1'='1` | Login fallido / error controlado | ✅ EF Core previene |
| PT-09 | XSS en campo nombre | Nombre: `<script>alert(1)</script>` | Texto escapado, sin ejecución | ✅ Blazor escapa |
| PT-10 | Enumeración de usuarios | GET /usersView sin auth | Redirect a login | ❌ Lista de usuarios visible |

**Leyenda:** ✅ Pasa | ❌ Falla | ⚠ Parcial

---

## ETAPA 5 — RELEASE (Liberación)

### 5.1 Objetivo
Verificar que la versión a liberar cumple los requisitos de seguridad mínimos.

### 5.2 Security Gate — Checklist de Release

```
SECURITY GATE — TIProject Release Checklist

BLOQUEADORES (No se puede liberar si alguno falla):
□ RSE-001: ASP.NET Core Identity integrado y probado
□ RSE-002: Contraseñas hasheadas (0 passwords en plain-text en BD)
□ RSE-003: [Authorize] en todas las rutas protegidas (100%)
□ RSE-005: Validación MIME + extensión en file upload
□ Sin secretos detectados por TruffleHog en código fuente
□ SAST sin hallazgos críticos
□ Dependencias sin CVE críticos conocidos

ADVERTENCIAS (Documentar si no se cumplen):
□ RSE-004: Connection string fuera de appsettings.json
□ RSE-006: Security headers HTTP configurados
□ RSE-007: AuditLog implementado
□ RSE-008: Rate limiting en login activo
□ Pruebas de seguridad unitarias ejecutando en CI (>80% cobertura)
□ DAST ejecutado sin hallazgos críticos o altos

DOCUMENTACIÓN:
□ CHANGELOG con cambios de seguridad documentados
□ Reporte de SAST adjunto
□ Reporte de Dependency Check adjunto
□ Pentest report adjunto (si aplica)
```

### 5.3 Versionado y Changelog de Seguridad

```markdown
## [Unreleased] — Sprint 1

### Security
- BREAKING: Migrado a ASP.NET Core Identity (contraseñas hasheadas)
- BREAKING: Eliminadas cookies de credenciales plain-text
- Added: [Authorize] attributes en todas las rutas protegidas
- Added: Role-based authorization via ClaimsPrincipal
- Fixed: Eliminado console.log() de credenciales (I-01)
- Fixed: Eliminado query param ?role= en navegación (E-01)
```

---

## ETAPA 6 — DEPLOY (Despliegue)

### 6.1 Objetivo
Asegurar que el entorno de despliegue está correctamente configurado y endurecido.

### 6.2 Hardening de Configuración

#### 6.2.1 appsettings.Production.json (Sin secretos)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "TIProject.Audit": "Information"
    }
  },
  "AllowedHosts": "tudominio.com",
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://+:443",
        "Certificate": {
          "Path": "/etc/ssl/certs/tiproject.pfx"
        }
      }
    }
  }
}
```

#### 6.2.2 Variables de Entorno (Producción)
```bash
# Configurar en el sistema operativo / contenedor
export ASPNETCORE_ENVIRONMENT=Production
export ConnectionStrings__DefaultConnection="Server=prod-sql;Database=TIProjectDB;..."
export DataProtection__KeyPath="/var/dpkeys/"
```

#### 6.2.3 Middleware de Seguridad (Program.cs — Objetivo)
```csharp
var builder = WebApplication.CreateBuilder(args);

// 1. Autenticación y Autorización
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
.AddEntityFrameworkStores<TIProjectDbContext>();

// 2. Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("login", o =>
    {
        o.PermitLimit = 5;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueLimit = 0;
    });
});

// 3. Data Protection
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/var/dpkeys/"))
    .SetApplicationName("TIProject");

var app = builder.Build();

// Pipeline de middleware (orden importante)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Security Headers
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers.Add("X-Frame-Options", "DENY");
    ctx.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    ctx.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    ctx.Response.Headers.Add("Content-Security-Policy",
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; " +
        "font-src 'self' https://cdnjs.cloudflare.com; img-src 'self' data:;");
    await next();
});

app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
```

### 6.3 Checklist de Despliegue

```
INFRAESTRUCTURA
□ TLS 1.2/1.3 habilitado, TLS 1.0/1.1 deshabilitado
□ HTTP → HTTPS redirect configurado
□ Certificado SSL válido y vigente
□ Servidor actualizado con últimos parches de seguridad
□ Puertos no necesarios cerrados (solo 80, 443)

APLICACIÓN
□ ASPNETCORE_ENVIRONMENT = Production
□ Detailed errors deshabilitados
□ Debug logging deshabilitado
□ Secretos en variables de entorno (no en appsettings)
□ Static files servidos correctamente
□ Directory browsing deshabilitado en wwwroot

BASE DE DATOS
□ Contraseñas del sistema con hashing verificado
□ Usuario de BD con permisos mínimos necesarios
□ Backups configurados y probados
□ Acceso a BD solo desde servidor de aplicación

MONITOREO
□ Logging habilitado y apuntando a destino seguro
□ Alertas configuradas para intentos de acceso fallidos
□ Health check endpoint configurado
```

---

## ETAPA 7 — OPERATE (Operación)

### 7.1 Objetivo
Mantener la seguridad del sistema en producción mediante controles operativos.

### 7.2 Procedimientos Operativos de Seguridad

#### 7.2.1 Gestión de Acceso
- Revisión trimestral de usuarios activos y sus roles
- Eliminación inmediata de acceso al desvincularse un usuario
- Rotación de contraseñas de cuentas de servicio cada 90 días
- Auditoría mensual de cuentas con rol `admin`

#### 7.2.2 Gestión de Parches
```
Prioridad 1 (24h): CVE CVSS ≥ 9.0 en dependencias del sistema
Prioridad 2 (7 días): CVE CVSS 7.0-8.9 en dependencias
Prioridad 3 (30 días): CVE CVSS < 7.0 o actualizaciones de .NET SDK
Prioridad 4 (trimestral): Actualizaciones menores de paquetes
```

#### 7.2.3 Respuesta a Incidentes de Seguridad

**Clasificación de Incidentes:**

| Nivel | Descripción | Tiempo de Respuesta | Ejemplo |
|-------|-------------|---------------------|---------|
| P1 — Crítico | Compromiso confirmado de datos | 1 hora | Fuga de contraseñas, acceso no autorizado a BD |
| P2 — Alto | Explotación activa en progreso | 4 horas | Brute force exitoso, web shell detectada |
| P3 — Medio | Intento de ataque detectado | 24 horas | Múltiples fallos de login, escaneo de puertos |
| P4 — Bajo | Anomalía de seguridad | 72 horas | Acceso fuera de horario, cambio de configuración |

**Playbook P1 — Compromiso de Credenciales:**
```
1. CONTENER (0-15 min)
   □ Deshabilitar acceso externo a la aplicación
   □ Revocar todas las sesiones activas (invalidar tokens)
   □ Cambiar contraseñas de cuentas de servicio BD

2. ANALIZAR (15-60 min)
   □ Revisar AuditLog para determinar alcance
   □ Identificar qué datos fueron accedidos
   □ Determinar vector de ataque

3. ERRADICAR (1-4 horas)
   □ Parchear vulnerabilidad explotada
   □ Forzar reset de contraseñas de usuarios afectados
   □ Revisar y limpiar backdoors/uploads maliciosos

4. RECUPERAR
   □ Restaurar servicio con controles adicionales
   □ Monitoreo intensivo por 72 horas

5. DOCUMENTAR
   □ Informe post-incidente (root cause, impacto, lecciones)
   □ Actualizar matriz de riesgos
```

### 7.3 Mantenimiento de Seguridad

| Frecuencia | Actividad |
|------------|-----------|
| Diaria | Revisar logs de autenticación fallida |
| Semanal | Revisar AuditLog de cambios de roles |
| Mensual | Ejecutar OWASP Dependency Check |
| Trimestral | Revisión de usuarios y privilegios |
| Semestral | Pentest o security review completo |
| Anual | Actualización del modelado de amenazas STRIDE |

---

## ETAPA 8 — MONITOR (Monitoreo)

### 8.1 Objetivo
Detectar y responder a amenazas de seguridad en tiempo real mediante observabilidad continua.

### 8.2 Implementación de Logging de Seguridad

```csharp
// Serilog configurado con sinks de seguridad
// Program.cs
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "TIProject")
    .WriteTo.Console(outputTemplate: 
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/security-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 90,
        restrictedToMinimumLevel: LogEventLevel.Warning)
);

// AuditService.cs — registro de acciones sensibles
public class AuditService
{
    private readonly TIProjectDbContext _db;
    private readonly ILogger<AuditService> _logger;

    public async Task LogAsync(string userId, string action, string entity, 
        string entityId, string ipAddress)
    {
        var entry = new AuditLog
        {
            UserId = userId,
            Action = action,        // "LOGIN", "UPDATE_ROLE", "DELETE_USER", "UPLOAD_FILE"
            Entity = entity,        // "User", "Course", "Document"
            EntityId = entityId,
            IpAddress = ipAddress,
            Timestamp = DateTime.UtcNow
        };
        _db.AuditLogs.Add(entry);
        await _db.SaveChangesAsync();
        
        _logger.LogInformation("AUDIT: {UserId} {Action} {Entity}/{EntityId} from {IP}",
            userId, action, entity, entityId, ipAddress);
    }
}
```

### 8.3 Métricas de Seguridad a Monitorear

| Métrica | Umbral de Alerta | Acción |
|---------|-----------------|--------|
| Intentos de login fallidos por IP | > 10 en 5 min | Bloquear IP temporalmente |
| Intentos de login fallidos por cuenta | > 5 en 15 min | Lockout automático |
| Uploads fallidos por validación | > 20 en 1 hora | Revisar origen |
| Accesos a rutas sin autorización | > 5 en 1 min | Alerta de seguridad |
| Cambios de rol de usuario | Cualquiera | Log obligatorio + alerta |
| Eliminación de usuario | Cualquiera | Log obligatorio + alerta |
| Errores 500 en producción | > 10 en 5 min | Alerta operacional |

### 8.4 Dashboard de Seguridad (KPIs)

```
┌────────────────────────────────────────────────────────────────┐
│              DASHBOARD DE SEGURIDAD — TIProject                │
├────────────────────┬───────────────────┬───────────────────────┤
│  Logins Exitosos   │  Logins Fallidos  │  Cuentas Bloqueadas   │
│      [HOY]         │      [HOY]        │      [ACTIVAS]        │
├────────────────────┴───────────────────┴───────────────────────┤
│  Uploads Rechazados (tipo inválido)  │  Cambios de Rol         │
│         [ÚLTIMAS 24H]                │  [ÚLTIMAS 24H]          │
├──────────────────────────────────────┴─────────────────────────┤
│  INTENTOS DE ACCESO NO AUTORIZADO A RUTAS PROTEGIDAS           │
│  [Timeline — últimas 24h]                                      │
├────────────────────────────────────────────────────────────────┤
│  TOP IPs CON MÁS INTENTOS FALLIDOS  │  USUARIOS RECIÉN CREADOS │
└────────────────────────────────────────────────────────────────┘
```

### 8.5 Alertas Automatizadas

```csharp
// Ejemplo: Alerta por detección de patrón sospechoso
public class SecurityAlertService
{
    public async Task CheckAndAlertAsync(string userId, string ipAddress)
    {
        var recentFailures = await _auditDb.AuditLogs
            .Where(a => a.IpAddress == ipAddress 
                && a.Action == "LOGIN_FAILED"
                && a.Timestamp > DateTime.UtcNow.AddMinutes(-5))
            .CountAsync();
        
        if (recentFailures >= 10)
        {
            _logger.LogWarning("SECURITY ALERT: Posible brute force desde {IP}. " +
                "{Count} intentos fallidos en 5 minutos", ipAddress, recentFailures);
            // Enviar notificación al administrador
            await _notificationService.AlertAdminAsync(
                $"Alerta de seguridad: {recentFailures} intentos de login fallidos desde {ipAddress}");
        }
    }
}
```

---

## Resumen de Artefactos por Etapa

| Etapa | Artefactos Generados | Estado |
|-------|---------------------|--------|
| Plan | Matriz de activos, STRIDE, Árbol de ataque, Arquitectura | ✅ Completo |
| Code | Guías de codificación segura, Checklist de PR | ✅ Completo |
| Build | Pipeline de CI/CD con seguridad, Configuración SAST | ✅ Definido |
| Test | Casos de prueba de seguridad, Pentest manual, DAST config | ✅ Definido |
| Release | Security Gate checklist, Plantilla de changelog | ✅ Completo |
| Deploy | Hardening config, Checklist de despliegue, Program.cs objetivo | ✅ Completo |
| Operate | Playbooks de incidentes, Gestión de parches | ✅ Completo |
| Monitor | AuditService, Métricas, Dashboard, Alertas | ✅ Definido |

---

*Documento maestro del ciclo SecDevOps para TIProject. Actualizar con cada sprint o cambio significativo de arquitectura.*
