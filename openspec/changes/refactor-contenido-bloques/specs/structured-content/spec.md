# Spec Delta

## Purpose

Define el contenido editorial estructurado en bloques tipados para artículos y páginas informativas, incluyendo su validación, sanitización, edición y renderizado seguro.

## ADDED Requirements

### Requirement: Modelo de contenido por bloques tipados

El contenido editorial SHALL representarse como una secuencia ordenada de bloques. Cada bloque MUST declarar un tipo soportado y MUST contener únicamente los campos definidos para ese tipo.

#### Scenario: Bloque válido con su tipo y campos

- **WHEN** se guarda un artículo o página con un bloque de tipo párrafo y un contenido de texto válido
- **THEN** el sistema persiste el bloque con su tipo y contenido, y lo devuelve en el mismo orden al consultarlo

#### Scenario: Tipo de bloque desconocido

- **WHEN** se recibe un bloque con un tipo que no está en el catálogo soportado
- **THEN** el sistema rechaza la operación completa y devuelve un error de validación, sin persistir ningún bloque

#### Scenario: Campo no permitido para el tipo

- **WHEN** un bloque incluye propiedades que no corresponden a su tipo
- **THEN** el sistema rechaza el bloque y devuelve un error de validación

### Requirement: Validación estructural y límites de contenido

El sistema MUST validar la estructura del cuerpo antes de persistirlo: cantidad máxima de bloques, longitud de textos, dimensiones de tablas y contenido obligatorio para publicar. Los límites MUST aplicarse tanto en creación como en edición.

#### Scenario: Cuerpo vacío al publicar

- **WHEN** se intenta publicar un artículo o página sin bloques de contenido
- **THEN** el sistema rechaza la publicación con un error de validación asociado al cuerpo

#### Scenario: Tabla fuera de límites

- **WHEN** una tabla supera la cantidad máxima de filas o columnas permitida
- **THEN** el sistema rechaza el bloque y no persiste el contenido

#### Scenario: Cantidad excesiva de bloques

- **WHEN** el cuerpo supera el número máximo de bloques permitido
- **THEN** el sistema rechaza la operación con un error de validación

#### Scenario: Encabezado con nivel no permitido

- **WHEN** un bloque de encabezado declara un nivel distinto de H2 o H3
- **THEN** el sistema rechaza el bloque con un error de validación

### Requirement: Sanitización del HTML inline

El sistema MUST sanitizar en servidor todo HTML inline aceptado dentro de bloques de texto, aplicando una lista blanca de etiquetas, atributos y esquemas de URL. El HTML no permitido MUST eliminarse antes de persistir y antes de renderizar.

#### Scenario: Intento de inyectar script

- **WHEN** un bloque de texto contiene una etiqueta `script`, un manejador de eventos o un atributo `style`
- **THEN** el sistema elimina esos elementos y conserva únicamente el contenido permitido

#### Scenario: Enlace con esquema peligroso

- **WHEN** un enlace inline utiliza un esquema como `javascript:` o `data:`
- **THEN** el sistema rechaza o elimina el enlace y no lo renderiza como URL navegable

#### Scenario: Enlace externo permitido

- **WHEN** un enlace inline utiliza `https` o `mailto`
- **THEN** el sistema conserva el enlace y añade los atributos de seguridad necesarios al renderizarlo

### Requirement: Referencias de imagen seguras

Los bloques de imagen MUST referenciar una imagen de la biblioteca mediante su identificador o una URL externa con esquema y host permitidos. Los textos alternativo, pie de foto y crédito MUST tratarse como texto plano.

#### Scenario: Imagen de la biblioteca

- **WHEN** un bloque de imagen referencia un identificador existente de la biblioteca de medios
- **THEN** el sistema resuelve la URL de la variante adecuada y la renderiza con el texto alternativo indicado

#### Scenario: URL de imagen no permitida

- **WHEN** un bloque de imagen utiliza un esquema `data:`, `javascript:` o un host fuera de la lista permitida
- **THEN** el sistema rechaza el bloque con un error de validación

#### Scenario: Texto alternativo con HTML

- **WHEN** el texto alternativo, pie de foto o crédito contiene etiquetas HTML
- **THEN** el sistema los almacena y renderiza como texto plano, sin interpretar el HTML

### Requirement: Contrato JSON estricto del editor

