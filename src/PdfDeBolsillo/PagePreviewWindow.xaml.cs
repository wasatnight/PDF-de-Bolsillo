using System.Windows;
using System.Windows.Input;
using PdfDeBolsillo.Models;
using PdfDeBolsillo.Services;
namespace PdfDeBolsillo;
public partial class PagePreviewWindow : Window
{
 public PagePreviewWindow(PageItem page)
 {
  InitializeComponent(); DataContext=page; Language=System.Windows.Markup.XmlLanguage.GetLanguage(Localizer.Current.LanguageTag);
  MaxHeight=SystemParameters.WorkArea.Height;
  MaxWidth=SystemParameters.WorkArea.Width;
 }
 void CloseClick(object sender,RoutedEventArgs e)=>Close();
 void WindowKeyDown(object sender,KeyEventArgs e) {if(e.Key==Key.Escape){Close();e.Handled=true;}}
}

