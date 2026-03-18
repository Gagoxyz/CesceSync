
# 🚀 CESCE Sync – Worker Service .NET

![Status](https://img.shields.io/badge/Status-Active-brightgreen)
![.NET](https://img.shields.io/badge/.NET-8.0-blueviolet)
![Platform](https://img.shields.io/badge/Platform-Windows-blue)
![API](https://img.shields.io/badge/API-CESCE-orange)
![License](https://img.shields.io/badge/License-Internal-lightgrey)

CESCE Sync es un **servicio Windows basado en .NET Worker Service** diseñado para procesar y sincronizar movimientos de riesgo provenientes de la **API oficial de CESCE**, actualizarlos en la base de datos interna y generar alertas automáticas en caso de incidencias.

Este proyecto está pensado para ejecutarse **de forma programada**, integrarse con SQL Server y funcionar sin supervisión manual.

---

# 📌 Características principales

 ✔ Autenticación OAuth2 contra CESCE  
 ✔ Obtención paginada de movimientos de riesgo  
 ✔ Normalización de los datos recibidos  
 ✔ Ejecución de SP para actualizar riesgo en SQL Server  
 ✔ Gestión de clientes no encontrados  
 ✔ Inserción de correos en cola + ejecución de EnviarCorreus.exe  
 ✔ Sistema de logs en fichero con retención automática  
 ✔ Arquitectura modular, escalable y fácil de mantener  

---

# 🏗 Arquitectura General

La aplicación se divide en los siguientes componentes:

## **1. Worker Service**
Punto de entrada del proceso. Inicia el flujo, ejecuta el procesador y detiene la aplicación al terminar.

## **2. Servicios**
- **AuthService** → Obtiene y cachea el token OAuth2  
- **CesceMovimientosService** → Llama a la API CESCE y gestiona la paginación  
- **LogFileService** → Crea y purga logs persistentes  
- **EnviarCorreusLauncher** → Ejecuta el EXE externo de envío de correos  

## **3. Procesador**
### `MovimientoProcessor`
Orquesta todo el flujo:
1. Obtener movimientos CESCE  
2. Procesarlos y enviarlos a BD  
3. Identificar los clientes no encontrados  
4. Insertar mail en cola  
5. Lanzar EnviarCorreus.exe  
6. Generar logs en caso de error o incidencias  

## **4. Repositorios**
- **ClienteRepository** → Ejecuta el SP de sincronización  
- **MailQueueRepository** → Inserta correos en la cola SQL para posteriormente ser enviados  

## **5. Modelos**
Se definen modelos para:
- MovimientoCesce  
- DTOs de CESCE  
- ClienteNoEncontrado  
- Configuraciones (CesceApiConfig, EnviarCorreusOptions, LoggingFilesOptions…)  

---

# 🔄 Flujo Completo del Proceso

### **1. Worker inicia**
Se crea el scope, se cargan las configuraciones y se inicializa el log.

### **2. Obtención del Token OAuth2**
`AuthService` solicita el token a la API CESCE utilizando credenciales configuradas.

### **3. Descarga de movimientos**
`CesceMovimientosService`:
- Construye la URL  
- Ejecuta GET con el token  
- Deserializa la respuesta  
- Gestiona paginación con `nextEndorsementNo`  
- Convierte datos (fechas, importes)  

### **4. Procesamiento**
Cada movimiento:
1. Se mapea a `MovimientoCesce`  
2. Se envía al SP `NET_SyncClientesRiesgoCESCE`  
3. Si el SP devuelve registros → se consideran *clientes no encontrados*  

### **5. Generación de alertas**
Si hay clientes no encontrados:
- Se compone el mail  
- Se inserta en registro mediante `MailQueueRepository`  
- Se ejecuta `EnviarCorreus.exe` mediante `EnviarCorreusLauncher`  

### **6. Logs**
Todo error o evento importante queda registrado en:
- Logging estándar  
- Logging persistente en fichero (con retención)  

### **7. Finalización**
El Worker detiene la aplicación (StopApplication).

---

# 🧠 Funcionamiento Técnico (resumen)

### **Tecnologías clave**
- .NET 8 Worker Service  
- HttpClient + OAuth2  
- SQL Server  
- Ejecución de procesos externos  
- Logs en fichero con retención  
- Inyección de dependencias nativa  
- Configuración mediante IOptions  

### **Patrones aplicados**
- Inversión de Dependencias (DI)  
- Repository pattern  
- DTO Mapping  
- Error Handling centralizado  
- Background Worker  

---

# ⚙️ Configuración completa del `appsettings.json`

## **1. CesceApi**
```json
"CesceApi": {
  "BaseUrl": "...",
  "TokenUrl": "...",
  "Scope": "read create update delete",
  "ClientId": "...",
  "ClientSecret": "...",
  "Username": "...",
  "Password": "...",
  "LanguageCode": "ES"
}
```

### Descripción:
- **BaseUrl / TokenUrl** → Endpoints CESCE  
- **ClientId / ClientSecret** → OAuth2  
- **Username / Password** → Credenciales  
- **Scope** → Permisos  
- **LanguageCode** → Idioma de la consulta  

---

## **2. Parametros**
```json
"Parametros": {
  "ContractNo": "12345678901"
}
```
Indica el contrato CESCE del cual se deben obtener los movimientos.

---

## **3. ConnectionStrings**
```json
"ConnectionStrings": {
  "DbConnection": "Server=...;Database=...;User Id=...;Password=...;"
}
```
Conexión a SQL Server para ejecutar procedimientos almacenados.

---

## **4. EnviarCorreus**
```json
"EnviarCorreus": {
  "ExePath": ".../EnviarCorreus.exe",
  "SqlServer": "...",
  "Database": "...",
  "UserDB": "...",
  "PasswordDB": "...",
  "TimeoutSeconds": 120,
  "Servidor": "smtp.gmail.com",
  "Puerto": 587,
  "UseSSL": true,
  "MailOrigen": "...",
  "Usuario": "...",
  "Pass": "...",
  "DefaultAsunto": "CESCE – Clientes no encontrados",
  "DefaultPara": "...",
  "DefaultQuien": 900
}
```

---

## **5. LoggingFiles**
```json
"LoggingFiles": {
  "BasePath": "C:/Logs",
  "RetentionDays": 7,
  "FilePrefix": "CesceSync"
}
```
Controla el sistema de logs en fichero:
- Carpeta  
- Días de retención  
- Prefijo  

---

# ▶️ Ejecución

1. Configurar appsettings.json  
2. Compilar el proyecto  
3. Ejecutarlo como servicio o programarlo mediante tareas  
4. Consultar logs y correos en caso de incidencias  

---

# 📄 Licencia
Proyecto interno de uso corporativo.

---

# 🙌 Autor
**David Gago**  
Desarrollador .NET & Integraciones
