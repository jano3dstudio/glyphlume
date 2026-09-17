using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace LocalIconStudio {
class StyleOptions {
    public string Background="#0D0F11",Surface="#15181B",Accent="#3CFF91",Text="#E7EBEE",Arrows="#3ADACC";
    public string Frame="solid",UiFont="Bahnschrift",TitleFont="Eurostar",Logo="";
    public int Radius=7;public bool ShowLogo=true;
    public int[] CustomColors=Enumerable.Repeat(0xFFFFFF,16).ToArray();
    public StyleOptions Clone(){var json=new JavaScriptSerializer();return json.Deserialize<StyleOptions>(json.Serialize(this));}
}
static class Styles {
    public static StyleOptions Current=new StyleOptions();
    public static string SettingsPath=Path.Combine(Program.Data,"style.json");
    public static Color ColorValue(string value){if(value==null||value.Length!=7||value[0]!='#')throw new FormatException("Invalid color");return ColorTranslator.FromHtml(value);}
    public static string LogoPath(StyleOptions style){return String.IsNullOrEmpty(style.Logo)?Path.Combine(Program.Root,"assets","Glyphlume.png"):Path.IsPathRooted(style.Logo)?style.Logo:Path.Combine(Path.GetDirectoryName(SettingsPath),style.Logo);}
    public static void Validate(StyleOptions value){foreach(string color in new[]{value.Background,value.Surface,value.Accent,value.Text,value.Arrows})ColorValue(color);if(value.Radius<0||value.Radius>18||!new[]{"solid","dashed","none"}.Contains(value.Frame)||String.IsNullOrWhiteSpace(value.UiFont)||String.IsNullOrWhiteSpace(value.TitleFont))throw new FormatException("Invalid style");}
    public static void Load(){try{var value=File.Exists(SettingsPath)?new JavaScriptSerializer().Deserialize<StyleOptions>(File.ReadAllText(SettingsPath)):new StyleOptions();Validate(value);Apply(value);}catch{Apply(new StyleOptions());}}
    public static void Save(StyleOptions value){
        Validate(value);var saved=value.Clone();saved.Arrows=saved.Accent;string folder=Path.GetDirectoryName(SettingsPath);Directory.CreateDirectory(folder);
        if(!String.IsNullOrEmpty(saved.Logo)&&Path.IsPathRooted(saved.Logo)){
            string branding=Path.Combine(folder,"branding");Directory.CreateDirectory(branding);string name="logo-"+Guid.NewGuid().ToString("N")+".png";
            using(var image=Image.FromFile(saved.Logo)){if(image.Width>8192||image.Height>8192)throw new IOException(L.T("Bitte ein Bild bis maximal 8192 × 8192 px laden."));image.Save(Path.Combine(branding,name),ImageFormat.Png);}saved.Logo=Path.Combine("branding",name);
        }
        string temp=SettingsPath+".tmp";try{File.WriteAllText(temp,new JavaScriptSerializer().Serialize(saved));if(File.Exists(SettingsPath))File.Replace(temp,SettingsPath,null);else File.Move(temp,SettingsPath);}finally{if(File.Exists(temp))File.Delete(temp);}
        Apply(saved);
    }
    static Color[] Palette(){return new[]{Theme.Background,Theme.Surface,Theme.Raised,Theme.Text,Theme.Muted,Theme.Line,Theme.Turquoise,Theme.Accent};}
    class ControlLook {public Control Control;public Color Back,Fore;public string Font;public float Size;public FontStyle Style;}
    static void Remember(Control c,List<ControlLook> values){values.Add(new ControlLook{Control=c,Back=c.BackColor,Fore=c.ForeColor,Font=c.Font.Name,Size=c.Font.Size,Style=c.Font.Style});foreach(Control child in c.Controls)Remember(child,values);}
    public static void Apply(StyleOptions value){
        Validate(value);var old=Palette();string oldFont=Theme.UiFont,oldTitle=Theme.DisplayFont;var controls=new List<ControlLook>();var forms=Application.OpenForms.Cast<Form>().ToArray();foreach(var form in forms){Remember(form,controls);form.SuspendLayout();}
        Current=value.Clone();Current.Arrows=Current.Accent;Theme.Background=ColorValue(value.Background);Theme.Surface=ColorValue(value.Surface);Theme.Text=ColorValue(value.Text);Theme.Accent=ColorValue(value.Accent);Theme.Turquoise=Theme.Accent;
        Theme.Raised=Theme.Blend(Theme.Surface,Theme.Text,.035f);Theme.Muted=Theme.Blend(Theme.Background,Theme.Text,.56f);Theme.Line=Theme.Blend(Theme.Surface,Theme.Text,.11f);Theme.Radius=value.Radius;Theme.Frame=value.Frame;
        Theme.UiFont=Theme.AvailableFont(value.UiFont,"Segoe UI");Theme.DisplayFont=Theme.AvailableFont(value.TitleFont,Theme.UiFont);var next=Palette();
        foreach(var look in controls){var c=look.Control;if(c.IsDisposed)continue;int back=Array.FindIndex(old,v=>v.ToArgb()==look.Back.ToArgb()),fore=Array.FindIndex(old,v=>v.ToArgb()==look.Fore.ToArgb());if(back>=0)c.BackColor=next[back];if(fore>=0)c.ForeColor=next[fore];if(look.Font==oldFont||look.Font==oldTitle)c.Font=new Font(look.Font==oldTitle&&oldTitle!=oldFont?Theme.DisplayFont:Theme.UiFont,look.Size,look.Style);if(c.Region!=null&&!(c is Form))Theme.RoundControl(c);c.Invalidate();}
        foreach(var form in forms){var studio=form as Studio;if(studio!=null)studio.RefreshLook();form.ResumeLayout(true);Theme.TitleBar(form);}
    }
}
class StyleChoice : DarkButton {
    readonly ContextMenuStrip menu=new ContextMenuStrip();readonly string[] labels;int selected;
    public event Action<int> Changed;
    public int SelectedIndex {get{return selected;}set{selected=Math.Max(0,Math.Min(labels.Length-1,value));Text=labels[selected]+"   ▾";}}
    public StyleChoice(string[] items){labels=items;Height=34;Dock=DockStyle.Top;Margin=Padding.Empty;menu.ShowImageMargin=false;menu.ShowCheckMargin=false;menu.Renderer=new ToolStripProfessionalRenderer(new SizeDropdown.MenuColors());
        for(int i=0;i<items.Length;i++){int index=i;var item=new ToolStripMenuItem(items[i].Replace("&","&&")){BackColor=Theme.Surface,ForeColor=Theme.Text};item.Click+=(s,e)=>{SelectedIndex=index;if(Changed!=null)Changed(index);};menu.Items.Add(item);}
        Click+=(s,e)=>{menu.Font=Font;menu.BackColor=Theme.Surface;foreach(ToolStripItem item in menu.Items){item.BackColor=Theme.Surface;item.ForeColor=Theme.Text;item.AutoSize=false;item.Size=new Size(Math.Max(Width-4,180),Height);}menu.Show(this,new Point(0,Height));};SelectedIndex=0;
    }
    protected override void Dispose(bool disposing){if(disposing)menu.Dispose();base.Dispose(disposing);}
}
class ColorSwatch : Button {
    public Color Value;public event Action<Color> Picked;
    public Func<int[]> ReadCustomColors;public Action<int[]> WriteCustomColors;
    public string ColorName;
    public ColorSwatch(){Height=36;Dock=DockStyle.Top;FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Cursor=Cursors.Hand;Margin=Padding.Empty;Click+=(s,e)=>{using(var picker=new StudioColorPicker(Value,ReadCustomColors==null?null:ReadCustomColors(),ColorName)){if(picker.ShowDialog(this)==DialogResult.OK){if(WriteCustomColors!=null)WriteCustomColors(picker.CustomColors);Value=picker.SelectedColor;Invalidate();if(Picked!=null)Picked(Value);}}};}
    protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(Theme.Surface);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var path=Theme.Rounded(new RectangleF(2,2,Width-4,Height-4),Theme.Radius)){using(var fill=new SolidBrush(Value))e.Graphics.FillPath(fill,path);using(var pen=Theme.Border(Theme.Line))e.Graphics.DrawPath(pen,path);}var ink=Value.GetBrightness()>.55?Color.Black:Color.White;TextRenderer.DrawText(e.Graphics,ColorTranslator.ToHtml(Value),Font,ClientRectangle,ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);}
}
class StylePanel : StudioWindow {
    readonly StyleOptions original;StyleOptions draft;bool committed;StyleChoice preset,frame,corners,uiFont,titleFont;readonly List<ColorSwatch> swatches=new List<ColorSwatch>();PictureBox logoPreview;CheckBox showLogo;Bitmap previewImage;string[] fonts;
    static string Tr(string de,string en){return L.English?en:de;}
    public StylePanel(){
        original=Styles.Current.Clone();draft=original.Clone();SuspendLayout();Text=Tr("Style · Dein Studio. Dein Look.","Style · Your studio. Your look.");Font=new Font(Theme.UiFont,10);BackColor=Theme.Surface;ForeColor=Theme.Text;
        AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;ClientSize=new Size(600,660);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;StartPosition=FormStartPosition.CenterParent;
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=9,Padding=new Padding(24),BackColor=Theme.Surface};root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));for(int i=0;i<9;i++)root.RowStyles.Add(new RowStyle(SizeType.AutoSize));Controls.Add(root);
        var heading=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=1,RowCount=2,Margin=new Padding(0,0,0,10)};heading.Controls.Add(new Label{Text=Tr("DEIN STUDIO. DEIN LOOK.","YOUR STUDIO. YOUR LOOK."),Font=new Font(Theme.UiFont,9),ForeColor=Theme.Muted,AutoSize=true,Margin=new Padding(0,0,0,8)},0,0);heading.Controls.Add(new Label{Text="LOOKS",Font=new Font(Theme.DisplayFont,24),ForeColor=Theme.Text,AutoSize=true,Margin=Padding.Empty},0,1);root.Controls.Add(heading,0,0);
        preset=new StyleChoice(new[]{Tr("Eigener Look","Custom look"),"GLYPHLUME Original","Arctic Cyan","Violet","Amber"});preset.Changed+=ChoosePreset;root.Controls.Add(Field(Tr("Farbpreset","Color preset"),preset),0,1);
        var sample=new TableLayoutPanel{Dock=DockStyle.Top,Height=44,ColumnCount=3,Padding=new Padding(10),BackColor=Theme.Background,Margin=new Padding(0,0,0,10)};for(int i=0;i<3;i++)sample.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,33.333f));sample.Controls.Add(new DarkButton{Text="ICON STUDIO",Dock=DockStyle.Fill,Margin=new Padding(0,0,6,0)},0,0);sample.Controls.Add(new DarkButton{Text=Tr("Vorschau","Preview"),Primary=true,Dock=DockStyle.Fill,Margin=new Padding(0,0,6,0)},1,0);sample.Controls.Add(new DarkButton{Text="Aa 123",Dock=DockStyle.Fill,Margin=Padding.Empty},2,0);root.Controls.Add(sample,0,2);
        var colors=Columns(4);string[] names={Tr("Akzent","Accent"),Tr("Hintergrund","Background"),Tr("Flächen","Surface"),"Text"};
        for(int i=0;i<4;i++){
            int index=i;var swatch=new ColorSwatch{ColorName=names[i],AccessibleName=names[i]};swatches.Add(swatch);
            swatch.ReadCustomColors=()=>draft.CustomColors==null?Enumerable.Repeat(0xFFFFFF,16).ToArray():(int[])draft.CustomColors.Clone();
            swatch.WriteCustomColors=values=>draft.CustomColors=(int[])values.Clone();
            swatch.Picked+=c=>SetColor(index,c);
            colors.Controls.Add(Field(names[i],swatch),i,0);
        }
        root.Controls.Add(colors,0,3);
        var options=Columns(2);frame=new StyleChoice(new[]{Tr("Durchgehend","Solid"),Tr("Gestrichelt","Dashed"),Tr("Ohne Rahmen","No frame")});frame.Changed+=i=>{draft.Frame=new[]{"solid","dashed","none"}[i];Preview();};corners=new StyleChoice(new[]{"0 px","4 px","7 px","12 px","18 px"});corners.Changed+=i=>{draft.Radius=new[]{0,4,7,12,18}[i];Preview();};options.Controls.Add(Field(Tr("Rahmen","Frame"),frame),0,0);options.Controls.Add(Field(Tr("Rundung","Corners"),corners),1,0);root.Controls.Add(options,0,4);
        using(var installed=new InstalledFontCollection())fonts=new[]{"Bahnschrift","Inter","Segoe UI","Eurostar","Consolas","Arial"}.Where(name=>installed.Families.Any(f=>f.Name==name)).ToArray();if(fonts.Length==0)fonts=new[]{"Segoe UI"};
        var typography=Columns(2);uiFont=new StyleChoice(fonts);titleFont=new StyleChoice(fonts);uiFont.Changed+=i=>{draft.UiFont=fonts[i];Preview();};titleFont.Changed+=i=>{draft.TitleFont=fonts[i];Preview();};typography.Controls.Add(Field(Tr("Schrift · Oberfläche","Font · Interface"),uiFont),0,0);typography.Controls.Add(Field(Tr("Schrift · Titel","Font · Title"),titleFont),1,0);root.Controls.Add(typography,0,5);
        var branding=Columns(2);logoPreview=new PictureBox{Height=70,MaximumSize=new Size(0,70),Dock=DockStyle.Top,SizeMode=PictureBoxSizeMode.Zoom,BackColor=Theme.Background,Margin=Padding.Empty};branding.Controls.Add(Field("Logo",logoPreview),0,0);var logoActions=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=1,RowCount=3,Margin=Padding.Empty};logoActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));logoActions.Controls.Add(ActionButton(Tr("Logo auswählen …","Choose logo …"),ChooseLogo));showLogo=new CheckBox{Text=Tr("Logo anzeigen","Show logo"),AutoSize=true,Margin=new Padding(0,8,0,0)};showLogo.CheckedChanged+=(s,e)=>{draft.ShowLogo=showLogo.Checked;Preview();};logoActions.Controls.Add(showLogo);logoActions.Controls.Add(ActionButton(Tr("Original-Logo","Original logo"),()=>{draft.Logo="";draft.ShowLogo=true;Preview();Sync();}));branding.Controls.Add(logoActions,1,0);root.Controls.Add(branding,0,6);
        root.Controls.Add(new Label{Text=Tr("Farbfeld anklicken zum Bearbeiten · Pfeile = Akzent","Click a color to edit · Arrows follow accent"),ForeColor=Theme.Muted,AutoSize=true,Margin=new Padding(0,10,0,12)},0,7);
        var actions=Columns(3);var reset=ActionButton(Tr("Standard","Reset"),()=>{draft=new StyleOptions();Preview();Sync();});var cancel=ActionButton(L.T("Abbrechen"),()=>Close());var apply=ActionButton(Tr("Übernehmen","Apply"),Commit);apply.Primary=true;actions.Controls.Add(reset,0,0);actions.Controls.Add(cancel,1,0);actions.Controls.Add(apply,2,0);root.Controls.Add(actions,0,8);AcceptButton=apply;CancelButton=cancel;
        Shown+=(s,e)=>{Theme.TitleBar(this);if(Owner!=null){var area=Screen.FromControl(Owner).WorkingArea;Location=new Point(Math.Max(area.Left,Math.Min(Owner.Right-Width,area.Right-Width)),Math.Max(area.Top,Math.Min(Owner.Top+36,area.Bottom-Height)));}};
        FormClosing+=(s,e)=>{if(!committed)Styles.Apply(original);};FormClosed+=(s,e)=>{if(previewImage!=null)previewImage.Dispose();};Sync();ResumeLayout(true);
    }
    static TableLayoutPanel Columns(int count){var panel=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=count,RowCount=1,Margin=Padding.Empty};for(int i=0;i<count;i++)panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100f/count));return panel;}
    static Control Field(string name,Control input){var panel=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=1,RowCount=2,Margin=new Padding(0,0,10,10)};panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));panel.Controls.Add(new Label{Text=name,AutoSize=true,ForeColor=Theme.Muted,Font=new Font(Theme.UiFont,9),Margin=new Padding(0,0,0,6)},0,0);panel.Controls.Add(input,0,1);return panel;}
    static DarkButton ActionButton(string text,Action action){var button=new DarkButton{Text=text,Height=34,Width=235,Dock=DockStyle.Top,Margin=new Padding(0,0,8,0)};button.Click+=(s,e)=>action();return button;}
    public static string Hex(Color color){return "#"+color.R.ToString("X2")+color.G.ToString("X2")+color.B.ToString("X2");}
    public static bool TryHex(string text,out Color color){color=Color.Empty;string value=(text??"").Trim();if(value.StartsWith("#"))value=value.Substring(1);int number;if(value.Length!=6||!Int32.TryParse(value,System.Globalization.NumberStyles.AllowHexSpecifier,System.Globalization.CultureInfo.InvariantCulture,out number))return false;color=Color.FromArgb(255,(number>>16)&255,(number>>8)&255,number&255);return true;}
    void SetColor(int index,Color color){string hex=Hex(color);if(index==0)draft.Accent=hex;else if(index==1)draft.Background=hex;else if(index==2)draft.Surface=hex;else draft.Text=hex;swatches[index].Value=color;swatches[index].Invalidate();preset.SelectedIndex=0;Preview();}
    void ChoosePreset(int index){if(index==0)return;string[][] colors={new[]{"#0D0F11","#15181B","#3CFF91","#E7EBEE","#3ADACC"},new[]{"#0A111B","#122434","#63D8FF","#E9F7FF","#63D8FF"},new[]{"#141020","#231C34","#BE91FF","#F1E9FF","#E8A0EF"},new[]{"#18130E","#292019","#FFBD63","#FFF1DF","#F5A76B"}};var p=colors[index-1];draft.Background=p[0];draft.Surface=p[1];draft.Accent=p[2];draft.Text=p[3];draft.Arrows=p[4];Preview();Sync();preset.SelectedIndex=index;}
    void Sync(){string[] colors={draft.Accent,draft.Background,draft.Surface,draft.Text};for(int i=0;i<swatches.Count;i++){swatches[i].Value=Styles.ColorValue(colors[i]);swatches[i].Invalidate();}frame.SelectedIndex=Array.IndexOf(new[]{"solid","dashed","none"},draft.Frame);corners.SelectedIndex=Array.IndexOf(new[]{0,4,7,12,18},draft.Radius);uiFont.SelectedIndex=Array.IndexOf(fonts,draft.UiFont);titleFont.SelectedIndex=Array.IndexOf(fonts,draft.TitleFont);if(showLogo.Checked!=draft.ShowLogo)showLogo.Checked=draft.ShowLogo;UpdateLogo();}
    void Preview(){draft.Arrows=draft.Accent;Styles.Apply(draft);UpdateLogo();}
    void UpdateLogo(){if(logoPreview==null)return;Bitmap next=null;string path=Styles.LogoPath(draft);if(draft.ShowLogo&&File.Exists(path))using(var source=Image.FromFile(path))next=new Bitmap(source);var old=previewImage;previewImage=next;logoPreview.Image=next;if(old!=null)old.Dispose();}
    public void LoadLogo(string path){using(var image=Image.FromFile(path)){if(image.Width>8192||image.Height>8192)throw new IOException(L.T("Bitte ein Bild bis maximal 8192 × 8192 px laden."));}draft.Logo=Path.GetFullPath(path);draft.ShowLogo=true;Preview();Sync();}
    void ChooseLogo(){using(var dialog=new OpenFileDialog{Title=Tr("Eigenes Logo auswählen","Choose your logo"),Filter="PNG / JPG / ICO|*.png;*.jpg;*.jpeg;*.ico;*.bmp"})if(dialog.ShowDialog(this)==DialogResult.OK)try{LoadLogo(dialog.FileName);}catch(Exception e){MessageBox.Show(this,e.Message,"Style");}}
    void Commit(){try{Styles.Save(draft);committed=true;DialogResult=DialogResult.OK;Close();}catch(Exception e){MessageBox.Show(this,e.Message,"Style");}}
}
}

