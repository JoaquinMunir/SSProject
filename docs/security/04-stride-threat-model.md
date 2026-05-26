# Modelado de Amenazas STRIDE — TIProject

**Proyecto:** TIProject — Sistema de Gestión de Brigadas y Capacitaciones  
**Fecha:** 2026-05-25  
**Metodología:** STRIDE (Microsoft)  
**Herramienta base:** Análisis manual sobre código fuente y arquitectura  
**Clasificación:** Confidencial — Uso Interno  
**Versión:** 1.0  

---

## 1. Introducción a STRIDE

STRIDE es un acrónimo que clasifica las amenazas de seguridad en seis categorías:

| Letra | Amenaza | Violación de propiedad |
|-------|---------|----------------------|
| **S** | **Spoofing** (Suplantación de identidad) | Autenticación |
| **T** | **Tampering** (Manipulación de datos) | Integridad |
| **R** | **Repudiation** (Repudio) | No repudio |
| **I** | **Information Disclosure** (Divulgación de información) | Confidencialidad |
| **D** | **Denial of Service** (Denegación de servicio) | Disponibilidad |
| **E** | **Elevation of Privilege** (Escalación de privilegios) | Autorización |

---

## 2. Componentes Analizados (Data Flow Diagram)

### 2.1 Entidades Externas
- **EU-1:** Usuario del sistema (Admin, Jefe, Brigadista, Auxiliar, Estudiante)
- **EU-2:** Administrador de base de datos

### 2.2 Procesos
- **P-1:** Login.razor — Proceso de autenticación
- **P-2:** Dashboard*.razor — Paneles de control por rol
- **P-3:** AddUser.razor / Edit — Gestión de usuarios
- **P-4:** AddCourse.razor — Gestión de cursos
- **P-5:** DocumentsView.razor — Gestión de archivos
- **P-6:** Repository Layer — Capa de acceso a datos

### 2.3 Flujos de Datos
- **FD-1:** EU-1 → P-1: Credenciales (email + password)
- **FD-2:** P-1 → P-2: Redirect post-login con userId en URL
- **FD-3:** P-6 → BD: Consultas SQL via EF Core
- **FD-4:** EU-1 → P-5: Archivo subido
- **FD-5:** Navegador ← P-1: Cookie con credenciales plain-text
- **FD-6:** P-3 → BD: Operaciones CRUD de usuarios

### 2.4 Almacenes de Datos
- **AD-1:** SQL Server LocalDB — TIProjectDB
- **AD-2:** wwwroot/uploads/ — Archivos subidos
- **AD-3:** Data/documentos_db.json — Metadatos de documentos
- **AD-4:** appsettings.json — Configuración de la aplicación

---

## 3. Análisis STRIDE Completo

### 3.1 S — SPOOFING (Suplantación de Identidad)

| ID | Amenaza | Componente Afectado | Vector | Probabilidad | Impacto | Riesgo | Estado |
|----|---------|--------------------|----|------|-------|-----|--------|
| S-01 | Suplantación de usuario mediante manipulación de cookie | FD-5, P-1 | Editar valor de cookie `savedAccount` en DevTools del navegador | Alta | Crítico | **CRÍTICO** | Abierto |
| S-02 | Suplantación de rol mediante parámetro URL | FD-2, P-2 | Cambiar `?role=0` en URL para acceder como admin | Alta | Crítico | **CRÍTICO** | Abierto |
| S-03 | Suplantación de sesión por ausencia de binding server-side | P-1 | Sin estado de sesión server-side, cualquier cookie válida funciona | Alta | Crítico | **CRÍTICO** | Abierto |
| S-04 | Credential stuffing — uso de credenciales robadas | P-1, AD-1 | Contraseñas plain-text en BD facilita uso de credenciales filtradas | Media | Alto | **ALTO** | Abierto |
| S-05 | Replay attack de cookie de sesión | FD-5 | Cookie sin expiración ni renovación puede reutilizarse indefinidamente | Media | Alto | **ALTO** | Abierto |

**Contramedidas S:**
- S-01, S-03: Implementar ASP.NET Core Identity con cookie encriptada via Data Protection API
- S-02: Eliminar query params de rol; usar `ClaimsPrincipal` para determinar rol server-side
- S-04: Hashear contraseñas con PBKDF2 (Identity default)
- S-05: Configurar expiración de cookie + sliding expiration

---

### 3.2 T — TAMPERING (Manipulación de Datos)

