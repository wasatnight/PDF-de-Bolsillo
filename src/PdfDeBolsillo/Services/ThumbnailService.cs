using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.Storage.Streams;
using PdfDeBolsillo.Models;
namespace PdfDeBolsillo.Services;

/// <summary>48 imágenes de 240px como máximo; documentos nativos de vida acotada.</summary>
public sealed class ThumbnailService
{
 readonly Dictionary<(Guid,int),ImageSource> cache=[];
 readonly LinkedList<(Guid,int)> lru=[];
 readonly SemaphoreSlim gate=new(1);
 public async Task<ImageSource> RenderAsync(PageItem page,int width,CancellationToken token)
 {
  if(page.Document is null)return page.Thumbnail ?? throw new DocumentException("La muestra no tiene miniatura.");
  await gate.WaitAsync(token);
  try{
   var key=(page.Document.Id,page.SourcePage);
   if(width<=240 && cache.TryGetValue(key,out var hit)){lru.Remove(key);lru.AddLast(key);return hit;}
   ImageSource image;
   if(page.Document.IsImage)image=await Task.Run(()=>ImageLoader.Read(page.Document.SnapshotPath,width),token);
   else
   {
    using var stream=File.OpenRead(page.Document.SnapshotPath).AsRandomAccessStream();
    var pdf=await Windows.Data.Pdf.PdfDocument.LoadFromStreamAsync(stream).AsTask(token);
    using var nativePage=pdf.GetPage((uint)page.SourcePage-1);
    using var memory=new InMemoryRandomAccessStream();
    double scale=Math.Min(width/nativePage.Size.Width,width*1.5/nativePage.Size.Height);
    var options=new Windows.Data.Pdf.PdfPageRenderOptions{DestinationWidth=(uint)Math.Max(1,nativePage.Size.Width*scale),DestinationHeight=(uint)Math.Max(1,nativePage.Size.Height*scale)};
    await nativePage.RenderToStreamAsync(memory,options).AsTask(token);
    token.ThrowIfCancellationRequested();
    using var input=memory.AsStreamForRead();input.Position=0;
    var bitmap=new BitmapImage();bitmap.BeginInit();bitmap.CacheOption=BitmapCacheOption.OnLoad;bitmap.StreamSource=input;bitmap.EndInit();bitmap.Freeze();image=bitmap;
   }
   if(width<=240){
    cache[key]=image;lru.AddLast(key);
    while(lru.Count>48){var first=lru.First!.Value;lru.RemoveFirst();cache.Remove(first);}
   }
   return image;
  }finally{gate.Release();}
 }
 public void Clear(){cache.Clear();lru.Clear();}
}

