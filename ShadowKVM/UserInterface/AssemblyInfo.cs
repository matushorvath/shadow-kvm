using System.Runtime.CompilerServices;
using System.Windows;

// Theme specific and generic resource dictionaies
[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]

// Allow the test project to access internal classes
[assembly: InternalsVisibleTo("ShadowKVM.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")] // for Moq
