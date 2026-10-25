using PdfSharp.Pdf;
namespace PdfDeBolsillo.Services;
public static class PdfProtection
{
 // PdfReader restablece los permisos en memoria al terminar la lectura.
 // Se comprueba también el objeto de seguridad que permanece en la tabla de origen.
 public static bool IsProtected(PdfDocument document)
  =>document.Internals.GetAllObjects().Any(o=>o.GetType().Name.Contains("SecurityHandler",StringComparison.Ordinal));
}
