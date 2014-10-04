using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel;

namespace BZSpriteEditor.Controls
{

    /// <summary>
    /// This is thread-safe
    /// </summary>
    public class QueuedBackgroundWorker
    {
        #region Constructors

        public QueuedBackgroundWorker() { }

        #endregion

        #region Properties

        Queue<object> Queue = new Queue<object>();

        object lockingObject1 = new object();

        public delegate void WorkerDoWork(object sender, DoWorkEventArgs e);
        public delegate void WorkerReportProgress(object sender, ProgressChangedEventArgs e);
        public delegate void WorkerCompletedDelegate(object sender, RunWorkerCompletedEventArgs e);

        #endregion

        #region Methods

        /// <summary>
        /// doWork is a method with one argument
        /// </summary>
        /// <typeparam name="T">is the type of the input parameter</typeparam>
        /// <typeparam name="K">is the type of the output result</typeparam>
        /// <param name="inputArgument"></param>
        /// <param name="doWork"></param>
        /// <param name="workerCompleted"></param>
        public void RunAsync(WorkerDoWork doWork, WorkerCompletedDelegate workerCompleted = null, WorkerReportProgress workerProgress = null)
        {
            BackgroundWorker bw = GetBackgroundWorker(doWork, workerCompleted, workerProgress);

            //current = bw;

            //if (Canceling)
            //{
            //    //bw.CancelAsync();
            //}

            Queue.Enqueue(new QueueItem(bw));

            lock (lockingObject1)
            {
                if (Queue.Count == 1)
                {
                    ((QueueItem)this.Queue.Peek()).RunWorkerAsync();
                }
            }
        }

        private BackgroundWorker GetBackgroundWorker(WorkerDoWork doWork, WorkerCompletedDelegate workerCompleted, WorkerReportProgress workerProgress)
        {
            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            bw.WorkerSupportsCancellation = true;

            bw.DoWork += (sender, args) =>
            {
                if (doWork != null)
                {
                    doWork(sender, args);
                }
            };

            bw.ProgressChanged += (sender, args) =>
            {
                if (workerProgress != null)
                {
                    workerProgress(sender, args);
                }
            };

            bw.RunWorkerCompleted += (sender, args) =>
            {
                if (workerCompleted != null)
                {
                    workerCompleted(sender, args);
                }
                Queue.Dequeue();
                lock (lockingObject1)
                {
                    if (Queue.Count > 0)
                    {
                        ((QueueItem)this.Queue.Peek()).RunWorkerAsync();
                    }
                }
            };

            return bw;
        }

        #endregion
    }

    public class QueueItem
    {
        #region Constructors

        public QueueItem(BackgroundWorker backgroundWorker)
        {
            this.BackgroundWorker = backgroundWorker;
        }

        #endregion

        #region Properties

        public BackgroundWorker BackgroundWorker { get; private set; }

        #endregion

        #region Methods

        public void RunWorkerAsync()
        {
            this.BackgroundWorker.RunWorkerAsync();
        }

        #endregion
    }
}