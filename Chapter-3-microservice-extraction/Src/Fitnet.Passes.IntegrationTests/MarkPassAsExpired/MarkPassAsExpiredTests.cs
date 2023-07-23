namespace EvolutionaryArchitecture.Fitnet.Passes.IntegrationTests.MarkPassAsExpired;

using EvolutionaryArchitecture.Fitnet.Common.IntegrationTests.TestEngine.Database;
using Api;
using Api.GetAllPasses;
using Api.RegisterPass;
using Common.Infrastructure.Events.EventBus.InMemory;
using Common.IntegrationTests.TestEngine;
using Common.IntegrationTests.TestEngine.Configuration;
using Common.IntegrationTests.TestEngine.EventBus.External;
using Common.IntegrationTests.TestEngine.EventBus.InMemory;
using Contracts.IntegrationEvents;
using IntegrationEvents;
using RegisterPass;

public sealed class MarkPassAsExpiredTests : IClassFixture<FitnetWebApplicationFactory<Program>>, IClassFixture<DatabaseContainer>
{
    private static readonly StringContent EmptyContent = new(string.Empty);
    
    private readonly HttpClient _applicationHttpClient;
    private readonly Mock<IInMemoryEventBus> _testInMemoryEventBus = new();
    private readonly ITestHarness _testExternalEventBus;

    public MarkPassAsExpiredTests(FitnetWebApplicationFactory<Program> applicationInMemoryFactory,
        DatabaseContainer database)
    {
        var applicationInMemory = applicationInMemoryFactory
            .WithContainerDatabaseConfigured(new PassesDatabaseConfiguration(database.ConnectionString!))
            .WithTestInMemoryEventBus(_testInMemoryEventBus)
            .WithTestExternalEventBus(typeof(ContractSignedEventConsumer));
        _applicationHttpClient = applicationInMemory.CreateClient();
        _testExternalEventBus = applicationInMemory.GetTestExternalEventBus();
    }

    [Fact]
    internal async Task Given_valid_mark_pass_as_expired_request_Then_should_return_no_content()
    {
        // Arrange
        var contractSigned = await RegisterPass();
        var registeredPassId = await GetCreatedPass(contractSigned.ContractCustomerId);
        var url = BuildUrl(registeredPassId);

        // Act
        await _applicationHttpClient.PatchAsJsonAsync(url, EmptyContent);

        // Assert
        EnsureThatPassExpiredEventWasPublished();
    }
    
    [Fact]
    internal async Task Given_valid_mark_pass_as_expired_request_Then_should_publish_pass_expired_event()
    {
        // Arrange
        var contractSigned = await RegisterPass();
        var registeredPassId = await GetCreatedPass(contractSigned.ContractCustomerId);
        var url = BuildUrl(registeredPassId);

        // Act
        var markAsExpiredResponse = await _applicationHttpClient.PatchAsJsonAsync(url, EmptyContent);

        // Assert
        markAsExpiredResponse.Should().HaveStatusCode(HttpStatusCode.NoContent);
    }
    
    [Fact]
    internal async Task Given_mark_pass_as_expired_request_with_not_existing_id_Then_should_return_not_found()
    {
        // Arrange
        var notExistingId = Guid.NewGuid();
        var url = BuildUrl(notExistingId);

        // Act
        var markAsExpiredResponse = await _applicationHttpClient.PatchAsJsonAsync(url, EmptyContent);

        // Assert
        markAsExpiredResponse.Should().HaveStatusCode(HttpStatusCode.NotFound);
    }

    private async Task<ContractSignedEvent> RegisterPass()
    {
        var @event = ContractSignedEventFaker.Create();
        await _testExternalEventBus.Bus.Publish(@event);
        await _testExternalEventBus.WaitToConsumeMessageAsync<ContractSignedEvent>();

        return @event;
    }
    
    private async Task<Guid> GetCreatedPass(Guid customerId)
    {
        var getAllPassesResponse = await _applicationHttpClient.GetAsync(PassesApiPaths.GetAll);
        var response = await getAllPassesResponse.Content.ReadFromJsonAsync<GetAllPassesResponse>();
        var createdPass = response!.Passes.FirstOrDefault(pass => pass.CustomerId == customerId);
        createdPass.Should().NotBeNull();
        
        return createdPass!.Id;
    }
    
    private static string BuildUrl(Guid id) => PassesApiPaths.MarkPassAsExpired.Replace("{id}", id.ToString());

    private void EnsureThatPassExpiredEventWasPublished() => _testInMemoryEventBus.Verify(eventBus => eventBus.PublishAsync(It.IsAny<PassExpiredEvent>(), It.IsAny<CancellationToken>()), Times.Once);
}