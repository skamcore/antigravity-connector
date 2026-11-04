using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using Microsoft.Win32;

namespace AntigravityConnect {
public sealed class Route {
    public string Kind, Host, Label; public int Port;
    public override string ToString() { return Label; }
}
public static class Net {
    public static byte[] Read(Stream s, int n) {
        byte[] b=new byte[n]; int p=0;
        while(p<n) { int k=s.Read(b,p,n-p); if(k==0)throw new IOException("Соединение закрыто"); p+=k; } return b;
    }
    public static byte[] Header(Stream s) {
        var b=new List<byte>();
        while(b.Count<65536) { b.Add(Read(s,1)[0]); int n=b.Count;
            if(n>=4 && b[n-4]==13 && b[n-3]==10 && b[n-2]==13 && b[n-1]==10)return b.ToArray(); }
        throw new IOException("Слишком большой заголовок прокси");
    }
    public static void Write(Stream s,string text) { byte[] b=Encoding.ASCII.GetBytes(text);s.Write(b,0,b.Length); }
    public static TcpClient Tcp(string host,int port,int timeout) {
        var c=new TcpClient();try { var t=c.ConnectAsync(host,port);if(!t.Wait(timeout))throw new IOException("Прокси не отвечает");
            t.GetAwaiter().GetResult();c.NoDelay=true;c.ReceiveTimeout=10000;c.SendTimeout=10000;return c;
        }catch {c.Close();throw;}
    }
    public static TcpClient Open(Route r,string authority) {
        var u=new Uri("http://"+authority);string host=u.DnsSafeHost;int port=u.Port;
        if(r.Kind=="direct")return Tcp(host,port,8000);
        var c=Tcp(r.Host,r.Port,2500);
        try { var s=c.GetStream();
            if(r.Kind=="http") {
                Write(s,"CONNECT "+authority+" HTTP/1.1\r\nHost: "+authority+"\r\n\r\n");
                string response=Encoding.ASCII.GetString(Header(s));
                if(!IsConnectSuccess(response))throw new IOException("VPN-прокси отклонил соединение");
            } else {
                s.Write(new byte[]{5,1,0},0,3);byte[] hello=Read(s,2);
                if(hello[0]!=5 || hello[1]!=0)throw new IOException("SOCKS5 требует неподдерживаемую авторизацию");
                byte[] name=Encoding.ASCII.GetBytes(host);if(name.Length>255)throw new IOException("Имя сервера слишком длинное");
                var request=new List<byte>(new byte[]{5,1,0,3,(byte)name.Length});request.AddRange(name);request.Add((byte)(port>>8));request.Add((byte)port);
                s.Write(request.ToArray(),0,request.Count);var response=Read(s,4);
                if(response[0]!=5 || response[1]!=0)throw new IOException("SOCKS5 не смог подключиться");
                int len=response[3]==1?4:response[3]==4?16:response[3]==3?Read(s,1)[0]:-1;
                if(len<0)throw new IOException("Некорректный ответ SOCKS5");Read(s,len+2);
            }return c;
        }catch {c.Close();throw;}
    }
    internal static bool IsConnectSuccess(string response) {
        string first=response.Split(new[]{"\r\n"},StringSplitOptions.None)[0];
        string[] fields=first.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries);
        return fields.Length>=2 && (fields[0]=="HTTP/1.1" || fields[0]=="HTTP/1.0") && fields[1]=="200";
    }
    public static bool Gate(string authority) {
        return String.Equals(authority,"cloudcode-pa.googleapis.com:443",StringComparison.OrdinalIgnoreCase) || String.Equals(authority,"daily-cloudcode-pa.googleapis.com:443",StringComparison.OrdinalIgnoreCase);
    }
    public static string Target(string authority) {return Gate(authority)?"www.google.com:443":authority;}
    public static void Hello(Stream input,Stream output) {
        var h=Read(input,5);int len=h[3]*256+h[4];
        if(h[0]!=22 || h[1]!=3 || len<33 || len>16384) {output.Write(h,0,5);return;}
        var b=Read(input,len);
        foreach(var p in new[]{new[]{0,32},new[]{32,len-32}}) {
            byte[] record={h[0],h[1],h[2],(byte)(p[1]>>8),(byte)p[1]};output.Write(record,0,5);output.Write(b,p[0],p[1]);output.Flush();
        }
    }
}
public sealed class Relay : IDisposable {
    TcpListener server; Route route; public int Port; public long Connections,Failures;public string LastError="";
    readonly object sync=new object();readonly HashSet<TcpClient> clients=new HashSet<TcpClient>();bool disposed;
    public Relay(Route upstream) {
        route=upstream;server=new TcpListener(IPAddress.Loopback,0);server.Start();Port=((IPEndPoint)server.LocalEndpoint).Port;
        Task.Run(()=>{try {while(true){var c=server.AcceptTcpClient();lock(sync){if(disposed){c.Close();return;}clients.Add(c);}Task.Run(()=>Serve(c));}}catch(SocketException){}catch(ObjectDisposedException){}});
    }
    void Serve(TcpClient client) {
        Interlocked.Increment(ref Connections);
        using(client) {TcpClient other=null;
            try {
                client.NoDelay=true;client.ReceiveTimeout=15000;
                var input=client.GetStream();string header=Encoding.ASCII.GetString(Net.Header(input));int end=header.IndexOf("\r\n");
                string[] first=header.Substring(0,end).Split(' ');if(first.Length!=3)throw new IOException("Неверный запрос прокси");
                if(first[0]=="CONNECT") {
                    other=Net.Open(route,Net.Target(first[1]));Track(other);Net.Write(input,"HTTP/1.1 200 Connection established\r\n\r\n");
                    if(Net.Gate(first[1]))Net.Hello(input,other.GetStream());
                }else {
                    var uri=new Uri(first[1]);if(uri.Scheme!="http")throw new IOException("Неподдерживаемый запрос");
                    if(route.Kind=="http")other=Net.Tcp(route.Host,route.Port,2500);
                    else {other=Net.Open(route,uri.Authority);header=first[0]+" "+uri.PathAndQuery+" "+first[2]+header.Substring(end);}
                    Track(other);
                    Net.Write(other.GetStream(),header);
                }
                client.ReceiveTimeout=0;other.ReceiveTimeout=0;
                // A client may finish sending while the server still has a response.
                // Preserve TCP half-close instead of discarding the other direction.
                Task.WhenAll(Pump(client,other),Pump(other,client)).GetAwaiter().GetResult();
            }catch(Exception e){Interlocked.Increment(ref Failures);LastError=e.GetType().Name;}
            finally {lock(sync){clients.Remove(client);if(other!=null)clients.Remove(other);}if(other!=null)other.Close();}
        }
    }
    void Track(TcpClient client){lock(sync){if(disposed){client.Close();throw new ObjectDisposedException("Relay");}clients.Add(client);}}
    static async Task Pump(TcpClient source,TcpClient target) {
        try {await source.GetStream().CopyToAsync(target.GetStream()).ConfigureAwait(false);try{target.Client.Shutdown(SocketShutdown.Send);}catch(SocketException){}catch(ObjectDisposedException){}}
        catch {source.Close();target.Close();throw;}
    }
    public void Dispose(){lock(sync){if(disposed)return;disposed=true;server.Stop();foreach(var client in clients)client.Close();clients.Clear();}}
}
public static class Discovery {
    static readonly int[] Ports={10809,10808,7890,7897,7891,1080,2080,2081,20171,20172,1087,1086,10810,9080};
    public static List<Route> Candidates() {
        var result=new List<Route>();var seen=new HashSet<string>();
        using(var key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings")) {
            if(key!=null && Convert.ToInt32(key.GetValue("ProxyEnable",0))!=0) {
                string text=Convert.ToString(key.GetValue("ProxyServer",""));
                foreach(var rawPart in text.Split(';')) {
                    string part=rawPart.Trim();
                    string kind=part.StartsWith("socks=",StringComparison.OrdinalIgnoreCase)?"socks":"http";
                    string value=part.Contains("=")?part.Substring(part.IndexOf('=')+1):part;
                    Uri uri;if(!Uri.TryCreate(value.Contains("://")?value:"http://"+value,UriKind.Absolute,out uri))continue;
                    if(uri.Scheme=="socks" || uri.Scheme=="socks5")kind="socks";
                    IPAddress ip;if(uri.UserInfo.Length>0 || !(uri.Host=="localhost" || (IPAddress.TryParse(uri.Host,out ip) && IPAddress.IsLoopback(ip))))continue;
                    Add(result,seen,new Route{Kind=kind,Host=uri.DnsSafeHost,Port=uri.Port,Label="Системный прокси · порт "+uri.Port});
                }
            }
        }
        var listeners=IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
        var ports=new HashSet<int>(Ports);
        // Recognize custom local ports belonging to common VPN cores, without reading subscriptions or secrets.
        try {
            var info=new ProcessStartInfo("netstat.exe","-ano -p tcp"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true};
            using(var p=Process.Start(info)) {
                Task<string> read=p.StandardOutput.ReadToEndAsync();
                if(!p.WaitForExit(3000)){try{p.Kill();}catch{}throw new IOException("netstat timed out");}
                string output=read.GetAwaiter().GetResult();
                foreach(string line in output.Split('\n')) {
                    var m=System.Text.RegularExpressions.Regex.Match(line,@"^\s*TCP\s+(?:127\.0\.0\.1|0\.0\.0\.0):(\d+)\s+\S+\s+LISTENING\s+(\d+)");
                    if(!m.Success)continue;
                    try {string name=Process.GetProcessById(Int32.Parse(m.Groups[2].Value)).ProcessName.ToLowerInvariant();
                        if(new[]{"xray","sing-box","mihomo","clash","v2ray","hysteria","trojan","ss-local","nekobox","hiddify","happ"}.Any(x=>name.Contains(x)))ports.Add(Int32.Parse(m.Groups[1].Value));
                    }catch(ArgumentException){}
                }
            }
        }catch{}
        foreach(int port in ports)if(listeners.Any(ep=>ep.Port==port && (IPAddress.IsLoopback(ep.Address)||ep.Address.Equals(IPAddress.Any)))) {
            foreach(string kind in new[]{"http","socks"})Add(result,seen,new Route{Kind=kind,Host="127.0.0.1",Port=port,Label=(kind=="http"?"HTTP-прокси":"SOCKS5-прокси")+" · порт "+port});
        }
        return result;
    }
    static void Add(List<Route> list,HashSet<string> seen,Route r) {if(seen.Add(r.Kind+"/"+r.Host+"/"+r.Port))list.Add(r);}
    public static bool TunnelActive() {
        return NetworkInterface.GetAllNetworkInterfaces().Any(n=>n.OperationalStatus==OperationalStatus.Up &&
            n.NetworkInterfaceType!=NetworkInterfaceType.Loopback &&
            System.Text.RegularExpressions.Regex.IsMatch(n.Name+" "+n.Description,@"(?i)\btun\b|wintun|wireguard|openvpn|tap-windows|outline|amnezia|proton|nordlynx|hiddify|mihomo|clash|happ"));
    }
    public static Route Find(bool allowTunnel) {
        foreach(var r in Candidates()) {
            try {using(Net.Open(r,"www.google.com:443"))return r;}catch{}
        }
        if(allowTunnel && TunnelActive())return new Route{Kind="direct",Host="",Label="Системный туннель VPN"};
        throw new IOException("Подключите VPN и повторите запуск. Поддерживаются локальные HTTP/SOCKS5-прокси и распознаваемые TUN-адаптеры.");
    }
}
public static class Health {
    public static void Check(Relay relay) {
        foreach(string host in new[]{"cloudcode-pa.googleapis.com","daily-cloudcode-pa.googleapis.com"}) {
            var local=new Route{Kind="http",Host="127.0.0.1",Port=relay.Port};
            using(var c=Net.Open(local,host+":443"))using(var tls=new SslStream(c.GetStream(),false)) {
                // Normal Windows trust validation; no certificate callback or insecure bypass.
                tls.ReadTimeout=10000;tls.WriteTimeout=10000;tls.AuthenticateAsClient(host,null,SslProtocols.Tls12,true);
                Net.Write(tls,"HEAD / HTTP/1.1\r\nHost: "+host+"\r\nConnection: close\r\n\r\n");
                string response=Encoding.ASCII.GetString(Net.Header(tls));if(!response.StartsWith("HTTP/1."))throw new IOException("Сервер Google не ответил");
            }
        }
    }
}
public sealed class Options {public bool Consent,AllowTunnel=true,Desktop=true;public string AppPath;public string Language="ru";}
public static class Store {
    public static string Root=Path.GetFileName(System.Windows.Forms.Application.ExecutablePath).Equals("AntigravityConnect.exe",StringComparison.OrdinalIgnoreCase)
        ?Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath)
        :Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Antigravity Connect");
    [System.Runtime.InteropServices.DllImport("kernel32.dll",CharSet=System.Runtime.InteropServices.CharSet.Unicode,SetLastError=true)]
    static extern uint GetFinalPathNameByHandle(Microsoft.Win32.SafeHandles.SafeFileHandle handle,StringBuilder path,uint length,uint flags);
    public static string PhysicalPath(string path) {
        using(var file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete)) {
            var text=new StringBuilder(32768);uint count=GetFinalPathNameByHandle(file.SafeFileHandle,text,(uint)text.Capacity,0);
            if(count==0 || count>=text.Capacity)throw new IOException("Не удалось определить путь установки.");
            string result=text.ToString();return result.StartsWith(@"\\?\UNC\")?@"\\"+result.Substring(8):result.StartsWith(@"\\?\")?result.Substring(4):result;
        }
    }
    public static string Exe {get{return Path.Combine(Root,"AntigravityConnect.exe");}}
    public static Options Load(){
        string p=Path.Combine(Root,"settings.json");if(!File.Exists(p))return new Options();
        try{return new JavaScriptSerializer().Deserialize<Options>(File.ReadAllText(p))??new Options();}
        catch(ArgumentException){Log("Invalid settings; connection remains disabled");return new Options();}
        catch(InvalidOperationException){Log("Invalid settings; connection remains disabled");return new Options();}
        catch(IOException){Log("Settings unavailable; connection remains disabled");return new Options();}
        catch(UnauthorizedAccessException){Log("Settings inaccessible; connection remains disabled");return new Options();}
    }
    public static void Save(Options o){WriteAtomic(Path.Combine(Root,"settings.json"),new JavaScriptSerializer().Serialize(o));}
    internal static void WriteAtomic(string path,string value){
        Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try {File.WriteAllText(temp,value,Encoding.UTF8);if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}
        finally {if(File.Exists(temp))File.Delete(temp);}
    }
    public static void Log(string message){try {Directory.CreateDirectory(Root);File.AppendAllText(Path.Combine(Root,"connection.log"),DateTime.Now.ToString("s")+" "+message+Environment.NewLine);}catch{}}
    public static string FindApp() {
        foreach(string root in new[]{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)+@"\Programs",Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)}) {
            string p=Path.Combine(root,@"Antigravity\Antigravity.exe");if(File.Exists(p))return p;
        }throw new IOException("Antigravity не найден. Сначала установите приложение.");
    }
    public static void Prepare(string app) {
        string folder=Path.Combine(Path.GetDirectoryName(app),"resources");
        string[] binaries=Directory.GetFiles(folder,"*.exe",SearchOption.AllDirectories).Where(p=>new[]{"language_server.exe","agy.exe"}.Contains(Path.GetFileName(p))).ToArray();
        if(binaries.Length==0)throw new IOException("Обновлённая версия Antigravity требует проверки совместимости.");
        foreach(string file in binaries) {
            byte[] bytes=File.ReadAllBytes(file);int changed=Replace(bytes,"ineligible","inexigible");
            if(changed==0) {if(Replace(bytes,"inexigible","inexigible")==0)throw new IOException("Обновлённая версия Antigravity требует проверки совместимости.");continue;}
            string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(file))).Replace("-","");
            string backups=Path.Combine(Root,"backups");Directory.CreateDirectory(backups);
            string backup=Path.Combine(backups,hash+".bin");if(!File.Exists(backup))File.Copy(file,backup);
            string temp=file+".connect-new";try {File.WriteAllBytes(temp,bytes);File.Replace(temp,file,null);}finally{if(File.Exists(temp))File.Delete(temp);}
            Log("Prepared "+Path.GetFileName(file)+"; backup "+hash);
        }
    }
    public static int Replace(byte[] b,string from,string to) {
        byte[] a=Encoding.ASCII.GetBytes(from),z=Encoding.ASCII.GetBytes(to);if(a.Length!=z.Length)throw new ArgumentException();int n=0;
        for(int i=0;i<=b.Length-a.Length;i++) {if(b[i]!=a[0])continue;int j=1;while(j<a.Length&&b[i+j]==a[j])j++;if(j!=a.Length)continue;Buffer.BlockCopy(z,0,b,i,z.Length);n++;i+=a.Length-1;}return n;
    }
}
}
