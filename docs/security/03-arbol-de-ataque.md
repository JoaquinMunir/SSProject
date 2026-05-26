# Árbol de Ataque — TIProject

**Proyecto:** TIProject — Sistema de Gestión de Brigadas y Capacitaciones  
**Fecha:** 2026-05-25  
**Metodología:** Attack Tree (Schneier, 1999) adaptada a contexto web  
**Clasificación:** Confidencial — Uso Interno  
**Versión:** 1.0  

---

## 1. Objetivo del Árbol de Ataque

El árbol de ataque modela las rutas que un atacante podría seguir para comprometer el sistema TIProject. Cada nodo hoja representa una acción del atacante; los nodos internos representan condiciones (AND/OR) que deben cumplirse para alcanzar el objetivo padre.

**Convenciones:**
- `[OR]` = El atacante puede alcanzar el objetivo usando CUALQUIERA de los nodos hijo
- `[AND]` = El atacante DEBE completar TODOS los nodos hijo
- `[P]` = Probabilidad: A=Alta, M=Media, B=Baja
- `[E]` = Esfuerzo: 1=Muy bajo, 5=Muy alto
- `[I]` = Impacto: C=Crítico, A=Alto, M=Medio, B=Bajo

---

## 2. Árbol de Ataque Raíz

```
OBJETIVO RAÍZ: Comprometer TIProject
[OR]
├── 1. Obtener acceso no autorizado como administrador
├── 2. Robar o exponer datos sensibles de usuarios
├── 3. Ejecutar código malicioso en el servidor
├── 4. Interrumpir la disponibilidad del servicio
└── 5. Manipular datos de brigadas o cursos
```

---

## 3. Árbol Expandido

### OBJETIVO 1: Obtener Acceso No Autorizado como Administrador

```
1. Obtener acceso no autorizado como administrador [OR]
│
├── 1.1 Bypassear autenticación [OR]
│   │
│   ├── 1.1.1 Manipulación de cookie de sesión [AND]
│   │   │     [P:A] [E:1] [I:C]
│   │   ├── 1.1.1a Interceptar cookie HTTP (si HTTP disponible)
│   │   │         → Herramienta: Wireshark, Burp Suite
│   │   │         → Superficie: Cookie sin flag Secure (puerto 5264)
│   │   └── 1.1.1b Editar cookie en navegador (DevTools)
│   │             → Cambiar valor de email/role en cookie guardada
│   │             → La app confía ciegamente en la cookie
│   │
│   ├── 1.1.2 Fuerza bruta de contraseña [AND]
│   │   │     [P:A] [E:2] [I:C]
│   │   ├── 1.1.2a Enumerar usuarios vía UI (sin captcha)
│   │   ├── 1.1.2b Automatizar intentos (sin rate limiting)
│   │   │         → POST /login sin límite de intentos
│   │   └── 1.1.2c Usar diccionario de contraseñas comunes
│   │             → Contraseñas mínimo 8 chars sin complejidad fuerte
│   │
│   ├── 1.1.3 Acceso directo a ruta protegida [OR]
│   │   │     [P:A] [E:1] [I:C]
│   │   ├── 1.1.3a Navegar directamente a /dashboard_admin
│   │   │         → Sin middleware [Authorize] en rutas
│   │   │         → Sin redirección a login
│   │   ├── 1.1.3b Navegar a /add o /addCourse
│   │   │         → Crear usuario admin directamente
│   │   └── 1.1.3c Navegar a /usersView
│   │             → Ver todos los usuarios sin autenticación
│   │
│   └── 1.1.4 Escalación de privilegios via URL [AND]
│       │     [P:A] [E:1] [I:C]
│       ├── 1.1.4a Autenticarse con cuenta de bajo privilegio
│       ├── 1.1.4b Modificar query param: ?role=0 (admin)
│       └── 1.1.4c Acceder a funciones de administrador
│                 → Role validado solo en UI, no server-side
│
└── 1.2 Comprometer credenciales [OR]
    │
    ├── 1.2.1 Acceso a base de datos SQL Server [AND]
    │   │     [P:M] [E:3] [I:C]
    │   ├── 1.2.1a Obtener cadena de conexión (appsettings.json)
    │   ├── 1.2.1b Conectar a instancia localdb
    │   └── 1.2.1c Extraer tabla Users (passwords en plain-text)
    │             → SELECT * FROM Users → credenciales expuestas
    │
    ├── 1.2.2 Phishing de credenciales [P:M] [E:3] [I:A]
    │         → Sin 2FA, credencial única = acceso total
    │
    └── 1.2.3 Credential stuffing [P:M] [E:2] [I:A]
              → Sin bloqueo por intentos fallidos
              → Contraseñas simples en texto plano en BD
```

