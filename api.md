# API reference

Every method of `DesktopAccountingApiClient`, generated from the API contract (439 operations, contract sha256 `e5fba4897290`). Types are in `DesktopAccountingApi.QuickBooksDesktop.Models`.

Every method also takes `RequestOptions? options = null` and `CancellationToken cancellationToken = default` (omitted below). Variants:

- `XxxWithResponseAsync(...)` returns `ApiResponse<T>` (status, headers, request ID) for every non-paginated method.
- `Enqueue.XxxAsync(...)` (methods marked *async mode*) returns `RequestHandle<T>` without waiting for QuickBooks.
- List methods marked *paginated* return `Pager<T>`: `await foreach`, `GetFirstPageAsync()`, `PagesAsync()`, `ListAllAsync()`.

## client.AuthSessions

Auth sessions.

- `Task<AuthSession> CreateAsync(AuthSessionCreateInput input)`  
  `POST /v1/auth-sessions` (`authSessions.create`): Create an auth session

## client.ChangeFeedSubscriptions

Change feed.

- `Task<ChangeFeedSubscription> CreateAsync(ChangeFeedSubscriptionCreateInput input)`  
  `POST /v1/change-feed-subscriptions` (`changeFeedSubscriptions.create`): Create a change feed subscription
- `Task<ChangeFeedSubscriptionDeleted> DeleteAsync(string id)`  
  `DELETE /v1/change-feed-subscriptions/{id}` (`changeFeedSubscriptions.delete`): Delete a change feed subscription
- `Pager<ChangeFeedSubscription> ListAsync(ChangeFeedSubscriptionListParams? parameters = null)`  
  `GET /v1/change-feed-subscriptions` (`changeFeedSubscriptions.list`) *paginated*: List change feed subscriptions
- `Task<ChangeFeedSubscription> RetrieveAsync(string id)`  
  `GET /v1/change-feed-subscriptions/{id}` (`changeFeedSubscriptions.retrieve`): Retrieve a change feed subscription
- `Task<ChangeFeedSubscription> UpdateAsync(string id, ChangeFeedSubscriptionUpdateInput input)`  
  `POST /v1/change-feed-subscriptions/{id}` (`changeFeedSubscriptions.update`): Update a change feed subscription

## client.EndUsers

End users.

- `Task<EndUser> CreateAsync(EndUserCreateInput input)`  
  `POST /v1/end-users` (`endUsers.create`): Create an end user
- `Task<EndUserDeleted> DeleteAsync(string id)`  
  `DELETE /v1/end-users/{id}` (`endUsers.delete`): Delete an end user
- `Task<EndUserConnection> DisableConnectionAsync(string id)`  
  `POST /v1/end-users/{id}/connection/disable` (`endUsers.disableConnection`): Disable the connection
- `Task<EndUserConnection> EnableConnectionAsync(string id)`  
  `POST /v1/end-users/{id}/connection/enable` (`endUsers.enableConnection`): Enable the connection
- `Pager<EndUser> ListAsync(EndUserListParams? parameters = null)`  
  `GET /v1/end-users` (`endUsers.list`) *paginated*: List end users
- `Task<PassthroughResponse> PassthroughAsync(string id, PassthroughInput? input = null)`  
  `POST /v1/end-users/{id}/passthrough/{integrationSlug}` (`endUsers.passthrough`) *async mode*: Send a raw qbXML request
- `Task<string> PassthroughXmlAsync(string id, string xml)`  
  Same operation with a raw qbXML body (`Content-Type: application/xml`).
- `Task<EndUserConnection> RemoveConnectionInstallAsync(string id, string installId)`  
  `POST /v1/end-users/{id}/connection/installs/{installId}/remove` (`endUsers.removeConnectionInstall`): Remove a Web Connector install
- `Task<EndUser> ResetCompanyFileAsync(string id, EndUserResetCompanyFileInput input)`  
  `POST /v1/end-users/{id}/reset-company-file` (`endUsers.resetCompanyFile`): Reset the company file
- `Task<EndUser> RetrieveAsync(string id)`  
  `GET /v1/end-users/{id}` (`endUsers.retrieve`): Retrieve an end user
- `Task<EndUserConnection> RetrieveConnectionAsync(string id)`  
  `GET /v1/end-users/{id}/connection` (`endUsers.retrieveConnection`): Retrieve the connection
- `Task<EndUser> UpdateAsync(string id, EndUserUpdateInput input)`  
  `POST /v1/end-users/{id}` (`endUsers.update`): Update an end user

## client.Qbd

Batch.

- `Task<Batch> BatchAsync(BatchInput input)`  
  `POST /v1/quickbooks-desktop/batch` (`qbd.batch`) *async mode*: Run several operations in one round trip
- `Task<HealthCheck> HealthCheckAsync()`  
  `GET /v1/quickbooks-desktop/health-check` (`qbd.healthCheck`) *async mode*: Check the QuickBooks Desktop connection

## client.Qbd.Accounts

Accounts.

- `Task<Account> CreateAsync(AccountCreateInput input)`  
  `POST /v1/quickbooks-desktop/accounts` (`qbd.accounts.create`) *async mode*: Create an account
- `Task<AccountDeleted> DeleteAsync(string id, AccountDeleteParams parameters)`  
  `DELETE /v1/quickbooks-desktop/accounts/{id}` (`qbd.accounts.delete`) *async mode*: Delete an account
- `Task<AccountList> ListAsync(AccountListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/accounts` (`qbd.accounts.list`) *async mode*: List accounts
- `Task<ListMerge> MergeAsync(string id, ListMergeInput input)`  
  `POST /v1/quickbooks-desktop/accounts/{id}/merge` (`qbd.accounts.merge`) *async mode*: Merge an account into another
- `Task<Account> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/accounts/{id}` (`qbd.accounts.retrieve`) *async mode*: Retrieve an account
- `Task<CustomFieldValue> SetCustomFieldAsync(string id, CustomFieldValueInput input)`  
  `POST /v1/quickbooks-desktop/accounts/{id}/custom-fields` (`qbd.accounts.setCustomField`) *async mode*: Set a custom field on an account
- `Task<Account> UpdateAsync(string id, AccountUpdateInput input)`  
  `POST /v1/quickbooks-desktop/accounts/{id}` (`qbd.accounts.update`) *async mode*: Update an account

## client.Qbd.AccountTaxLines

Account tax lines.

- `Task<AccountTaxLineList> ListAsync()`  
  `GET /v1/quickbooks-desktop/account-tax-lines` (`qbd.accountTaxLines.list`) *async mode*: List account tax lines

## client.Qbd.BillCheckPayments

Bill check payments.

- `Task<BillCheckPayment> CreateAsync(BillCheckPaymentCreateInput input)`  
  `POST /v1/quickbooks-desktop/bill-check-payments` (`qbd.billCheckPayments.create`) *async mode*: Create a bill check payment
- `Task<BillCheckPaymentDeleted> DeleteAsync(string id)`  
  `DELETE /v1/quickbooks-desktop/bill-check-payments/{id}` (`qbd.billCheckPayments.delete`) *async mode*: Delete a bill check payment
- `Pager<BillCheckPayment> ListAsync(BillCheckPaymentListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/bill-check-payments` (`qbd.billCheckPayments.list`) *paginated, async mode*: List bill check payments
- `Task<BillCheckPayment> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/bill-check-payments/{id}` (`qbd.billCheckPayments.retrieve`) *async mode*: Retrieve a bill check payment
- `Task<CustomFieldValue> SetCustomFieldAsync(string id, TransactionCustomFieldValueInput input)`  
  `POST /v1/quickbooks-desktop/bill-check-payments/{id}/custom-fields` (`qbd.billCheckPayments.setCustomField`) *async mode*: Set a custom field on a bill check payment
- `Task<BillCheckPayment> UpdateAsync(string id, BillCheckPaymentUpdateInput input)`  
  `POST /v1/quickbooks-desktop/bill-check-payments/{id}` (`qbd.billCheckPayments.update`) *async mode*: Update a bill check payment
- `Task<BillCheckPaymentVoided> VoidAsync(string id)`  
  `POST /v1/quickbooks-desktop/bill-check-payments/{id}/void` (`qbd.billCheckPayments.void`) *async mode*: Void a bill check payment

## client.Qbd.BillCreditCardPayments

Bill credit card payments.

- `Task<BillCreditCardPayment> CreateAsync(BillCreditCardPaymentCreateInput input)`  
  `POST /v1/quickbooks-desktop/bill-credit-card-payments` (`qbd.billCreditCardPayments.create`) *async mode*: Create a bill credit card payment
- `Task<BillCreditCardPaymentDeleted> DeleteAsync(string id)`  
  `DELETE /v1/quickbooks-desktop/bill-credit-card-payments/{id}` (`qbd.billCreditCardPayments.delete`) *async mode*: Delete a bill credit card payment
- `Pager<BillCreditCardPayment> ListAsync(BillCreditCardPaymentListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/bill-credit-card-payments` (`qbd.billCreditCardPayments.list`) *paginated, async mode*: List bill credit card payments
- `Task<BillCreditCardPayment> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/bill-credit-card-payments/{id}` (`qbd.billCreditCardPayments.retrieve`) *async mode*: Retrieve a bill credit card payment
- `Task<CustomFieldValue> SetCustomFieldAsync(string id, TransactionCustomFieldValueInput input)`  
  `POST /v1/quickbooks-desktop/bill-credit-card-payments/{id}/custom-fields` (`qbd.billCreditCardPayments.setCustomField`) *async mode*: Set a custom field on a bill credit card payment
- `Task<BillCreditCardPaymentVoided> VoidAsync(string id)`  
  `POST /v1/quickbooks-desktop/bill-credit-card-payments/{id}/void` (`qbd.billCreditCardPayments.void`) *async mode*: Void a bill credit card payment

## client.Qbd.BillingRates

Billing rates.

- `Task<BillingRate> CreateAsync(BillingRateCreateInput input)`  
  `POST /v1/quickbooks-desktop/billing-rates` (`qbd.billingRates.create`) *async mode*: Create a billing rate
- `Task<BillingRateDeleted> DeleteAsync(string id, BillingRateDeleteParams parameters)`  
  `DELETE /v1/quickbooks-desktop/billing-rates/{id}` (`qbd.billingRates.delete`) *async mode*: Delete a billing rate
- `Task<BillingRateList> ListAsync(BillingRateListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/billing-rates` (`qbd.billingRates.list`) *async mode*: List billing rates
- `Task<BillingRate> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/billing-rates/{id}` (`qbd.billingRates.retrieve`) *async mode*: Retrieve a billing rate

## client.Qbd.Bills

Bills.

- `Task<Bill> CreateAsync(BillCreateInput input)`  
  `POST /v1/quickbooks-desktop/bills` (`qbd.bills.create`) *async mode*: Create a bill
- `Task<BillDeleted> DeleteAsync(string id)`  
  `DELETE /v1/quickbooks-desktop/bills/{id}` (`qbd.bills.delete`) *async mode*: Delete a bill
- `Pager<Bill> ListAsync(BillListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/bills` (`qbd.bills.list`) *paginated, async mode*: List bills
- `Task<Bill> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/bills/{id}` (`qbd.bills.retrieve`) *async mode*: Retrieve a bill
- `Task<CustomFieldValue> SetCustomFieldAsync(string id, TransactionCustomFieldValueInput input)`  
  `POST /v1/quickbooks-desktop/bills/{id}/custom-fields` (`qbd.bills.setCustomField`) *async mode*: Set a custom field on a bill
- `Task<Bill> UpdateAsync(string id, BillUpdateInput input)`  
  `POST /v1/quickbooks-desktop/bills/{id}` (`qbd.bills.update`) *async mode*: Update a bill
- `Task<BillVoided> VoidAsync(string id)`  
  `POST /v1/quickbooks-desktop/bills/{id}/void` (`qbd.bills.void`) *async mode*: Void a bill

## client.Qbd.BillsToPay

Bills to pay.

- `Task<BillsToPayList> ListAsync(BillsToPayListParams parameters)`  
  `GET /v1/quickbooks-desktop/bills-to-pay` (`qbd.billsToPay.list`) *async mode*: List bill to pays

## client.Qbd.BuildAssemblies

Build assemblies.

- `Task<BuildAssembly> CreateAsync(BuildAssemblyCreateInput input)`  
  `POST /v1/quickbooks-desktop/build-assemblies` (`qbd.buildAssemblies.create`) *async mode*: Create a build assembly
