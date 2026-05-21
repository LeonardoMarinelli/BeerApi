# 🍺 BeerApi

API REST para gestão de cervejarias e atacadistas belgas, desenvolvida em C# como projeto de portfólio.

---

## 📋 Proposta

Bem-vindo à Bélgica! Este sistema foi construído para gerenciar o relacionamento entre **cervejeiros**, **atacadistas** e o **estoque e venda de cervejas belgas**.

As principais funcionalidades são:

- Cervejeiros podem **cadastrar, editar e excluir** as cervejas que produzem
- Cervejeiros podem **vender** cervejas aos atacadistas (incrementando o estoque automaticamente)
- Qualquer usuário autenticado pode **solicitar um orçamento** a um atacadista, com descontos automáticos por volume
- Toda alteração no banco de dados é **registrada em um log de auditoria** (quem, quando e o quê)
- Banco de dados **pré-populado** com 7 cervejarias belgas famosas, 16 cervejas e 3 atacadistas

---

## 🛠️ Tecnologias e Boas Práticas

### Stack

| Tecnologia | Versão | Uso |
|---|---|---|
| **.NET** | 10 (LTS) | Runtime e SDK |
| **ASP.NET Core** | 10 | Framework Web / REST API |
| **Entity Framework Core** | 9.x | ORM |
| **Pomelo.EntityFrameworkCore.MySql** | 9.0.0 | Provider MySQL para EF Core |
| **MySQL** | 8.0 | Banco de dados relacional |
| **Docker / Docker Compose** | — | Container do banco de dados |
| **ASP.NET Core Identity** | 9.x | Autenticação e gerenciamento de usuários |
| **Serilog** | 8.0 | Structured logging para console e arquivo rolling |
| **DotNetEnv** | 3.x | Carrega o arquivo `.env` como variáveis de ambiente no startup |
| **Swashbuckle (Swagger UI)** | 6.9 | Documentação interativa da API |

### Arquitetura

O projeto segue **Clean Architecture** dividida em 4 camadas com dependências em uma única direção:

```
BeerApi.Domain          ← Entidades, Interfaces, Exceções de domínio
      ↑
BeerApi.Application     ← DTOs, Interfaces de serviço, Implementação da lógica de negócio
      ↑
BeerApi.Infrastructure  ← EF Core, Repositórios, Identity, Seed, Auditoria
      ↑
BeerApi.Api             ← Controllers, Middleware, Program.cs
```

### Boas Práticas Aplicadas

- **Clean Architecture**: separação clara de responsabilidades entre as camadas
- **Repository Pattern**: isolamento do acesso a dados por meio de interfaces
- **Dependency Injection**: todas as dependências resolvidas via DI do ASP.NET Core
- **Bearer Token Authentication**: via `AddIdentityApiEndpoints` (abordagem oficial do .NET 9/10) — sem JWT customizado
- **Role-based Authorization**: roles `Admin`, `Brewer` e `Wholesaler` com validação de posse (brewer só edita suas próprias cervejas)
- **Custom Claims**: `BreweryId` e `WholesalerId` injetados no token via `IUserClaimsPrincipalFactory`
- **Audit Log automático**: override de `SaveChangesAsync` no `DbContext` captura todas as operações de escrita (Create/Update/Delete) com o usuário responsável
- **Global Exception Middleware**: respostas de erro padronizadas com HTTP status correto (400, 404, 500)
- **EF Core Migrations**: schema versionado, aplicado automaticamente no startup
- **Seed via `HasData`**: dados das cervejarias, cervejas e atacadistas são parte da migration (reproduzível)
- **Transações explícitas**: registro de novo usuário (brewer/wholesaler) usa `BeginTransactionAsync` para garantir consistência
- **Records para DTOs**: imutabilidade e igualdade estrutural por padrão
- **Credenciais externalizadas**: banco e admin via `.env` + variáveis de ambiente (nada sensível em arquivos versionados)
- **Validação de entrada**: `DataAnnotations` em todos os DTOs de entrada — `[ApiController]` rejeita automaticamente com `400` antes de chegar aos serviços
- **Rate Limiting nativo**: política por IP com janela fixa (10 req/min) aplicada a todos os endpoints de autenticação via `AddRateLimiter` do ASP.NET Core
- **Limite de body**: Kestrel configurado para rejeitar bodies acima de 1 MB
- **Structured Logging com Serilog**: request logging automático por request (método, path, status, tempo), rolling file diário e níveis de log configuráveis por namespace via `appsettings.json`
- **Configuração via `.env`**: `DotNetEnv` carrega o arquivo `.env` no startup, tornando todas as variáveis disponíveis para o `IConfiguration` do ASP.NET Core

