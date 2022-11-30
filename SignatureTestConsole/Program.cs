using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System.Security.Cryptography.X509Certificates;
using PdfSharpCore.Pdf.IO;
using PdfSharpCore.Drawing.Layout;
using PdfSharpCore.Pdf.Signatures;
using System;
using PdfSharpCore.Pdf.AcroForms;

namespace TestConsole
{
    class Program
    {
        public static void Main(string[] args)
        {
            //Program.CreateAndSign();
            Program.SignNewField();
            Program.SignExisting();
            Program.SignExistingNonDigital();
        }

        private static void CreateAndSign()
        {
            string text = "CreateAndSign.pdf";
            XFont font = new XFont("Verdana", 10.0, XFontStyle.Regular);
            PdfDocument pdfDocument = new PdfDocument();
            PdfPage pdfPage = pdfDocument.AddPage();
            XGraphics xGraphics = XGraphics.FromPdfPage(pdfPage);
            XRect layoutRectangle = new XRect(0.0, 0.0, pdfPage.Width, pdfPage.Height);
            xGraphics.DrawString("Sample document", font, XBrushes.Black, layoutRectangle, XStringFormats.TopCenter);
            PdfSignatureOptions options = new PdfSignatureOptions
            {
                ContactInfo = "Contact Info",
                Location = "Paris",
                Reason = "Test signatures",
                Rectangle = new XRect(36.0, 700.0, 200.0, 50.0)
            };
            PdfSignatureHandler pdfSignatureHandler = new PdfSignatureHandler( new DefaultSigner(Program.GetCertificate()), options);
            pdfSignatureHandler.AttachToDocument(pdfDocument);
            pdfDocument.Save(text);
        }
        private static void SignNewField()
        {
            string text = string.Format("SignNewField.pdf", new object[0]);
            PdfDocument pdfDocument = PdfReader.Open("TestFiles\\doc1.pdf");
            PdfSignatureOptions options = new PdfSignatureOptions
            {
                ContactInfo = "Contact Info",
                Location = "Paris",
                Reason = "Test signatures",
                Rectangle = new XRect(36.0, 735.0, 200.0, 50.0),
                AppearanceHandler = new Program.SignAppearenceHandler()
            };
            
            PdfSignatureHandler pdfSignatureHandler = new PdfSignatureHandler(new DefaultSigner(Program.GetCertificate()), options);
            pdfSignatureHandler.AttachToDocument(pdfDocument);
            pdfDocument.Save(text);
        }
        private static void SignExisting()
        {
            string text = string.Format("SignExisting.pdf", new object[0]);
            PdfDocument pdfDocument = PdfReader.Open(@"TestFiles\\Adobe Digital signing instructions-unsigned.pdf");
            PdfSignatureOptions options = new PdfSignatureOptions
            {
                ContactInfo = "Contact Info",
                Location = "Paris",
                Reason = "Test signatures",
                //Rectangle = new XRect(32, 348, 316, 50),
                AppearanceHandler = new Program.SignAppearenceHandler(),
                FieldName = "Signature1"
            };

            PdfSignatureHandler pdfSignatureHandler = new PdfSignatureHandler(new DefaultSigner(Program.GetCertificate()), options);
            pdfSignatureHandler.AttachToDocument(pdfDocument);
            pdfDocument.Save(text);
        }

        private static void SignExistingNonDigital()
        {
            string text = string.Format("SignExistingNonDigital.pdf", new object[0]);
            PdfDocument pdfDocument = PdfReader.Open(@"TestFiles\\Adobe Digital signing instructions-unsigned.pdf");
            (pdfDocument.AcroForm.Fields["Signature1"] as PdfSignatureField).RenderAppearance(new SignAppearenceHandler());
            pdfDocument.Save(text);
        }

        private static X509Certificate2 GetCertificate()
        {
            // add yours here
            return new X509Certificate2("TestFiles\\myself.pfx", "password", X509KeyStorageFlags.Exportable);
        }

        private class SignAppearenceHandler : ISignatureAppearanceHandler
        {
            private XImage Image = XImage.FromFile("TestFiles\\logo.jpg");
            public void RenderAppearance(XGraphics gfx, XRect rect)
            {
                XColor empty = XColor.Empty;
                string text = "Signed by Napoleon \nLocation: Paris \nDate: " + DateTime.Now.ToString();
                XFont font = new XFont("Verdana", 8.0, XFontStyle.Regular);
                XTextFormatter xTextFormatter = new XTextFormatter(gfx);
                XPoint xPoint = new XPoint(0.0, 0.0);
                bool flag = this.Image != null;
                if (flag)
                {
                    double ratio = 1;
                    if (this.Image.PixelHeight > rect.Height)
                    {
                        ratio = rect.Height / this.Image.PixelHeight;
                    }
                    gfx.DrawImage(this.Image, xPoint.X, xPoint.Y, Image.PixelWidth * ratio, Image.PixelHeight * ratio);
                    xPoint = new XPoint(Image.PixelWidth * ratio + 5, 0.0);
                }
                xTextFormatter.DrawString(text, font, new XSolidBrush(XColor.FromKnownColor(XKnownColor.Black)), new XRect(xPoint.X, xPoint.Y, rect.Width - xPoint.X, rect.Height), XStringFormats.TopLeft);
            }
        }
    }
}
