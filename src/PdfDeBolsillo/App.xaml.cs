using System.Windows;
using System.Windows.Threading;
namespace PdfDeBolsillo;
public partial class App : Application
{
 protected override async void OnStartup(StartupEventArgs e)
 {
  base.OnStartup(e);
  int functional=Array.IndexOf(e.Args,"--verify-pdf");
  if(functional>=0 && functional+1<e.Args.Length){
   ShutdownMode=ShutdownMode.OnExplicitShutdown;
   try{await FunctionalVerification.RunAsync(e.Args[functional+1]);Shutdown(0);}
   catch(Exception ex){System.IO.File.WriteAllText(System.IO.Path.Combine(e.Args[functional+1],"error-verificacion.txt"),ex.ToString());Shutdown(1);}
   return;
  }
  if(e.Args.Contains("--verify")) { Verification.Run(); Shutdown(); return; }
  if(e.Args.Contains("--english")) Services.Localizer.Current.SetEnglish(true);
  var window=new MainWindow();
  if(e.Args.Contains("--compact")) { window.Width=900;window.Height=720; }
  window.Show();
#if DEMO_RECORDING
  int demo=Array.IndexOf(e.Args,"--record-demo");
  if(demo>=0 && demo+1<e.Args.Length){
   try{await window.RecordDemoAsync(e.Args[demo+1]);Shutdown(0);}
   catch(Exception ex){System.IO.File.WriteAllText(System.IO.Path.Combine(e.Args[demo+1],"error.txt"),ex.ToString());Shutdown(1);}
   return;
  }
#endif
  int capture=Array.IndexOf(e.Args,"--capture");
  if(capture>=0 && capture+1<e.Args.Length) {
   var timer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(1)};
   timer.Tick+=(_,_)=> {timer.Stop();window.SavePreview(e.Args[capture+1]);Shutdown();};timer.Start();
  }
 }
}



