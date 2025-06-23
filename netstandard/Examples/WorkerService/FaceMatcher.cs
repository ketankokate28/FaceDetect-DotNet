using FaceONNX;
using Microsoft.Data.Sqlite;
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
using System.Xml.Linq;
using UMapx.Visualization;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace WorkerService
{
    class FaceMatcher
    {
        public double MatchThreshold { get; set; } = 0.65;
        string connectionString = "";
        private readonly IConfiguration _configuration;
        private readonly ILogger<FaceMatcher> _logger;
        private readonly AppSettingsOptions _appSettings;
        public FaceMatcher(IConfiguration configuration, AppSettingsOptions appSettings)
        {
            _configuration = configuration;
            connectionString = _configuration.GetConnectionString("DefaultConnection");
            _logger = LogHelper.GetLogger<FaceMatcher>();
            _appSettings = appSettings;
            MatchThreshold = _appSettings.MatchThreshold;
        }
        public Dictionary<string, float[]> PrecomputeSuspectEmbeddingsFromBlobs(Dictionary<int, (string name, List<byte[]> blobs)> suspects, Action<string> log)
        {
            using var faceDetector = new FaceDetector();
            using var faceEmbedder = new FaceEmbedder();

            var result = new Dictionary<string, float[]>();

            foreach (var kvp in suspects)
            {
                int suspectId = kvp.Key;
                string name = kvp.Value.name;
                var blobs = kvp.Value.blobs;
                var embeddings = new List<float[]>();

                foreach (var blob in blobs)
                {
                    try
                    {
                        using var ms = new MemoryStream(blob);
                        using var image = new Bitmap(ms);
                        var faces = faceDetector.Forward(image);

                        if (faces.Length == 0) continue;

                        using var cropped = CropFace(image, faces[0].Box);
                        var augmentations = GenerateAugmentations(cropped);

                        var imageEmbeddings = new ConcurrentBag<float[]>();
                        Parallel.ForEach(augmentations, img =>
                        {
                            try
                            {
                                using (img)
                                {
                                    var emb = faceEmbedder.Forward(img);
                                    imageEmbeddings.Add(emb);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Embedding failed: {ex.Message}");
                            }
                        });

                        embeddings.AddRange(imageEmbeddings);
                        log?.Invoke($"Processed blob image for suspect {suspectId}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Blob read failed: {ex.Message}");
                    }
                }

                if (embeddings.Count > 0)
                {
                    string key = $"{suspectId}-{name}";
                    result[key] = AverageEmbedding(embeddings);
                    log?.Invoke($"Finished embeddings for {key} with {embeddings.Count} vectors.");
                }
                else
                {
                    log?.Invoke($"No faces found for suspect {suspectId}, skipping.");
                }
            }

            return result;
        }

        public Dictionary<string, float[]> PrecomputeSuspectEmbeddings(List<string> suspectFolders, Action<string> log)
        {
            using var faceDetector = new FaceDetector();
            using var faceEmbedder = new FaceEmbedder();

            var result = new Dictionary<string, float[]>();

            foreach (var folder in suspectFolders)
            {
                var suspectId = Path.GetFileName(folder); // suspect folder name is the ID (as string)
                var embeddings = new List<float[]>();

                foreach (var suspectFile in Directory.GetFiles(folder, "*"))
                {
                    using var image = LoadBitmapUnlocked(suspectFile);
                   var faces = faceDetector.Forward(image);
                    if (faces.Length == 0) continue;

                   using var cropped = CropFace(image, faces[0].Box);
                    var augmentations = GenerateAugmentations(cropped);
                    //var imageEmbeddings = augmentations.Select(img =>
                    //{
                    //    using (img) return faceEmbedder.Forward(img);
                    //});

                    var imageEmbeddings = new ConcurrentBag<float[]>();

                    Parallel.ForEach(augmentations, img =>
                    {
                        try
                        {
                            using (img)
                            {
                                var embedding = faceEmbedder.Forward(img);
                                imageEmbeddings.Add(embedding);
                            }
                        }
                        catch (Exception ex)
                        {
                            // Optionally log or ignore failed face
                            Console.WriteLine($"Embedding failed: {ex.Message}");
                        }
                    });

                    embeddings.AddRange(imageEmbeddings);
                    log?.Invoke($"Processed image {Path.GetFileName(suspectFile)} for suspect {suspectId}");
                }

                if (embeddings.Count > 0)
                {
                    result[suspectId +"-" +GetSuspectName(suspectId)] = AverageEmbedding(embeddings);
                    log?.Invoke($"Finished embeddings for suspect {suspectId} with {embeddings.Count} vectors.");
                }
                else
                {
                    log?.Invoke($"No faces found for suspect {suspectId}, skipping.");
                }
            }

            return result;
        }

        //    public void RunBatch(
        //         Dictionary<string, float[]> suspectEmbeddings,
        //         List<ImageFrame> frames,
        //         string resultDir,
        //         string tempResultDir,
        //         Action<string> logCallback, FaceDetector faceDetector, FaceEmbedder faceEmbedder)
        //    {
        //        //using var painter = new Painter() { BoxPen = new Pen(Color.Yellow, 15,), Transparency = 0 };

        //        using var painter = new Painter()
        //        {
        //            BoxPen = new Pen(Color.Yellow, 4), // Thinner and more modern
        //            TextFont = new Font("Segoe UI", 10, FontStyle.Bold), // Clearer system font
        //            Transparency = 0
        //        };
        //        var processingTasks = new List<Task>();
        //        foreach (var frame in frames)
        //        {
        //            var sw = Stopwatch.StartNew();
        //            try
        //            {
        //                // using var bitmap = LoadBitmapUnlocked(imageFile);
        //                using var graphics = Graphics.FromImage(frame.Image);


        //                var sw1 = Stopwatch.StartNew();
        //                var groupFaces = faceDetector.Forward(frame.Image);
        //                sw1.Stop();
        //                logCallback?.Invoke($"Image detects in {sw1.ElapsedMilliseconds} ms");
        //                bool matchedAny = false;
        //                int bestMatchID = 0;
        //                string bestMatchName = "";
        //                float bestMatchDistance = 0.0f;
        //                foreach (var face in groupFaces)
        //                {
        //                    using var crop = CropFace(frame.Image, face.Box);
        //                    var queryAugmentations = GenerateAugmentations(crop);
        //                    var embeddingArray = new float[queryAugmentations.Count][];
        //                    var sw2 = Stopwatch.StartNew();
        //                    Parallel.For(0, queryAugmentations.Count, i =>
        //                    {
        //                        using var img = queryAugmentations[i];
        //                        embeddingArray[i] = faceEmbedder.Forward(img);
        //                    });

        //                    var queryEmbeddings = embeddingArray.ToList();
        //                    sw2.Stop();
        //                    logCallback?.Invoke($"faceEmbedder in {sw2.ElapsedMilliseconds} ms");
        //                    var averagedQuery = AverageEmbedding(queryEmbeddings);
        //                    var bestMatch = suspectEmbeddings
        //.AsParallel()
        //.Select(kvp => new
        //{
        //    Name = kvp.Key,
        //    Distance = CosineDistanceSIMD(averagedQuery, kvp.Value)
        //})
        //.OrderBy(kvp => kvp.Distance)
        //.First();


        //                    if (bestMatch.Distance < MatchThreshold)
        //                    {
        //                        double similarityPercent = (1.0 - bestMatch.Distance) * 100.0;
        //                        var nameBox = new Rectangle(face.Box.X, face.Box.Y - 20, Math.Max(face.Box.Width, 150), 20);
        //                        painter.Draw(graphics, new PaintData { Rectangle = face.Box });
        //                        graphics.DrawString(bestMatch.Name + " " + Math.Round(similarityPercent) + "%", painter.TextFont, Brushes.White, nameBox.Location);
        //                        logCallback?.Invoke($"Matched suspect: {bestMatch} with distance {bestMatch.Distance:F3}");
        //                        matchedAny = true;
        //                        //  Bitmap clonedBitmap = (Bitmap)bitmap.Clone(); // Clone for background use
        //                        //originalBitmap.Dispose();
        //                        // ProcessAndLogMatch(bestMatch.Name, (float)bestMatch.Distance, imageFile, clonedBitmap, resultDir, tempResultDir, logCallback);

        //                        //            processingTasks.Add(
        //                        //      ProcessAndLogMatchAsync(bestMatch.Name, (float)bestMatch.Distance, imageFile, clonedBitmap, resultDir, tempResultDir, logCallback)
        //                        //);
        //                    }
        //                }

        //                if (matchedAny)
        //                {
        //                    // File.Delete(imageFile);
        //                    string timestamp = frame.Timestamp.ToString("yyyyMMdd_HHmmss_fff"); // safe format
        //                    string fileName = $"{frame.SourceId}_{timestamp}.jpg";

        //                    frame.Image.Save(Path.Combine(resultDir, fileName), ImageFormat.Jpeg);
        //                    frame.Image.Save(Path.Combine(tempResultDir, fileName), ImageFormat.Jpeg);


        //                    //logCallback?.Invoke($"Processed and saved: {Path.GetFileName(imageFile)}");
        //                    //InsertMatchFaceLog("", Path.Combine(resultDir, Path.GetFileName(imageFile)), 1, bestMatchID, bestMatchName, bestMatchDistance);
        //                }
        //                else
        //                {
        //                    //File.Delete(imageFile);
        //                    logCallback?.Invoke($"No match found in {Path.GetFileName(frame.SourceId + "_" + frame.Timestamp)}");

        //                }
        //                try
        //                {
        //                    // File.Delete(imageFile);
        //                    File.Delete(frame.FilePath);
        //                    // logCallback?.Invoke($"Deleted processed file: {Path.GetFileName(imageFile)}");
        //                }
        //                catch (Exception ex)
        //                {
        //                    logCallback?.Invoke($"Error deleting file {Path.GetFileName(frame.SourceId + "_" + frame.Timestamp)}: {ex.Message}");
        //                }
        //                frame.Image.Dispose();
        //            }
        //            catch (Exception ex)
        //            {
        //                logCallback?.Invoke($"Error processing image {frame.SourceId + "_" + frame.Timestamp}: {ex.Message}");
        //            }


        //            sw.Stop();
        //            logCallback?.Invoke($"Image {Path.GetFileName(frame.SourceId + "_" + frame.Timestamp)} processed in {sw.ElapsedMilliseconds} ms");
        //        }
        //    }

        public void RunBatch_CUDA(
  Dictionary<string, float[]> suspectEmbeddings,
  string[] imageFiles,
  string resultDir,
  string tempResultDir,
  Action<string> logCallback,
  FaceDetector faceDetector,
  FaceEmbedder faceEmbedder)
        {
            using var painter = new Painter()
            {
                BoxPen = new Pen(Color.Yellow, 4),
                TextFont = new Font("Segoe UI", 10, FontStyle.Bold),
                Transparency = 0
            };

            foreach (var imageFile in imageFiles)
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    using var bitmap = LoadBitmapUnlocked(imageFile);
                    using var graphics = Graphics.FromImage(bitmap);

                    var sw1 = Stopwatch.StartNew();
                    var groupFaces = faceDetector.Forward(bitmap);
                    sw1.Stop();
                    logCallback?.Invoke($"Image detects in {sw1.ElapsedMilliseconds} ms");

                    if (groupFaces.Length == 0)
                    {
                        logCallback?.Invoke($"No faces detected in {Path.GetFileName(imageFile)}");
                        File.Delete(imageFile);
                        sw.Stop();
                        logCallback?.Invoke($"Image {Path.GetFileName(imageFile)} processed in {sw.ElapsedMilliseconds} ms");
                        continue;
                    }

                    // Prepare batch inputs
                    var allAugmentedImages = new List<Bitmap>();
                    var faceAugmentationMap = new List<(int AugCount, FaceDetectionResult Face)>();

                    foreach (var face in groupFaces)
                    {
                        using var crop = CropFace(bitmap, face.Box);
                        var augmentations = GenerateAugmentations(crop);
                        allAugmentedImages.AddRange(augmentations);
                        faceAugmentationMap.Add((augmentations.Count, face));
                    }

                    var sw2 = Stopwatch.StartNew();
                    var allEmbeddings = faceEmbedder.ForwardBatch(allAugmentedImages);
                    sw2.Stop();
                    logCallback?.Invoke($"faceEmbedder batch in {sw2.ElapsedMilliseconds} ms");

                    bool matchedAny = false;

                    int cursor = 0;
                    foreach (var (augCount, face) in faceAugmentationMap)
                    {
                        var faceEmbeddings = allEmbeddings.Skip(cursor).Take(augCount).ToList();
                        cursor += augCount;

                        var averagedQuery = AverageEmbedding(faceEmbeddings);

                        var bestMatch = suspectEmbeddings
                            .AsParallel()
                            .Select(kvp => new
                            {
                                Name = kvp.Key,
                                Distance = CosineDistanceSIMD(averagedQuery, kvp.Value)
                            })
                            .OrderBy(kvp => kvp.Distance)
                            .First();

                        if (bestMatch.Distance < MatchThreshold)
                        {
                            double similarityPercent = (1.0 - bestMatch.Distance) * 100.0;
                            var nameBox = new Rectangle(face.Box.X, face.Box.Y - 20, Math.Max(face.Box.Width, 150), 20);
                            painter.Draw(graphics, new PaintData { Rectangle = face.Box });
                            graphics.DrawString(bestMatch.Name + " " + Math.Round(similarityPercent) + "%", painter.TextFont, Brushes.White, nameBox.Location);
                            logCallback?.Invoke($"Matched suspect: {bestMatch.Name} with distance {bestMatch.Distance:F3}");
                            matchedAny = true;
                        }
                        else
                        {
                            logCallback?.Invoke($"No match for face in {Path.GetFileName(imageFile)}");
                        }
                    }

                    if (matchedAny)
                    {
                        bitmap.Save(Path.Combine(resultDir, Path.GetFileName(imageFile)));
                        bitmap.Save(Path.Combine(tempResultDir, Path.GetFileName(imageFile)));
                        File.Delete(imageFile);
                    }
                    else
                    {
                        File.Delete(imageFile);
                    }

                    sw.Stop();
                    logCallback?.Invoke($"Image {Path.GetFileName(imageFile)} processed in {sw.ElapsedMilliseconds} ms");
                }
                catch (Exception ex)
                {
                    logCallback?.Invoke($"Error processing image {imageFile}: {ex.Message}");
                }
            }
        }


        public void RunBatch(
                 Dictionary<string, float[]> suspectEmbeddings,
                 string[] imageFiles,
                 string resultDir,
                 string tempResultDir,
                 Action<string> logCallback, FaceDetector faceDetector, FaceEmbedder faceEmbedder)
            {
                //using var painter = new Painter() { BoxPen = new Pen(Color.Yellow, 15,), Transparency = 0 };

                using var painter = new Painter()
                {
                    BoxPen = new Pen(Color.Yellow, 4), // Thinner and more modern
                    TextFont = new Font("Segoe UI", 10, FontStyle.Bold), // Clearer system font
                    Transparency = 0
                };
                var processingTasks = new List<Task>();
                foreach (var imageFile in imageFiles)
                {
                    var sw = Stopwatch.StartNew();
                    try
                    {
                        using var bitmap = LoadBitmapUnlocked(imageFile);
                        using var graphics = Graphics.FromImage(bitmap);


                        var sw1 = Stopwatch.StartNew();
                        var groupFaces = faceDetector.Forward(bitmap);
                        sw1.Stop();
                        logCallback?.Invoke($"Image detects in {sw1.ElapsedMilliseconds} ms");
                        bool matchedAny = false;
                        int bestMatchID = 0;
                        string bestMatchName = "";
                        float bestMatchDistance = 0.0f;
                        foreach (var face in groupFaces)
                        {
                            using var crop = CropFace(bitmap, face.Box);
                            var queryAugmentations = GenerateAugmentations(crop);
                            var embeddingArray = new float[queryAugmentations.Count][];
                            var sw2 = Stopwatch.StartNew();
                        var augmentationImages = queryAugmentations.ToArray();

                        Parallel.For(0, augmentationImages.Length, i =>
                        {
                            try
                            {
                                using var img = augmentationImages[i]; // Safe copy per thread
                                embeddingArray[i] = faceEmbedder.Forward(img);
                            }
                            catch (Exception ex)
                            {
                                // Optional: logging
                                Console.WriteLine($"Error in embedding at index {i}: {ex.Message}");
                                embeddingArray[i] = new float[FaceEmbedder.EmbeddingSize]; // fallback zero vector
                            }
                        });


                        var queryEmbeddings = embeddingArray.ToList();
                            sw2.Stop();
                            logCallback?.Invoke($"faceEmbedder in {sw2.ElapsedMilliseconds} ms");
                            var averagedQuery = AverageEmbedding(queryEmbeddings);
                            var bestMatch = suspectEmbeddings
        .AsParallel()
        .Select(kvp => new {
            Name = kvp.Key,
            Distance = CosineDistanceSIMD(averagedQuery, kvp.Value)
        })
        .OrderBy(kvp => kvp.Distance)
        .First();


                            if (bestMatch.Distance < MatchThreshold)
                            {
                                double similarityPercent = (1.0 - bestMatch.Distance) * 100.0;
                                var nameBox = new Rectangle(face.Box.X, face.Box.Y - 20, Math.Max(face.Box.Width, 150), 20);
                                painter.Draw(graphics, new PaintData { Rectangle = face.Box });
                                graphics.DrawString(bestMatch.Name + " " + Math.Round(similarityPercent) + "%", painter.TextFont, Brushes.White, nameBox.Location);
                                logCallback?.Invoke($"Matched suspect: {bestMatch} with distance {bestMatch.Distance:F3}");
                                matchedAny = true;
                                Bitmap clonedBitmap = (Bitmap)bitmap.Clone(); // Clone for background use
                                                                              //originalBitmap.Dispose();
                                                                              // ProcessAndLogMatch(bestMatch.Name, (float)bestMatch.Distance, imageFile, clonedBitmap, resultDir, tempResultDir, logCallback);

                                      processingTasks.Add(
                                ProcessAndLogMatchAsync(bestMatch.Name, (float)bestMatch.Distance, imageFile, clonedBitmap, resultDir, tempResultDir, logCallback)
                          );
                            }
                        }

                        if (matchedAny)
                        {
                           // File.Delete(imageFile);
                           // bitmap.Save(Path.Combine(resultDir, Path.GetFileName(imageFile)));
                          //bitmap.Save(Path.Combine(tempResultDir, Path.GetFileName(imageFile)));                       
                            //logCallback?.Invoke($"Processed and saved: {Path.GetFileName(imageFile)}");
                            //InsertMatchFaceLog("", Path.Combine(resultDir, Path.GetFileName(imageFile)), 1, bestMatchID, bestMatchName, bestMatchDistance);
                        }
                        else
                        {
                            File.Delete(imageFile);
                            logCallback?.Invoke($"No match found in {Path.GetFileName(imageFile)}");

                        }
                        try
                        {
                           // File.Delete(imageFile);
                            // logCallback?.Invoke($"Deleted processed file: {Path.GetFileName(imageFile)}");
                        }
                        catch (Exception ex)
                        {
                            logCallback?.Invoke($"Error deleting file {Path.GetFileName(imageFile)}: {ex.Message}");
                        }
                        bitmap.Dispose();
                    }
                    catch (Exception ex)
                    {
                        logCallback?.Invoke($"Error processing image {imageFile}: {ex.Message}");
                    }


                    sw.Stop();
                    logCallback?.Invoke($"Image {Path.GetFileName(imageFile)} processed in {sw.ElapsedMilliseconds} ms");
                }
            }


        private async Task ProcessAndLogMatchAsync(string bestMatch, float bestDistance, string imageFile, Bitmap bitmap, string resultDir, string tempResultDir, Action<string>? logCallback)
        {
            try
            {
                await Task.Run(() =>
                {
                   
                    int bestMatchID = 0;
                    string bestMatchName = "";
                    // Save images
                    bitmap.Save(Path.Combine(resultDir, Path.GetFileName(imageFile)));
                    bitmap.Save(Path.Combine(tempResultDir, Path.GetFileName(imageFile)));
                    //logCallback?.Invoke($"Processed and saved: {Path.GetFileName(imageFile)}");

                    int hyphenIndex = bestMatch.IndexOf('-');
                    if (hyphenIndex > 0)
                    {
                        string idPart = bestMatch.Substring(0, hyphenIndex);
                        bestMatchName = bestMatch.Substring(hyphenIndex + 1);
                        if (int.TryParse(idPart, out int parsedId))
                        {
                            bestMatchID = parsedId;
                        }
                    }

                    string fileName = Path.GetFileName(imageFile);
                    int camId = 0;
                    string frameCaptureTime = "";
                    // Step 2: Extract parts
                    // Remove file extension
                    string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName); // Cam4_frame_20250525_182446_384278
                    var parts = nameWithoutExt.Split('_');
                    try
                    {
                        if (parts.Length >= 3 && parts[0].StartsWith("Cam"))
                        {
                            string camPart = "Cam";                       // "Cam"
                            string idPart = parts[0].Substring(3);        // "4"
                            camId = Convert.ToInt32(idPart);
                            string datePart = $"{parts[2]}_{parts[3]}_{parts[4]}"; // "20250525_182446_384278"

                            // Step 3: Parse datetime
                            if (DateTime.TryParseExact(datePart, "yyyyMMdd_HHmmss_ffffff",
                                                       System.Globalization.CultureInfo.InvariantCulture,
                                                       System.Globalization.DateTimeStyles.None,
                                                       out DateTime parsedDateTime))
                            {
                                frameCaptureTime = Convert.ToString(parsedDateTime);
                            }
                            else
                            {
                                Console.WriteLine("Failed to parse datetime.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                    _logger.LogInformation($"Face Match found from Camera: {camId} Matcher Name: {bestMatchName} with Distance: {bestDistance} ");

                    // Log to database
                    InsertMatchFaceLog(
                        frameCaptureTime,
                        Path.Combine(resultDir, Path.GetFileName(imageFile)),
                        camId,
                        bestMatchID,
                        bestMatchName,
                        bestDistance
                    );

                    // logCallback?.Invoke($"Deleted processed file: {Path.GetFileName(imageFile)}");
                });
            }
                catch (Exception ex)
                {
                    logCallback?.Invoke($"Error during async processing of {Path.GetFileName(imageFile)}: {ex.Message}");
            }
                finally
                {
                File.Delete(imageFile);
                bitmap.Dispose();
                }
        }


        private static Bitmap LoadBitmapUnlocked(string path)
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    return new Bitmap(fs);
            //using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            //using var ms = new MemoryStream();
            //fs.CopyTo(ms);
            //ms.Position = 0;
            //return new Bitmap(ms);
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
                   //new Bitmap(original, new Size(original.Width / 2, original.Height / 2)), // Downscaled
                   FlipHorizontal(original) // Flip
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
        public void InsertMatchFaceLog(string captureTime, string frame, int cctvId, int? suspectId, string suspectName, float distance)
        {
            string formattedCaptureTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
            string formattedCreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                INSERT INTO Matchfacelogs 
                (capture_time, frame, cctv_id, suspect_id, suspect, distance, created_date)
                VALUES 
                (@capture_time, @frame, @cctv_id, @suspect_id, @suspect, @distance, @created_date);
            ";

                    command.Parameters.AddWithValue("@capture_time", formattedCaptureTime);
                    command.Parameters.AddWithValue("@frame", frame);
                    command.Parameters.AddWithValue("@cctv_id", cctvId);
                    command.Parameters.AddWithValue("@suspect_id", (object?)suspectId ?? DBNull.Value); // nullable FK
                    command.Parameters.AddWithValue("@suspect", suspectName ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@distance", distance);
                    command.Parameters.AddWithValue("@created_date", formattedCreatedDate);
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (SqliteException ex)
                    {
                        // Log or rethrow as needed
                        Console.WriteLine($"SQLite error: {ex.Message}");
                        throw;
                    }
                }
            }
        }
        private string GetSuspectName(string suspectid)
        {
            string suspectName = "Unknown";

            try
            {
                using (var connection = new SqliteConnection(connectionString))
                {
                    connection.Open();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                    SELECT first_name 
                    FROM suspects where suspect_id=" + suspectid;

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {

                                suspectName = reader.IsDBNull(0) ? "" : reader.GetString(0); // "firstName" column, null-safe
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle or log the exception as per your application's logging strategy
                Console.Error.WriteLine($"Error loading suspect list: {ex.Message}");
                throw; // Optional: rethrow or return empty dictionary
            }

            return suspectName;
        }


        public void Run(string suspectDir, string imageDir, string resultDir, Action<string> logCallback)
        {
            var stopwatch = Stopwatch.StartNew(); // Start timing
            logCallback?.Invoke("FaceONNX: Optimized Multi-Suspect Augmented Face Matching");

            if (!Directory.Exists(resultDir))
            {
                Directory.CreateDirectory(resultDir);
            }
            else
            {
                Array.ForEach(Directory.GetFiles(resultDir), File.Delete);
            }

            using var faceDetector = new FaceDetector();
            using var faceEmbedder = new FaceEmbedder();

            var suspectEmbeddings = new ConcurrentDictionary<string, float[]>();
            var suspectFiles = Directory.GetFiles(suspectDir);

            // Parallel loading of suspect images
            foreach (var suspectFile in suspectFiles)
            {
                try
                {
                    using var suspectImage = LoadBitmapUnlocked(suspectFile);
                    var faces = faceDetector.Forward(suspectImage);
                    if (faces.Length == 0)
                    {
                        logCallback?.Invoke($"No face found in suspect image: {suspectFile}");
                        continue;
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
            }
            var groupImages = Directory.GetFiles(imageDir);
            using var painter = new Painter() { BoxPen = new Pen(Color.Yellow, 4), Transparency = 0 };

            foreach (var imageFile in groupImages)
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    using var bitmap = LoadBitmapUnlocked(imageFile);
                    using var graphics = Graphics.FromImage(bitmap);

                    var groupFaces = faceDetector.Forward(bitmap);
                    bool matchedAny = false;

                    foreach (var face in groupFaces)
                    {
                        using var crop = CropFace(bitmap, face.Box);
                        var queryAugmentations = GenerateAugmentations(crop);

                        var queryEmbeddings = new List<float[]>();
                        foreach (var img in queryAugmentations)
                        {
                            using (img)
                            {
                                queryEmbeddings.Add(faceEmbedder.Forward(img));
                            }
                        }

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
                            //DrawFaceBox(graphics, face.Box, bestMatch);
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
                    // Delete the processed file
                    try
                    {
                        File.Delete(imageFile);
                        logCallback?.Invoke($"Deleted processed file: {Path.GetFileName(imageFile)}");
                    }
                    catch (Exception ex)
                    {
                        logCallback?.Invoke($"Error deleting file {Path.GetFileName(imageFile)}: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    logCallback?.Invoke($"Error processing image {imageFile}: {ex.Message}");
                }

                sw.Stop();
                logCallback?.Invoke($"Image {Path.GetFileName(imageFile)} processed in {sw.ElapsedMilliseconds} ms");
            }

            stopwatch.Stop();
            logCallback?.Invoke("Face matching complete.");
            logCallback?.Invoke($"\nTotal processing time: {stopwatch.Elapsed.TotalSeconds:F2} seconds");
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
    public static class LogHelper
    {
        public static ILoggerFactory LoggerFactory { get; set; }

        public static ILogger<T> GetLogger<T>() =>
            LoggerFactory?.CreateLogger<T>();
    }
}
