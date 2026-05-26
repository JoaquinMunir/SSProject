# Documentación de Seguridad — TIProject

**Sistema:** TIProject — Gestión de Brigadas y Capacitaciones  
**Framework:** SecDevOps  
**Fecha:** 2026-05-25  

---

## Documentos

| # | Documento | Descripción | Etapa SecDevOps |
|---|-----------|-------------|-----------------|
| 01 | [Matriz de Activos de Información](01-matriz-activos-informacion.md) | 25 activos clasificados por CIA, 7 críticos identificados | Plan |
| 02 | [Arquitectura de Seguridad](02-arquitectura-seguridad.md) | Arquitectura AS-IS con vulnerabilidades y TO-BE con controles | Plan / Design |
| 03 | [Árbol de Ataque](03-arbol-de-ataque.md) | 12 vectores de ataque, probabilidad, esfuerzo e impacto | Plan |
| 04 | [Modelado de Amenazas STRIDE](04-stride-threat-model.md) | 33 amenazas clasificadas, plan de remediación priorizado | Plan |
| 05 | [Documentación SecDevOps](05-secdevops-documentacion.md) | Controles, artefactos y procedimientos para las 8 etapas | Todas |

## Hallazgos Críticos (Top 5 — Acción Inmediata)

| # | Hallazgo | Archivo | CWE |
|---|---------|---------|-----|
| 1 | Contraseñas en texto plano en BD y cookies | `Login.razor`, `Users` tabla | CWE-256 |
| 2 | Sin [Authorize] en rutas protegidas | Todos los Dashboard*.razor | CWE-862 |
| 3 | Rol obtenido desde query param URL | `Login.razor` (redirect) | CWE-269 |
| 4 | console.log() con credenciales | `Login.razor` | CWE-532 |
| 5 | Sin validación de tipo en file upload | `Documents.cs` | CWE-434 |

## Stack del Proyecto

- ASP.NET Core Blazor Server (.NET 8)  
- SQL Server LocalDB (EF Core 9)  
- 5 roles: admin, brigaderChief, brigader, auxiliar, student  
- Módulos: Usuarios, Cursos, Documentos  
