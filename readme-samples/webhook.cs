// Harness for the README's ASP.NET Core webhook endpoint: `app` and the signing `secret` are in scope.
using System;
using DesktopAccountingApi.QuickBooksDesktop;
using Microsoft.AspNetCore.Builder;

internal static class {{NAME}}
{
    public static void Map(WebApplication app, string secret)
    {
        {{SAMPLE}}
    }
}
