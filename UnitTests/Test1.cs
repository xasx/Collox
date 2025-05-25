using Collox;
using Collox.ViewModels;
using Microsoft.VisualStudio.TestTools.UITesting;

namespace UnitTests;

[TestClass]
public sealed class Test1
{
    [UITestMethod]
    public void TestMethod1()
    {
        new App(); // Initialize the application to set up services and other dependencies
        WriteViewModel writeViewModel = new();
        writeViewModel.InputMessage = "Hello, World!";
        writeViewModel.IsSpeaking = true;
        writeViewModel.IsBeeping = true;
        writeViewModel.ChangeModeToWrite();
        writeViewModel.SubmitCommand.Execute(null);

    }
}
