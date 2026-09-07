# CP4 — .NET (Monitoramento e Qualidade)

API de gerenciamento de **Tarefas (To-Do)** com **Health Checks**, **logging estruturado**,
**métricas/tracing** e cobertura por **testes unitários** e **testes de integração**.

---

## 👥 Desenvolvedor do Projeto

| Nome | RM |
|------|----|
| Lucas Grillo Alcântara | 561413 |

---

## 🧱 Estrutura da solução

A solução contém **exatamente dois projetos**, conforme o enunciado:

```
CP4.Monitoramento/
├── CP4.Monitoramento.sln
├── README.md
├── .gitignore
│
├── src/
│   └── TarefasApi/                          ← PROJETO 1: Web API
│       ├── Program.cs                       Composition root (DI, logs, health, OTel, pipeline)
│       ├── Controllers/
│       │   └── TarefasController.cs         POST /api/tarefas, GET /api/tarefas, GET /api/tarefas/{id}
│       ├── Services/
│       │   ├── ITarefasService.cs
│       │   └── TarefasService.cs            ← CAMADA DE SERVIÇO (regras de negócio)
│       ├── Repositories/
│       │   ├── ITarefasRepository.cs        ← INTERFACE DE REPOSITÓRIO (é ela que o Moq mocka)
│       │   └── TarefasRepositoryEmMemoria.cs   Implementação em memória (sem banco de dados)
│       ├── Models/
│       │   ├── Tarefa.cs
│       │   ├── PrioridadeTarefa.cs
│       │   ├── CriarTarefaRequest.cs        Payload do POST + validações (DataAnnotations)
│       │   ├── FiltroTarefasQuery.cs        Parâmetros de query do GET
│       │   └── ResultadoPaginado.cs
│       ├── Middlewares/
│       │   └── MetricasRequisicoesMiddleware.cs  Cronometra e conta TODA requisição HTTP
│       └── Observabilidade/
│           ├── MetricasTarefas.cs               Meter/Counter/Histogram (OpenTelemetry)
│           ├── RepositorioTarefasHealthCheck.cs Health Check customizado
│           ├── EscritorRespostaHealthCheck.cs   Resposta JSON do /health
│           └── FabricaRespostaValidacao.cs      Log + métrica em todo 400 de validação
│
└── tests/
    └── TarefasApi.Tests/                    ← PROJETO 2: Testes Automatizados
        ├── Unitarios/
        │   └── TarefasServiceTests.cs       15 testes unitários (Moq + AAA)
        └── Integracao/
            ├── TarefasApiFactory.cs         WebApplicationFactory<Program>
            ├── TarefasControllerIntegrationTests.cs  10 testes dos controllers
            └── MonitoramentoIntegrationTests.cs      8 testes de health/métricas
```

**Fluxo da injeção de dependência:**

```
TarefasController  →  ITarefasService  →  ITarefasRepository
   (Scoped)            (TarefasService)     (TarefasRepositoryEmMemoria, Singleton)
```

---

## 🛠️ Requisitos

| Item | Versão |
|------|--------|
| .NET SDK | **8.0** ou superior (a solução tem como alvo `net8.0`) |
| Visual Studio | 2022 17.8+ / Visual Studio Insiders |
| Banco de dados | **Nenhum** — os dados ficam em memória |

Verifique o SDK com:

```bash
dotnet --list-sdks
```

---

## ▶️ Como executar

### Pelo Visual Studio (recomendado)

1. Abra **`CP4.Monitoramento.sln`**.
2. Clique com o botão direito em **`TarefasApi`** → **Definir como Projeto de Inicialização**.
3. Pressione **F5** (ou Ctrl+F5).
4. O navegador abre direto no **Swagger**: <http://localhost:5275/swagger>

### Pela linha de comando

```bash
dotnet run --project src/TarefasApi
```

---

## 🌐 Endpoints

