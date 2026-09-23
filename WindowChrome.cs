using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LocalIconStudio {
// A shaped native Win10 caption falls back to the classic light frame.
// Keep Windows move/resize semantics, but draw the entire frame in the app.
class StudioWindow : Form {
    bool chrome, resizable, updating, restoringBounds;
    Rectangle normalBounds;
    FormWindowState previousState;
    int edge, captionHeight, creditHeight;
    LinkLabel creatorCredit;
    float scale=1;
    CaptionButton minimize, maximize, close;
    [DllImport("user32.dll")] static extern IntPtr GetSystemMenu(IntPtr window,bool reset);
    [DllImport("user32.dll")] static extern int TrackPopupMenu(IntPtr menu,uint flags,int x,int y,int reserved,IntPtr window,IntPtr rect);
    [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr window,int message,IntPtr wParam,IntPtr lParam);
    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr window,int attribute,ref int value,int size);
    [StructLayout(LayoutKind.Sequential)] struct MinMaxInfo { public Point Reserved,MaxSize,MaxPosition,MinTrackSize,MaxTrackSize; }

    static Icon appIcon;
    public StudioWindow(){
        DoubleBuffered=true;SetStyle(ControlStyles.ResizeRedraw,true);
        try{if(appIcon==null)appIcon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);Icon=appIcon;}catch{ShowIcon=false;}
    }
    protected override CreateParams CreateParams {
        get {
            var cp=base.CreateParams;
            if(chrome&&TopLevel){
                cp.Style&=~0x00C00000; // no native caption or border
                cp.Style|=0x00080000; // system menu / Alt+F4
                if(resizable)cp.Style|=0x00040000;
                if(MinimizeBox)cp.Style|=0x00020000;
                if(MaximizeBox)cp.Style|=0x00010000;
            }
            return cp;
        }
    }
    protected override void OnLoad(EventArgs e){
        if(TopLevel&&!chrome){
            var content=ClientSize;var inset=Padding;
            resizable=FormBorderStyle==FormBorderStyle.Sizable||FormBorderStyle==FormBorderStyle.SizableToolWindow;
            using(var g=CreateGraphics())scale=g.DpiX/96f;
            edge=Math.Max(4,(int)Math.Round(4*scale));captionHeight=(int)Math.Round(32*scale);
            creditHeight=(int)Math.Round(26*scale);
            SuspendLayout();chrome=true;FormBorderStyle=FormBorderStyle.None;
            Padding=new Padding(inset.Left+edge,inset.Top+captionHeight+edge,inset.Right+edge,inset.Bottom+edge+creditHeight);
            ClientSize=new Size(content.Width+edge*2,content.Height+captionHeight+edge*2+creditHeight);
            minimize=new CaptionButton(this,0);maximize=new CaptionButton(this,1);close=new CaptionButton(this,2);
            minimize.Click+=(s,a)=>SendMessage(Handle,0x112,new IntPtr(0xF020),IntPtr.Zero);
            maximize.Click+=(s,a)=>ToggleMaximize();close.Click+=(s,a)=>Close();
            HideLegacyCredits(this);
            creatorCredit=new LinkLabel{Text="Created by Jona Fynn Schlegelmilch",AutoSize=false,TextAlign=ContentAlignment.MiddleLeft,LinkColor=Theme.Muted,ActiveLinkColor=Theme.Text,VisitedLinkColor=Theme.Muted,BackColor=Theme.Background,LinkBehavior=LinkBehavior.HoverUnderline,Font=new Font(Theme.UiFont,8),AccessibleName="Created by Jona Fynn Schlegelmilch",TabStop=true};
            creatorCredit.LinkClicked+=(s,a)=>{try{System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://www.linkedin.com/in/jonaschlegelmilch/"){UseShellExecute=true});}catch{}};
            Controls.AddRange(new Control[]{minimize,maximize,close,creatorCredit});
            ResumeLayout(true);RefreshChrome();
        }
        base.OnLoad(e);
    }
    static void HideLegacyCredits(Control parent){foreach(Control child in parent.Controls){if(child is LinkLabel&&child.Text.IndexOf("created by Jona Fynn Schlegelmilch",StringComparison.OrdinalIgnoreCase)>=0)child.Visible=false;else HideLegacyCredits(child);}}
    internal void ToggleMaximize(){if(MaximizeBox)SendMessage(Handle,0x112,new IntPtr(WindowState==FormWindowState.Maximized?0xF120:0xF030),IntPtr.Zero);}
    internal void RefreshChrome(){
        if(!chrome||!TopLevel||IsDisposed||updating)return;
        updating=true;
        try {
            if(close!=null){
                int width=(int)Math.Round(42*scale),x=ClientSize.Width-edge;
                close.SetBounds(x-width,edge,width,captionHeight);x-=width;
                maximize.Visible=MaximizeBox;minimize.Visible=MinimizeBox;
                if(MaximizeBox){maximize.SetBounds(x-width,edge,width,captionHeight);x-=width;}
                if(MinimizeBox)minimize.SetBounds(x-width,edge,width,captionHeight);
                close.BringToFront();maximize.BringToFront();minimize.BringToFront();
                minimize.AccessibleName=L.English?"Minimize":"Minimieren";
                maximize.AccessibleName=WindowState==FormWindowState.Maximized?(L.English?"Restore":"Wiederherstellen"):(L.English?"Maximize":"Maximieren");
                close.AccessibleName=L.English?"Close":"Schließen";
                close.Invalidate();maximize.Invalidate();minimize.Invalidate();
            }
            if(creatorCredit!=null){creatorCredit.SetBounds(edge+(int)(12*scale),ClientSize.Height-edge-creditHeight,Math.Max(1,ClientSize.Width-edge*2-(int)(24*scale)),creditHeight);creatorCredit.BringToFront();}
            bool native=false;
            if(IsHandleCreated)try{int preference=WindowState==FormWindowState.Normal?2:1;native=DwmSetWindowAttribute(Handle,33,ref preference,4)==0;}catch{}
            Region next=null;
            if(!native&&WindowState==FormWindowState.Normal&&Width>0&&Height>0)
                using(var path=Theme.Rounded(new RectangleF(0,0,Width,Height),10*scale))next=new Region(path);
            var old=Region;Region=next;if(old!=null)old.Dispose();Invalidate();
        } finally {updating=false;}
    }
    protected override void OnResize(EventArgs e){
        if(chrome&&!restoringBounds){
            var state=WindowState;
            if(state==FormWindowState.Normal)normalBounds=Bounds;
            previousState=state;
        }
        base.OnResize(e);RefreshChrome();
    }
    protected override void OnLocationChanged(EventArgs e){
        base.OnLocationChanged(e);
        if(chrome&&!restoringBounds&&previousState==FormWindowState.Normal&&WindowState==FormWindowState.Normal)normalBounds=Bounds;
    }
    protected override void OnShown(EventArgs e){base.OnShown(e);RefreshChrome();}
    protected override void OnLayout(LayoutEventArgs e){base.OnLayout(e);RefreshChrome();}
    protected override void OnTextChanged(EventArgs e){base.OnTextChanged(e);Invalidate();}
    protected override void OnActivated(EventArgs e){base.OnActivated(e);Invalidate();}
    protected override void OnDeactivate(EventArgs e){base.OnDeactivate(e);Invalidate();}
    protected override void OnPaint(PaintEventArgs e){
        base.OnPaint(e);if(!chrome)return;
        var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
        using(var fill=new SolidBrush(Theme.Background))g.FillRectangle(fill,0,0,Width,captionHeight+edge);
        int left=(int)(14*scale),iconSize=(int)(14*scale);
        if(ShowIcon&&Icon!=null){g.DrawIcon(Icon,new Rectangle(left,edge+(captionHeight-iconSize)/2,iconSize,iconSize));left+=iconSize+(int)(8*scale);}
        int right=close==null?Width:Math.Min(close.Left,MinimizeBox?minimize.Left:(MaximizeBox?maximize.Left:close.Left));
        using(var font=new Font(Theme.UiFont,9))TextRenderer.DrawText(g,Text,font,new Rectangle(left,edge,Math.Max(1,right-left-8),captionHeight),Theme.Muted,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
        using(var path=Theme.Rounded(new RectangleF(.5f,.5f,Width-1,Height-1),WindowState==FormWindowState.Normal?10*scale:0))
        using(var pen=new Pen(Theme.Line))g.DrawPath(pen,path);
    }
    void SystemMenu(Point position){
        int command=TrackPopupMenu(GetSystemMenu(Handle,false),0x100,position.X,position.Y,0,Handle,IntPtr.Zero);
        if(command!=0)SendMessage(Handle,0x112,new IntPtr(command),IntPtr.Zero);
    }
    protected override bool ProcessCmdKey(ref Message msg,Keys keyData){
        if(chrome&&keyData==(Keys.Alt|Keys.Space)){SystemMenu(PointToScreen(new Point(edge,captionHeight+edge)));return true;}
        return base.ProcessCmdKey(ref msg,keyData);
    }
    protected override void WndProc(ref Message m){
        if(chrome&&TopLevel){
            if(m.Msg==0x47&&!restoringBounds&&previousState!=FormWindowState.Normal&&!normalBounds.IsEmpty){
                // WinForms applies native frame metrics after the restore resize
                // event. Correct at the end of WM_WINDOWPOSCHANGED instead.
                var saved=normalBounds;restoringBounds=true;
                try{base.WndProc(ref m);if(WindowState==FormWindowState.Normal)Bounds=saved;previousState=WindowState;}
                finally{restoringBounds=false;}
                return;
            }
            if(m.Msg==0x83){m.Result=IntPtr.Zero;return;} // WM_NCCALCSIZE: all pixels belong to the app
            if(m.Msg==0x85){m.Result=IntPtr.Zero;return;} // no classic nonclient paint
            if(m.Msg==0x86){m.Result=new IntPtr(1);Invalidate();return;}
            if(m.Msg==0x24){
                base.WndProc(ref m);
                var info=(MinMaxInfo)Marshal.PtrToStructure(m.LParam,typeof(MinMaxInfo));var screen=Screen.FromHandle(Handle);
                info.MaxPosition=new Point(screen.WorkingArea.Left-screen.Bounds.Left,screen.WorkingArea.Top-screen.Bounds.Top);
                info.MaxSize=new Point(screen.WorkingArea.Width,screen.WorkingArea.Height);
                Marshal.StructureToPtr(info,m.LParam,false);return;
            }
            if(m.Msg==0x84){
                long packed=m.LParam.ToInt64();var point=PointToClient(new Point(unchecked((short)packed),unchecked((short)(packed>>16))));
                // Caption controls are client controls, never draggable/nonclient pixels.
                if((minimize!=null&&minimize.Visible&&minimize.Bounds.Contains(point))||
                   (maximize!=null&&maximize.Visible&&maximize.Bounds.Contains(point))||
                   (close!=null&&close.Visible&&close.Bounds.Contains(point))){m.Result=new IntPtr(1);return;}
                int result=1;
                if(resizable&&WindowState==FormWindowState.Normal){
                    int grip=Math.Max(edge,(int)(6*scale));bool l=point.X<grip,r=point.X>=ClientSize.Width-grip,t=point.Y<grip,b=point.Y>=ClientSize.Height-grip;
                    result=t?(l?13:r?14:12):b?(l?16:r?17:15):l?10:r?11:1;
                }
                if(result==1&&point.Y<captionHeight+edge)result=2;
                m.Result=new IntPtr(result);return;
            }
            if(m.Msg==0xA5&&m.WParam.ToInt32()==2){SystemMenu(Cursor.Position);m.Result=IntPtr.Zero;return;}
            if(m.Msg==0xA3&&m.WParam.ToInt32()==2&&!MaximizeBox){m.Result=IntPtr.Zero;return;}
        }
        base.WndProc(ref m);
    }
    sealed class CaptionButton : Button {
        readonly StudioWindow window;readonly int kind;bool hover;
        public CaptionButton(StudioWindow owner,int type){window=owner;kind=type;FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;TabStop=false;AccessibleRole=AccessibleRole.PushButton;SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);}
        protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
        protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
        protected override void OnPaint(PaintEventArgs e){
            var g=e.Graphics;g.Clear(hover?(kind==2?Color.FromArgb(190,48,57):Theme.Raised):Theme.Background);
            g.SmoothingMode=SmoothingMode.AntiAlias;float s=g.DpiX/96f,cx=Width/2f,cy=Height/2f,r=4*s;
            using(var pen=new Pen(hover&&kind==2?Color.White:Theme.Text,1.15f*s)){
                if(kind==0)g.DrawLine(pen,cx-r,cy+2*s,cx+r,cy+2*s);
                else if(kind==2){g.DrawLine(pen,cx-r,cy-r,cx+r,cy+r);g.DrawLine(pen,cx+r,cy-r,cx-r,cy+r);}
                else if(window.WindowState==FormWindowState.Maximized){g.DrawLines(pen,new[]{new PointF(cx-r+2*s,cy-r),new PointF(cx+r,cy-r),new PointF(cx+r,cy+r-2*s)});g.DrawRectangle(pen,cx-r,cy-r+2*s,6*s,6*s);}
                else g.DrawRectangle(pen,cx-r,cy-r,8*s,8*s);
            }
        }
    }
}
}