---

### OBJETIVO 2: Robar o Exponer Datos Sensibles

```
2. Robar o exponer datos sensibles [OR]
│
├── 2.1 Exfiltración via UI sin autenticación [AND]
│   │   [P:A] [E:1] [I:A]
│   ├── 2.1.1 Acceder a /usersView directamente (sin auth)
│   ├── 2.1.2 Ver lista completa de usuarios con datos personales
│   └── 2.1.3 Copiar: nombres, cédulas, emails, teléfonos, escuelas
│
├── 2.2 Exposición por log de consola [AND]
│   │   [P:A] [E:1] [I:A]
│   ├── 2.2.1 Abrir herramientas de desarrollador del navegador
│   ├── 2.2.2 Ir a consola
│   └── 2.2.3 Ver credenciales en console.log() durante login
│             → Login.razor registra email + password en consola
│
├── 2.3 Extracción de cookie con credenciales [AND]
│   │   [P:A] [E:1] [I:C]
│   ├── 2.3.1 Acceder a cookies del navegador (DevTools)
│   ├── 2.3.2 Leer valores: "savedAccount" y "savePassword"
│   └── 2.3.3 Obtener credenciales en texto plano del usuario
│
├── 2.4 SQL Injection (residual) [AND]
│   │   [P:B] [E:4] [I:C]
│   │   → EF Core mitiga en mayor parte, pero si hay consultas raw:
│   ├── 2.4.1 Identificar campos de entrada sin sanitización
│   ├── 2.4.2 Inyectar payload SQL (UNION SELECT, etc.)
│   └── 2.4.3 Extraer tabla Users completa
│
└── 2.5 Acceso a documentos institucionales [AND]
    │   [P:A] [E:1] [I:M]
    ├── 2.5.1 Enumerar /wwwroot/uploads/ (si Directory Browsing activo)
    ├── 2.5.2 Descargar archivos via URL directa
    └── 2.5.3 Acceder a documentos sin autorización
              → Archivos servidos como estáticos sin auth check
```

---

### OBJETIVO 3: Ejecutar Código Malicioso en el Servidor

```
3. Ejecutar código malicioso en el servidor [OR]
│
├── 3.1 Upload de archivo malicioso [AND]
│   │   [P:M] [E:2] [I:C]
│   ├── 3.1.1 Autenticarse (cualquier rol puede subir docs)
│   ├── 3.1.2 Preparar archivo malicioso
│   │   │     [OR]
│   │   ├── 3.1.2a Archivo .aspx/.ashx con código C# (web shell)
│   │   ├── 3.1.2b Archivo .exe o .bat
│   │   └── 3.1.2c HTML/JS malicioso (XSS stored via docs)
│   ├── 3.1.3 Subir archivo via /documentsView
│   │         → Sin validación de MIME ni extensión
│   └── 3.1.4 Ejecutar/acceder al archivo via URL directa
│             → wwwroot/uploads/{guid}.aspx
│
└── 3.2 Cross-Site Scripting (XSS) [AND]
    │   [P:M] [E:3] [I:A]
    │   → Blazor Server mitiga XSS en render, pero:
    ├── 3.2.1 Encontrar campo que renderice HTML sin escaping
    ├── 3.2.2 Inyectar payload JavaScript
    └── 3.2.3 Robar cookie de sesión / ejecutar acciones
              → Sin CSP headers configurados
```

---

### OBJETIVO 4: Interrumpir Disponibilidad del Servicio (DoS)

```
4. Interrumpir disponibilidad del servicio [OR]
│
├── 4.1 Saturación por uploads masivos [AND]
│   │   [P:M] [E:2] [I:A]
│   ├── 4.1.1 Script automatizado de uploads (20MB c/u)
│   ├── 4.1.2 Llenar disco del servidor
│   └── 4.1.3 Sin rate limiting en endpoint de archivos
│
├── 4.2 Creación masiva de usuarios [AND]
│   │   [P:A] [E:1] [I:M]
│   ├── 4.2.1 Acceder a /add sin autenticación
│   ├── 4.2.2 Script automatizado de creación de usuarios
│   └── 4.2.3 Saturar tabla Users en SQL Server
│
└── 4.3 Saturación de SignalR circuits [AND]
    │   [P:M] [E:3] [I:A]
    ├── 4.3.1 Abrir muchas conexiones simultáneas (Blazor Server)
    └── 4.3.2 Agotar memoria del servidor
              → Sin límite de conexiones concurrentes configurado
```

---