- `Task<BuildAssemblyDeleted> DeleteAsync(string id)`  
  `DELETE /v1/quickbooks-desktop/build-assemblies/{id}` (`qbd.buildAssemblies.delete`) *async mode*: Delete a build assembly
- `Pager<BuildAssembly> ListAsync(BuildAssemblyListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/build-assemblies` (`qbd.buildAssemblies.list`) *paginated, async mode*: List build assemblies
- `Task<BuildAssembly> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/build-assemblies/{id}` (`qbd.buildAssemblies.retrieve`) *async mode*: Retrieve a build assembly
- `Task<CustomFieldValue> SetCustomFieldAsync(string id, TransactionCustomFieldValueInput input)`  
  `POST /v1/quickbooks-desktop/build-assemblies/{id}/custom-fields` (`qbd.buildAssemblies.setCustomField`) *async mode*: Set a custom field on a build assembly
- `Task<BuildAssembly> UpdateAsync(string id, BuildAssemblyUpdateInput input)`  
  `POST /v1/quickbooks-desktop/build-assemblies/{id}` (`qbd.buildAssemblies.update`) *async mode*: Update a build assembly

## client.Qbd.Checks

Checks.

- `Task<Check> CreateAsync(CheckCreateInput input)`  
  `POST /v1/quickbooks-desktop/checks` (`qbd.checks.create`) *async mode*: Create a check
- `Task<CheckDeleted> DeleteAsync(string id)`  
  `DELETE /v1/quickbooks-desktop/checks/{id}` (`qbd.checks.delete`) *async mode*: Delete a check
- `Pager<Check> ListAsync(CheckListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/checks` (`qbd.checks.list`) *paginated, async mode*: List checks
- `Task<Check> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/checks/{id}` (`qbd.checks.retrieve`) *async mode*: Retrieve a check
- `Task<CustomFieldValue> SetCustomFieldAsync(string id, TransactionCustomFieldValueInput input)`  
  `POST /v1/quickbooks-desktop/checks/{id}/custom-fields` (`qbd.checks.setCustomField`) *async mode*: Set a custom field on a check
- `Task<Check> UpdateAsync(string id, CheckUpdateInput input)`  
  `POST /v1/quickbooks-desktop/checks/{id}` (`qbd.checks.update`) *async mode*: Update a check
- `Task<CheckVoided> VoidAsync(string id)`  
  `POST /v1/quickbooks-desktop/checks/{id}/void` (`qbd.checks.void`) *async mode*: Void a check

## client.Qbd.Classes

Classes.

- `Task<Class> CreateAsync(ClassCreateInput input)`  
  `POST /v1/quickbooks-desktop/classes` (`qbd.classes.create`) *async mode*: Create a class
- `Task<ClassDeleted> DeleteAsync(string id, ClassDeleteParams parameters)`  
  `DELETE /v1/quickbooks-desktop/classes/{id}` (`qbd.classes.delete`) *async mode*: Delete a class
- `Task<ClassList> ListAsync(ClassListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/classes` (`qbd.classes.list`) *async mode*: List classes
- `Task<ListMerge> MergeAsync(string id, ListMergeInput input)`  
  `POST /v1/quickbooks-desktop/classes/{id}/merge` (`qbd.classes.merge`) *async mode*: Merge a class into another
- `Task<Class> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/classes/{id}` (`qbd.classes.retrieve`) *async mode*: Retrieve a class
- `Task<Class> UpdateAsync(string id, ClassUpdateInput input)`  
  `POST /v1/quickbooks-desktop/classes/{id}` (`qbd.classes.update`) *async mode*: Update a class

## client.Qbd.Company

Company.

- `Task<Company> RetrieveAsync()`  
  `GET /v1/quickbooks-desktop/company` (`qbd.company.retrieve`) *async mode*: Retrieve company information

## client.Qbd.CreditCardCharges

Credit card charges.

- `Task<CreditCardCharge> CreateAsync(CreditCardChargeCreateInput input)`  
  `POST /v1/quickbooks-desktop/credit-card-charges` (`qbd.creditCardCharges.create`) *async mode*: Create a credit card charge
- `Task<CreditCardChargeDeleted> DeleteAsync(string id)`  
  `DELETE /v1/quickbooks-desktop/credit-card-charges/{id}` (`qbd.creditCardCharges.delete`) *async mode*: Delete a credit card charge
- `Pager<CreditCardCharge> ListAsync(CreditCardChargeListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/credit-card-charges` (`qbd.creditCardCharges.list`) *paginated, async mode*: List credit card charges
- `Task<CreditCardCharge> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/credit-card-charges/{id}` (`qbd.creditCardCharges.retrieve`) *async mode*: Retrieve a credit card charge
- `Task<CustomFieldValue> SetCustomFieldAsync(string id, TransactionCustomFieldValueInput input)`  
  `POST /v1/quickbooks-desktop/credit-card-charges/{id}/custom-fields` (`qbd.creditCardCharges.setCustomField`) *async mode*: Set a custom field on a credit card charge
- `Task<CreditCardCharge> UpdateAsync(string id, CreditCardChargeUpdateInput input)`  
  `POST /v1/quickbooks-desktop/credit-card-charges/{id}` (`qbd.creditCardCharges.update`) *async mode*: Update a credit card charge
- `Task<CreditCardChargeVoided> VoidAsync(string id)`  
  `POST /v1/quickbooks-desktop/credit-card-charges/{id}/void` (`qbd.creditCardCharges.void`) *async mode*: Void a credit card charge

## client.Qbd.CreditCardCredits

Credit card credits.

- `Task<CreditCardCredit> CreateAsync(CreditCardCreditCreateInput input)`  
  `POST /v1/quickbooks-desktop/credit-card-credits` (`qbd.creditCardCredits.create`) *async mode*: Create a credit card credit
- `Task<CreditCardCreditDeleted> DeleteAsync(string id)`  
  `DELETE /v1/quickbooks-desktop/credit-card-credits/{id}` (`qbd.creditCardCredits.delete`) *async mode*: Delete a credit card credit
- `Pager<CreditCardCredit> ListAsync(CreditCardCreditListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/credit-card-credits` (`qbd.creditCardCredits.list`) *paginated, async mode*: List credit card credits
- `Task<CreditCardCredit> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/credit-card-credits/{id}` (`qbd.creditCardCredits.retrieve`) *async mode*: Retrieve a credit card credit
- `Task<CustomFieldValue> SetCustomFieldAsync(string id, TransactionCustomFieldValueInput input)`  
  `POST /v1/quickbooks-desktop/credit-card-credits/{id}/custom-fields` (`qbd.creditCardCredits.setCustomField`) *async mode*: Set a custom field on a credit card credit
- `Task<CreditCardCredit> UpdateAsync(string id, CreditCardCreditUpdateInput input)`  
  `POST /v1/quickbooks-desktop/credit-card-credits/{id}` (`qbd.creditCardCredits.update`) *async mode*: Update a credit card credit
- `Task<CreditCardCreditVoided> VoidAsync(string id)`  
  `POST /v1/quickbooks-desktop/credit-card-credits/{id}/void` (`qbd.creditCardCredits.void`) *async mode*: Void a credit card credit

## client.Qbd.CreditCardRefunds

Credit card refunds.

- `Task<CreditCardRefund> CreateAsync(CreditCardRefundCreateInput input)`  
  `POST /v1/quickbooks-desktop/credit-card-refunds` (`qbd.creditCardRefunds.create`) *async mode*: Create a credit card refund
- `Task<CreditCardRefundDeleted> DeleteAsync(string id)`  
  `DELETE /v1/quickbooks-desktop/credit-card-refunds/{id}` (`qbd.creditCardRefunds.delete`) *async mode*: Delete a credit card refund
- `Pager<CreditCardRefund> ListAsync(CreditCardRefundListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/credit-card-refunds` (`qbd.creditCardRefunds.list`) *paginated, async mode*: List credit card refunds
- `Task<CreditCardRefund> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/credit-card-refunds/{id}` (`qbd.creditCardRefunds.retrieve`) *async mode*: Retrieve a credit card refund
- `Task<CustomFieldValue> SetCustomFieldAsync(string id, TransactionCustomFieldValueInput input)`  
  `POST /v1/quickbooks-desktop/credit-card-refunds/{id}/custom-fields` (`qbd.creditCardRefunds.setCustomField`) *async mode*: Set a custom field on a credit card refund
- `Task<CreditCardRefundVoided> VoidAsync(string id)`  
  `POST /v1/quickbooks-desktop/credit-card-refunds/{id}/void` (`qbd.creditCardRefunds.void`) *async mode*: Void a credit card refund

## client.Qbd.CreditMemos

Credit memos.

- `Task<CreditMemo> CreateAsync(CreditMemoCreateInput input)`  
  `POST /v1/quickbooks-desktop/credit-memos` (`qbd.creditMemos.create`) *async mode*: Create a credit memo
- `Task<CreditMemoDeleted> DeleteAsync(string id)`  
  `DELETE /v1/quickbooks-desktop/credit-memos/{id}` (`qbd.creditMemos.delete`) *async mode*: Delete a credit memo
- `Pager<CreditMemo> ListAsync(CreditMemoListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/credit-memos` (`qbd.creditMemos.list`) *paginated, async mode*: List credit memos
- `Task<CreditMemo> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/credit-memos/{id}` (`qbd.creditMemos.retrieve`) *async mode*: Retrieve a credit memo
- `Task<CustomFieldValue> SetCustomFieldAsync(string id, TransactionCustomFieldValueInput input)`  
  `POST /v1/quickbooks-desktop/credit-memos/{id}/custom-fields` (`qbd.creditMemos.setCustomField`) *async mode*: Set a custom field on a credit memo
- `Task<CreditMemo> UpdateAsync(string id, CreditMemoUpdateInput input)`  
  `POST /v1/quickbooks-desktop/credit-memos/{id}` (`qbd.creditMemos.update`) *async mode*: Update a credit memo
- `Task<CreditMemoVoided> VoidAsync(string id)`  
  `POST /v1/quickbooks-desktop/credit-memos/{id}/void` (`qbd.creditMemos.void`) *async mode*: Void a credit memo

## client.Qbd.Currencies

Currencies.

- `Task<Currency> CreateAsync(CurrencyCreateInput input)`  
  `POST /v1/quickbooks-desktop/currencies` (`qbd.currencies.create`) *async mode*: Create a currency
- `Task<CurrencyDeleted> DeleteAsync(string id, CurrencyDeleteParams parameters)`  
  `DELETE /v1/quickbooks-desktop/currencies/{id}` (`qbd.currencies.delete`) *async mode*: Delete a currency
- `Task<CurrencyList> ListAsync(CurrencyListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/currencies` (`qbd.currencies.list`) *async mode*: List currencies
- `Task<Currency> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/currencies/{id}` (`qbd.currencies.retrieve`) *async mode*: Retrieve a currency
- `Task<Currency> UpdateAsync(string id, CurrencyUpdateInput input)`  
  `POST /v1/quickbooks-desktop/currencies/{id}` (`qbd.currencies.update`) *async mode*: Update a currency

## client.Qbd.CustomerMessages

Customer messages.

- `Task<CustomerMessage> CreateAsync(CustomerMessageCreateInput input)`  
  `POST /v1/quickbooks-desktop/customer-messages` (`qbd.customerMessages.create`) *async mode*: Create a customer message
- `Task<CustomerMessageDeleted> DeleteAsync(string id, CustomerMessageDeleteParams parameters)`  
  `DELETE /v1/quickbooks-desktop/customer-messages/{id}` (`qbd.customerMessages.delete`) *async mode*: Delete a customer message
- `Task<CustomerMessageList> ListAsync(CustomerMessageListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/customer-messages` (`qbd.customerMessages.list`) *async mode*: List customer messages
- `Task<CustomerMessage> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/customer-messages/{id}` (`qbd.customerMessages.retrieve`) *async mode*: Retrieve a customer message

## client.Qbd.Customers

Customers.

- `Task<Customer> CreateAsync(CustomerCreateInput input)`  
  `POST /v1/quickbooks-desktop/customers` (`qbd.customers.create`) *async mode*: Create a customer
- `Task<CustomerDeleted> DeleteAsync(string id, CustomerDeleteParams parameters)`  
  `DELETE /v1/quickbooks-desktop/customers/{id}` (`qbd.customers.delete`) *async mode*: Delete a customer
