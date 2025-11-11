# Guía de Pruebas - API Gateway EventMesh

Esta guía te ayudará a probar el API Gateway que actúa como intermediario entre tu frontend React y los servicios backend en C#.

## 📋 Prerequisitos

1. **.NET 8.0 SDK** instalado
2. **Servicio de Eventos** corriendo (puerto 5001 por defecto)
3. **Keycloak** configurado (opcional, solo para endpoints autenticados)
4. **Postman, curl, o similar** para hacer requests HTTP

## 🚀 Iniciar el API Gateway

### Opción 1: Desde Visual Studio / Rider
1. Abre el proyecto `svc_yar_api-gateway.Api`
2. Presiona F5 o ejecuta el proyecto
3. El API Gateway estará disponible en `http://localhost:5000` (o el puerto configurado)

### Opción 2: Desde la línea de comandos
```bash
cd src/svc_yar_api-gateway.Api
dotnet run
```

### Opción 3: Con Docker
```bash
docker-compose up --build
```

## 🔧 Configuración

### Variables de Entorno

Puedes configurar el API Gateway usando variables de entorno o `appsettings.json`:

```bash
# Windows (PowerShell)
$env:EVENTOS_SERVICE_URL="http://localhost:5001"
$env:KEYCLOAK_AUTHORITY="http://localhost:8080/auth/realms/myrealm"
$env:CORS_ALLOWED_ORIGINS="http://localhost:3000,http://localhost:5173"

# Linux/Mac
export EVENTOS_SERVICE_URL="http://localhost:5001"
export KEYCLOAK_AUTHORITY="http://localhost:8080/auth/realms/myrealm"
export CORS_ALLOWED_ORIGINS="http://localhost:3000,http://localhost:5173"
```

O edita `appsettings.Development.json` directamente.

## 🧪 Pruebas Básicas

### 1. Health Check

Verifica que el API Gateway esté funcionando:

```bash
curl http://localhost:5000/health
```

**Respuesta esperada:**
```json
{
  "status": "Healthy"
}
```

### 2. Swagger UI

Abre en tu navegador:
```
http://localhost:5000/swagger
```

**Nota:** Swagger ahora está disponible en todos los entornos, no solo en Development.

Aquí podrás ver todos los endpoints disponibles y probarlos directamente. Puedes usar el botón "Try it out" para probar los endpoints.

### 3. Endpoint Público (Sin Autenticación)

Prueba el endpoint público que hace proxy al servicio de eventos:

```bash
curl http://localhost:5000/api/eventos/publicados
```

**Con parámetros de query:**
```bash
curl "http://localhost:5000/api/eventos/publicados?page=1&pageSize=10&categoria=musica"
```

**Respuesta esperada (con servicio real):**
```json
{
  "data": [
    {
      "id": "123",
      "nombre": "Concierto de Rock",
      "categoria": "musica",
      "fecha": "2025-03-15T20:00:00Z",
      "precio": 50.00
    }
  ],
  "page": 1,
  "pageSize": 10,
  "total": 1,
  "totalPages": 1
}
```

**Respuesta con datos mock (si el servicio no está disponible):**
El API Gateway automáticamente retornará datos mock cuando el servicio de eventos no esté disponible. La respuesta incluirá un campo `"_mock": true` para indicar que son datos de prueba.

```json
{
  "data": [
    {
      "id": "1",
      "nombre": "Concierto de Rock",
      "categoria": "musica",
      "fecha": "2025-03-15T20:00:00Z",
      "precio": 50.00,
      "aforo": 5000,
      "aforoDisponible": 3500
    }
  ],
  "page": 1,
  "pageSize": 10,
  "total": 5,
  "totalPages": 1,
  "_mock": true
}
```

**Nota importante:** Si no tienes el servicio de eventos implementado, el API Gateway automáticamente usará datos mock, así que puedes probarlo sin necesidad de tener los microservicios corriendo.

### 4. Endpoint Autenticado

Para probar endpoints que requieren autenticación, necesitas un token JWT de Keycloak:

```bash
# Primero obtén un token de Keycloak
TOKEN=$(curl -X POST "http://localhost:8080/auth/realms/myrealm/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "client_id=eventmesh-api" \
  -d "username=usuario" \
  -d "password=password" \
  -d "grant_type=password" | jq -r '.access_token')

# Luego usa el token para hacer la request
curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:5000/api/eventos/123
```

## 🧪 Pruebas desde el Frontend React

### Configuración en React

