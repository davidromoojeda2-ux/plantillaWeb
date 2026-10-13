# Spec Delta: article-comments

## Purpose
Gestiona la recepción de comentarios de visitantes sin autenticación previa, protecciones antispam y el flujo de moderación administrativa para su publicación en artículos.

## ADDED Requirements

### Requirement: Envío de comentarios por visitantes
El sistema SHALL permitir a los visitantes enviar comentarios en artículos habilitados proporcionando su nombre, correo electrónico y mensaje, sin requerir una cuenta de usuario.

#### Scenario: Envío válido de comentario
- **WHEN** un visitante envía el formulario con datos válidos y consentimiento requerido
- **THEN** el sistema registra o actualiza al visitante en MongoDB y almacena el comentario con estado pendiente

#### Scenario: Protección por honeypot
- **WHEN** un bot completa el campo oculto de trampa (honeypot)
- **THEN** el sistema descarta la solicitud silenciosamente sin persistir el comentario

### Requirement: Protección de privacidad del visitante
El sistema SHALL salvaguardar los datos personales del visitante y evitar la exposición pública de su correo o dirección IP.

#### Scenario: Renderizado público de comentario
- **WHEN** un comentario es mostrado públicamente en el artículo
- **THEN** el sistema muestra exclusivamente el nombre del visitante y la fecha, manteniendo oculto el correo electrónico y almacenando la IP únicamente como hash

### Requirement: Moderación y visibilidad pública de comentarios
El sistema SHALL condicionar la visualización de los comentarios al estado de aprobación otorgado por un administrador.

#### Scenario: Comentario pendiente o rechazado
- **WHEN** un comentario se encuentra en estado pendiente o rechazado
- **THEN** el sistema no lo incluye en el listado público de comentarios del artículo

#### Scenario: Comentario aprobado
- **WHEN** un administrador aprueba el comentario desde el panel de gestión
- **THEN** el comentario se vuelve inmediatamente visible en la sección de comentarios del artículo
