using System.Reflection;
using System.Text;
using System.Text.Json;

namespace FireSaveRepair.Core;

public record Effect(int Offset,string Sid,int Source,float Value);
public record Aggregate(int Offset,int Type,float Value);
public record ContributionValue(int Offset,int Source,float Additive,float Percentage);
public record Contribution(int Offset,int Type,List<ContributionValue> Values);
public record PlayerState(int ArrayStart,int TableOffset,int AggregateCountOffset,int End,
    List<Effect> Effects,List<Aggregate> Aggregates,List<Contribution> Contributions);
public record Diagnosis(PlayerState State,bool CanRepair,string Message)
{
    public bool HasFire => State.Effects.Any(x=>x.Sid==RepairEngine.Target);
}

public static class RepairEngine
{
    public const string Target="FireBreathDamage";
    private static readonly string[] Anchors=["BleedingMechanics","HealthMechanics","PsyMechanics","HungerMechanics","SleepMechanics"];
    private static readonly Dictionary<string,string> Types=LoadTypes();
    private static Dictionary<string,string> LoadTypes()
    {
        using var resource=Assembly.GetExecutingAssembly().GetManifestResourceStream("FireSaveRepair.Core.effect-types.json")
            ?? throw new InvalidOperationException("Missing effect catalog");
        using var json=JsonDocument.Parse(resource);
        return json.RootElement.GetProperty("types").EnumerateObject().ToDictionary(p=>p.Name,p=>p.Value.GetString()!);
    }
    public static PlayerState Locate(byte[] raw)
    {
        var (table,sids)=SaveFormat.ReadNames(raw);
        byte[] marker=Encoding.ASCII.GetBytes("struct FUnitModel");
        int start=raw.AsSpan(9,table-9).IndexOf(marker)+9;
        Bin.Require(start>=11 && Bin.U16(raw,start-2)==marker.Length,"Не найдена таблица FUnitModel.");
        // The complete save schema is not known. Search a bounded region after
        // the first unit storage header; require one unique, fully parsed state
        // with all five player-only mechanics anchors. Never pick first match.
        int stop=Math.Min(start+65536,table-128);
        var candidates=new List<PlayerState>();
        for(int p=start+marker.Length;p<stop;p++)
        {
            int count=Bin.U16(raw,p-2);
            if(count<5 || count>1024 || p+(long)count*36+8>=table) continue;
            if(raw[p]!=0 || raw[p+36]!=0 || raw[p+72]!=0) continue;
            if(Bin.U16(raw,p+1)>=sids.Length || Bin.U16(raw,p+37)>=sids.Length || Bin.U16(raw,p+73)>=sids.Length) continue;
            try
            {
                var state=ParseState(raw,p,table,sids);
                if(Anchors.All(a=>state.Effects.Any(e=>e.Sid==a))) candidates.Add(state);
            }
            catch(RepairException) { /* Not a state boundary. */ }
        }
        Bin.Require(candidates.Count==1,candidates.Count==0
            ? "Состояние игрока не распознано. Формат или набор полей не поддерживается."
            : "Найдено несколько похожих состояний игрока. Изменения запрещены.");
        return candidates[0];
    }
    private static PlayerState ParseState(byte[] raw,int start,int table,string[] sids)
    {
        int count=Bin.U16(raw,start-2),p=start;
        var effects=new List<Effect>();
        for(int i=0;i<count;i++,p+=36)
        {
            Bin.Require(p+36<table && raw[p]==0,"Неверная запись эффекта.");
            int index=Bin.U16(raw,p+1),source=Bin.U16(raw,p+7);
            Bin.Require(index<sids.Length && source<=8,"Неизвестный индекс / источник эффекта.");
            string sid=sids[index];
            Bin.Require(sid.Length>0 && sid.Length<=200 && sid.All(c=>char.IsAsciiLetterOrDigit(c)||c=='_'),"Неизвестный SID.");
            effects.Add(new(p,sid,source,Bin.F32(raw,p+30)));
        }
        int aggregateCountOffset=p; count=Bin.U16(raw,p); p+=2;
        Bin.Require(count is >0 and <200,"Неизвестная карта агрегатов.");
        var aggregates=new List<Aggregate>();
        for(int i=0;i<count;i++,p+=8)
        {
            int type=Bin.I32(raw,p);
            Bin.Require(type>=0 && type<200 && !aggregates.Any(a=>a.Type==type),"Некорректный агрегат.");
            aggregates.Add(new(p,type,Bin.F32(raw,p+4)));
        }
        Bin.Require(Bin.U32(raw,p)==0,"Неизвестные поля перед картой источников.");p+=4;
        count=Bin.U16(raw,p);p+=2; Bin.Require(count is >0 and <200,"Неизвестная карта источников.");
        var contributions=new List<Contribution>();
        for(int i=0;i<count;i++)
        {
            int at=p,type=Bin.I32(raw,p),n=Bin.U16(raw,p+4);p+=6;
            Bin.Require(type>=0 && type<200 && n<20 && !contributions.Any(c=>c.Type==type),"Некорректная карта источников.");
            var values=new List<ContributionValue>();
            for(int j=0;j<n;j++,p+=9)
            {
                Bin.Bounds(raw,p,9); int source=raw[p];
                Bin.Require(source<=8 && !values.Any(v=>v.Source==source),"Некорректный источник.");
                values.Add(new(p,source,Bin.F32(raw,p+1),Bin.F32(raw,p+5)));
            }
            contributions.Add(new(at,type,values));
        }
        Bin.Require(p<table,"Карта источников вышла за границу данных.");
        return new(start,table,aggregateCountOffset,p,effects,aggregates,contributions);
    }
    public static Diagnosis Analyze(byte[] raw)
    {
        var s=Locate(raw);
        var fire=s.Effects.Where(e=>e.Sid==Target).ToArray();
        var aggregate=s.Aggregates.SingleOrDefault(a=>a.Type==31);
        var damage=s.Contributions.SingleOrDefault(c=>c.Type==31);
        if(fire.Length==0)
        {
            bool cached=aggregate is {Value: not 0} || damage?.Values.Any(v=>v.Additive!=0||v.Percentage!=0)==true;
            return new(s,false,cached ? "FireBreathDamage нет, но есть другой или остаточный урон. Автоисправление запрещено."
                : "FireBreathDamage не найден. Это не проверка всех возможных причин потери здоровья.");
        }
        if(fire.Length!=1) return new(s,false,"Огненный эффект найден несколько раз; этот случай не проверен.");
        string[] unknown=s.Effects.Where(e=>!Types.ContainsKey(e.Sid)).Select(e=>e.Sid).Distinct().ToArray();
        if(unknown.Length>0) return new(s,false,"Огонь найден, но есть неизвестные эффекты: "+string.Join(", ",unknown.Take(5))+". Запись запрещена.");
        string[] other=s.Effects.Where(e=>e.Sid!=Target && Types[e.Sid]=="Damage").Select(e=>e.Sid).Distinct().ToArray();
        if(other.Length>0) return new(s,false,"Огонь и другие источники урона: "+string.Join(", ",other)+". Безопасное разделение не подтверждено.");
        // Match the confirmed permanent Other-source record, except its runtime
        // value and SID index, which vary. Reject modified or unknown layouts.
        byte[] signature=Convert.FromHexString("000200FEFFFFFF0600FFFFFFFF00010000000000803F000000000000803FD7A869420000");
        Effect f=fire[0];
        bool shape=Enumerable.Range(0,36).Where(i=>i!=1 && i!=2 && (i<30||i>33)).All(i=>raw[f.Offset+i]==signature[i]);
        if(!shape || f.Value<=0 || f.Value>10000) return new(s,false,"Огонь найден, но его параметры отличаются от проверенного постоянного эффекта.");
        bool map=damage is {Values.Count:2} && damage.Values[0].Source==1 && damage.Values[0].Additive==0 && damage.Values[0].Percentage==0
            && damage.Values[1].Source==6 && damage.Values[1].Percentage==0 && damage.Values[1].Additive>0;
        if(aggregate==null || aggregate.Value<=0 || aggregate.Value>10000 || !map || aggregate.Value!=damage!.Values[1].Additive)
            return new(s,false,"Огонь найден, но сохранённые суммы урона не соответствуют проверенному случаю. Запись запрещена.");
        return new(s,true,$"Найден FireBreathDamage и совпадающий остаточный урон ({aggregate.Value:0.###}). Можно подготовить исправление.");
    }
    public static byte[] Repair(byte[] raw)
    {
        var diagnosis=Analyze(raw); Bin.Require(diagnosis.CanRepair,diagnosis.Message);
        PlayerState s=diagnosis.State;
        Effect fire=s.Effects.Single(e=>e.Sid==Target);
        Aggregate damage=s.Aggregates.Single(a=>a.Type==31);
        Contribution contribution=s.Contributions.Single(c=>c.Type==31);
        var edits=new List<(int Offset,int Length)> { (5,4),(s.ArrayStart-2,2),(s.AggregateCountOffset,2),(contribution.Values[1].Offset+1,4) };
        byte[] work=(byte[])raw.Clone();
        Bin.W32(work,5,s.TableOffset-44);
        Bin.W16(work,s.ArrayStart-2,s.Effects.Count-1);
        Bin.W16(work,s.AggregateCountOffset,s.Aggregates.Count-1);
        Bin.W32(work,contribution.Values[1].Offset+1,0);
        for(int i=0;i<raw.Length;i++) if(work[i]!=raw[i])
            Bin.Require(edits.Any(e=>i>=e.Offset && i<e.Offset+e.Length),"Непредусмотренное изменение данных.");
        var removals=new[]{(Offset:fire.Offset,Length:36),(Offset:damage.Offset,Length:8)}.OrderBy(e=>e.Offset).ToArray();
        byte[] result=new byte[raw.Length-44]; int from=0,to=0;
        foreach(var r in removals)
        { work.AsSpan(from,r.Offset-from).CopyTo(result.AsSpan(to)); to+=r.Offset-from; from=r.Offset+r.Length; }
        work.AsSpan(from).CopyTo(result.AsSpan(to));
        var after=Analyze(result);
        Bin.Require(!after.HasFire && !after.State.Aggregates.Any(a=>a.Type==31) && after.State.Contributions.Single(c=>c.Type==31).Values.All(v=>v.Additive==0 && v.Percentage==0),"Итоговое состояние не прошло проверку.");
        // An explicit preservation check supplements the whitelist above: the
        // remaining player/world/quest payload and dictionaries are unchanged.
        Bin.Require(result.AsSpan(after.State.End).SequenceEqual(raw.AsSpan(s.End)),"Изменились данные вне состояния эффектов.");
        Bin.Require(result.AsSpan(after.State.TableOffset).SequenceEqual(raw.AsSpan(s.TableOffset)),"Изменились словари.");
        return result;
    }
}
