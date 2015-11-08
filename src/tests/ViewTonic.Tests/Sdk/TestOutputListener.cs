// <copyright file="EventStore.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic.Tests.Sdk
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using Xunit.Abstractions;

    public class TestOutputListener : TraceListener
    {
        private readonly ITestOutputHelper testOutputHelper;

        public TestOutputListener(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

        public override void Write(string message)
        {
            this.testOutputHelper.WriteLine(string.Format("{2} [{0}] {1}", Thread.CurrentThread.ManagedThreadId, message, DateTime.Now.ToString("m.s.fff")));
        }

        public override void WriteLine(string message)
        {
            this.Write(message);
        }
    }
}
