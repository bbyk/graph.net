using System;
using System.Diagnostics;
using System.Threading;

namespace Facebook
{
    /// <summary>
    /// A generic base class for IAsyncResult implementations
    /// that wraps a ManualResetEvent.
    /// </summary>
    public class AsyncResult : IAsyncResult
    {
        readonly AsyncCallback _callback;
        readonly object _state;
        bool _completedSynchronously;
        bool _endCalled;
        Exception _exception;
        bool _isCompleted;
        ManualResetEvent _manualResetEvent;
        readonly object _thisLock;

        ///<summary>
        ///</summary>
        ///<param name="callback"></param>
        ///<param name="state"></param>
        public AsyncResult(AsyncCallback callback, object state)
        {
            _callback = callback;
            _state = state;
            _thisLock = new object();
        }

        ///<summary>
        ///</summary>
        public Exception Exception
        {
            get { return _exception; }
        }

        /// <summary>
        /// 
        /// </summary>
        public object AsyncState
        {
            get
            {
                return _state;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public WaitHandle AsyncWaitHandle
        {
            get
            {
                if (_manualResetEvent != null)
                {
                    return _manualResetEvent;
                }

                lock (ThisLock)
                {
                    if (_manualResetEvent == null)
                    {
                        _manualResetEvent = new ManualResetEvent(_isCompleted);
                    }
                }

                return _manualResetEvent;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool CompletedSynchronously
        {
            get
            {
                return _completedSynchronously;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsCompleted
        {
            get
            {
                return _isCompleted;
            }
        }

        object ThisLock
        {
            get
            {
                return _thisLock;
            }
        }

        // Call this version of complete when your asynchronous operation is complete.  This will update the state
        // of the operation and notify the callback.
        ///<summary>
        ///</summary>
        ///<param name="completedSynchronously"></param>
        ///<exception cref="InvalidOperationException"></exception>
        public void Complete(bool completedSynchronously)
        {
            if (_isCompleted)
            {
                // It's a bug to call Complete twice.
                throw new InvalidOperationException("Cannot call Complete twice");
            }

            _completedSynchronously = completedSynchronously;

            if (completedSynchronously)
            {
                // If we completedSynchronously, then there's no chance that the manualResetEvent was created so
                // we don't need to worry about a race
                Debug.Assert(_manualResetEvent == null, "No ManualResetEvent should be created for a synchronous AsyncResult.");
                _isCompleted = true;
            }
            else
            {
                lock (ThisLock)
                {
                    _isCompleted = true;
                    if (_manualResetEvent != null)
                        _manualResetEvent.Set();
                }
            }

            // If the callback throws, there is a bug in the callback implementation
            if (_callback != null)
            {
                _callback(this);
            }
        }

        // Call this version of complete if you raise an exception during processing.  In addition to notifying
        // the callback, it will capture the exception and store it to be thrown during AsyncResult.End.
        ///<summary>
        ///</summary>
        ///<param name="completedSynchronously"></param>
        ///<param name="exception"></param>
        public void Complete(bool completedSynchronously, Exception exception)
        {
            _exception = exception;
            Complete(completedSynchronously);
        }

        // End should be called when the End function for the asynchronous operation is complete.  It
        // ensures the asynchronous operation is complete, and does some common validation.
        ///<summary>
        ///</summary>
        ///<param name="result"></param>
        ///<param name="exConverter"></param>
        ///<typeparam name="TAsyncResult"></typeparam>
        ///<returns></returns>
        ///<exception cref="ArgumentNullException"></exception>
        ///<exception cref="ArgumentException"></exception>
        ///<exception cref="InvalidOperationException"></exception>
        ///<exception cref="Exception"></exception>
        public static TAsyncResult End<TAsyncResult>(IAsyncResult result, Func<Exception, Exception> exConverter)
            where TAsyncResult : AsyncResult
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }

            var asyncResult = result as TAsyncResult;

            if (asyncResult == null)
            {
                throw new ArgumentException("Invalid async result.", "result");
            }

            if (asyncResult._endCalled)
            {
                throw new InvalidOperationException("Async object already ended.");
            }

            asyncResult._endCalled = true;

            if (!asyncResult._isCompleted)
            {
                asyncResult.AsyncWaitHandle.WaitOne();
            }

            if (asyncResult._manualResetEvent != null)
            {
                asyncResult._manualResetEvent.Close();
            }

            if (asyncResult._exception != null)
            {
                throw exConverter == null ? asyncResult._exception : exConverter(asyncResult._exception);
            }

            return asyncResult;
        }


        ///<summary>
        ///</summary>
        ///<param name="func"></param>
        ///<returns></returns>
        public AsyncCallback AsSafe(AsyncCallback func)
        {
            return AsSafe(func, null);
        }

        ///<summary>
        ///</summary>
        ///<param name="func"></param>
        ///<param name="cleanup"></param>
        ///<returns></returns>
        public AsyncCallback AsSafe(AsyncCallback func, Action<Exception> cleanup)
        {
            if (func == null) return func;

            return sar =>
            {
                try
                {
                    func(sar);
                }
                catch (Exception ex)
                {
                    if (cleanup != null)
                        cleanup(ex);
                    if (!IsCompleted)
                        Complete(false, ex);
                }
            };
        }
    }

    //A strongly typed AsyncResult
    ///<summary>
    ///</summary>
    ///<typeparam name="T"></typeparam>
    public class TypedAsyncResult<T> : AsyncResult
    {
        T _data;

        ///<summary>
        ///</summary>
        ///<param name="callback"></param>
        ///<param name="state"></param>
        public TypedAsyncResult(AsyncCallback callback, object state)
            : base(callback, state)
        {
        }

        ///<summary>
        ///</summary>
        public T Data
        {
            get { return _data; }
        }

        ///<summary>
        ///</summary>
        ///<param name="data"></param>
        ///<param name="completedSynchronously"></param>
        public void Complete(T data, bool completedSynchronously)
        {
            _data = data;
            Complete(completedSynchronously);
        }

        ///<summary>
        ///</summary>
        ///<param name="result"></param>
        ///<param name="exConverter"></param>
        ///<returns></returns>
        public static T End(IAsyncResult result, Func<Exception, Exception> exConverter)
        {
            var typedResult = End<TypedAsyncResult<T>>(result, exConverter);
            return typedResult.Data;
        }
    }
}
