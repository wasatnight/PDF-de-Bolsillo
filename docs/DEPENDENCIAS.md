# Dependencias y distribución
Revisión: 25 de octubre de 2026. Versión de aplicación 0.2.0.

## Incluidas
| Componente | Versión | Uso / términos |
| --- | --- | --- |
| PDFsharp-WPF | 6.2.2 exacta | Manipulación y creación de PDF, MIT |
| .NET / WindowsDesktop | 10.0.13 | Runtime autocontenido; MIT y términos de binarios Windows |
| Microsoft.Extensions.DependencyInjection | 8.0.1 | Dependencia transitiva, MIT |
| Microsoft.Extensions.DependencyInjection.Abstractions | 8.0.2 | Dependencia transitiva, MIT |
| Microsoft.Extensions.Logging | 8.0.1 | Dependencia transitiva, MIT; sin proveedor de logs documental configurado |
| Microsoft.Extensions.Logging.Abstractions | 8.0.2 | Dependencia transitiva, MIT |
| Microsoft.Extensions.Options | 8.0.2 | Dependencia transitiva, MIT |
| Microsoft.Extensions.Primitives | 8.0.0 | Dependencia transitiva, MIT |
| Microsoft.Windows.SDK.NET.Ref | 10.0.19041.57 | Contratos/runtime de integración Windows, licencia del Windows SDK |

El SDK de compilación es .NET 10.0.402, fijado por global.json. Las dependencias NuGet explícitas y transitivas están fijadas por packages.lock.json. La referencia implícita al SDK de Windows queda determinada por el SDK y el TargetFramework. Se incluyen únicamente los ensamblados que publica dotnet, no el SDK completo.

## Condiciones relevantes
MIT permite redistribuir gratis o comercialmente sin comprar licencia; exige conservar copyright y texto de permiso/garantía. Se incluyen los textos de PDFsharp y Microsoft.Extensions en app/licencias.

La distribución de .NET en Windows contiene binarios bajo .NET Library License y Windows SDK License además de MIT. Se conservan los avisos y términos Microsoft pertinentes en app/licencias. No se redistribuyen archivos de tipografía ni el motor PDF de Windows; este se usa desde el sistema operativo instalado. No se exige adquirir una licencia comercial de un proveedor PDF para distribuir esta app.

PDFsharp está fijado a **6.2.2**. El tag y el paquete de esa versión son MIT; no sustituirlo por una versión reciente sin revisar su licencia. La documentación actual indica AGPL/comercial desde 6.5.0 y 7.0 Preview 2.

Fuentes:
- https://github.com/empira/PDFsharp/blob/v6.2.2/LICENSE
- https://www.nuget.org/packages/PDFsharp-WPF/6.2.2
- https://docs.pdfsharp.net/General/License/License.html
- https://github.com/dotnet/core/blob/main/license-information-windows.md
- https://dotnet.microsoft.com/dotnet_library_license.htm
- https://learn.microsoft.com/legal/windows-sdk/license
- https://dotnet.microsoft.com/en-us/platform/support/policy

## Manipular y renderizar son tareas separadas
PDFsharp compone la salida y genera páginas desde imágenes. Las miniaturas y la vista ampliada usan **Windows.Data.Pdf**, con streams nativos de vida acotada y bitmaps inmutables.
https://learn.microsoft.com/en-us/uwp/api/windows.data.pdf

Las fotos se decodifican con WIC/WPF; se aplican las ocho transformaciones EXIF previstas por el código. La prueba de orientación usa EXIF 6. No hay PDFium, Ghostscript, iText, OCR ni servicios remotos.

## Auditoría
Se ejecutó dotnet list package --vulnerable --include-transitive contra NuGet.org, sin paquetes vulnerables reportados en la consulta completada. Resultado en auditoria-nuget.json. Esto no garantiza ausencia de fallos no publicados ni compatibilidad con todos los PDF.

## Comportamiento real
Se bloquean PDF protegidos antes de incorporarlos a la composición. Como PdfReader restablece cifrado/permisos en memoria al finalizar su lectura, la comprobación revisa además los objetos de seguridad del documento, no solo los flags posteriores.

La salida copia páginas, no clona la estructura documental completa. Se omiten widgets de formulario/firma para no dejar campos huérfanos; los valores o apariencias pueden perderse. No se garantiza conservar marcadores, otras anotaciones, metadatos ni etiquetas de accesibilidad, y ninguna firma digital se considera preservada como válida. La app avisa antes de exportar.

ReportLab, Pillow, pypdf y Poppler solo se utilizaron para generar y verificar ejemplos en el entorno de desarrollo. No forman parte del runtime distribuido de la aplicación.
