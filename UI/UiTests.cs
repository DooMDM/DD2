using System.Text.Json;
using System.Text.RegularExpressions;
using FireSaveRepair.Core;

namespace FireSaveRepair.UI;

internal static class UiTests
{
    static int count;
    static void Check(bool condition,string name)
    {if(!condition)throw new Exception("FAIL: "+name);count++;Console.WriteLine("PASS "+name);}
    static bool HasRussian(string value)=>Regex.IsMatch(value,"[А-Яа-яЁё]");
    static IEnumerable<Control> All(Control root)
    {yield return root;foreach(Control child in root.Controls)foreach(var nested in All(child))yield return nested;}
    public static int Run(string[] args)
    {
        using var form=new MainForm();form.CreateControl();form.PerformLayout();
        Check(form.English && form.ScanButtonText.Contains("Scan folder"),"English is default on a fresh form");
        Check(form.ExplanationControl is Label,"main explanation is a label, not a scrolling textbox");
        Check(!All(form).OfType<TextBox>().Any(t=>t.Multiline),"main window has no multiline scroll boxes");
        Check(!All(form).OfType<HScrollBar>().Any(s=>s.Visible),"main window has no visible horizontal scrollbars");
        Check(form.ClientSize.Height<=780*form.DeviceDpi/96 && form.ClientSize.Width<=1060*form.DeviceDpi/96,"window remains compact after media-page restyle");
        Check(!All(form).Where(c=>c is Label or Button or CheckBox).Any(c=>HasRussian(c.Text)),"initial labels, actions and hints are English");
        Check(form.Font.Name.StartsWith("Segoe UI") && form.Font.Size<=9.5f,"compact Segoe UI typography");
        Check(All(form).OfType<SurfacePanel>().Count()>=4,"media-page card surfaces are present");
        Check(!All(form).OfType<Button>().Any(c=>Regex.IsMatch(c.Text,@"[🔥⚠▤▣□⌕●]")),"button labels avoid technical glyphs");
        Check(!All(form).OfType<SoftButton>().Any(c=>c.Text.Contains("Launch",StringComparison.OrdinalIgnoreCase)),"failed launch/test action removed from main window");
        var entry=new ScanEntry(@"C:\fixtures\test.sav",1000,DateTime.Now,"TEST",true,false,"Огонь · правка заблокирована",
            "Огонь и другие источники урона: RadiationLevel10Damage. Безопасное разделение не подтверждено.");
        form.LoadUiTestEntries([entry]);
        Check(form.CurrentResult.Contains("Fire · repair blocked") && form.CurrentDetail.Contains("RadiationLevel10Damage") && !HasRussian(form.CurrentDetail),"English scan status and full blocked reason");
        Check(form.ConfirmationText(entry,false).Contains("WITHOUT A BACKUP"),"English no-backup warning is explicit");
        form.SetEnglish(false);
        Check(!form.English && form.ScanButtonText.Contains("Сканировать"),"RU switch changes actions");
        Check(!All(form).OfType<SoftButton>().Any(c=>c.Text.Contains("Запустить",StringComparison.OrdinalIgnoreCase)),"failed launch/test action removed from Russian main window");
        Check(form.CurrentResult.Contains("заблокирована") && form.CurrentDetail.Contains("источники урона"),"RU switch translates existing results without rescanning");
        Check(form.ConfirmationText(entry,false).Contains("БЕЗ БЭКАПА"),"Russian no-backup warning is explicit");
        form.SetEnglish(true);
        Check(form.CurrentResult.Contains("Fire · repair blocked") && !HasRussian(form.CurrentDetail),"switch back preserves selected result");
        SaveScreenshotsIfRequested(args,form);
        form.Size=form.MinimumSize;form.PerformLayout();
        foreach(bool english in new[]{true,false})
        {
            form.SetEnglish(english);
            foreach(var c in All(form).Reverse())c.PerformLayout();
            CheckActionSurface(form,english);
        }
        form.SetEnglish(true);
        using var another=new MainForm();Check(another.English,"every new instance starts in English");
        var locale=new UiLocale();
        Check(locale.Diagnostic("Найден FireBreathDamage и совпадающий остаточный урон (51,818). Можно подготовить исправление.").Contains("(51.818)"),"English decimal separator in dynamic diagnosis");
        Check(!HasRussian(locale.Error(new UnauthorizedAccessException("Отказано в доступе"))),"OS access errors receive an English explanation");
        Check(!HasRussian(locale.Error(new IOException("Ошибка доступа"))),"OS file errors receive an English explanation");
        int sourceIndex=Array.IndexOf(args,"--source");
        if(sourceIndex>=0 && sourceIndex+1<args.Length)
        {
            var untranslated=new List<string>();int checkedMessages=0;
            foreach(string file in Directory.EnumerateFiles(Path.Combine(args[sourceIndex+1],"Core"),"*.cs"))
                foreach(Match m in Regex.Matches(File.ReadAllText(file),"\"(?:\\\\.|[^\"\\\\])*\""))
                {
                    if(!HasRussian(m.Value))continue;
                    string text=JsonSerializer.Deserialize<string>(m.Value)!;
                    text=Regex.Replace(text,@"\{[^}]+\}","7");checkedMessages++;
                    if(HasRussian(locale.Diagnostic(text)))untranslated.Add(Path.GetFileName(file)+": "+text);
                }
            Check(checkedMessages>50,"diagnostic coverage test inspected the engine messages");
            Check(untranslated.Count==0,"every engine diagnostic fragment has an English translation: "+string.Join(" | ",untranslated));
        }
        Console.WriteLine($"ALL {count} UI CHECKS PASSED");return 0;
    }