- `Pager<Customer> ListAsync(CustomerListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/customers` (`qbd.customers.list`) *paginated, async mode*: List customers
- `Task<ListMerge> MergeAsync(string id, CustomerMergeInput input)`  
  `POST /v1/quickbooks-desktop/customers/{id}/merge` (`qbd.customers.merge`) *async mode*: Merge a customer into another
- `Task<Customer> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/customers/{id}` (`qbd.customers.retrieve`) *async mode*: Retrieve a customer
- `Task<CustomFieldValue> SetCustomFieldAsync(string id, CustomFieldValueInput input)`  
  `POST /v1/quickbooks-desktop/customers/{id}/custom-fields` (`qbd.customers.setCustomField`) *async mode*: Set a custom field on a customer
- `Task<Customer> UpdateAsync(string id, CustomerUpdateInput input)`  
  `POST /v1/quickbooks-desktop/customers/{id}` (`qbd.customers.update`) *async mode*: Update a customer

## client.Qbd.CustomerTypes

Customer types.

- `Task<CustomerType> CreateAsync(CustomerTypeCreateInput input)`  
  `POST /v1/quickbooks-desktop/customer-types` (`qbd.customerTypes.create`) *async mode*: Create a customer type
- `Task<CustomerTypeDeleted> DeleteAsync(string id, CustomerTypeDeleteParams parameters)`  
  `DELETE /v1/quickbooks-desktop/customer-types/{id}` (`qbd.customerTypes.delete`) *async mode*: Delete a customer type
- `Task<CustomerTypeList> ListAsync(CustomerTypeListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/customer-types` (`qbd.customerTypes.list`) *async mode*: List customer types
- `Task<CustomerType> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/customer-types/{id}` (`qbd.customerTypes.retrieve`) *async mode*: Retrieve a customer type

## client.Qbd.CustomFieldDefinitions

Custom field definitions.

- `Task<CustomFieldDefinition> CreateAsync(CustomFieldDefinitionCreateInput input)`  
  `POST /v1/quickbooks-desktop/custom-field-definitions` (`qbd.customFieldDefinitions.create`) *async mode*: Create a custom field definition
- `Task<CustomFieldDefinitionDeleted> DeleteAsync(string definitionName, CustomFieldDefinitionDeleteParams parameters)`  
  `DELETE /v1/quickbooks-desktop/custom-field-definitions/{definitionName}` (`qbd.customFieldDefinitions.delete`) *async mode*: Request deletion of a private custom field definition
- `Task<CustomFieldDefinitionList> ListAsync(CustomFieldDefinitionListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/custom-field-definitions` (`qbd.customFieldDefinitions.list`) *async mode*: List custom field definitions
- `Task<CustomFieldDefinition> UpdateAsync(string definitionName, CustomFieldDefinitionUpdateInput input)`  
  `POST /v1/quickbooks-desktop/custom-field-definitions/{definitionName}` (`qbd.customFieldDefinitions.update`) *async mode*: Update a custom field definition

## client.Qbd.DateDrivenTerms

Date driven terms.

- `Task<DateDrivenTerm> CreateAsync(DateDrivenTermCreateInput input)`  
  `POST /v1/quickbooks-desktop/date-driven-terms` (`qbd.dateDrivenTerms.create`) *async mode*: Create a date-driven term
- `Task<DateDrivenTermDeleted> DeleteAsync(string id, DateDrivenTermDeleteParams parameters)`  
  `DELETE /v1/quickbooks-desktop/date-driven-terms/{id}` (`qbd.dateDrivenTerms.delete`) *async mode*: Delete a date-driven term
- `Task<DateDrivenTermList> ListAsync(DateDrivenTermListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/date-driven-terms` (`qbd.dateDrivenTerms.list`) *async mode*: List date-driven terms
- `Task<DateDrivenTerm> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/date-driven-terms/{id}` (`qbd.dateDrivenTerms.retrieve`) *async mode*: Retrieve a date-driven term

## client.Qbd.DeletedListObjects

Deleted list objects.

- `Task<DeletedListObjectList> ListAsync(DeletedListObjectListParams parameters)`  
  `GET /v1/quickbooks-desktop/deleted-list-objects` (`qbd.deletedListObjects.list`) *async mode*: List deleted list objects

## client.Qbd.DeletedTransactions

Deleted transactions.

- `Task<DeletedTransactionList> ListAsync(DeletedTransactionListParams parameters)`  
  `GET /v1/quickbooks-desktop/deleted-transactions` (`qbd.deletedTransactions.list`) *async mode*: List deleted transactions

## client.Qbd.Deposits

Deposits.

- `Task<Deposit> CreateAsync(DepositCreateInput input)`  
  `POST /v1/quickbooks-desktop/deposits` (`qbd.deposits.create`) *async mode*: Create a deposit
- `Task<DepositDeleted> DeleteAsync(string id)`  
  `DELETE /v1/quickbooks-desktop/deposits/{id}` (`qbd.deposits.delete`) *async mode*: Delete a deposit
- `Pager<Deposit> ListAsync(DepositListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/deposits` (`qbd.deposits.list`) *paginated, async mode*: List deposits
- `Task<Deposit> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/deposits/{id}` (`qbd.deposits.retrieve`) *async mode*: Retrieve a deposit
- `Task<CustomFieldValue> SetCustomFieldAsync(string id, TransactionCustomFieldValueInput input)`  
  `POST /v1/quickbooks-desktop/deposits/{id}/custom-fields` (`qbd.deposits.setCustomField`) *async mode*: Set a custom field on a deposit
- `Task<Deposit> UpdateAsync(string id, DepositUpdateInput input)`  
  `POST /v1/quickbooks-desktop/deposits/{id}` (`qbd.deposits.update`) *async mode*: Update a deposit
- `Task<DepositVoided> VoidAsync(string id)`  
  `POST /v1/quickbooks-desktop/deposits/{id}/void` (`qbd.deposits.void`) *async mode*: Void a deposit

## client.Qbd.DiscountItems

Discount items.

- `Task<DiscountItem> CreateAsync(DiscountItemCreateInput input)`  
  `POST /v1/quickbooks-desktop/discount-items` (`qbd.discountItems.create`) *async mode*: Create a discount item
- `Task<DiscountItemDeleted> DeleteAsync(string id, DiscountItemDeleteParams parameters)`  
  `DELETE /v1/quickbooks-desktop/discount-items/{id}` (`qbd.discountItems.delete`) *async mode*: Delete a discount item
- `Pager<DiscountItem> ListAsync(DiscountItemListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/discount-items` (`qbd.discountItems.list`) *paginated, async mode*: List discount items
- `Task<ListMerge> MergeAsync(string id, ListMergeInput input)`  
  `POST /v1/quickbooks-desktop/discount-items/{id}/merge` (`qbd.discountItems.merge`) *async mode*: Merge a discount item into another
- `Task<DiscountItem> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/discount-items/{id}` (`qbd.discountItems.retrieve`) *async mode*: Retrieve a discount item
- `Task<CustomFieldValue> SetCustomFieldAsync(string id, CustomFieldValueInput input)`  
  `POST /v1/quickbooks-desktop/discount-items/{id}/custom-fields` (`qbd.discountItems.setCustomField`) *async mode*: Set a custom field on a discount item
- `Task<DiscountItem> UpdateAsync(string id, DiscountItemUpdateInput input)`  
  `POST /v1/quickbooks-desktop/discount-items/{id}` (`qbd.discountItems.update`) *async mode*: Update a discount item

## client.Qbd.Employees

Employees.

- `Task<Employee> CreateAsync(EmployeeCreateInput input)`  
  `POST /v1/quickbooks-desktop/employees` (`qbd.employees.create`) *async mode*: Create an employee
- `Task<EmployeeDeleted> DeleteAsync(string id, EmployeeDeleteParams parameters)`  
  `DELETE /v1/quickbooks-desktop/employees/{id}` (`qbd.employees.delete`) *async mode*: Delete an employee
- `Task<EmployeeList> ListAsync(EmployeeListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/employees` (`qbd.employees.list`) *async mode*: List employees
- `Task<Employee> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/employees/{id}` (`qbd.employees.retrieve`) *async mode*: Retrieve an employee
- `Task<CustomFieldValue> SetCustomFieldAsync(string id, CustomFieldValueInput input)`  
  `POST /v1/quickbooks-desktop/employees/{id}/custom-fields` (`qbd.employees.setCustomField`) *async mode*: Set a custom field on an employee
- `Task<Employee> UpdateAsync(string id, EmployeeUpdateInput input)`  
  `POST /v1/quickbooks-desktop/employees/{id}` (`qbd.employees.update`) *async mode*: Update an employee

## client.Qbd.Entities

Entities.

- `Pager<IEntity> ListAsync(EntityListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/entities` (`qbd.entities.list`) *paginated, async mode*: List entities
- `Task<IEntity> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/entities/{id}` (`qbd.entities.retrieve`) *async mode*: Retrieve an entity

## client.Qbd.Estimates

Estimates.

- `Task<Estimate> CreateAsync(EstimateCreateInput input)`  
  `POST /v1/quickbooks-desktop/estimates` (`qbd.estimates.create`) *async mode*: Create an estimate
- `Task<EstimateDeleted> DeleteAsync(string id)`  
  `DELETE /v1/quickbooks-desktop/estimates/{id}` (`qbd.estimates.delete`) *async mode*: Delete an estimate
- `Pager<Estimate> ListAsync(EstimateListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/estimates` (`qbd.estimates.list`) *paginated, async mode*: List estimates
- `Task<Estimate> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/estimates/{id}` (`qbd.estimates.retrieve`) *async mode*: Retrieve an estimate
- `Task<CustomFieldValue> SetCustomFieldAsync(string id, TransactionCustomFieldValueInput input)`  
  `POST /v1/quickbooks-desktop/estimates/{id}/custom-fields` (`qbd.estimates.setCustomField`) *async mode*: Set a custom field on an estimate
- `Task<Estimate> UpdateAsync(string id, EstimateUpdateInput input)`  
  `POST /v1/quickbooks-desktop/estimates/{id}` (`qbd.estimates.update`) *async mode*: Update an estimate

## client.Qbd.FixedAssetItems

Fixed asset items.

- `Task<FixedAssetItem> CreateAsync(FixedAssetItemCreateInput input)`  
  `POST /v1/quickbooks-desktop/fixed-asset-items` (`qbd.fixedAssetItems.create`) *async mode*: Create a fixed asset item
- `Task<FixedAssetItemDeleted> DeleteAsync(string id, FixedAssetItemDeleteParams parameters)`  
  `DELETE /v1/quickbooks-desktop/fixed-asset-items/{id}` (`qbd.fixedAssetItems.delete`) *async mode*: Delete a fixed asset item
- `Pager<FixedAssetItem> ListAsync(FixedAssetItemListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/fixed-asset-items` (`qbd.fixedAssetItems.list`) *paginated, async mode*: List fixed asset items
- `Task<FixedAssetItem> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/fixed-asset-items/{id}` (`qbd.fixedAssetItems.retrieve`) *async mode*: Retrieve a fixed asset item
- `Task<CustomFieldValue> SetCustomFieldAsync(string id, CustomFieldValueInput input)`  
  `POST /v1/quickbooks-desktop/fixed-asset-items/{id}/custom-fields` (`qbd.fixedAssetItems.setCustomField`) *async mode*: Set a custom field on a fixed asset item
- `Task<FixedAssetItem> UpdateAsync(string id, FixedAssetItemUpdateInput input)`  
  `POST /v1/quickbooks-desktop/fixed-asset-items/{id}` (`qbd.fixedAssetItems.update`) *async mode*: Update a fixed asset item

## client.Qbd.InventoryAdjustments

Inventory adjustments.

- `Task<InventoryAdjustment> CreateAsync(InventoryAdjustmentCreateInput input)`  
  `POST /v1/quickbooks-desktop/inventory-adjustments` (`qbd.inventoryAdjustments.create`) *async mode*: Create an inventory adjustment
- `Task<InventoryAdjustmentDeleted> DeleteAsync(string id)`  
  `DELETE /v1/quickbooks-desktop/inventory-adjustments/{id}` (`qbd.inventoryAdjustments.delete`) *async mode*: Delete an inventory adjustment
