﻿namespace R3;

public static partial class ObservableExtensions
{
    public static Observable<T> Debounce<T>(this Observable<T> source, TimeSpan timeSpan)
    {
        return new Debounce<T>(source, timeSpan, ObservableSystem.DefaultTimeProvider);
    }

    public static Observable<T> Debounce<T>(this Observable<T> source, TimeSpan timeSpan, TimeProvider timeProvider)
    {
        return new Debounce<T>(source, timeSpan, timeProvider);
    }
}


internal sealed class Debounce<T>(Observable<T> source, TimeSpan timeSpan, TimeProvider timeProvider) : Observable<T>
{
    protected override IDisposable SubscribeCore(Observer<T> observer)
    {
        return source.Subscribe(new _Debounce(observer, timeSpan.Normalize(), timeProvider));
    }

    sealed class _Debounce : Observer<T>
    {
        static readonly TimerCallback timerCallback = RaiseOnNext;

        readonly Observer<T> observer;
        readonly TimeSpan timeSpan;
        readonly ITimer timer;
        readonly object gate = new object();
        T? latestValue;
        bool hasvalue;
        int timerId;

        public _Debounce(Observer<T> observer, TimeSpan timeSpan, TimeProvider timeProvider)
        {
            this.observer = observer;
            this.timeSpan = timeSpan;
            this.timer = timeProvider.CreateStoppedTimer(timerCallback, this);
        }

        protected override void OnNextCore(T value)
        {
            lock (gate)
            {
                latestValue = value;
                hasvalue = true;
                Volatile.Write(ref timerId, unchecked(timerId + 1));
                timer.InvokeOnce(timeSpan); // restart timer
            }
        }

        protected override void OnErrorResumeCore(Exception error)
        {
            observer.OnErrorResume(error);
        }

        protected override void OnCompletedCore(Result result)
        {
            lock (gate)
            {
                if (hasvalue)
                {
                    observer.OnNext(latestValue!);
                    hasvalue = false;
                    latestValue = default;
                }
            }
            observer.OnCompleted(result);
        }

        protected override void DisposeCore()
        {
            timer.Dispose();
        }

        static void RaiseOnNext(object? state)
        {
            var self = (_Debounce)state!;

            var timerId = Volatile.Read(ref self.timerId);
            lock (self.gate)
            {
                if (timerId != self.timerId) return;
                if (!self.hasvalue) return;

                self.observer.OnNext(self.latestValue!);
                self.hasvalue = false;
                self.latestValue = default;
            }
        }
    }
}