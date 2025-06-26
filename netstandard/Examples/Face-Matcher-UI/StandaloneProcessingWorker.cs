using FaceONNX;
using Model;
using System;
using System.Collections.Concurrent;
using UMapx.Window;
//using System.Collections.Generic;
//using System.Text;
//using static Face_Matcher_UI.Form1;

namespace Face_Matcher_UI
{
    public class StandaloneProcessingWorker : IDisposable
    {
        private readonly FaceDetector _faceDetector;
        private readonly FaceEmbedder _faceEmbedder;
        private readonly SuspectMatcher _faceMatcher;
        private readonly CancellationToken _token;
        private readonly Task _processingTask;

       private readonly BlockingCollection<string> _imageQueue = new();
       private readonly BlockingCollection<ImageFrame> _videoImageQueue = new();

        private readonly Dictionary<string, float[]> _suspectEmbeddings;
        private readonly string _resultDir;
        private readonly string _tempResultDir;
        private readonly Action<string> _logCallback;
        private volatile bool _isRunning = false;
        public bool IsRunning => _isRunning;

        public StandaloneProcessingWorker(
            Dictionary<string, float[]> suspectEmbeddings,
            string resultDir,
            string tempResultDir,
            Action<string> logCallback,
            CancellationToken token, FaceDetector sharedFaceDetector, FaceEmbedder sharedFaceEmbedder)
        {
            _faceDetector = sharedFaceDetector;
           _faceEmbedder = sharedFaceEmbedder;
            //_faceDetector = new FaceDetector();
            //_faceEmbedder = new FaceEmbedder();
            _faceMatcher = new SuspectMatcher();

            //var detectorTask = Task.Run(() => new FaceDetector());
            //var embedderTask = Task.Run(() => new FaceEmbedder());
            //var matcherTask = Task.Run(() => new FaceMatcher());

            //Task.WaitAll(detectorTask, embedderTask, matcherTask);

            //_faceDetector = detectorTask.Result;
            //_faceEmbedder = embedderTask.Result;
            //_faceMatcher = matcherTask.Result;

            _suspectEmbeddings = suspectEmbeddings;
            _resultDir = resultDir;
            _tempResultDir = tempResultDir;
            _logCallback = logCallback;
            _token = token;
            if(ExecutionProviderManager.CurrentExecutionProvider == ExecutionProviderManager.CUDA)
            {
                _processingTask = Task.Run(ProcessLoop_CUDA, token);
            }
            else
            {
                _processingTask = Task.Run(ProcessLoop, token);
            }
            
        }

        public void EnqueueImage(string frame)
        {
            _imageQueue.Add(frame);
        }

        private async Task ProcessLoop()
        {
            const int MaxBatchSize = 16;
            const int DelayInterval = 10; // ms

            var batch = new List<string>(MaxBatchSize);

            while (!_token.IsCancellationRequested)
            {
                try
                {
                    while (batch.Count < MaxBatchSize &&
                           _imageQueue.TryTake(out var imageFile, TimeSpan.FromMilliseconds(DelayInterval)))
                    {
                        batch.Add(imageFile);
                    }

                    if (batch.Count > 0)
                    {


                            _faceMatcher.RunBatch(_suspectEmbeddings, batch.ToArray(),
                            _resultDir, _tempResultDir, _logCallback, _faceDetector, _faceEmbedder);
                            batch.Clear();
                    
                    }
                    await Task.Delay(5, _token);
                }
                catch (OperationCanceledException)
                {
                    _logCallback?.Invoke("Processing loop canceled.");
                    break; // Gracefully exit loop
                }
                catch (ObjectDisposedException ex)
                {
                    _logCallback?.Invoke($"Object disposed during processing: {ex.Message}");
                    break;
                }
                catch (Exception ex)
                {
                    _logCallback?.Invoke($"Worker loop error: {ex.Message}");
                }
                //await Task.Delay(50, _token);
            }
        }
        private async Task ProcessLoop_CUDA()
        {
            const int MaxBatchSize = 16;
            const int DelayInterval = 10; // ms

            var batch = new List<string>(MaxBatchSize);

            while (!_token.IsCancellationRequested)
            {
                try
                {
                    while (batch.Count < MaxBatchSize &&
                           _imageQueue.TryTake(out var imageFile, TimeSpan.FromMilliseconds(DelayInterval)))
                    {
                        batch.Add(imageFile);
                    }

                    if (batch.Count > 0)
                    {
                        _faceMatcher.RunBatch_CUDA(_suspectEmbeddings, batch.ToArray(),
                            _resultDir, _tempResultDir, _logCallback, _faceDetector, _faceEmbedder);
                        batch.Clear();
                    }
                }
                catch (Exception ex)
                {
                    _logCallback?.Invoke($"Worker loop error: {ex.Message}");
                }
            }
        }



