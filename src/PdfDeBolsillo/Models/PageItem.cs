using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows;
using PdfDeBolsillo.Services;
namespace PdfDeBolsillo.Models;
public class PageItem : INotifyPropertyChanged
{
 public PageItem() {PropertyChangedEventManager.AddHandler(Localizer.Current,(_,_)=> {Changed(nameof(OriginalLabel));Changed(nameof(RotationLabel));Changed(nameof(AccessibleName));Changed(nameof(PreviewPlaceholder));Changed(nameof(SourceKindLabel));},string.Empty);}
 public string OriginalLabel => Localizer.Current.Format("Original · página {0}",SourcePage);
 public string RotationLabel => Localizer.Current.Format("Rotación · {0}°",Rotation);
 public string SourceKindLabel=>Document is null?Localizer.Current.Translate("Documento de muestra"):Document.IsImage?Localizer.Current.Translate("Imagen"):"PDF";
 public string PreviewPlaceholder=>ThumbnailError is null?(Document?.IsImage==true?"IMG":"PDF"):Localizer.Current.Translate(ThumbnailError);
 public string AccessibleName => $"{Source} · {OriginalLabel}";
 public int Id { get; init; }
 public string Source { get; init; } = "";
 public int SourcePage { get; init; }
 public string Title { get; init; } = "";
 public DocumentSource? Document { get; init; }
 public double WidthPoints { get; init; }=595.28;
 public double HeightPoints { get; init; }=841.89;
 ImageSource? thumbnail;
 public ImageSource? Thumbnail {get=>thumbnail;set{thumbnail=value;Changed();}}
 string? thumbnailError;
 public string? ThumbnailError {get=>thumbnailError;set{thumbnailError=value;Changed();Changed(nameof(PreviewPlaceholder));}}
 int rotation, number;
 public int Rotation { get => rotation; set { rotation = value; Changed(); Changed(nameof(RotationLabel)); } }
 public int Number { get => number; set { number = value; Changed(); } }
 public PageItem Copy() => new() { Id=Id, Source=Source, SourcePage=SourcePage, Title=Title, Thumbnail=Document is null?Thumbnail:null, Document=Document,WidthPoints=WidthPoints,HeightPoints=HeightPoints, Rotation=Rotation, Number=Number };
 public event PropertyChangedEventHandler? PropertyChanged;
 void Changed([CallerMemberName] string? p=null) => PropertyChanged?.Invoke(this,new(p));
}






