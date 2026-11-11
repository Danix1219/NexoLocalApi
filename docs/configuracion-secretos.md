# Configuracion y secretos

## Cuenta — moderación (2026-11-09)

`ContentSafety__Endpoint=<https-endpoint-content-safety>` y
`ContentSafety__ApiKey=<credencial-content-safety>` configuran Azure AI Content Safety para cambios
de nombre. Son opcionales para arrancar; si faltan, el cambio de nombre falla con 503, sin persistir.
No se crea ningún recurso cloud ni se modifica el `.env` personal. Timeout de 8 s; respuesta
incompleta o error no autoriza contenido. La integración usa
[REST 2024-09-01](https://learn.microsoft.com/en-us/rest/api/contentsafety/text-operations/analyze-text?view=rest-contentsafety-2024-09-01),
las cuatro categorías y rechaza severidad >=2. La política es conservadora y debe evaluarse con
nombres reales antes de aceptación productiva, sin registrar texto ni credenciales.

Las fotos se resuelven en el registro `uploads` de Identity: `_id`, `ownerId`, `status: approved`,
`purpose: avatar`, `url` HTTPS. Esta iteración consume el registro, no implementa la carga/moderación
de imágenes de nv-05. No insertar aprobaciones manuales para aparentar aceptación de esa fase.

Pertenece a [nv-00](../specs/arquitectura/nv-00/spec.md) y a las [convenciones de endpoints](../specs/endpoints/00-convenciones/).

## Regla

**Todo se configura por variables de entorno. Ningun secreto se versiona.** No hay cadenas de
conexion ni llaves en `appsettings.json`: ese archivo solo lleva el nivel de registro y los hosts
permitidos. Cada propietario usa su propia base: Identity no abre la base de Discovery.

## Un solo `.env` para desarrollo

En la raiz del repositorio, copiado de [.env.example](../.env.example):

```bash
cp .env.example .env     # y rellenar los valores
```

`.env` esta en `.gitignore` y **nunca** se commitea. `.env.example` solo documenta los nombres.

.NET no lee `.env` de forma nativa, asi que el host lo carga al arrancar
([DotEnv.cs](../src/Host/Nexo.Host/DotEnv.cs)): busca el archivo desde el binario hacia arriba hasta
la raíz del sistema de archivos (puede encontrar un `.env` en la carpeta padre del clon), y **no sobrescribe** ninguna variable que ya exista en el entorno.

## En Azure

Las mismas variables se definen como **app settings** del App Service. Como el `.env` no pisa lo que
ya existe en el entorno, en Azure mandan siempre los app settings del sitio y el archivo es
irrelevante. No hace falta cambiar nada en el codigo entre un entorno y otro.

El doble guion bajo es el separador de seccion de .NET: `Mongo__ConnectionString` equivale a
`{ "Mongo": { "ConnectionString": ... } }`.

## Variables

| Variable | Para que | Secreta |
|---|---|---|
| `Mongo__ConnectionString` | Cluster de Atlas | **si** |
| `Mongo__IdentityDatabase`, `Mongo__DiscoveryDatabase`, `Mongo__EngagementDatabase` | Una base por dominio | no |
| `Mongo__MaxPoolSize` | Pool por instancia; las instancias del plan no deben agotar el cluster | no |
| `Redis__ConnectionString` | Cache L2, lista negra de `jti`, cuotas exactas, idempotencia y backplane | **si** |
| `Identity__CodeHmacKey` | Llave HMAC del codigo de verificacion del correo | **si** |
| `Identity__SigningCertificateBase64` y su `Password` | Firma de los tokens | **si** |
| `Identity__EncryptionCertificateBase64` y su `Password` | Cifrado de los tokens de refresco | **si** |
| `Mail__Host`, `Mail__Port`, `Mail__User`, `Mail__Password` | SMTP de salida; MailKit. Sin `Host` el host no arranca | **si** (`Password`) |
| `Mail__From`, `Mail__FromName` | Remitente de los codigos de verificacion | no |
| `Orders__KeyVaultKeyId` | URI HTTPS versionada de la clave RSA del Key Vault que protege las claves AES de domicilios | no |
| `Orders__AddressEncryptionKeyBase64` | Clave AES-256 solo para desarrollo y lectura heredada del formato `v1` | **si** |

En Azure configurar `Orders__KeyVaultKeyId` con el identificador completo de `orders-address-kek-dev`
(incluida la version). La API usa `DefaultAzureCredential`; en App Service debe tener identidad
administrada asignada por sistema y el rol **Key Vault Crypto User** en el vault. El formato `v2`
guarda el identificador de version usado para envolver cada clave de datos, por lo que las versiones
anteriores siguen disponibles para descifrar. Si existen domicilios `v1`, conservar en App Service
el valor anterior de `Orders__AddressEncryptionKeyBase64` solo mientras se necesite leer esos registros;
no rotarlo ni regenerarlo en ese periodo. Si no existen registros `v1`, se puede omitir esa variable.

## Certificados de los tokens

En **Development** se usan certificados de desarrollo si no hay variables. **Fuera de Development
el arranque falla a proposito** si faltan: un certificado de desarrollo en produccion permitiria a
cualquiera firmar tokens validos.

```bash
openssl req -x509 -newkey rsa:2048 -keyout k.pem -out c.pem -days 730 -nodes -subj "/CN=NexoLocal"
openssl pkcs12 -export -out nexo.pfx -inkey k.pem -in c.pem -passout pass:CLAVE
base64 -w0 nexo.pfx
```

## Comprobacion

El host valida al arrancar y dice **que** falta, sin imprimir ningun valor: cadena de Mongo ausente o
con esquema invalido, base sin nombre, llave HMAC ausente. Con la configuracion correcta,
`GET /health/ready` responde 200 solo si Mongo y Redis contestan; con una dependencia caida responde
**503**, nunca 200 por omision.

Entorno local completo: `docker compose up -d` levanta MongoDB en replica, Redis y el emulador de
colas, y [scripts/mongo-init.js](../scripts/mongo-init.js) crea las tres bases de forma idempotente.


## Correo y aislamiento de pruebas

Gmail usa SMTP/MailKit, no una API de Gmail. Configurar `Mail__Host=smtp.gmail.com`,
`Mail__Port=587`, `Mail__User=<correo-remitente>`, `Mail__Password=<credencial-smtp>` y
`Mail__From=<correo-remitente>` en el entorno o `.env` ignorado. Nunca versionar valores reales.
El adaptador usa STARTTLS en 587 y TLS directo en 465. `.env.example` conserva Mailtrap como
alternativa sandbox, que captura correos sin entregarlos a destinatarios reales.

Para probar sin cargar ningún `.env`, establecer `NEXO_DISABLE_DOTENV=1` en el proceso de
`dotnet test`: omite la carga incluso en carpetas padre. No borra variables previamente exportadas;
no heredar credenciales reales. Las suites usan bases efímeras y no acreditan entrega por Gmail.

## Imágenes reales (grupo14)
MediaStorage__ConnectionString configura Blob y Queue de la cuenta existente, nunca Flutter. Contenedores incoming/public privados, cola image-processing. MediaStorage__PublicBaseUrl es el origen estable de la API: obligatorio HTTPS en producción; en Development se permite loopback y por defecto http://127.0.0.1:5188 (adb reverse). Para emulador/LAN usar un origen HTTPS accesible; no guardar SAS temporales como foto de perfil.
ContentSafety__Endpoint/ApiKey también moderan imágenes reales. Processor de Host requiere Skia native assets para plataforma. Sin CDN/SignalR en este incremento; el worker reintenta, Flutter espera estado approved. No cambia claves ni recursos de Azure. La entrega media consulta estado y cuenta activa en cada lectura con no-store. Las subidas abandonadas caducadas se borran; aprobadas sin referencias actuales se auditan cada hora, respetando baja reversible y referencias compartidas. Cuota30/díaUTC durable Mongo; no TTL que pierda referencias antes de limpiar Blob.
