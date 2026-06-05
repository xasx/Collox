using Collox.Models;
using Collox.Services;
using NFluent;

namespace Collox.Tests.Services;

[TestClass]
public class ProcessorInitializerTests
{
    [TestMethod]
    public void Initialize_CalledTwiceWithSameProcessor_ReusesSameChatClientManager()
    {
        // Arrange
        var provider = new IntelligenceApiProvider
        {
            Id = Guid.NewGuid(),
            Name = "Test Provider",
            ApiType = AIProvider.Ollama,
            Endpoint = "http://localhost:11434"
        };
        var processor = new IntelligentProcessor
        {
            Id = Guid.NewGuid(),
            Name = "Test Processor",
            ApiProviderId = provider.Id
        };
        var apiProviders = new Dictionary<Guid, IntelligenceApiProvider>
        {
            [provider.Id] = provider
        };

        // Act
        ProcessorInitializer.Initialize([processor], apiProviders);
        var firstManager = processor.ClientManager;

        ProcessorInitializer.Initialize([processor], apiProviders);
        var secondManager = processor.ClientManager;

        // Assert
        Check.That(firstManager).IsNotNull();
        Check.That(secondManager).IsSameReferenceAs(firstManager);
    }
}
