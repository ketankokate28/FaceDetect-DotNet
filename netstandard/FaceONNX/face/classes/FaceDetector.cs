using FaceONNX.Properties;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UMapx.Core;
using UMapx.Imaging;

namespace FaceONNX
{
    /// <summary>
    /// Defines face detector using YOLOv5 ONNX model.
    /// </summary>
    public class FaceDetector : IFaceDetector, IDisposable
    {
        private readonly InferenceSession _session;
        private bool _disposed;

        public float DetectionThreshold { get; set; }
        public float ConfidenceThreshold { get; set; }
        public float NmsThreshold { get; set; }

        public static readonly string[] Labels = new[] { "Face" };

        public FaceDetector(float detectionThreshold = 0.5f, float confidenceThreshold = 0.4f, float nmsThreshold = 0.5f)
            : this(new SessionOptions(), detectionThreshold, confidenceThreshold, nmsThreshold) { }

        public FaceDetector(SessionOptions options, float detectionThreshold = 0.5f, float confidenceThreshold = 0.4f, float nmsThreshold = 0.5f)
        {
            options.AppendExecutionProvider_DML();  // Enable DirectML

            _session = new InferenceSession(Resources.yolov5s_face, options);
            DetectionThreshold = detectionThreshold;
            ConfidenceThreshold = confidenceThreshold;
            NmsThreshold = nmsThreshold;
        }

        public FaceDetectionResult[] Forward(Bitmap image)
        {
            return Forward(image.ToRGB(false));
        }

        public FaceDetectionResult[] Forward(float[][,] image)
        {
            if (image.Length != 3)
                throw new ArgumentException("Image must have 3 channels (BGR).");

            int originalWidth = image[0].GetLength(1);
            int originalHeight = image[0].GetLength(0);
            var targetSize = new Size(640, 640);
            var resized = ResizePreserved(image, targetSize);

            int yoloVectorLength = 15;
            int classCount = Labels.Length;
            int predictionLength = yoloVectorLength + classCount;

            var inputMeta = _session.InputMetadata;
            var inputName = inputMeta.Keys.First();

            var tensorData = resized.ToFloatTensor(true);
            tensorData.Compute(255f, Matrice.Div);
            var inputArray = tensorData.Merge(true);

            var tensor = new DenseTensor<float>(inputArray, new[] { 1, 3, targetSize.Height, targetSize.Width });
            var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(inputName, tensor) };

            using var results = _session.Run(inputs);
            var resultTensor = results.FirstOrDefault()?.AsTensor<float>();
            if (resultTensor == null)
                return Array.Empty<FaceDetectionResult>();

            var resultArray = resultTensor.ToArray();
            int resultCount = resultArray.Length / predictionLength;

            var detections = new List<float[]>();

            for (int i = 0; i < resultCount; i++)
            {
                var score = resultArray[i * predictionLength + 4];
                if (score <= DetectionThreshold)
                    continue;

                var det = new float[predictionLength];
                for (int j = 0; j < predictionLength; j++)
                    det[j] = resultArray[i * predictionLength + j];

                // Convert center x, y, w, h to x1, y1, x2, y2
                float cx = det[0], cy = det[1], w = det[2], h = det[3];
                det[0] = cx - w / 2;
                det[1] = cy - h / 2;
                det[2] = cx + w / 2;
                det[3] = cy + h / 2;

                detections.Add(det);
            }

            var filtered = NonMaxSuppressionExensions.AgnosticNMSFiltration(detections, NmsThreshold);
            return PostProcess(filtered, originalWidth, originalHeight, targetSize, yoloVectorLength, classCount);
        }

        private FaceDetectionResult[] PostProcess(List<float[]> detections, int originalWidth, int originalHeight, Size resizedSize, int yoloVectorLength, int classCount)
        {
            var results = new List<FaceDetectionResult>();

            float scaleX = (float)resizedSize.Width / originalWidth;
            float scaleY = (float)resizedSize.Height / originalHeight;
            float scale = Math.Min(scaleX, scaleY);
            float padX = (resizedSize.Width - originalWidth * scale) / 2f;
            float padY = (resizedSize.Height - originalHeight * scale) / 2f;

            foreach (var det in detections)
            {
                var classScores = new float[classCount];
                Array.Copy(det, yoloVectorLength, classScores, 0, classCount);
                float maxScore = Matrice.Max(classScores, out int classIndex);

                if (maxScore > ConfidenceThreshold)
                {
                    Rectangle rect = Rectangle.FromLTRB(
                        (int)((det[0] - padX) / scale),
                        (int)((det[1] - padY) / scale),
                        (int)((det[2] - padX) / scale),
                        (int)((det[3] - padY) / scale)
                    );

                    var points = new Point[5];
                    for (int i = 0; i < 5; i++)
                    {
                        points[i] = new Point
                        {
                            X = (int)((det[5 + 2 * i] - padX) / scale),
                            Y = (int)((det[5 + 2 * i + 1] - padY) / scale)
                        };
                    }

                    results.Add(new FaceDetectionResult
                    {
                        Rectangle = rect,
                        Id = classIndex,
                        Score = maxScore,
                        Points = new Face5Landmarks(points)
                    });
                }
            }

            return results.ToArray();
        }

        private static float[][,] ResizePreserved(float[][,] image, Size targetSize)
        {
            var resized = new float[3][,];
            for (int i = 0; i < image.Length; i++)
            {
                resized[i] = image[i].ResizePreserved(targetSize.Height, targetSize.Width, 0.0f, InterpolationMode.Bilinear);
            }
            return resized;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _session?.Dispose();
                }
                _disposed = true;
            }
        }

        ~FaceDetector()
        {
            Dispose(false);
        }
    }
}