---

## 🗂️ Estrutura do Projeto

```
BeerApi/
├── docker-compose.yml              # Container MySQL 8.0
├── .env.example                    # Template de credenciais (copie para .env)
├── BeerApi_Insomnia.json           # Collection de testes para o Insomnia
├── BeerApi.sln
└── src/
    ├── BeerApi.Domain/
    │   ├── Entities/               # Brewery, Beer, Wholesaler, WholesalerBeer, Sale, AuditLog
    │   ├── Interfaces/             # IBreweryRepository, IBeerRepository, ...
    │   └── Exceptions/             # BusinessException, NotFoundException
    │
    ├── BeerApi.Application/
    │   ├── DTOs/                   # BeerDto, QuoteRequestDto, RegisterBrewerDto, ...
    │   └── Services/
    │       ├── Interfaces/         # IBeerService, IWholesalerService, ...
    │       └── (implementações)    # BeerService, WholesalerService, SaleService, ...
    │
    ├── BeerApi.Infrastructure/
    │   ├── Data/
    │   │   ├── AppDbContext.cs     # DbContext com audit log automático
    │   │   ├── Configurations/     # Fluent API + HasData (seed)
    │   │   ├── Migrations/         # Migrations geradas pelo EF Core
    │   │   └── Seed/               # DataSeeder (roles + admin)
    │   ├── Identity/               # ApplicationUser, ClaimsPrincipalFactory
    │   ├── Repositories/           # Implementações dos repositórios
    │   └── Services/               # AuthService
    │
    └── BeerApi.Api/
        ├── Controllers/            # BreweriesController, SalesController, ...
        ├── Middleware/             # ExceptionMiddleware
        ├── Program.cs
        └── appsettings.json
```

---

## 🗄️ Modelo de Dados

```
Brewery  1──*  Beer  *──*  Wholesaler   (via WholesalerBeer com campo Quantity)
                Beer  1──*  Sale
           Brewery  1──*  Sale
                    Wholesaler  1──*  Sale
ApplicationUser  *──1  Brewery    (brewer)
ApplicationUser  *──1  Wholesaler (wholesaler)
AuditLog  (log de todas as operações de escrita)
```

---

## ▶️ Como Rodar

### Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)

### 1. Clonar e configurar credenciais

```bash
git clone <url-do-repositório>
cd BeerApi

# Copie o template de credenciais e edite conforme necessário
cp .env.example .env
```

O arquivo `.env` controla as credenciais do MySQL. Os valores padrão funcionam sem alterações para desenvolvimento local.

### 2. Subir o banco de dados

```bash
docker-compose up -d
```

Aguarde o container ficar saudável (cerca de 30 segundos na primeira vez):

```bash
docker-compose ps
```

### 3. Rodar a API

```bash
cd src/BeerApi.Api
dotnet run
```

Na inicialização, a API:
1. Executa todas as migrations pendentes automaticamente
2. Cria as roles (`Admin`, `Brewer`, `Wholesaler`)
3. Cria o usuário admin padrão

**URLs:**
- HTTP: `http://localhost:5157`
- HTTPS: `https://localhost:7182`
- Swagger UI: `http://localhost:5157/swagger`

### 4. Credenciais do Admin

