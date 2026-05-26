# Arquitectura de Seguridad — TIProject

**Proyecto:** TIProject — Sistema de Gestión de Brigadas y Capacitaciones  
**Fecha:** 2026-05-25  
**Versión:** 1.0  
**Clasificación:** Confidencial — Uso Interno  

---

## 1. Descripción General

Este documento describe la arquitectura de seguridad actual del sistema TIProject e incluye el diseño de la arquitectura objetivo con los controles de seguridad requeridos según el ciclo SecDevOps.

---

## 2. Arquitectura Actual (AS-IS)

### 2.1 Diagrama de Componentes Actual

```
┌─────────────────────────────────────────────────────────────────────┐
│                        CLIENTE (Navegador)                          │
│                                                                     │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │  Blazor WebAssembly / Server Circuit                         │  │
│  │                                                              │  │
│  │  [Cookie: email + password en texto plano]  ← ⚠ CRÍTICO    │  │
│  │  [URL: ?id={userId}&role={roleInt}]         ← ⚠ CRÍTICO    │  │
│  └──────────────────────────────────────────────────────────────┘  │
└──────────────────────┬──────────────────────────────────────────────┘
                       │  HTTPS (Kestrel / IIS Express)
                       │  Puertos: 7242 (HTTPS), 5264 (HTTP)
                       ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    SERVIDOR — ASP.NET Core Blazor                   │
│                                                                     │
│  ┌─────────────────┐   ┌──────────────────┐   ┌─────────────────┐ │
│  │  Program.cs     │   │  Razor Pages     │   │  Repositories   │ │
│  │  (sin Auth MW)  │   │  (sin [Authorize]│   │  RepoUsers      │ │
│  │                 │   │   en rutas)      │   │  RepoCourses    │ │
│  └─────────────────┘   └──────────────────┘   └────────┬────────┘ │
│                                                          │          │
│  ┌─────────────────────────────────────────────────────┐│          │
│  │  Documents.cs (Singleton)                           ││          │
│  │  Sin validación MIME — max 20MB                     ││          │
│  │  Escritura directa a wwwroot/uploads/               ││          │
│  └─────────────────────────────────────────────────────┘│          │
└─────────────────────────────────────────────┬────────────┘
                                              │  EF Core 9
                                              │  Trusted Connection
                                              ▼
                              ┌───────────────────────────┐
                              │  SQL Server LocalDB        │
                              │  TIProjectDB               │
                              │                            │
                              │  Users (pwd plain-text)   │
                              │  Courses                   │
                              └───────────────────────────┘
```

### 2.2 Vulnerabilidades Identificadas en Arquitectura Actual

| # | Componente | Vulnerabilidad | CWE | Severidad |
|---|------------|----------------|-----|-----------|
| V-01 | Login.razor | Sin autenticación framework — validación manual | CWE-287 | Crítica |
| V-02 | Tabla Users | Contraseñas en texto plano | CWE-256 | Crítica |
| V-03 | Cookie sesión | Credenciales en cookie del navegador (plain-text) | CWE-312 | Crítica |
| V-04 | Rutas Blazor | Sin decoradores [Authorize] — acceso libre | CWE-862 | Crítica |
| V-05 | URL params | userId y role pasados por query string | CWE-639 | Alta |
| V-06 | Login.razor | console.log() de credenciales | CWE-532 | Alta |
| V-07 | Documents.cs | Sin validación de tipo de archivo | CWE-434 | Alta |
| V-08 | appsettings.json | Cadena de conexión sin cifrado | CWE-260 | Alta |
| V-09 | Program.cs | Sin rate limiting en endpoints | CWE-307 | Media |
| V-10 | Headers HTTP | Sin Content Security Policy ni X-Frame-Options | CWE-693 | Media |
| V-11 | Session mgmt | Sin expiración ni invalidación de sesión | CWE-613 | Alta |
| V-12 | File upload | Sin escaneo de malware en archivos | CWE-351 | Media |

---

## 3. Arquitectura Objetivo (TO-BE)

### 3.1 Diagrama de Arquitectura Segura

