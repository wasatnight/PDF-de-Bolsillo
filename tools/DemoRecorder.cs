#if DEMO_RECORDING
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using PdfDeBolsillo.Models;
using PdfDeBolsillo.Services;
using PdfSharp.Pdf.IO;
namespace PdfDeBolsillo;

/// <summary>Receta de demostración del producto. Solo se compila con -p:RecordDemo=true.
/// Captura superficies WPF reales; nunca captura el escritorio ni documentos del usuario.</summary>
public partial class MainWindow
{
 public async Task RecordDemoAsync(string outputFolder)
 {
  Directory.CreateDirectory(outputFolder);
  string frames=Path.Combine(outputFolder,"frames");Directory.CreateDirectory(frames);
  var watch=Stopwatch.StartNew();int frame=0;string caption="PDF de Bolsillo · demostración automatizada";
  var chapters=new List<object>();Window? overlay=null;bool capturing=false;
  var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(125)};
  timer.Tick+=(_,_)=>{
   if(capturing)return;capturing=true;
   try{
    var surface=(FrameworkElement)Content;surface.UpdateLayout();
    int w=(int)surface.ActualWidth,h=(int)surface.ActualHeight;
    if(w<1 || h<1)return;
    var shot=new RenderTargetBitmap(w,h,96,96,PixelFormats.Pbgra32);shot.Render(surface);
    var visual=new DrawingVisual();
    using(var draw=visual.RenderOpen()){
     draw.DrawRectangle(new SolidColorBrush(Color.FromRgb(243,240,232)),null,new Rect(0,0,w,h+58));
     draw.DrawImage(shot,new Rect(0,0,w,h));
     if(overlay?.IsVisible==true){
      var content=(FrameworkElement)overlay.Content;content.UpdateLayout();
      int ow=(int)content.ActualWidth,oh=(int)content.ActualHeight;
      var modal=new RenderTargetBitmap(ow,oh,96,96,PixelFormats.Pbgra32);modal.Render(content);
      double x=(w-ow)/2d,y=(h-oh)/2d;
      draw.DrawRectangle(new SolidColorBrush(Color.FromArgb(120,41,43,41)),null,new Rect(0,0,w,h));
      draw.DrawRectangle(new SolidColorBrush(Color.FromRgb(255,252,247)),null,new Rect(x,y,ow,oh));
      draw.DrawImage(modal,new Rect(x,y,ow,oh));
     }
     draw.DrawRectangle(new SolidColorBrush(Color.FromRgb(41,43,41)),null,new Rect(0,h,w,58));
     var text=new FormattedText(caption,CultureInfo.GetCultureInfo("es-MX"),FlowDirection.LeftToRight,new Typeface("Segoe UI"),17,Brushes.White,1);
     draw.DrawText(text,new Point(22,h+17));
    }
    var final=new RenderTargetBitmap(w,h+58,96,96,PixelFormats.Pbgra32);final.Render(visual);
    var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(final));
    using var stream=File.Create(Path.Combine(frames,$"{frame++:00000}.png"));encoder.Save(stream);
   }finally{capturing=false;}
  };
  async Task Step(string title,int milliseconds){
   caption=title;chapters.Add(new{seconds=watch.Elapsed.TotalSeconds,title});await Task.Delay(milliseconds);
  }
  void Select(params int[] indices){PageList.SelectedItems.Clear();foreach(var i in indices)PageList.SelectedItems.Add(vm.Pages[i]);}
  try{
   Width=1200;Height=860;await Task.Delay(400);timer.Start();
   await Step("01 · PDF de Bolsillo: documentos locales, originales intactos",2200);
   var args=Environment.GetCommandLineArgs();int fixturesArg=Array.IndexOf(args,"--demo-fixtures");
   string fixtures=fixturesArg>=0?Path.GetFullPath(args[fixturesArg+1]):Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../ejemplos"));
   await ImportAsync(new[]{Path.Combine(fixtures,"Cuaderno de prueba - acentos áé.pdf"),Path.Combine(fixtures,"Notas de prueba.pdf"),Path.Combine(fixtures,"Retrato.png"),Path.Combine(fixtures,"Paisaje.jpg")});
   if(vm.Pages.Count!=7)throw new InvalidOperationException("Demo import failed");
   await LoadVisibleAsync();await Step("02 · Cargar varios PDF e imágenes en una composición",3200);
   Select(0,1);await Step("03 · Seleccionar varias páginas",2600);
   vm.Rotate.Execute(null);await Step("04 · Girar la selección; los cambios solo afectan a la salida",3000);
   vm.Next.Execute(null);await Step("05 · Reordenar con las flechas de la barra",3000);
   vm.Undo.Execute(null);await LoadVisibleAsync();await Step("06 · Deshacer y conservar el historial",2300);
   var image=vm.Pages[^1];var expanded=image.Copy();expanded.Thumbnail=await thumbnails.RenderAsync(image,1400,CancellationToken.None);
   overlay=new PagePreviewWindow(expanded){Owner=this,Width=730,Height=720};overlay.Show();
   await Step("07 · Previsualización ampliada de una imagen",3300);overlay.Close();overlay=null;
   Localizer.Current.SetEnglish(true);await Step("08 · English / Español: cambio de idioma inmediato",3200);
   Localizer.Current.SetEnglish(false);await Task.Delay(800);
   ShowDialog("Ayuda",Localizer.Current["FunctionalHelp"]);await Step("09 · Ayuda con instrucciones separadas en párrafos",3000);CloseDialog(this,new());
   Select(0,2,5);
   var export=new ExportWindow(vm.Pages.ToList(),vm.Selection.ToList(),1){Owner=this};overlay=export;export.Show();
   await Step("10 · Extraer la selección o escribir páginas y rangos",3500);
   ((ComboBox)export.FindName("Mode")).SelectedIndex=3;
   ((TextBox)export.FindName("Ranges")).Text="1-2, 3-5, 6-7";
   await Step("11 · Separar: un archivo por página o por rango",3500);
   ((ComboBox)export.FindName("Mode")).SelectedIndex=0;
   ((ComboBox)export.FindName("SizeMode")).SelectedIndex=1;
   await Step("12 · Imágenes a PDF: tamaño adaptado, A4 y márgenes",3200);
   export.Close();overlay=null;
   var approved=ConfirmAsync("Limitaciones del PDF",Localizer.Current["PdfWarning"]);
   await Step("13 · Aviso antes de reconstruir documentos PDF",3200);ConfirmDialog(this,new());
   if(!await approved)throw new InvalidOperationException("Demo confirmation failed");
   string results=Path.Combine(outputFolder,"resultados");Directory.CreateDirectory(results);
   var pages=vm.Pages.ToList();
   var jobs=new List<ExportJob>{new(Path.Combine(results,"composicion.pdf"),pages),new(Path.Combine(results,"extraccion.pdf"),new[]{pages[0],pages[2],pages[5]})};
   foreach(var group in PageRanges.Split("1-2,3-5,6-7",pages.Count).Select((g,i)=>(g,i)))
    jobs.Add(new(Path.Combine(results,$"rango-{group.i+1}.pdf"),group.g.Select(n=>pages[n]).ToArray()));
   bool success=await RunAsync("Exportando PDF…",(token,progress)=>new PdfExportService().ExportAsync(jobs,new(),session.ProtectedOriginals,false,progress,token));
   if(!success)throw new InvalidOperationException("Demo export failed");
   bool intact=session.Documents.All(d=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(d.OriginalPath)))==d.Hash);
   if(!intact)throw new InvalidOperationException("Original changed");
   var files=jobs.Select(j=>{using var pdf=PdfReader.Open(j.Destination,PdfDocumentOpenMode.Import);return new{name=Path.GetFileName(j.Destination),pages=pdf.PageCount};}).ToArray();
   if(!files.Select(f=>f.pages).SequenceEqual(new[]{7,3,2,3,2}))throw new InvalidOperationException("Demo page counts differ");
   ShowDialog("Exportación completada",Localizer.Current.Format("Archivos PDF guardados: {0}.",jobs.Count));
   await Step("14 · Cinco PDF exportados; recuentos y originales verificados",3600);CloseDialog(this,new());
   Select(0);await Step("PDF de Bolsillo · unir, separar, extraer e imágenes a PDF · demostración 2×",3500);
   timer.Stop();
   File.WriteAllText(Path.Combine(outputFolder,"resultado.json"),JsonSerializer.Serialize(new{passed=true,originalsIntact=intact,files,frames=frame,fps=8,speed=2,chapters},new JsonSerializerOptions{WriteIndented=true}));
  }finally{timer.Stop();overlay?.Close();}
 }
}
#endif