    static void CheckActionSurface(Form form,bool english)
    {
        string[] required=english
            ?["Save folder","One save","Scan folder","Stop","Fire only","Create backup","Repair selected save","Report / backup"]
            :["Папка сейвов","Один сейв","Сканировать","Стоп","Только огонь","Создать бэкап","Исправить сейв","Отчёт / бэкап"];
        foreach(string text in required)
        {
            var control=All(form).FirstOrDefault(c=>c.Text.Contains(text,StringComparison.Ordinal));
            Check(control!=null && control.Parent!=null && control.Width>0 && control.Height>0,"laid out control in "+(english?"EN: ":"RU: ")+text);
            Check(control!.Right<=control.Parent!.ClientSize.Width && control.Bottom<=control.Parent.ClientSize.Height,"control fits parent in "+(english?"EN: ":"RU: ")+text);
            if(control is SoftButton or CheckBox)Check(TextFits(control),"text fits in "+(english?"EN: ":"RU: ")+text);
        }
    }

    static bool TextFits(Control control)
    {
        var proposed=new Size(Math.Max(1,control.ClientSize.Width-10),Math.Max(1,control.ClientSize.Height));
        var flags=TextFormatFlags.SingleLine|TextFormatFlags.NoPrefix;
        Size measured=TextRenderer.MeasureText(control.Text,control.Font,proposed,flags);
        return measured.Width<=proposed.Width && measured.Height<=proposed.Height+2;
    }

    static void SaveScreenshotsIfRequested(string[] args,MainForm form)
    {
        int index=Array.IndexOf(args,"--screenshot-dir");
        if(index<0 || index+1>=args.Length)return;
        string directory=Path.GetFullPath(args[index+1]);Directory.CreateDirectory(directory);
        form.Size=form.MinimumSize;form.PerformLayout();
        if(!form.Visible){form.Show();Application.DoEvents();}
        SaveScreenshot(form,Path.Combine(directory,"ui-en.png"));
        form.SetEnglish(false);form.PerformLayout();
        Application.DoEvents();
        SaveScreenshot(form,Path.Combine(directory,"ui-ru.png"));
        form.SetEnglish(true);Application.DoEvents();form.Hide();
    }

    static void SaveScreenshot(Form form,string path)
    {
        form.Refresh();Application.DoEvents();
        using var bitmap=new Bitmap(form.Width,form.Height);
        form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,bitmap.Size));
        bitmap.Save(path,System.Drawing.Imaging.ImageFormat.Png);
        Console.WriteLine("SCREENSHOT "+path);
    }
}
