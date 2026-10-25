using System.IO;
using System.Security.Cryptography;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfDeBolsillo.Models;
using PdfDeBolsillo.Files;
namespace PdfDeBolsillo.Services;

public sealed class PdfExportService
{
 sealed class Stage(string destination,string temp,string? fingerprint) {
  public string Destination=destination,Temp=temp,Backup=temp+".backup";
  public string? Fingerprint=fingerprint;
  public bool Committed,BackedUp;
 }
 static string Fingerprint(string path)
 {
  using var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);
  return Convert.ToHexString(SHA256.HashData(stream));
 }
 public async Task ExportAsync(IReadOnlyList<ExportJob> jobs,ImageExportOptions options,IEnumerable<DocumentSource> originals,
   bool replaceApproved,IProgress<double>? progress,CancellationToken token)
 {
  if(jobs.Count==0 || jobs.Any(j=>j.Pages.Count==0))throw new DocumentException("No se puede exportar un documento sin páginas.");
  if(options.MarginMm<0 || options.MarginMm>70)throw new DocumentException("El margen debe estar entre 0 y 70 mm.");
  if(jobs.Any(j=>j.Pages.Any(p=>p.Document is null)))throw new DocumentException("Abre documentos reales antes de exportar.");
  var sources=originals.ToArray();PathSafety.Validate(jobs.Select(j=>j.Destination),sources);
  await Task.Run(()=>{
   var stages=new List<Stage>();
   try{
    foreach(var job in jobs){
     string dest=Path.GetFullPath(job.Destination);
     bool exists=File.Exists(dest);
     if(exists && !replaceApproved)throw new DocumentException("El archivo de salida ya existe. Confirma el reemplazo o elige otro nombre.");
     string temp=Path.Combine(Path.GetDirectoryName(dest)!,".pdfbolsillo-"+Guid.NewGuid().ToString("N")+".tmp");
     stages.Add(new(dest,temp,exists?Fingerprint(dest):null));
    }
    int total=jobs.Sum(j=>j.Pages.Count),done=0;
    for(int i=0;i<jobs.Count;i++){
     token.ThrowIfCancellationRequested();
     Build(jobs[i].Pages,options,stages[i].Temp,()=>{token.ThrowIfCancellationRequested();progress?.Report(++done*90d/total);},token);
    }
    token.ThrowIfCancellationRequested();
    // Las comprobaciones se repiten después de construir, justo antes de publicar.
    PathSafety.Validate(stages.Select(s=>s.Destination),sources);
    foreach(var stage in stages){
     bool exists=File.Exists(stage.Destination);
     if(stage.Fingerprint is null && exists || stage.Fingerprint is not null && (!exists || Fingerprint(stage.Destination)!=stage.Fingerprint))
      throw new DocumentException("La salida cambió durante la operación. Elige otro nombre e inténtalo de nuevo.");
    }
    // Fase corta de publicación; no se cancela a mitad de un lote.
    foreach(var stage in stages){
     if(stage.Fingerprint is not null){File.Replace(stage.Temp,stage.Destination,stage.Backup);stage.BackedUp=true;}
     else File.Move(stage.Temp,stage.Destination,false);
     stage.Committed=true;
    }
    progress?.Report(100);
   }
   catch{
    // Si un destino falla, se revierten los resultados ya publicados.
    var rollbackFailed=false;
    foreach(var stage in stages.AsEnumerable().Reverse()){
     try{
      if(stage.BackedUp)File.Move(stage.Backup,stage.Destination,true);
      else if(stage.Committed)File.Delete(stage.Destination);
     }catch(IOException){rollbackFailed=true;}catch(UnauthorizedAccessException){rollbackFailed=true;}
    }
    if(rollbackFailed)throw new DocumentException("La exportación falló y no se pudieron revertir todas las salidas. Revisa la carpeta; se conservaron las copias de respaldo.");
    throw;
   }
   finally{
    foreach(var stage in stages)TryDelete(stage.Temp);
   }
   foreach(var stage in stages)TryDelete(stage.Backup);
  },token);
 }
 static void TryDelete(string path){try{File.Delete(path);}catch(IOException){}catch(UnauthorizedAccessException){}}
 static void Build(IReadOnlyList<PageItem> pages,ImageExportOptions options,string temp,Action advance,CancellationToken token)
 {
  using var output=new PdfDocument();
  var opened=new Dictionary<Guid,PdfDocument>();
  try{
   foreach(var item in pages){
    token.ThrowIfCancellationRequested();
    var source=item.Document!;
    if(!source.IsImage){
     if(!opened.TryGetValue(source.Id,out var input)){
      input=PdfReader.Open(source.SnapshotPath,PdfDocumentOpenMode.Import);
      if(PdfProtection.IsProtected(input))throw new DocumentException("El PDF está cifrado o restringido. Esta versión no abre documentos protegidos.");
      opened[source.Id]=input;
     }
     var page=output.AddPage(input.Pages[item.SourcePage-1]);
     page.Rotate=(page.Rotate+item.Rotation)%360;
     // No se dejan widgets huérfanos: esta herramienta no exporta campos ni firmas interactivas.
     if(page.Elements.ContainsKey("/Annots"))
      for(int a=page.Annotations.Count-1;a>=0;a--)
       if(page.Annotations[a].Elements.GetName("/Subtype")=="/Widget")page.Annotations.Remove(page.Annotations[a]);
    }else{
     var page=output.AddPage();double margin=options.MarginMm*72/25.4;
     double naturalW=source.Widths[0],naturalH=source.Heights[0];
     double width=options.A4?(options.Landscape?841.8898:595.2756):naturalW+margin*2;
     double height=options.A4?(options.Landscape?595.2756:841.8898):naturalH+margin*2;
     page.Width=XUnit.FromPoint(width);page.Height=XUnit.FromPoint(height);page.Rotate=item.Rotation;
     using var imageStream=new MemoryStream(ImageLoader.Png(source.SnapshotPath));
     using var image=XImage.FromStream(imageStream);
     using var graphics=XGraphics.FromPdfPage(page);
     graphics.DrawRectangle(XBrushes.White,0,0,width,height);
     double scale=Math.Min((width-2*margin)/naturalW,(height-2*margin)/naturalH);
     double w=naturalW*scale,h=naturalH*scale;
     graphics.DrawImage(image,(width-w)/2,(height-h)/2,w,h);
    }
    advance();
   }
   token.ThrowIfCancellationRequested();
   using var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None,81920,FileOptions.WriteThrough);
   output.Save(stream,false);stream.Flush(true);
  }finally{foreach(var input in opened.Values)input.Dispose();}
 }
}





