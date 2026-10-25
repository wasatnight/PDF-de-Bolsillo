using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows.Media.Imaging;
using PdfSharp.Pdf.IO;
using PdfDeBolsillo.Models;
using PdfDeBolsillo.Services;
using PdfDeBolsillo.Files;
namespace PdfDeBolsillo;
internal static class FunctionalVerification
{
 sealed class InlineProgress(Action<double> report):IProgress<double>{public void Report(double value)=>report(value);}
 public static async Task RunAsync(string root)
 {
  string input=Path.Combine(root,"ejemplos"),dest=Path.Combine(root,"verificacion-funcional");
  if(Directory.Exists(dest) && Directory.EnumerateFileSystemEntries(dest).Any())throw new InvalidOperationException("La carpeta de verificación debe estar vacía.");
  Directory.CreateDirectory(dest);
  var results=new List<string>();
  void Check(bool value,string label){if(!value)throw new InvalidOperationException(label);results.Add(label);}
  async Task Reject(Func<Task> action,string label){
   bool rejected=false;try{await action();}catch(DocumentException){rejected=true;}Check(rejected,label);
  }
  using var session=new DocumentSession();
  var a=await session.ImportAsync(Path.Combine(input,"Cuaderno de prueba - acentos áé.pdf"),CancellationToken.None);
  var b=await session.ImportAsync(Path.Combine(input,"Notas de prueba.pdf"),CancellationToken.None);
  Check(a.Count==3 && b.Count==2,"Importación PDF, rutas con espacios y acentos");
  Check(session.Documents.Count==2 && File.Exists(a[0].Document!.SnapshotPath),"Copias privadas de sesión");
  var snapshots=session.Documents.Select(d=>d.SnapshotPath).ToArray();
  var thumbs=new ThumbnailService();
  var thumb=await thumbs.RenderAsync(a[0],240,CancellationToken.None);
  Check(thumb.IsFrozen,"Miniatura PDF real mediante renderer Windows");
  var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create((BitmapSource)thumb));
  using(var file=File.Create(Path.Combine(dest,"miniatura-renderer-windows.png")))encoder.Save(file);
  var repeated=await thumbs.RenderAsync(a[0],240,CancellationToken.None);
  Check(ReferenceEquals(thumb,repeated),"Caché de miniaturas reutilizada");
  var exporter=new PdfExportService();
  async Task Export(string filename,IReadOnlyList<PageItem> pages,ImageExportOptions? options=null,bool replace=false)
   =>await exporter.ExportAsync([new(Path.Combine(dest,filename),pages)],options??new(),session.Documents,replace,null,CancellationToken.None);
  var ordered=a.Concat(b).ToList();ordered[1].Rotation=90;
  await Export("01-unido.pdf",ordered);
  using(var pdf=PdfReader.Open(Path.Combine(dest,"01-unido.pdf"),PdfDocumentOpenMode.Import)){
   Check(pdf.PageCount==5,"Unir cinco páginas");
   Check(pdf.Pages[1].Rotate==90,"Rotación adicional conservada al exportar");
   Check(Math.Abs(pdf.Pages[1].Width.Point-841.89)<1,"Tamaño original de página conservado");
  }
  var selection=PageRanges.Parse("1, 3, 5",ordered.Count).Select(i=>ordered[i]).ToArray();
  await Export("02-extraido.pdf",selection);
  Check(PageRanges.Parse("1,3,5",5).SequenceEqual(new[]{0,2,4}),"Extracción por números");
  foreach(string invalid in new[]{"","0","2-1","6","1,1","1-3,3-4","1,,2","1-x","1-2-3","-1","999999999999"})
   await Reject(()=>{PageRanges.Parse(invalid,5);return Task.CompletedTask;},"Rango rechazado: "+invalid);
  var groups=PageRanges.Split("1-2,3-5",5);
  Check(groups.Count==2 && groups[0].Count==2 && groups[1].Count==3,"Separar por rangos");
  var jobs=groups.Select((g,i)=>new ExportJob(Path.Combine(dest,$"03-rango-{i+1}.pdf"),g.Select(n=>ordered[n]).ToArray())).ToArray();
  await exporter.ExportAsync(jobs,new(),session.Documents,false,null,CancellationToken.None);
  var singles=ordered.Select((p,i)=>new ExportJob(Path.Combine(dest,$"04-página-{i+1}.pdf"),[p])).ToArray();
  await exporter.ExportAsync(singles,new(),session.Documents,false,null,CancellationToken.None);
  Check(singles.All(j=>File.Exists(j.Destination)),"Separación individual y nombres Unicode");
  var imagePages=new List<PageItem>();
  foreach(var name in new[]{"Paisaje.jpg","Retrato.png","Orientación EXIF.jpg"})imagePages.AddRange(await session.ImportAsync(Path.Combine(input,name),CancellationToken.None));
  Check(imagePages[2].WidthPoints<imagePages[2].HeightPoints,"Orientación EXIF detectada");
  var decoded=ImageLoader.Read(imagePages[2].Document!.SnapshotPath,240);
  Check(decoded.PixelWidth<decoded.PixelHeight,"Orientación EXIF aplicada a los píxeles");
  await Export("05-imagenes-A4.pdf",imagePages,new(true,false,12));
  await Export("06-imagenes-adaptado.pdf",imagePages,new(false,false,0));
  await Export("07-imagenes-horizontal.pdf",imagePages,new(true,true,8));
  using(var pdf=PdfReader.Open(Path.Combine(dest,"05-imagenes-A4.pdf"),PdfDocumentOpenMode.Import))
   Check(pdf.PageCount==3 && Math.Abs(pdf.Pages[0].Width.Point-595.28)<1 && Math.Abs(pdf.Pages[0].Height.Point-841.89)<1,"Imágenes a PDF A4 con márgenes");
  using(var pdf=PdfReader.Open(Path.Combine(dest,"06-imagenes-adaptado.pdf"),PdfDocumentOpenMode.Import))
   Check(pdf.Pages[0].Width.Point>pdf.Pages[0].Height.Point && pdf.Pages[1].Width.Point<pdf.Pages[1].Height.Point && pdf.Pages[2].Width.Point<pdf.Pages[2].Height.Point,"Tamaño adaptado y proporciones distintas");
  var vm=new ViewModels.WorkspaceViewModel();vm.Reset();vm.Append(ordered);vm.Select([vm.Pages[1]]);
  vm.Next.Execute(null);Check(vm.Pages[2].Id==a[1].Id,"Mover página real");
  vm.Undo.Execute(null);Check(vm.Pages[1].Id==a[1].Id,"Deshacer página real");
  vm.Redo.Execute(null);Check(vm.Pages[2].Id==a[1].Id,"Rehacer página real");
  vm.Select([vm.Pages[2]]);vm.Remove.Execute(null);Check(vm.Pages.Count==4,"Quitar página real de composición");
  await Export("08-reordenado.pdf",vm.Pages.ToList());
  await Reject(()=>exporter.ExportAsync([new(Path.Combine(dest,"vacio.pdf"),[])],new(),session.Documents,false,null,CancellationToken.None),"Selección vacía bloqueada");
  await Reject(()=>exporter.ExportAsync([new(a[0].Document!.OriginalPath,a)],new(),session.Documents,true,null,CancellationToken.None),"Sobrescritura del original bloqueada");
  await Reject(()=>exporter.ExportAsync([new(Path.Combine(dest,"duplicado.pdf"),a),new(Path.Combine(dest,"duplicado.pdf"),b)],new(),session.Documents,false,null,CancellationToken.None),"Salidas duplicadas rechazadas");
  await Reject(()=>Export("01-unido.pdf",b),"Reemplazo requiere decisión explícita");
  await Export("09-reemplazo.pdf",a);
  await Export("09-reemplazo.pdf",b,replace:true);
  using(var pdf=PdfReader.Open(Path.Combine(dest,"09-reemplazo.pdf"),PdfDocumentOpenMode.Import))Check(pdf.PageCount==2,"Reemplazo explícito completado");
  foreach(var bad in new[]{"Dañado.pdf","Protegido.pdf","Restringido.pdf"})
   await Reject(async()=>{await session.ImportAsync(Path.Combine(input,bad),CancellationToken.None);},"Documento rechazado: "+bad);
  var form=await session.ImportAsync(Path.Combine(input,"Formulario.pdf"),CancellationToken.None);
  Check(form[0].Document!.HasSpecialElements,"Formulario detectado para aviso");
  await Export("10-formulario-limitaciones.pdf",form);
  using(var formOutput=PdfReader.Open(Path.Combine(dest,"10-formulario-limitaciones.pdf"),PdfDocumentOpenMode.Import))
   Check(formOutput.Pages[0].Annotations.Count==0,"Exportación omite widgets de formulario sin dejar campos huérfanos");
  var rotated=await session.ImportAsync(Path.Combine(input,"Rotado.pdf"),CancellationToken.None);
  Check(rotated[0].WidthPoints>rotated[0].HeightPoints,"Vista previa respeta la orientación de origen");
  rotated[0].Rotation=90;await Export("11-rotacion-acumulada.pdf",rotated);
  using(var pdf=PdfReader.Open(Path.Combine(dest,"11-rotacion-acumulada.pdf"),PdfDocumentOpenMode.Import))Check(pdf.Pages[0].Rotate==180,"Rotación de origen más rotación de composición");
  using(var canceled=new CancellationTokenSource()){
   canceled.Cancel();bool stopped=false;
   try{await exporter.ExportAsync([new(Path.Combine(dest,"cancelado.pdf"),ordered)],new(),session.Documents,false,null,canceled.Token);}catch(OperationCanceledException){stopped=true;}
   Check(stopped && !File.Exists(Path.Combine(dest,"cancelado.pdf")),"Cancelación previa sin salida");
  }
  using(var canceled=new CancellationTokenSource()){
   bool stopped=false;
   try{await exporter.ExportAsync([new(Path.Combine(dest,"cancelado-medio.pdf"),ordered)],new(),session.Documents,false,new InlineProgress(_=>canceled.Cancel()),canceled.Token);}catch(OperationCanceledException){stopped=true;}
   Check(stopped && !File.Exists(Path.Combine(dest,"cancelado-medio.pdf")),"Cancelación durante composición sin salida parcial");
  }
  string rollback=Path.Combine(dest,"rollback.pdf"),blocked=Path.Combine(dest,"bloqueado.pdf");
  File.WriteAllText(rollback,"contenido anterior");Directory.CreateDirectory(blocked);
  bool failed=false;
  try{await exporter.ExportAsync([new(rollback,a),new(blocked,b)],new(),session.Documents,true,null,CancellationToken.None);}catch(IOException){failed=true;}
  Check(failed && File.ReadAllText(rollback)=="contenido anterior","Fallo de publicación revierte resultados anteriores");
  string locked=Path.Combine(dest,"ocupado.pdf");File.WriteAllText(locked,"sin cambios");
  using(var hold=new FileStream(locked,FileMode.Open,FileAccess.ReadWrite,FileShare.None)){
   bool writeFailed=false;try{await Export("ocupado.pdf",a,replace:true);}catch(IOException){writeFailed=true;}
   Check(writeFailed,"Archivo de salida ocupado produce error de escritura");
  }
  Check(File.ReadAllText(locked)=="sin cambios","Error de escritura conserva salida anterior");
  Check(!Directory.EnumerateFiles(dest,".pdfbolsillo-*").Any(),"Temporales de exportación limpiados");
  var hashes=JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(Path.Combine(input,"hashes-originales.json")))!;
  Check(hashes.All(kv=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(input,kv.Key)))).Equals(kv.Value,StringComparison.OrdinalIgnoreCase)),"Todos los originales conservan SHA-256 exacto");
  session.Dispose();
  Check(snapshots.All(p=>!File.Exists(p)),"Copias de sesión liberadas al cerrar");
  File.WriteAllText(Path.Combine(dest,"informe.json"),JsonSerializer.Serialize(new{estado="correcto",comprobaciones=results},new JsonSerializerOptions{WriteIndented=true}));
 }
}


