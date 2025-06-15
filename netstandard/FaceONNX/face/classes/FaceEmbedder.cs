using FaceONNX.Properties;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using UMapx.Core;
using UMapx.Imaging;

namespace FaceONNX
{
    /// <summary>
    /// Defines face embedder.
    /// </summary>
    public class FaceEmbedder : IFaceClassifier
    {
        #region Private data
        /// <summary>
        /// Inference session.
        /// </summary>
        private readonly InferenceSession _session;
        #endregion
        private static readonly object _sessionLock = new object();
        #region Constructor

        /// <summary>
        /// Initializes face embedder.
        /// </summary>
        public FaceEmbedder()
        {
            var options = new SessionOptions();
            options.EnableMemoryPattern = true;
            options.EnableCpuMemArena = true;
            options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
            options.ExecutionMode = ExecutionMode.ORT_PARALLEL;
            options.IntraOpNumThreads = Environment.ProcessorCount;
            int batchSize = 1;
            //options.EnableProfiling = true;
            //options.AppendExecutionProvider_CPU();
            //options.AppendExecutionProvider_DML(); // ✅ DirectML instead of CUDA
            // options.AppendExecutionProvider_CPU();
            //options.AppendExecutionProvider_CUDA();
            if (TrySetExecutionProvider(options, ExecutionProviderManager.CUDA))
            {
                Console.WriteLine("Using CUDA execution provider.");
                ExecutionProviderManager.SetExecutionProvider(ExecutionProviderManager.CUDA);
            }
            else if (TrySetExecutionProvider(options, ExecutionProviderManager.DirectML))
            {
                Console.WriteLine("Using DirectML execution provider.");
                ExecutionProviderManager.SetExecutionProvider(ExecutionProviderManager.DirectML);
            }
            else
            {
                Console.WriteLine("Using CPU execution provider.");
                ExecutionProviderManager.SetExecutionProvider(ExecutionProviderManager.CPU);
                options.AppendExecutionProvider_CPU();
            }

            var modelPath = Path.Combine(AppContext.BaseDirectory, "recognition_resnet27_fully_dynamic_batch.onnx");
            _session = new InferenceSession(modelPath, options);

            // _session = new InferenceSession(Resources.recognition_resnet27, options);

            //var dummyInput = new DenseTensor<float>(new float[1 * 3 * 128 * 128], new[] { 1, 3, 128, 128 });
            //var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("input", dummyInput) };
            //_session.Run(inputs).ToList().ForEach(o => o.Dispose());

            
            var dummyInput = new DenseTensor<float>(new float[batchSize * 3 * 128 * 128], new[] { batchSize, 3, 128, 128 });
            var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("input", dummyInput) };

            _session.Run(inputs).ToList().ForEach(o => o.Dispose());
        }
        private bool TrySetExecutionProvider(SessionOptions options, string provider)
        {
            try
            {
                // Attempt to append the provider to the session options
                switch (provider)
                {
                    case ExecutionProviderManager.CUDA:
                        options.AppendExecutionProvider_CUDA();
                        return true;
                    case ExecutionProviderManager.DirectML:
                        options.AppendExecutionProvider_DML();
                        return true;
                    case ExecutionProviderManager.CPU:
                        options.AppendExecutionProvider_CPU();
                        return true;
                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                // If we catch an exception, it means the provider is not available
                Console.WriteLine($"Error appending {provider} provider: {ex.Message}");
                return false;
            }
        }
        /// <summary>
        /// Initializes face embedder.
        /// </summary>
        /// <param name="options">Session options</param>
        public FaceEmbedder(SessionOptions options)
        {
            _session = new InferenceSession(Resources.recognition_resnet27, options);
        }

        #endregion

        #region Methods
        /// <summary>
        /// Returns size of the embedding vector.
        /// </summary>
        public static int EmbeddingSize => 512;

        /// <inheritdoc/>
        public float[] Forward(Bitmap image)
        {
            var rgb = image.ToRGB(false);
            return Forward(rgb);
        }

        /// <inheritdoc/>
        public float[] Forward(float[][,] image)
        {
            if (image.Length != 3)
                throw new ArgumentException("Image must be in BGR terms");

            var size = new Size(128, 128);
            var resized = new float[3][,];

            for (int i = 0; i < image.Length; i++)
            {
                resized[i] = image[i].Resize(size.Height, size.Width, InterpolationMode.Bilinear);
            }

            var inputMeta = _session.InputMetadata;
            var name = inputMeta.Keys.ToArray()[0];

            // pre-processing
            var dimentions = new int[] { 1, 3, size.Height, size.Width };
            var tensors = resized.ToFloatTensor(false);
            tensors.Compute(new float[] { 127.5f, 127.5f, 127.5f }, Matrice.Sub);
            tensors.Compute(128, Matrice.Div);
            var inputData = tensors.Merge(true);

            // session run
            var t = new DenseTensor<float>(inputData, dimentions);
            var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input", new DenseTensor<float>(inputData, new[] { 1, 3, 128, 128 }))
        };
            // var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(name, t) };

            IReadOnlyCollection<DisposableNamedOnnxValue> outputs;

            lock (_sessionLock)
            {
                outputs = _session.Run(inputs);
            }
            var results = outputs.ToArray();  // Convert to array if needed
            var length = results.Length;
            var confidences = results[length - 1].AsTensor<float>().ToArray();

            // Clean up ONNX outputs
            foreach (var output in outputs)
            {
                output.Dispose();
            }

            return confidences;
            //lock (_sessionLock)
            //{
            //    using var outputs = _session.Run(inputs);
            //    var results = outputs.ToArray();
            //    var length = results.Length;
            //    var confidences = results[length - 1].AsTensor<float>().ToArray();
            //    return confidences;
            //}

            //return confidences;
        }

        public List<float[]> ForwardBatch(List<Bitmap> images)
        {
            var inputList = new List<float>();

            foreach (var image in images)
            {
                var rgb = image.ToRGB(false);
                var resized = new float[3][,];
                for (int i = 0; i < 3; i++)
                    resized[i] = rgb[i].Resize(128, 128, InterpolationMode.Bilinear);

                var tensor = resized.ToFloatTensor(false);
                tensor.Compute(new float[] { 127.5f, 127.5f, 127.5f }, Matrice.Sub);
                tensor.Compute(128, Matrice.Div);
                inputList.AddRange(tensor.Merge(true));
            }

            var batchSize = images.Count;
            var inputTensor = new DenseTensor<float>(inputList.ToArray(), new[] { batchSize, 3, 128, 128 });
            var inputs = new List<NamedOnnxValue>
    {
        NamedOnnxValue.CreateFromTensor("input", inputTensor)
    };

            IReadOnlyCollection<DisposableNamedOnnxValue> results;
            lock (_sessionLock)
            {
                results = _session.Run(inputs);
            }

            var output = results.First().AsTensor<float>();
            var embeddingList = new List<float[]>();

            for (int i = 0; i < batchSize; i++)
            {
                var slice = output.Skip(i * FaceEmbedder.EmbeddingSize).Take(FaceEmbedder.EmbeddingSize).ToArray();
                embeddingList.Add(slice);
            }

            foreach (var result in results)
                result.Dispose();

            return embeddingList;
        }


        #endregion

        #region IDisposable

        private bool _disposed;

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
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

        /// <summary>
        /// Destructor.
        /// </summary>
        ~FaceEmbedder()
        {
            Dispose(false);
        }

        #endregion
    }
}
