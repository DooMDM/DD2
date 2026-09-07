using System.Buffers.Binary;
using System.Text;

namespace FireSaveRepair.Core;

public sealed class RepairException(string message) : Exception(message);

public static class Bin
{
    public static void Require(bool condition, string message)
    { if (!condition) throw new RepairException(message); }
    public static void Bounds(byte[] b, int p, int n)
        => Require(p >= 0 && n >= 0 && p <= b.Length - n, "Оборванная структура сохранения.");
    public static ushort U16(byte[] b, int p) { Bounds(b,p,2); return BinaryPrimitives.ReadUInt16LittleEndian(b.AsSpan(p)); }
    public static uint U32(byte[] b, int p) { Bounds(b,p,4); return BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(p)); }
    public static int I32(byte[] b, int p) => unchecked((int)U32(b,p));
    public static float F32(byte[] b, int p)
    { float v=BitConverter.Int32BitsToSingle(I32(b,p)); Require(float.IsFinite(v), "Некорректное числовое значение."); return v; }
    public static void W16(byte[] b,int p,int v) => BinaryPrimitives.WriteUInt16LittleEndian(b.AsSpan(p),checked((ushort)v));
    public static void W32(byte[] b,int p,int v) => BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(p),checked((uint)v));
}

public static class SaveFormat
{
    public const int MaxPacked = 128 * 1024 * 1024, MaxRaw = 256 * 1024 * 1024;
    private static readonly uint[] CrcTable = Enumerable.Range(0,256).Select(i => {
        uint c=(uint)i; for(int k=0;k<8;k++) c=(c&1)!=0 ? 0xedb88320U^(c>>1) : c>>1; return c;
    }).ToArray();
    public static uint Crc32(ReadOnlySpan<byte> data)
    { uint c=0xffffffff; foreach(byte x in data) c=CrcTable[(c^x)&255]^(c>>8); return c^0xffffffff; }
    public static int ValidateEnvelope(byte[] packed)
    {
        Bin.Require(packed.Length >= 16 && packed.Length <= MaxPacked, "Неподдерживаемый размер SAV.");
        Bin.Require(Crc32(packed.AsSpan(0,packed.Length-4)) == Bin.U32(packed,packed.Length-4),
            "CRC32 не совпадает. Файл повреждён или имеет другой формат; изменения запрещены.");
        uint size=Bin.U32(packed,0);
        Bin.Require(size >= 1024 && size <= MaxRaw, "Неподдерживаемый распакованный размер.");
        return (int)size;
    }
    public static byte[] Frame(byte[] raw,byte[] compressed)
    {
        Bin.Require(raw.Length <= MaxRaw && compressed.Length <= MaxPacked-8,"Превышен лимит размера.");
        var result=new byte[compressed.Length+8]; Bin.W32(result,0,raw.Length);
        compressed.CopyTo(result,4);
        BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(result.Length-4),Crc32(result.AsSpan(0,result.Length-4)));
        return result;
    }
    public static (int Offset, string[] Sids) ReadNames(byte[] raw)
    {
        Bin.Require(raw.Length >= 1024 && raw.Length <= MaxRaw,"Неподдерживаемый размер данных.");
        Bin.Require(Bin.U32(raw,0)==182 && raw[4]==0,"Поддерживается только проверенный формат 182 (игра 2.0.4).");
        uint offset=Bin.U32(raw,5);
        Bin.Require(offset > 100 && offset < raw.Length-44,"Некорректный указатель словарей.");
        int p=(int)offset; string[]? sids=null;
        var utf8=new UTF8Encoding(false,true);
        for(int section=0;section<2;section++)
        {
            Bin.Require(Bin.U16(raw,p)==11,"Неизвестная структура словарей."); p+=2;
            for(int group=0;group<11;group++)
            {
                int count=Bin.U16(raw,p);p+=2;
                var names = section==1 && group==0 ? new string[count] : null;
                for(int i=0;i<count;i++)
                {
                    int len=Bin.U16(raw,p);p+=2; Bin.Bounds(raw,p,len);
                    // Reject invalid encodings even in groups not used by the repair.
                    string name;
                    try { name=utf8.GetString(raw,p,len); }
                    catch(DecoderFallbackException) { throw new RepairException("Неверная кодировка словаря."); }
                    if(names!=null) names[i]=name; p+=len;
                }
                if(names!=null) sids=names;
            }
        }
        Bin.Require(p==raw.Length && sids is {Length:>2} && sids[0]=="Player" && sids[1]=="",
            "Неизвестная структура словаря Player / лишние данные.");
        return ((int)offset,sids!);
    }
}
