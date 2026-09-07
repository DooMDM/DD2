using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

namespace FireSaveRepair.Core;

public record ScanEntry(string Path,long Size,DateTime Modified,string Hash,bool HasFire,bool CanRepair,string Status,string Detail);
public record PreparedRepair(ScanEntry Entry,byte[] Original,byte[] Repaired,string RawHashBefore,string RawHashAfter);
public record RepairResult(string JobDirectory,string? Backup,string RepairedFile,bool CreatedBackup);

public static class RepairService
{
    public static string Hash(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes));
    public static byte[] ReadSave(string path)
    {
        Bin.Require(!Path.GetFileName(path).Equals("CampaignsSave.sav",StringComparison.OrdinalIgnoreCase),"CampaignsSave.sav — это список кампаний, не сейв игрока.");
        Bin.Require(Path.GetExtension(path).Equals(".sav",StringComparison.OrdinalIgnoreCase),"Выберите файл .sav.");
        using var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);
        Bin.Require(stream.Length>=16 && stream.Length<=SaveFormat.MaxPacked,"Неподдерживаемый размер SAV.");
        byte[] data=new byte[(int)stream.Length];stream.ReadExactly(data); return data;
    }
    public static ScanEntry Inspect(string path,OodleCodec codec)
    {
        path=System.IO.Path.GetFullPath(path);
        long size=0;DateTime modified=DateTime.MinValue;
        try
        {
            var fi=new FileInfo(path);size=fi.Length;modified=fi.LastWriteTime;
            byte[] packed=ReadSave(path),raw=codec.Decode(packed);
            Diagnosis d=RepairEngine.Analyze(raw);
            string status=d.CanRepair ? "Огонь · можно исправить" : d.HasFire ? "Огонь · правка заблокирована" : "Эффект не найден";
            return new(path,size,modified,Hash(packed),d.HasFire,d.CanRepair,status,d.Message);
        }
        catch(Exception e) when(e is IOException or UnauthorizedAccessException or RepairException)
        { return new(path,size,modified,"",false,false,"Не удалось проверить",e.Message); }
    }
    public static PreparedRepair Prepare(ScanEntry entry,OodleCodec codec)
    {
        Bin.Require(entry.CanRepair,"Сначала выберите распознанный огненный сейв.");
        byte[] original=ReadSave(entry.Path);
        Bin.Require(Hash(original)==entry.Hash,"Сейв изменился после сканирования. Повторите проверку.");
        byte[] before=codec.Decode(original),after=RepairEngine.Repair(before);
        byte[] repaired=codec.EncodeVerified(after);
        return new(entry,original,repaired,Hash(before),Hash(after));
    }
    public static void RequireGameClosed()
    {
        foreach(string name in new[]{"Stalker2","Stalker2-Win64-Shipping","Stalker2-WinGDK-Shipping"})
        {
            Process[] found=Process.GetProcessesByName(name);
            bool running=found.Length>0;foreach(var p in found)p.Dispose();
            Bin.Require(!running,"Закройте S.T.A.L.K.E.R. 2 перед исправлением. Программа не закрывает игру сама.");
        }
    }
    public static string[] FindSaves(string folder)
    {
        folder=System.IO.Path.GetFullPath(folder);
        Bin.Require(Directory.Exists(folder),"Папка не найдена.");
        string data=System.IO.Path.Combine(folder,"Data");
        if(Directory.Exists(data)) folder=data;
        string[] files=Directory.EnumerateFiles(folder,"*.sav",SearchOption.TopDirectoryOnly)
            .Where(p=>!System.IO.Path.GetFileName(p).Equals("CampaignsSave.sav",StringComparison.OrdinalIgnoreCase))
            .Take(501).ToArray();
        Bin.Require(files.Length<=500,"В папке больше 500 сейвов. Выберите более узкую папку.");
        return files.OrderByDescending(File.GetLastWriteTimeUtc).ToArray();
    }
    private static void WriteNewVerified(string path,byte[] bytes)
    {
        using(var fs=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None))
        { fs.Write(bytes);fs.Flush(true); }
        Bin.Require(File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes),"Проверка записанного файла не прошла: "+path);
    }
    public static RepairResult Commit(PreparedRepair prepared,string destination,bool createBackup)
    {
        RequireGameClosed();
        Bin.Require(Hash(ReadSave(prepared.Entry.Path))==prepared.Entry.Hash,"Исходный сейв изменился. Запись отменена.");
        string job=System.IO.Path.Combine(System.IO.Path.GetFullPath(destination),$"FireRepair_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}");
        Directory.CreateDirectory(job);
        string name=System.IO.Path.GetFileName(prepared.Entry.Path);
        string? backup=null;
        if(createBackup)
        {
            string backupFolder=System.IO.Path.Combine(job,"original");Directory.CreateDirectory(backupFolder);
            backup=System.IO.Path.Combine(backupFolder,name);WriteNewVerified(backup,prepared.Original);
        }
        SaveFormat.ValidateEnvelope(prepared.Repaired);
        var report=new {
            program="STALKER 2 Fire Save Repair",version="0.6.3",timeUtc=DateTime.UtcNow,
            targetFile=name,originalSHA256=prepared.Entry.Hash,repairedSHA256=Hash(prepared.Repaired),
            rawSHA256Before=prepared.RawHashBefore,rawSHA256After=prepared.RawHashAfter,
            removed="One FireBreathDamage record (36 bytes) and Damage aggregate (8 bytes)",
            cleared="Only Other-source Damage additive cache; all other bytes preserved except counts and dictionary pointer",
            validation="CRC32, exact Oodle round trip, structural checks, restricted byte changes",
            replacementRequested=true,backupRequested=createBackup,
            backupRelativePath=createBackup?"original/"+name:null
        };
        File.WriteAllText(System.IO.Path.Combine(job,"report.json"),JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
        try
        {
            RequireGameClosed();
            Bin.Require(Hash(ReadSave(prepared.Entry.Path))==prepared.Entry.Hash,"Сейв изменился. Замена отменена: "+job);
            string temp=prepared.Entry.Path+".firerepair-"+Guid.NewGuid().ToString("N")+".tmp";
            WriteNewVerified(temp,prepared.Repaired);
            // Atomic NTFS replacement, never delete-then-copy. A durable original
            // backup exists only when the user requested it. No-backup mode is
            // explicitly confirmed in the UI, with no hidden persistent SAV copy.
            // Cloud sync should be paused: no file API can prevent a later cloud restore.
            Bin.Require(Hash(ReadSave(prepared.Entry.Path))==prepared.Entry.Hash,"Сейв изменился перед заменой. Оригинал не заменён; временная копия: "+temp);
            File.Replace(temp,prepared.Entry.Path,null);
            Bin.Require(Hash(ReadSave(prepared.Entry.Path))==Hash(prepared.Repaired),"После замены хеш отличается. Проверьте синхронизацию. Резервная копия: "+(backup??"не создавалась"));
            File.WriteAllText(System.IO.Path.Combine(job,"replacement-completed.txt"),"Original save replaced; verified SHA256: "+Hash(prepared.Repaired));
        }
        catch(Exception e) when(e is IOException or UnauthorizedAccessException or RepairException)
        {throw new RepairException(e.Message+"\nПапка отчёта: "+job+"\nРезервная копия: "+(backup??"не создавалась"));}
        return new(job,backup,prepared.Entry.Path,createBackup);
    }
}
