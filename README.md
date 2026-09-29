# OrderWebAPI - Order Web API (.NET 10 / Clean Architecture)

API REST para gestão de Pedidos (Order) desenvolvida para o Teste Técnico em **.NET 10**, seguindo os princípios de 
**Clean Architecture**, 
**DDD (Domain-Driven Design)**, 
**SOLID**, 
**EF Core com PostgreSQL**, 
**Autenticação JWT**, 
**Testes Automatizados (xUnit)** e 
**Docker Compose**.

---

## 🛠️ Tecnologia & Stack
- **Plataforma:** .NET 10 (C#)
- **Framework:** ASP.NET Core Web API Async/Await end-to-end
- **Persistência:** EF Core 10 com Npgsql PostgreSQL
- **Banco de Dados:** PostgreSQL 17 (via Docker)
- **Autenticação:** JWT Bearer Token (System.IdentityModel.Tokens.Jwt)
- **Testes:** xUnit + Moq
- **Documentação API:** Swagger UI / OpenAPI com suporte a Bearer Token
- **Conteinerização:** Docker & Docker Compose com Healthcheck e Migrations automáticas

---

## 🏛️ Arquitetura da Solução

O projeto segue a **Clean Architecture** dividida em camadas isoladas com responsabilidades bem definidas:

```text
OrderWebAPI/
├── src/
│   ├── OrderWebAPI.Domain/        # Entidades ricas (Order, OrderItem, Product), Enums, Exceções de Domínio, Interfaces
│   ├── OrderWebAPI.Application/   # DTOs, Use Cases (OrderAppService, ProductAppService), Interfaces de Serviços
│   ├── OrderWebAPI.Infrastructure/# DbContext, Repositórios, Migrations, Inicializador/Seed, Serviços JWT
│   └── OrderWebAPI.API/           # Controllers, Middleware de Exceção Global, Swagger & Configurações
├── tests/
│   └── OrderWebAPI.Tests/         # Testes de Unidade e Aplicação (xUnit + Moq)
├── docker-compose.yml                 # Orquestração do PostgreSQL 17 + Web API
└── README.md
```

---

## 🚀 Como Rodar o Projeto

### Opção 1: Via Docker Compose (Recomendado - Pronto para Produção)

Certifique-se de que o **Docker Desktop** esteja rodando na sua máquina.

1. Na raiz do projeto, execute:
   ```bash
   docker compose up --build -d
   ```
2. A API aplicará as migrations e o seed de produtos automaticamente e estará disponível em:
   - **Swagger UI:**

---

### Opção 2: Localmente via .NET CLI

1. Suba apenas o banco PostgreSQL via Docker:
   ```bash
   docker compose up postgres-db -d
   ```
2. Execute os testes unitários:
   ```bash
   dotnet test
   ```
3. Execute a Web API:
   ```bash
   dotnet run --project OrderWebAPI.API.API
   ```
4. Acesse o Swagger UI na URL exibida no console (ex: `http://localhost:5260`).

---

## 🧪 Testes Automatizados

Para rodar a suíte completa de testes de unidade e aplicação:

```bash
dotnet test
```

A suíte cobre:
- Criação de pedidos, cálculo de totais e validações de itens/estoque.
- Transições de estado (`Placed` -> `Confirmed` -> `Canceled`).
- Testes de **Idempotência** em confirmação e cancelamento repetidos.
- Testes de regras de negócio em reservas e liberação de estoque.
- Tratamento de exceções em recursos inexistentes (`EntityNotFoundException`).

---

## 🔑 Autenticação & Como Usar a API

### 1. Obter Token JWT
**Endpoint:** `POST /auth/token`
```bash
curl -X POST "http://localhost:5260/auth/token" \
  -H "Content-Type: application/json" \
  -d '{"username": "admin", "password": "admin123"}'
```
*Resposta:*
```json
{
  "token": "eyJhbGciOiJIUzI1Ni...",
  "expiresAt": "2026-09-25T19:00:00Z"
}
```

No Swagger UI, clique no botão **Authorize** no canto superior direito e insira:
`<seu_token_jwt>`

---

### 2. Criar Pedido
**Endpoint:** `POST /orders`
```bash
curl -X POST "http://localhost:5260/orders" \
  -H "Authorization: Bearer <seu_token_jwt>" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "6bdea242-f6cd-48ab-a340-ff484fe8a357",
    "currency": "BRL",
    "items": [
      {
        "productId": "11111111-1111-1111-1111-111111111111",
        "quantity": 2
      }
    ]
  }'
```
*O pedido nasce no estado `Placed`.*

---

### 3. Confirmar Pedido (Idempotente)
**Endpoint:** `POST /orders/{id}/confirm`
```bash
curl -X POST "http://localhost:5260/orders/{id}/confirm" \
  -H "Authorization: Bearer <seu_token_jwt>"
```
*Reserva o estoque dos produtos e altera o status para `Confirmed`. Se chamado mais de uma vez, mantém a confirmação sem dar baixa dupla no estoque.*

---

### 4. Cancelar Pedido (Idempotente)
**Endpoint:** `POST /orders/{id}/cancel`
```bash
curl -X POST "http://localhost:5260/orders/{id}/cancel" \
  -H "Authorization: Bearer <seu_token_jwt>"
```
*Cancela o pedido. Se o pedido estava `Confirmed`, devolve o estoque reservado para os produtos. Chamadas subsequentes mantêm o resultado sem dar devolução dupla no estoque.*

---

### 5. Consultar Pedido por ID
**Endpoint:** `GET /orders/{id}`
```bash
curl -X GET "http://localhost:5260/orders/{id}" \
  -H "Authorization: Bearer <seu_token_jwt>"
```

---

### 7. Listar Pedidos (Paginação e Filtros)
**Endpoint:** `GET /orders?customerId=&status=&from=&to=&page=1&pageSize=10`
```bash
curl -X GET "http://localhost:5260/orders?page=1&pageSize=10" \
  -H "Authorization: Bearer <seu_token_jwt>"
```

---