- `Task<InventoryAdjustmentList> ListAsync(InventoryAdjustmentListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/inventory-adjustments` (`qbd.inventoryAdjustments.list`) *async mode*: List inventory adjustments
- `Task<InventoryAdjustment> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/inventory-adjustments/{id}` (`qbd.inventoryAdjustments.retrieve`) *async mode*: Retrieve an inventory adjustment
- `Task<CustomFieldValue> SetCustomFieldAsync(string id, TransactionCustomFieldValueInput input)`  
  `POST /v1/quickbooks-desktop/inventory-adjustments/{id}/custom-fields` (`qbd.inventoryAdjustments.setCustomField`) *async mode*: Set a custom field on an inventory adjustment
- `Task<InventoryAdjustment> UpdateAsync(string id, InventoryAdjustmentUpdateInput input)`  
  `POST /v1/quickbooks-desktop/inventory-adjustments/{id}` (`qbd.inventoryAdjustments.update`) *async mode*: Update an inventory adjustment
- `Task<InventoryAdjustmentVoided> VoidAsync(string id)`  
  `POST /v1/quickbooks-desktop/inventory-adjustments/{id}/void` (`qbd.inventoryAdjustments.void`) *async mode*: Void an inventory adjustment

## client.Qbd.InventoryAssemblyItems

Inventory assembly items.

- `Task<AssemblyBuildCapacity> CanBuildAsync(string id, InventoryAssemblyItemCanBuildParams parameters)`  
  `GET /v1/quickbooks-desktop/inventory-assembly-items/{id}/can-build` (`qbd.inventoryAssemblyItems.canBuild`) *async mode*: Check how many assemblies can be built
- `Task<InventoryAssemblyItem> CreateAsync(InventoryAssemblyItemCreateInput input)`  
  `POST /v1/quickbooks-desktop/inventory-assembly-items` (`qbd.inventoryAssemblyItems.create`) *async mode*: Create an inventory assembly item
- `Task<InventoryAssemblyItemDeleted> DeleteAsync(string id, InventoryAssemblyItemDeleteParams parameters)`  
  `DELETE /v1/quickbooks-desktop/inventory-assembly-items/{id}` (`qbd.inventoryAssemblyItems.delete`) *async mode*: Delete an inventory assembly item
- `Pager<InventoryAssemblyItem> ListAsync(InventoryAssemblyItemListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/inventory-assembly-items` (`qbd.inventoryAssemblyItems.list`) *paginated, async mode*: List inventory assembly items
- `Task<InventoryAssemblyItem> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/inventory-assembly-items/{id}` (`qbd.inventoryAssemblyItems.retrieve`) *async mode*: Retrieve an inventory assembly item
- `Task<CustomFieldValue> SetCustomFieldAsync(string id, CustomFieldValueInput input)`  
  `POST /v1/quickbooks-desktop/inventory-assembly-items/{id}/custom-fields` (`qbd.inventoryAssemblyItems.setCustomField`) *async mode*: Set a custom field on an inventory assembly item
- `Task<InventoryAssemblyItem> UpdateAsync(string id, InventoryAssemblyItemUpdateInput input)`  
  `POST /v1/quickbooks-desktop/inventory-assembly-items/{id}` (`qbd.inventoryAssemblyItems.update`) *async mode*: Update an inventory assembly item

## client.Qbd.InventoryItems

Inventory items.

- `Task<InventoryItem> CreateAsync(InventoryItemCreateInput input)`  
  `POST /v1/quickbooks-desktop/inventory-items` (`qbd.inventoryItems.create`) *async mode*: Create an inventory item
- `Task<InventoryItemDeleted> DeleteAsync(string id, InventoryItemDeleteParams parameters)`  
  `DELETE /v1/quickbooks-desktop/inventory-items/{id}` (`qbd.inventoryItems.delete`) *async mode*: Delete an inventory item
- `Pager<InventoryItem> ListAsync(InventoryItemListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/inventory-items` (`qbd.inventoryItems.list`) *paginated, async mode*: List inventory items
- `Task<ListMerge> MergeAsync(string id, ListMergeInput input)`  
  `POST /v1/quickbooks-desktop/inventory-items/{id}/merge` (`qbd.inventoryItems.merge`) *async mode*: Merge an inventory item into another
- `Task<InventoryItem> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/inventory-items/{id}` (`qbd.inventoryItems.retrieve`) *async mode*: Retrieve an inventory item
- `Task<CustomFieldValue> SetCustomFieldAsync(string id, CustomFieldValueInput input)`  
  `POST /v1/quickbooks-desktop/inventory-items/{id}/custom-fields` (`qbd.inventoryItems.setCustomField`) *async mode*: Set a custom field on an inventory item
- `Task<InventoryItem> UpdateAsync(string id, InventoryItemUpdateInput input)`  
  `POST /v1/quickbooks-desktop/inventory-items/{id}` (`qbd.inventoryItems.update`) *async mode*: Update an inventory item

## client.Qbd.InventorySites

Inventory sites.

- `Task<InventorySite> CreateAsync(InventorySiteCreateInput input)`  
  `POST /v1/quickbooks-desktop/inventory-sites` (`qbd.inventorySites.create`) *async mode*: Create an inventory site
- `Task<InventorySiteDeleted> DeleteAsync(string id, InventorySiteDeleteParams parameters)`  
  `DELETE /v1/quickbooks-desktop/inventory-sites/{id}` (`qbd.inventorySites.delete`) *async mode*: Delete an inventory site
- `Task<InventorySiteList> ListAsync(InventorySiteListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/inventory-sites` (`qbd.inventorySites.list`) *async mode*: List inventory sites
- `Task<InventorySite> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/inventory-sites/{id}` (`qbd.inventorySites.retrieve`) *async mode*: Retrieve an inventory site
- `Task<InventorySite> UpdateAsync(string id, InventorySiteUpdateInput input)`  
  `POST /v1/quickbooks-desktop/inventory-sites/{id}` (`qbd.inventorySites.update`) *async mode*: Update an inventory site

## client.Qbd.InventoryTransfers

Inventory transfers.

- `Task<InventoryTransfer> CreateAsync(InventoryTransferCreateInput input)`  
  `POST /v1/quickbooks-desktop/inventory-transfers` (`qbd.inventoryTransfers.create`) *async mode*: Create an inventory transfer
- `Task<InventoryTransferDeleted> DeleteAsync(string id)`  
  `DELETE /v1/quickbooks-desktop/inventory-transfers/{id}` (`qbd.inventoryTransfers.delete`) *async mode*: Delete an inventory transfer
- `Pager<InventoryTransfer> ListAsync(InventoryTransferListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/inventory-transfers` (`qbd.inventoryTransfers.list`) *paginated, async mode*: List inventory transfers
- `Task<InventoryTransfer> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/inventory-transfers/{id}` (`qbd.inventoryTransfers.retrieve`) *async mode*: Retrieve an inventory transfer
- `Task<InventoryTransfer> UpdateAsync(string id, InventoryTransferUpdateInput input)`  
  `POST /v1/quickbooks-desktop/inventory-transfers/{id}` (`qbd.inventoryTransfers.update`) *async mode*: Update an inventory transfer

## client.Qbd.Invoices

Invoices.

- `Task<Invoice> CreateAsync(InvoiceCreateInput input)`  
  `POST /v1/quickbooks-desktop/invoices` (`qbd.invoices.create`) *async mode*: Create an invoice
- `Task<InvoiceDeleted> DeleteAsync(string id)`  
  `DELETE /v1/quickbooks-desktop/invoices/{id}` (`qbd.invoices.delete`) *async mode*: Delete an invoice
- `Pager<Invoice> ListAsync(InvoiceListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/invoices` (`qbd.invoices.list`) *paginated, async mode*: List invoices
- `Task<Invoice> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/invoices/{id}` (`qbd.invoices.retrieve`) *async mode*: Retrieve an invoice
- `Task<CustomFieldValue> SetCustomFieldAsync(string id, TransactionCustomFieldValueInput input)`  
  `POST /v1/quickbooks-desktop/invoices/{id}/custom-fields` (`qbd.invoices.setCustomField`) *async mode*: Set a custom field on an invoice
- `Task<Invoice> UpdateAsync(string id, InvoiceUpdateInput input)`  
  `POST /v1/quickbooks-desktop/invoices/{id}` (`qbd.invoices.update`) *async mode*: Update an invoice
- `Task<InvoiceVoided> VoidAsync(string id)`  
  `POST /v1/quickbooks-desktop/invoices/{id}/void` (`qbd.invoices.void`) *async mode*: Void an invoice

## client.Qbd.ItemGroups

Item groups.

- `Task<ItemGroup> CreateAsync(ItemGroupCreateInput input)`  
  `POST /v1/quickbooks-desktop/item-groups` (`qbd.itemGroups.create`) *async mode*: Create an item group
- `Task<ItemGroupDeleted> DeleteAsync(string id, ItemGroupDeleteParams parameters)`  
  `DELETE /v1/quickbooks-desktop/item-groups/{id}` (`qbd.itemGroups.delete`) *async mode*: Delete an item group
- `Pager<ItemGroup> ListAsync(ItemGroupListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/item-groups` (`qbd.itemGroups.list`) *paginated, async mode*: List item groups
- `Task<ListMerge> MergeAsync(string id, ListMergeInput input)`  
  `POST /v1/quickbooks-desktop/item-groups/{id}/merge` (`qbd.itemGroups.merge`) *async mode*: Merge an item group into another
- `Task<ItemGroup> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/item-groups/{id}` (`qbd.itemGroups.retrieve`) *async mode*: Retrieve an item group
- `Task<CustomFieldValue> SetCustomFieldAsync(string id, CustomFieldValueInput input)`  
  `POST /v1/quickbooks-desktop/item-groups/{id}/custom-fields` (`qbd.itemGroups.setCustomField`) *async mode*: Set a custom field on an item group
- `Task<ItemGroup> UpdateAsync(string id, ItemGroupUpdateInput input)`  
  `POST /v1/quickbooks-desktop/item-groups/{id}` (`qbd.itemGroups.update`) *async mode*: Update an item group

## client.Qbd.ItemReceipts

Item receipts.

- `Task<ItemReceipt> CreateAsync(ItemReceiptCreateInput input)`  
  `POST /v1/quickbooks-desktop/item-receipts` (`qbd.itemReceipts.create`) *async mode*: Create an item receipt
- `Task<ItemReceiptDeleted> DeleteAsync(string id)`  
  `DELETE /v1/quickbooks-desktop/item-receipts/{id}` (`qbd.itemReceipts.delete`) *async mode*: Delete an item receipt
- `Pager<ItemReceipt> ListAsync(ItemReceiptListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/item-receipts` (`qbd.itemReceipts.list`) *paginated, async mode*: List item receipts
- `Task<ItemReceipt> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/item-receipts/{id}` (`qbd.itemReceipts.retrieve`) *async mode*: Retrieve an item receipt
- `Task<CustomFieldValue> SetCustomFieldAsync(string id, TransactionCustomFieldValueInput input)`  
  `POST /v1/quickbooks-desktop/item-receipts/{id}/custom-fields` (`qbd.itemReceipts.setCustomField`) *async mode*: Set a custom field on an item receipt
- `Task<ItemReceipt> UpdateAsync(string id, ItemReceiptUpdateInput input)`  
  `POST /v1/quickbooks-desktop/item-receipts/{id}` (`qbd.itemReceipts.update`) *async mode*: Update an item receipt
- `Task<ItemReceiptVoided> VoidAsync(string id)`  
  `POST /v1/quickbooks-desktop/item-receipts/{id}/void` (`qbd.itemReceipts.void`) *async mode*: Void an item receipt

## client.Qbd.Items

Items.

- `Pager<IItem> ListAsync(ItemListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/items` (`qbd.items.list`) *paginated, async mode*: List items
- `Task<IItem> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/items/{id}` (`qbd.items.retrieve`) *async mode*: Retrieve an item

## client.Qbd.ItemSites

Item sites.

- `Pager<ItemSite> ListAsync(ItemSiteListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/item-sites` (`qbd.itemSites.list`) *paginated, async mode*: List item sites
- `Task<ItemSite> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/item-sites/{id}` (`qbd.itemSites.retrieve`) *async mode*: Retrieve an item site

## client.Qbd.JobTypes

Job types.

- `Task<JobType> CreateAsync(JobTypeCreateInput input)`  
  `POST /v1/quickbooks-desktop/job-types` (`qbd.jobTypes.create`) *async mode*: Create a job type
- `Task<JobTypeDeleted> DeleteAsync(string id, JobTypeDeleteParams parameters)`  
  `DELETE /v1/quickbooks-desktop/job-types/{id}` (`qbd.jobTypes.delete`) *async mode*: Delete a job type
