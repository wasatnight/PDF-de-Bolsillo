using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PdfDeBolsillo.Models;
namespace PdfDeBolsillo.Services;
public static class ImageLoader
{
 public sealed record ImageInfo(int Width,int Height,double DpiX,double DpiY);
 static int Orientation(BitmapFrame frame)
 {
  if(frame.Metadata is not BitmapMetadata m)return 1;
  foreach(var q in new[]{"/app1/ifd/{ushort=274}","/ifd/{ushort=274}"})
   try{if(m.ContainsQuery(q))return Convert.ToInt32(m.GetQuery(q));}catch(NotSupportedException){}
  return 1;
 }
 public static ImageInfo Info(string path)
 {
  using var stream=File.OpenRead(path);
  var decoder=BitmapDecoder.Create(stream,BitmapCreateOptions.DelayCreation,BitmapCacheOption.None);
  var f=decoder.Frames[0];bool swap=Orientation(f) is >=5 and <=8;
  if((long)f.PixelWidth*f.PixelHeight>40_000_000)throw new DocumentException("La imagen supera el límite de 40 megapíxeles.");
  double dx=f.DpiX>0?f.DpiX:96,dy=f.DpiY>0?f.DpiY:96;
  return swap?new(f.PixelHeight,f.PixelWidth,dy,dx):new(f.PixelWidth,f.PixelHeight,dx,dy);
 }
 public static BitmapSource Read(string path,int decodeWidth=0)
 {
  // WIC decodifica a resolución limitada para miniaturas; no mantiene el archivo abierto.
  int orientation;int rawWidth,rawHeight;
  using(var metadataStream=File.OpenRead(path)){var frame=BitmapDecoder.Create(metadataStream,BitmapCreateOptions.DelayCreation,BitmapCacheOption.None).Frames[0];orientation=Orientation(frame);rawWidth=frame.PixelWidth;rawHeight=frame.PixelHeight;}
  using var input=File.OpenRead(path);
  var bitmap=new BitmapImage();bitmap.BeginInit();bitmap.CacheOption=BitmapCacheOption.OnLoad;bitmap.StreamSource=input;
  if(decodeWidth>0)bitmap.DecodePixelWidth=Math.Max(1,(int)(rawWidth*Math.Min(1,Math.Min((double)decodeWidth/rawWidth,decodeWidth*1.5/rawHeight))));bitmap.EndInit();bitmap.Freeze();
  Matrix matrix=orientation switch {
   2=>new(-1,0,0,1,0,0),3=>new(-1,0,0,-1,0,0),4=>new(1,0,0,-1,0,0),
   5=>new(0,1,1,0,0,0),6=>new(0,1,-1,0,0,0),7=>new(0,-1,-1,0,0,0),8=>new(0,-1,1,0,0,0),_=>Matrix.Identity};
  if(matrix.IsIdentity)return bitmap;
  var transformed=new TransformedBitmap(bitmap,new MatrixTransform(matrix));transformed.Freeze();return transformed;
 }
 public static byte[] Png(string path)
 {
  var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(Read(path)));
  using var stream=new MemoryStream();encoder.Save(stream);return stream.ToArray();
 }
}