| Campo | Valor padrão |
|---|---|
| E-mail | `admin@beerapi.com` |
| Senha | `Admin@123!` |

Para alterar, defina as variáveis de ambiente no arquivo `.env` (recomendado) ou via `appsettings.json`:

```env
AdminUser__Email=seu@email.com
AdminUser__Password=SuaSenhaSegura!
```

> **Produção:** nunca suba credenciais reais em arquivos versionados. Use variáveis de ambiente ou um secrets manager.

---

## 🔐 Autenticação

A API usa **ASP.NET Core Identity com Bearer Tokens** (abordagem nativa do .NET 9/10 via `AddIdentityApiEndpoints`).

### Fluxo

```
POST /api/auth/register/brewer      → cria conta + cervejaria vinculada
POST /api/auth/register/wholesaler  → cria conta + atacadista vinculado
POST /api/auth/login                → retorna accessToken + refreshToken
POST /api/auth/refresh              → renova o accessToken
```

### Usar o token

Passe o token no header de todas as requisições protegidas:

```
Authorization: Bearer <accessToken>
```

### Roles e permissões

| Role | Permissões |
|---|---|
| **Admin** | Acesso total a todos os endpoints e todas as cervejarias |
| **Brewer** | Gerencia cervejas da **própria** cervejaria; registra vendas das **próprias** cervejas |
| **Wholesaler** | Leitura geral; solicita orçamentos |
| *(qualquer autenticado)* | Leitura de cervejarias, cervejas, atacadistas e estoque |

---

## 📡 Endpoints

### Autenticação

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| `POST` | `/api/auth/register/brewer` | Público | Registrar novo cervejeiro + cervejaria |
| `POST` | `/api/auth/register/wholesaler` | Público | Registrar novo atacadista |
| `POST` | `/api/auth/login` | Público | Login → retorna `accessToken` e `refreshToken` |
| `POST` | `/api/auth/refresh` | Público | Renovar token com `refreshToken` |
| `GET`  | `/api/auth/manage/info` | Auth | Ver e-mail do usuário logado |

### Cervejarias e Cervejas

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| `GET` | `/api/breweries` | Auth | Listar todas as cervejarias |
| `GET` | `/api/breweries/{id}` | Auth | Detalhes de uma cervejaria |
| `GET` | `/api/breweries/{breweryId}/beers` | Auth | Listar cervejas de uma cervejaria |
| `POST` | `/api/breweries/{breweryId}/beers` | Brewer/Admin | Criar nova cerveja |
| `PUT` | `/api/breweries/{breweryId}/beers/{beerId}` | Brewer/Admin | Atualizar cerveja |
| `DELETE` | `/api/breweries/{breweryId}/beers/{beerId}` | Brewer/Admin | Excluir cerveja |

### Vendas

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| `POST` | `/api/sales` | Brewer/Admin | Registrar venda de cerveja para um atacadista (incrementa estoque) |

### Atacadistas e Orçamentos

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| `GET` | `/api/wholesalers` | Auth | Listar todos os atacadistas |
| `GET` | `/api/wholesalers/{id}/beers` | Auth | Ver estoque de cervejas do atacadista |
| `POST` | `/api/wholesalers/{id}/quote` | Auth | Solicitar orçamento |

---

## 🧾 Regras de Negócio

### Venda (brewery → wholesaler)
- Um cervejeiro só pode vender cervejas da **própria** cervejaria
- A venda incrementa automaticamente o estoque do atacadista
- Se o atacadista ainda não vendia aquela cerveja, uma nova entrada de estoque é criada

### Orçamento

**Corpo da requisição:**
```json
POST /api/wholesalers/1/quote
{
  "items": [
    { "beerId": 1, "quantity": 5 },
    { "beerId": 4, "quantity": 8 }
  ]
}
```

**Regras de desconto:**
| Quantidade total | Desconto |
|---|---|
| ≤ 10 unidades | 0% |
| > 10 unidades | 10% |
| > 20 unidades | 20% |

