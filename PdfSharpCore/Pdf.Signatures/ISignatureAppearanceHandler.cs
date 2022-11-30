using PdfSharpCore.Drawing;

namespace PdfSharpCore.Pdf.Signatures
{
    public interface ISignatureAppearanceHandler
    {
        void RenderAppearance(XGraphics gfx, XRect rect);
    }
}
