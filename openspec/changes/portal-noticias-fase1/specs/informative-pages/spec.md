# Spec Delta: informative-pages

## Purpose
Permite la administración y renderizado de secciones informativas independientes de las noticias, el pie de página dinámico y el aviso de consentimiento de cookies.

## ADDED Requirements

### Requirement: Renderizado de páginas informativas con índice navegable
El sistema SHALL presentar el contenido estático institucional en `/info/{slug}` conforme al diseño de referencia (![Páginas informativas](../../assets/frame%20politicas%20de%20calidad.png)), incluyendo encabezado hero con fecha de última actualización, título, subtítulo e índice navegable de secciones.

#### Scenario: Visualización de página informativa
- **WHEN** un visitante accede a una ruta informativa como `/info/politica-de-privacidad` o `/info/politica-de-calidad`
- **THEN** el sistema entrega la cabecera hero con la fecha de actualización, el título y un índice de secciones internas para saltar al contenido deseado

### Requirement: Pie de página dinámico estructurado
El sistema SHALL construir el pie de página conforme al diseño de referencia (![Footer de referencia](../../assets/frame%20footer.png)) con fondo oscuro, navegación multiculumna, selector de idioma, enlaces legales e iconos de redes sociales.

#### Scenario: Construcción de enlaces en pie de página
- **WHEN** se renderiza el pie de página en cualquier vista del portal
- **THEN** se organiza en columnas temáticas con enlaces a páginas institucionales, fila de enlaces legales/privacidad y barra de redes sociales

### Requirement: Aviso y consentimiento de cookies
El sistema SHALL mostrar un aviso de cookies con enlace a la política de cookies y registrar la aceptación del visitante.

#### Scenario: Aceptación del aviso de cookies
- **WHEN** un visitante acepta el aviso de cookies
- **THEN** el sistema oculta el aviso y recuerda la preferencia en el navegador del usuario
