# Spec Delta

## Purpose

Define el pipeline de automatización que descubre temáticas actuales mediante Serper, genera artículos estructurados con IA y los publica o deja en borrador, garantizando resiliencia y control administrativo.

## ADDED Requirements

### Requirement: Descubrimiento de temáticas mediante Serper (opcional)

El sistema SHALL permitir elegir si descubre temáticas con Serper. Cuando el descubrimiento está activado, SHALL consultar Serper a partir de los temas base, el país y el idioma configurados dentro de una ventana temporal y MUST devolver los resultados para usarlos como keywords. Cuando está desactivado, MUST usar las temáticas base directamente como keywords principales.

#### Scenario: Descubrimiento activado con Serper

- **WHEN** un administrador activa "Descubrir con Serper" y ejecuta una corrida
- **THEN** el sistema busca con Serper y usa los resultados obtenidos como keywords para generar los artículos

#### Scenario: Descubrimiento desactivado

- **WHEN** un administrador desactiva "Descubrir con Serper"
- **THEN** el sistema omite Serper y usa las temáticas base directamente como keywords principales

#### Scenario: Serper no disponible

- **WHEN** el descubrimiento con Serper está activado y Serper no responde o devuelve un error durante una corrida
- **THEN** el sistema registra el fallo y no genera artículos en esa corrida

### Requirement: Deduplicación con decisión del usuario

El sistema MUST detectar las temáticas candidatas que ya fueron procesadas o que cuentan con un artículo equivalente, comparando una forma normalizada del título. Cuando detecta una repetición, el sistema SHALL marcarla como pendiente de decisión sin detener la corrida y MUST permitir al usuario decidir si continúa con la creación del artículo, para no bloquear temáticas que se repiten de nombre pero abordan un tema distinto.

#### Scenario: Temática repetida marcada para decisión

- **WHEN** una temática candidata coincide con una temática ya procesada o con un artículo publicado de título equivalente
- **THEN** el sistema la marca como pendiente de decisión y no genera el artículo automáticamente

#### Scenario: Usuario decide continuar

- **WHEN** el usuario decide continuar con una temática repetida
- **THEN** el sistema genera el artículo con normalidad

#### Scenario: Usuario decide omitir

- **WHEN** el usuario decide omitir una temática repetida
- **THEN** el sistema no genera el artículo y registra la omisión

#### Scenario: Temática sin repetición

- **WHEN** una temática candidata no coincide con ninguna temática procesada ni con un artículo existente
- **THEN** el sistema la genera directamente sin requerir decisión

### Requirement: Generación de artículos estructurados con IA

El sistema SHALL generar, para cada temática aceptada, un artículo completo (título, resumen, bloques, etiquetas, SEO y categoría) mediante una IA con salida estructurada en el contrato de bloques del portal, respetando la plantilla, el tono y la estructura configurados. Si no cumple los estándares tras los intentos, MUST dejarlo pendiente de confirmación.

#### Scenario: Generación válida

- **WHEN** la IA responde con una estructura de artículo válida
- **THEN** el sistema obtiene un borrador de artículo con bloques y metadatos listos para validar

#### Scenario: Idioma de salida

- **WHEN** la configuración define un idioma de salida
- **THEN** el artículo generado (título, resumen, bloques y etiquetas) se redacta íntegramente en ese idioma, sin mezclar otros

#### Scenario: Contenido original y sin duplicados

- **WHEN** la IA genera un artículo
- **THEN** el prompt exige contenido 100% original (sin copiar ni parafrasear literalmente otras fuentes) y la deduplicación evita repetir temáticas ya procesadas

#### Scenario: Generación inválida o incompleta

- **WHEN** la IA responde con una estructura inválida o incompleta
- **THEN** el sistema la valida antes de persistirla y, si no cumple, solicita una corrección indicando los errores detectados

#### Scenario: Reintento correctivo

- **WHEN** el artículo generado no cumple la extensión, el rango de bloques, la jerarquía de encabezados o las secciones requeridas
- **THEN** el sistema reintenta la generación hasta el número de intentos configurado, enviando los errores como indicaciones de corrección

#### Scenario: Confirmación de artículo fuera de estándar

- **WHEN** el artículo sigue sin cumplir los estándares después de los intentos configurados
- **THEN** el sistema lo deja pendiente de confirmación, muestra los incumplimientos y permite guardarlo de todos modos o descartarlo

### Requirement: Configuración avanzada de generación

