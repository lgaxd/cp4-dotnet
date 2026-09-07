using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;

namespace TarefasApi.Tests.Integracao;

/// <summary>
/// Fábrica de host de testes. Sobe a API inteira EM MEMÓRIA (sem porta TCP,
/// sem banco de dados) para que os testes de integração exercitem o pipeline
/// real: middlewares, model binding, validação, controllers, service e repositório.
/// </summary>
public class TarefasApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Ambiente "Testing": desliga o Swagger e os exportadores de console
        // do OpenTelemetry, deixando a saída dos testes limpa.
        builder.UseEnvironment("Testing");

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.Warning);
        });
    }
}
