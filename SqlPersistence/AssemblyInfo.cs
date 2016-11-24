using System.Reflection;
using System.Runtime.CompilerServices;
using Fody;

[assembly: AssemblyTitle("NServiceBus.Persistence.Sql")]
[assembly: AssemblyProduct("NServiceBus.Persistence.Sql")]
[assembly: InternalsVisibleTo("SqlPersistence.Tests")]
[assembly: ConfigureAwait(false)]