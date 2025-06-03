# 🌊 WaterWise API - Sistema IoT para Prevenção de Enchentes

[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![Oracle](https://img.shields.io/badge/Oracle-Database-red.svg)](https://www.oracle.com/)
[![RabbitMQ](https://img.shields.io/badge/RabbitMQ-Messaging-orange.svg)](https://www.rabbitmq.com/)
[![ML.NET](https://img.shields.io/badge/ML.NET-Machine%20Learning-green.svg)](https://dotnet.microsoft.com/apps/machinelearning-ai/ml-dotnet)

## 📋 Descrição do Projeto

O **WaterWise** é um sistema IoT inteligente desenvolvido para a Global Solution 2025 da FIAP, com foco na prevenção de enchentes urbanas através do monitoramento de propriedades rurais. O sistema utiliza sensores IoT para coletar dados em tempo real sobre umidade do solo, temperatura e precipitação, aplicando machine learning para prever riscos de enchentes.

### 🎯 Principais Funcionalidades

- **CRUD Completo**: Gerenciamento de produtores rurais, propriedades e sensores IoT
- **API RESTful**: Implementação com HATEOAS, versionamento e rate limiting
- **Machine Learning**: Predição de riscos de enchente usando ML.NET
- **Microsserviços**: Arquitetura distribuída com RabbitMQ
- **Monitoramento IoT**: Recepção e processamento de dados de sensores em tempo real
- **Sistema de Alertas**: Notificações automáticas baseadas em condições críticas

## 🛠️ Tecnologias Utilizadas

### Backend & API
- **.NET 8.0** - Framework principal
- **ASP.NET Core Web API** - API RESTful
- **Entity Framework Core** - ORM para acesso a dados
- **Oracle Database** - Banco de dados principal

### Machine Learning & IA
- **ML.NET** - Framework de machine learning
- **Algoritmos de Classificação** - Predição de riscos de enchente

### Mensageria & Microsserviços
- **RabbitMQ** - Message broker para comunicação assíncrona
- **Microsserviços** - Arquitetura distribuída

### Qualidade & Testes
- **XUnit** - Framework de testes unitários
- **FluentAssertions** - Biblioteca para assertions mais legíveis
- **Moq** - Framework para mock objects

### Documentação & API Design
- **Swagger/OpenAPI** - Documentação interativa da API
- **HATEOAS** - Hypermedia as the Engine of Application State
- **Rate Limiting** - Controle de taxa de requisições
- **API Versioning** - Versionamento da API

### Observabilidade
- **Serilog** - Framework de logging estruturado
- **Health Checks** - Monitoramento de saúde da aplicação

## 🚀 Como Executar o Projeto

### Pré-requisitos

- **.NET 8.0 SDK** ou superior
- **Oracle Database** (local ou Docker)
- **RabbitMQ** (local ou Docker)
- **Git**

### 1. Clone o Repositório

```bash
git clone https://github.com/seu-usuario/waterwise-api.git
cd waterwise-api
```

Para enviar uma notificação de teste, faça uma requisição POST para o endereço `http://localhost:5086/test-alert`, enviando o seguinte objeto:
```json
{
	"Message": "Teste notificação",
	"Type": "manual",
	"Severity": "Alta"
}
```

Use um Fetch Client como Postman ou Insomnia (Exemplo de uso na imagem abaixo):

<img src="./imgs/POST-alert-notification.png"/>

Além do respose (preview, no Insomnia) como mostra a imagem acima, a notificação também aparece no console onde está rodando a NotificationService:

<img src="./imgs/console-alert-notification.png"/>