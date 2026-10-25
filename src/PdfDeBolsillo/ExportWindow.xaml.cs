using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using PdfDeBolsillo.Models;
using PdfDeBolsillo.Services;
namespace PdfDeBolsillo;
public sealed record ExportChoice(int Mode,List<List<int>> Groups,string Prefix,ImageExportOptions Images);
public partial class ExportWindow : Window
{
 readonly IReadOnlyList<PageItem> pages;
 public ExportChoice? Choice {get;private set;}
 public ExportWindow(IReadOnlyList<PageItem> pages,IEnumerable<PageItem> selection,int mode)
 {
  this.pages=pages;InitializeComponent();Mode.SelectedIndex=mode;
  Prefix.Text=Localizer.Current.IsEnglish?"document":"documento";
  Ranges.Text=string.Join(", ",selection.Select(p=>pages.ToList().IndexOf(p)+1).Order());
  MaxHeight=SystemParameters.WorkArea.Height;ModeChanged(this,null!);
 }
 void ModeChanged(object s,SelectionChangedEventArgs e)
 {
  if(Ranges is null || Prefix is null)return;
  Ranges.IsEnabled=Mode.SelectedIndex is 1 or 3;Prefix.IsEnabled=Mode.SelectedIndex>=2;
 }
 void ContinueClick(object sender,RoutedEventArgs e)
 {
  try{
   List<List<int>> groups=Mode.SelectedIndex switch {
    0=>[Enumerable.Range(0,pages.Count).ToList()],
    1=>[PageRanges.Parse(Ranges.Text,pages.Count)],
    2=>Enumerable.Range(0,pages.Count).Select(i=>new List<int>{i}).ToList(),
    _=>PageRanges.Split(Ranges.Text,pages.Count)};
   string prefix=Prefix.Text.Trim();
   if(Mode.SelectedIndex>=2 && (string.IsNullOrEmpty(prefix) || prefix.IndexOfAny(Path.GetInvalidFileNameChars())>=0 || prefix.EndsWith('.') ||
       prefix is "." or ".."))throw new DocumentException("Introduce un prefijo de archivo válido.");
   if(!double.TryParse(MarginInput.Text.Replace(',','.'),NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out double margin) || margin<0 || margin>70)
    throw new DocumentException("El margen debe estar entre 0 y 70 mm.");
   Choice=new(Mode.SelectedIndex,groups,prefix,new(SizeMode.SelectedIndex==0,Landscape.IsChecked==true,margin));
   DialogResult=true;
  }catch(DocumentException error){ErrorText.Text=Localizer.Current.Translate(error.Message);}
 }
}


