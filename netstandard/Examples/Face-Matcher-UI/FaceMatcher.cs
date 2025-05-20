using FaceONNX;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UMapx.Visualization;

namespace Face_Matcher_UI
{
    class FaceMatcher
    {
        public double MatchThreshold { get; set; } = 0.7;

        public void Run(string suspectDir, string imageDir, string resultDir, Action<string> logCallback)
        {
            var stopwatch = Stopwatch.StartNew(); // Start timing
            logCallback?.Invoke("FaceONNX: Optimized Multi-Suspect Augmented Face Matching");
            Directory.CreateDirectory(resultDir);

            using var faceDetector = new FaceDetector();
            using var faceEmbedder = new FaceEmbedder();

            var suspectEmbeddings = new ConcurrentDictionary<string, float[]>();
            var suspectFiles = Directory.GetFiles(suspectDir);

            // Parallel loading of suspect images
            Parallel.ForEach(suspectFiles, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, suspectFile =>
            {
                try
                {
                    using var suspectImage = LoadBitmapUnlocked(suspectFile);
                    var faces = faceDetector.Forward(suspectImage);
                    if (faces.Length == 0)
                    {
                        logCallback?.Invoke($"No face found in suspect image: {suspectFile}");
                        return;
                    }

                    using var cropped = CropFace(suspectImage, faces[0].Box);
                    var augmentations = GenerateAugmentations(cropped);
                    var embeddings = new List<float[]>();

                    foreach (var img in augmentations)
                    {
                        using (img)
                        {
                            embeddings.Add(faceEmbedder.Forward(img));
                        }
                    }

                    suspectEmbeddings[Path.GetFileNameWithoutExtension(suspectFile)] = AverageEmbedding(embeddings);
                    logCallback?.Invoke($"Loaded suspect: {Path.GetFileNameWithoutExtension(suspectFile)} with {embeddings.Count} embeddings");
                }
                catch (Exception ex)
                {
                    logCallback?.Invoke($"Error loading suspect {suspectFile}: {ex.Message}");
                }
            });

            var groupImages = Directory.GetFiles(imageDir);
            using var painter = new Painter() { BoxPen = new Pen(Color.Yellow, 4), Transparency = 0 };

            Parallel.ForEach(groupImages, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, imageFile =>
            {
                var sw = Stopwatch.StartNew();

                try
                {
                    using var bitmap = LoadBitmapUnlocked(imageFile);
                    using var graphics = Graphics.FromImage(bitmap);

                    // Optionally: use thread-local faceDetector/faceEmbedder if not thread-safe
                    var groupFaces = faceDetector.Forward(bitmap);
                    bool matchedAny = false;

                    foreach (var face in groupFaces)
                    {
                        using var crop = CropFace(bitmap, face.Box);
                        var queryAugmentations = GenerateAugmentations(crop);

                        // Faster parallel embedding generation
                        var queryEmbeddings = queryAugmentations.AsParallel().Select(img =>
                        {
                            using (img) return faceEmbedder.Forward(img);
                        }).ToList();

                        var averagedQuery = AverageEmbedding(queryEmbeddings);

                        string bestMatch = "Unknown";
                        double bestDist = double.MaxValue;

                        foreach (var (name, embedding) in suspectEmbeddings)
                        {
                            double dist = CosineDistanceSIMD(averagedQuery, embedding);
                            if (dist < bestDist)
                            {
                                bestDist = dist;
                                bestMatch = name;
                            }
                        }

                        if (bestDist < MatchThreshold)
                        {
                            painter.Draw(graphics, new PaintData { Rectangle = face.Box, Title = bestMatch });
                            logCallback?.Invoke($"Matched suspect: {bestMatch} with distance {bestDist:F3}");
                            matchedAny = true;
                        }
                    }

                    if (matchedAny)
                    {
                        bitmap.Save(Path.Combine(resultDir, Path.GetFileName(imageFile)));
                        logCallback?.Invoke($"Processed and saved: {Path.GetFileName(imageFile)}");
                    }
                    else
                    {
                        logCallback?.Invoke($"No match found in {Path.GetFileName(imageFile)}");
                    }

                    File.Delete(imageFile);
                    logCallback?.Invoke($"Deleted input image: {Path.GetFileName(imageFile)}");
                }
                catch (Exception ex)
                {
                    logCallback?.Invoke($"Error processing image {imageFile}: {ex.Message}");
                }

                sw.Stop();
                logCallback?.Invoke($"Image {Path.GetFileName(imageFile)} processed in {sw.ElapsedMilliseconds} ms");
            });
            stopwatch.Stop();
            logCallback?.Invoke("Face matching complete.");
            logCallback?.Invoke($"\nTotal processing time: {stopwatch.Elapsed.TotalSeconds:F2} seconds");
        }
        private static Bitmap LoadBitmapUnlocked(string path)
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var ms = new MemoryStream();
            fs.CopyTo(ms);
            ms.Position = 0;
            return new Bitmap(ms);
        }
        public static double CosineDistanceSIMD(float[] a, float[] b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Embeddings must be the same length");

            var dot = 0f;
            var normA = 0f;
            var normB = 0f;

            int i = 0;
            int simdLength = Vector<float>.Count;

            while (i + simdLength <= a.Length)
            {
                var va = new Vector<float>(a, i);
                var vb = new Vector<float>(b, i);

                dot += Vector.Dot(va, vb);
                normA += Vector.Dot(va, va);
                normB += Vector.Dot(vb, vb);

                i += simdLength;
            }

            // tail processing
            for (; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                normA += a[i] * a[i];
                normB += b[i] * b[i];
            }

            return 1.0 - (dot / (Math.Sqrt(normA) * Math.Sqrt(normB) + 1e-10));
        }

