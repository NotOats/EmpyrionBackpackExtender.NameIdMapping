using EmpyrionBackpackExtender.NameIdMapping.Commands;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;
using System.IO.Abstractions;

namespace EmpyrionBackpackExtender.NameIdMapping;

public class Program
{
    public static async Task Main(string[] args)
    {
        AnsiConsole.Write(new FigletText("VB Tools").Centered());

        var app = new CommandApp(RegisterServices());
        app.Configure(config => ConfigureCommands(config));
        await app.RunAsync(args);
    }

    static ITypeRegistrar RegisterServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IFileSystem, FileSystem>();

        return new TypeRegistrar(services);
    }

    static IConfigurator ConfigureCommands(IConfigurator config)
    {
        config.CaseSensitivity(CaseSensitivity.None);
        config.ValidateExamples();

        config.AddCommand<CreateMapCommand>("create-map")
            .WithDescription("Creates a NameIdMapping.json file");

        return config;
    }

    // Spectre.Console.Cli DI Interfaces
    private sealed class TypeRegistrar : ITypeRegistrar
    {
        private readonly IServiceCollection _builder;

        public TypeRegistrar(IServiceCollection builder) =>
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));

        public ITypeResolver Build()
        {
            return new TypeResolver(_builder.BuildServiceProvider());
        }

        public void Register(Type service, Type implementation)
        {
            _builder.AddSingleton(service, implementation);
        }

        public void RegisterInstance(Type service, object implementation)
        {
            _builder.AddSingleton(service, implementation);
        }

        public void RegisterLazy(Type service, Func<object> factory)
        {
            if (factory is null)
                throw new ArgumentNullException(nameof(factory));

            _builder.AddSingleton(service, provider => factory());
        }
    }

    private sealed class TypeResolver : ITypeResolver, IDisposable
    {
        private readonly IServiceProvider _provider;
        public TypeResolver(IServiceProvider provider) =>
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));

        public void Dispose()
        {
            if (_provider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        public object? Resolve(Type? type)
        {
            if (type == null)
                return null;

            return _provider.GetRequiredService(type);
        }
    }
}