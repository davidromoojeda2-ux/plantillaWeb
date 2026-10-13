# Spec Delta: admin-auth

## Purpose
Proporciona el inicio de sesión seguro, gestión de sesión mediante cookies y control de acceso protegido para administradores de contenido en el área administrativa.

## ADDED Requirements

### Requirement: Autenticación de administradores
El sistema SHALL validar las credenciales de usuario administrador y emitir una cookie de autenticación segura.

#### Scenario: Credenciales válidas de administrador
- **WHEN** un administrador ingresa su usuario y contraseña correctos en `/admin/login`
- **THEN** el sistema establece la cookie de sesión autenticada y redirige al panel principal `/admin`

#### Scenario: Credenciales inválidas
- **WHEN** se ingresan credenciales incorrectas
- **THEN** el sistema deniega el acceso y muestra un mensaje de error sin revelar qué campo falló

### Requirement: Protección de rutas administrativas
El sistema SHALL denegar el acceso anónimo a todas las páginas ubicadas bajo el prefijo `/admin`, excepto la página de inicio de sesión.

#### Scenario: Acceso anónimo a ruta protegida
- **WHEN** un visitante sin sesión activa intenta acceder a cualquier pantalla de `/admin`
- **THEN** el sistema redirige automáticamente al formulario `/admin/login`

### Requirement: Cierre de sesión de administrador
El sistema SHALL invalidar la cookie de autenticación cuando el administrador finalice su sesión.

#### Scenario: Cierre de sesión exitoso
- **WHEN** el administrador pulsa la opción de cerrar sesión
- **THEN** el sistema destruye la sesión y redirige a la pantalla de acceso
