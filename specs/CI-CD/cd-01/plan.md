# Plan — CD-01 · Despliegue continuo a Azure

Versión: 0.1.0 | Fecha: 2026-11-05 | Estado: Borrador documental | Spec: [spec.md](spec.md)

## Enfoque

Reutilizar lo que ya existe antes de crear nada: los `Dockerfile` por servicio, `backend-base.yml` como gate de build y `GET /health/ready` como sonda. El despliegue vive solo en `.github/workflows/deploy-azure.yml`; ninguna capa de los servicios cambia, así que la arquitectura (hexagonal en S01, slices/módulos en el resto) queda intacta y `check_architecture.py` sigue siendo el juez.

## Decisiones

| Decisión | Motivo | Alternativa descartada |
|---|---|---|
| Azure Container Apps | contenedor Linux, ingress con TLS, escalado y revisiones sin administrar nodos | AKS (operación desproporcionada), App Service (menos natural para varios servicios) |
| ACR por suscripción | la Container App extrae con identidad administrada; sin credenciales de registro en GitHub | GHCR (obliga a credenciales cruzadas) |
| OIDC federado | sin secreto de cliente que rote ni caduque en el repositorio | `AZURE_CREDENTIALS` con secreto |
| Alta de recursos por runbook de Azure CLI | el alta ocurre una vez; IaC sería código sin lector | Bicep/Terraform ahora |
| Etiqueta = SHA del commit | trazabilidad e idempotencia de la revisión | `latest` |
| Secrets de Container App para `Mongo__ConnectionString` | el secreto no entra al repositorio, a la imagen ni a los logs | variable de entorno en claro, `appsettings.Production.json` |

## Fases

1. **Habilitar el arranque del host sin base real** — el smoke de CI tumbaba S01 porque `ValidateOnStart` exige la cadena de Mongo. Cadena ficticia de loopback en el script de smoke.
2. **Contrato navegable** — Swashbuckle en la capa Api de S01 (`/swagger`).
3. **Pipeline de despliegue** — `deploy-azure.yml` con matriz de servicios.
4. **Runbook de Azure** — recursos, identidad federada, secrets y variables de GitHub (spec §7).
5. **Verificación** — primera revisión en verde contra `/health/ready` y `/swagger`.

## Riesgos

- Atlas rechaza la IP saliente del entorno: añadir la IP estática en Network Access antes del primer despliegue.
- Un `/health/ready` que depende de Mongo hace fallar la revisión si Atlas está caído: es intencional, pero implica que el despliegue no es independiente de la base.
- La matriz crece a ocho servicios: cada entrada necesita su Container App creada antes del primer despliegue.