| ID | Amenaza | Componente Afectado | Vector | Probabilidad | Impacto | Riesgo | Estado |
|----|---------|--------------------|----|------|-------|-----|--------|
| T-01 | Modificación del ID de usuario en URL para editar otro usuario | P-3 | GET /edit/{id} — cualquier ID accesible sin validar propietario | Alta | Alto | **ALTO** | Abierto |
| T-02 | Modificación de roles de usuario sin autorización | P-3, AD-1 | Acceso a /edit sin autenticación permite cambiar cualquier campo | Alta | Crítico | **CRÍTICO** | Abierto |
| T-03 | Alteración de metadatos de documentos (JSON file) | AD-3 | documentos_db.json accesible si existe path traversal o acceso FS | Baja | Medio | **BAJO** | Abierto |
| T-04 | Modificación de datos de inscripción a cursos | P-4, AD-1 | Sin validación de quién puede inscribir a quién | Alta | Medio | **MEDIO** | Abierto |
| T-05 | Path traversal en upload de archivos | P-5 | Nombre de archivo no completamente sanitizado podría sobrescribir archivos | Baja | Alto | **MEDIO** | Abierto |
| T-06 | Eliminación no autorizada de usuarios o cursos | P-3, P-4 | DELETE sin verificación de permisos del solicitante | Alta | Alto | **ALTO** | Abierto |

**Contramedidas T:**
- T-01, T-04, T-06: Validar que el usuario autenticado (ClaimsPrincipal) tiene permisos sobre el recurso
- T-02: Agregar `[Authorize(Roles="admin")]` en páginas de edición
- T-03: Mover documentos_db.json fuera del directorio wwwroot
- T-05: Usar `Guid.NewGuid()` como nombre de archivo (ya parcialmente implementado)

---

### 3.3 R — REPUDIATION (Repudio)

| ID | Amenaza | Componente Afectado | Vector | Probabilidad | Impacto | Riesgo | Estado |
|----|---------|--------------------|----|------|-------|-----|--------|
| R-01 | Usuario niega haber modificado datos de brigada | P-3, AD-1 | Sin tabla de auditoría ni logs de cambios | Alta | Alto | **ALTO** | Abierto |
| R-02 | Usuario niega haber subido archivo malicioso | P-5, AD-2 | Sin registro de qué usuario subió qué archivo con timestamp | Alta | Alto | **ALTO** | Abierto |
| R-03 | Administrador niega haber eliminado usuario o curso | P-3, P-4 | Sin soft-delete ni log de eliminaciones | Alta | Alto | **ALTO** | Abierto |
| R-04 | Imposibilidad de auditar intentos de acceso fallidos | P-1 | Sin registro de intentos de login fallidos | Alta | Medio | **MEDIO** | Abierto |
| R-05 | Logs del sistema insuficientes para forensia | Program.cs | Solo se usa Information logging por defecto, sin audit log | Alta | Medio | **MEDIO** | Abierto |

**Contramedidas R:**
- R-01, R-02, R-03: Implementar tabla `AuditLog` con: UserId, Action, Entity, EntityId, Timestamp, IPAddress
- R-04: Registrar intentos de login con IP, timestamp, email intentado, resultado
- R-05: Implementar structured logging (Serilog/NLog) con nivel de auditoría separado

---

### 3.4 I — INFORMATION DISCLOSURE (Divulgación de Información)

| ID | Amenaza | Componente Afectado | Vector | Probabilidad | Impacto | Riesgo | Estado |
|----|---------|--------------------|----|------|-------|-----|--------|
| I-01 | Credenciales visibles en console.log() del navegador | P-1 | Login.razor registra email + password en consola JavaScript | Alta | Crítico | **CRÍTICO** | Abierto |
| I-02 | Contraseñas en texto plano en base de datos | AD-1 | SELECT * FROM Users expone todas las contraseñas | Alta | Crítico | **CRÍTICO** | Abierto |
| I-03 | Credenciales en cookie del navegador (plain-text) | FD-5 | Cookies `savedAccount`/`savePassword` legibles por JS/usuario | Alta | Crítico | **CRÍTICO** | Abierto |
| I-04 | Cadena de conexión SQL en appsettings.json | AD-4 | Exposición del archivo de configuración revela acceso a BD | Media | Alto | **ALTO** | Abierto |
| I-05 | Datos personales accesibles sin autenticación | P-2, P-3 | /usersView accesible sin login — expone PII de todos los usuarios | Alta | Alto | **ALTO** | Abierto |
| I-06 | Documentos institucionales accesibles sin auth | AD-2 | Archivos en wwwroot/uploads/ servidos como estáticos sin verificación | Alta | Medio | **ALTO** | Abierto |
| I-07 | Stack traces y mensajes de error detallados | P-* | Errores no manejados pueden exponer rutas, estructura de BD, versiones | Media | Medio | **MEDIO** | Abierto |
| I-08 | Enumeración de usuarios via UI | P-1 | Mensajes de error diferentes para "usuario no existe" vs "password incorrecto" | Media | Bajo | **BAJO** | Abierto |
| I-09 | Exposición de metadatos en cabeceras HTTP | Servidor | Server: Kestrel, X-Powered-By, etc. revelan tecnología | Baja | Bajo | **BAJO** | Abierto |

