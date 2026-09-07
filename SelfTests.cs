using System.Text;
using System.Text.Json;
using FireSaveRepair.Core;

namespace FireSaveRepair;

internal static class SelfTests
{
    static int passed;
    static void Check(bool condition,string name)
    {if(!condition)throw new Exception("FAIL: "+name);passed++;Console.WriteLine("PASS "+name);}
    static void Reject(Action action,string name)
    {try{action();}catch(RepairException){Check(true,name);return;}throw new Exception("FAIL (accepted unsafe input): "+name);}
    static byte[] Fixture(string extra="",bool duplicate=false)
    {
        string[] names=["Player","","FireBreathDamage","BleedingMechanics","HealthMechanics","PsyMechanics","HungerMechanics","SleepMechanics",extra];
        byte[] raw=new byte[100000];Bin.W32(raw,0,182);Bin.W32(raw,5,raw.Length);
        Encoding.ASCII.GetBytes("struct FUnitModel").CopyTo(raw,4096);Bin.W16(raw,4094,17);
        int n=extra.Length==0?6:7;Bin.W16(raw,7998,n);
        byte[] fire=Convert.FromHexString("000200FEFFFFFF0600FFFFFFFF00010000000000803F000000000000803FD7A869420000");
        fire.CopyTo(raw,8000);
        for(int i=1;i<n;i++){Bin.W16(raw,8000+i*36+1,i+2);}
        int p=8000+n*36;Bin.W16(raw,p,2);p+=2;
        Bin.W32(raw,p,31);Bin.W32(raw,p+4,BitConverter.SingleToInt32Bits(51.818161f));p+=8;
        Bin.W32(raw,p,71);Bin.W32(raw,p+4,BitConverter.SingleToInt32Bits(12.5f));p+=8;
        p+=4;Bin.W16(raw,p,1);p+=2;Bin.W32(raw,p,31);Bin.W16(raw,p+4,2);p+=6;
        raw[p]=1;p+=9;raw[p]=6;Bin.W32(raw,p+1,BitConverter.SingleToInt32Bits(51.818161f));p+=9;
        if(duplicate)raw.AsSpan(7998,p-7998).CopyTo(raw.AsSpan(30000));
        using var stream=new MemoryStream();stream.Write(raw);
        using var writer=new BinaryWriter(stream,Encoding.UTF8,true);
        for(int section=0;section<2;section++)
        {
            writer.Write((ushort)11);
            for(int group=0;group<11;group++)
            {
                var strings=section==1 && group==0?names:[];
                writer.Write((ushort)strings.Length);
                foreach(string name in strings){byte[] bytes=Encoding.UTF8.GetBytes(name);writer.Write((ushort)bytes.Length);writer.Write(bytes);}
            }
        }
        return stream.ToArray();
    }
    public static int Run(string[] args)
    {
        string? Option(string key){int i=Array.IndexOf(args,key);return i>=0 && i+1<args.Length?args[i+1]:null;}
        if(args[0]=="--scan")
        {
            if(args.Length<2)throw new Exception("Usage: --scan <folder> [--dll <trusted DLL>]");
            using var codec=Option("--dll") is string explicitDll?new OodleCodec(explicitDll):OodleCodec.OpenBundled();
            var entries=RepairService.FindSaves(args[1]).Select(p=>RepairService.Inspect(p,codec)).ToArray();
            Console.WriteLine(JsonSerializer.Serialize(entries,new JsonSerializerOptions{WriteIndented=true}));return 0;
        }
        if(args[0]!="--self-test")throw new Exception("Options: --self-test [--dll <path>] [--corpus <private test folder>] or --scan <folder> --dll <path>");
        Check(SaveFormat.Crc32(Encoding.ASCII.GetBytes("123456789"))==0xcbf43926,"CRC32 standard check vector");
        byte[] raw=Fixture();var state=RepairEngine.Analyze(raw);
        Check(state.CanRepair && state.HasFire,"locate player without fixed save offsets");
        byte[] repaired=RepairEngine.Repair(raw);
        Check(repaired.Length==raw.Length-44,"only 44 bytes removed");
        Check(!RepairEngine.Analyze(repaired).HasFire,"repaired state has no fire");
        Reject(()=>RepairEngine.Repair(repaired),"second repair rejected / idempotent no write");
        var changed=(byte[])raw.Clone();Bin.W32(changed,0,183);Reject(()=>RepairEngine.Analyze(changed),"unsupported version");
        changed=(byte[])raw.Clone();Bin.W32(changed,5,raw.Length+100);Reject(()=>RepairEngine.Analyze(changed),"invalid name pointer");
        Reject(()=>RepairEngine.Analyze(raw[..9000]),"truncated raw");
        Reject(()=>RepairEngine.Analyze(Fixture(duplicate:true)),"ambiguous player states");
        Check(!RepairEngine.Analyze(Fixture("UnknownModEffect")).CanRepair,"unknown effect refuses damage-cache clear");
        Check(!RepairEngine.Analyze(Fixture("StrongRadiationDamage")).CanRepair,"coexisting Damage effect refuses repair");
        changed=Fixture();var d=RepairEngine.Analyze(changed);int aggregate=d.State.Aggregates.Single(a=>a.Type==31).Offset;
        Bin.W32(changed,aggregate+4,BitConverter.SingleToInt32Bits(19f));Check(!RepairEngine.Analyze(changed).CanRepair,"mismatched cached damage refuses repair");
        changed=Fixture();changed[8007]=7;Check(!RepairEngine.Analyze(changed).CanRepair,"unknown Fire source refuses repair");
        changed=Fixture();changed[8000+14]=0;Check(!RepairEngine.Analyze(changed).CanRepair,"unknown Fire flag refuses repair");
        changed=Fixture();Bin.W32(changed,8030,unchecked((int)0x7fc00000));Reject(()=>RepairEngine.Analyze(changed),"NaN effect value");
        changed=Fixture();Bin.W16(changed,8001,65535);Reject(()=>RepairEngine.Analyze(changed),"invalid SID index");
        changed=Fixture();Bin.W16(changed,7998,65535);Reject(()=>RepairEngine.Analyze(changed),"oversized effect array");
        changed=Fixture();changed[changed.Length-1]=255;Reject(()=>RepairEngine.Analyze(changed),"malformed dictionary tail");
        changed=Fixture();byte[] first=changed.AsSpan(8000,36).ToArray();changed.AsSpan(8036,5*36).CopyTo(changed.AsSpan(8000));first.CopyTo(changed,8000+5*36);
        Check(!RepairEngine.Analyze(RepairEngine.Repair(changed)).HasFire,"fire is not necessarily first effect");
        byte[] packed=SaveFormat.Frame(raw,new byte[100]);Check(SaveFormat.ValidateEnvelope(packed)==raw.Length,"valid SAV framing");
        packed[8]^=1;Reject(()=>SaveFormat.ValidateEnvelope(packed),"corrupt compressed byte / CRC");
        Reject(()=>SaveFormat.ValidateEnvelope(new byte[15]),"short SAV envelope");
        Check(SaveFormat.Crc32([])==0,"empty CRC vector");
        string? corpus=Option("--corpus"),dll=Option("--dll");
        if(corpus!=null)
        {
            int rawCount=0,fireCount=0;
            foreach(string file in Directory.EnumerateFiles(corpus,"*.raw").Where(p=>!Path.GetFileName(p).StartsWith("Campaign")))
            {
                byte[] data=File.ReadAllBytes(file);Diagnosis diag=RepairEngine.Analyze(data);rawCount++;
                Console.WriteLine($"CORPUS {Path.GetFileName(file)}: fire={diag.HasFire}, repair={diag.CanRepair}, offset={diag.State.ArrayStart}");
                if(diag.CanRepair){fireCount++;byte[] result=RepairEngine.Repair(data);Check(!RepairEngine.Analyze(result).HasFire,"corpus repair "+Path.GetFileName(file));}
            }
            Check(rawCount>=10 && fireCount>=6,"private regression corpus parsed");
            if(Option("--original-raw") is string sourceRaw && Option("--repaired-raw") is string expectedRaw)
                Check(RepairEngine.Repair(File.ReadAllBytes(sourceRaw)).AsSpan().SequenceEqual(File.ReadAllBytes(expectedRaw)),"EXACT golden match to user-confirmed in-game repair");
        }
        if(dll!=null || OodleCodec.HasBundled)
        {
            using var codec=dll!=null?new OodleCodec(dll):OodleCodec.OpenBundled();
            byte[] encoded=codec.EncodeVerified(raw);
            Check(codec.Decode(encoded).AsSpan().SequenceEqual(raw),"synthetic Oodle round trip + CRC");
            RunFileTests(codec,encoded);
            if(Option("--original-sav") is string original && Option("--repaired-raw") is string goldenPath)
            {
                byte[] decoded=codec.Decode(File.ReadAllBytes(original));
                if(Option("--original-raw") is string sourceRaw)
                    Check(decoded.AsSpan().SequenceEqual(File.ReadAllBytes(sourceRaw)),"original real save decode");
                byte[] golden=File.ReadAllBytes(goldenPath);
                byte[] result=codec.EncodeVerified(RepairEngine.Repair(decoded));
                Check(codec.Decode(result).AsSpan().SequenceEqual(golden),"real SAV exact golden repair and Oodle verification");
            }
            if(Option("--invalid-sav") is string invalidSav)
                Reject(()=>codec.Decode(File.ReadAllBytes(invalidSav)),"historical missing-CRC save rejected before Oodle");
        }
        Console.WriteLine($"ALL {passed} CHECKS PASSED");return 0;
    }
    static void RunFileTests(OodleCodec codec,byte[] encoded)
    {
        string temp=Path.Combine(Path.GetTempPath(),"FireSaveRepair-tests-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp); // Retained intentionally; never recursive-delete user paths.
        string input=Path.Combine(temp,"test.sav");File.WriteAllBytes(input,encoded);
        string metadata=Path.Combine(temp,"CampaignsSave.sav");File.WriteAllBytes(metadata,encoded);
        Check(RepairService.FindSaves(temp).Length==1,"folder scanner excludes campaign metadata");
        Reject(()=>RepairService.ReadSave(metadata),"cannot repair campaign metadata");
        var entry=RepairService.Inspect(input,codec);Check(entry.CanRepair,"scanner detects fire");
        var prepared=RepairService.Prepare(entry,codec);
        var result=RepairService.Commit(prepared,temp,true);
        Check(!RepairService.Inspect(input,codec).HasFire,"backup mode replaces original with repaired save");
        Check(File.ReadAllBytes(result.Backup!).AsSpan().SequenceEqual(encoded),"backup exactly matches original");
        Check(!RepairService.Inspect(result.RepairedFile,codec).HasFire,"repaired original rescans without Fire");
        File.WriteAllBytes(input,encoded);
        var result2=RepairService.Commit(prepared,temp,true);
        Check(result2.JobDirectory!=result.JobDirectory && File.Exists(result.Backup),"repeat repair never overwrites backup");
        File.WriteAllBytes(input,encoded);
        File.AppendAllText(input,"changed");
        Reject(()=>RepairService.Prepare(entry,codec),"source changed since scan");
        Reject(()=>RepairService.Commit(prepared,temp,true),"source changed before commit");
        File.WriteAllBytes(input,encoded);
        var installed=RepairService.Commit(prepared,temp,false);
        Check(!installed.CreatedBackup && !RepairService.Inspect(input,codec).HasFire,"unchecked backup still atomically replaces original");
        Check(installed.Backup==null && !Directory.EnumerateFiles(installed.JobDirectory,"*.sav",SearchOption.AllDirectories).Any(),"unchecked backup creates NO persistent original or repaired copy");
        Check(File.ReadAllBytes(metadata).AsSpan().SequenceEqual(encoded),"campaign metadata unchanged");
        Console.WriteLine("Synthetic filesystem test artifacts: "+temp);
    }
}
