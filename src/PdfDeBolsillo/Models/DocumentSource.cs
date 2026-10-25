namespace PdfDeBolsillo.Models;
public sealed record DocumentSource(Guid Id,string OriginalPath,string SnapshotPath,bool IsImage,int PageCount,string Hash,
 double[] Widths,double[] Heights,bool HasSpecialElements);
public sealed record ImageExportOptions(bool A4=true,bool Landscape=false,double MarginMm=12);
public sealed record ExportJob(string Destination,IReadOnlyList<PageItem> Pages);
public sealed class DocumentException(string message) : Exception(message);
