# Gates — CD-01 · Despliegue continuo a Azure

Versión: 0.1.0 | Fecha: 2026-11-05 | Spec: [spec.md](../spec.md)

## Antes de fusionar a main

- [ ] `dotnet build nexolocal.Api.slnx -c Release` sin advertencias nuevas.
- [ ] `python3 -B scripts/check_architecture.py` en verde: ninguna capa nueva cruza su frontera.
- [ ] `./scripts/Test-ScaffoldHosts.ps1` devuelve `SCAFFOLD-HOSTS-OK`.
- [ ] `/swagger/v1/swagger.json` responde 200 en local y lista las operaciones de ep-02..ep-07.
- [ ] `git grep -nE "mongodb\+srv://|AccountKey=|AZURE_CLIENT_SECRET"` sin coincidencias fuera de ejemplos con marcador.

## Antes del primer despliegue

- [ ] Environment `produccion` con revisores requeridos.
- [ ] Credencial federada creada para `ref:refs/heads/main` y para `environment:produccion`; ningún secreto de cliente en GitHub.
- [ ] Roles `Contributor` (grupo de recursos) y `AcrPush` asignados al App Registration; `AcrPull` a la identidad de la Container App.
- [ ] Secret `mongo-connection` cargado en la Container App y referenciado por `secretref:`; valor ausente del repositorio, de la imagen y de los logs.
- [ ] IP saliente del entorno autorizada en Atlas.

## Después del despliegue

- [ ] `/health/ready` responde 200 sobre el FQDN de la revisión.
- [ ] `/swagger` accesible y coincide con el contrato acordado con la app.
- [ ] Logs de la revisión sin cadenas de conexión ni credenciales.
- [ ] Reversión ensayada con `az containerapp ingress traffic set`.

Sin estas casillas marcadas con evidencia real, el despliegue no se declara Ready.

## Al habilitar los gates DevSecOps (nv-03)

- [ ] Ningún control bloqueante corre después del `docker push`: una imagen con hallazgo High no llega al registro.
- [ ] CodeQL y Trivy fallan el job, no solo anotan el PR.
- [ ] El DAST corre contra la revisión nueva y, al fallar, el tráfico vuelve a la anterior.
- [ ] Cada excepción tiene motivo y fecha de caducidad registrados; ninguna silenciada en la herramienta.
- [ ] Rulesets de `main`, `develop` y `release` aplicados, no solo propuestos.
