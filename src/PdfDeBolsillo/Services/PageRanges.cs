using System.Globalization;
using PdfDeBolsillo.Models;
namespace PdfDeBolsillo.Services;
public static class PageRanges
{
 public static List<int> Parse(string text,int count)
 {
  if(string.IsNullOrWhiteSpace(text))throw new DocumentException("Introduce páginas o rangos, por ejemplo: 1, 3, 5-8.");
  var result=new List<int>();var used=new HashSet<int>();
  foreach(var part in text.Split(',',StringSplitOptions.TrimEntries)){
   var bounds=part.Split('-',StringSplitOptions.TrimEntries);
   if(bounds.Length>2 || !int.TryParse(bounds[0],NumberStyles.None,CultureInfo.InvariantCulture,out int start))throw Invalid();
   int end=start;
   if(bounds.Length==2 && !int.TryParse(bounds[1],NumberStyles.None,CultureInfo.InvariantCulture,out end))throw Invalid();
   if(start<1 || end<start || end>count)throw Invalid();
   for(int n=start;n<=end;n++){if(!used.Add(n))throw new DocumentException("Hay páginas repetidas o rangos superpuestos.");result.Add(n-1);}
  }
  return result;
 }
 public static List<List<int>> Split(string text,int count)
 {
  var ranges=text.Split(',',StringSplitOptions.TrimEntries);
  var all=Parse(text,count); // valida también repeticiones entre archivos
  return ranges.Select(r=>Parse(r,count)).ToList();
 }
 static DocumentException Invalid()=>new("Rango inválido. Usa números de la composición, dentro del documento y en orden ascendente.");
}
