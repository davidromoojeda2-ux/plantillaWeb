# Spec Delta

## Purpose

Establece los contratos de organización modular para entidades del dominio, DTOs de transferencia de datos y mapeadores en archivos de código independientes con responsabilidad única.

## ADDED Requirements

### Requirement: Aislamiento de entidades de dominio en archivos individuales
The domain layer SHALL maintain each entity, sub-entity, and enum in its own standalone source file matching the type name.

#### Scenario: Separación de entidades y enums
- **WHEN** domain models and enums are compiled and referenced across layers
- **THEN** each type (including Article, ArticleStatus, Comment, CommentStatus, Visitor, ImageMetadata, and ImageVariant) is defined in its own dedicated source file

### Requirement: Desacoplamiento de DTOs en archivos individuales
The application layer SHALL define every input DTO, response DTO, and auxiliary transfer model in an independent source file matching its type name.

#### Scenario: Separación de DTOs por archivo
- **WHEN** application data contracts are consumed by controllers and services
- **THEN** each DTO (including ArticleDto, ArticleResponseDto, AuthorDto, CategoryDto, CategoryResponseDto, CommentSubmissionDto, CommentResponseDto, ImageUploadDto, ImageVariantDto, ImageMetadataResponseDto, PageDto, PageResponseDto, AdminLoginDto, and AdminUserDto) resides in its own isolated file

### Requirement: Mapeadores especializados por entidad y concepto
The application layer SHALL provide standalone mappers for each domain aggregate and concept in individual mapper files with single responsibility.

#### Scenario: Ejecución de mapeos bidireccionales
- **WHEN** domain entities are converted to DTOs or DTO inputs are applied to domain entities
- **THEN** mapping operations are executed through individual mapper classes dedicated to each aggregate without cross-file mingling
