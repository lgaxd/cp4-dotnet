# CP4 Monitoramento

Web API em ASP.NET Core 8 para gerenciamento de tarefas, com armazenamento em memória e recursos de observabilidade.

## Autor

Lucas Grillo Alcântara - RM 561413

## Recursos

- CRUD básico de tarefas: criação, listagem e consulta por ID;
- filtros, busca, ordenação e paginação;
- Swagger para testar a API;
- health checks, logging estruturado, métricas e tracing com OpenTelemetry;
- testes unitários e de integração.

## Tecnologias

- .NET 8 (`net8.0`);
- ASP.NET Core Web API;
- xUnit, Moq e `WebApplicationFactory`;
- OpenTelemetry e Swashbuckle.

Os dados são mantidos em memória e são perdidos quando a aplicação é reiniciada. Não é necessário configurar um banco de dados.

## Requisitos

- .NET SDK 8.0 ou superior;
- Visual Studio 2022 ou outro editor compatível.

## Executar a API

Na raiz da solução:

```bash
dotnet restore
dotnet run --project src/TarefasApi
```

Com o perfil padrão de desenvolvimento, acesse:

- Swagger: <http://localhost:5000/swagger>
- API: <http://localhost:5000/api/tarefas>
- Health check: <http://localhost:5000/health>
- Métricas: <http://localhost:5000/metrics>

Também é possível abrir `CP4.Monitoramento.sln` no Visual Studio e executar o projeto `TarefasApi`.

## Testes

Para executar todos os testes:

```bash
dotnet test
```

Os testes estão em `tests/TarefasApi.Tests`, separados entre testes unitários e testes de integração.

## Endpoints principais

| Método | Rota | Descrição |
| --- | --- | --- |
| `POST` | `/api/tarefas` | Cria uma tarefa |
| `GET` | `/api/tarefas` | Lista tarefas com filtros e paginação |
| `GET` | `/api/tarefas/{id}` | Consulta uma tarefa por ID |
| `GET` | `/health` | Verifica a saúde da aplicação |
| `GET` | `/health/live` | Verifica se a aplicação está em execução |
| `GET` | `/health/ready` | Verifica se a aplicação está pronta |
| `GET` | `/metrics` | Exibe métricas coletadas |
| `GET` | `/swagger` | Abre a documentação interativa |

### Criar tarefa

```json
{
  "titulo": "Estudar OpenTelemetry",
  "descricao": "Revisar métricas da aplicação",
  "prioridade": "Alta"
}
```

O campo `titulo` é obrigatório e deve ter entre 3 e 120 caracteres. `prioridade` aceita `Baixa`, `Media` ou `Alta`.

### Listar tarefas

Exemplo com filtros:

```text
GET /api/tarefas?prioridade=Alta&busca=estudar&pagina=1&tamanhoPagina=10
```

Os parâmetros disponíveis incluem `concluida`, `prioridade`, `busca`, `pagina`, `tamanhoPagina`, `ordenarPor` e `ordem`. A resposta informa os dados de paginação nos headers `X-Total-Itens`, `X-Pagina`, `X-Tamanho-Pagina` e `X-Total-Paginas`.

## Estrutura

```text
src/TarefasApi          API, regras de negócio, repositório e observabilidade
tests/TarefasApi.Tests  Testes unitários e de integração
```