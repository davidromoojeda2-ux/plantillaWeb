# Spec Delta: news-navigation

## Purpose
Provee la navegación global del portal cargando dinámicamente el árbol de categorías y subcategorías desde MongoDB con almacenamiento en caché en memoria y resiliencia ante fallos.

## ADDED Requirements

### Requirement: Carga dinámica del menú de categorías
El sistema SHALL consultar las categorías activas y sus subcategorías asociadas desde la colección de categorías en MongoDB para construir el menú de navegación principal conforme al diseño de referencia (![Navbar de referencia](../../assets/frame%20navbar.png)).

#### Scenario: Renderizado exitoso del árbol de categorías
- **WHEN** un visitante accede a cualquier página del portal
- **THEN** el Navbar muestra las categorías ordenadas por orden de visualización y despliega sus subcategorías en menús interactivos con indicadores de flecha, acceso directo para categorías sin hijos, y botón CTA destacado

#### Scenario: Categoría sin subcategorías
- **WHEN** una categoría no tiene subcategorías con su identificador como padre
- **THEN** el elemento del menú actúa como un enlace directo sin desplegable

### Requirement: Almacenamiento en caché y resiliencia del menú
El sistema SHALL almacenar el árbol de navegación en memoria utilizando expiración temporal e invalidación reactiva al guardar categorías.

#### Scenario: Consulta desde caché en memoria
- **WHEN** se solicita el menú de navegación dentro del período de validez de la caché
- **THEN** el sistema entrega la estructura desde memoria sin realizar consultas a MongoDB

#### Scenario: Recuperación ante fallo de base de datos
- **WHEN** la conexión a MongoDB no está disponible y existe una versión previa en memoria
- **THEN** el sistema entrega la última versión en caché garantizando la disponibilidad del Navbar

### Requirement: Enlaces de navegación estructurada
El sistema SHALL generar enlaces consistentes basados en slugs para cada categoría raíz y subcategoría.

#### Scenario: Clic en categoría raíz
- **WHEN** el usuario hace clic en una categoría raíz
- **THEN** el sistema redirige a la ruta `/categorias/{slug}`

#### Scenario: Clic en subcategoría
- **WHEN** el usuario hace clic en una subcategoría del Navbar
- **THEN** el sistema redirige a la ruta `/categorias/{slug-padre}/{slug}`