| Método | Rota | Descrição | Status esperados |
|--------|------|-----------|------------------|
| `POST` | `/api/tarefas` | Cria uma nova tarefa | `201 Created` / `400 Bad Request` |
| `GET` | `/api/tarefas` | Lista as tarefas (com filtros) | `200 OK` / `400 Bad Request` |
| `GET` | `/api/tarefas/{id}` | Busca uma tarefa pelo Id | `200 OK` / `404 Not Found` |
| `GET` | `/health` | Health check completo (JSON) | `200 OK` — `"status": "Healthy"` |
| `GET` | `/health/live` | *Liveness* (texto puro) | `200 OK` — `Healthy` |
| `GET` | `/health/ready` | *Readiness* (dependências) | `200 OK` — `"status": "Healthy"` |
| `GET` | `/metrics` | Snapshot das métricas coletadas | `200 OK` |
| `GET` | `/swagger` | Documentação interativa | `200 OK` |

### Parâmetros do `POST /api/tarefas` (corpo JSON)

| Parâmetro | Tipo | Obrigatório | Regra |
|-----------|------|-------------|-------|
| `titulo` | `string` | ✅ **Sim** | 3 a 120 caracteres, não pode ser vazio ou só espaços |
| `descricao` | `string` | Não | Até 500 caracteres |
| `prioridade` | `enum` | Não | `Baixa` \| `Media` \| `Alta` (padrão: `Media`) |
| `dataConclusaoPrevista` | `datetime` | Não | Não pode estar no passado |

```json
{
  "titulo": "Estudar OpenTelemetry",
  "descricao": "Rever a aula de métricas",
  "prioridade": "Alta",
  "dataConclusaoPrevista": "2026-12-20T00:00:00Z"
}
```

### Parâmetros do `GET /api/tarefas` (query string)

| Parâmetro | Tipo | Padrão | Descrição |
|-----------|------|--------|-----------|
| `concluida` | `bool` | — | `true` = só concluídas, `false` = só pendentes |
| `prioridade` | `enum` | — | Filtra por `Baixa`, `Media` ou `Alta` |
| `busca` | `string` | — | Busca textual (sem diferenciar maiúsculas) no título e na descrição |
| `pagina` | `int` | `1` | Número da página (mínimo 1) |
| `tamanhoPagina` | `int` | `50` | Itens por página (1 a 100) |
| `ordenarPor` | `string` | `criadaEm` | `criadaEm`, `titulo` ou `prioridade` |
| `ordem` | `string` | `asc` | `asc` ou `desc` |

Exemplo:

```
GET /api/tarefas?prioridade=Alta&busca=estudar&ordenarPor=titulo&ordem=desc&pagina=1&tamanhoPagina=10
```

A resposta traz os metadados de paginação nos **headers**:
`X-Total-Itens`, `X-Pagina`, `X-Tamanho-Pagina`, `X-Total-Paginas`.
Toda resposta também devolve `X-Correlation-Id` (gerado pelo middleware de observabilidade).

---

## 📊 Monitoramento (Observabilidade)

### 1. Health Checks — nativos do .NET

Registrados em `Program.cs` com `AddHealthChecks()`:

- **`repositorio-tarefas`** (`RepositorioTarefasHealthCheck`) — verifica se o repositório responde;
- **`self`** — verifica se a aplicação está de pé.

`GET /health` devolve JSON detalhado:

```json
{
  "status": "Healthy",
  "duracaoTotalMs": 4.36,
  "verificacoes": [
    { "nome": "repositorio-tarefas", "status": "Healthy", "dados": { "totalTarefasEmMemoria": 3 } },
    { "nome": "self", "status": "Healthy", "descricao": "API no ar." }
  ]
}
```

### 2. Logging Estruturado — `ILogger`

Todos os logs usam **parâmetros nomeados**, nunca concatenação de string.

`Services/TarefasService.cs` — caso de sucesso:

```csharp
_logger.LogInformation(
    "Nova tarefa criada: {NomeTarefa} {TarefaId} {Prioridade} {CriadaEm}",
    tarefaCriada.Titulo, tarefaCriada.Id, tarefaCriada.Prioridade, tarefaCriada.CriadaEm);
```

