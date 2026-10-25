using System.IO;
using System.Text.Json;
using PdfDeBolsillo.ViewModels;
namespace PdfDeBolsillo;
internal static class Verification
{
 public static void Run()
 {
  var results=new List<string>();
  void Check(bool valid,string name) {if(!valid)throw new InvalidOperationException(name);results.Add(name);}
  var vm=new WorkspaceViewModel();
  Check(vm.Pages.Count==6 && vm.Pages.Select(p=>p.Id).Distinct().Count()==6,"Seis páginas con identidad única");
  Check(vm.Pages.All(p=>p.Thumbnail!.IsFrozen),"Miniaturas inmutables reutilizables");
  vm.Select([vm.Pages[0],vm.Pages[1]]);vm.Rotate.Execute(null);
  Check(vm.Pages.Take(2).All(p=>p.Rotation==90),"Giro de selección múltiple");
  vm.Undo.Execute(null); Check(vm.Pages.All(p=>p.Rotation==0),"Deshacer giro");
  vm.Redo.Execute(null); Check(vm.Pages.Take(2).All(p=>p.Rotation==90),"Rehacer giro");
  vm.Select([vm.Pages[1],vm.Pages[2]]);vm.Next.Execute(null);
  Check(string.Join(",",vm.Pages.Select(p=>p.Id))=="1,4,2,3,5,6","Mover bloque conservando su orden");
  vm.Undo.Execute(null);
  vm.DropAt(vm.Pages[0],vm.Pages[3]);
  Check(string.Join(",",vm.Pages.Select(p=>p.Id))=="2,3,4,1,5,6","Reordenar a la posición de destino");
  Check(vm.Pages.Select(p=>p.Number).SequenceEqual(Enumerable.Range(1,6)),"Renumeración de la salida");
  vm.Select(vm.Pages.ToList());vm.Remove.Execute(null);
  Check(vm.IsEmpty && !vm.HasSelection,"Quitar todas muestra estado vacío");
  Check(!vm.Rotate.CanExecute(null) && !vm.Next.CanExecute(null),"Acciones bloqueadas sin selección");
  vm.Undo.Execute(null);Check(vm.Pages.Count==6,"Deshacer eliminación completa");
  vm.LoadDemo.Execute(null);Check(vm.Pages[0].Rotation==0 && vm.Pages[0].Id==1,"Restaurar muestra");
  vm.Select([vm.Pages[0]]);Check(!vm.Previous.CanExecute(null),"Límite inicial del movimiento");
  vm.Select([vm.Pages[^1]]);Check(!vm.Next.CanExecute(null),"Límite final del movimiento");
  VerifyLanguage(Check);
  File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"verificacion.json"),JsonSerializer.Serialize(new {estado="correcto",comprobaciones=results},new JsonSerializerOptions{WriteIndented=true}));
 }
 static void VerifyLanguage(Action<bool,string> check)
 {
  var l=Services.Localizer.Current;
  l.SetEnglish(false);
  l.ValidateCatalog();
  check(true,"Catálogo bilingüe completo y formatos válidos");
  var vm=new WorkspaceViewModel();
  vm.Select([vm.Pages[1]]);
  vm.Rotate.Execute(null);
  var thumbnail=vm.Pages[1].Thumbnail;
  var source=vm.Pages[1].Source;
  string before=string.Join(",",vm.Pages.Select(p=>$"{p.Id}:{p.Rotation}"));
  var liveCaption=new System.Windows.Controls.TextBlock();
  System.Windows.Data.BindingOperations.SetBinding(liveCaption,System.Windows.Controls.TextBlock.TextProperty,
    new System.Windows.Data.Binding("[T002]"){Source=l});
  check(liveCaption.Text=="Ayuda","Enlace de interfaz inicial en español");
  l.Toggle();
  check(liveCaption.Text=="Help" && l.ToggleLabel=="Español","Enlace observable cambia a inglés sin recrear controles");
  check(vm.SelectionCount=="1 selected" && vm.SelectionSource.Contains("Original page 2"),"Inspector y selección en inglés");
  check(vm.Pages[1].OriginalLabel=="Original · page 2" && vm.Pages[1].RotationLabel=="Rotation · 90°","Etiquetas de previsualización en inglés");
  check(vm.Pages[1].Source==source && ReferenceEquals(thumbnail,vm.Pages[1].Thumbnail),"Idioma conserva nombre y contenido documental");
  check(before==string.Join(",",vm.Pages.Select(p=>$"{p.Id}:{p.Rotation}")),"Idioma conserva composición y rotaciones");
  check(l.Translate("Ayuda")=="Help" && l.Translate("Abrir documentos")=="Open documents","Encabezados de diálogos traducidos");
  check(vm.Status=="Rotation applied to the composition.","Mensajes dinámicos traducidos");
  vm.Undo.Execute(null);
  check(vm.Pages[1].Rotation==0,"Historial de deshacer conservado al cambiar idioma");
  l.Toggle();
  check(liveCaption.Text=="Ayuda" && vm.Pages[1].OriginalLabel=="Original · página 2" && l.ToggleLabel=="English","Regreso a español sin reiniciar");
  vm.Select(vm.Pages.ToList()); vm.Remove.Execute(null);
  l.Toggle();
  check(vm.IsEmpty && vm.SelectionCount=="No selection","Estado vacío bilingüe sin selección");
  l.SetEnglish(false);
 }
}