        public void EnqueueVideoImage(ImageFrame? frame,string s)
        {
            _videoImageQueue.Add(frame);
        }
        private async Task ProcessLoop_VIdeo()
        {
            const int MaxBatchSize = 8;
            const int DelayInterval = 10;

            var batch = new List<ImageFrame>(MaxBatchSize);

            while (!_token.IsCancellationRequested)
            {
                try
                {
                    while (batch.Count < MaxBatchSize &&
                           _videoImageQueue.TryTake(out var frame, TimeSpan.FromMilliseconds(DelayInterval)))
                    {
                        batch.Add(frame);
                    }

                    if (batch.Count > 0)
                    {
                     //   _faceMatcher.RunBatch(_suspectEmbeddings, batch, _resultDir, _tempResultDir, _logCallback, _faceDetector, _faceEmbedder);
                        batch.Clear();
                    }
                }
                catch (Exception ex)
                {
                    _logCallback?.Invoke($"Worker loop error: {ex.Message}");
                }
            }
        }


        //public void EnqueueImage(string imageFile)
        //{
        //    _imageQueue.Add(imageFile);
        //}
        //private async Task ProcessLoop()
        //{
        //    while (!_token.IsCancellationRequested)
        //    {
        //        if (_imageQueue.TryTake(out var imageFile, TimeSpan.FromMilliseconds(10)))
        //        {
        //            try
        //            {
        //                _faceMatcher.RunBatch(_suspectEmbeddings, new[] { imageFile }, _resultDir, _tempResultDir, _logCallback, _faceDetector, _faceEmbedder);
        //            }
        //            catch (Exception ex)
        //            {
        //                _logCallback?.Invoke($"Error processing file {imageFile}: {ex.Message}");
        //            }
        //        }
        //    }
        //}



        //private async Task ProcessLoop_bakup_Latest()
        //{
        //    while (!_token.IsCancellationRequested)
        //    {
        //        if (_imageQueue.TryTake(out var imageFile, TimeSpan.FromMilliseconds(500)))
        //        {
        //            try
        //            {
        //                _faceMatcher.RunBatch(_suspectEmbeddings, new[] { imageFile }, _resultDir, _tempResultDir, _logCallback, _faceDetector, _faceEmbedder);
        //            }
        //            catch (Exception ex)
        //            {
        //                _logCallback?.Invoke($"Error processing file {imageFile}: {ex.Message}");
        //            }
        //        }
        //    }
        //}


        public async void Dispose()
        {
            _imageQueue.CompleteAdding();
            await _processingTask.ConfigureAwait(false); // If you make Dispose async
            _faceEmbedder?.Dispose();
            // Dispose other resources if needed
        }

        //private async Task ProcessLoop_backup()
        //{
        //    List<string> currentBatch = new();

        //    DateTime lastBatchTime = DateTime.UtcNow;

        //    while (!_token.IsCancellationRequested)
        //    {
        //        bool newItemAdded = false;

        //        while (_imageQueue.TryTake(out var imageFile))
        //        {
        //            currentBatch.Add(imageFile);
        //            newItemAdded = true;

        //            if (currentBatch.Count >= 2000)
        //                break;
        //        }

        //        bool timeToFlush = (DateTime.UtcNow - lastBatchTime).TotalSeconds >= 1;

        //        if (currentBatch.Count > 0 && (currentBatch.Count >= 2000 || timeToFlush))
        //        {
        //            _faceMatcher.RunBatch(_suspectEmbeddings, currentBatch.ToArray(), _resultDir, _tempResultDir, _logCallback, _faceDetector, _faceEmbedder);
        //            currentBatch.Clear();
        //            lastBatchTime = DateTime.UtcNow;
        //        }

        //        if (!newItemAdded)
        //            await Task.Delay(500);
        //    }
        //}
    }
}
