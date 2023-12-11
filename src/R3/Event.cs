﻿#pragma warning disable CS0618

using System.Diagnostics;

namespace R3;

// similar as IObservable<T> (only OnNext)
// IDisposable Subscribe(Subscriber<TMessage> subscriber)
public abstract class Event<TMessage>
{
    // [DebuggerStepThrough]
    [StackTraceHidden]
    public IDisposable Subscribe(Subscriber<TMessage> subscriber)
    {
        try
        {
            var subscription = SubscribeCore(subscriber);

            if (SubscriptionTracker.TryTrackActiveSubscription(subscription, 2, out var trackableDisposable))
            {
                subscription = trackableDisposable;
            }

            subscriber.SourceSubscription.Disposable = subscription;
            return subscriber; // return subscriber to make subscription chain.
        }
        catch
        {
            subscriber.Dispose(); // when SubscribeCore failed, auto detach caller subscriber
            throw;
        }
    }

    protected abstract IDisposable SubscribeCore(Subscriber<TMessage> subscriber);
}

// similar as IObserver<T>
// void OnNext(TMessage message);
public abstract class Subscriber<TMessage> : IDisposable
{
#if DEBUG
    [Obsolete("Only allow in Event<TMessage>.")]
#endif
    internal SingleAssignmentDisposableCore SourceSubscription;

    int calledDispose;

    public bool IsDisposed => Volatile.Read(ref calledDispose) != 0;

    public abstract void OnNext(TMessage message);

    [StackTraceHidden]
    [DebuggerStepThrough]
    protected virtual void DisposeCore() { }

    // [DebuggerStepThrough]
    [StackTraceHidden]
    public void Dispose()
    {
        if (Interlocked.Exchange(ref calledDispose, 1) != 0)
        {
            return;
        }

        DisposeCore();                // Dispose self
        SourceSubscription.Dispose(); // Dispose attached parent
    }
}

// similar as IObservable<T>
public abstract class CompletableEvent<TMessage, TComplete>
{
    // [DebuggerStepThrough]
    [StackTraceHidden]
    public IDisposable Subscribe(Subscriber<TMessage, TComplete> subscriber)
    {
        try
        {
            var subscription = SubscribeCore(subscriber);

            if (SubscriptionTracker.TryTrackActiveSubscription(subscription, 2, out var trackableDisposable))
            {
                subscription = trackableDisposable;
            }

            subscriber.SourceSubscription.Disposable = subscription;
            return subscriber; // return subscriber to make subscription chain.
        }
        catch
        {
            subscriber.Dispose(); // when SubscribeCore failed, auto detach caller subscriber
            throw;
        }
    }

    protected abstract IDisposable SubscribeCore(Subscriber<TMessage, TComplete> subscriber);
}


// similar as IObserver<T>
// void OnNext(TMessage message);
// void OnCompleted(TComplete complete);
public abstract class Subscriber<TMessage, TComplete> : IDisposable
{
#if DEBUG
    [Obsolete("Only allow in CompletableEvent<TMessage>.")]
#endif
    internal SingleAssignmentDisposableCore SourceSubscription;

    int calledOnCompleted;
    int disposed;

    public bool IsDisposed => Volatile.Read(ref disposed) != 0;

    public abstract void OnNext(TMessage message);

    // // [DebuggerStepThrough]
    public void OnCompleted(TComplete complete)
    {
        if (Interlocked.Exchange(ref calledOnCompleted, 1) != 0)
        {
            return;
        }

        try
        {
            OnCompletedCore(complete);
        }
        finally
        {
            Dispose();
        }
    }

    protected abstract void OnCompletedCore(TComplete complete);

    [StackTraceHidden]
    [DebuggerStepThrough]
    protected virtual void DisposeCore() { }

    // [DebuggerStepThrough]
    [StackTraceHidden]
    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
        {
            return;
        }

        DisposeCore();                // Dispose self
        SourceSubscription.Dispose(); // Dispose attached parent
    }
}
