// Quickstart: check the end user's QuickBooks Desktop connection, then list the first 10 invoices.
//
//   DAAPI_SECRET_KEY=sk_test_... DAAPI_END_USER_ID=eu_... dotnet run --project examples/Quickstart

using System;
using DesktopAccountingApi.QuickBooksDesktop;
using DesktopAccountingApi.QuickBooksDesktop.Models;

var endUserId = Environment.GetEnvironmentVariable("DAAPI_END_USER_ID")
    ?? throw new InvalidOperationException("Set DAAPI_END_USER_ID to the end user (eu_...) whose company file to use.");

// Reads DAAPI_SECRET_KEY (and DAAPI_BASE_URL, if set).
using var client = new DesktopAccountingApiClient(new ClientOptions { EndUserId = endUserId });

try
{
    var health = await client.Qbd.HealthCheckAsync();
    Console.WriteLine($"Connected to \"{health.Quickbooks.CompanyName}\" ({health.Quickbooks.Product}) in {health.Duration} ms");

    var page = await client.Qbd.Invoices.ListAsync(new InvoiceListParams { Limit = 10 }).GetFirstPageAsync();
    foreach (var invoice in page.Data)
    {
        Console.WriteLine($"{invoice.RefNumber,-12} {invoice.TransactionDate,-12} {invoice.Customer?.FullName,-30} {invoice.Subtotal,12}");
    }
    Console.WriteLine(page.HasMore ? $"... {page.RemainingCount} more" : "(end of list)");
}
catch (ApiException ex)
{
    Console.Error.WriteLine($"{ex.Code}: {ex.Message}");
    foreach (var fix in ex.Fixes) Console.Error.WriteLine($"  {fix.Actor}: {fix.Action}");
    Console.Error.WriteLine($"Request ID: {ex.RequestId}");
    return 1;
}
return 0;
