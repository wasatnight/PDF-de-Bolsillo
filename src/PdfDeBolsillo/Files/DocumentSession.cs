using System.IO;
using System.Security.Cryptography;
using PdfSharp.Pdf.IO;
using PdfDeBolsillo.Models;
namespace PdfDeBolsillo.Files;

/// <summary>Copias privadas de sesión: nunca se escribe en la ruta original.</summary>
public sealed class DocumentSession : IDisposable
{
 readonly string root=Path.Combine(Path.GetTempPath(),"PdfDeBolsillo",Guid.NewGuid().ToString("N"));
 readonly List<DocumentSource> documents=[];
 readonly List<DocumentSource> originalLedger=[];
 public IReadOnlyList<DocumentSource> ProtectedOriginals=>originalLedger;
 static int nextId=100;
 public IReadOnlyList<DocumentSource> Documents=>documents;
 public async Task<List<PageItem>> ImportAsync(string path,CancellationToken token)
 {
  string extension=Path.GetExtension(path).ToLowerInvariant();
  if(extension is not (".pdf" or ".jpg" or ".jpeg" or ".png"))throw new DocumentException("Formato no compatible. Usa PDF, JPG o PNG.");
  Directory.CreateDirectory(root);
  string snapshot=Path.Combine(root,Guid.NewGuid().ToString("N")+extension);
  try
  {
   using(var input=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read,81920,true))
   {
    if(input.Length>256L*1024*1024)throw new DocumentException("El archivo supera el límite de 256 MB.");
    using var output=new FileStream(snapshot,FileMode.CreateNew,FileAccess.Write,FileShare.None,81920,true);
    await input.CopyToAsync(output,token);
   }
   string hash; using(var hashing=File.OpenRead(snapshot))hash=Convert.ToHexString(await SHA256.HashDataAsync(hashing,token));
   // El hash se calcula con un stream de vida acotada.
   bool image=extension!=".pdf";double[] widths,heights;int[] rotations=[0];bool special=false;
   if(image)
   {
    var info=Services.ImageLoader.Info(snapshot);
    widths=[info.Width*72/info.DpiX];heights=[info.Height*72/info.DpiY];
   }
   else
   {
    using(var nativeStream=File.OpenRead(snapshot).AsRandomAccessStream()){
     var native=await Windows.Data.Pdf.PdfDocument.LoadFromStreamAsync(nativeStream).AsTask(token);
     if(native.IsPasswordProtected)throw new DocumentException("El PDF está cifrado o restringido. Esta versión no abre documentos protegidos.");
    }
    using var pdf=PdfReader.Open(snapshot,PdfDocumentOpenMode.Import);
    if(Services.PdfProtection.IsProtected(pdf))throw new DocumentException("El PDF está cifrado o restringido. Esta versión no abre documentos protegidos.");
    if(pdf.PageCount==0 || pdf.PageCount>2000)throw new DocumentException("El PDF debe tener entre 1 y 2000 páginas.");
    widths=pdf.Pages.Cast<PdfSharp.Pdf.PdfPage>().Select(p=>p.Width.Point).ToArray();heights=pdf.Pages.Cast<PdfSharp.Pdf.PdfPage>().Select(p=>p.Height.Point).ToArray();
    rotations=pdf.Pages.Cast<PdfSharp.Pdf.PdfPage>().Select(p=>p.Rotate).ToArray();
    special=pdf.Internals.Catalog.Elements.ContainsKey("/AcroForm") || pdf.Internals.Catalog.Elements.ContainsKey("/Outlines") ||
      pdf.Pages.Cast<PdfSharp.Pdf.PdfPage>().Any(p=>p.Elements.ContainsKey("/Annots"));
    // Firmas pueden residir en campos: se avisa antes de toda exportación PDF.
   }
   token.ThrowIfCancellationRequested();
   var doc=new DocumentSource(Guid.NewGuid(),Path.GetFullPath(path),snapshot,image,widths.Length,hash,widths,heights,special);
   documents.Add(doc);
   if(!originalLedger.Any(d=>string.Equals(d.OriginalPath,doc.OriginalPath,StringComparison.OrdinalIgnoreCase)))originalLedger.Add(doc with {Widths=[],Heights=[]});
   return Enumerable.Range(0,widths.Length).Select(i=>new PageItem{
    Id=Interlocked.Increment(ref nextId),Document=doc,Source=Path.GetFileName(path),SourcePage=i+1,Title=Path.GetFileName(path),
    WidthPoints=rotations[i]%180==0?widths[i]:heights[i],HeightPoints=rotations[i]%180==0?heights[i]:widths[i]
   }).ToList();
  }
  catch(OperationCanceledException){TryDelete(snapshot);throw;}
  catch(DocumentException){TryDelete(snapshot);throw;}
  catch(PdfReaderException){TryDelete(snapshot);throw new DocumentException("No se pudo abrir el PDF. Puede estar dañado, protegido o usar un formato no compatible.");}
  catch(Exception ex) when(ex is ArgumentException or InvalidOperationException or NotSupportedException or FormatException or CryptographicException or System.Runtime.InteropServices.COMException){TryDelete(snapshot);throw new DocumentException("No se pudo abrir el PDF. Puede estar dañado, protegido o usar un formato no compatible.");}
  catch{TryDelete(snapshot);throw;}
 }
 public void Prune(IEnumerable<Guid> live)
 {
  var ids=live.ToHashSet();
  foreach(var doc in documents.Where(d=>!ids.Contains(d.Id)).ToList()){TryDelete(doc.SnapshotPath);documents.Remove(doc);}
 }
 static void TryDelete(string p){try{File.Delete(p);}catch(IOException){}catch(UnauthorizedAccessException){}}
 public void Dispose()
 {
  foreach(var doc in documents)TryDelete(doc.SnapshotPath);
  documents.Clear();
  try{if(Directory.Exists(root))Directory.Delete(root,true);}catch(IOException){}catch(UnauthorizedAccessException){}
 }
}







