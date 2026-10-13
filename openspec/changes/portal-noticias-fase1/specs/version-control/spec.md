# Spec Delta: version-control

## Purpose
Establece los puntos de control obligatorios mediante commits de Git a lo largo de la implementación para garantizar trazabilidad, reproducibilidad y estabilidad incremental del proyecto.

## ADDED Requirements

### Requirement: Puntos de control mediante commits de Git
El flujo de implementación SHALL registrar commits en el repositorio de Git como puntos de control tras completar y validar cada bloque funcional o hito verificable del desarrollo.

#### Scenario: Commit al completar bloque o capa arquitectónica
- **WHEN** se implementa y valida con éxito un conjunto de tareas de una capa (Dominio, Infraestructura, Aplicación, Estilos, Admin, Portal Público)
- **THEN** el sistema registra un commit de control con un mensaje descriptivo siguiendo la convención de commits estructurados

### Requirement: Validación previa al commit de control
El flujo de desarrollo SHALL verificar que el código compile sin errores antes de registrar un commit de punto de control.

#### Scenario: Bloqueo de commit ante fallos de compilación
- **WHEN** una tarea o bloque presenta errores de compilación (`dotnet build` o `pnpm run build:css`)
- **THEN** el sistema no registra el commit de control hasta que todos los errores hayan sido resueltos satisfactoriamente
