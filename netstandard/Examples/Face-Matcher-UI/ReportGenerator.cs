using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using PdfSharp.Fonts;
namespace Face_Matcher_UI
{
    public class ReportGenerator
    {
        public static void ExportSuspectReport(int suspectId)
        {
            GlobalFontSettings.FontResolver = new CustomFontResolver();
            var suspect = DbHelper.GetAllSuspects().FirstOrDefault(x => x.SuspectId == suspectId);
            if (suspect == null)
            {
                MessageBox.Show("Suspect not found.");
                return;
            }

            var faceImages = suspect.GetImageList(); // Assumes GetImageList returns List<Image>
            var matchLogs = DbHelper.GetMatchedLogsForSuspect(suspectId);
            string logoPath = "Logo.png";

            using var doc = new PdfDocument();
            doc.Info.Title = "Suspect Match Report";

            // Page 1: Logo + Suspect Info
            // ========== Page 1 ==========
            var page1 = doc.AddPage();
            page1.Size = PdfSharp.PageSize.A4;
            var gfx1 = XGraphics.FromPdfPage(page1);
            int margin = 40;

            if (File.Exists(logoPath))
            {
                using var logo = Image.FromFile(logoPath);
                var logoXImage = XImage.FromFile(logoPath);
                double centerX = (page1.Width.Point - 150) / 2;
                gfx1.DrawImage(logoXImage, centerX, margin, 150, 100);
            }

            double detailsY = 160;
            gfx1.DrawString("Suspect Details", new XFont("Roboto", 16), XBrushes.Black, new XPoint(margin, detailsY));
            gfx1.DrawString($"Name: {suspect.FirstName} {suspect.LastName}", new XFont("Roboto", 12), XBrushes.Black, new XPoint(margin, detailsY + 30));
            gfx1.DrawString($"Gender: {suspect.Gender}", new XFont("Roboto", 12), XBrushes.Black, new XPoint(margin, detailsY + 55));
            gfx1.DrawString($"DOB: {suspect.Dob}", new XFont("Roboto", 12), XBrushes.Black, new XPoint(margin, detailsY + 80));
            gfx1.DrawString($"FIR No: {suspect.FirNo}", new XFont("Roboto", 12), XBrushes.Black, new XPoint(margin, detailsY + 105));
            gfx1.DrawString($"Created At: {suspect.CreatedAt}", new XFont("Roboto", 12), XBrushes.Black, new XPoint(margin, detailsY + 130));
            gfx1.DrawString($"Updated At: {suspect.UpdatedAt}", new XFont("Roboto", 12), XBrushes.Black, new XPoint(margin, detailsY + 155));


            // Page 2: Face Images
            if (faceImages.Count > 0)
            {
                var page2 = doc.AddPage();
                page2.Size = PdfSharp.PageSize.A4;
                var gfx2 = XGraphics.FromPdfPage(page2);
                gfx2.DrawString("Suspect Face Images", new XFont("Roboto", 14), XBrushes.Black, new XPoint(margin, 40));

                int x = margin, y = 70, w = 100, h = 100, padding = 20;
                foreach (var img in faceImages)
                {
                    if (img == null || img.Width == 0 || img.Height == 0)
                        continue;

                    if (x + w > page2.Width - margin)
                    {
                        x = margin;
                        y += h + padding;
                    }

                    try
                    {
                        using var ms = new MemoryStream();
                        img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        ms.Position = 0;
                        var ximg = XImage.FromStream(ms);
                        gfx2.DrawImage(ximg, x, y, w, h);
                    }
                    catch (Exception ex)
                    {
                        gfx2.DrawString("Image Error", new XFont("Roboto", 10), XBrushes.Red, new XPoint(x, y));
                    }

                    x += w + padding;
                }

            }

            // Page 3+: Match Logs
            foreach (var log in matchLogs)
            {
                var page = doc.AddPage();
                page.Size = PdfSharp.PageSize.A4;
                var gfx = XGraphics.FromPdfPage(page);

                gfx.DrawString("Matched Frame", new XFont("Roboto", 14), XBrushes.Black, new XPoint(margin, 40));
                gfx.DrawString($"Match Time: {log.CaptureTime}", new XFont("Roboto", 12), XBrushes.Black, new XPoint(margin, 70));
                gfx.DrawString($"Confidence: {(100 - log.Distance * 100):F2}%", new XFont("Roboto", 12), XBrushes.Black, new XPoint(margin, 95));
                gfx.DrawString($"Filename: {log.Filename}", new XFont("Roboto", 12), XBrushes.Black, new XPoint(margin, 120));
                gfx.DrawString($"Frame Time: {log.Frametime}", new XFont("Roboto", 12), XBrushes.Black, new XPoint(margin, 145));

                try
                {
                    byte[] imageBytes = Convert.FromBase64String(log.FrameBase64);
                    using var stream = new MemoryStream(Convert.FromBase64String(log.FrameBase64));
                    stream.Position = 0;
                    gfx.DrawImage(XImage.FromStream(stream), margin, 180, 300, 200);
                }
                catch (Exception ex)
                {
                    gfx.DrawString("Error loading image", new XFont("Roboto", 10), XBrushes.Red, new XPoint(margin, 180));
                }
            }

            // Ask user to save
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf",
                FileName = $"Suspect_Report_{suspect.FirstName}_{DateTime.Now:yyyyMMddHHmmss}.pdf"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                doc.Save(saveDialog.FileName);
                Process.Start("explorer.exe", saveDialog.FileName);
            }
        }
    }
}