- `Task<JobTypeList> ListAsync(JobTypeListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/job-types` (`qbd.jobTypes.list`) *async mode*: List job types
- `Task<JobType> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/job-types/{id}` (`qbd.jobTypes.retrieve`) *async mode*: Retrieve a job type

## client.Qbd.JournalEntries

Journal entries.

- `Task<JournalEntry> CreateAsync(JournalEntryCreateInput input)`  
  `POST /v1/quickbooks-desktop/journal-entries` (`qbd.journalEntries.create`) *async mode*: Create a journal entry
- `Task<JournalEntryDeleted> DeleteAsync(string id)`  
  `DELETE /v1/quickbooks-desktop/journal-entries/{id}` (`qbd.journalEntries.delete`) *async mode*: Delete a journal entry
- `Pager<JournalEntry> ListAsync(JournalEntryListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/journal-entries` (`qbd.journalEntries.list`) *paginated, async mode*: List journal entries
- `Task<JournalEntry> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/journal-entries/{id}` (`qbd.journalEntries.retrieve`) *async mode*: Retrieve a journal entry
- `Task<CustomFieldValue> SetCustomFieldAsync(string id, TransactionCustomFieldValueInput input)`  
  `POST /v1/quickbooks-desktop/journal-entries/{id}/custom-fields` (`qbd.journalEntries.setCustomField`) *async mode*: Set a custom field on a journal entry
- `Task<JournalEntry> UpdateAsync(string id, JournalEntryUpdateInput input)`  
  `POST /v1/quickbooks-desktop/journal-entries/{id}` (`qbd.journalEntries.update`) *async mode*: Update a journal entry
- `Task<JournalEntryVoided> VoidAsync(string id)`  
  `POST /v1/quickbooks-desktop/journal-entries/{id}/void` (`qbd.journalEntries.void`) *async mode*: Void a journal entry

## client.Qbd.Leads

Leads.

- `Task<Lead> CreateAsync(LeadCreateInput input)`  
  `POST /v1/quickbooks-desktop/leads` (`qbd.leads.create`) *async mode*: Create a lead
- `Pager<Lead> ListAsync(LeadListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/leads` (`qbd.leads.list`) *paginated, async mode*: List leads
- `Task<Lead> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/leads/{id}` (`qbd.leads.retrieve`) *async mode*: Retrieve a lead
- `Task<Lead> UpdateAsync(string id, LeadUpdateInput input)`  
  `POST /v1/quickbooks-desktop/leads/{id}` (`qbd.leads.update`) *async mode*: Update a lead

## client.Qbd.NonInventoryItems

Non inventory items.

- `Task<NonInventoryItem> CreateAsync(NonInventoryItemCreateInput input)`  
  `POST /v1/quickbooks-desktop/non-inventory-items` (`qbd.nonInventoryItems.create`) *async mode*: Create a non-inventory item
- `Task<NonInventoryItemDeleted> DeleteAsync(string id, NonInventoryItemDeleteParams parameters)`  
  `DELETE /v1/quickbooks-desktop/non-inventory-items/{id}` (`qbd.nonInventoryItems.delete`) *async mode*: Delete a non-inventory item
- `Pager<NonInventoryItem> ListAsync(NonInventoryItemListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/non-inventory-items` (`qbd.nonInventoryItems.list`) *paginated, async mode*: List non-inventory items
- `Task<ListMerge> MergeAsync(string id, ListMergeInput input)`  
  `POST /v1/quickbooks-desktop/non-inventory-items/{id}/merge` (`qbd.nonInventoryItems.merge`) *async mode*: Merge a non-inventory item into another
- `Task<NonInventoryItem> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/non-inventory-items/{id}` (`qbd.nonInventoryItems.retrieve`) *async mode*: Retrieve a non-inventory item
- `Task<CustomFieldValue> SetCustomFieldAsync(string id, CustomFieldValueInput input)`  
  `POST /v1/quickbooks-desktop/non-inventory-items/{id}/custom-fields` (`qbd.nonInventoryItems.setCustomField`) *async mode*: Set a custom field on a non-inventory item
- `Task<NonInventoryItem> UpdateAsync(string id, NonInventoryItemUpdateInput input)`  
  `POST /v1/quickbooks-desktop/non-inventory-items/{id}` (`qbd.nonInventoryItems.update`) *async mode*: Update a non-inventory item

## client.Qbd.OtherChargeItems

Other charge items.

- `Task<OtherChargeItem> CreateAsync(OtherChargeItemCreateInput input)`  
  `POST /v1/quickbooks-desktop/other-charge-items` (`qbd.otherChargeItems.create`) *async mode*: Create an other charge item
- `Task<OtherChargeItemDeleted> DeleteAsync(string id, OtherChargeItemDeleteParams parameters)`  
  `DELETE /v1/quickbooks-desktop/other-charge-items/{id}` (`qbd.otherChargeItems.delete`) *async mode*: Delete an other charge item
- `Pager<OtherChargeItem> ListAsync(OtherChargeItemListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/other-charge-items` (`qbd.otherChargeItems.list`) *paginated, async mode*: List other charge items
- `Task<ListMerge> MergeAsync(string id, ListMergeInput input)`  
  `POST /v1/quickbooks-desktop/other-charge-items/{id}/merge` (`qbd.otherChargeItems.merge`) *async mode*: Merge an other charge item into another
- `Task<OtherChargeItem> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/other-charge-items/{id}` (`qbd.otherChargeItems.retrieve`) *async mode*: Retrieve an other charge item
- `Task<CustomFieldValue> SetCustomFieldAsync(string id, CustomFieldValueInput input)`  
  `POST /v1/quickbooks-desktop/other-charge-items/{id}/custom-fields` (`qbd.otherChargeItems.setCustomField`) *async mode*: Set a custom field on an other charge item
- `Task<OtherChargeItem> UpdateAsync(string id, OtherChargeItemUpdateInput input)`  
  `POST /v1/quickbooks-desktop/other-charge-items/{id}` (`qbd.otherChargeItems.update`) *async mode*: Update an other charge item

## client.Qbd.OtherNames

Other names.

- `Task<OtherName> CreateAsync(OtherNameCreateInput input)`  
  `POST /v1/quickbooks-desktop/other-names` (`qbd.otherNames.create`) *async mode*: Create an other name
- `Task<OtherNameDeleted> DeleteAsync(string id, OtherNameDeleteParams parameters)`  
  `DELETE /v1/quickbooks-desktop/other-names/{id}` (`qbd.otherNames.delete`) *async mode*: Delete an other name
- `Task<OtherNameList> ListAsync(OtherNameListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/other-names` (`qbd.otherNames.list`) *async mode*: List other names
- `Task<OtherName> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/other-names/{id}` (`qbd.otherNames.retrieve`) *async mode*: Retrieve an other name
- `Task<CustomFieldValue> SetCustomFieldAsync(string id, CustomFieldValueInput input)`  
  `POST /v1/quickbooks-desktop/other-names/{id}/custom-fields` (`qbd.otherNames.setCustomField`) *async mode*: Set a custom field on an other name
- `Task<OtherName> UpdateAsync(string id, OtherNameUpdateInput input)`  
  `POST /v1/quickbooks-desktop/other-names/{id}` (`qbd.otherNames.update`) *async mode*: Update an other name

## client.Qbd.PaymentItems

Payment items.

- `Task<PaymentItem> CreateAsync(PaymentItemCreateInput input)`  
  `POST /v1/quickbooks-desktop/payment-items` (`qbd.paymentItems.create`) *async mode*: Create a payment item
- `Task<PaymentItemDeleted> DeleteAsync(string id, PaymentItemDeleteParams parameters)`  
  `DELETE /v1/quickbooks-desktop/payment-items/{id}` (`qbd.paymentItems.delete`) *async mode*: Delete a payment item
- `Pager<PaymentItem> ListAsync(PaymentItemListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/payment-items` (`qbd.paymentItems.list`) *paginated, async mode*: List payment items
- `Task<ListMerge> MergeAsync(string id, ListMergeInput input)`  
  `POST /v1/quickbooks-desktop/payment-items/{id}/merge` (`qbd.paymentItems.merge`) *async mode*: Merge a payment item into another
- `Task<PaymentItem> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/payment-items/{id}` (`qbd.paymentItems.retrieve`) *async mode*: Retrieve a payment item
- `Task<CustomFieldValue> SetCustomFieldAsync(string id, CustomFieldValueInput input)`  
  `POST /v1/quickbooks-desktop/payment-items/{id}/custom-fields` (`qbd.paymentItems.setCustomField`) *async mode*: Set a custom field on a payment item
- `Task<PaymentItem> UpdateAsync(string id, PaymentItemUpdateInput input)`  
  `POST /v1/quickbooks-desktop/payment-items/{id}` (`qbd.paymentItems.update`) *async mode*: Update a payment item

## client.Qbd.PaymentMethods

Payment methods.

- `Task<PaymentMethod> CreateAsync(PaymentMethodCreateInput input)`  
  `POST /v1/quickbooks-desktop/payment-methods` (`qbd.paymentMethods.create`) *async mode*: Create a payment method
- `Task<PaymentMethodDeleted> DeleteAsync(string id, PaymentMethodDeleteParams parameters)`  
  `DELETE /v1/quickbooks-desktop/payment-methods/{id}` (`qbd.paymentMethods.delete`) *async mode*: Delete a payment method
- `Task<PaymentMethodList> ListAsync(PaymentMethodListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/payment-methods` (`qbd.paymentMethods.list`) *async mode*: List payment methods
- `Task<PaymentMethod> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/payment-methods/{id}` (`qbd.paymentMethods.retrieve`) *async mode*: Retrieve a payment method

## client.Qbd.PaymentsToDeposit

Payments to deposit.

- `Task<PaymentsToDepositList> ListAsync()`  
  `GET /v1/quickbooks-desktop/payments-to-deposit` (`qbd.paymentsToDeposit.list`) *async mode*: List payment to deposits

## client.Qbd.PayrollNonWageItems

Payroll non-wage items.

- `Task<PayrollNonWageItemDeleted> DeleteAsync(string id, PayrollNonWageItemDeleteParams parameters)`  
  `DELETE /v1/quickbooks-desktop/payroll-non-wage-items/{id}` (`qbd.payrollNonWageItems.delete`) *async mode*: Delete a payroll non-wage item
- `Task<PayrollNonWageItemList> ListAsync(PayrollNonWageItemListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/payroll-non-wage-items` (`qbd.payrollNonWageItems.list`) *async mode*: List payroll non-wage items
- `Task<PayrollNonWageItem> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/payroll-non-wage-items/{id}` (`qbd.payrollNonWageItems.retrieve`) *async mode*: Retrieve a payroll non-wage item

## client.Qbd.PayrollWageItems

Payroll wage items.

- `Task<PayrollWageItem> CreateAsync(PayrollWageItemCreateInput input)`  
  `POST /v1/quickbooks-desktop/payroll-wage-items` (`qbd.payrollWageItems.create`) *async mode*: Create a payroll wage item
- `Task<PayrollWageItemDeleted> DeleteAsync(string id, PayrollWageItemDeleteParams parameters)`  
  `DELETE /v1/quickbooks-desktop/payroll-wage-items/{id}` (`qbd.payrollWageItems.delete`) *async mode*: Delete a payroll wage item
- `Task<PayrollWageItemList> ListAsync(PayrollWageItemListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/payroll-wage-items` (`qbd.payrollWageItems.list`) *async mode*: List payroll wage items
- `Task<PayrollWageItem> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/payroll-wage-items/{id}` (`qbd.payrollWageItems.retrieve`) *async mode*: Retrieve a payroll wage item
- `Task<PayrollWageItem> UpdateAsync(string id, PayrollWageItemUpdateInput input)`  
  `POST /v1/quickbooks-desktop/payroll-wage-items/{id}` (`qbd.payrollWageItems.update`) *async mode*: Update a payroll wage item

## client.Qbd.Preferences

Preferences.

- `Task<Preference> RetrieveAsync()`  
  `GET /v1/quickbooks-desktop/preferences` (`qbd.preferences.retrieve`) *async mode*: Retrieve company preferences

## client.Qbd.PriceLevels

Price levels.

- `Task<PriceLevel> CreateAsync(PriceLevelCreateInput input)`  
  `POST /v1/quickbooks-desktop/price-levels` (`qbd.priceLevels.create`) *async mode*: Create a price level