```
┌──────────────────────────────────────────────────────────────────────┐
│                         CLIENTE (Navegador)                          │
│                                                                      │
│  ┌───────────────────────────────────────────────────────────────┐  │
│  │  Blazor Server Circuit (SignalR over HTTPS)                   │  │
│  │                                                               │  │
│  │  [Cookie: HttpOnly + Secure + SameSite=Strict]               │  │
│  │  [Anti-CSRF token automático]                                 │  │
│  │  [Sin credenciales en URL ni cookies de navegador]            │  │
│  └───────────────────────────────────────────────────────────────┘  │
└──────────────────────────┬───────────────────────────────────────────┘
                           │  TLS 1.2/1.3 — HTTPS obligatorio
                           │  HSTS + Preload activado
                           ▼
┌──────────────────────────────────────────────────────────────────────┐
│                   CAPA DE PRESENTACIÓN / SEGURIDAD                   │
│                                                                      │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │  Middleware Pipeline (Program.cs)                           │    │
│  │                                                             │    │
│  │  UseHsts()                ← HTTPS forzado                  │    │
│  │  UseHttpsRedirection()    ← HTTP→HTTPS redirect            │    │
│  │  UseSecurityHeaders()     ← CSP, X-Frame, X-XSS           │    │
│  │  UseRateLimiter()         ← Protección brute force         │    │
│  │  UseAuthentication()      ← ASP.NET Core Identity          │    │
│  │  UseAuthorization()       ← Policy-based RBAC              │    │
│  │  UseAntiforgery()         ← CSRF protection                │    │
│  └─────────────────────────────────────────────────────────────┘    │
│                                                                      │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │  Razor Pages — Protegidas con [Authorize(Roles="...")]      │    │
│  │                                                             │    │
│  │  /login               → Público                            │    │
│  │  /dashboard_admin     → [Authorize(Roles="admin")]          │    │
│  │  /dashboard_chief     → [Authorize(Roles="brigaderChief")]  │    │
│  │  /dashboard_brigader  → [Authorize(Roles="brigader")]       │    │
│  │  /dashboard_student   → [Authorize(Roles="student")]        │    │
│  │  /add, /edit          → [Authorize(Roles="admin")]          │    │
│  │  /addCourse           → [Authorize(Roles="admin")]          │    │
│  │  /documentsView       → [Authorize]                         │    │
│  └─────────────────────────────────────────────────────────────┘    │
└──────────────────────────┬───────────────────────────────────────────┘
                           │
┌──────────────────────────▼───────────────────────────────────────────┐
│                      CAPA DE NEGOCIO                                 │
│                                                                      │
│  ┌──────────────────────┐   ┌──────────────────────────────────┐    │
│  │  Repository Layer    │   │  Identity Services               │    │
│  │  (Validated Input)   │   │  - UserManager<AppUser>          │    │
│  │  - RepoUsers         │   │  - SignInManager<AppUser>        │    │
│  │  - RepoCourses       │   │  - RoleManager<IdentityRole>     │    │
│  └──────────────────────┘   └──────────────────────────────────┘    │
│                                                                      │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │  FileUploadService                                           │   │
│  │  - Validación MIME type                                      │   │
│  │  - Validación de extensión (whitelist)                       │   │
│  │  - Límite de tamaño (20MB)                                   │   │
│  │  - Renombrado con GUID (sin path traversal)                  │   │
│  │  - Almacenamiento fuera de wwwroot (opcional)                │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │  Audit Logger                                                │   │
│  │  - Registro de login/logout                                  │   │
│  │  - Registro de cambios a datos sensibles                     │   │
│  │  - Registro de uploads                                       │   │
│  └──────────────────────────────────────────────────────────────┘   │
└──────────────────────────┬───────────────────────────────────────────┘
                           │  EF Core con parametrización automática
┌──────────────────────────▼───────────────────────────────────────────┐
│                      CAPA DE DATOS                                   │
│                                                                      │
│  ┌───────────────────────────────────────────────────────────┐      │
│  │  SQL Server (Production)                                  │      │
│  │  TIProjectDB                                              │      │
│  │                                                           │      │
│  │  AspNetUsers        ← Contraseñas hasheadas (PBKDF2)     │      │
│  │  AspNetRoles        ← Roles definidos                     │      │
│  │  AspNetUserRoles    ← Asignación usuario-rol              │      │
│  │  Courses            ← Tabla de cursos                     │      │
│  │  AuditLog           ← Registro de auditoría              │      │
│  └───────────────────────────────────────────────────────────┘      │
│                                                                      │
│  ┌───────────────────────────────────────────────────────────┐      │
│  │  Secret Manager / Environment Variables                   │      │
│  │  - Connection string fuera de appsettings.json            │      │
│  │  - Sin secretos en repositorio de código                  │      │
│  └───────────────────────────────────────────────────────────┘      │
└──────────────────────────────────────────────────────────────────────┘
```