`Services/TarefasService.cs` — caso de erro (tarefa inválida):

```csharp
_logger.LogWarning(
    "Falha de validação ao criar tarefa: título não informado. {Operacao} {MotivoRejeicao}",
    nameof(CriarTarefa), "titulo_obrigatorio");
```

`Observabilidade/FabricaRespostaValidacao.cs` — todo `400` de validação também é logado:

```csharp
logger.LogWarning(
    "Requisição rejeitada na validação do modelo. {HttpMethod} {RequestPath} {QuantidadeErros} {CamposInvalidos} {MensagensErro} {StatusCode}",
    ...);
```

> ℹ️ Como o `[ApiController]` devolve o `400` **antes** de a action executar, o log
> desse caso de erro é emitido pela `InvalidModelStateResponseFactory` — sem isso,
> nenhum log seria gerado para payloads inválidos.

### 3. Tracing e Métricas

Implementados das **duas** formas aceitas pelo enunciado:

**a) OpenTelemetry** (`Program.cs`):

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddMeter(MetricasTarefas.NomeMeter))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation());
```

Em `Development` o **Console Exporter** está ligado: os *traces* aparecem no console a
cada requisição e as métricas são exportadas a cada 15 segundos.

**b) Middleware customizado** (`Middlewares/MetricasRequisicoesMiddleware.cs`) — cronometra
cada requisição com `Stopwatch`, incrementa os contadores e emite log estruturado com o
tempo de resposta.

Instrumentos publicados no Meter `TarefasApi.Metricas`:

| Instrumento | Tipo | O que mede |
|-------------|------|------------|
| `tarefas_api.requisicoes.total` | Counter | Total de requisições HTTP |
| `tarefas_api.requisicoes.duracao` | Histogram | Tempo de resposta (ms) |
| `tarefas_api.requisicoes.duracao_media` | ObservableGauge | Tempo médio de resposta |
| `tarefas_api.tarefas.criadas` | Counter | Tarefas criadas com sucesso |
| `tarefas_api.tarefas.rejeitadas` | Counter | Tentativas rejeitadas por validação |

`GET /metrics` mostra o snapshot legível:

```json
{
  "totalRequisicoes": 13,
  "tempoMedioRespostaMs": 13.744,
  "tempoMinimoRespostaMs": 0.408,
  "tempoMaximoRespostaMs": 84.594,
  "totalTarefasCriadas": 3,
  "totalTarefasRejeitadas": 2,
  "requisicoesPorStatusCode": { "200": 6, "201": 3, "400": 4 },
  "requisicoesPorRota": { "POST /api/tarefas": 6, "GET /api/tarefas": 5 }
}
```

---

## ✅ Testes automatizados

**33 testes, todos passando.**

### Como rodar no Visual Studio (Test Explorer)

1. Abra `CP4.Monitoramento.sln`.
2. Menu **Testar → Gerenciador de Testes** (`Ctrl+E, T`).
3. Clique em **Executar Todos os Testes** (`Ctrl+R, A`).
4. Resultado esperado: **33 aprovados, 0 com falha**.

### Como rodar pela linha de comando

```bash
dotnet test
```

Com a lista detalhada de cada teste:

```bash
dotnet test --logger "console;verbosity=detailed"
```

### Testes Unitários — `Unitarios/TarefasServiceTests.cs` (15 testes)

Usam **Moq** com `MockBehavior.Strict` e seguem o padrão **AAA (Arrange, Act, Assert)**,
com os blocos explicitamente comentados no código.

| Teste | O que valida |
|-------|--------------|
| `CriarTarefa_QuandoDadosValidos_...` | **Criação com sucesso** + `Verify(..., Times.Once)` |
| `CriarTarefa_QuandoTituloTemEspacos_...` | Título é normalizado (`Trim`) |
| `CriarTarefa_QuandoPrioridadeNaoInformada_...` | Prioridade padrão = `Media` |
| `CriarTarefa_QuandoTituloNulo_...` | **Falha de negócio**: lança `ArgumentException` |
| `CriarTarefa_QuandoTituloVazioOuEmBranco_...` | `[Theory]` com `""`, `"   "`, `"\t"` |
| `CriarTarefa_QuandoRequisicaoNula_...` | Lança `ArgumentNullException` |
| `CriarTarefa_QuandoDataPrevistaNoPassado_...` | Regra de data lança `ArgumentException` |
| `CriarTarefa_QuandoPrioridadeInvalida_...` | Enum fora do range lança `ArgumentException` |
| `ListarTarefas_*` (4 testes) | Filtros, busca e paginação |
| `ObterPorId_QuandoNaoExiste_...` | Devolve `null` |

Em **todos** os testes de falha há `Verify(r => r.Adicionar(...), Times.Never)` — provando
que a regra de negócio barra a operação **antes** de tocar no repositório.

### Testes de Integração — `Integracao/` (18 testes)

Sobem a API inteira em memória com **`WebApplicationFactory<Program>`** e usam o
`HttpClient` de teste. Os **3 fluxos HTTP obrigatórios** estão cobertos:

| # | Fluxo exigido | Teste |
|---|---------------|-------|
| 1 | **201 Created** — POST válido | `PostTarefas_QuandoPayloadValido_DeveRetornar201Created` |
| 2 | **200 OK** — GET listagem | `GetTarefas_DeveRetornar200OkComListaJson` |
| 3 | **400 Bad Request** — POST sem título | `PostTarefas_QuandoPayloadSemTitulo_DeveRetornar400BadRequest` |

Testes adicionais: `Location` do 201 é navegável, filtros por query string, `pagina=0`
inválida, título em branco, título curto, JSON malformado (400 e **não** 500), `404` em
id inexistente, `/health`, `/health/live`, `/health/ready`, `/metrics` e o header
`X-Correlation-Id`.

---

## 🧭 Rastreabilidade — enunciado × implementação

| Critério (10 pontos) | Onde está | Status |
|----------------------|-----------|--------|
| **Web API base** (1 pt) — DI e separação Service/Repository | `Program.cs`, `Services/`, `Repositories/`, `Controllers/` | ✅ |
| **Health Checks** (1 pt) — `/health` respondendo | `Program.cs`, `Observabilidade/RepositorioTarefasHealthCheck.cs` | ✅ |
| **Logging Estruturado** (1 pt) — `ILogger` com parâmetros | `TarefasService.cs`, `MetricasRequisicoesMiddleware.cs`, `FabricaRespostaValidacao.cs` | ✅ |
| **Tracing/Métricas** (1 pt) — OTel **ou** middleware | Ambos: `Program.cs` + `MetricasRequisicoesMiddleware.cs` | ✅ |
| **Testes Unitários** (3 pts) — AAA + Moq + regra de negócio | `Unitarios/TarefasServiceTests.cs` (15 testes) | ✅ |
| **Testes de Integração** (3 pts) — `WebApplicationFactory` + 3 fluxos | `Integracao/` (18 testes) | ✅ |

---

## 🧪 Roteiro de conferência passo a passo

Siga na ordem. Cada passo prova **um item cobrado pelo enunciado**.

### Passo 0 — Abrir a solução

1. Abra `CP4.Monitoramento.sln` no Visual Studio.
2. Confirme no **Gerenciador de Soluções** que existem **2 projetos**: `TarefasApi` (em `src`) e `TarefasApi.Tests` (em `tests`).
3. Menu **Compilar → Compilar Solução** (`Ctrl+Shift+B`).
4. ✅ **Esperado:** `Compilação com êxito — 0 erros, 0 avisos`.

### Passo 1 — Testes automatizados (6 dos 10 pontos)

1. Menu **Testar → Gerenciador de Testes** (`Ctrl+E, T`).
2. **Executar Todos os Testes** (`Ctrl+R, A`).
3. ✅ **Esperado:** **33 aprovados / 0 com falha**.
4. Expanda a árvore e confira os dois grupos:
   - `TarefasApi.Tests.Unitarios` → **15 testes** (Moq + AAA)
   - `TarefasApi.Tests.Integracao` → **18 testes** (WebApplicationFactory)

Equivalente por linha de comando:

```bash
dotnet test
```

### Passo 2 — Subir a API e abrir o Swagger

1. Defina `TarefasApi` como projeto de inicialização e pressione **F5**.
2. ✅ **Esperado:** o navegador abre em <http://localhost:5275/swagger> mostrando
   `POST /api/tarefas`, `GET /api/tarefas`, `GET /api/tarefas/{id}` e `GET /metrics`.

> Dica: o arquivo `src/TarefasApi/TarefasApi.http` tem as 13 requisições prontas —
> abra-o no Visual Studio e clique em **Send Request** em cada uma.

### Passo 3 — Health Check (1 ponto)

Abra <http://localhost:5275/health>.

✅ **Esperado:** `200 OK` com `"status": "Healthy"` e as verificações
`repositorio-tarefas` e `self`.

Confira também <http://localhost:5275/health/live> → texto puro `Healthy`.

### Passo 4 — POST criando uma tarefa (201 Created)

No Swagger, `POST /api/tarefas` → **Try it out** → corpo:

```json
{ "titulo": "Estudar OpenTelemetry", "prioridade": "Alta" }
```

✅ **Esperado:** `201 Created`, corpo com o `id` gerado e header `Location`.

### Passo 5 — Logging estruturado (1 ponto)

Olhe a **janela de console da API** logo após o passo 4.

✅ **Esperado**, com os valores como parâmetros e não concatenados:

```
info: TarefasApi.Services.TarefasService[0]
      Nova tarefa criada: Estudar OpenTelemetry e87098da-... Alta 09/01/2026 01:01:34