- `Task<PriceLevelDeleted> DeleteAsync(string id, PriceLevelDeleteParams parameters)`  
  `DELETE /v1/quickbooks-desktop/price-levels/{id}` (`qbd.priceLevels.delete`) *async mode*: Delete a price level
- `Task<PriceLevelList> ListAsync(PriceLevelListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/price-levels` (`qbd.priceLevels.list`) *async mode*: List price levels
- `Task<PriceLevel> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/price-levels/{id}` (`qbd.priceLevels.retrieve`) *async mode*: Retrieve a price level
- `Task<PriceLevel> UpdateAsync(string id, PriceLevelUpdateInput input)`  
  `POST /v1/quickbooks-desktop/price-levels/{id}` (`qbd.priceLevels.update`) *async mode*: Update a price level

## client.Qbd.PurchaseOrders

Purchase orders.

- `Task<PurchaseOrder> CreateAsync(PurchaseOrderCreateInput input)`  
  `POST /v1/quickbooks-desktop/purchase-orders` (`qbd.purchaseOrders.create`) *async mode*: Create a purchase order
- `Task<PurchaseOrderDeleted> DeleteAsync(string id)`  
  `DELETE /v1/quickbooks-desktop/purchase-orders/{id}` (`qbd.purchaseOrders.delete`) *async mode*: Delete a purchase order
- `Pager<PurchaseOrder> ListAsync(PurchaseOrderListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/purchase-orders` (`qbd.purchaseOrders.list`) *paginated, async mode*: List purchase orders
- `Task<PurchaseOrder> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/purchase-orders/{id}` (`qbd.purchaseOrders.retrieve`) *async mode*: Retrieve a purchase order
- `Task<CustomFieldValue> SetCustomFieldAsync(string id, TransactionCustomFieldValueInput input)`  
  `POST /v1/quickbooks-desktop/purchase-orders/{id}/custom-fields` (`qbd.purchaseOrders.setCustomField`) *async mode*: Set a custom field on a purchase order
- `Task<PurchaseOrder> UpdateAsync(string id, PurchaseOrderUpdateInput input)`  
  `POST /v1/quickbooks-desktop/purchase-orders/{id}` (`qbd.purchaseOrders.update`) *async mode*: Update a purchase order

## client.Qbd.ReceivePayments

Receive payments.

- `Task<ReceivePayment> CreateAsync(ReceivePaymentCreateInput input)`  
  `POST /v1/quickbooks-desktop/receive-payments` (`qbd.receivePayments.create`) *async mode*: Create a received payment
- `Task<ReceivePaymentDeleted> DeleteAsync(string id)`  
  `DELETE /v1/quickbooks-desktop/receive-payments/{id}` (`qbd.receivePayments.delete`) *async mode*: Delete a received payment
- `Pager<ReceivePayment> ListAsync(ReceivePaymentListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/receive-payments` (`qbd.receivePayments.list`) *paginated, async mode*: List received payments
- `Task<ReceivePayment> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/receive-payments/{id}` (`qbd.receivePayments.retrieve`) *async mode*: Retrieve a received payment
- `Task<CustomFieldValue> SetCustomFieldAsync(string id, TransactionCustomFieldValueInput input)`  
  `POST /v1/quickbooks-desktop/receive-payments/{id}/custom-fields` (`qbd.receivePayments.setCustomField`) *async mode*: Set a custom field on a received payment
- `Task<ReceivePayment> UpdateAsync(string id, ReceivePaymentUpdateInput input)`  
  `POST /v1/quickbooks-desktop/receive-payments/{id}` (`qbd.receivePayments.update`) *async mode*: Update a received payment

## client.Qbd.Reports

Reports.

- `Task<Report> AgingAsync(ReportAgingParams parameters)`  
  `GET /v1/quickbooks-desktop/reports/aging` (`qbd.reports.aging`) *async mode*: Run an aging report
- `Task<Report> BudgetSummaryAsync(ReportBudgetSummaryParams parameters)`  
  `GET /v1/quickbooks-desktop/reports/budget-summary` (`qbd.reports.budgetSummary`) *async mode*: Run a budget summary report
- `Task<Report> CustomDetailAsync(ReportCustomDetailParams parameters)`  
  `GET /v1/quickbooks-desktop/reports/custom-detail` (`qbd.reports.customDetail`) *async mode*: Run a custom detail report
- `Task<Report> CustomSummaryAsync(ReportCustomSummaryParams parameters)`  
  `GET /v1/quickbooks-desktop/reports/custom-summary` (`qbd.reports.customSummary`) *async mode*: Run a custom summary report
- `Task<Report> GeneralDetailAsync(ReportGeneralDetailParams parameters)`  
  `GET /v1/quickbooks-desktop/reports/general-detail` (`qbd.reports.generalDetail`) *async mode*: Run a general detail report
- `Task<Report> GeneralSummaryAsync(ReportGeneralSummaryParams parameters)`  
  `GET /v1/quickbooks-desktop/reports/general-summary` (`qbd.reports.generalSummary`) *async mode*: Run a general summary report
- `Task<Report> JobAsync(ReportJobParams parameters)`  
  `GET /v1/quickbooks-desktop/reports/job` (`qbd.reports.job`) *async mode*: Run a job report
- `Task<Report> PayrollDetailAsync(ReportPayrollDetailParams parameters)`  
  `GET /v1/quickbooks-desktop/reports/payroll-detail` (`qbd.reports.payrollDetail`) *async mode*: Run a payroll detail report
- `Task<Report> PayrollSummaryAsync(ReportPayrollSummaryParams parameters)`  
  `GET /v1/quickbooks-desktop/reports/payroll-summary` (`qbd.reports.payrollSummary`) *async mode*: Run a payroll summary report
- `Task<Report> TimeAsync(ReportTimeParams parameters)`  
  `GET /v1/quickbooks-desktop/reports/time` (`qbd.reports.time`) *async mode*: Run a time report

## client.Qbd.SalesOrders

Sales orders.

- `Task<SalesOrder> CreateAsync(SalesOrderCreateInput input)`  
  `POST /v1/quickbooks-desktop/sales-orders` (`qbd.salesOrders.create`) *async mode*: Create a sales order
- `Task<SalesOrderDeleted> DeleteAsync(string id)`  
  `DELETE /v1/quickbooks-desktop/sales-orders/{id}` (`qbd.salesOrders.delete`) *async mode*: Delete a sales order
- `Pager<SalesOrder> ListAsync(SalesOrderListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/sales-orders` (`qbd.salesOrders.list`) *paginated, async mode*: List sales orders
- `Task<SalesOrder> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/sales-orders/{id}` (`qbd.salesOrders.retrieve`) *async mode*: Retrieve a sales order
- `Task<CustomFieldValue> SetCustomFieldAsync(string id, TransactionCustomFieldValueInput input)`  
  `POST /v1/quickbooks-desktop/sales-orders/{id}/custom-fields` (`qbd.salesOrders.setCustomField`) *async mode*: Set a custom field on a sales order
- `Task<SalesOrder> UpdateAsync(string id, SalesOrderUpdateInput input)`  
  `POST /v1/quickbooks-desktop/sales-orders/{id}` (`qbd.salesOrders.update`) *async mode*: Update a sales order

## client.Qbd.SalesReceipts

Sales receipts.

- `Task<SalesReceipt> CreateAsync(SalesReceiptCreateInput input)`  
  `POST /v1/quickbooks-desktop/sales-receipts` (`qbd.salesReceipts.create`) *async mode*: Create a sales receipt
- `Task<SalesReceiptDeleted> DeleteAsync(string id)`  
  `DELETE /v1/quickbooks-desktop/sales-receipts/{id}` (`qbd.salesReceipts.delete`) *async mode*: Delete a sales receipt
- `Pager<SalesReceipt> ListAsync(SalesReceiptListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/sales-receipts` (`qbd.salesReceipts.list`) *paginated, async mode*: List sales receipts
- `Task<SalesReceipt> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/sales-receipts/{id}` (`qbd.salesReceipts.retrieve`) *async mode*: Retrieve a sales receipt
- `Task<CustomFieldValue> SetCustomFieldAsync(string id, TransactionCustomFieldValueInput input)`  
  `POST /v1/quickbooks-desktop/sales-receipts/{id}/custom-fields` (`qbd.salesReceipts.setCustomField`) *async mode*: Set a custom field on a sales receipt
- `Task<SalesReceipt> UpdateAsync(string id, SalesReceiptUpdateInput input)`  
  `POST /v1/quickbooks-desktop/sales-receipts/{id}` (`qbd.salesReceipts.update`) *async mode*: Update a sales receipt
- `Task<SalesReceiptVoided> VoidAsync(string id)`  
  `POST /v1/quickbooks-desktop/sales-receipts/{id}/void` (`qbd.salesReceipts.void`) *async mode*: Void a sales receipt

## client.Qbd.SalesRepresentatives

Sales representatives.

- `Task<SalesRepresentative> CreateAsync(SalesRepresentativeCreateInput input)`  
  `POST /v1/quickbooks-desktop/sales-representatives` (`qbd.salesRepresentatives.create`) *async mode*: Create a sales representative
- `Task<SalesRepresentativeDeleted> DeleteAsync(string id, SalesRepresentativeDeleteParams parameters)`  
  `DELETE /v1/quickbooks-desktop/sales-representatives/{id}` (`qbd.salesRepresentatives.delete`) *async mode*: Delete a sales representative
- `Task<SalesRepresentativeList> ListAsync(SalesRepresentativeListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/sales-representatives` (`qbd.salesRepresentatives.list`) *async mode*: List sales representatives
- `Task<SalesRepresentative> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/sales-representatives/{id}` (`qbd.salesRepresentatives.retrieve`) *async mode*: Retrieve a sales representative
- `Task<SalesRepresentative> UpdateAsync(string id, SalesRepresentativeUpdateInput input)`  
  `POST /v1/quickbooks-desktop/sales-representatives/{id}` (`qbd.salesRepresentatives.update`) *async mode*: Update a sales representative

## client.Qbd.SalesTaxCodes

Sales tax codes.

- `Task<SalesTaxCode> CreateAsync(SalesTaxCodeCreateInput input)`  
  `POST /v1/quickbooks-desktop/sales-tax-codes` (`qbd.salesTaxCodes.create`) *async mode*: Create a sales tax code
- `Task<SalesTaxCodeDeleted> DeleteAsync(string id, SalesTaxCodeDeleteParams parameters)`  
  `DELETE /v1/quickbooks-desktop/sales-tax-codes/{id}` (`qbd.salesTaxCodes.delete`) *async mode*: Delete a sales tax code
- `Task<SalesTaxCodeList> ListAsync(SalesTaxCodeListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/sales-tax-codes` (`qbd.salesTaxCodes.list`) *async mode*: List sales tax codes
- `Task<SalesTaxCode> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/sales-tax-codes/{id}` (`qbd.salesTaxCodes.retrieve`) *async mode*: Retrieve a sales tax code
- `Task<SalesTaxCode> UpdateAsync(string id, SalesTaxCodeUpdateInput input)`  
  `POST /v1/quickbooks-desktop/sales-tax-codes/{id}` (`qbd.salesTaxCodes.update`) *async mode*: Update a sales tax code

## client.Qbd.SalesTaxGroupItems

Sales tax group items.

- `Task<SalesTaxGroupItem> CreateAsync(SalesTaxGroupItemCreateInput input)`  
  `POST /v1/quickbooks-desktop/sales-tax-group-items` (`qbd.salesTaxGroupItems.create`) *async mode*: Create a sales tax group item
- `Task<SalesTaxGroupItemDeleted> DeleteAsync(string id, SalesTaxGroupItemDeleteParams parameters)`  
  `DELETE /v1/quickbooks-desktop/sales-tax-group-items/{id}` (`qbd.salesTaxGroupItems.delete`) *async mode*: Delete a sales tax group item
- `Pager<SalesTaxGroupItem> ListAsync(SalesTaxGroupItemListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/sales-tax-group-items` (`qbd.salesTaxGroupItems.list`) *paginated, async mode*: List sales tax group items
- `Task<ListMerge> MergeAsync(string id, ListMergeInput input)`  
  `POST /v1/quickbooks-desktop/sales-tax-group-items/{id}/merge` (`qbd.salesTaxGroupItems.merge`) *async mode*: Merge a sales tax group item into another
