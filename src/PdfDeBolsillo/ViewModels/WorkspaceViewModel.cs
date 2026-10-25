using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using PdfDeBolsillo.Models;
using PdfDeBolsillo.Services;
namespace PdfDeBolsillo.ViewModels;
public class ActionCommand(Action action, Func<bool>? can=null) : ICommand
{
 public bool CanExecute(object? p) => can?.Invoke() ?? true;
 public void Execute(object? p) => action();
 public event EventHandler? CanExecuteChanged { add => CommandManager.RequerySuggested+=value; remove => CommandManager.RequerySuggested-=value; }
}
public class WorkspaceViewModel : INotifyPropertyChanged
{
 public ObservableCollection<PageItem> Pages { get; } = [];
 public List<PageItem> Selection { get; } = [];
 readonly Stack<List<PageItem>> undo = [], redo = [];
 bool editing; public event Action? SelectionSync;
 string status = "Ejemplo listo. Selecciona páginas para componer tu documento.";
 public string Status { get=>Localizer.Current.Translate(status); set {status=value; Notify();} }
 public string Summary => Localizer.Current.Format("{0} páginas en la composición",Pages.Count);
 public bool IsEmpty => Pages.Count==0;
 public bool HasSelection => Selection.Count>0;
 public string SelectionTitle => Selection.Count==0 ? Localizer.Current.Translate("Tu composición") : Selection.Count==1 ? Localizer.Current.Format("Página {0:00}",Selection[0].Number) : Localizer.Current.Format("{0} páginas",Selection.Count);
 public string SelectionSource => Selection.Count==1 ? Selection[0].Source+"\n"+Localizer.Current.Format("Página original {0}",Selection[0].SourcePage) : Selection.Count>1 ? Localizer.Current.Translate("Selección múltiple · edición en conjunto") : Localizer.Current.Translate("Selecciona una página para ver sus propiedades.");
 bool busy;
 public bool Busy {get=>busy;set{busy=value;Refresh();Notify();}}
 public bool CanExport=>!Busy && Pages.Count>0 && Pages.All(p=>p.Document is not null);
 public string PageSizeText=>Selection.Count==1?$"{(Selection[0].Rotation%180==0?Selection[0].WidthPoints:Selection[0].HeightPoints)*25.4/72:0.#} × {(Selection[0].Rotation%180==0?Selection[0].HeightPoints:Selection[0].WidthPoints)*25.4/72:0.#} mm":"—";
 public string OutputPosition=>Selection.Count==1?Localizer.Current.Format("Posición de salida · {0}",Selection[0].Number):"—";
 public event Action? CompositionChanged;
 public IEnumerable<Guid> ReferencedDocuments=>Pages.Concat(undo.SelectMany(s=>s)).Concat(redo.SelectMany(s=>s)).Where(p=>p.Document is not null).Select(p=>p.Document!.Id);
 public void Append(IReadOnlyList<PageItem> pages)=>Edit(()=>{if(Pages.Any(p=>p.Document is null))Pages.Clear();foreach(var page in pages)Pages.Add(page);Selection.Clear();},"Documentos cargados.");
 public void Reset(){Pages.Clear();Selection.Clear();undo.Clear();redo.Clear();Refresh();CompositionChanged?.Invoke();}
 public string RotationText => Selection.Count==1 ? $"{Selection[0].Rotation}°" : "—";
 public string SelectionCount => Selection.Count==0 ? Localizer.Current.Translate("Sin selección") : Localizer.Current.Format("{0} seleccionada(s)",Selection.Count);
 public ActionCommand Rotate { get; }
 public ActionCommand Remove { get; }
 public ActionCommand Previous { get; }
 public ActionCommand Next { get; }
 public ActionCommand Undo { get; }
 public ActionCommand Redo { get; }
 public ActionCommand LoadDemo { get; }
 public WorkspaceViewModel()
 {
  Localizer.Current.PropertyChanged+=(_,_)=>{Refresh();Notify(nameof(Status));};
  Rotate=new(()=>Edit(()=> {foreach(var p in Selection) p.Rotation=(p.Rotation+90)%360;},"Rotación aplicada a la composición."),()=>!Busy && HasSelection);
  Remove=new(()=>Edit(()=> {foreach(var p in Selection.ToList()) Pages.Remove(p); Selection.Clear();},"Páginas quitadas. Puedes deshacer el cambio."),()=>!Busy && HasSelection);
  Previous=new(()=>Move(-1),()=>!Busy && HasSelection && Pages.Count>0 && !Selection.Contains(Pages[0]));
  Next=new(()=>Move(1),()=>!Busy && HasSelection && Pages.Count>0 && !Selection.Contains(Pages[^1]));
  Undo=new(()=>Restore(undo,redo,"Cambio deshecho."),()=>!Busy && undo.Count>0);
  Redo=new(()=>Restore(redo,undo,"Cambio rehecho."),()=>!Busy && redo.Count>0);
  LoadDemo=new(()=>Edit(()=> {Pages.Clear(); foreach(var p in DemoPageService.Create()) Pages.Add(p); Selection.Clear();},"Ejemplo restaurado. Los cambios anteriores se pueden deshacer."));
  foreach(var p in DemoPageService.Create()) Pages.Add(p);
 }
 public void Select(IEnumerable<PageItem> items) {if(editing)return; Selection.Clear(); Selection.AddRange(items); Refresh();}
 List<PageItem> Snapshot()=>Pages.Select(p=>p.Copy()).ToList();
 void Edit(Action action,string message) {undo.Push(Snapshot()); if(undo.Count>30){var keep=undo.Take(30).Reverse().ToList();undo.Clear();foreach(var state in keep)undo.Push(state);} redo.Clear(); var ids=Selection.Select(p=>p.Id).ToHashSet(); editing=true; try { action(); Renumber(); Selection.Clear(); Selection.AddRange(Pages.Where(p=>ids.Contains(p.Id))); } finally {editing=false;} SelectionSync?.Invoke(); Status=message; Refresh(); CompositionChanged?.Invoke();}
 void Renumber() {for(int i=0;i<Pages.Count;i++) Pages[i].Number=i+1;}
 void Refresh() {foreach(var p in new[]{nameof(Summary),nameof(IsEmpty),nameof(HasSelection),nameof(SelectionTitle),nameof(SelectionSource),nameof(RotationText),nameof(SelectionCount),nameof(CanExport),nameof(PageSizeText),nameof(OutputPosition)}) Notify(p); CommandManager.InvalidateRequerySuggested();}
 void Restore(Stack<List<PageItem>> from,Stack<List<PageItem>> to,string message) {if(from.Count==0)return; to.Push(Snapshot()); var old=from.Pop(); editing=true; try {Selection.Clear(); Pages.Clear(); foreach(var p in old)Pages.Add(p);} finally {editing=false;} SelectionSync?.Invoke(); Status=message; Refresh(); CompositionChanged?.Invoke();}
 void Move(int direction)
 {
  if(!HasSelection)return;
  Edit(()=> {var ordered=Selection.OrderBy(p=>Pages.IndexOf(p)).ToList(); if(direction>0)ordered.Reverse(); foreach(var p in ordered) {int i=Pages.IndexOf(p), dest=i+direction; if(dest>=0 && dest<Pages.Count && !Selection.Contains(Pages[dest]))Pages.Move(i,dest);}},"Orden de salida actualizado.");
 }
 public void DropAt(PageItem moving,PageItem target)
 {
  if(moving==target || !Pages.Contains(moving) || !Pages.Contains(target))return;
  Edit(()=> {int destination=Pages.IndexOf(target); Pages.Move(Pages.IndexOf(moving),destination);},"Página movida a la posición indicada.");
 }
 public event PropertyChangedEventHandler? PropertyChanged;
 void Notify([CallerMemberName]string? p=null)=>PropertyChanged?.Invoke(this,new(p));
}