namespace LocalIconStudio {
// Saturation/value field and hue strip share mouse, drag and keyboard behavior.
class ColorPlane : Control {
    public bool HueOnly;public double Hue,Saturation,Value=1;public event Action Changed;
    public ColorPlane(){SetStyle(ControlStyles.Selectable|ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);TabStop=true;Cursor=Cursors.Cross;}
    public static Color Hsv(double hue,double saturation,double value){double h=((hue%360)+360)%360/60,c=value*saturation,x=c*(1-Math.Abs(h%2-1)),m=value-c;double r=0,g=0,b=0;if(h<1){r=c;g=x;}else if(h<2){r=x;g=c;}else if(h<3){g=c;b=x;}else if(h<4){g=x;b=c;}else if(h<5){r=x;b=c;}else{r=c;b=x;}return Color.FromArgb((int)Math.Round((r+m)*255),(int)Math.Round((g+m)*255),(int)Math.Round((b+m)*255));}
    public void SetColor(Color color){double max=Math.Max(color.R,Math.Max(color.G,color.B))/255.0,min=Math.Min(color.R,Math.Min(color.G,color.B))/255.0;if(max>min)Hue=color.GetHue();Saturation=max==0?0:(max-min)/max;Value=max;Invalidate();}
    public void SelectAt(int x,int y){double width=Math.Max(1,Width-9),height=Math.Max(1,Height-9);if(HueOnly)Hue=Math.Max(0,Math.Min(1,(x-4)/width))*360;else{Saturation=Math.Max(0,Math.Min(1,(x-4)/width));Value=1-Math.Max(0,Math.Min(1,(y-4)/height));}Invalidate();if(Changed!=null)Changed();}
    protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Left){Focus();Capture=true;SelectAt(e.X,e.Y);}}
    protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(Capture&&e.Button==MouseButtons.Left)SelectAt(e.X,e.Y);}
    protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);if(e.Button==MouseButtons.Left)Capture=false;}
    protected override bool IsInputKey(Keys key){return (key&Keys.KeyCode)==Keys.Left||(key&Keys.KeyCode)==Keys.Right||(key&Keys.KeyCode)==Keys.Up||(key&Keys.KeyCode)==Keys.Down||base.IsInputKey(key);}
    protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);double step=e.Shift?.1:.01;bool handled=true;if(HueOnly){if(e.KeyCode==Keys.Left||e.KeyCode==Keys.Down)Hue=Math.Max(0,Hue-step*360);else if(e.KeyCode==Keys.Right||e.KeyCode==Keys.Up)Hue=Math.Min(360,Hue+step*360);else handled=false;}else{if(e.KeyCode==Keys.Left)Saturation=Math.Max(0,Saturation-step);else if(e.KeyCode==Keys.Right)Saturation=Math.Min(1,Saturation+step);else if(e.KeyCode==Keys.Up)Value=Math.Min(1,Value+step);else if(e.KeyCode==Keys.Down)Value=Math.Max(0,Value-step);else handled=false;}if(handled){e.Handled=true;Invalidate();if(Changed!=null)Changed();}}
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var g=e.Graphics;g.Clear(BackColor);if(Width<10||Height<10)return;var rect=new Rectangle(4,4,Width-9,Height-9);using(var clip=Theme.Rounded(rect,6)){var state=g.Save();g.SetClip(clip);if(HueOnly){using(var fill=new LinearGradientBrush(rect,Color.Red,Color.Red,0f)){fill.InterpolationColors=new ColorBlend{Colors=new[]{Color.Red,Color.Yellow,Color.Lime,Color.Cyan,Color.Blue,Color.Magenta,Color.Red},Positions=new[]{0f,1f/6,2f/6,3f/6,4f/6,5f/6,1f}};g.FillRectangle(fill,rect);}}else{using(var fill=new LinearGradientBrush(rect,Color.White,Hsv(Hue,1,1),0f))g.FillRectangle(fill,rect);using(var shade=new LinearGradientBrush(rect,Color.FromArgb(0,0,0,0),Color.Black,90f))g.FillRectangle(shade,rect);}g.Restore(state);}g.SmoothingMode=SmoothingMode.AntiAlias;float x=rect.Left+(float)(HueOnly?Hue/360:Saturation)*rect.Width,y=HueOnly?rect.Top+rect.Height/2:rect.Top+(float)(1-Value)*rect.Height;using(var dark=new Pen(Color.FromArgb(180,0,0,0),4))using(var light=new Pen(Color.White,2)){g.DrawEllipse(dark,x-6,y-6,12,12);g.DrawEllipse(light,x-6,y-6,12,12);}if(Focused)ControlPaint.DrawFocusRectangle(g,ClientRectangle,Theme.Text,BackColor);}
}
class ColorTile : Button {
    public Color Color;public bool Empty;public string Caption="";
    public ColorTile(){FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Cursor=Cursors.Hand;Dock=DockStyle.Fill;Margin=new Padding(0,0,6,6);}
    protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(BackColor);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var path=Theme.Rounded(new RectangleF(1,1,Width-3,Height-3),6)){using(var brush=new SolidBrush(Empty?Theme.Raised:Color))e.Graphics.FillPath(brush,path);using(var pen=new Pen(Theme.Line))e.Graphics.DrawPath(pen,path);}if(Caption.Length>0)TextRenderer.DrawText(e.Graphics,Caption,Font,ClientRectangle,Color.GetBrightness()>.5?Color.Black:Color.White,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);if(Focused)ControlPaint.DrawFocusRectangle(e.Graphics,ClientRectangle);}
}
class StudioColorPicker : StudioWindow {
    readonly ColorPlane plane,hue;readonly TextBox hex;readonly Label message;readonly ColorTile before,after;readonly DarkButton use,remember;readonly TableLayoutPanel paletteGrid;readonly List<int> saved=new List<int>();readonly ToolTip tips=new ToolTip();bool syncing;
    public Color SelectedColor{get;private set;}
    public int[] CustomColors{get{return saved.Concat(Enumerable.Repeat(-1,16-saved.Count)).ToArray();}}
    static string Tr(string de,string en){return L.English?en:de;}
    public StudioColorPicker(Color color,int[] custom,string name){
        SuspendLayout();AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;ClientSize=new Size(460,620);Font=new Font(Theme.UiFont,10);BackColor=Theme.Surface;ForeColor=Theme.Text;Text=Tr("Farbe wählen","Choose color");FormBorderStyle=FormBorderStyle.FixedDialog;MinimizeBox=MaximizeBox=false;StartPosition=FormStartPosition.CenterParent;
        if(custom!=null&&!custom.All(v=>v==0xFFFFFF))saved.AddRange(custom.Where(v=>v>=0&&v<=0xFFFFFF).Distinct().Take(16));
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(22),ColumnCount=1,RowCount=11};root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));for(int i=0;i<11;i++)root.RowStyles.Add(new RowStyle(i==1?SizeType.Percent:SizeType.AutoSize,i==1?100:0));
        root.Controls.Add(new Label{Text=(name??Tr("Deine Farbe","Your color")).ToUpperInvariant(),Font=new Font(Theme.DisplayFont,20),AutoSize=true,Margin=new Padding(0,0,0,12)},0,0);
        plane=new ColorPlane{Dock=DockStyle.Fill,MinimumSize=new Size(0,140),BackColor=Theme.Surface,Margin=new Padding(0,0,0,5),AccessibleName=Tr("Sättigung und Helligkeit","Saturation and brightness")};root.Controls.Add(plane,0,1);
        hue=new ColorPlane{HueOnly=true,Dock=DockStyle.Top,Height=28,BackColor=Theme.Surface,Margin=new Padding(0,0,0,10),AccessibleName=Tr("Farbton","Hue")};root.Controls.Add(hue,0,2);
        var compare=Row(2);before=new ColorTile{Color=color,Caption=Tr("Bisher","Before"),Height=34,Dock=DockStyle.Top};after=new ColorTile{Color=color,Caption=Tr("Neu","New"),Height=34,Dock=DockStyle.Top};compare.Controls.Add(before,0,0);compare.Controls.Add(after,1,0);root.Controls.Add(compare,0,3);before.Click+=(s,e)=>SetColor(color);tips.SetToolTip(before,Tr("Ursprüngliche Farbe wiederherstellen","Restore original color"));
        root.Controls.Add(Label(Tr("HEX-FARBCODE","HEX COLOR CODE")),0,4);
        var code=Row(3);code.ColumnStyles.Clear();code.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,40));code.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,30));code.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,30));hex=new TextBox{Dock=DockStyle.Top,BackColor=Theme.Background,ForeColor=Theme.Text,BorderStyle=BorderStyle.FixedSingle,Font=new Font(Theme.UiFont,13),TextAlign=HorizontalAlignment.Center,MaxLength=16,Margin=new Padding(0,4,8,0),AccessibleName=Tr("HEX-Farbcode","HEX color code")};code.Controls.Add(hex,0,0);code.Controls.Add(Button(Tr("Kopieren","Copy"),Copy),1,0);code.Controls.Add(Button(Tr("Einfügen","Paste"),Paste),2,0);root.Controls.Add(code,0,5);
        message=new Label{AutoSize=true,ForeColor=Theme.Muted,Margin=new Padding(0,4,0,8)};root.Controls.Add(message,0,6);
        var paletteHeading=Row(2);paletteHeading.Controls.Add(Label(Tr("GESPEICHERTE FARBEN","SAVED COLORS")),0,0);remember=Button(Tr("＋ Farbe merken","＋ Save color"),SaveColor);paletteHeading.Controls.Add(remember,1,0);root.Controls.Add(paletteHeading,0,7);
        paletteGrid=Row(8);paletteGrid.RowCount=2;paletteGrid.Height=68;paletteGrid.AutoSize=false;paletteGrid.RowStyles.Add(new RowStyle(SizeType.Percent,50));paletteGrid.RowStyles.Add(new RowStyle(SizeType.Percent,50));root.Controls.Add(paletteGrid,0,8);
        root.Controls.Add(new Label{Text=Tr("Farben bleiben für die nächsten Farbfelder verfügbar.","Saved colors are available in every color field."),AutoSize=true,ForeColor=Theme.Muted,Font=new Font(Theme.UiFont,9),Margin=new Padding(0,4,0,14)},0,9);
        var actions=Row(2);var cancel=Button(L.T("Abbrechen"),()=>Close());use=Button(Tr("Farbe übernehmen","Use color"),()=>{DialogResult=DialogResult.OK;Close();});use.Primary=true;actions.Controls.Add(cancel,0,0);actions.Controls.Add(use,1,0);root.Controls.Add(actions,0,10);CancelButton=cancel;AcceptButton=use;
        hex.TextChanged+=(s,e)=>{if(syncing)return;Color parsed;if(StylePanel.TryHex(hex.Text,out parsed)){SetColor(parsed,false);Validity(true);}else Validity(false);};
        plane.Changed+=()=>{SelectedColor=ColorPlane.Hsv(plane.Hue,plane.Saturation,plane.Value);UpdateCode();};hue.Changed+=()=>{plane.Hue=hue.Hue;plane.Invalidate();SelectedColor=ColorPlane.Hsv(plane.Hue,plane.Saturation,plane.Value);UpdateCode();};
        Controls.Add(root);SetColor(color);RefreshPalette();Shown+=(s,e)=>Theme.TitleBar(this);FormClosed+=(s,e)=>tips.Dispose();ResumeLayout(true);
    }
    static TableLayoutPanel Row(int count){var row=new TableLayoutPanel{ColumnCount=count,RowCount=1,Dock=DockStyle.Top,AutoSize=true,Margin=Padding.Empty};for(int i=0;i<count;i++)row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100f/count));return row;}
    static Label Label(string text){return new Label{Text=text,AutoSize=true,ForeColor=Theme.Muted,Font=new Font(Theme.UiFont,9),Margin=new Padding(0,8,0,8)};}
    static DarkButton Button(string text,Action action){var button=new DarkButton{Text=text,Height=34,Dock=DockStyle.Top,Margin=new Padding(0,0,6,0),Font=new Font(Theme.UiFont,9)};button.Click+=(s,e)=>action();return button;}
    public void SetColor(Color color){SetColor(color,true);}
    void SetColor(Color color,bool updateHex){SelectedColor=color;plane.SetColor(color);hue.Hue=plane.Hue;hue.Invalidate();after.Color=color;after.Invalidate();if(updateHex)UpdateCode();}
    void UpdateCode(){syncing=true;hex.Text=StylePanel.Hex(SelectedColor);syncing=false;after.Color=SelectedColor;after.Invalidate();Validity(true);}
    void Validity(bool valid){use.Enabled=remember.Enabled=valid;hex.ForeColor=valid?Theme.Text:Color.Salmon;message.Text=valid?"RGB  "+SelectedColor.R+" / "+SelectedColor.G+" / "+SelectedColor.B:Tr("Bitte 6 HEX-Zeichen eingeben, z. B. #3CFF91.","Enter 6 HEX digits, for example #3CFF91.");}
    void Copy(){try{Clipboard.SetText(StylePanel.Hex(SelectedColor));message.Text=Tr("Farbcode kopiert.","Color code copied.");}catch{message.Text=Tr("Zwischenablage ist gerade belegt.","Clipboard is busy. Try again.");}}
    void Paste(){try{Color parsed;if(!Clipboard.ContainsText()||!StylePanel.TryHex(Clipboard.GetText(),out parsed)){message.Text=Tr("Kein gültiger HEX-Farbcode in der Zwischenablage.","Clipboard does not contain a valid HEX color code.");return;}SetColor(parsed);}catch{message.Text=Tr("Zwischenablage ist gerade belegt.","Clipboard is busy. Try again.");}}
    public void SaveColor(){int value=SelectedColor.R|(SelectedColor.G<<8)|(SelectedColor.B<<16);saved.Remove(value);saved.Insert(0,value);if(saved.Count>16)saved.RemoveAt(16);RefreshPalette();message.Text=Tr("Farbe gemerkt. Kachel anklicken zum Wiederverwenden.","Color saved. Click its tile to reuse it.");}
    public void SelectSaved(int index){int value=saved[index];SetColor(Color.FromArgb(value&255,(value>>8)&255,(value>>16)&255));}
    void RefreshPalette(){foreach(Control tile in paletteGrid.Controls.Cast<Control>().ToArray()){paletteGrid.Controls.Remove(tile);tile.Dispose();}for(int i=0;i<16;i++){int index=i;var tile=new ColorTile{Empty=i>=saved.Count,BackColor=Theme.Surface,Enabled=i<saved.Count};if(i<saved.Count){int value=saved[i];tile.Color=Color.FromArgb(value&255,(value>>8)&255,(value>>16)&255);tile.AccessibleName=StylePanel.Hex(tile.Color);tips.SetToolTip(tile,tile.AccessibleName);tile.Click+=(s,e)=>SelectSaved(index);}paletteGrid.Controls.Add(tile,i%8,i/8);}}
}
}