        private static Bitmap CropFace(Bitmap image, Rectangle box)
        {
            Rectangle safeBox = Rectangle.Intersect(new Rectangle(Point.Empty, image.Size), box);
            return image.Clone(safeBox, image.PixelFormat);
        }

        private static double CosineDistance(float[] v1, float[] v2)
        {
            double dot = 0, normA = 0, normB = 0;
            for (int i = 0; i < v1.Length; i++)
            {
                dot += v1[i] * v2[i];
                normA += v1[i] * v1[i];
                normB += v2[i] * v2[i];
            }
            return 1.0 - (dot / (Math.Sqrt(normA) * Math.Sqrt(normB)));
        }

        private static List<Bitmap> GenerateAugmentations(Bitmap original)
        {
            try
            {
                return new List<Bitmap>
                {
                    new Bitmap(original), // Original
                    ToGrayscale(original), // Grayscale
                    //ApplyGaussianBlur(original), // Blurred
                   // new Bitmap(original, new Size(original.Width / 2, original.Height / 2)), // Downscaled
                   // FlipHorizontal(original) // Flip
                };
            }
            catch
            {
                return new List<Bitmap> { new Bitmap(original) };
            }
        }

        private static Bitmap FlipHorizontal(Bitmap image)
        {
            var flipped = new Bitmap(image);
            flipped.RotateFlip(RotateFlipType.RotateNoneFlipX);
            return flipped;
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
                g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height), 0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);
            }
            return gray;
        }

        private static Bitmap ApplyGaussianBlur(Bitmap image)
        {
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
                for (int i = 0; i < length; i++)
                    avg[i] += emb[i];

            for (int i = 0; i < length; i++)
                avg[i] /= embeddings.Count;

            return avg;
        }

