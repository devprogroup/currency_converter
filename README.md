# 💱 Currency Converter API


## ⚙️ Setup Instructions

1. **Clone the Repository**  
   ```bash
   git clone https://github.com/bytes4guru/currency-converter.git
   cd currency-converter
   ```

2. **Configure Environment Settings**  
   Update `appsettings.Development.json` or use environment variables:
   
3. **Restore and Build the Project**  
   ```bash
   dotnet restore
   dotnet build
   ```

4. **Run the API**  
   ```bash
   dotnet run --project src/CurrencyConverter.API
   ```

5. **Run Tests**  
   ```bash
   dotnet test
   ```
---

## 📄 Assumptions Made

- The Frankfurter API is data and error response type.

---

## 🚀 Future Improvements

- Support for multiple exchange rate providers using a provider strategy pattern.
- Docker support for containerized deployment and orchestration.
- Add Redis or distributed cache for multi-instance scaling.
- Integration with Prometheus and Grafana for metrics.
- Real-time update capability using SignalR or WebSockets.
- Include Postman collections and Swagger for testing and documentation.
- OAuth2 or IdentityServer integration for enhanced security.

---
```