- `Task<SalesTaxGroupItem> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/sales-tax-group-items/{id}` (`qbd.salesTaxGroupItems.retrieve`) *async mode*: Retrieve a sales tax group item
- `Task<CustomFieldValue> SetCustomFieldAsync(string id, CustomFieldValueInput input)`  
  `POST /v1/quickbooks-desktop/sales-tax-group-items/{id}/custom-fields` (`qbd.salesTaxGroupItems.setCustomField`) *async mode*: Set a custom field on a sales tax group item
- `Task<SalesTaxGroupItem> UpdateAsync(string id, SalesTaxGroupItemUpdateInput input)`  
  `POST /v1/quickbooks-desktop/sales-tax-group-items/{id}` (`qbd.salesTaxGroupItems.update`) *async mode*: Update a sales tax group item

## client.Qbd.SalesTaxItems

Sales tax items.

- `Task<SalesTaxItem> CreateAsync(SalesTaxItemCreateInput input)`  
  `POST /v1/quickbooks-desktop/sales-tax-items` (`qbd.salesTaxItems.create`) *async mode*: Create a sales tax item
- `Task<SalesTaxItemDeleted> DeleteAsync(string id, SalesTaxItemDeleteParams parameters)`  
  `DELETE /v1/quickbooks-desktop/sales-tax-items/{id}` (`qbd.salesTaxItems.delete`) *async mode*: Delete a sales tax item
- `Pager<SalesTaxItem> ListAsync(SalesTaxItemListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/sales-tax-items` (`qbd.salesTaxItems.list`) *paginated, async mode*: List sales tax items
- `Task<ListMerge> MergeAsync(string id, ListMergeInput input)`  
  `POST /v1/quickbooks-desktop/sales-tax-items/{id}/merge` (`qbd.salesTaxItems.merge`) *async mode*: Merge a sales tax item into another
- `Task<SalesTaxItem> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/sales-tax-items/{id}` (`qbd.salesTaxItems.retrieve`) *async mode*: Retrieve a sales tax item
- `Task<CustomFieldValue> SetCustomFieldAsync(string id, CustomFieldValueInput input)`  
  `POST /v1/quickbooks-desktop/sales-tax-items/{id}/custom-fields` (`qbd.salesTaxItems.setCustomField`) *async mode*: Set a custom field on a sales tax item
- `Task<SalesTaxItem> UpdateAsync(string id, SalesTaxItemUpdateInput input)`  
  `POST /v1/quickbooks-desktop/sales-tax-items/{id}` (`qbd.salesTaxItems.update`) *async mode*: Update a sales tax item

## client.Qbd.SalesTaxPaymentChecks

Sales tax payment checks.

- `Task<SalesTaxPaymentCheck> CreateAsync(SalesTaxPaymentCheckCreateInput input)`  
  `POST /v1/quickbooks-desktop/sales-tax-payment-checks` (`qbd.salesTaxPaymentChecks.create`) *async mode*: Create a sales tax payment check
- `Task<SalesTaxPaymentCheckDeleted> DeleteAsync(string id)`  
  `DELETE /v1/quickbooks-desktop/sales-tax-payment-checks/{id}` (`qbd.salesTaxPaymentChecks.delete`) *async mode*: Delete a sales tax payment check
- `Pager<SalesTaxPaymentCheck> ListAsync(SalesTaxPaymentCheckListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/sales-tax-payment-checks` (`qbd.salesTaxPaymentChecks.list`) *paginated, async mode*: List sales tax payment checks
- `Task<SalesTaxPaymentCheck> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/sales-tax-payment-checks/{id}` (`qbd.salesTaxPaymentChecks.retrieve`) *async mode*: Retrieve a sales tax payment check
- `Task<CustomFieldValue> SetCustomFieldAsync(string id, TransactionCustomFieldValueInput input)`  
  `POST /v1/quickbooks-desktop/sales-tax-payment-checks/{id}/custom-fields` (`qbd.salesTaxPaymentChecks.setCustomField`) *async mode*: Set a custom field on a sales tax payment check
- `Task<SalesTaxPaymentCheck> UpdateAsync(string id, SalesTaxPaymentCheckUpdateInput input)`  
  `POST /v1/quickbooks-desktop/sales-tax-payment-checks/{id}` (`qbd.salesTaxPaymentChecks.update`) *async mode*: Update a sales tax payment check

## client.Qbd.ServiceItems

Service items.

- `Task<ServiceItem> CreateAsync(ServiceItemCreateInput input)`  
  `POST /v1/quickbooks-desktop/service-items` (`qbd.serviceItems.create`) *async mode*: Create a service item
- `Task<ServiceItemDeleted> DeleteAsync(string id, ServiceItemDeleteParams parameters)`  
  `DELETE /v1/quickbooks-desktop/service-items/{id}` (`qbd.serviceItems.delete`) *async mode*: Delete a service item
- `Pager<ServiceItem> ListAsync(ServiceItemListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/service-items` (`qbd.serviceItems.list`) *paginated, async mode*: List service items
- `Task<ListMerge> MergeAsync(string id, ListMergeInput input)`  
  `POST /v1/quickbooks-desktop/service-items/{id}/merge` (`qbd.serviceItems.merge`) *async mode*: Merge a service item into another
- `Task<ServiceItem> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/service-items/{id}` (`qbd.serviceItems.retrieve`) *async mode*: Retrieve a service item
- `Task<CustomFieldValue> SetCustomFieldAsync(string id, CustomFieldValueInput input)`  
  `POST /v1/quickbooks-desktop/service-items/{id}/custom-fields` (`qbd.serviceItems.setCustomField`) *async mode*: Set a custom field on a service item
- `Task<ServiceItem> UpdateAsync(string id, ServiceItemUpdateInput input)`  
  `POST /v1/quickbooks-desktop/service-items/{id}` (`qbd.serviceItems.update`) *async mode*: Update a service item

## client.Qbd.ShippingMethods

Shipping methods.

- `Task<ShippingMethod> CreateAsync(ShippingMethodCreateInput input)`  
  `POST /v1/quickbooks-desktop/shipping-methods` (`qbd.shippingMethods.create`) *async mode*: Create a shipping method
- `Task<ShippingMethodDeleted> DeleteAsync(string id, ShippingMethodDeleteParams parameters)`  
  `DELETE /v1/quickbooks-desktop/shipping-methods/{id}` (`qbd.shippingMethods.delete`) *async mode*: Delete a shipping method
- `Task<ShippingMethodList> ListAsync(ShippingMethodListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/shipping-methods` (`qbd.shippingMethods.list`) *async mode*: List shipping methods
- `Task<ShippingMethod> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/shipping-methods/{id}` (`qbd.shippingMethods.retrieve`) *async mode*: Retrieve a shipping method

## client.Qbd.StandardTerms

Standard terms.

- `Task<StandardTerm> CreateAsync(StandardTermCreateInput input)`  
  `POST /v1/quickbooks-desktop/standard-terms` (`qbd.standardTerms.create`) *async mode*: Create a standard term
- `Task<StandardTermDeleted> DeleteAsync(string id, StandardTermDeleteParams parameters)`  
  `DELETE /v1/quickbooks-desktop/standard-terms/{id}` (`qbd.standardTerms.delete`) *async mode*: Delete a standard term
- `Task<StandardTermList> ListAsync(StandardTermListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/standard-terms` (`qbd.standardTerms.list`) *async mode*: List standard terms
- `Task<StandardTerm> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/standard-terms/{id}` (`qbd.standardTerms.retrieve`) *async mode*: Retrieve a standard term

## client.Qbd.StatementCharges

Statement charges.

- `Task<StatementCharge> CreateAsync(StatementChargeCreateInput input)`  
  `POST /v1/quickbooks-desktop/statement-charges` (`qbd.statementCharges.create`) *async mode*: Create a statement charge
- `Task<StatementChargeDeleted> DeleteAsync(string id)`  
  `DELETE /v1/quickbooks-desktop/statement-charges/{id}` (`qbd.statementCharges.delete`) *async mode*: Delete a statement charge
- `Pager<StatementCharge> ListAsync(StatementChargeListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/statement-charges` (`qbd.statementCharges.list`) *paginated, async mode*: List statement charges
- `Task<StatementCharge> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/statement-charges/{id}` (`qbd.statementCharges.retrieve`) *async mode*: Retrieve a statement charge
- `Task<CustomFieldValue> SetCustomFieldAsync(string id, TransactionCustomFieldValueInput input)`  
  `POST /v1/quickbooks-desktop/statement-charges/{id}/custom-fields` (`qbd.statementCharges.setCustomField`) *async mode*: Set a custom field on a statement charge
- `Task<StatementCharge> UpdateAsync(string id, StatementChargeUpdateInput input)`  
  `POST /v1/quickbooks-desktop/statement-charges/{id}` (`qbd.statementCharges.update`) *async mode*: Update a statement charge
- `Task<StatementChargeVoided> VoidAsync(string id)`  
  `POST /v1/quickbooks-desktop/statement-charges/{id}/void` (`qbd.statementCharges.void`) *async mode*: Void a statement charge

## client.Qbd.SubtotalItems

Subtotal items.

- `Task<SubtotalItem> CreateAsync(SubtotalItemCreateInput input)`  
  `POST /v1/quickbooks-desktop/subtotal-items` (`qbd.subtotalItems.create`) *async mode*: Create a subtotal item
- `Task<SubtotalItemDeleted> DeleteAsync(string id, SubtotalItemDeleteParams parameters)`  
  `DELETE /v1/quickbooks-desktop/subtotal-items/{id}` (`qbd.subtotalItems.delete`) *async mode*: Delete a subtotal item
- `Pager<SubtotalItem> ListAsync(SubtotalItemListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/subtotal-items` (`qbd.subtotalItems.list`) *paginated, async mode*: List subtotal items
- `Task<ListMerge> MergeAsync(string id, ListMergeInput input)`  
  `POST /v1/quickbooks-desktop/subtotal-items/{id}/merge` (`qbd.subtotalItems.merge`) *async mode*: Merge a subtotal item into another
- `Task<SubtotalItem> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/subtotal-items/{id}` (`qbd.subtotalItems.retrieve`) *async mode*: Retrieve a subtotal item
- `Task<CustomFieldValue> SetCustomFieldAsync(string id, CustomFieldValueInput input)`  
  `POST /v1/quickbooks-desktop/subtotal-items/{id}/custom-fields` (`qbd.subtotalItems.setCustomField`) *async mode*: Set a custom field on a subtotal item
- `Task<SubtotalItem> UpdateAsync(string id, SubtotalItemUpdateInput input)`  
  `POST /v1/quickbooks-desktop/subtotal-items/{id}` (`qbd.subtotalItems.update`) *async mode*: Update a subtotal item

## client.Qbd.Templates

Templates.

- `Task<TemplateList> ListAsync()`  
  `GET /v1/quickbooks-desktop/templates` (`qbd.templates.list`) *async mode*: List templates

## client.Qbd.TimeTrackingActivities

Time tracking activities.

- `Task<TimeTrackingActivity> CreateAsync(TimeTrackingActivityCreateInput input)`  
  `POST /v1/quickbooks-desktop/time-tracking-activities` (`qbd.timeTrackingActivities.create`) *async mode*: Create a time tracking activity
- `Task<TimeTrackingActivityDeleted> DeleteAsync(string id)`  
  `DELETE /v1/quickbooks-desktop/time-tracking-activities/{id}` (`qbd.timeTrackingActivities.delete`) *async mode*: Delete a time tracking activity
- `Pager<TimeTrackingActivity> ListAsync(TimeTrackingActivityListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/time-tracking-activities` (`qbd.timeTrackingActivities.list`) *paginated, async mode*: List time tracking activities
- `Task<TimeTrackingActivity> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/time-tracking-activities/{id}` (`qbd.timeTrackingActivities.retrieve`) *async mode*: Retrieve a time tracking activity
- `Task<TimeTrackingActivity> UpdateAsync(string id, TimeTrackingActivityUpdateInput input)`  
  `POST /v1/quickbooks-desktop/time-tracking-activities/{id}` (`qbd.timeTrackingActivities.update`) *async mode*: Update a time tracking activity

## client.Qbd.ToDos

To-dos.

- `Task<ToDo> CreateAsync(ToDoCreateInput input)`  
  `POST /v1/quickbooks-desktop/to-dos` (`qbd.toDos.create`) *async mode*: Create a to-do
- `Task<ToDoDeleted> DeleteAsync(string id, ToDoDeleteParams parameters)`  
  `DELETE /v1/quickbooks-desktop/to-dos/{id}` (`qbd.toDos.delete`) *async mode*: Delete a to-do
