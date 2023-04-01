﻿using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Masuit.MyBlogs.Core.Extensions;

public class PerfCounterFilterAttribute : ActionFilterAttribute
{
    public Stopwatch Stopwatch { get; set; }

    /// <inheritdoc />
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        Stopwatch = Stopwatch.StartNew();
    }

    /// <inheritdoc />
    public override void OnActionExecuted(ActionExecutedContext context)
    {
        context.HttpContext.Response.Headers.Add("X-Action-Time", Stopwatch.ElapsedMilliseconds + "ms");
    }

    /// <inheritdoc />
    public override void OnResultExecuting(ResultExecutingContext context)
    {
        Stopwatch.Restart();
        context.HttpContext.Response.OnStarting(() =>
        {
            context.HttpContext.Response.Headers.Add("X-Result-Time", Stopwatch.ElapsedMilliseconds + "ms");
            return Task.CompletedTask;
        });
    }
}
