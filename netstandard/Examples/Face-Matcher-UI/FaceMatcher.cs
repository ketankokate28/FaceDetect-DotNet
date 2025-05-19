using FaceONNX;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using UMapx.Visualization;

namespace Face_Matcher_UI
{
    class FaceMatcher
    {
        public void Run(string SuspectDir,string ImageDir,string ResultDir, Action<string> logCallback)
        {
            logCallback?.Invoke("FaceONNX: Multi-Suspect Augmented Face Matching");

            //string suspectDir = @"..\..\..\suspect";
            //string imageDir = @"..\..\..\images";
            //string resultDir = @"..\..\..\results";
            string suspectDir = SuspectDir;
            string imageDir = ImageDir;
            string resultDir = ResultDir;
            Directory.CreateDirectory(resultDir);

            using var faceDetector = new FaceDetector();
            using var faceEmbedder = new FaceEmbedder();

            // Step 1: Load all suspect embeddings with augmentations
            var suspectEmbeddings = new Dictionary<string, float[]>();
            var suspectFiles = Directory.GetFiles(suspectDir, "*.*", SearchOption.TopDirectoryOnly);

            foreach (var suspectFile in suspectFiles)
            {
                using var suspectImage = new Bitmap(suspectFile);
                var faces = faceDetector.Forward(suspectImage);
                if (faces.Length == 0)
                {
                    logCallback?.Invoke($"No face found in suspect image: {suspectFile}");
                    continue;
                }

                var face = faces[0]; // assuming one face per suspect
                using var cropped = CropFace(suspectImage, face.Box);
                var augmentations = GenerateAugmentations(cropped);

                var embeddings = augmentations.Select(img =>
                {
                    var emb = faceEmbedder.Forward(img);
                    img.Dispose();
                    return emb;
                }).ToList();

                var averagedEmbedding = AverageEmbedding(embeddings);
                var name = Path.GetFileNameWithoutExtension(suspectFile);
                suspectEmbeddings[name] = averagedEmbedding;

                logCallback?.Invoke($"Loaded suspect: {name} with {embeddings.Count} augmented embeddings");
            }

            // Step 2: Process input group images
            var groupImages = Directory.GetFiles(imageDir, "*.*", SearchOption.TopDirectoryOnly);

            using var painter = new Painter()
            {
                BoxPen = new Pen(Color.Yellow, 4),
                Transparency = 0
            };

            foreach (var imageFile in groupImages)
            {
                // Wrap all bitmap work in a dedicated scope so it's disposed before deletion
                using (var bitmap = new Bitmap(imageFile))
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    var groupFaces = faceDetector.Forward(bitmap);

                    foreach (var face in groupFaces)
                    {
                        using var crop = CropFace(bitmap, face.Box);
                        var queryAugmentations = GenerateAugmentations(crop);

                        var queryEmbeddings = queryAugmentations.Select(img =>
                        {
                            var emb = faceEmbedder.Forward(img);
                            img.Dispose(); // free up memory
                            return emb;
                        }).ToList();

                        var averagedQueryEmbedding = AverageEmbedding(queryEmbeddings);

                        string matchedName = "Unknown";
                        double bestDistance = double.MaxValue;

                        foreach (var (name, embedding) in suspectEmbeddings)
                        {
                            double distance = CosineDistance(averagedQueryEmbedding, embedding);
                            if (distance < bestDistance && distance < 0.8)
                            {
                                bestDistance = distance;
                                matchedName = name;

                                var paintData = new PaintData()
                                {
                                    Rectangle = face.Box,
                                    Title = matchedName
                                };
                                painter.Draw(graphics, paintData);

                                logCallback?.Invoke($"Loaded suspect: {matchedName} with distance {bestDistance}");
                                var outputFile = Path.Combine(resultDir, Path.GetFileName(imageFile));
                                bitmap.Save(outputFile);
                                logCallback?.Invoke($"Processed {Path.GetFileName(imageFile)}");
                            }
                        }
                    }
                }

                // All using blocks have completed here; file should now be unlocked
                try
                {
                    File.Delete(imageFile);                  
                    logCallback?.Invoke($"Deleted input image: {Path.GetFileName(imageFile)}");
                }
                catch (Exception ex)
                {
                    logCallback?.Invoke($"Failed to delete image: {imageFile}. Error: {ex.Message}");
                }
            }
            logCallback?.Invoke("Face matching complete.");
        }

        private static Bitmap CropFace(Bitmap image, Rectangle box)
        {
            Rectangle safeBox = Rectangle.Intersect(new Rectangle(Point.Empty, image.Size), box);
            return image.Clone(safeBox, image.PixelFormat);
        }

        private static double CosineDistance(float[] v1, float[] v2)
        {
            double dot = 0.0, normA = 0.0, normB = 0.0;
            for (int i = 0; i < v1.Length; i++)
            {
                dot += v1[i] * v2[i];
                normA += Math.Pow(v1[i], 2);
                normB += Math.Pow(v2[i], 2);
            }
            return 1.0 - (dot / (Math.Sqrt(normA) * Math.Sqrt(normB)));
        }

        private static List<Bitmap> GenerateAugmentations(Bitmap original)
        {
            List<Bitmap> variants = new List<Bitmap>
            {
                new Bitmap(original), // Original
                ToGrayscale(original), // Grayscale
                ApplyGaussianBlur(original), // Blurred
                new Bitmap(original, new Size(original.Width / 2, original.Height / 2)) // Downscaled
            };
            return variants;
        }

        private static Bitmap ToGrayscale(Bitmap original)
        {
            Bitmap gray = new Bitmap(original.Width, original.Height);
            using (Graphics g = Graphics.FromImage(gray))
            {
                var colorMatrix = new System.Drawing.Imaging.ColorMatrix(new float[][]
                {
                    new float[] {0.3f, 0.3f, 0.3f, 0, 0},
                    new float[] {0.59f, 0.59f, 0.59f, 0, 0},
                    new float[] {0.11f, 0.11f, 0.11f, 0, 0},
                    new float[] {0, 0, 0, 1, 0},
                    new float[] {0, 0, 0, 0, 1}
                });
                var attributes = new System.Drawing.Imaging.ImageAttributes();
                attributes.SetColorMatrix(colorMatrix);
                g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
                    0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);
            }
            return gray;
        }

        private static Bitmap ApplyGaussianBlur(Bitmap image)
        {
            // Simulate Gaussian blur by downscaling and upscaling
            var small = new Bitmap(image, new Size(image.Width / 2, image.Height / 2));
            var blurred = new Bitmap(small, image.Size);
            small.Dispose();
            return blurred;
        }

        private static float[] AverageEmbedding(List<float[]> embeddings)
        {
            if (embeddings == null || embeddings.Count == 0)
                return null;

            int length = embeddings[0].Length;
            float[] avg = new float[length];

            foreach (var emb in embeddings)
            {
                for (int i = 0; i < length; i++)
                    avg[i] += emb[i];
            }

            for (int i = 0; i < length; i++)
                avg[i] /= embeddings.Count;

            return avg;
        }
    }
}
