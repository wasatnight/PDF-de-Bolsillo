using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Windows.Data;
using System.Windows.Markup;
namespace PdfDeBolsillo.Services;

/// <summary>Catálogo local observable. Nunca traduce el contenido o nombre de documentos.</summary>
public sealed class Localizer : INotifyPropertyChanged
{
 public sealed record Translation(string Es,string En);
 public static Localizer Current { get; } = new();
 readonly Dictionary<string,Translation> catalog;
 readonly Dictionary<string,Translation> bySpanish;
 public bool IsEnglish { get; private set; }
 public string ToggleLabel => IsEnglish ? "Español" : "English";
 public string ToggleHint => Translate(IsEnglish ? "Cambiar a español" : "Cambiar a inglés");
 public string LanguageTag => IsEnglish ? "en-US" : "es-MX";
 public string this[string key] => catalog.TryGetValue(key,out var value) ? Pick(value) : throw new KeyNotFoundException(key);
 Localizer()
 {
  using var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("PdfDeBolsillo.Localization.Strings.json")
    ?? throw new InvalidOperationException("Missing localization catalog");
  catalog=JsonSerializer.Deserialize<Dictionary<string,Translation>>(stream)!;
  bySpanish=catalog.Values.ToDictionary(v=>v.Es,StringComparer.Ordinal);
 }
 string Pick(Translation value)=>IsEnglish ? value.En : value.Es;
 public string Translate(string spanish)=>bySpanish.TryGetValue(spanish,out var value)?Pick(value):spanish;
 public string Format(string spanish,params object[] args)=>string.Format(CultureInfo.GetCultureInfo(LanguageTag),Translate(spanish),args);
 public void Toggle()=>SetEnglish(!IsEnglish);
 public void SetEnglish(bool english)
 {
  if(IsEnglish==english)return;
  IsEnglish=english;
  CultureInfo.CurrentUICulture=CultureInfo.GetCultureInfo(LanguageTag);
  PropertyChanged?.Invoke(this,new PropertyChangedEventArgs(string.Empty));
 }
 public void ValidateCatalog()
 {
  foreach(var entry in catalog.Values) {
   if(string.IsNullOrWhiteSpace(entry.Es) || string.IsNullOrWhiteSpace(entry.En))throw new InvalidOperationException("Empty translation");
   // Todas las traducciones con parámetros deben aceptar sus números de página.
   _=string.Format(CultureInfo.InvariantCulture,entry.Es,1);
   _=string.Format(CultureInfo.InvariantCulture,entry.En,1);
  }
 }
 public event PropertyChangedEventHandler? PropertyChanged;
}

[MarkupExtensionReturnType(typeof(string))]
public sealed class LocExtension(string key) : MarkupExtension
{
 public override object ProvideValue(IServiceProvider serviceProvider)
  =>new Binding($"[{key}]"){Source=Localizer.Current,Mode=BindingMode.OneWay}.ProvideValue(serviceProvider);
}
