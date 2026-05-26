# Matriz de Activos de Información — TIProject

**Proyecto:** TIProject — Sistema de Gestión de Brigadas y Capacitaciones  
**Fecha:** 2026-05-25  
**Clasificación del documento:** Confidencial — Uso Interno  
**Versión:** 1.0  

---

## 1. Contexto del Sistema

TIProject es una aplicación web desarrollada en ASP.NET Core Blazor (.NET 8) que gestiona:
- Registro y administración de usuarios con diferentes roles (Admin, Jefe de Brigada, Brigadista, Auxiliar, Estudiante)
- Gestión de cursos y capacitaciones
- Carga y distribución de documentos institucionales
- Asignación de brigadas (Primeros Auxilios, Evacuación, Incendios, Rescate, Comunicaciones)

---

## 2. Clasificación de Activos

### 2.1 Escala de Clasificación

| Nivel | Etiqueta | Descripción |
|-------|----------|-------------|
| 4 | Crítico | Compromiso causa daño severo o irreversible |
| 3 | Alto | Compromiso causa daño significativo al sistema u organización |
| 2 | Medio | Compromiso causa daño moderado o temporal |
| 1 | Bajo | Compromiso causa impacto menor o recuperable |

### 2.2 Dimensiones de Evaluación (CIA)

- **C** = Confidencialidad  
- **I** = Integridad  
- **D** = Disponibilidad  

---

## 3. Matriz de Activos de Información

### 3.1 Activos de Datos

| ID | Activo | Descripción | Clasificación | C | I | D | Propietario | Ubicación | Formato |
|----|--------|-------------|---------------|---|---|---|-------------|-----------|---------|
| DA-01 | Credenciales de usuarios | Email y contraseñas de acceso al sistema | Crítico (4) | 4 | 4 | 3 | Admin del Sistema | SQL Server — Tabla `Users` (campo `Password`) | Plain-text en BD |
| DA-02 | Datos personales de usuarios | Nombre, cédula, teléfono, dirección, colegio, tipo de sangre | Alto (3) | 4 | 3 | 2 | Admin del Sistema | SQL Server — Tabla `Users` | Registros BD |
| DA-03 | Datos de cursos y capacitaciones | Nombre, fecha, lugar, instructor asignado | Medio (2) | 2 | 3 | 3 | Admin del Sistema | SQL Server — Tabla `Courses` | Registros BD |
| DA-04 | Asignaciones de brigada | Qué brigada pertenece cada usuario | Alto (3) | 3 | 4 | 3 | Jefe de Brigada | SQL Server — Tabla `Users` (campo `Brigade`) | Enum int en BD |
| DA-05 | Roles de usuario | Nivel de privilegio de cada usuario | Crítico (4) | 3 | 4 | 4 | Admin del Sistema | SQL Server — Tabla `Users` (campo `Role`) | Enum int en BD |
| DA-06 | Documentos institucionales | Archivos PDF/imágenes subidos al sistema | Alto (3) | 3 | 3 | 3 | Admin / Jefe de Brigada | `wwwroot/uploads/` + `Data/documentos_db.json` | Archivos + JSON |
| DA-07 | Metadatos de documentos | Nombres, rutas, tipos de archivo subidos | Medio (2) | 2 | 3 | 2 | Admin del Sistema | `Data/documentos_db.json` | JSON |
| DA-08 | Tokens de sesión (cookies) | Cookies de navegador con credenciales en texto plano | Crítico (4) | 4 | 4 | 2 | Usuario final | Navegador del cliente | Cookie HTTP |
| DA-09 | Registros de inscripción a cursos | Qué usuarios están inscritos en cada curso | Medio (2) | 2 | 3 | 2 | Admin del Sistema | SQL Server — relación `Users.CourseId` | FK en BD |

---

### 3.2 Activos de Software / Aplicación

| ID | Activo | Descripción | Clasificación | C | I | D | Propietario | Ubicación |
|----|--------|-------------|---------------|---|---|---|-------------|-----------|
| SA-01 | Código fuente de la aplicación | Proyecto ASP.NET Core Blazor completo | Crítico (4) | 4 | 4 | 3 | Equipo de desarrollo | `TIProject/` (repo local) |
| SA-02 | Lógica de autenticación | Componente `Login.razor` y validación de credenciales | Crítico (4) | 4 | 4 | 4 | Equipo de desarrollo | `Components/Pages/Login.razor` |
| SA-03 | Capa de acceso a datos (Repositorios) | `RepoUsers.cs`, `RepoCourses.cs` — acceso directo a BD | Alto (3) | 3 | 4 | 3 | Equipo de desarrollo | `Repository/` |
| SA-04 | Contexto de base de datos (EF Core) | `TIProjectDbContext.cs` — configuración ORM | Alto (3) | 3 | 4 | 4 | Equipo de desarrollo | `Data/TIProjectDbContext.cs` |
| SA-05 | Lógica de gestión de archivos | Clase `Documents.cs` singleton para upload | Alto (3) | 3 | 3 | 3 | Equipo de desarrollo | `Data/Documents.cs` |
| SA-06 | Migraciones de base de datos | Scripts de creación/modificación del esquema BD | Alto (3) | 3 | 4 | 3 | Equipo de desarrollo | `Migrations/` |
| SA-07 | Suite de pruebas unitarias | Proyecto `TIProject.Tests` con xUnit | Medio (2) | 2 | 3 | 2 | Equipo de desarrollo | `TIProject.Tests/` |
| SA-08 | Activos web estáticos | Bootstrap, JS (login.js, site.js), CSS | Bajo (1) | 1 | 2 | 2 | Equipo de desarrollo | `wwwroot/` |

