using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace FireSaveRepair.UI;

public static class Theme
{
    public static readonly Color Background=Color.FromArgb(16,16,20),Surface=Color.FromArgb(24,24,30),Panel=Color.FromArgb(30,30,37),
        Border=Color.FromArgb(62,62,72),Text=Color.FromArgb(244,244,247),Muted=Color.FromArgb(184,187,197),
        Accent=Color.FromArgb(196,116,56),AccentSoft=Color.FromArgb(82,58,43),Green=Color.FromArgb(112,203,139),Red=Color.FromArgb(255,111,111);
    static readonly string Family=ChooseFont();
    static string ChooseFont()
    {
        using var installed=new InstalledFontCollection();
        return installed.Families.Any(f=>f.Name=="Segoe UI Variable Text")?"Segoe UI Variable Text":"Segoe UI";
    }
    public static Font Font(float size=9.5f,FontStyle style=FontStyle.Regular)=>new(Family,size,style);
    public static GraphicsPath Rounded(RectangleF r,float radius)
    {
        var p=new GraphicsPath();float d=radius*2;
        p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);
        p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;
    }
}

public sealed class SurfacePanel : Panel
{
    public Color BorderColor {get;set;}=Theme.Border;
    public float Radius {get;set;}=7;
    public SurfacePanel()
    {
        BackColor=Theme.Panel;Padding=new(1);
        SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent?.BackColor??Theme.Background);
        e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
        using var path=Theme.Rounded(new RectangleF(0.5f,0.5f,Width-1,Height-1),Radius*DeviceDpi/96f);
        using var brush=new SolidBrush(BackColor);
        using var pen=new Pen(BorderColor);
        e.Graphics.FillPath(brush,path);e.Graphics.DrawPath(pen,path);
    }
}

public sealed class SoftButton : Button
{
    public bool Accent {get;set;}
    public bool Active {get;set;}
    bool hovered;
    public SoftButton()
    {
        FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Cursor=Cursors.Hand;
        SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);
        Height=38;Margin=new(0,0,8,0);Padding=new(10,0,10,0);BackColor=Theme.Background;ForeColor=Theme.Text;
    }
    protected override void OnMouseEnter(EventArgs e){hovered=true;Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(EventArgs e){hovered=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnEnabledChanged(EventArgs e){base.OnEnabledChanged(e);Invalidate();}
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent?.BackColor??Theme.Background);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
        Color fill=!Enabled?Color.FromArgb(37,37,44):Accent?(hovered?Color.FromArgb(213,132,68):Theme.Accent):Active?Theme.AccentSoft:hovered?Color.FromArgb(45,45,54):Color.FromArgb(34,34,41);
        Color fg=!Enabled?Color.FromArgb(119,123,133):Accent?Color.FromArgb(34,24,18):Active?Color.FromArgb(255,173,103):Theme.Text;
        using var path=Theme.Rounded(new RectangleF(0.5f,0.5f,Width-1,Height-1),5*DeviceDpi/96f);
        using var brush=new SolidBrush(fill);e.Graphics.FillPath(brush,path);
        using var pen=new Pen(Accent||Active||hovered?Theme.Accent:Theme.Border);e.Graphics.DrawPath(pen,path);
        TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,fg,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.NoPrefix);
        if(Focused && ShowFocusCues)ControlPaint.DrawFocusRectangle(e.Graphics,Rectangle.Inflate(ClientRectangle,-5,-5),fg,fill);
    }
}

public static class AppDialog
{
    // Own buttons, unlike native MessageBox, follow the selected app language,
    // not the operating system language. The main window stays scroll-free.
    public static bool Show(Form owner,UiLocale locale,string title,string message,bool confirm=false,string? copyText=null,string? confirmText=null)
    {
        using var form=new Form {Text=title,Font=Theme.Font(),BackColor=Theme.Background,ForeColor=Theme.Text,
            StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,
            MaximizeBox=false,MinimizeBox=false,ShowInTaskbar=false,AutoScaleMode=AutoScaleMode.Dpi};
        int width=620;
        int textHeight=TextRenderer.MeasureText(message,form.Font,new Size(width-48,int.MaxValue),TextFormatFlags.WordBreak|TextFormatFlags.NoPrefix).Height;
        form.ClientSize=new(width,Math.Clamp(textHeight+104,210,700));
        var layout=new TableLayoutPanel {Dock=DockStyle.Fill,Padding=new(20),ColumnCount=1,RowCount=2};
        layout.RowStyles.Add(new(SizeType.Percent,100));layout.RowStyles.Add(new(SizeType.Absolute,38));
        var text=new TextBox {ReadOnly=true,Multiline=true,BorderStyle=BorderStyle.None,BackColor=Theme.Background,ForeColor=Theme.Text,
            Dock=DockStyle.Fill,Text=message,ScrollBars=textHeight>580?ScrollBars.Vertical:ScrollBars.None,TabStop=false};
        var actions=new FlowLayoutPanel {Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft,WrapContents=false};
        var ok=new SoftButton {Text=confirm?(confirmText??locale.Text("Replace save","Заменить сейв")):locale.Text("Close","Закрыть"),Width=150,Accent=confirm,
            DialogResult=DialogResult.OK};
        if(confirm)
        {
            var cancel=new SoftButton {Text=locale.Text("Cancel","Отмена"),Width=105,DialogResult=DialogResult.Cancel};
            actions.Controls.Add(cancel);form.CancelButton=cancel;
            // Enter defaults to cancel, especially for irreversible no-backup mode.
            form.AcceptButton=cancel;
        }
        else {form.AcceptButton=ok;form.CancelButton=ok;}
        actions.Controls.Add(ok);
        if(copyText!=null)
        {
            var copy=new SoftButton {Text=locale.Text("Copy details","Копировать"),Width=130};
            copy.Click+=(_,_)=>{try{Clipboard.SetText(copyText);}catch(System.Runtime.InteropServices.ExternalException){copy.Text=locale.Text("Try again","Повторить");}};
            actions.Controls.Add(copy);
        }
        layout.Controls.Add(text,0,0);layout.Controls.Add(actions,0,1);form.Controls.Add(layout);
        return form.ShowDialog(owner)==DialogResult.OK;
    }
}
