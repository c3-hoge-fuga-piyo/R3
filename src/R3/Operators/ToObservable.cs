﻿namespace R3;

public static partial class EventExtensions
{
    // TODO: more overload?
    public static IObservable<T> ToObservable<T>(this Event<T> source)
    {
        return new ToObservable<T>(source);
    }
}

internal sealed class ToObservable<T>(Event<T> source) : IObservable<T>
{
    public IDisposable Subscribe(IObserver<T> observer)
    {
        return source.Subscribe(new ObserverToSubscriber(observer));
    }

    sealed class ObserverToSubscriber(IObserver<T> observer) : Subscriber<T>
    {
        protected override void OnNextCore(T value)
        {
            observer.OnNext(value);
        }

        protected override void OnErrorResumeCore(Exception error)
        {
            observer.OnError(error);
        }

        protected override void OnCompletedCore(Result result)
        {
            if (result.IsFailure)
            {
                observer.OnError(result.Exception);
            }
            else
            {
                observer.OnCompleted();
            }
        }
    }
}
