# Contexto del proyecto · PDF de Bolsillo
Actualizado: 25 de octubre de 2026. Producto de Ivan Solano. Versión 0.2.1.

## Objetivo y alcance
App gratuita, local y bilingüe para Windows, C#/.NET/WPF: unir, separar, extraer, reordenar, girar/quitar páginas y crear PDF desde JPG/PNG.
Fuera de alcance: edición de texto, OCR, firmas digitales y compresión avanzada.

## Aprobación
VISUAL: Ivan probó la app real, pidió simplificaciones, aprobó el diseño 0.1.1 y verificó el selector English/Español 0.1.2.
FUNCIONAL: Ivan considera excelente la versión funcional; solicitó únicamente separación de párrafos, implementada en español e inglés en 0.2.1.
PUBLICACIÓN: autorizada expresamente por Ivan. Él creó el repositorio público wasatnight/PDF-de-Bolsillo con README inicial. Se prepara la subida mediante el conector, sin depender de Opera.

## Entorno
Proyecto inicialmente vacío, sin AGENTS.md aplicable ni SDK. SDK local 10.0.402 en work/dotnet, runtime distribuido 10.0.13. Target net10.0-windows10.0.19041.0; probado en este Windows x64. global.json fija SDK y packages.lock.json fija dependencias. Cachés y temporales de compilación aislados en work.
FotoLista y Ordena se revisaron mediante su código disponible para diferenciar esta identidad.

## Diseño aprobado
Papel cálido #F3F0E8, superficies #FFFCF7, grafito y naranja #AD3E20. Segoe UI Variable Text/Display, respaldo Segoe UI. Logo p. e icono propio en ventana/EXE.
Sin franja de originales, pie inferior, badge de versión ni encabezados promocionales. Miniaturas con nombre y Original · página N. Inspector sin encabezado Página 02. Cabecera con English/Español, Ayuda y Abrir. Doble clic abre vista ampliada que respeta el tamaño y giro.
La dirección visual está aprobada; no reintroducir las zonas que Ivan eliminó.

## Implementado
- Abrir múltiples PDF/JPG/PNG y arrastrarlos a la ventana; importación completa por lote antes de añadir a la composición.
- Copias privadas de sesión, protección de rutas originales y limpieza al liberar historial/cerrar.
- Miniaturas de PDF reales mediante Windows.Data.Pdf; imágenes WIC/EXIF; generación visible bajo demanda, cache LRU de 48 imágenes y ancho hasta 240 px. Vista ampliada hasta 1400 px.
- Selección múltiple, movimiento con arrastre/botones/teclado, giros, eliminación de salida, historial de 30 acciones.
- Unir/composición completa, extraer selección o números, separar por página o por grupos de rangos.
- Imágenes a PDF con A4/adaptado, márgenes, orientación de A4, proporciones y EXIF.
- Exportación temporal, destinos Unicode, reemplazos explícitos, comprobación de cambios en destino, reversión de lotes al fallar.
- Progreso real, cancelación y bloqueo de edición durante tareas. Nueva composición con confirmación.
- Interfaz, diálogos y errores en español/inglés; no se traduce el contenido documental.

## Dependencias y licencias
PDFsharp-WPF exactamente 6.2.2, MIT, para composición. Versiones recientes cambian a AGPL/comercial: no actualizar automáticamente.
Windows.Data.Pdf para renderizado; Microsoft.Windows.SDK.NET.Ref 10.0.19041.57 provisto por SDK y runtime/contratos Microsoft.
Microsoft.Extensions transitivas fijadas en lockfile. Avisos en app/licencias; detalle en docs/DEPENDENCIAS.md.
Consulta NuGet de vulnerabilidades completada sin paquetes vulnerables reportados. No equivale a garantía de seguridad para todo archivo PDF.

## Verificaciones
49 pruebas funcionales y 26 de composición/idioma correctas.
Segundo lector pypdf strict=True: 16 PDF, 35 páginas, orden, extracción, separación, tamaños y giros correctos.
Poppler/pdfinfo/pdftoppm abrió y renderizó ejemplos; se inspeccionaron imágenes.
UI real con Computer Use: selector con nombre acentuado, importación de tres páginas, miniaturas, formulario de exportación, aviso y archivo final de tres páginas confirmado con pdfinfo.
Cancelación previa y durante composición, errores de escritura, reversión de lote, duplicados, cifrado, restricciones con contraseña vacía, archivo dañado y SHA-256 de todos los originales comprobados.
Informes en docs. La prueba manual de Ivan no se sustituye.

## Comportamiento especial y límites
Se rechazan PDF protegidos. PdfReader restablece flags de cifrado en memoria; por eso se comprueba además el objeto de seguridad original, sin usar esos flags como prueba única.
Se omiten widgets de formulario/firma al exportar para no dejar campos huérfanos. Valores/apariencias pueden perderse; firmas no válidas, marcadores/anotaciones/metadatos sin garantía. Aviso explícito antes de exportar cualquier composición con PDF.
Límites iniciales: 100 archivos por importación, 2000 páginas, 256 MB por archivo, 40 MP por imagen.
No hay transacción global entre varios archivos ni garantía de limpieza tras apagado/cierre forzado. Los pasos nativos pueden retrasar la cancelación; la fase corta de publicación no se interrumpe.
No se ha validado en otra máquina ni con todos los tipos de PDF, lectores de pantalla o escalados múltiples.
El idioma es de sesión, español por defecto. Las opciones de margen/tamaño de imágenes se aplican al exportar.

## Siguiente paso
Entregar código limpio para VS Code / Visual Studio, ZIP portable y GIF acelerado a 2×. Publicar código y el mismo GIF en el README del repositorio autorizado; después atender observaciones.
Entrega final 0.2.0 publicada solo a la carpeta local app; ProductVersion 0.2.0 y FileVersion 0.2.0.0 confirmadas. El paquete final pasó otra ejecución de las 75 comprobaciones. Se abrió con dos PDF y una imagen sintéticos (seis páginas de composición) para la prueba manual de Ivan.

## Entrega 0.2.1 y demo
Se separaron las instrucciones de Ayuda y el aviso del original en párrafos, en español e inglés.
Demo automatizada de la interfaz real: 256 capturas WPF, GIF de 1040 × 772, 24.04 s a 2×. No contiene capturas de escritorio ni rutas personales.
Se exportaron composición (7 páginas), extracción (3) y tres rangos (2, 3, 2). Se verificaron sus recuentos y SHA-256 de los originales; resultado en docs/demo-verificacion.json.
Se incluyeron solución .sln, workspace de VS Code, configuración F5 y script de compilación portable con copia de licencias.