private static List<Bitmap> GenerateAugmentations_cctv(Bitmap original)
        {
            List<Bitmap> variants = new List<Bitmap>
    {
        new Bitmap(original), // Original
        AdjustBrightness(original, 1.2f),
        AdjustBrightness(original, 0.8f),
        AdjustContrast(original, 1.2f),
        AdjustContrast(original, 0.8f),
        RotateImage(original, 5),
        RotateImage(original, -5)
    };
            return variants;
        }
        private static Bitmap AdjustBrightness(Bitmap image, float factor)
        {
            Bitmap adjusted = new Bitmap(image.Width, image.Height);
            using (Graphics g = Graphics.FromImage(adjusted))
            {
                var colorMatrix = new System.Drawing.Imaging.ColorMatrix(new float[][]
                {
            new float[] {factor, 0, 0, 0, 0},
            new float[] {0, factor, 0, 0, 0},
            new float[] {0, 0, factor, 0, 0},
            new float[] {0, 0, 0, 1, 0},
            new float[] {0, 0, 0, 0, 1}
                });
                var attributes = new System.Drawing.Imaging.ImageAttributes();
                attributes.SetColorMatrix(colorMatrix);
                g.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height),
                    0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
            }
            return adjusted;
        }
        private static Bitmap AdjustContrast(Bitmap image, float contrast)
        {
            float t = 0.5f * (1.0f - contrast);
            Bitmap adjusted = new Bitmap(image.Width, image.Height);
            using (Graphics g = Graphics.FromImage(adjusted))
            {
                var colorMatrix = new System.Drawing.Imaging.ColorMatrix(new float[][]
                {
            new float[] {contrast, 0, 0, 0, 0},
            new float[] {0, contrast, 0, 0, 0},
            new float[] {0, 0, contrast, 0, 0},
            new float[] {0, 0, 0, 1, 0},
            new float[] {t, t, t, 0, 1}
                });
                var attributes = new System.Drawing.Imaging.ImageAttributes();
                attributes.SetColorMatrix(colorMatrix);
                g.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height),
                    0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
            }
            return adjusted;
        }
        private static Bitmap RotateImage(Bitmap image, float angle)
        {
            Bitmap rotated = new Bitmap(image.Width, image.Height);
            using (Graphics g = Graphics.FromImage(rotated))
            {
                g.TranslateTransform(image.Width / 2, image.Height / 2);
                g.RotateTransform(angle);
                g.TranslateTransform(-image.Width / 2, -image.Height / 2);
                g.DrawImage(image, new Point(0, 0));
            }
            return rotated;
        }
        private static Bitmap NormalizeBrightnessContrast(Bitmap image)
        {
            var adjusted = new Bitmap(image.Width, image.Height);
            using (Graphics g = Graphics.FromImage(adjusted))
            {
                float brightness = 1.1f; // Slight boost
                float contrast = 1.2f;   // Increase contrast

                var colorMatrix = new System.Drawing.Imaging.ColorMatrix(new float[][]
                {
            new float[] {contrast, 0, 0, 0, 0},
            new float[] {0, contrast, 0, 0, 0},
            new float[] {0, 0, contrast, 0, 0},
            new float[] {0, 0, 0, 1, 0},
            new float[] {brightness - 1, brightness - 1, brightness - 1, 0, 1}
                });

                var attributes = new System.Drawing.Imaging.ImageAttributes();
                attributes.SetColorMatrix(colorMatrix);

                g.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height),
                    0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
            }
            return adjusted;
        }
        private static Bitmap EqualizeHistogram(Bitmap bmp)
        {
            var gray = ToGrayscale(bmp);
            System.Drawing.Imaging.BitmapData data = gray.LockBits(new Rectangle(0, 0, gray.Width, gray.Height),
                                            System.Drawing.Imaging.ImageLockMode.ReadWrite, gray.PixelFormat);

            int bytes = Math.Abs(data.Stride) * gray.Height;
            byte[] buffer = new byte[bytes];
            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, buffer, 0, bytes);

            // Build histogram
            int[] hist = new int[256];
            for (int i = 0; i < buffer.Length; i++) hist[buffer[i]]++;

            // Build cumulative distribution
            int[] cdf = new int[256];
            cdf[0] = hist[0];
            for (int i = 1; i < 256; i++) cdf[i] = cdf[i - 1] + hist[i];

            int cdfMin = cdf.First(c => c > 0);
            int totalPixels = gray.Width * gray.Height;
            byte[] result = new byte[buffer.Length];
            for (int i = 0; i < buffer.Length; i++)
            {
                result[i] = (byte)((cdf[buffer[i]] - cdfMin) * 255 / (totalPixels - cdfMin));
            }

            Marshal.Copy(result, 0, data.Scan0, bytes);
            gray.UnlockBits(data);
            return gray;
        }
        private static Bitmap Sharpen(Bitmap image)
        {
            float[,] kernel = {
        { -1, -1, -1 },
        { -1,  9, -1 },
        { -1, -1, -1 }
    };

            Bitmap sharpened = new Bitmap(image.Width, image.Height);

            for (int y = 1; y < image.Height - 1; y++)
            {
                for (int x = 1; x < image.Width - 1; x++)
                {
                    float r = 0, g = 0, b = 0;

                    for (int ky = -1; ky <= 1; ky++)
                    {
                        for (int kx = -1; kx <= 1; kx++)
                        {
                            Color pixel = image.GetPixel(x + kx, y + ky);
                            float kernelValue = kernel[ky + 1, kx + 1];

                            r += pixel.R * kernelValue;
                            g += pixel.G * kernelValue;
                            b += pixel.B * kernelValue;
                        }
                    }

                    int red = Clamp((int)r, 0, 255);
                    int green = Clamp((int)g, 0, 255);
                    int blue = Clamp((int)b, 0, 255);

                    sharpened.SetPixel(x, y, Color.FromArgb(red, green, blue));
                }
            }

            return sharpened;
        }

        private static int Clamp(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(value, max));
        }

        private static Bitmap PreprocessCCTVFace(Bitmap image)
        {
            var steps = new List<Func<Bitmap, Bitmap>>
    {
        NormalizeBrightnessContrast,
        EqualizeHistogram,
        ApplyGaussianBlur
       // Sharpen,
    };

            Bitmap current = new Bitmap(image);
            foreach (var step in steps)
            {
                var next = step(current);
                current.Dispose();
                current = next;
            }

            return current;
        }



    }
}
