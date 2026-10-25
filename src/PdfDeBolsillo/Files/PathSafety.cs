using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using PdfDeBolsillo.Models;
namespace PdfDeBolsillo.Files;
public static class PathSafety
{
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]
 static extern uint GetFinalPathNameByHandle(SafeFileHandle handle,StringBuilder path,uint length,uint flags);
 public static string Canonical(string path)
 {
  string full=Path.GetFullPath(path);
  if(File.Exists(full)){
   using var file=new FileStream(full,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);
   var buffer=new StringBuilder(32768);uint length=GetFinalPathNameByHandle(file.SafeFileHandle,buffer,(uint)buffer.Capacity,0);
   if(length==0 || length>=buffer.Capacity)throw new IOException("Cannot resolve path");
   return buffer.ToString();
  }
  var parent=Path.GetDirectoryName(full)!;
  if(!Directory.Exists(parent))throw new DirectoryNotFoundException();
  // Resolve enlaces/junctions también cuando la salida todavía no existe.
  using var handle=CreateFile(parent,0,7,IntPtr.Zero,3,0x02000000,IntPtr.Zero);
  if(handle.IsInvalid)throw new IOException("Cannot resolve output folder");
  var result=new StringBuilder(32768);uint n=GetFinalPathNameByHandle(handle,result,(uint)result.Capacity,0);
  if(n==0 || n>=result.Capacity)throw new IOException("Cannot resolve output folder");
  return Path.Combine(result.ToString(),Path.GetFileName(full));
 }
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]
 static extern SafeFileHandle CreateFile(string name,uint access,uint share,IntPtr security,uint creation,uint flags,IntPtr template);
 public static void Validate(IEnumerable<string> destinations,IEnumerable<DocumentSource> originals)
 {
  var inputs=originals.SelectMany(d=>new[]{Canonical(d.OriginalPath),Canonical(d.SnapshotPath)}).ToHashSet(StringComparer.OrdinalIgnoreCase);
  var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  foreach(string target in destinations){
   if(!string.Equals(Path.GetExtension(target),".pdf",StringComparison.OrdinalIgnoreCase))throw new DocumentException("La salida debe tener extensión .pdf.");
   string resolved=Canonical(target);
   if(inputs.Contains(resolved))throw new DocumentException("La salida no puede reemplazar un documento original.");
   if(!seen.Add(resolved))throw new DocumentException("Dos archivos de salida tienen el mismo nombre.");
  }
 }
}