---

## 4. Controles de Seguridad por Capa

### 4.1 Capa de Transporte

| Control | Estado Actual | Estado Objetivo | Implementación |
|---------|---------------|-----------------|----------------|
| TLS 1.2/1.3 | Parcial (Kestrel default) | Implementado | Configurar en `appsettings.json` |
| HSTS | Solo en producción | Implementado + Preload | `UseHsts()` con `IncludeSubDomains` |
| Redirect HTTP→HTTPS | Implementado | Implementado | Ya presente en `Program.cs` |
| Certificate Pinning | No implementado | Recomendado en prod | Configuración IIS/Nginx |

### 4.2 Capa de Autenticación

| Control | Estado Actual | Estado Objetivo | Implementación |
|---------|---------------|-----------------|----------------|
| Framework de Auth | Ninguno | ASP.NET Core Identity | `AddIdentity<AppUser, IdentityRole>()` |
| Hashing de contraseñas | Plain-text | PBKDF2 (Identity default) | Automático con Identity |
| Gestión de sesiones | Cookie manual | Cookie encriptada por Data Protection | `AddAuthentication().AddCookie()` |
| Expiración de sesión | No implementada | 30 min inactividad | `ExpireTimeSpan` en CookieOptions |
| Logout seguro | No implementado | Invalidación server-side | `SignInManager.SignOutAsync()` |
| Rate limiting en login | No implementado | 5 intentos / 15 min | `AddRateLimiter()` .NET 7+ |
| Lockout de cuenta | No implementado | 5 fallos → bloqueo 15 min | `IdentityOptions.Lockout` |

### 4.3 Capa de Autorización

| Control | Estado Actual | Estado Objetivo | Implementación |
|---------|---------------|-----------------|----------------|
| Protección de rutas | Sin protección | [Authorize(Roles=)] | Atributos en cada componente Blazor |
| RBAC | Solo visual | Policy-based RBAC | `AddAuthorization()` + policies |
| Validación server-side de ID | No implementada | ClaimsPrincipal | Usar `User.FindFirstValue()` |
| Separación de privilegios | No implementada | Implementada | Roles mapeados a claims |

### 4.4 Capa de Datos

| Control | Estado Actual | Estado Objetivo | Implementación |
|---------|---------------|-----------------|----------------|
| Contraseñas hasheadas | Plain-text en BD | PBKDF2 + salt | `UserManager.CreateAsync()` |
| Protección de conn. string | En appsettings.json | Secret Manager | `dotnet user-secrets` + env vars |
| Prevención SQL injection | EF Core ORM (bueno) | Mantenido + revisión | EF Core parametriza automáticamente |
| Auditoría de acceso | No implementada | Tabla AuditLog | EF Core interceptors |
| Encriptación en reposo | No implementada | TDE en producción | Configuración SQL Server |

### 4.5 Capa de Archivos

| Control | Estado Actual | Estado Objetivo | Implementación |
|---------|---------------|-----------------|----------------|
| Validación de tipo MIME | No implementada | Whitelist MIME types | Servicio de validación custom |
| Validación de extensión | No implementada | Whitelist de extensiones | Verificar Content-Type + extensión |
| Límite de tamaño | 20MB (parcial) | 20MB verificado server-side | Multipart form size limit |
| Path traversal | Potencialmente vulnerable | GUID-based naming | Ya implementado parcialmente |
| Malware scanning | No implementado | Recomendado en prod | Windows Defender API o ClamAV |

