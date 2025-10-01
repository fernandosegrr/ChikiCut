# ChikiCut.web

Proyecto Razor Pages (.NET 8) para gesti�n de belleza infantil.

## Requisitos
- .NET 8 SDK
- PostgreSQL (o la base de datos configurada en `appsettings.json`)

## Instalaci�n
1. Clona el repositorio:
   ```
   git clone https://github.com/tu-usuario/tu-repo.git
   cd ChikiCut.web
   ```
2. Restaura paquetes:
   ```
   dotnet restore
   ```
3. Configura la cadena de conexi�n en `appsettings.json`.

## Ejecuci�n local
```
dotnet run --project ChikiCut.web/ChikiCut.web.csproj
```

## Despliegue en Railway
1. Sube el repositorio a GitHub.
2. En Railway, crea un nuevo proyecto y conecta tu repositorio.
3. Configura las variables de entorno necesarias (por ejemplo, la cadena de conexi�n de la base de datos).
4. Railway detectar� autom�ticamente el proyecto .NET y lo desplegar�.

## Notas
- No subas datos sensibles ni cadenas de conexi�n reales.
- Para producci�n, configura correctamente las variables de entorno en Railway.