### OBJETIVO 5: Manipular Datos de Brigadas o Cursos

```
5. Manipular datos de brigadas o cursos [OR]
│
├── 5.1 Modificación directa de usuario [AND]
│   │   [P:A] [E:1] [I:A]
│   ├── 5.1.1 Acceder a /edit/{id} sin autenticación
│   ├── 5.1.2 Cambiar rol de cualquier usuario (admin, role, brigade)
│   └── 5.1.3 Sin validación server-side de permisos del solicitante
│
├── 5.2 Eliminación de datos críticos [AND]
│   │   [P:A] [E:1] [I:A]
│   ├── 5.2.1 Acceder a gestión de usuarios o cursos sin auth
│   ├── 5.2.2 Eliminar usuarios o cursos arbitrariamente
│   └── 5.2.3 Cascade delete puede borrar cursos relacionados
│
└── 5.3 Inscripción no autorizada a cursos [AND]
    │   [P:A] [E:1] [I:M]
    ├── 5.3.1 Acceder a /courseRegistration sin autenticación
    ├── 5.3.2 Inscribir o desinscribir usuarios arbitrariamente
    └── 5.3.3 Sin verificación de identidad del solicitante
```

---

## 4. Tabla de Resumen — Vectores de Ataque Prioritarios

| ID | Vector de Ataque | Probabilidad | Esfuerzo | Impacto | Riesgo Total | CWE Relacionado |
|----|-----------------|--------------|----------|---------|--------------|-----------------|
| AT-01 | Acceso directo a rutas protegidas (sin auth) | Alta | Muy bajo (1) | Crítico | **CRÍTICO** | CWE-862 |
| AT-02 | Escalación de privilegios via query param ?role= | Alta | Muy bajo (1) | Crítico | **CRÍTICO** | CWE-269 |
| AT-03 | Lectura de credenciales en console.log() | Alta | Muy bajo (1) | Alto | **CRÍTICO** | CWE-532 |
| AT-04 | Lectura de cookie con password plain-text | Alta | Muy bajo (1) | Crítico | **CRÍTICO** | CWE-312 |
| AT-05 | Fuerza bruta sin rate limiting | Alta | Bajo (2) | Crítico | **ALTO** | CWE-307 |
| AT-06 | Upload de web shell (.aspx sin validación) | Media | Bajo (2) | Crítico | **ALTO** | CWE-434 |
| AT-07 | Extracción de contraseñas plain-text de BD | Media | Medio (3) | Crítico | **ALTO** | CWE-256 |
| AT-08 | Acceso a documentos sin autenticación | Alta | Muy bajo (1) | Medio | **ALTO** | CWE-285 |
| AT-09 | Creación masiva de usuarios (DoS) | Alta | Bajo (2) | Medio | **MEDIO** | CWE-400 |
| AT-10 | Upload masivo para agotar disco (DoS) | Media | Bajo (2) | Alto | **MEDIO** | CWE-400 |
| AT-11 | SQL Injection (residual — EF Core mitiga) | Baja | Alto (4) | Crítico | **BAJO** | CWE-89 |
| AT-12 | XSS (Blazor mitiga en gran medida) | Baja | Alto (4) | Alto | **BAJO** | CWE-79 |

---

## 5. Contramedidas por Vector de Ataque

| ID Vector | Contramedida Principal | Capa | Prioridad |
|-----------|----------------------|------|-----------|
| AT-01 | Agregar `[Authorize]` a todas las rutas protegidas | Aplicación | P1 |
| AT-02 | Usar `ClaimsPrincipal` — nunca confiar en query params | Aplicación | P1 |
| AT-03 | Eliminar todos los `console.log()` de credenciales | Aplicación | P1 |
| AT-04 | Eliminar cookies manuales, usar ASP.NET Core Identity | Aplicación | P1 |
| AT-05 | Implementar `AddRateLimiter()` con policy por IP | Aplicación | P2 |
| AT-06 | Whitelist de extensiones + validación MIME en upload | Aplicación | P2 |
| AT-07 | Hashear contraseñas con PBKDF2 (Identity) | Datos | P1 |
| AT-08 | Servir archivos solo via endpoint autenticado | Aplicación | P2 |
| AT-09 | Rate limiting + CAPTCHA en registro | Aplicación | P3 |
| AT-10 | Rate limiting en uploads + límite de cuota por usuario | Aplicación | P3 |
| AT-11 | Revisión de consultas raw, mantener EF Core | Datos | P3 |
| AT-12 | Configurar Content Security Policy headers | Infraestructura | P2 |

---

*Documento generado en la etapa de Modelado de Amenazas del ciclo SecDevOps.*
