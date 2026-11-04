using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AntigravityConnect {
static class Shape {
    public static GraphicsPath Round(RectangleF r,float radius) {
        float d=radius*2;var p=new GraphicsPath();
        p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);
        p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;
    }
}
sealed class SoftPanel : Panel {
    public SoftPanel(){DoubleBuffered=true;}
    protected override void OnPaintBackground(PaintEventArgs e) {
        e.Graphics.Clear(Parent==null?Color.White:Parent.BackColor);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
        using(var p=Shape.Round(new RectangleF(0,0,Width-1,Height-1),10*e.Graphics.DpiX/96))using(var b=new SolidBrush(BackColor))e.Graphics.FillPath(b,p);
    }
}
sealed class ActionButton : Button {
    public bool Primary;bool hover;
    public ActionButton(){FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Cursor=Cursors.Hand;SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);}
    protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnPaint(PaintEventArgs e) {
        var g=e.Graphics;float s=g.DpiX/96;g.Clear(Parent.BackColor);g.SmoothingMode=SmoothingMode.AntiAlias;
        Color fill=Primary?(Enabled?(hover?Color.FromArgb(85,62,194):Color.FromArgb(104,80,218)):Color.FromArgb(210,204,235)):(hover?Color.FromArgb(242,242,247):Color.White);
        using(var p=Shape.Round(new RectangleF(1,1,Width-3,Height-3),8*s))using(var b=new SolidBrush(fill)) {
            g.FillPath(b,p);
            bool focus=Focused&&ShowFocusCues;
            if(!Primary || focus)using(var pen=new Pen(focus?Color.FromArgb(156,139,230):Color.FromArgb(225,226,234),focus?2*s:s))g.DrawPath(pen,p);
        }
        TextRenderer.DrawText(g,Text,Font,ClientRectangle,!Enabled?Color.FromArgb(150,148,160):Primary?Color.White:Color.FromArgb(76,77,94),TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine);
    }
}
sealed class OptionRow : CheckBox {
    public string Hint;public bool Divider=true;
    public OptionRow(){AutoSize=false;Cursor=Cursors.Hand;FlatStyle=FlatStyle.Flat;SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);}
    protected override void OnCheckedChanged(EventArgs e){base.OnCheckedChanged(e);Invalidate();}
    protected override void OnGotFocus(EventArgs e){base.OnGotFocus(e);Invalidate();}
    protected override void OnLostFocus(EventArgs e){base.OnLostFocus(e);Invalidate();}
    protected override void OnPaint(PaintEventArgs e) {
        var g=e.Graphics;float s=g.DpiX/96;g.Clear(BackColor);g.SmoothingMode=SmoothingMode.AntiAlias;
        int right=(int)(60*s);var titleRect=new Rectangle(0,(int)(7*s),Width-right,(int)(21*s));
        TextRenderer.DrawText(g,Text,Font,titleRect,Enabled?Color.FromArgb(44,45,62):Color.Gray,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine);
        using(var small=new Font(Font.FontFamily,8.2f))TextRenderer.DrawText(g,L.Text(Hint),small,new Rectangle(0,(int)(29*s),Width-right,(int)(19*s)),Color.FromArgb(125,126,143),TextFormatFlags.Left|TextFormatFlags.SingleLine);
        var track=new RectangleF(Width-36*s,(Height-20*s)/2,34*s,20*s);
        Color fill=Checked?(Enabled?Color.FromArgb(104,80,218):Color.FromArgb(188,179,222)):Color.FromArgb(220,222,231);
        using(var p=Shape.Round(track,10*s))using(var b=new SolidBrush(fill))g.FillPath(b,p);
        using(var b=new SolidBrush(Color.White))g.FillEllipse(b,track.X+(Checked?17:3)*s,track.Y+3*s,14*s,14*s);
        if(Divider)using(var pen=new Pen(Color.FromArgb(240,240,245)))g.DrawLine(pen,0,Height-1,Width,Height-1);
        if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(g,new Rectangle(0,2,Width-1,Height-4),Color.FromArgb(122,102,209),BackColor);
    }
}
}