El sistema SHALL permitir configurar y persistir la plantilla, el tono, el mínimo de bloques (sin máximo), las secciones requeridas, la temperatura, los intentos y la tolerancia de palabras. La configuración avanzada MUST mostrarse al activar un control de alternancia. Los estándares automáticos por longitud (tokens, mínimo de bloques y tolerancia) MUST aplicarse salvo que el usuario los desactive.

Las plantillas disponibles son "news" (Noticia), "informative" (Informativa), "comparative" (Comparativo), "analysis" (Análisis), "guide" (Guía práctica), "tutorial" (Tutorial paso a paso), "faq" (Preguntas frecuentes), "ppa" (People Also Ask) y "job" (Convocatoria laboral). Los tonos disponibles son "informativo", "analitico", "cronica", "divulgativo" y "amigable" (cercano, amable y conversacional). Las secciones opcionales activables de forma independiente son: lista, tabla, cita, conclusión, preguntas frecuentes, llamada a la acción y enlaces de referencia (internos y externos).

#### Scenario: Ajuste de estructura y tono

- **WHEN** un administrador cambia la plantilla, el tono o las secciones requeridas
- **THEN** la siguiente generación aplica esos cambios al prompt y a la validación

#### Scenario: Estándares automáticos según la longitud

- **WHEN** un administrador selecciona una longitud de artículo
- **THEN** el sistema ajusta automáticamente el presupuesto de tokens, el rango de bloques y la tolerancia de palabras recomendados para esa longitud

#### Scenario: Configuración avanzada bajo demanda

- **WHEN** un administrador activa el control de configuración avanzada
- **THEN** se muestran los controles de plantilla, tono, bloques, temperatura, intentos y tolerancia

#### Scenario: Validación de extensión y bloques

- **WHEN** un administrador define la longitud del artículo y su tolerancia
- **THEN** el sistema acepta únicamente artículos dentro del margen y del rango de bloques configurados, salvo que el usuario confirme guardarlos fuera de estándar

#### Scenario: Secciones opcionales de preguntas frecuentes, llamada a la acción y enlaces de referencia

- **WHEN** un administrador activa la sección de preguntas frecuentes, la llamada a la acción o los enlaces de referencia
- **THEN** el sistema exige cada sección activa en el prompt de generación y la valida en el artículo resultante, reintentando o dejándolo pendiente de confirmación si falta

#### Scenario: Tono amigable conversacional

- **WHEN** un administrador selecciona el tono "amigable"
- **THEN** el sistema instruye a la IA con un estilo cercano, amable y conversacional

### Requirement: Sanitización y validación del contenido generado

El contenido generado por la IA MUST pasar por el sanitizador y el validador del portal antes de persistirse, y el contenido que no cumpla las reglas MUST quedar pendiente de confirmación o rechazarse según la configuración.

#### Scenario: Contenido con HTML no permitido

- **WHEN** el contenido generado contiene HTML no permitido
- **THEN** el sistema lo sanitiza antes de persistirlo

#### Scenario: Contenido fuera de límites

- **WHEN** el contenido no cumple los límites de bloques o de longitud establecidos
- **THEN** el sistema lo deja pendiente de confirmación con los incumplimientos detectados para que el usuario decida

### Requirement: Publicación con control de estado

El sistema SHALL publicar, programar o dejar en borrador cada artículo generado según la configuración de auto-publicación y de programación, y MUST reutilizar el flujo de publicación existente con slug único, campos derivados y validación de invariantes.

#### Scenario: Auto-publicación habilitada

- **WHEN** la auto-publicación está habilitada, la programación está deshabilitada y el artículo generado es válido
- **THEN** el sistema lo publica y el artículo queda visible en el portal

#### Scenario: Auto-publicación deshabilitada

- **WHEN** la auto-publicación está deshabilitada y la programación está deshabilitada
- **THEN** el sistema guarda el artículo en borrador para revisión administrativa

#### Scenario: Programación habilitada

- **WHEN** la programación de publicación está habilitada
- **THEN** el sistema guarda el artículo con estado programado y una fecha de publicación, sin publicarlo de inmediato

### Requirement: Resiliencia del worker ante fallos externos

Un fallo en Serper, la IA o la base de datos MUST NOT detener la aplicación web. La automatización SHALL aplicar timeouts, reintentos y circuit breaker, y MUST registrar el estado de cada corrida para reintentarla en la siguiente.

#### Scenario: Fallo externo durante una corrida

- **WHEN** un servicio externo falla durante una corrida
- **THEN** la aplicación web sigue disponible y la corrida se marca como fallida

#### Scenario: Reintento sin duplicados

- **WHEN** una corrida falla y se ejecuta una siguiente
- **THEN** el sistema reintenta las temáticas pendientes sin duplicar los artículos ya publicados

