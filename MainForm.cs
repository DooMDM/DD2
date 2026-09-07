using System.Diagnostics;
using FireSaveRepair.Core;
using FireSaveRepair.UI;

namespace FireSaveRepair;

public sealed class MainForm : Form
{
    readonly UiLocale locale=new();
    readonly TextBox folder=new(){Dock=DockStyle.Fill,ReadOnly=true,BorderStyle=BorderStyle.None,TabStop=false,BackColor=Theme.Surface,ForeColor=Theme.Text};
    readonly SoftButton pickFolder=new(){Width=154},pickFile=new(){Width=118},scan=new(){Width=140,Accent=true},cancel=new(){Width=108,Enabled=false};
    readonly SoftButton repair=new(){Width=224,Enabled=false,Accent=true},openResult=new(){Width=174,Enabled=false};
    readonly SoftButton en=new(){Text="EN",Width=44,Height=29},ru=new(){Text="RU",Width=44,Height=29};
    readonly SoftButton more=new(){Width=108,Height=30,Margin=new(12,6,0,0)};
    readonly CheckBox backup=new(){AutoSize=false,Width=180,Height=24,Checked=true,Margin=new(0,0,0,0)};
    readonly CheckBox firesOnly=new(){AutoSize=false,Width=118,Height=28,Margin=new(10,5,0,0)};
    readonly Label subtitle=Line(),sectionFolder=Line(),sectionFolderHelp=Line(),sectionScan=Line(),sectionScanHelp=Line(),
        status=Line(),detailTitle=Line(),detailBody=Line(),backupHint=Line(),dateHint=Line(),footer=Line();
    readonly ProgressBar progress=new(){Dock=DockStyle.Fill,Margin=Padding.Empty};
    readonly DataGridView grid=new(){Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,
        AllowUserToResizeRows=false,RowHeadersVisible=false,MultiSelect=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,
        BackgroundColor=Theme.Surface,BorderStyle=BorderStyle.None,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,
        CellBorderStyle=DataGridViewCellBorderStyle.SingleHorizontal,ScrollBars=ScrollBars.Vertical,Margin=Padding.Empty};
    readonly ToolTip tips=new(){AutoPopDelay=20000,InitialDelay=450,ReshowDelay=150};
    readonly List<ScanEntry> entries=[];
    CancellationTokenSource? cancellation;
    bool busy,closeAfter,refreshing;
    string? lastResult;
    Func<string>? statusValue;
    public MainForm()
    {
        Text="STALKER 2 · Fire Save Repair 0.6.3";
        AutoScaleMode=AutoScaleMode.Dpi;AutoScaleDimensions=new(96,96);
        Font=Theme.Font();BackColor=Theme.Background;ForeColor=Theme.Text;
        ClientSize=new(1060,760);MinimumSize=new(960,720);StartPosition=FormStartPosition.CenterScreen;
        folder.BackColor=Theme.Surface;backup.BackColor=Theme.Background;firesOnly.BackColor=Theme.Panel;
        backup.ForeColor=Theme.Text;firesOnly.ForeColor=Theme.Text;
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new(26,24,26,16),ColumnCount=1,RowCount=6,Margin=Padding.Empty};
        layout.RowStyles.Add(new(SizeType.Absolute,72));
        layout.RowStyles.Add(new(SizeType.Absolute,130));
        layout.RowStyles.Add(new(SizeType.Percent,100));
        layout.RowStyles.Add(new(SizeType.Absolute,28));
        layout.RowStyles.Add(new(SizeType.Absolute,88));
        layout.RowStyles.Add(new(SizeType.Absolute,62));
        var header=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty};
        header.ColumnStyles.Add(new(SizeType.Percent,100));header.ColumnStyles.Add(new(SizeType.Absolute,106));
        var titles=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,Margin=Padding.Empty};
        titles.RowStyles.Add(new(SizeType.Absolute,40));titles.RowStyles.Add(new(SizeType.Percent,100));
        var title=Line();title.Text="Fire Save Repair";title.Font=Theme.Font(22,FontStyle.Bold);title.ForeColor=Theme.Text;
        subtitle.Font=Theme.Font(9.5f);subtitle.ForeColor=Theme.Muted;
        titles.Controls.Add(title,0,0);titles.Controls.Add(subtitle,0,1);
        var languages=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,Margin=new(0,4,0,0),FlowDirection=FlowDirection.RightToLeft};
        en.Margin=new(0,0,4,0);ru.Margin=Padding.Empty;languages.Controls.AddRange([en,ru]);
        en.AccessibleName="English";ru.AccessibleName="Русский";
        header.Controls.Add(titles,0,0);header.Controls.Add(languages,1,0);
        layout.Controls.Add(header,0,0);
        var folderCard=new SurfacePanel{Dock=DockStyle.Fill,BackColor=Theme.Panel,Padding=new(22,14,22,12),Margin=new(0,0,0,14)};
        var folderLayout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Margin=Padding.Empty};
        folderLayout.RowStyles.Add(new(SizeType.Absolute,29));folderLayout.RowStyles.Add(new(SizeType.Absolute,25));
        folderLayout.RowStyles.Add(new(SizeType.Absolute,42));folderLayout.RowStyles.Add(new(SizeType.Percent,100));
        sectionFolder.Font=Theme.Font(13,FontStyle.Bold);sectionFolder.ForeColor=Theme.Text;
        sectionFolderHelp.Font=Theme.Font(9);sectionFolderHelp.ForeColor=Theme.Muted;
        folderLayout.Controls.Add(sectionFolder,0,0);folderLayout.Controls.Add(sectionFolderHelp,0,1);
        var pathRow=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=4,RowCount=1,Margin=Padding.Empty};
        pathRow.ColumnStyles.Add(new(SizeType.Percent,100));pathRow.ColumnStyles.Add(new(SizeType.Absolute,164));pathRow.ColumnStyles.Add(new(SizeType.Absolute,126));pathRow.ColumnStyles.Add(new(SizeType.Absolute,150));
        var pathBox=new SurfacePanel{Dock=DockStyle.Fill,BackColor=Theme.Surface,Padding=new(14,11,14,6),Margin=new(0,0,10,0)};
        folder.Font=Theme.Font(9.5f);pathBox.Controls.Add(folder);
        pathRow.Controls.Add(pathBox,0,0);pathRow.Controls.Add(pickFolder,1,0);pathRow.Controls.Add(pickFile,2,0);pathRow.Controls.Add(scan,3,0);
        folderLayout.Controls.Add(pathRow,0,2);
        var toolbar=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=new(0,8,0,0)};
        toolbar.ColumnStyles.Add(new(SizeType.Absolute,428));toolbar.ColumnStyles.Add(new(SizeType.Percent,100));
        var scanActions=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,Margin=Padding.Empty};
        scanActions.Controls.AddRange([cancel,firesOnly]);toolbar.Controls.Add(scanActions,0,0);
        dateHint.TextAlign=ContentAlignment.MiddleRight;dateHint.Font=Theme.Font(8.5f);dateHint.ForeColor=Theme.Muted;toolbar.Controls.Add(dateHint,1,0);
        folderLayout.Controls.Add(toolbar,0,3);folderCard.Controls.Add(folderLayout);layout.Controls.Add(folderCard,0,1);
        var scanCard=new SurfacePanel{Dock=DockStyle.Fill,BackColor=Theme.Panel,Padding=new(22,16,22,16),Margin=new(0,0,0,14)};
        var scanLayout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Margin=Padding.Empty};
        scanLayout.RowStyles.Add(new(SizeType.Absolute,29));scanLayout.RowStyles.Add(new(SizeType.Absolute,25));
        scanLayout.RowStyles.Add(new(SizeType.Percent,100));scanLayout.RowStyles.Add(new(SizeType.Absolute,3));
        sectionScan.Font=Theme.Font(13,FontStyle.Bold);sectionScan.ForeColor=Theme.Text;
        sectionScanHelp.Font=Theme.Font(9);sectionScanHelp.ForeColor=Theme.Muted;
        scanLayout.Controls.Add(sectionScan,0,0);scanLayout.Controls.Add(sectionScanHelp,0,1);
        grid.Columns.Add(new DataGridViewTextBoxColumn{Name="file",FillWeight=37});
        grid.Columns.Add(new DataGridViewTextBoxColumn{Name="date",FillWeight=23});
        grid.Columns.Add(new DataGridViewTextBoxColumn{Name="size",FillWeight=7});
        grid.Columns.Add(new DataGridViewTextBoxColumn{Name="state",FillWeight=33});
        grid.EnableHeadersVisualStyles=false;
        grid.ColumnHeadersDefaultCellStyle=new(){BackColor=Color.FromArgb(34,34,42),ForeColor=Theme.Text,Font=Theme.Font(8.5f,FontStyle.Bold),Padding=new(10,0,0,0)};
        grid.DefaultCellStyle=new(){BackColor=Theme.Surface,ForeColor=Theme.Text,SelectionBackColor=Theme.AccentSoft,SelectionForeColor=Color.White,Padding=new(10,0,0,0),Font=Theme.Font(9.3f)};
        grid.AlternatingRowsDefaultCellStyle.BackColor=Color.FromArgb(21,21,27);
        grid.GridColor=Theme.Border;grid.RowTemplate.Height=39;grid.ColumnHeadersHeight=38;grid.ColumnHeadersHeightSizeMode=DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.Columns["size"].DefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleRight;
        grid.Columns["size"].DefaultCellStyle.Padding=new(0,0,8,0);
        scanLayout.Controls.Add(grid,0,2);scanLayout.Controls.Add(progress,0,3);scanCard.Controls.Add(scanLayout);layout.Controls.Add(scanCard,0,2);
        status.Font=Theme.Font(8.5f);status.ForeColor=Theme.Muted;status.TextAlign=ContentAlignment.MiddleLeft;layout.Controls.Add(status,0,3);
        var detailCard=new SurfacePanel{Dock=DockStyle.Fill,BackColor=Theme.Panel,Padding=new(1),Margin=new(0,0,0,10)};
        var card=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,BackColor=Theme.Panel,Padding=new(18,12,18,12),Margin=Padding.Empty};
        card.ColumnStyles.Add(new(SizeType.Percent,100));card.ColumnStyles.Add(new(SizeType.Absolute,120));
        var explanation=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,Margin=Padding.Empty};
        explanation.RowStyles.Add(new(SizeType.Absolute,21));explanation.RowStyles.Add(new(SizeType.Percent,100));
        detailTitle.Font=Theme.Font(9.5f,FontStyle.Bold);detailBody.Font=Theme.Font(9);detailBody.ForeColor=Theme.Muted;
        detailBody.TextAlign=ContentAlignment.TopLeft;detailTitle.Name="detailTitle";detailBody.Name="detailBody";
        explanation.Controls.Add(detailTitle,0,0);explanation.Controls.Add(detailBody,0,1);
        card.Controls.Add(explanation,0,0);card.Controls.Add(more,1,0);detailCard.Controls.Add(card);layout.Controls.Add(detailCard,0,4);
        var bottom=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=2,Margin=Padding.Empty};
        bottom.ColumnStyles.Add(new(SizeType.Absolute,438));bottom.ColumnStyles.Add(new(SizeType.Percent,100));bottom.ColumnStyles.Add(new(SizeType.Absolute,360));
        bottom.RowStyles.Add(new(SizeType.Absolute,22));bottom.RowStyles.Add(new(SizeType.Percent,100));
        backupHint.TextAlign=ContentAlignment.MiddleRight;backupHint.Font=Theme.Font(8.5f);
        bottom.Controls.Add(backup,0,0);bottom.SetColumnSpan(backup,2);bottom.Controls.Add(backupHint,2,0);
        var actionButtons=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,Margin=Padding.Empty};
        actionButtons.Controls.AddRange([repair,openResult]);bottom.Controls.Add(actionButtons,0,1);
        footer.Font=Theme.Font(8.5f);footer.ForeColor=Theme.Muted;footer.TextAlign=ContentAlignment.MiddleRight;
        bottom.Controls.Add(footer,2,1);layout.Controls.Add(bottom,0,5);Controls.Add(layout);
        folder.Text=DefaultFolder();tips.SetToolTip(folder,folder.Text);
        pickFolder.Click+=(_,_)=>{
            using var dialog=new FolderBrowserDialog{Description=T("Select Data or SaveGames","Выберите Data или SaveGames"),UseDescriptionForTitle=true,InitialDirectory=folder.Text};
            if(dialog.ShowDialog(this)==DialogResult.OK){folder.Text=dialog.SelectedPath;tips.SetToolTip(folder,folder.Text);ClearEntries();}
        };
        pickFile.Click+=async(_,_)=>{
            using var dialog=new OpenFileDialog{Title=T("Select a save","Выберите сейв"),Filter=T("STALKER 2 save|*.sav","Сейв STALKER 2|*.sav"),InitialDirectory=folder.Text};
            if(dialog.ShowDialog(this)==DialogResult.OK)await ScanAsync([dialog.FileName]);
        };
        scan.Click+=async(_,_)=>{try{await ScanAsync(RepairService.FindSaves(folder.Text));}catch(Exception e){ShowError(e);}};
        cancel.Click+=(_,_)=>{cancellation?.Cancel();SetStatus(()=>T("Stopping after this file…","Остановка после текущего файла…"));};
        firesOnly.CheckedChanged+=(_,_)=>RefreshGrid();
        grid.SelectionChanged+=(_,_)=>{if(!refreshing)SelectionChanged();};
        backup.CheckedChanged+=(_,_)=>UpdateBackupHint();
        en.Click+=(_,_)=>SetEnglish(true);ru.Click+=(_,_)=>SetEnglish(false);
        repair.Click+=async(_,_)=>await RepairAsync();
        more.Click+=(_,_)=>ShowDetails();
        openResult.Click+=(_,_)=>{try{if(lastResult!=null)Process.Start(new ProcessStartInfo("explorer.exe"){ArgumentList={lastResult},UseShellExecute=false});}catch(Exception e){ShowError(e);}};
        FormClosing+=(_,e)=>{
            if(!busy)return;e.Cancel=true;
            if(cancellation!=null){cancellation.Cancel();closeAfter=true;SetStatus(()=>T("Finishing this check before closing…","Завершаю текущую проверку перед закрытием…"));}
            else SetStatus(()=>T("Finishing verification and writing. Please wait.","Завершаю проверку и запись. Пожалуйста, подождите."));
        };
        Shown+=(_,_)=>scan.Focus();
        SetStatus(()=>T("Choose a folder, then scan. No files are changed during scanning.","Выберите папку и запустите проверку. Сканирование не меняет файлы."));
        SetEnglish(true);
    }
    static Label Line()=>new(){Dock=DockStyle.Fill,AutoEllipsis=true,Margin=Padding.Empty,UseMnemonic=false};
    string T(string en,string ru)=>locale.Text(en,ru);
    internal bool English=>locale.English;
    internal string ScanButtonText=>scan.Text;
    internal Control ExplanationControl=>detailBody;
    internal void SetEnglish(bool english)
    {
        locale.English=english;en.Active=english;ru.Active=!english;en.Invalidate();ru.Invalidate();
        subtitle.Text=T("Offline save scanner and repair utility","Офлайн-проверка и ремонт сохранений");
        sectionFolder.Text=T("Save source","Источник сейвов");
        sectionFolderHelp.Text=T("Choose your STALKER 2 SaveGames/Data folder or inspect one save file.","Выберите папку STALKER 2 SaveGames/Data или проверьте один сейв.");
        sectionScan.Text=T("Saves","Сейвы");
        sectionScanHelp.Text=T("The list shows file dates. A fire match means the save needs a closer check, not always a bug.","В списке показаны даты файлов. Огонь в сейве требует проверки, но не всегда означает баг.");
        pickFolder.Text=T("Save folder","Папка сейвов");pickFile.Text=T("One save","Один сейв");
        scan.Text=T("Scan folder","Сканировать");cancel.Text=T("Stop","Стоп");
        firesOnly.Text=T("Fire only","Только огонь");backup.Text=T("Create backup","Создать бэкап");
        repair.Text=T("Repair selected save","Исправить сейв");openResult.Text=T("Report / backup","Отчёт / бэкап");
        footer.Text=T("Offline mode · local files only","Офлайн-режим · только локальные файлы");
        tips.SetToolTip(footer,T("The utility scans and repairs local save files only.","Программа проверяет и исправляет только локальные файлы сохранений."));
        dateHint.Text=T("Sorted by file date","Сортировка по дате файла");
        grid.Columns["file"].HeaderText=T("SAVE FILE","ФАЙЛ СЕЙВА");grid.Columns["date"].HeaderText=T("MODIFIED","ИЗМЕНЁН");
        grid.Columns["size"].HeaderText=T("MB","МБ");grid.Columns["state"].HeaderText=T("RESULT","РЕЗУЛЬТАТ");
        folder.AccessibleName=T("Save folder path","Путь к папке сейвов");grid.AccessibleName=T("Save scan results","Результаты проверки сейвов");
        tips.SetToolTip(dateHint,T("Fire detection alone does not prove a stuck effect.","Само наличие огня ещё не доказывает, что эффект застрял."));
        UpdateBackupHint();RefreshGrid();RenderStatus();
    }
    void UpdateBackupHint()
    {
        backupHint.Text=backup.Checked?T("Replaces original · backup kept","Замена оригинала · с бэкапом"):T("Replaces original · NO backup","Замена оригинала · БЕЗ бэкапа");
        backupHint.ForeColor=backup.Checked?Theme.Muted:Theme.Red;
    }
    static string DefaultFolder()
    {
        string root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Stalker2","Saved");
        string steam=Path.Combine(root,"STEAM","SaveGames","Data");
        return Directory.Exists(steam)?steam:Directory.Exists(root)?root:Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }
    void SetStatus(Func<string> text){statusValue=text;RenderStatus();}
    void RenderStatus(){status.Text=statusValue?.Invoke()??"";tips.SetToolTip(status,status.Text);}
    void ClearEntries(){entries.Clear();RefreshGrid();SetStatus(()=>T("Folder changed. Run a new scan.","Папка изменена. Запустите новую проверку."));}
    ScanEntry? Selected=>grid.SelectedRows.Count>0?grid.SelectedRows[0].Tag as ScanEntry:null;
    void SelectionChanged()
    {
        var e=Selected;repair.Enabled=!busy && e?.CanRepair==true;
        more.Text=e==null?T("About","О программе"):T("Details","Подробнее");
        detailTitle.Text=e==null?T("Ready to scan","Готово к проверке"):locale.Diagnostic(e.Status);
        detailTitle.ForeColor=e?.HasFire==true?(e.CanRepair?Theme.Accent:Theme.Red):e==null?Theme.Text:Theme.Green;
        detailBody.Text=e==null?T("Choose a save folder above. Files are only changed after you confirm a repair.","Выберите папку выше. Файлы меняются только после подтверждения исправления."):locale.Diagnostic(e.Detail);
        tips.SetToolTip(detailBody,detailBody.Text);
    }
    void RefreshGrid()
    {
        string? selected=Selected?.Path;refreshing=true;
        try
        {
            grid.Rows.Clear();DataGridViewRow? chosen=null;
            foreach(var e in entries.Where(e=>!firesOnly.Checked||e.HasFire))
            {
                string state=locale.Diagnostic(e.Status);
                int i=grid.Rows.Add(Path.GetFileName(e.Path),e.Modified==DateTime.MinValue?"-":e.Modified.ToString(locale.English?"yyyy-MM-dd HH:mm:ss":"dd.MM.yyyy HH:mm:ss"),(e.Size/1048576d).ToString("0.0",locale.English?System.Globalization.CultureInfo.InvariantCulture:System.Globalization.CultureInfo.GetCultureInfo("ru-RU")),state);
                var row=grid.Rows[i];row.Tag=e;row.Cells[0].ToolTipText=e.Path;row.Cells[3].ToolTipText=locale.Diagnostic(e.Detail);
                row.Cells[3].Style.ForeColor=e.HasFire?(e.CanRepair?Theme.Accent:Theme.Red):e.Hash.Length==0?Theme.Red:Theme.Green;
                if(e.Path==selected)chosen=row;
            }
            grid.ClearSelection();chosen??=grid.Rows.Count>0?grid.Rows[0]:null;
            if(chosen!=null){chosen.Selected=true;grid.CurrentCell=chosen.Cells[0];}
        }
        finally{refreshing=false;}
        SelectionChanged();
    }
    void SetBusy(bool value)
    {
        busy=value;foreach(var c in new Control[]{pickFile,pickFolder,scan,backup})c.Enabled=!value;
        cancel.Enabled=value&&cancellation!=null;SelectionChanged();
    }
    void ShowDetails()
    {
        var e=Selected;
        string body=e==null
            ?T("Scans STALKER 2 saves and repairs the supported permanent-fire bug.\n\nThe selected save is replaced in place. The checkbox controls only whether an original backup is kept. No donor save is used; other progress and inventory data are preserved.\n\nClose the game before repair. Pause cloud sync. Test the same slot in game with the trainer OFF and outside an anomaly.\n\nFire detection alone does not prove a stuck effect. Concurrent damage, unknown effects and unsupported structures block repair.",
                "Проверяет сейвы STALKER 2 и исправляет поддерживаемый баг с постоянным огнём.\n\nВыбранный сейв заменяется на месте. Галочка отвечает только за сохранение бэкапа. Донорский сейв не используется; остальной прогресс и инвентарь сохраняются.\n\nЗакройте игру перед ремонтом. Приостановите облачную синхронизацию. Проверьте тот же слот с выключенным трейнером, вне аномалии.\n\nСамо наличие огня не доказывает баг. Другой урон, неизвестные эффекты и неподдерживаемые структуры блокируют правку.")
            :locale.Diagnostic(e.Status)+"\r\n\r\n"+locale.Diagnostic(e.Detail)+"\r\n\r\n"+T("File: ","Файл: ")+e.Path+"\r\n\r\nSHA256: "+(e.Hash.Length==0?"—":e.Hash);
        AppDialog.Show(this,locale,e==null?T("About Fire Save Repair","О Fire Save Repair"):T("Save details","Подробности сейва"),body,copyText:e==null?null:body);
    }
    async Task ScanAsync(string[] files)
    {
        if(busy)return;
        if(files.Length==0){AppDialog.Show(this,locale,T("No saves found","Сейвы не найдены"),T("No .sav files in this folder. Select Data inside SaveGames.","В папке нет файлов .sav. Выберите Data внутри SaveGames."));return;}
        entries.Clear();RefreshGrid();cancellation=new();SetBusy(true);progress.Maximum=files.Length;progress.Value=0;
        try
        {
            using var codec=OodleCodec.OpenBundled();
            foreach(string file in files)
            {
                if(cancellation.IsCancellationRequested)break;
                int current=progress.Value+1;SetStatus(()=>T($"Scanning {current}/{files.Length} · {Path.GetFileName(file)}",$"Проверка {current}/{files.Length} · {Path.GetFileName(file)}"));
                var e=await Task.Run(()=>RepairService.Inspect(file,codec));entries.Add(e);progress.Value++;RefreshGrid();
            }
            SetStatus(()=>T($"Checked {entries.Count}/{files.Length}  ·  Fire {entries.Count(e=>e.HasFire)}  ·  Repairable {entries.Count(e=>e.CanRepair)}  ·  Unchecked {entries.Count(e=>e.Hash.Length==0)}",
                $"Проверено {entries.Count}/{files.Length}  ·  Огонь {entries.Count(e=>e.HasFire)}  ·  Можно исправить {entries.Count(e=>e.CanRepair)}  ·  Не проверены {entries.Count(e=>e.Hash.Length==0)}"));
        }
        catch(Exception e){ShowError(e);}
        finally{cancellation.Dispose();cancellation=null;SetBusy(false);if(closeAfter)Close();}
    }
    internal string ConfirmationText(ScanEntry entry,bool createBackup)=>T(
        $"Save: {Path.GetFileName(entry.Path)}\nFile date: {entry.Modified:yyyy-MM-dd HH:mm:ss}\n\nRemove FireBreathDamage and its supported cached damage? Other effects and progress remain from this save.\n\n"+
        (createBackup?"The original file will be REPLACED after a verified backup is created.":"WARNING: the original file will be REPLACED WITHOUT A BACKUP. This app will not be able to undo the replacement.")+
        "\n\nClose the game and pause cloud sync. Test the repaired slot with the trainer OFF and outside an anomaly.",
        $"Сейв: {Path.GetFileName(entry.Path)}\nДата файла: {entry.Modified:dd.MM.yyyy HH:mm:ss}\n\nУдалить FireBreathDamage и связанный сохранённый урон? Другие эффекты и прогресс останутся из этого сейва.\n\n"+
        (createBackup?"Исходный файл будет ЗАМЕНЁН после создания проверенной резервной копии.":"ВНИМАНИЕ: исходный файл будет ЗАМЕНЁН БЕЗ БЭКАПА. Отменить замену через эту программу будет нельзя.")+
        "\n\nЗакройте игру и приостановите облачную синхронизацию. Проверьте исправленный слот с выключенным трейнером и вне аномалии.");
    async Task RepairAsync()
    {
        var entry=Selected;if(busy||entry?.CanRepair!=true)return;
        try{RepairService.RequireGameClosed();}catch(Exception e){ShowError(e);return;}
        bool createBackup=backup.Checked;
        if(!AppDialog.Show(this,locale,T("Confirm replacement","Подтвердите замену"),ConfirmationText(entry,createBackup),confirm:true))return;
        SetBusy(true);SetStatus(()=>T("Preparing and verifying the repaired save…","Подготовка и проверка исправленного сейва…"));progress.Style=ProgressBarStyle.Marquee;
        try
        {
            string destination=Path.Combine(Path.GetDirectoryName(entry.Path)!,"FireSaveRepair-results");
            var result=await Task.Run(()=>{
                using var codec=OodleCodec.OpenBundled();var prepared=RepairService.Prepare(entry,codec);
                return RepairService.Commit(prepared,destination,createBackup);
            });
            lastResult=result.JobDirectory;openResult.Enabled=true;
            SetStatus(()=>result.CreatedBackup?T("Save replaced. Backup and report saved.","Сейв заменён. Бэкап и отчёт сохранены."):T("Save replaced WITHOUT a backup. Report saved.","Сейв заменён БЕЗ бэкапа. Отчёт сохранён."));
            entries.Remove(entry);
            entries.Add(entry with{CanRepair=false,HasFire=false,Status="Исправлен · проверьте в игре",Detail="Папка отчёта: "+result.JobDirectory,Hash=RepairService.Hash(RepairService.ReadSave(entry.Path))});
            RefreshGrid();
            string message=status.Text+"\n\n"+T("Load this exact slot with the trainer OFF.","Загрузите именно этот слот с выключенным трейнером.")+" "+
                (createBackup?T("If needed, close the game and restore the same-named file from original/.","При проблеме закройте игру и верните одноимённый файл из original/."):T("You disabled backup: this app did not retain the original version.","Вы отключили бэкап: программа не сохранила исходную версию."))+
                "\n\n"+T("Report folder:\n","Папка отчёта:\n")+result.JobDirectory;
            AppDialog.Show(this,locale,T("Repair complete","Исправление завершено"),message,copyText:result.JobDirectory);
        }
        catch(Exception e){ShowError(e);}
        finally{progress.Style=ProgressBarStyle.Blocks;SetBusy(false);}
    }
    void ShowError(Exception e)
    {
        SetStatus(()=>T("Operation stopped. Open the message for details.","Операция остановлена. Подробности — в сообщении."));
        AppDialog.Show(this,locale,T("Operation stopped","Операция остановлена"),locale.Error(e));
    }
    internal void LoadUiTestEntries(IEnumerable<ScanEntry> data)
    {entries.Clear();entries.AddRange(data);RefreshGrid();}
    internal string CurrentDetail=>detailBody.Text;
    internal string CurrentResult=>grid.Rows.Count==0?"":grid.Rows[0].Cells[3].Value?.ToString()??"";
    protected override void Dispose(bool disposing){if(disposing)tips.Dispose();base.Dispose(disposing);}
}
