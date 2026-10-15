# Spec Delta

## Purpose

Define el comportamiento del renderizado modular de la interfaz pública mediante componentes y vistas parciales aisladas para autor, botones de compartir, comentarios y artículos recomendados.

## ADDED Requirements

### Requirement: Renderizado desacoplado de la ficha de autor
The system SHALL render author profile information in article views through a dedicated, isolated presentation component without embedding raw author layout in the parent view.

#### Scenario: Visualización completa del autor
- **WHEN** a reader views an article with author details populated
- **THEN** the author card is rendered via the dedicated component displaying the author's name, role, bio, avatar, and social profile link

### Requirement: Renderizado modular de botones para compartir
The system SHALL render social sharing actions and copy-link interactions through an isolated, reusable share presentation component.

#### Scenario: Visualización de barra de compartir
- **WHEN** a reader views an article page
- **THEN** the social share bar is rendered through the isolated component providing direct actions for social networks and URL clipboard copy

### Requirement: Modularización de la sección de comentarios
The system SHALL isolate comment listings and comment submission forms into independent presentation components separated from the main article template.

#### Scenario: Despliegue de comentarios y formulario
- **WHEN** an article page is rendered with its comment stream
- **THEN** the approved comments list and the submission form are rendered via dedicated components maintaining the main view decoupled

### Requirement: Modularización de recomendaciones y tarjetas de artículo
The system SHALL render recommended article grids and article cards through reusable partial components across home, article, and category views.

#### Scenario: Renderizado de artículos recomendados
- **WHEN** an article displays related or recommended content
- **THEN** each recommended article item is rendered using a shared card component with its cover image, title, author, and reading time