info: TarefasApi.Middlewares.MetricasRequisicoesMiddleware[0]
      Requisição finalizada. POST /api/tarefas (sem query) 201 24.73 0e4480c1... 
```

### Passo 6 — Tratamento de erro (400 Bad Request)

No Swagger, `POST /api/tarefas` com um payload **sem título**:

```json
{ "descricao": "Esta tarefa nao tem titulo" }
```

✅ **Esperado:** `400 Bad Request` (**nunca 500**) com:

```json
{ "errors": { "Titulo": ["O campo 'titulo' é obrigatório."] } }
```

✅ **No console** aparece o log de erro:

```
warn: TarefasApi.Validacao[0]
      Requisição rejeitada na validação do modelo. POST /api/tarefas 1 Titulo O campo 'titulo' é obrigatório. 400
```

### Passo 7 — GET listando (200 OK) e os parâmetros

1. <http://localhost:5275/api/tarefas> → ✅ `200 OK` com a lista JSON.
2. Teste os parâmetros:
   <http://localhost:5275/api/tarefas?prioridade=Alta&ordenarPor=titulo&ordem=desc&pagina=1&tamanhoPagina=10>
3. ✅ **Esperado:** só as tarefas de prioridade `Alta`, ordenadas por título decrescente.
4. Parâmetro inválido: <http://localhost:5275/api/tarefas?pagina=0> → ✅ `400 Bad Request`.

### Passo 8 — Métricas e Tracing (1 ponto)

Abra <http://localhost:5275/metrics>.

✅ **Esperado:** contadores preenchidos —

```json
{
  "totalRequisicoes": 3,
  "tempoMedioRespostaMs": 40.321,
  "totalTarefasCriadas": 1,
  "totalTarefasRejeitadas": 2,
  "requisicoesPorStatusCode": { "201": 1, "400": 2 },
  "motivosDeRejeicao": { "validacao:titulo": 2 }
}
```

✅ **No console da API** o OpenTelemetry mostra os *traces* de cada requisição
(`Activity.TraceId`, `Activity.DisplayName: POST api/tarefas`) e, a cada 15 segundos,
exporta as métricas incluindo `tarefas_api.requisicoes.total`,
`tarefas_api.requisicoes.duracao` e `http.server.request.duration`.
