﻿using Microsoft.Extensions.Time.Testing;

namespace R3.Tests.FactoryTests;

public class EmptyTest
{
    [Fact]
    public void Empty()
    {
        using var list = Event.Empty<int>().ToLiveList();
        list.AssertIsCompleted();
    }

    [Fact]
    public void EmptyWithTime()
    {
        var fakeTime = new FakeTimeProvider();
        using var list = Event.Empty<int>(TimeSpan.FromSeconds(5), fakeTime).ToLiveList();

        fakeTime.Advance(TimeSpan.FromSeconds(4));
        list.AssertIsNoResulted();

        fakeTime.Advance(TimeSpan.FromSeconds(1));
        list.AssertIsCompleted();
    }
}
