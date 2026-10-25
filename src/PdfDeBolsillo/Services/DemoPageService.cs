using System.Globalization;
using System.Windows;
using System.Windows.Media;
using PdfDeBolsillo.Models;
namespace PdfDeBolsillo.Services;
// Ilustraciones vectoriales de muestra, no PDF importados.
public static class DemoPageService
{
 static SolidColorBrush B(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
 public static IEnumerable<PageItem> Create()
 {
  string[] titles=["Ideas que toman forma","Una mirada cercana","El proceso creativo","Notas de proyecto","Ritmo y proporción","Lo que sigue"];
  for(int i=0;i<6;i++) yield return new(){Id=i+1,Number=i+1,Source=i<4?"Cuaderno editorial.pdf":"Notas de taller.pdf",SourcePage=i<4?i+1:i-3,Title=titles[i],Thumbnail=Draw(i)};
 }
 static ImageSource Draw(int n)
 {
  var group=new DrawingGroup(); using(var d=group.Open())
  {
   void Rect(string c,double x,double y,double w,double h)=>d.DrawRectangle(B(c),null,new(x,y,w,h));
   void Text(string text,double size,double x,double y,string color="#30352F",bool bold=false) => d.DrawText(new FormattedText(text,CultureInfo.GetCultureInfo("es-MX"),FlowDirection.LeftToRight,new Typeface(new FontFamily("Segoe UI"),FontStyles.Normal,bold?FontWeights.Bold:FontWeights.Normal,FontStretches.Normal),size,B(color),1),new(x,y));
   Rect("#FFFEFA",0,0,210,297); Rect(n<4?"#AD3E20":"#335E68",19,19,20,3);
   Text(n<4?"ESTUDIO / CUADERNO 01":"TALLER / APUNTES",6,45,16); Text($"{n+1:00}",7,181,16);
   if(n==0) {
    Text("Ideas que\ntoman forma.",25,19,44,"#30352F",true); Text("Una pequeña exploración editorial",7,20,112);
    Rect("#E8C6A3",19,139,172,112); d.DrawEllipse(B("#AD3E20"),null,new(86,195),43,43); Rect("#344C46",108,160,55,75); d.DrawEllipse(B("#F9EADC"),null,new(138,197),19,19);
   } else if(n==1 || n==4) {
    Text(n==1?"Una mirada\ncercana.":"Ritmo y\nproporción.",21,19,42,"#30352F",true);
    Rect("#D7DDD1",19,110,172,92); for(int k=0;k<5;k++)Rect(k%2==0?"#426057":"#A7B39C",29+k*31,122+(k%2)*17,21,65-(k%2)*17);
    for(int k=0;k<7;k++)Rect("#C7C7BB",19,219+k*5,k%3==0?124:169,1.4);
   } else if(n==2) {
    Text("El proceso\ncreativo.",21,19,42,"#30352F",true);
    for(int k=0;k<3;k++){Text($"0{k+1}",18,19,116+k*45,"#AD3E20",true); Text(new[]{"Observar","Explorar","Dar forma"}[k],10,56,116+k*45,"#30352F",true); for(int j=0;j<3;j++)Rect("#CAC9BD",56,134+k*45+j*5,j==2?108:132,1.3);}
   } else if(n==3) {
    Text("Notas de\nproyecto.",21,19,42,"#30352F",true); Rect("#F1E9DA",19,112,172,67);Text("Dejar espacio\ntambién es diseñar.",14,30,128,"#8E3820",true);
    for(int k=0;k<12;k++)Rect("#C9C8BC",19,198+k*4.5,k%4==0?116:170,1.2);
   } else {Text("Lo que\nsigue.",26,19,44,"#30352F",true); d.DrawEllipse(B("#335E68"),null,new(104,173),53,53); Text("→",58,69,126,"#FFFEFA");Text("Las buenas ideas se construyen\npágina a página.",9,19,244);}
   Rect("#D5D0C3",19,276,172,0.7);Text("PDF DE BOLSILLO  /  DOCUMENTO DE MUESTRA",5,19,282);
  }
  group.Freeze(); var img=new DrawingImage(group); img.Freeze(); return img;
 }
}
