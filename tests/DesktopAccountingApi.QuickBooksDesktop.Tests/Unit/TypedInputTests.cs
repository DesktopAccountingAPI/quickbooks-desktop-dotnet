using System.Linq;
using System.Text.Json;
using DesktopAccountingApi.QuickBooksDesktop.Models;
using Xunit;

namespace DesktopAccountingApi.QuickBooksDesktop.Tests.Unit;

// Gap G4: confirm required at compile time, open enums as typed values, shared input interfaces.
public class TypedInputTests
{
    [Fact]
    public void Delete_parameters_take_Confirm_Yes_and_send_confirm_true()
    {
        var p = new CustomerDeleteParams(Confirm.Yes);
        Assert.True(p.Confirm);
        Assert.Equal(new[] { "confirm=true" }, p.ToQuery().Select(kv => kv.Key + "=" + kv.Value));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new CustomerDeleteParams((Confirm)7));
    }

    [Fact]
    public void Merge_input_takes_Confirm_Yes_and_writes_it_in_the_body()
    {
        var input = new ListMergeInput(Confirm.Yes) { RevisionNumber = "1", MergeIntoId = "80000002-1", MergeIntoRevisionNumber = "2" };
        var json = JsonSerializer.Serialize(input, DesktopAccountingApiJson.Options);
        Assert.Equal("{\"revisionNumber\":\"1\",\"mergeIntoId\":\"80000002-1\",\"mergeIntoRevisionNumber\":\"2\",\"confirm\":true}", json);
        IListMergeInput shared = new CustomerMergeInput(Confirm.Yes) { RevisionNumber = "1", MergeIntoId = "80000002-1", MergeIntoRevisionNumber = "2" };
        Assert.True(shared.Confirm);
        Assert.Equal("80000002-1", shared.MergeIntoId);
    }

    [Fact]
    public void Open_enums_are_typed_and_keep_unknown_values()
    {
        var input = new LeadUpdateInput { RevisionNumber = "1", FullName = "Lead", Status = LeadStatus.Warm };
        Assert.Contains("\"status\":\"warm\"", JsonSerializer.Serialize(input, DesktopAccountingApiJson.Options));
        var lead = JsonSerializer.Deserialize<Lead>("{\"id\":\"1\",\"objectType\":\"qbd_lead\",\"status\":\"lukewarm\"}", DesktopAccountingApiJson.Options)!;
        Assert.Equal("lukewarm", lead.Status!.Value.Value);
        Assert.False(lead.Status.Value.IsKnown);
        Assert.True(LeadStatus.Hot.IsKnown);
        Assert.True(lead.Status == "lukewarm");
        var definition = JsonSerializer.Deserialize<CustomFieldDefinition>("{\"assignToObjects\":[\"customer\",\"invoice\"]}", DesktopAccountingApiJson.Options)!;
        Assert.Equal(new[] { AssignToObject.Customer, AssignToObject.Invoice }, definition.AssignToObjects);
        var cleared = JsonSerializer.Deserialize<TransactionClearedStatus>("{\"clearedStatus\":\"cleared\"}", DesktopAccountingApiJson.Options)!;
        Assert.Equal(ClearedStatus.Cleared, cleared.ClearedStatus);
    }

    [Fact]
    public void Custom_field_value_inputs_share_an_interface()
    {
        ICustomFieldValueInput list = new CustomFieldValueInput { Operation = CustomFieldOperation.Update, Name = "Region", Value = "West" };
        ICustomFieldValueInput transaction = new TransactionCustomFieldValueInput { Operation = CustomFieldOperation.Update, Name = "Region", Value = "West", TransactionLineId = "1-1" };
        Assert.Equal(list.Name, transaction.Name);
        Assert.Equal(list.Operation, transaction.Operation);
    }
}
