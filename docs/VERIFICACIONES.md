# Verificaciones · PDF de Bolsillo 0.2.0
Fecha de entrega: 25 de octubre de 2026.

## Compilación
Release, Windows x64, .NET 10.0.13 autocontenido; SDK 10.0.402.
PDFsharp-WPF 6.2.2 exacta y contratos Windows 10.0.19041.
Auditoría NuGet con transitivas completada sin paquetes vulnerables reportados.

## Comprobaciones automáticas
- 26 de composición/idioma: selección, giro, movimiento, historial, estado vacío y traducción en vivo sin cambiar los documentos.
- 49 funcionales con archivos sintéticos: importación, renderer Windows, caché, unión, extracción, separación, rangos inválidos, dimensiones, EXIF, movimiento y undo/redo reales, salida vacía, protección de originales, duplicados, reemplazo explícito, corrupción, cifrado y restricciones sin contraseña de apertura.
- Cancelación previa y durante composición sin salida parcial.
- Error de escritura con archivo ocupado y conservación de salida anterior.
- Fallo del segundo destino de un lote: reversión del resultado anterior.
- Temporales de exportación y copias de sesión liberados.
- SHA-256 exacto de los diez originales de prueba.

Los resultados detallados están en verificacion-funcional.json. El informe de composición/idioma se genera con --verify.

## Lector independiente y render
pypdf strict=True abrió **16 PDF / 35 páginas**. Se contrastaron por texto los órdenes de unión, extracción, rangos, páginas individuales y reordenamiento. Se verificaron rotaciones 90°/180°, tamaños A4/adaptados, proporciones e imágenes embebidas.
El caso de formulario exportado no conserva campos ni widgets; la app informa esta pérdida antes de exportar.
Resultado: verificacion-independiente.json.

Poppler/pdfinfo/pdftoppm abrió y renderizó ejemplos. Se revisaron la primera página del unido y la imagen con EXIF. El runtime de Poppler emitió advertencias sobre dos fuentes auxiliares ausentes; los textos Helvetica y las imágenes inspeccionadas se renderizaron correctamente.

## Interfaz real
Computer Use verificó:
- Selector nativo y apertura de un PDF con espacios y acentos.
- Tres miniaturas reales, tamaño de página y exportación habilitada.
- Ventana de exportación con modos, rangos y opciones de imágenes.
- Selector de destino, aviso de limitaciones y confirmación.
- Archivo exportado desde la interfaz, luego abierto con pdfinfo: tres páginas.

## Alcance de esta evidencia
No sustituye la prueba manual de Ivan ni concede aprobación funcional. Pendientes: documentos ordinarios del usuario, otra computadora, monitores con escalados distintos y lector de pantalla completo. No se afirma compatibilidad con todos los PDF, preservación de formularios/firmas ni transacción global ante apagado abrupto.
El ejecutable final de app (0.2.0) también pasó tools/Verificar.ps1: 49 funcionales y 26 de composición/idioma. Captura de la entrega local: vista-funcional.png. Aplicación abierta con seis páginas de los ejemplos.
