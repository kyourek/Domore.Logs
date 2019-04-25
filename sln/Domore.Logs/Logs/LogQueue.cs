﻿using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Domore.Logs {
    class LogQueue : IDisposable {
        Thread Thread;
        readonly object ThreadLocker = new object();
        readonly BlockingCollection<Item> Collection = new BlockingCollection<Item>();

        void ThreadStart() {
            for (; ; ) {
                var item = default(Item);
                try {
                    item = Collection.Take();
                }
                catch (Exception ex) {
                    if (ex is ObjectDisposedException) {
                        break;
                    }
                    if (ex is InvalidOperationException && Collection.IsAddingCompleted) {
                        break;
                    }
                    throw;
                }

                var handler = item.Handler;
                var message = item.Message;
                var severity = item.Severity;
                try {
                    handler.HandleWork(message, severity);
                }
                catch (Exception ex) {
                    handler.Error = ex;
                }
            }
        }

        bool Complete(TimeSpan? timeout) {
            Collection.CompleteAdding();

            if (Thread != null) {
                lock (ThreadLocker) {
                    if (Thread != null) {
                        if (timeout.HasValue) {
                            return Thread.Join(timeout.Value);
                        }
                        Thread.Join();
                        return true;
                    }
                }
            }

            return true;
        }

        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                Collection.Dispose();
            }
        }

        public void Add(Item item) {
            try {
                Collection.Add(item);
            }
            catch (Exception ex) {
                if (ex is ObjectDisposedException) {
                    return;
                }
                if (ex is InvalidOperationException && Collection.IsAddingCompleted) {
                    return;
                }
                throw;
            }

            if (Thread == null) {
                ThreadPool.QueueUserWorkItem(_ => {
                    lock (ThreadLocker) {
                        if (Thread == null) {
                            Thread = new Thread(ThreadStart);
                            Thread.Name = GetType().FullName;
                            Thread.IsBackground = true;
                            Thread.Start();
                        }
                    }
                });
            }
        }

        public void Complete() {
            Complete(null);
        }

        public bool Complete(TimeSpan timeout) {
            return Complete(new TimeSpan?(timeout));
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~LogQueue() {
            Dispose(false);
        }

        public class Item {
            public string Message { get; }
            public Handler Handler { get; }
            public LogSeverity Severity { get; }

            public Item(Handler handler, string message, LogSeverity severity) {
                Handler = handler;
                Message = message;
                Severity = severity;
            }
        }

        public abstract class Handler : LogHandler {
            public Exception Error { get; set; }
            public LogQueue Queue { get; set; }

            public sealed override void Handle(string message, LogSeverity severity) {
                var queue = Queue;
                if (queue == null) throw new InvalidOperationException("Queue is null.");

                queue.Add(new Item(this, message, severity));
            }

            public abstract void HandleWork(string message, LogSeverity severity);
        }
    }
}
