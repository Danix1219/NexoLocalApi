# Configuracion y secretos de base de datos

Pertenece a [nv-01](../specs/arquitectura/nv-00/spec.md) (configuracion por servicio sin secretos) y a [nv-22](../specs/arquitectura/nv-14/spec.md) (operacion de datos). No ratifica proveedor: MongoDB/Atlas y Redis siguen abiertos en D01 del [registro de decisiones](decisions/registro.md).

## Regla

Ningun secreto se versiona. `appsettings.json` solo declara el nombre de la base. La cadena de conexion llega por almacen de secretos local en desarrollo y por variable de entorno en despliegue. Cada propietario usa su propia base: S01 no abre la base de S02.

## Claves de configuracion

| Clave | Origen en desarrollo | Origen en despliegue | Versionada |
|---|---|---|---|
| `Mongo:Database` | `appsettings.json` del host | configuracion del entorno | si |
| `Mongo:ConnectionString` | `dotnet user-secrets` | variable `Mongo__ConnectionString` | no |

## Hosts con conexion propia

| Host | UserSecretsId | Base | Colecciones |
|---|---|---|---|
| S01 Identity | `nexolocal-identity` | `nexolocal_identity` | `accounts`, `sessions`, `otp_challenges`, `otp_proofs` |
| S03 Community | `nexolocal-community` | `nexolocal_community` | `posts` |

Cada host abre solo su base. Los otros siete repiten el patron cuando su historia lo encargue.

## Desarrollo local (S01 Identity)

```powershell
cd services/identity/src/Api
dotnet user-secrets init
dotnet user-secrets set "Mongo:ConnectionString" "mongodb+srv://<usuario>:<password>@<cluster>.mongodb.net/?retryWrites=true&w=majority"
```

El `UserSecretsId` del host es `nexolocal-identity`; el archivo vive fuera del repositorio, en el perfil del usuario.

## Despliegue

```
Mongo__Database=nexolocal_identity
Mongo__ConnectionString=mongodb+srv://...
```

El doble guion bajo es el separador de seccion que usa la configuracion de .NET. En Atlas, restringir por lista de acceso de red y usar un usuario por servicio con permiso solo sobre su base.

## Comprobacion

El host valida la configuracion al arrancar: si falta la cadena o no empieza por `mongodb://` o `mongodb+srv://`, el arranque falla con el motivo y sin imprimir el secreto. Con configuracion valida, `GET /health/ready` responde `Healthy` solo si el `ping` real al cluster responde. Es un endpoint de infraestructura: no figura en el [inventario de operaciones](contracts/inventario.json) y no se consume desde la app.

Para recorrer los endpoints de identidad contra un host en marcha: `python -B scripts/probe_endpoints.py <log del host>`. Lee el codigo de verificacion del registro porque en desarrollo no hay proveedor de correo.

Codigo: [MongoOptions.cs](../src/BuildingBlocks/Nexo.Common/Mongo/MongoOptions.cs) y [MongoRegistration.cs](../src/BuildingBlocks/Nexo.Common/Mongo/MongoRegistration.cs). Para S03: [CommunityMongo.cs](../src/BuildingBlocks/Nexo.Common/Mongo/MongoRegistration.cs). Hoy solo S01 y S03 tienen conexion.