- `Task<ToDoList> ListAsync(ToDoListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/to-dos` (`qbd.toDos.list`) *async mode*: List to-dos
- `Task<ToDo> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/to-dos/{id}` (`qbd.toDos.retrieve`) *async mode*: Retrieve a to-do
- `Task<ToDo> UpdateAsync(string id, ToDoUpdateInput input)`  
  `POST /v1/quickbooks-desktop/to-dos/{id}` (`qbd.toDos.update`) *async mode*: Update a to-do

## client.Qbd.Transactions

Transactions.

- `Pager<Transaction> ListAsync(TransactionListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/transactions` (`qbd.transactions.list`) *paginated, async mode*: List transactions
- `Task<Transaction> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/transactions/{id}` (`qbd.transactions.retrieve`) *async mode*: Retrieve a transaction
- `Task<TransactionClearedStatus> UpdateClearedStatusAsync(string id, ClearedStatusUpdateInput input)`  
  `POST /v1/quickbooks-desktop/transactions/{id}/cleared-status` (`qbd.transactions.updateClearedStatus`) *async mode*: Update the cleared status of a transaction

## client.Qbd.Transfers

Transfers.

- `Task<Transfer> CreateAsync(TransferCreateInput input)`  
  `POST /v1/quickbooks-desktop/transfers` (`qbd.transfers.create`) *async mode*: Create a transfer
- `Pager<Transfer> ListAsync(TransferListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/transfers` (`qbd.transfers.list`) *paginated, async mode*: List transfers
- `Task<Transfer> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/transfers/{id}` (`qbd.transfers.retrieve`) *async mode*: Retrieve a transfer
- `Task<Transfer> UpdateAsync(string id, TransferUpdateInput input)`  
  `POST /v1/quickbooks-desktop/transfers/{id}` (`qbd.transfers.update`) *async mode*: Update a transfer

## client.Qbd.UnitOfMeasureSets

Unit of measure sets.

- `Task<UnitOfMeasureSet> CreateAsync(UnitOfMeasureSetCreateInput input)`  
  `POST /v1/quickbooks-desktop/unit-of-measure-sets` (`qbd.unitOfMeasureSets.create`) *async mode*: Create an unit of measure set
- `Task<UnitOfMeasureSetList> ListAsync(UnitOfMeasureSetListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/unit-of-measure-sets` (`qbd.unitOfMeasureSets.list`) *async mode*: List unit of measure sets
- `Task<UnitOfMeasureSet> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/unit-of-measure-sets/{id}` (`qbd.unitOfMeasureSets.retrieve`) *async mode*: Retrieve an unit of measure set

## client.Qbd.VehicleMileage

Vehicle mileage.

- `Task<VehicleMileage> CreateAsync(VehicleMileageCreateInput input)`  
  `POST /v1/quickbooks-desktop/vehicle-mileage` (`qbd.vehicleMileage.create`) *async mode*: Create a vehicle mileage entry
- `Task<VehicleMileageDeleted> DeleteAsync(string id)`  
  `DELETE /v1/quickbooks-desktop/vehicle-mileage/{id}` (`qbd.vehicleMileage.delete`) *async mode*: Delete a vehicle mileage entry
- `Pager<VehicleMileage> ListAsync(VehicleMileageListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/vehicle-mileage` (`qbd.vehicleMileage.list`) *paginated, async mode*: List vehicle mileage entries
- `Task<VehicleMileage> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/vehicle-mileage/{id}` (`qbd.vehicleMileage.retrieve`) *async mode*: Retrieve a vehicle mileage entry

## client.Qbd.Vehicles

Vehicles.

- `Task<Vehicle> CreateAsync(VehicleCreateInput input)`  
  `POST /v1/quickbooks-desktop/vehicles` (`qbd.vehicles.create`) *async mode*: Create a vehicle
- `Task<VehicleDeleted> DeleteAsync(string id, VehicleDeleteParams parameters)`  
  `DELETE /v1/quickbooks-desktop/vehicles/{id}` (`qbd.vehicles.delete`) *async mode*: Delete a vehicle
- `Task<VehicleList> ListAsync(VehicleListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/vehicles` (`qbd.vehicles.list`) *async mode*: List vehicles
- `Task<Vehicle> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/vehicles/{id}` (`qbd.vehicles.retrieve`) *async mode*: Retrieve a vehicle
- `Task<Vehicle> UpdateAsync(string id, VehicleUpdateInput input)`  
  `POST /v1/quickbooks-desktop/vehicles/{id}` (`qbd.vehicles.update`) *async mode*: Update a vehicle

## client.Qbd.VendorCredits

Vendor credits.

- `Task<VendorCredit> CreateAsync(VendorCreditCreateInput input)`  
  `POST /v1/quickbooks-desktop/vendor-credits` (`qbd.vendorCredits.create`) *async mode*: Create a vendor credit
- `Task<VendorCreditDeleted> DeleteAsync(string id)`  
  `DELETE /v1/quickbooks-desktop/vendor-credits/{id}` (`qbd.vendorCredits.delete`) *async mode*: Delete a vendor credit
- `Pager<VendorCredit> ListAsync(VendorCreditListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/vendor-credits` (`qbd.vendorCredits.list`) *paginated, async mode*: List vendor credits
- `Task<VendorCredit> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/vendor-credits/{id}` (`qbd.vendorCredits.retrieve`) *async mode*: Retrieve a vendor credit
- `Task<CustomFieldValue> SetCustomFieldAsync(string id, TransactionCustomFieldValueInput input)`  
  `POST /v1/quickbooks-desktop/vendor-credits/{id}/custom-fields` (`qbd.vendorCredits.setCustomField`) *async mode*: Set a custom field on a vendor credit
- `Task<VendorCredit> UpdateAsync(string id, VendorCreditUpdateInput input)`  
  `POST /v1/quickbooks-desktop/vendor-credits/{id}` (`qbd.vendorCredits.update`) *async mode*: Update a vendor credit
- `Task<VendorCreditVoided> VoidAsync(string id)`  
  `POST /v1/quickbooks-desktop/vendor-credits/{id}/void` (`qbd.vendorCredits.void`) *async mode*: Void a vendor credit

## client.Qbd.Vendors

Vendors.

- `Task<Vendor> CreateAsync(VendorCreateInput input)`  
  `POST /v1/quickbooks-desktop/vendors` (`qbd.vendors.create`) *async mode*: Create a vendor
- `Task<VendorDeleted> DeleteAsync(string id, VendorDeleteParams parameters)`  
  `DELETE /v1/quickbooks-desktop/vendors/{id}` (`qbd.vendors.delete`) *async mode*: Delete a vendor
- `Pager<Vendor> ListAsync(VendorListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/vendors` (`qbd.vendors.list`) *paginated, async mode*: List vendors
- `Task<ListMerge> MergeAsync(string id, ListMergeInput input)`  
  `POST /v1/quickbooks-desktop/vendors/{id}/merge` (`qbd.vendors.merge`) *async mode*: Merge a vendor into another
- `Task<Vendor> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/vendors/{id}` (`qbd.vendors.retrieve`) *async mode*: Retrieve a vendor
- `Task<CustomFieldValue> SetCustomFieldAsync(string id, CustomFieldValueInput input)`  
  `POST /v1/quickbooks-desktop/vendors/{id}/custom-fields` (`qbd.vendors.setCustomField`) *async mode*: Set a custom field on a vendor
- `Task<Vendor> UpdateAsync(string id, VendorUpdateInput input)`  
  `POST /v1/quickbooks-desktop/vendors/{id}` (`qbd.vendors.update`) *async mode*: Update a vendor

## client.Qbd.VendorTypes

Vendor types.

- `Task<VendorType> CreateAsync(VendorTypeCreateInput input)`  
  `POST /v1/quickbooks-desktop/vendor-types` (`qbd.vendorTypes.create`) *async mode*: Create a vendor type
- `Task<VendorTypeDeleted> DeleteAsync(string id, VendorTypeDeleteParams parameters)`  
  `DELETE /v1/quickbooks-desktop/vendor-types/{id}` (`qbd.vendorTypes.delete`) *async mode*: Delete a vendor type
- `Task<VendorTypeList> ListAsync(VendorTypeListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/vendor-types` (`qbd.vendorTypes.list`) *async mode*: List vendor types
- `Task<VendorType> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/vendor-types/{id}` (`qbd.vendorTypes.retrieve`) *async mode*: Retrieve a vendor type

## client.Qbd.WorkersCompCodes

Workers' comp codes.

- `Task<WorkersCompCode> CreateAsync(WorkersCompCodeCreateInput input)`  
  `POST /v1/quickbooks-desktop/workers-comp-codes` (`qbd.workersCompCodes.create`) *async mode*: Create a workers' compensation code
- `Task<WorkersCompCodeDeleted> DeleteAsync(string id, WorkersCompCodeDeleteParams parameters)`  
  `DELETE /v1/quickbooks-desktop/workers-comp-codes/{id}` (`qbd.workersCompCodes.delete`) *async mode*: Delete a workers' compensation code
- `Task<WorkersCompCodeList> ListAsync(WorkersCompCodeListParams? parameters = null)`  
  `GET /v1/quickbooks-desktop/workers-comp-codes` (`qbd.workersCompCodes.list`) *async mode*: List workers' compensation codes
- `Task<WorkersCompCode> RetrieveAsync(string id)`  
  `GET /v1/quickbooks-desktop/workers-comp-codes/{id}` (`qbd.workersCompCodes.retrieve`) *async mode*: Retrieve a workers' compensation code
- `Task<WorkersCompCode> UpdateAsync(string id, WorkersCompCodeUpdateInput input)`  
  `POST /v1/quickbooks-desktop/workers-comp-codes/{id}` (`qbd.workersCompCodes.update`) *async mode*: Update a workers' compensation code

## client.Requests

Requests.

- `Task<Request> CancelAsync(string id)`  
  `POST /v1/requests/{id}/cancel` (`requests.cancel`): Cancel a request
- `Pager<RequestSummary> ListAsync(RequestListParams? parameters = null)`  
  `GET /v1/requests` (`requests.list`) *paginated*: List requests
- `Task<Request> RetrieveAsync(string id, RequestRetrieveParams? parameters = null)`  
  `GET /v1/requests/{id}` (`requests.retrieve`): Retrieve a request

## client.WebhookEndpoints

Webhooks.

- `Task<WebhookEndpointWithSecret> CreateAsync(WebhookEndpointCreateInput input)`  
  `POST /v1/webhook-endpoints` (`webhookEndpoints.create`): Create a webhook endpoint
- `Task<WebhookEndpointDeleted> DeleteAsync(string id)`  
  `DELETE /v1/webhook-endpoints/{id}` (`webhookEndpoints.delete`): Delete a webhook endpoint
- `Pager<WebhookEndpoint> ListAsync(WebhookEndpointListParams? parameters = null)`  
  `GET /v1/webhook-endpoints` (`webhookEndpoints.list`) *paginated*: List webhook endpoints
- `Pager<WebhookDelivery> ListDeliveriesAsync(string id, WebhookEndpointListDeliveriesParams? parameters = null)`  
  `GET /v1/webhook-endpoints/{id}/deliveries` (`webhookEndpoints.listDeliveries`) *paginated*: List deliveries
- `Task<WebhookDelivery> ResendDeliveryAsync(string id, string deliveryId)`  
  `POST /v1/webhook-endpoints/{id}/deliveries/{deliveryId}/resend` (`webhookEndpoints.resendDelivery`): Resend a delivery
- `Task<WebhookEndpoint> RetrieveAsync(string id)`  
  `GET /v1/webhook-endpoints/{id}` (`webhookEndpoints.retrieve`): Retrieve a webhook endpoint
- `Task<WebhookEndpointWithSecret> RotateSecretAsync(string id)`  
  `POST /v1/webhook-endpoints/{id}/rotate-secret` (`webhookEndpoints.rotateSecret`): Rotate the signing secret
- `Task<WebhookDelivery> SendTestEventAsync(string id)`  
  `POST /v1/webhook-endpoints/{id}/test` (`webhookEndpoints.sendTestEvent`): Send a test event
- `Task<WebhookEndpoint> UpdateAsync(string id, WebhookEndpointUpdateInput input)`  
  `POST /v1/webhook-endpoints/{id}` (`webhookEndpoints.update`): Update a webhook endpoint