En tu aplicación React, configura la URL base del API Gateway:

```typescript
// src/config/api.ts
const API_BASE_URL = process.env.REACT_APP_API_URL || 'http://localhost:5000';

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Para requests autenticados, agrega el interceptor:
apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('access_token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});
```

### Ejemplo de uso en un componente React

```typescript
import { useEffect, useState } from 'react';
import { apiClient } from '../config/api';

function EventosList() {
  const [eventos, setEventos] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchEventos = async () => {
      try {
        const response = await apiClient.get('/api/eventos/publicados', {
          params: {
            page: 1,
            pageSize: 20,
            categoria: 'musica'
          }
        });
        setEventos(response.data);
      } catch (error) {
        console.error('Error fetching eventos:', error);
      } finally {
        setLoading(false);
      }
    };

    fetchEventos();
  }, []);

  if (loading) return <div>Cargando...</div>;

  return (
    <div>
      {eventos.map(evento => (
        <div key={evento.id}>
          <h3>{evento.titulo}</h3>
          <p>{evento.categoria}</p>
        </div>
      ))}
    </div>
  );
}
```

## 🔍 Verificación de Funcionalidades

### 1. CORS

Verifica que CORS esté funcionando correctamente. Desde la consola del navegador en tu app React:

```javascript
fetch('http://localhost:5000/api/eventos/publicados')
  .then(res => res.json())
  .then(data => console.log(data))
  .catch(err => console.error('CORS Error:', err));
```

Si CORS está bien configurado, deberías ver los datos. Si hay un error de CORS, verifica la configuración en `Program.cs`.

### 2. Correlation ID

El API Gateway genera automáticamente un `X-Correlation-ID` en cada request. Verifica que se esté propagando:

```bash
curl -v http://localhost:5000/api/eventos/publicados
```

Busca en los headers de respuesta:
```
< X-Correlation-ID: 12345678-1234-1234-1234-123456789abc
```

### 3. Manejo de Errores

Prueba qué pasa cuando el servicio backend no está disponible:

```bash
# Detén el servicio de eventos y luego:
curl http://localhost:5000/api/eventos/publicados
```

Deberías recibir un error 502 (Bad Gateway) con un mensaje descriptivo.

### 4. Timeout

Si el servicio backend tarda mucho, el API Gateway debería retornar un 504 (Gateway Timeout).

## 🐛 Troubleshooting

### El API Gateway no inicia

1. Verifica que el puerto 5000 esté libre:
   ```bash
   netstat -ano | findstr :5000  # Windows
   lsof -i :5000                 # Linux/Mac
   ```

2. Verifica que todas las dependencias estén instaladas:
   ```bash
   dotnet restore
   ```

### Error 502 Bad Gateway

- Verifica que el servicio de eventos esté corriendo en el puerto configurado
- Revisa la variable `EVENTOS_SERVICE_URL` en `appsettings.json`
- Verifica los logs del API Gateway para más detalles

### Error de CORS desde React

- Verifica que la URL de tu frontend esté en `CORS_ALLOWED_ORIGINS`
- Asegúrate de que el API Gateway esté usando la política CORS correcta
- Revisa la consola del navegador para el error específico

### Error de Autenticación

- Verifica que Keycloak esté corriendo y accesible
- Revisa la configuración de `KEYCLOAK_AUTHORITY` y `KEYCLOAK_AUDIENCE`
- Asegúrate de que el token JWT sea válido y no haya expirado

## 📊 Monitoreo

### Logs

Los logs del API Gateway incluyen:
- Requests entrantes con Correlation ID
- Errores de comunicación con servicios backend
- Timeouts y errores de red

Revisa los logs en la consola donde ejecutaste `dotnet run` o en los logs de Docker.

### Health Checks

El endpoint `/health` te permite verificar el estado del API Gateway. Puedes configurar monitoreo externo para hacer polling a este endpoint.

## 🎯 Próximos Pasos

1. **Agregar más endpoints proxy** según necesites
2. **Implementar rate limiting** para proteger los servicios backend
3. **Agregar circuit breaker** para manejar fallos de servicios
4. **Implementar caching** para mejorar el rendimiento
5. **Agregar métricas y observabilidad** (Prometheus, OpenTelemetry)

## 📚 Recursos Adicionales

- [Documentación de ASP.NET Core](https://docs.microsoft.com/aspnet/core)
- [Swagger/OpenAPI](https://swagger.io/docs/)
- [Keycloak Documentation](https://www.keycloak.org/documentation)

