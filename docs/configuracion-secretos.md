# Configuracion y secretos

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
la raiz, y **no sobrescribe** ninguna variable que ya exista en el entorno.

## En Azure

Las mismas variables se definen como **app settings** del App Service. Como el `.env` no pisa lo que
ya existe en el entorno, en Azure mandan siempre los app settings del sitio y el archivo es
irrelevante. No hace falta cambiar nada en el codigo entre un entorno y otro.

El doble guion bajo es el separador de seccion de .NET: `Mongo__ConnectionString` equivale a
`{ "Mongo": { "ConnectionString": ... } }`.

## Variables

| Variable | Para que | Secreta |
|---|---|---|
| `NEXO_ROLE` | `api` o `worker`; el mismo binario sirve para los dos | no |
| `Mongo__ConnectionString` | Cluster de Atlas | **si** |
| `Mongo__IdentityDatabase`, `Mongo__DiscoveryDatabase`, `Mongo__EngagementDatabase` | Una base por dominio | no |
| `Mongo__MaxPoolSize` | Pool por instancia; dos instancias mas el worker no deben agotar el cluster | no |
| `Redis__ConnectionString` | Cache L2, lista negra de `jti`, cuotas exactas, idempotencia y backplane | **si** |
| `Identity__CodeHmacKey` | Llave HMAC del codigo de verificacion del correo | **si** |
| `Identity__SigningCertificateBase64` y su `Password` | Firma de los tokens | **si** |
| `Identity__EncryptionCertificateBase64` y su `Password` | Cifrado de los tokens de refresco | **si** |
| `Mail__*` | SMTP; pendiente, MailKit aun no envia | **si** |

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