El editor administrativo MUST enviar el cuerpo como JSON estructurado. El servidor MUST deserializarlo con un contrato estricto, rechazar propiedades y tipos desconocidos, y asociar cualquier error al formulario sin persistir datos parciales.

#### Scenario: JSON válido

- **WHEN** el editor envía un arreglo de bloques con tipos y campos válidos
- **THEN** el servidor acepta el cuerpo y lo procesa para su sanitización y persistencia

#### Scenario: JSON malformado

- **WHEN** el editor envía un JSON inválido o incompleto
- **THEN** el servidor rechaza la operación y muestra un error de validación en el formulario

#### Scenario: Error de validación con conservación de contenido

- **WHEN** el servidor rechaza el cuerpo por un error de validación
- **THEN** la vista de edición conserva el JSON enviado para que el usuario pueda corregirlo sin perder su trabajo

### Requirement: Renderizado seguro y semántico

El sistema MUST renderizar el contenido en servidor a partir de los bloques tipados, usando HTML semántico y codificando por defecto todo texto plano. Solo el HTML previamente sanitizado MAY emitirse como HTML confiable.

#### Scenario: Renderizado de un artículo

- **WHEN** un lector solicita un artículo publicado
- **THEN** el sistema renderiza sus bloques con encabezados, párrafos, listas, imágenes, tablas y citas semánticas, sin ejecutar HTML no permitido

#### Scenario: Renderizado de una página informativa

- **WHEN** un lector solicita una página informativa publicada
- **THEN** el sistema aplica el mismo renderizado por bloques y las mismas reglas de seguridad que en los artículos

#### Scenario: Contenido no confiable en base de datos

- **WHEN** un documento almacenado contiene HTML no sanitizado
- **THEN** el renderizador no lo emite como HTML confiable y lo trata según la política de sanitización

### Requirement: Campos derivados del contenido

El sistema MUST derivar de los bloques el texto plano de búsqueda, el tiempo estimado de lectura y la primera imagen disponible para portada o Hero. Estos campos MUST actualizarse en cada creación o edición.

#### Scenario: Búsqueda por texto del cuerpo

- **WHEN** un lector busca una palabra que aparece únicamente dentro de un bloque de párrafo
- **THEN** el artículo aparece en los resultados de búsqueda

#### Scenario: Tiempo de lectura

- **WHEN** se guarda un artículo con varios bloques de texto
- **THEN** el tiempo estimado de lectura se calcula a partir del texto plano de los bloques y queda almacenado

#### Scenario: Detección de imagen para Hero

- **WHEN** un artículo se marca como destacado y contiene una imagen en su cuerpo pero no tiene portada asignada
- **THEN** el sistema detecta la primera imagen del cuerpo como imagen disponible para el Hero

### Requirement: Persistencia y recuperación del contenido estructurado

El sistema MUST persistir los bloques con su tipo, orden y payload, y MUST recuperarlos con la misma estructura al consultar el artículo o la página. El contenido legado basado en un string HTML deja de ser el formato de almacenamiento.

#### Scenario: Ida y vuelta de bloques

- **WHEN** se guarda un cuerpo con bloques de distintos tipos y luego se consulta la entidad
- **THEN** se recuperan los mismos tipos, el mismo orden y los mismos datos sanitizados

#### Scenario: Reinicio de la aplicación

- **WHEN** la aplicación se reinicia después de guardar contenido estructurado
- **THEN** los bloques se siguen recuperando y renderizando correctamente

### Requirement: Edición administrativa por bloques

El panel administrativo MUST ofrecer un editor que permita crear, editar, reordenar y eliminar bloques, aplicar formato inline permitido, insertar imágenes de la biblioteca o por URL permitida, y editar tablas, callouts, citas y código.

#### Scenario: Creación de un artículo con varios bloques

- **WHEN** un administrador crea un artículo y añade un encabezado, un párrafo, una imagen y una tabla
- **THEN** el editor guarda los cuatro bloques y el artículo se renderiza con ellos en el orden elegido

#### Scenario: Edición de un artículo existente

- **WHEN** un administrador abre un artículo existente
- **THEN** el editor se hidrata desde el JSON almacenado y permite modificar los bloques sin perder su contenido

#### Scenario: Reordenamiento y eliminación

- **WHEN** un administrador mueve o elimina un bloque
- **THEN** el editor actualiza el orden o la ausencia del bloque y lo refleja en el JSON que se enviará al servidor