**Contramedidas I:**
- I-01: Eliminar todos los `console.log()` de credenciales inmediatamente
- I-02: Migrar a hashing PBKDF2 con ASP.NET Core Identity
- I-03: Eliminar cookies manuales; usar Identity cookie encriptada + HttpOnly + Secure
- I-04: Usar `dotnet user-secrets` en desarrollo; env variables en producción
- I-05, I-06: Aplicar `[Authorize]` y servir archivos via endpoint autenticado
- I-07: Configurar página de error genérica en producción (ya existe /Error)
- I-08: Usar mensaje genérico "Credenciales inválidas" en todos los casos
- I-09: Eliminar/suprimir headers de servidor en middleware

---

### 3.5 D — DENIAL OF SERVICE (Denegación de Servicio)

| ID | Amenaza | Componente Afectado | Vector | Probabilidad | Impacto | Riesgo | Estado |
|----|---------|--------------------|----|------|-------|-----|--------|
| D-01 | Flood de uploads de 20MB para agotar disco | P-5 | Sin rate limiting ni cuota por usuario en subida de archivos | Media | Alto | **ALTO** | Abierto |
| D-02 | Creación masiva de usuarios (spam a BD) | P-3 | /add accesible sin autenticación y sin rate limiting | Alta | Medio | **MEDIO** | Abierto |
| D-03 | Saturación de SignalR circuits (Blazor Server) | Servidor | Abrir muchas sesiones simultáneas sin límite de conexiones | Media | Alto | **ALTO** | Abierto |
| D-04 | Consultas costosas a BD sin paginación | P-2, P-6 | /usersView carga todos los usuarios en memoria sin límite | Media | Medio | **MEDIO** | Abierto |
| D-05 | Loop infinito o timeout en validaciones | P-3, P-4 | Validaciones complejas sin timeout server-side | Baja | Bajo | **BAJO** | Abierto |

**Contramedidas D:**
- D-01: `AddRateLimiter()` en endpoint de archivos + cuota de almacenamiento por usuario
- D-02: Requiere autenticación + rate limiting en creación de usuarios
- D-03: Configurar `MaximumParallelHubInvocations` y limitar conexiones concurrentes
- D-04: Implementar paginación en listados de usuarios y cursos
- D-05: Agregar timeouts en consultas EF Core (`CommandTimeout`)

---

### 3.6 E — ELEVATION OF PRIVILEGE (Escalación de Privilegios)

| ID | Amenaza | Componente Afectado | Vector | Probabilidad | Impacto | Riesgo | Estado |
|----|---------|--------------------|----|------|-------|-----|--------|
| E-01 | Cambio de rol via parámetro URL ?role=0 | P-2, FD-2 | Role= en URL no validado server-side → cualquiera accede como admin | Alta | Crítico | **CRÍTICO** | Abierto |
| E-02 | Acceso a funciones de admin sin autenticación | P-2, P-3 | Sin [Authorize] en rutas → /dashboard_admin abierto a cualquiera | Alta | Crítico | **CRÍTICO** | Abierto |
| E-03 | Creación de usuario admin por usuario no autenticado | P-3 | /add sin auth → cualquiera puede crear cuenta admin | Alta | Crítico | **CRÍTICO** | Abierto |
| E-04 | Modificación de rol en /edit sin verificación de privilegio | P-3, AD-1 | /edit/{id} permite cambiar campo Role sin validar quién solicita | Alta | Crítico | **CRÍTICO** | Abierto |
| E-05 | Upload de archivo .aspx ejecutable (web shell) | P-5, AD-2 | Sin validación de tipo → web shell ejecutable en servidor | Media | Crítico | **ALTO** | Abierto |
| E-06 | Acceso a datos de otro usuario via IDOR | P-3 | /edit/{cualquier-id} accesible por cualquier usuario autenticado | Alta | Alto | **ALTO** | Abierto |