### Requirement: Control y observabilidad administrativa

Un administrador autenticado SHALL poder ejecutar una corrida manual, pausar o reanudar la automatización, consultar el historial de corridas y revisar los borradores generados desde el panel administrativo.

#### Scenario: Ejecución manual de una corrida

- **WHEN** un administrador solicita ejecutar una corrida manual
- **THEN** el sistema lanza el pipeline de inmediato y registra la corrida, aunque la automatización esté deshabilitada

#### Scenario: Detener una corrida en curso

- **WHEN** un administrador pulsa "Detener" mientras hay una corrida en curso
- **THEN** el sistema cancela la generación, marca la corrida como "Detenido" y el botón de ejecutar vuelve a estar disponible

#### Scenario: Pausa de la automatización

- **WHEN** un administrador pausa la automatización
- **THEN** el worker deja de ejecutar corridas programadas hasta que se reanude

#### Scenario: Revisión de un borrador generado

- **WHEN** un administrador revisa un borrador generado
- **THEN** puede publicarlo, editarlo o descartarlo mediante el flujo administrativo existente

#### Scenario: Decisión sobre temática repetida

- **WHEN** un administrador revisa una temática marcada como pendiente de decisión
- **THEN** puede continuar con la creación del artículo u omitirla desde el panel

### Requirement: Configuración del pipeline de automatización

El sistema SHALL permitir configurar la frecuencia, las categorías habilitadas, los temas base, el país/idioma, las claves y el proveedor de IA, la programación y los límites de artículos por corrida, y MUST persistir esa configuración para las siguientes ejecuciones.

#### Scenario: Configuración válida guardada

- **WHEN** un administrador guarda una configuración válida
- **THEN** el sistema la aplica en la siguiente corrida

#### Scenario: Configuración sin categorías ni temas base

- **WHEN** la configuración no incluye categorías habilitadas o temas base
- **THEN** el sistema no genera artículos y lo indica en el estado de la automatización

### Requirement: Configuración de claves API y proveedor de IA desde el panel

El sistema SHALL permitir configurar desde el panel las claves de Serper y de la IA, el proveedor de IA, la URL base y el modelo, y MUST persistirlas y mostrarlas enmascaradas. La selección de proveedor SHALL ofrecer presets compatibles con el formato OpenAI y una opción personalizada. El país y el idioma MUST presentarse como menú desplegable de opciones válidas.

#### Scenario: Guardado de claves desde el panel

- **WHEN** un administrador guarda las claves de Serper e IA desde el panel
- **THEN** el sistema las persiste y las utiliza en la siguiente corrida sin exponerlas en pantalla

#### Scenario: Selección de proveedor

- **WHEN** un administrador selecciona un proveedor de IA del desplegable
- **THEN** el sistema aplica la URL base y el modelo por defecto de ese proveedor, editables

#### Scenario: País e idioma desde desplegable

- **WHEN** un administrador configura el país o el idioma
- **THEN** el sistema lo presenta como menú desplegable de opciones válidas

### Requirement: Programación de publicación de artículos

El sistema SHALL permitir programar la publicación de artículos con una fecha y hora futuras, y MUST publicarlos automáticamente al alcanzarse ese momento. Un artículo programado se crea con estado programado y una fecha objetivo; el worker lo publica al vencerla. La configuración MAY definir un horario por defecto.

#### Scenario: Artículo programado a una fecha futura

- **WHEN** un artículo se crea con una fecha de publicación futura
- **THEN** el sistema lo guarda con estado programado y no lo muestra en el portal hasta que venza la fecha

#### Scenario: Publicación automática al vencer la fecha

- **WHEN** un artículo programado alcanza su fecha de publicación
- **THEN** el worker lo marca como publicado con su fecha de publicación

#### Scenario: Horario por defecto

- **WHEN** la configuración define un horario por defecto de publicación
- **THEN** los artículos generados se programan para la siguiente ocurrencia de ese horario

### Requirement: Gestión de categorías y subcategorías desde el panel

El sistema SHALL permitir crear categorías y subcategorías desde el propio panel de configuración de la automatización, reutilizando la funcionalidad de categorías existente, sin necesidad de salir de la vista.

#### Scenario: Crear categoría desde el panel de configuración

- **WHEN** un administrador crea una categoría o subcategoría desde el panel de configuración
- **THEN** la categoría queda disponible para seleccionarla en las categorías habilitadas

#### Scenario: Categoría creada reutilizando la lógica existente

- **WHEN** se crea una categoría desde el panel de configuración
- **THEN** el sistema valida y persiste la categoría con las mismas reglas que el módulo de categorías
