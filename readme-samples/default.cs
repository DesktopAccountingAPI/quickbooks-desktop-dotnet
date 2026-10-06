// Harness for README fragments (scripts/readme-samples.mjs): each fragment becomes the body of
// {{NAME}}.RunAsync, with these names in scope. Not part of the solution.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DesktopAccountingApi.QuickBooksDesktop;
using DesktopAccountingApi.QuickBooksDesktop.Models;

internal static class {{NAME}}
{
    private static void ShowToEndUser(string message) => Console.WriteLine(message);

    public static async Task RunAsync(DesktopAccountingApiClient client)
    {
        {{SAMPLE}}
    }
}