### 4.6 Headers de Seguridad HTTP

| Header | Estado Actual | Estado Objetivo | Valor Recomendado |
|--------|---------------|-----------------|-------------------|
| Content-Security-Policy | No configurado | Implementar | `default-src 'self'; script-src 'self'` |
| X-Frame-Options | No configurado | Implementar | `DENY` |
| X-Content-Type-Options | No configurado | Implementar | `nosniff` |
| Referrer-Policy | No configurado | Implementar | `strict-origin-when-cross-origin` |
| Permissions-Policy | No configurado | Implementar | Restringir features no usadas |
| X-XSS-Protection | No configurado | Implementar (legacy) | `1; mode=block` |

---

## 5. Flujo de Autenticación Seguro (Propuesto)

```
Usuario → POST /login (email + password)
            │
            ▼
    Rate Limiter: ¿Superó intentos?
        │ SÍ → 429 Too Many Requests + bloqueo temporal
        │ NO ↓
            ▼
    UserManager.FindByEmailAsync()
            │
            ▼
    PasswordHasher.VerifyHashedPassword()
        │ FAIL → IncrementoContadorFallos → Log de intento fallido
        │ OK ↓
            ▼
    SignInManager.SignInAsync()
            │
            ▼
    Crear ClaimsPrincipal con:
        - NameIdentifier = userId
        - Email = email
        - Role = [rol del usuario]
        - Brigade = [brigada asignada]
            │
            ▼
    Emitir Cookie HttpOnly + Secure + SameSite=Strict
            │
            ▼
    Redirect a dashboard según role (server-side)
            │
            ▼
    Log de autenticación exitosa (AuditLog)
```

---

## 6. Modelo de Confianza (Trust Model)

```
ZONA PÚBLICA (Sin autenticación)
├── /login
├── /roleSelection
└── Assets estáticos (CSS, JS, imágenes)

ZONA AUTENTICADA (Requiere Identity cookie válida)
├── /dashboard_student        → Role: student
├── /courseRegistration       → Role: student, auxiliar
├── /coursesView              → Role: student, auxiliar, brigader, brigaderChief
└── /documentsView            → Role: todos los autenticados

ZONA PRIVILEGIADA (Roles elevados)
├── /dashboard_brigader       → Role: brigader
├── /dashboard_chief          → Role: brigaderChief
├── /dashboard_auxiliar       → Role: auxiliar
└── /dashboard_admin          → Role: admin
    ├── /add, /edit           → Role: admin
    ├── /addCourse            → Role: admin
    └── /usersView            → Role: admin, brigaderChief
```

---

## 7. Plan de Implementación de Controles

| Prioridad | Control | Esfuerzo | Sprint |
|-----------|---------|----------|--------|
| P1 — Crítico | Integrar ASP.NET Core Identity | 2 días | Sprint 1 |
| P1 — Crítico | Migrar contraseñas a hashing PBKDF2 | 1 día | Sprint 1 |
| P1 — Crítico | Agregar [Authorize] a todas las rutas | 1 día | Sprint 1 |
| P1 — Crítico | Eliminar cookies de credenciales plain-text | 0.5 día | Sprint 1 |
| P2 — Alto | Implementar rate limiting en login | 1 día | Sprint 2 |
| P2 — Alto | Mover conn. string a Secret Manager | 0.5 día | Sprint 2 |
| P2 — Alto | Agregar security headers middleware | 1 día | Sprint 2 |
| P2 — Alto | Validación MIME en file upload | 1 día | Sprint 2 |
| P3 — Medio | Implementar AuditLog | 2 días | Sprint 3 |
| P3 — Medio | Lockout de cuentas | 0.5 día | Sprint 3 |
| P3 — Medio | Expiración de sesión | 0.5 día | Sprint 3 |
| P4 — Bajo | 2FA (Two-Factor Auth) | 2 días | Sprint 4 |
| P4 — Bajo | Encriptación en reposo (TDE) | 1 día | Sprint 4 |

---

*Documento generado en la etapa de Diseño del ciclo SecDevOps.*
