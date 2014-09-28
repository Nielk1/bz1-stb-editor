using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.ComponentModel;

public class PictureBoxWithInterpolationMode : PictureBox
{
    [Category("Render")]
    public InterpolationMode InterpolationMode { get; set; }
    [Category("Render")]
    public SmoothingMode SmoothingMode { get; set; }
    [Category("Render")]
    public PixelOffsetMode PixelOffsetMode { get; set; }

    protected override void OnPaint(PaintEventArgs paintEventArgs)
    {
        paintEventArgs.Graphics.SmoothingMode = SmoothingMode;
        paintEventArgs.Graphics.InterpolationMode = InterpolationMode;
        paintEventArgs.Graphics.PixelOffsetMode = PixelOffsetMode;

        base.OnPaint(paintEventArgs);
    }
}