**Validações (retornam HTTP 400):**
1. O pedido não pode estar vazio
2. O atacadista deve existir
3. Não pode haver cervejas duplicadas no pedido
4. A quantidade pedida não pode exceder o estoque do atacadista
5. A cerveja deve ser vendida por este atacadista

**Impostos:** atualmente 0% — o campo `TaxRate` existe na estrutura e está pronto para uso futuro.

---

## 📊 Auditoria

Toda operação que altera o banco de dados (Create, Update, Delete) é registrada automaticamente na tabela `AuditLogs` com:

| Campo | Descrição |
|---|---|
| `EntityName` | Nome da entidade afetada (ex: `Beer`, `Sale`) |
| `EntityId` | Chave primária do registro afetado |
| `Action` | `Create`, `Update` ou `Delete` |
| `OldValues` | JSON com os valores anteriores (null em criações) |
| `NewValues` | JSON com os novos valores (null em deleções) |
| `Timestamp` | Data e hora UTC da operação |
| `UserId` | ID do usuário responsável (null para operações de sistema) |
| `UserEmail` | E-mail do usuário responsável |

---

## 📋 Logs

A aplicação usa **Serilog** para logging estruturado em dois destinos simultâneos.

### Console

Uma linha por request, nível e mensagem:

```
[10:32:01 INF] HTTP POST /api/auth/login → 200 em 142.3ms
[10:32:05 INF] HTTP GET /api/breweries → 200 em 18.1ms
[10:32:07 WRN] Não foi possível encontrar Cerveja com id '99'.
[10:32:10 ERR] Ocorreu um erro inesperado.
```

### Arquivo

- **Localização:** `src/BeerApi.Api/logs/beerapi-YYYYMMDD.log`
- **Rolling diário** — um novo arquivo por dia
- **Retenção:** últimos 7 arquivos
- Formato mais detalhado com `SourceContext` e timestamp completo
- A pasta `logs/` está no `.gitignore` e não é versionada

---

## 🧪 Testando com o Insomnia

O arquivo `BeerApi_Insomnia.json` na raiz do projeto contém uma collection completa para importar no [Insomnia](https://insomnia.rest):

1. **Insomnia → File → Import** → selecione `BeerApi_Insomnia.json`
2. Faça login com o admin em **🔐 Auth → Login — Admin**
3. Copie o `accessToken` e cole na variável de ambiente `admin_token`
4. Todos os demais requests já estão configurados com `{{ admin_token }}`

A collection inclui casos de teste para todos os erros de orçamento (pedido vazio, duplicatas, estoque insuficiente, etc.).

---

## ⚙️ Variáveis de Configuração

### `.env` (credenciais do banco e do admin)

Copie `.env.example` para `.env` e ajuste os valores:

```env
MYSQL_ROOT_PASSWORD=root_secret
MYSQL_DATABASE=beerapi
MYSQL_USER=beerapi_user
MYSQL_PASSWORD=beerapi_pass
DB_PORT=3306

# Admin — sobrescreve os padrões do appsettings.json
AdminUser__Email=admin@beerapi.com
AdminUser__Password=CHANGE_ME_IN_PRODUCTION
```

O formato `AdminUser__Password` (duplo underscore) é o padrão do ASP.NET Core para mapear variáveis de ambiente a seções aninhadas do `appsettings.json`.

---

## 📦 Comandos Úteis

```bash
# Subir banco
docker-compose up -d

# Parar banco
docker-compose down

# Remover banco e dados persistidos
docker-compose down -v

# Adicionar nova migration
dotnet ef migrations add NomeDaMigration \
  --project src/BeerApi.Infrastructure \
  --startup-project src/BeerApi.Api \
  --output-dir Data/Migrations

# Reverter última migration
dotnet ef migrations remove \
  --project src/BeerApi.Infrastructure \
  --startup-project src/BeerApi.Api

# Build completo
dotnet build
```
