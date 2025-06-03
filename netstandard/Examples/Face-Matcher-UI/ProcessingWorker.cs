using FaceONNX;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Face_Matcher_UI
{
    public class ProcessingWorker : IDisposable
    {
        private readonly FaceDetector _faceDetector;
        private readonly FaceEmbedder _faceEmbedder;
        private readonly FaceMatcher _faceMatcher;
        private readonly CancellationToken _token;
        private readonly Task _processingTask;

        private readonly BlockingCollection<string> _imageQueue = new();

        private readonly Dictionary<string, float[]> _suspectEmbeddings;
        private readonly string _resultDir;
        private readonly string _tempResultDir;
        private readonly Action<string> _logCallback;

        public ProcessingWorker(
            Dictionary<string, float[]> suspectEmbeddings,
            string resultDir,
            string tempResultDir,
            Action<string> logCallback,
            CancellationToken token)
        {
            _faceDetector = new FaceDetector();
            _faceEmbedder = new FaceEmbedder();
            _faceMatcher = new FaceMatcher();

            _suspectEmbeddings = suspectEmbeddings;
            _resultDir = resultDir;
            _tempResultDir = tempResultDir;
            _logCallback = logCallback;
            _token = token;

            _processingTask = Task.Run(ProcessLoop, token);
        }

        public void EnqueueImage(string imageFile)
        {
            _imageQueue.Add(imageFile);
        }
        private async Task ProcessLoop()
        {
            while (!_token.IsCancellationRequested)
            {
                if (_imageQueue.TryTake(out var imageFile, TimeSpan.FromMilliseconds(500)))
                {
                    try
                    {
                        _faceMatcher.RunBatch(_suspectEmbeddings, new[] { imageFile }, _resultDir, _tempResultDir, _logCallback, _faceDetector, _faceEmbedder);
                    }
                    catch (Exception ex)
                    {
                        _logCallback?.Invoke($"Error processing file {imageFile}: {ex.Message}");
                    }
                }
            }
        }
        private async Task ProcessLoop_backup()
        {
            List<string> currentBatch = new();

            DateTime lastBatchTime = DateTime.UtcNow;

            while (!_token.IsCancellationRequested)
            {
                bool newItemAdded = false;

                while (_imageQueue.TryTake(out var imageFile))
                {
                    currentBatch.Add(imageFile);
                    newItemAdded = true;

                    if (currentBatch.Count >= 2000)
                        break;
                }

                bool timeToFlush = (DateTime.UtcNow - lastBatchTime).TotalSeconds >= 1;

                if (currentBatch.Count > 0 && (currentBatch.Count >= 2000 || timeToFlush))
                {
                    _faceMatcher.RunBatch(_suspectEmbeddings, currentBatch.ToArray(), _resultDir, _tempResultDir, _logCallback, _faceDetector, _faceEmbedder);
                    currentBatch.Clear();
                    lastBatchTime = DateTime.UtcNow;
                }

                if (!newItemAdded)
                    await Task.Delay(500);
            }
        }

        public void Dispose()
        {
            _imageQueue.CompleteAdding();
            _processingTask.Wait();
            _faceEmbedder?.Dispose();
            // Dispose other resources if needed
        }
    }

}