**Contramedidas E:**
- E-01: Nunca leer rol de query string; rol desde `ClaimsPrincipal.FindFirstValue(ClaimTypes.Role)`
- E-02, E-03: Implementar `[Authorize(Roles="admin")]` en todas las rutas administrativas
- E-04: Validar en RepoUsers que solo admins pueden cambiar roles
- E-05: Whitelist de extensiones + validación MIME; almacenar fuera de wwwroot
- E-06: Verificar que `User.FindFirstValue(ClaimTypes.NameIdentifier) == id` o que sea admin

---

## 4. Matriz de Riesgo STRIDE

```
             IMPACTO
             │ Bajo  │ Medio │ Alto  │ Crítico
PROB.        │       │       │       │
─────────────┼───────┼───────┼───────┼─────────
Alta         │ I-08  │ R-04  │ T-01  │ S-01
             │ I-09  │ R-05  │ T-06  │ S-02
             │       │ D-02  │ I-05  │ S-03
             │       │       │ I-06  │ T-02
             │       │       │       │ E-01
             │       │       │       │ E-02
             │       │       │       │ E-03
             │       │       │       │ E-04
             │       │       │       │ I-01
             │       │       │       │ I-02
             │       │       │       │ I-03
─────────────┼───────┼───────┼───────┼─────────
Media        │       │ T-04  │ S-04  │ E-05
             │       │ D-04  │ S-05  │ I-04
             │       │       │ D-01  │
             │       │       │ D-03  │
             │       │       │ T-05  │
─────────────┼───────┼───────┼───────┼─────────
Baja         │ D-05  │ T-03  │       │
             │       │       │       │
```

---

## 5. Resumen de Hallazgos

| Severidad | Cantidad | Amenazas |
|-----------|----------|---------|
| **CRÍTICO** | 13 | S-01, S-02, S-03, T-02, E-01, E-02, E-03, E-04, I-01, I-02, I-03 + más |
| **ALTO** | 11 | S-04, S-05, T-01, T-06, R-01, R-02, R-03, I-04, I-05, I-06, E-05, E-06, D-01, D-03 |
| **MEDIO** | 6 | T-04, T-05, R-04, R-05, D-02, D-04, I-07 |
| **BAJO** | 3 | T-03, D-05, I-08, I-09 |
| **Total** | **33** | |

---

## 6. Plan de Remediación Priorizado

### Sprint 1 — Críticos (Semana 1-2)
```
1. Eliminar console.log() de credenciales           [I-01]
2. Integrar ASP.NET Core Identity                   [S-01, S-03, I-02, I-03]
3. Agregar [Authorize] a TODAS las rutas            [E-02, E-03]
4. Eliminar query param ?role= — usar Claims        [S-02, E-01]
5. Hashear contraseñas (migración de datos)         [I-02, S-04]
```

### Sprint 2 — Altos (Semana 3-4)
```
6. Mover conn. string a Secret Manager              [I-04]
7. Implementar rate limiting en login y uploads     [D-01, AT-05]
8. Validar tipo MIME y extensión en file upload     [E-05]
9. Servir archivos via endpoint autenticado         [I-06]
10. Agregar security headers (CSP, X-Frame, etc.)  [I-09]
11. Validar IDOR en /edit/{id}                     [E-06, T-01]
```

### Sprint 3 — Medios y Auditoría (Semana 5-6)
```
12. Implementar AuditLog (Serilog + tabla BD)       [R-01, R-02, R-03, R-04]
13. Configurar expiración de sesión                 [S-05]
14. Lockout de cuentas tras N intentos fallidos     [S-04]
15. Paginación en listados de usuarios/cursos       [D-04]
```

---

## 7. Referencias

- STRIDE Threat Modeling — Microsoft Security Development Lifecycle (SDL)
- OWASP Top 10:2021 — https://owasp.org/Top10/
- CWE — Common Weakness Enumeration (MITRE)
- NIST SP 800-30 — Guide for Conducting Risk Assessments

---

*Documento generado en la etapa de Modelado de Amenazas del ciclo SecDevOps.*
