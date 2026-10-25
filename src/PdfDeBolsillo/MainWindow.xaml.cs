using System.IO;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using PdfDeBolsillo.Files;
using PdfDeBolsillo.Models;
using PdfDeBolsillo.Services;
using PdfDeBolsillo.ViewModels;
namespace PdfDeBolsillo;
public partial class MainWindow : Window
{
 readonly WorkspaceViewModel vm=new();
 readonly DocumentSession session=new();
 readonly ThumbnailService thumbnails=new();
 readonly DispatcherTimer scrollTimer=new(){Interval=TimeSpan.FromMilliseconds(160)};
 CancellationTokenSource renderCancel=new(),lifetime=new();
 CancellationTokenSource? operation;
 TaskCompletionSource<bool>? confirmation;
 Point dragStart;PageItem? dragPage;IInputElement? oldFocus;
 bool scanning,rescan;
 public MainWindow()
 {
  InitializeComponent();DataContext=vm;vm.Reset();
  vm.SelectionSync+=()=>{var selected=vm.Selection.ToList();PageList.SelectedItems.Clear();foreach(var p in selected)PageList.SelectedItems.Add(p);};
  vm.CompositionChanged+=()=>{session.Prune(vm.ReferencedDocuments);ScheduleThumbnails();};
  Localizer.Current.PropertyChanged+=(_,_)=>Language=System.Windows.Markup.XmlLanguage.GetLanguage(Localizer.Current.LanguageTag);
  PageList.AddHandler(ScrollViewer.ScrollChangedEvent,new ScrollChangedEventHandler((_,_)=>ScheduleThumbnails()));
  scrollTimer.Tick+=(_,_)=>{scrollTimer.Stop();_ = LoadVisibleAsync();};
  Closing+=OnClosing;
  Loaded+=async (_,_)=>{
   var args=Environment.GetCommandLineArgs();int open=Array.IndexOf(args,"--open");
   if(open>=0 && open+1<args.Length)await ImportAsync(args.Skip(open+1).ToArray());
  };
 }
 void OnClosing(object? sender,CancelEventArgs e)
 {
  if(operation!=null){operation.Cancel();e.Cancel=true;return;}
  scrollTimer.Stop();lifetime.Cancel();renderCancel.Cancel();thumbnails.Clear();session.Dispose();
 }
 void ToggleLanguageClick(object s,RoutedEventArgs e)=>Localizer.Current.Toggle();
 void SelectionChanged(object s,SelectionChangedEventArgs e)=>vm.Select(PageList.SelectedItems.Cast<PageItem>().ToList());
 async void OpenClick(object s,RoutedEventArgs e)
 {
  if(vm.Busy)return;
  var dialog=new OpenFileDialog{Title=T("Abrir documentos"),Filter=T("PDF e imágenes|*.pdf;*.jpg;*.jpeg;*.png"),Multiselect=true};
  if(dialog.ShowDialog(this)==true)await ImportAsync(dialog.FileNames);
 }
 async Task ImportAsync(string[] paths)
 {
  if(vm.Busy || paths.Length==0)return;
  if(paths.Length>100){ShowDialog("Abrir documentos","Abre un máximo de 100 archivos a la vez.");return;}
  StopRendering();
  var imported=new List<PageItem>();
  bool success=await RunAsync("Abriendo documentos…",async (token,progress)=>{
   int done=0;
   foreach(string path in paths){
    token.ThrowIfCancellationRequested();
    var pages=await Task.Run(()=>session.ImportAsync(path,token),token);
    imported.AddRange(pages);
    if(imported.Count+vm.Pages.Count>2000)throw new DocumentException("La composición admite hasta 2000 páginas.");
    progress.Report(++done*100d/paths.Length);
   }
   token.ThrowIfCancellationRequested();vm.Append(imported);
  });
  session.Prune(vm.ReferencedDocuments);
  if(success){thumbnails.Clear();if(vm.Pages.Count>0)PageList.SelectedIndex=0;}
  ScheduleThumbnails();
 }
 async void NewClick(object s,RoutedEventArgs e)
 {
  if(vm.Busy)return;
  if(vm.Pages.Count>0 && !await ConfirmAsync("Nueva composición","Se descartarán la composición y su historial. Los originales no se modificarán."))return;
  StopRendering();vm.Reset();session.Prune(vm.ReferencedDocuments);thumbnails.Clear();
 }
 void HelpClick(object s,RoutedEventArgs e)=>ShowDialog("Ayuda",Localizer.Current["FunctionalHelp"]);
 static string T(string text)=>Localizer.Current.Translate(text);
 void ShowDialog(string title,string body)
 {
  oldFocus=Keyboard.FocusedElement;DialogTitle.Text=T(title);DialogBody.Text=T(body);
  DialogClose.Content=T("Cerrar");DialogConfirm.Visibility=Visibility.Collapsed;DialogOverlay.Visibility=Visibility.Visible;
  KeyboardNavigation.SetTabNavigation(DialogOverlay,KeyboardNavigationMode.Cycle);DialogClose.Focus();
 }
 async Task<bool> ConfirmAsync(string title,string body)
 {
  ShowDialog(title,body);confirmation=new(TaskCreationOptions.RunContinuationsAsynchronously);
  DialogClose.Content=T("Cancelar");DialogConfirm.Visibility=Visibility.Visible;return await confirmation.Task;
 }
 void ConfirmDialog(object s,RoutedEventArgs e){var pending=confirmation;confirmation=null;DialogOverlay.Visibility=Visibility.Collapsed;pending?.TrySetResult(true);oldFocus?.Focus();}
 void CloseDialog(object s,RoutedEventArgs e){var pending=confirmation;confirmation=null;DialogOverlay.Visibility=Visibility.Collapsed;pending?.TrySetResult(false);oldFocus?.Focus();}
 void WindowResized(object s,SizeChangedEventArgs e)
 {
  if(InspectorColumn==null)return;InspectorColumn.Width=new GridLength(ActualWidth<1050?228:268);ScheduleThumbnails();
 }
 void WindowKeyDown(object s,KeyEventArgs e)
 {
  if(vm.Busy){if(e.Key==Key.Escape)operation?.Cancel();return;}
  if(DialogOverlay.Visibility==Visibility.Visible){if(e.Key==Key.Escape)CloseDialog(this,new());if(e.Key!=Key.Tab && e.Key!=Key.Enter && e.Key!=Key.Space)e.Handled=true;return;}
  if(e.Key==Key.Enter && PageList.IsKeyboardFocusWithin && PageList.SelectedItem is PageItem page){_ = OpenPreviewAsync(page);e.Handled=true;return;}
  ICommand? cmd=null;
  if(Keyboard.Modifiers==ModifierKeys.Control)cmd=e.Key switch{Key.Z=>vm.Undo,Key.Y=>vm.Redo,Key.R=>vm.Rotate,_=>null};
  if(Keyboard.Modifiers==ModifierKeys.Alt)cmd=(e.Key==Key.System?e.SystemKey:e.Key) switch{Key.Left=>vm.Previous,Key.Right=>vm.Next,_=>null};
  if(e.Key==Key.Delete)cmd=vm.Remove;
  if(Keyboard.Modifiers==ModifierKeys.Control && e.Key==Key.A){PageList.SelectAll();e.Handled=true;}
  if(cmd!=null){if(cmd.CanExecute(null))cmd.Execute(null);e.Handled=true;}
 }
 static ListBoxItem? Container(DependencyObject? source){while(source!=null && source is not ListBoxItem)source=VisualTreeHelper.GetParent(source);return source as ListBoxItem;}
 void PageMouseDown(object s,MouseButtonEventArgs e)
 {
  dragStart=e.GetPosition(PageList);dragPage=Container(e.OriginalSource as DependencyObject)?.DataContext as PageItem;
  if(e.ClickCount==2 && dragPage is PageItem page){dragPage=null;PageList.SelectedItem=page;e.Handled=true;_ = OpenPreviewAsync(page);}
 }
 async Task OpenPreviewAsync(PageItem page)
 {
  if(vm.Busy)return;ImageSource? image=null;
  bool success=await RunAsync("Generando previsualización…",async(token,progress)=>{image=await thumbnails.RenderAsync(page,1400,token);progress.Report(100);});
  if(success && image!=null){var copy=page.Copy();copy.Thumbnail=image;new PagePreviewWindow(copy){Owner=this}.ShowDialog();}
 }
 void PageMouseMove(object s,MouseEventArgs e)
 {
  if(vm.Busy || e.LeftButton!=MouseButtonState.Pressed || dragPage==null || Keyboard.Modifiers!=ModifierKeys.None)return;
  var p=e.GetPosition(PageList);if(Math.Abs(p.X-dragStart.X)<SystemParameters.MinimumHorizontalDragDistance && Math.Abs(p.Y-dragStart.Y)<SystemParameters.MinimumVerticalDragDistance)return;
  var moving=dragPage;dragPage=null;DragDrop.DoDragDrop(PageList,new DataObject(typeof(PageItem),moving),DragDropEffects.Move);
 }
 void PageDrop(object s,DragEventArgs e)
 {
  if(!vm.Busy && e.Data.GetData(typeof(PageItem)) is PageItem moving && Container(e.OriginalSource as DependencyObject)?.DataContext is PageItem target){vm.DropAt(moving,target);PageList.SelectedItem=moving;e.Handled=true;}
 }
 void WindowDragOver(object s,DragEventArgs e){e.Effects=vm.Busy?DragDropEffects.None:e.Data.GetDataPresent(typeof(PageItem))?DragDropEffects.Move:e.Data.GetDataPresent(DataFormats.FileDrop)?DragDropEffects.Copy:DragDropEffects.None;e.Handled=true;}
 async void WindowDrop(object s,DragEventArgs e){if(!vm.Busy && e.Data.GetData(DataFormats.FileDrop) is string[] paths){e.Handled=true;await ImportAsync(paths);}}
 void StopRendering(){renderCancel.Cancel();renderCancel.Dispose();renderCancel=CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);}
 void ScheduleThumbnails(){if(!IsLoaded || lifetime.IsCancellationRequested)return;scrollTimer.Stop();scrollTimer.Start();}
 void ThumbnailLoaded(object s,RoutedEventArgs e)=>ScheduleThumbnails();
 async Task LoadVisibleAsync()
 {
  if(vm.Busy)return;if(scanning){rescan=true;return;}scanning=true;
  var token=renderCancel.Token;
  try{
   var visible=new List<PageItem>();
   foreach(var page in vm.Pages){
    if(PageList.ItemContainerGenerator.ContainerFromItem(page) is not ListBoxItem box)continue;
    var rect=box.TransformToAncestor(PageList).TransformBounds(new Rect(0,0,box.ActualWidth,box.ActualHeight));
    bool onscreen=rect.Bottom>=-80 && rect.Top<=PageList.ActualHeight+80;
    if(onscreen)visible.Add(page);else if(page.Document!=null)page.Thumbnail=null;
   }
   foreach(var page in visible){
    token.ThrowIfCancellationRequested();
    if(page.Thumbnail!=null || page.ThumbnailError!=null)continue;
    try{page.Thumbnail=await thumbnails.RenderAsync(page,240,token);}
    catch(OperationCanceledException){throw;}
    catch{page.ThumbnailError="No se pudo generar la miniatura.";}
   }
  }catch(OperationCanceledException){}finally{scanning=false;if(rescan){rescan=false;ScheduleThumbnails();}}
 }
 async Task<bool> RunAsync(string title,Func<CancellationToken,IProgress<double>,Task> work)
 {
  if(vm.Busy)return false;vm.Busy=true;WorkspaceRoot.IsEnabled=false;HeaderRoot.IsEnabled=false;
  operation=CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
  BusyTitle.Text=T(title);BusyProgress.Value=0;BusyOverlay.Visibility=Visibility.Visible;
  string? error=null;bool success=false;
  try{
   await work(operation.Token,new Progress<double>(p=>BusyProgress.Value=Math.Clamp(p,0,100)));success=true;
  }catch(OperationCanceledException){error="Operación cancelada. No se completó la salida.";}
  catch(DocumentException ex){error=ex.Message;}
  catch(UnauthorizedAccessException){error="No se pudo leer o escribir. Revisa los permisos y elige una carpeta accesible.";}
  catch(IOException){error="No se pudo leer o escribir. El archivo puede estar en uso o la unidad no estar disponible.";}
  catch{error="No se pudo completar la operación. El documento puede estar dañado o no ser compatible.";}
  finally{operation.Dispose();operation=null;vm.Busy=false;WorkspaceRoot.IsEnabled=true;HeaderRoot.IsEnabled=true;BusyOverlay.Visibility=Visibility.Collapsed;ScheduleThumbnails();}
  if(error!=null)ShowDialog("Resultado",error);return success;
 }
 void CancelOperation(object s,RoutedEventArgs e){operation?.Cancel();BusyTitle.Text=T("Cancelando…");}
 async void ExportClick(object s,RoutedEventArgs e)=>await ExportAsync(0);
 async void ExtractClick(object s,RoutedEventArgs e)=>await ExportAsync(1);
 async void SplitClick(object s,RoutedEventArgs e)=>await ExportAsync(2);
 async Task ExportAsync(int mode)
 {
  if(!vm.CanExport)return;
  var setup=new ExportWindow(vm.Pages.ToList(),vm.Selection.ToList(),mode){Owner=this};
  if(setup.ShowDialog()!=true || setup.Choice is not ExportChoice choice)return;
  var pages=vm.Pages.ToList();var jobs=new List<ExportJob>();
  if(choice.Mode<2){
   var save=new SaveFileDialog{Title=T("Guardar PDF"),Filter="PDF|*.pdf",DefaultExt=".pdf",AddExtension=true,OverwritePrompt=false,FileName=choice.Mode==0?(Localizer.Current.IsEnglish?"composition.pdf":"composicion.pdf"):(Localizer.Current.IsEnglish?"pages.pdf":"paginas.pdf")};
   if(save.ShowDialog(this)!=true)return;
   jobs.Add(new(save.FileName,choice.Groups[0].Select(i=>pages[i]).ToArray()));
  }else{
   var folder=new OpenFolderDialog{Title=T("Elegir carpeta de salida")};
   if(folder.ShowDialog(this)!=true)return;
   for(int i=0;i<choice.Groups.Count;i++)jobs.Add(new(Path.Combine(folder.FolderName,$"{choice.Prefix}_{i+1:000}.pdf"),choice.Groups[i].Select(n=>pages[n]).ToArray()));
  }
  try{PathSafety.Validate(jobs.Select(j=>j.Destination),session.ProtectedOriginals);}
  catch(DocumentException ex){ShowDialog("Exportar PDF",ex.Message);return;}
  catch{ShowDialog("Exportar PDF","No se pudo validar la carpeta de salida.");return;}
  if(jobs.SelectMany(j=>j.Pages).Any(p=>p.Document is {IsImage:false}) &&
    !await ConfirmAsync("Limitaciones del PDF",Localizer.Current["PdfWarning"]))return;
  var existing=jobs.Where(j=>File.Exists(j.Destination)).ToList();
  if(existing.Count>0){
   string body=Localizer.Current.Format("Se reemplazarán {0} archivos. Esta decisión no afecta a los originales.",existing.Count)+"\n\n"+string.Join("\n",existing.Take(8).Select(j=>Path.GetFileName(j.Destination)));
   if(!await ConfirmAsync("Reemplazar archivos",body))return;
  }
  bool success=await RunAsync("Exportando PDF…",(token,progress)=>new PdfExportService().ExportAsync(jobs,choice.Images,session.ProtectedOriginals,existing.Count>0,progress,token));
  if(success)ShowDialog("Exportación completada",Localizer.Current.Format("Archivos PDF guardados: {0}.",jobs.Count)+"\n\n"+Path.GetDirectoryName(jobs[0].Destination));
 }
 public void SavePreview(string path)
 {
  UpdateLayout();var surface=(FrameworkElement)Content;var bmp=new RenderTargetBitmap((int)surface.ActualWidth,(int)surface.ActualHeight,96,96,PixelFormats.Pbgra32);bmp.Render(surface);
  var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bmp));using var stream=File.Create(path);encoder.Save(stream);
 }
}





