# Tareas — CD-01 · Despliegue continuo a Azure

Versión: 0.1.0 | Fecha: 2026-11-05 | Spec: [spec.md](spec.md) | Plan: [plan.md](plan.md)

| ID | Tarea | Requisito/AC | Evidencia | Estado |
|---|---|---|---|---|
| CD-T01 | Cadena Mongo ficticia de loopback en `scripts/Test-ScaffoldHosts.ps1` para que el host arranque en CI sin base ni secreto | §6 gate "Arranque real" | `SCAFFOLD-HOSTS-OK` local | Hecho |
| CD-T02 | Swashbuckle en `services/identity/src/Api` y publicación de `/swagger` | §3 contrato navegable | `/swagger/v1/swagger.json` = 200 | Hecho |
| CD-T03 | `.github/workflows/deploy-azure.yml` con matriz por servicio, OIDC, build `linux/amd64`, push a ACR y `containerapp update` | §5.2, §7.4 | workflow en el repositorio | Hecho |
| CD-T04 | Alta de recursos en Azure (RG, ACR, entorno, Container App) | §7.1 | salida de Azure CLI | Pendiente — operador |
| CD-T05 | App Registration, roles y credenciales federadas | §5.2, §7.2 | `az ad app federated-credential list` | Pendiente — operador |
| CD-T06 | Secret `mongo-connection` y `Mongo__ConnectionString=secretref:` en la Container App | §5.1 | `az containerapp secret list` (solo nombres) | Pendiente — operador |
| CD-T07 | Secrets y variables de GitHub + environment `produccion` con revisores | §5.3, §5.4, §7.3 | captura de Settings | Pendiente — operador |
| CD-T08 | Primer despliegue verificado y reversión probada | §6 | paso *Verify revision health* en verde | Pendiente |
| CD-T09 | Añadir S02–S08 a la matriz cuando tengan operaciones desplegables | §2 no-alcance | — | Diferida |
