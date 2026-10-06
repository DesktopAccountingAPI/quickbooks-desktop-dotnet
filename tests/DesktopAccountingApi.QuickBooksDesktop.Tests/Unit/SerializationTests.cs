using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DesktopAccountingApi.QuickBooksDesktop.Models;
using Xunit;

namespace DesktopAccountingApi.QuickBooksDesktop.Tests.Unit;

public class SerializationTests
{
    [Theory]
    [InlineData("52.75")]
    [InlineData("5.00")]
    [InlineData("-0.10")]
    [InlineData("1234567890.123456")]
    [InlineData("0")]
    public void Decimal_strings_round_trip_with_their_scale(string wire)
    {
        var json = $"{{\"id\":\"1\",\"balance\":\"{wire}\"}}";
        var customer = DesktopAccountingApiJson.Deserialize<Customer>(json)!;
        Assert.Equal(decimal.Parse(wire, System.Globalization.CultureInfo.InvariantCulture), customer.Balance);
        Assert.Contains($"\"balance\":\"{wire}\"", customer.ToJson(), StringComparison.Ordinal);
    }

    [Fact]
    public void Decimal_inputs_are_sent_as_strings()
    {
        var line = new InvoiceLineCreateInput { ItemId = "80000005-1700000000", Quantity = 2, Rate = 52.75m, Amount = 5.00m };
        Assert.Equal("{\"itemId\":\"80000005-1700000000\",\"quantity\":2,\"rate\":\"52.75\",\"amount\":\"5.00\"}", DesktopAccountingApiJson.Serialize(line));
    }

    [Fact]
    public void Dates_and_timestamps_keep_their_format_and_offset()
    {
        const string json = "{\"id\":\"1\",\"createdAt\":\"2026-10-05T09:14:03-07:00\",\"updatedAt\":\"2026-10-05T16:03:59.002Z\",\"transactionDate\":\"2026-10-05\"}";
        var invoice = DesktopAccountingApiJson.Deserialize<Invoice>(json)!;
        Assert.Equal(new DateOnly(2026, 10, 5), invoice.TransactionDate);
        Assert.Equal(TimeSpan.FromHours(-7), invoice.CreatedAt.Offset);
        var back = invoice.ToJson();
        Assert.Contains("\"createdAt\":\"2026-10-05T09:14:03-07:00\"", back, StringComparison.Ordinal);
        Assert.Contains("\"updatedAt\":\"2026-10-05T16:03:59.002Z\"", back, StringComparison.Ordinal);
        Assert.Contains("\"transactionDate\":\"2026-10-05\"", back, StringComparison.Ordinal);
    }

    [Fact]
    public void Timestamps_always_include_seconds()
    {
        Assert.Equal("2026-10-05T09:00:00+05:30", DateTimeOffsetConverter.Format(new DateTimeOffset(2026, 10, 5, 9, 0, 0, TimeSpan.FromMinutes(330))));
    }

    [Theory]
    [InlineData("2026-02-30")]
    [InlineData("2026-1-05")]
    [InlineData("05/10/2026")]
    public void Invalid_dates_are_rejected(string text)
    {
        Assert.False(DateOnlyConverter.TryParse(text, out _));
    }

    [Fact]
    public void Unknown_fields_and_enum_values_are_tolerated_and_round_trip()
    {
        const string json = "{\"id\":\"req_1\",\"status\":\"future_status\",\"someNewField\":{\"nested\":[1,2,3]}}";
        var request = DesktopAccountingApiJson.Deserialize<Request>(json)!;
        Assert.Equal("future_status", request.Status);
        Assert.Null(request.Error);
        Assert.Contains("\"someNewField\":{\"nested\":[1,2,3]}", request.ToJson(), StringComparison.Ordinal);
    }

    [Fact]
    public void Omitted_properties_are_not_sent_and_null_clears()
    {
        var update = new InvoiceUpdateInput { RevisionNumber = "1700000007", Memo = null };
        Assert.Equal("{\"revisionNumber\":\"1700000007\",\"memo\":null}", DesktopAccountingApiJson.Serialize(update));
        Assert.True(update.IsSet("memo"));
        Assert.True(update.IsSet(nameof(InvoiceUpdateInput.Memo)));
        Assert.False(update.IsSet("customerId"));

        update.Unset("memo");
        Assert.Equal("{\"revisionNumber\":\"1700000007\"}", DesktopAccountingApiJson.Serialize(update));
    }

    [Fact]
    public void Null_on_a_non_nullable_property_removes_it()
    {
        var update = new InvoiceUpdateInput { RevisionNumber = "1", CustomerId = "80000001-1700000000" };
        update.CustomerId = null;
        Assert.False(update.IsSet("customerId"));
        Assert.Equal("{\"revisionNumber\":\"1\"}", DesktopAccountingApiJson.Serialize(update));
    }

    [Fact]
    public void Missing_required_property_fails_locally()
    {
        var ex = Assert.Throws<DaapiException>(() => DesktopAccountingApiJson.Serialize(new InvoiceUpdateInput { Memo = "x" }));
        Assert.Contains("RevisionNumber", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Inputs_deserialize_with_explicit_nulls()
    {
        var update = DesktopAccountingApiJson.Deserialize<InvoiceUpdateInput>("{\"revisionNumber\":\"1\",\"memo\":null}")!;
        Assert.True(update.IsSet("memo"));
        Assert.Null(update.Memo);
        Assert.Throws<JsonException>(() => DesktopAccountingApiJson.Deserialize<InvoiceUpdateInput>("{\"nope\":1}"));
    }

    [Fact]
    public void Closed_enums_use_wire_values()
    {
        var p = new InvoiceListParams { PaymentStatus = PaymentStatus.NotPaid };
        Assert.Equal("{\"paymentStatus\":\"not_paid\"}", DesktopAccountingApiJson.Serialize(p));
        Assert.Equal(PaymentStatus.NotPaid, DesktopAccountingApiJson.Deserialize<InvoiceListParams>("{\"paymentStatus\":\"not_paid\"}")!.PaymentStatus);
    }

    [Fact]
    public void Query_parameters_repeat_array_keys_and_use_wire_formats()
    {
        var p = new InvoiceListParams
        {
            CustomerIds = new[] { "80000001-1700000000", "80000002-1700000000" },
            Limit = 2,
            PaymentStatus = PaymentStatus.NotPaid,
            TransactionDateFrom = new DateOnly(2026, 1, 31),
            IncludeLineItems = false,
            UpdatedAfter = "2026-10-01",
        };
        var pairs = p.ToQuery().Select(kv => kv.Key + "=" + kv.Value).ToList();
        Assert.Equal(
            new[] { "limit=2", "updatedAfter=2026-10-01", "transactionDateFrom=2026-01-31", "customerIds=80000001-1700000000", "customerIds=80000002-1700000000", "paymentStatus=not_paid", "includeLineItems=false" },
            pairs);
    }

    [Fact]
    public void Passthrough_body_is_free_form_json()
    {
        var body = new PassthroughInput { ["CustomerQueryRq"] = new Dictionary<string, object?> { ["MaxReturned"] = 5 } };
        Assert.Equal("{\"CustomerQueryRq\":{\"MaxReturned\":5}}", DesktopAccountingApiJson.Serialize(body));
    }
}
