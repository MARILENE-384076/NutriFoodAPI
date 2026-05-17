# NutriFoodAPI — Módulo de Validação Nutricional

Esta Web API foi desenvolvida em **ASP.NET Core** com o objetivo de centralizar a ingestão, validação de regras de negócio e a persistência de dados nutricionais coletados de APIs externas. O microsserviço atua de forma integrada com outras frentes de desenvolvimento (Squads), assegurando a qualidade e a rastreabilidade das informações antes de consolidadas no banco de dados Cloud Firestore.

---

## 🛠️ Stack Técnica e Arquitetura

* **Runtime:** .NET Core 6.0 / 7.0 / 8.0 (conforme configuração da aplicação)
* **Linguagem:** C# (Programação Assíncrona via `async/await`)
* **Persistência de Dados:** Google Cloud Firestore (NoSQL)
* **Monitorização:** Camada nativa de Log (`ILogger`) para rastreabilidade estruturada.
* **Documentação e Testes:** Swagger UI (OpenAPI v3).

---

## 🚀 Instruções para Execução e Configuração

### 1. Pré-requisitos
Antes de iniciar, certifique-se de que possui as seguintes ferramentas instaladas na sua máquina:
* [.NET SDK](https://dotnet.microsoft.com/download) (Versão correspondente ao projeto)
* [Git](https://git-scm.com)

### 2. Configuração de Credenciais do Google Firebase
Como o projeto utiliza o **Google Cloud Firestore**, é obrigatório fornecer o ficheiro de chaves do setor de IAM para autenticação:
1. Faça o download do ficheiro JSON de credenciais (`service-account.json`) a partir do Console do Firebase.
2. Adicione o ficheiro na raiz do projeto ou configure uma variável de ambiente no seu sistema operativo:
   ```bash
   # Windows (PowerShell)
   $env:GOOGLE_APPLICATION_CREDENTIALS="C:\caminho\para\seu\service-account.json"

   # Linux/macOS
   export GOOGLE_APPLICATION_CREDENTIALS="/caminho/para/seu/service-account.json"
### 3. Executar a Aplicação Localmente

Navegue até a pasta raiz do projeto onde se encontra o ficheiro `.csproj` através do terminal e execute os comandos:

```bash
# 1. Restaurar as dependências do NuGet
dotnet restore

# 2. Compilar a aplicação
dotnet build

# 3. Executar a API em modo de Desenvolvimento
dotnet run
```
O terminal exibirá os endereços locais em que a API está à escuta. 

**Exemplos:**
* `https://localhost:7001`
* `http://localhost:5001`

> 💡 **Nota:** Certifique-se de usar o protocolo correto (`http` ou `https`) ao realizar as requisições para a API.

---
   
