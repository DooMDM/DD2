using System.Runtime.InteropServices;
using System.Reflection;
using System.Security.Cryptography;

namespace FireSaveRepair.Core;

// Local bundled builds embed the owner's supplied runtime at build time.
// No runtime download. Public redistribution requires appropriate Oodle rights.
public sealed unsafe class OodleCodec : IDisposable
{
    public static bool HasBundled => Assembly.GetExecutingAssembly().GetManifestResourceNames().Contains("FireSaveRepair.Native.Oodle9");
    public static OodleCodec OpenBundled()
    {
        using var embedded=Assembly.GetExecutingAssembly().GetManifestResourceStream("FireSaveRepair.Native.Oodle9")
            ?? throw new RepairException("Неполная сборка приложения. Используйте готовый полный EXE; скачивание компонентов программой не предусмотрено.");
        using var bytes=new MemoryStream();embedded.CopyTo(bytes);byte[] payload=bytes.ToArray();
        string hash=Convert.ToHexString(SHA256.HashData(payload));
        string cache=Path.Combine(Path.GetTempPath(),"Stalker2FireSaveRepair-native",hash);
        Directory.CreateDirectory(cache);
        string library=Path.Combine(cache,"oo2core_9_win64.dll");
        if(!File.Exists(library))
        {
            // Unique file first, then no-overwrite move: concurrent instances
            // never observe a partially extracted native runtime.
            string pending=Path.Combine(cache,Guid.NewGuid().ToString("N")+".tmp");
            using(var file=new FileStream(pending,FileMode.CreateNew,FileAccess.Write,FileShare.None))
            {file.Write(payload);file.Flush(true);}
            try{File.Move(pending,library,false);}
            catch(IOException) when(File.Exists(library)) { /* Validate winner below. */ }
        }
        using var locked=new FileStream(library,FileMode.Open,FileAccess.Read,FileShare.Read);
        Bin.Require(Convert.ToHexString(SHA256.HashData(locked))==hash,"Повреждён встроенный компонент. Проверка сохранений остановлена.");
        return new OodleCodec(library);
    }
    [DllImport("kernel32",CharSet=CharSet.Unicode,SetLastError=true)]
    private static extern nint LoadLibraryExW(string file,nint reserved,uint flags);
    [DllImport("kernel32",SetLastError=true)] private static extern bool FreeLibrary(nint module);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint DecompressDelegate(byte* input,nint inputSize,byte* output,nint outputSize,
        int fuzzSafe,int checkCrc,int verbosity,nint decodeBuffer,nint decodeSize,nint callback,
        nint callbackUser,nint decoderMemory,nint decoderSize,int threadPhase);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint CompressDelegate(int compressor,byte* input,nint inputSize,byte* output,
        int level,nint options,nint dictionary,nint lrm,nint scratch,nint scratchSize);
    private nint module;
    private readonly DecompressDelegate decompress;
    private readonly CompressDelegate compress;
    public OodleCodec(string path)
    {
        path=Path.GetFullPath(path);
        Bin.Require(Path.GetFileName(path).Equals("oo2core_9_win64.dll",StringComparison.OrdinalIgnoreCase),
            "Нужна oo2core_9_win64.dll из доверенного источника (x64, Oodle 9).");
        Bin.Require(File.Exists(path),"DLL не найдена.");
        // Only the explicitly selected directory and System32 may supply imports.
        module=LoadLibraryExW(path,0,0x100 | 0x800);
        if(module==0) throw new RepairException($"Не удалось загрузить DLL (Windows {Marshal.GetLastWin32Error()}). Нужна версия x64.");
        try
        {
            decompress=Marshal.GetDelegateForFunctionPointer<DecompressDelegate>(NativeLibrary.GetExport(module,"OodleLZ_Decompress"));
            compress=Marshal.GetDelegateForFunctionPointer<CompressDelegate>(NativeLibrary.GetExport(module,"OodleLZ_Compress"));
        }
        catch { Dispose(); throw new RepairException("В DLL нет нужных функций Oodle."); }
    }
    public byte[] Decode(byte[] packed)
    {
        Bin.Require(module!=0,"Oodle закрыт.");
        int size=SaveFormat.ValidateEnvelope(packed);
        var raw=new byte[size];
        fixed(byte* src=packed) fixed(byte* dst=raw)
        {
            nint result=decompress(src+4,packed.Length-8,dst,size,1,0,0,0,0,0,0,0,0,3);
            Bin.Require(result==size,"Не удалось распаковать сохранение; файл не изменён.");
        }
        return raw;
    }
    public byte[] EncodeVerified(byte[] raw)
    {
        Bin.Require(module!=0 && raw.Length>=1024 && raw.Length<=SaveFormat.MaxRaw,"Неверный размер данных для Oodle.");
        // Conservative capacity used by the confirmed repair; no custom Oodle options.
        byte[] destination=new byte[checked(raw.Length*2+1024*1024)];
        int size;
        fixed(byte* src=raw) fixed(byte* dst=destination)
        {
            nint written=compress(8,src,raw.Length,dst,4,0,0,0,0,0);
            Bin.Require(written>0 && written<=destination.Length,"Ошибка сжатия Oodle."); size=(int)written;
        }
        byte[] packed=SaveFormat.Frame(raw,destination.AsSpan(0,size).ToArray());
        Bin.Require(Decode(packed).AsSpan().SequenceEqual(raw),"Проверка повторной распаковкой не прошла; запись запрещена.");
        return packed;
    }
    public void Dispose() { if(module!=0) { FreeLibrary(module); module=0; } }
}