---

### 3.3 Activos de Infraestructura / Configuración

| ID | Activo | Descripción | Clasificación | C | I | D | Propietario | Ubicación |
|----|--------|-------------|-----------|---|---|---|-------------|-----------|
| IA-01 | Cadena de conexión a base de datos | String de conexión SQL Server en configuración | Crítico (4) | 4 | 4 | 4 | Admin del Sistema | `appsettings.json` |
| IA-02 | Base de datos SQL Server (LocalDB) | Instancia `MSSQLLocalDB` — `TIProjectDB` | Crítico (4) | 4 | 4 | 4 | Admin del Sistema | Servidor local (localdb) |
| IA-03 | Configuración de entorno | `appsettings.json`, `appsettings.Development.json` | Alto (3) | 3 | 4 | 3 | Equipo de desarrollo | Raíz del proyecto |
| IA-04 | Configuración de lanzamiento | `launchSettings.json` — puertos y perfiles | Medio (2) | 2 | 3 | 3 | Equipo de desarrollo | `Properties/launchSettings.json` |
| IA-05 | Servidor web (Kestrel/IIS Express) | Proceso que sirve la aplicación Blazor | Alto (3) | 2 | 3 | 4 | Admin del Sistema | Runtime .NET 8 |
| IA-06 | Directorio de uploads | Carpeta con archivos subidos por usuarios | Alto (3) | 3 | 3 | 3 | Admin del Sistema | `wwwroot/uploads/` |
| IA-07 | Dependencias NuGet | Paquetes de terceros usados por la aplicación | Medio (2) | 2 | 3 | 3 | Equipo de desarrollo | `TIProject.csproj` + caché local |

---

### 3.4 Activos Humanos y de Proceso

| ID | Activo | Descripción | Clasificación | C | I | D |
|----|--------|-------------|---------------|---|---|---|
| PA-01 | Credenciales de administrador del sistema | Cuenta admin con acceso total | Crítico (4) | 4 | 4 | 4 |
| PA-02 | Credenciales de Jefe de Brigada | Cuenta con permisos de gestión de brigada | Alto (3) | 4 | 3 | 3 |
| PA-03 | Datos de estudiantes y auxiliares | Información personal del personal de brigadas | Alto (3) | 4 | 2 | 2 |
| PA-04 | Proceso de gestión de cursos | Flujo de creación y asignación de capacitaciones | Medio (2) | 2 | 3 | 3 |
| PA-05 | Proceso de asignación de brigadas | Flujo de asignación de roles y brigadas | Alto (3) | 3 | 4 | 3 |

---

## 4. Mapa de Dependencias de Activos

```
[Usuario Final / Navegador]
        │
        ├─► DA-08 (Cookie de sesión — plain-text) ──────► CRÍTICO
        │
        ▼
[Aplicación Web — Blazor Server]
        │
        ├─► SA-01 Código fuente
        ├─► SA-02 Lógica de Auth (Login.razor)
        ├─► SA-03 Repositorios
        ├─► SA-05 Gestión de archivos
        │
        ▼
[IA-01 Cadena de conexión]
        │
        ▼
[IA-02 SQL Server LocalDB]
        │
        ├─► DA-01 Credenciales (plain-text) ──────────► CRÍTICO
        ├─► DA-02 Datos personales
        ├─► DA-04 Brigadas
        ├─► DA-05 Roles ─────────────────────────────► CRÍTICO
        └─► DA-03 Cursos
        
[IA-06 Directorio uploads]
        │
        └─► DA-06 Documentos institucionales
            DA-07 Metadatos (documentos_db.json)
```

---

## 5. Resumen de Criticidad

| Nivel | Cantidad | Activos |
|-------|----------|---------|
| Crítico (4) | 7 | DA-01, DA-05, DA-08, SA-01, SA-02, IA-01, IA-02 |
| Alto (3) | 10 | DA-02, DA-04, DA-06, SA-03, SA-04, SA-05, SA-06, IA-03, IA-05, IA-06 |
| Medio (2) | 7 | DA-03, DA-07, DA-09, SA-07, IA-04, IA-07, PA-04 |
| Bajo (1) | 1 | SA-08 |

---

## 6. Brechas de Seguridad Identificadas por Activo

| Activo | Brecha Actual | Impacto | Recomendación |
|--------|---------------|---------|---------------|
| DA-01 — Contraseñas | Almacenadas en texto plano en SQL Server | Compromiso total de cuentas ante fuga de BD | Implementar bcrypt/Argon2 para hashing |
| DA-08 — Cookies | Credenciales guardadas en cookies del cliente | Robo de sesión, replay attack | Usar tokens de sesión opacos + HttpOnly/Secure |
| DA-05 — Roles | Sin validación server-side del rol en rutas | Escalación de privilegios horizontal | Implementar [Authorize(Roles=...)] en cada página |
| IA-01 — Conn. String | En archivo de configuración sin cifrado | Exposición de acceso a BD | Usar Secret Manager o Azure Key Vault |
| DA-06 — Archivos | Sin validación de tipo MIME ni escaneo | Subida de malware/scripts | Validar extensión + MIME + límite de tamaño |
| SA-02 — Auth | Sin framework de autenticación estándar | Bypasseo trivial de autenticación | Adoptar ASP.NET Core Identity o OIDC |

---

*Documento generado en la etapa de Planificación del ciclo SecDevOps — Revisión requerida ante cambios de arquitectura.*
