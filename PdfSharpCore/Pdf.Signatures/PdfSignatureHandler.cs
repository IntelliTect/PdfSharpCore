using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.AcroForms;
using PdfSharpCore.Pdf.Advanced;
using PdfSharpCore.Pdf.Annotations;
using PdfSharpCore.Pdf.Signatures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Linq;
using static PdfSharpCore.Pdf.AcroForms.PdfAcroField;

namespace PdfSharpCore.Pdf.Signatures
{
    public class IntEventArgs : EventArgs { public int Value { get; set; } }

    /// <summary>
    /// Handles the signature
    /// </summary>
    public class PdfSignatureHandler
    {

        private PositionTracker contentsTraker;
        private PositionTracker rangeTracker;
        private int? maximumSignatureLength;
        private const int byteRangePaddingLength = 36; // the place big enough required to replace [0 0 0 0] with the correct value

        public event EventHandler<IntEventArgs> SignatureSizeComputed = (s, e) => { };

        public PdfDocument Document { get; private set; }    
        public PdfSignatureOptions Options { get; private set; }
        private ISigner signer { get; set; }

        public void AttachToDocument(PdfDocument documentToSign)
        {            
            this.Document = documentToSign;
            this.Document.BeforeSave += AddSignatureComponents;
            this.Document.AfterSave += ComputeSignatureAndRange;

            if (!maximumSignatureLength.HasValue)
            {
                maximumSignatureLength = signer.GetSignedCms(new MemoryStream(new byte[] { 0})).Length;
                SignatureSizeComputed(this, new IntEventArgs() { Value = maximumSignatureLength.Value });
            }
        }

        public PdfSignatureHandler(ISigner signer, PdfSignatureOptions options, int? signatureMaximumLength = null)
        {            
            this.signer = signer;
            this.maximumSignatureLength = signatureMaximumLength;
            this.Options = options;           
        }

        private void ComputeSignatureAndRange(object sender, PdfDocumentEventArgs e)
        {
            var writer = e.Writer;
            writer.Stream.Position = rangeTracker.Start;
            var rangeArray = new PdfArray(new PdfInteger(0), 
                new PdfInteger((int)contentsTraker.Start),
                new PdfInteger((int)contentsTraker.End),
                new PdfInteger((int)(writer.Stream.Length - contentsTraker.End)));
            rangeArray.Write(writer);

            var rangeToSign = GetRangeToSign(writer.Stream);

            var signature = signer.GetSignedCms(rangeToSign);
            if (signature.Length > maximumSignatureLength)
                throw new Exception("The signature length is bigger that the approximation made.");

            var hexFormated = Encoding.Default.GetBytes(FormatHex(signature));

            writer.Stream.Position = contentsTraker.Start+1;
            writer.Write(hexFormated);
        }

        string FormatHex(byte[] bytes)
        {
            var retval = new StringBuilder();

            for (int idx = 0; idx < bytes.Length; idx++)
                retval.AppendFormat("{0:x2}", bytes[idx]);

            return retval.ToString();
        }

        private RangedStream GetRangeToSign(Stream stream)
        {
            return new RangedStream(stream, new List<RangedStream.Range>()
            {
                new RangedStream.Range(0, contentsTraker.Start),
                new RangedStream.Range(contentsTraker.End, stream.Length - contentsTraker.End)
            });
            
        }

        private void AddSignatureComponents(object sender, EventArgs e)
        {
            var catalog = Document.Catalog;

            if (catalog.AcroForm == null)
                catalog.AcroForm = new PdfAcroForm(Document);

            if (catalog.AcroForm.Elements.ContainsKey(PdfAcroForm.Keys.SigFlags))
            {
                catalog.AcroForm.Elements.Remove(PdfAcroForm.Keys.SigFlags);
            }
            catalog.AcroForm.Elements.Add(PdfAcroForm.Keys.SigFlags, new PdfInteger(3));

            PdfSignatureField signature = Options.FieldName == null ? null : catalog.AcroForm.Fields[Options.FieldName] as PdfSignatureField;
            bool isNew = signature == null;

            if (isNew)
            {
                signature = new PdfSignatureField(Document);
                signature.Elements[Keys.T] = new PdfString(Options.FieldName ?? "Signature1");
            }

            if (isNew || (Options.Rectangle != XRect.Empty && Options.Rectangle != default))
            {
                signature.Rectangle = new PdfRectangle(Options.Rectangle);
            }

            var paddedContents = new PdfString("", PdfStringFlags.HexLiteral, maximumSignatureLength.Value);
            var paddedRange = new PdfArray(Document, byteRangePaddingLength, new PdfInteger(0), new PdfInteger(0), new PdfInteger(0), new PdfInteger(0));

            this.contentsTraker = new PositionTracker(paddedContents);
            this.rangeTracker = new PositionTracker(paddedRange);

            signature.Contents = paddedContents;
            signature.ByteRange = paddedRange;
            signature.Reason = Options.Reason;
            signature.Location = Options.Location;

            if (signature.IsVisible)
            {
                signature.RenderAppearance(Options.AppearanceHandler ?? new DefaultAppearanceHandler()
                {
                    Location = Options.Location,
                    Reason = Options.Reason,
                    Signer = signer.GetName()
                });
            }

            if (isNew)
            {
                if (!Document.Pages[0].Elements.ContainsKey(PdfPage.Keys.Annots))
                    Document.Pages[0].Elements.Add(PdfPage.Keys.Annots, new PdfArray(Document));

                (Document.Pages[0].Elements[PdfPage.Keys.Annots] as PdfArray).Elements.Add(signature);

                catalog.AcroForm.Fields.Elements.Add(signature);
            }
        }
    }
}
