# Flujo Git del equipo

Acuerdo del usuario: `feature/* → develop → release/* → main`, aplicado igual en API y app. `main` sigue siendo la rama principal y predeterminada. Este flujo sustituye el acuerdo provisional de trabajo individual; las entradas anteriores de Memoria son históricas. API conserva autoridad de contratos y móvil registra consumo/compatibilidad.

| Rama | Se crea desde | PR de destino | Uso |
|---|---|---|---|
| `main` | existente | — | Línea estable; hoy contiene SDD, no producto desplegado. |
| `develop` | `main` inicial | `release/<version>` mediante creación de rama | Integración del trabajo del equipo. No abrir PR develop→main. |
| `feature/<CLAVE-JIRA>-<descripcion>` | `develop` actualizado | `develop` | Funcionalidad con spec y tareas. API: feature/NEXO-123-nv-16-recuperacion; app: feature/NEXO-124-017-cuenta. |
| `fix/*`, `docs/*`, `chore/*`, `refactor/*`, `test/*`, `ci/*` | `develop` | `develop` | Corrección, documentación y mantenimiento. |
| `release/<version>` | commit validado de `develop` | `main`, luego reintegración a `develop` | Candidato congelado; solo estabilización. Ejemplo futuro: release/0.1.0. |
| `fix/<version>-<descripcion>` | `release/<version>` | esa `release/<version>` | Corrección del candidato; luego se reintegra a develop. |
| `dependabot/*` | gestionada por Dependabot | `develop` | Actualizaciones de Actions revisadas, sin merge automático. |

No existe una rama permanente llamada `release`: se crea una por entrega, para separar candidatos y versiones. `release/configuracion-sdd` se usa únicamente para introducir esta configuración documental; no representa una versión de producto. No crear ramas vacías por integrante: cada integrante crea una por tarea.

## Trabajo diario

```powershell
git fetch origin
git switch develop
git pull --ff-only origin develop
git switch -c feature/NEXO-123-nv-16-recuperacion
# Editar la spec/tarea y archivos del incremento autorizado.
git add <archivos-revisados>
git commit -m "feat(cuenta): NEXO-123 recuperar acceso segun spec"
git push -u origin feature/NEXO-123-nv-16-recuperacion
# Abrir PR: feature/NEXO-123-nv-16-recuperacion -> develop.
```

En móvil usar el ID de spec local. Antes de empezar, pasar la historia a Ready conforme a su constitución; este ejemplo no autoriza implementar historias pendientes. Incluir FR/NFR, tareas, AC, contrato/contraparte y evidencia real. Actualizar la rama con merge de su base si CI exige actualización; no reescribir ramas compartidas con force-push.

## Preparar y cerrar una entrega

1. Integrar por PR las tareas aprobadas a develop y comprobar CI del commit elegido. Designar responsable humano de entrega y alcance/versiones por repositorio; documentar compatibilidad API/app.
2. Crear `release/<version>` desde ese commit de develop y publicarla. La creación está permitida sin check previo del nuevo ref; sus actualizaciones requieren PR y CI. No incorporar funcionalidades nuevas al candidato.
3. Probar el candidato. Las correcciones parten de release en `fix/*` y vuelven por PR a esa misma release. Si main ha avanzado por otra entrega, resolver en una rama fix basada en release, incorporando main con merge y verificando el resultado; no abrir main→release ni saltar gates.
4. Abrir PR `release/<version> → main`, con CI, conversaciones resueltas, evidencia, riesgos/rollback y revisión humana. Usar **Create a merge commit**, conservando ancestros; no squash ni rebase de promociones.
5. Abrir PR `main → develop` para devolver exactamente el resultado estable y correcciones; usar merge commit. También se permite release→develop para llevar correcciones antes de finalizar. No omitir esta reintegración.
6. Cuando exista producto aceptado, el responsable podrá crear tag anotado `v<version>` sobre el commit estable y tramitar CD. No se crea tag/release de producto por configurar ramas. Conservar release hasta completar promoción y reintegración; después limpiar manualmente la rama de entrega y ramas de trabajo comprobando sus PR. No borrar main/develop.

```powershell
git fetch origin
git switch -c release/0.1.0 origin/develop
git push -u origin release/0.1.0
# PR release/0.1.0 -> main; después PR main -> develop.
```

No se habilita una excepción hotfix→main: una urgencia también se estabiliza en release y cumple CI/revisión. El flujo normal no fuerza la creación de una entrega para cada PR a develop.

## Controles y revisión del equipo

- main, develop y release/*: PR, conversaciones resueltas, bloqueo de force-push y checks `SDD / docs` y `Git / flow` emitidos por GitHub Actions; rama actualizada respecto a su destino. main/develop además bloquean borrado. Sin bypass administrativo configurado.
- `Git / flow` verifica rutas de PR y exige mismo repositorio para promociones/reintegración y correcciones a release. Rechaza feature→main, develop→main, feature→release y destinos fuera del acuerdo. No verifica aceptación funcional, procedencia inicial de la rama ni autoriza despliegues. Cambiar CI/este validador requiere revisión cuidadosa.
- CI documental en push a main/develop/release y en PR (incluidos cambios de destino); sin filtros de archivos que dejen checks pendientes. Al introducir proyectos, configurar build/test/contrato/seguridad reales en ese PR: el wrapper documental rechaza manifiestos de producto sin esa transición.
- Danix1219 administra los repositorios; integrantes ordinarios: permiso Write, sin Admin por defecto. No hay usuarios ni CODEOWNERS inventados. Cada PR de equipo debe tener revisor humano distinto del autor.
- Mientras solo exista Danix1219 como colaborador, las aprobaciones técnicas obligatorias quedan en 0 para no bloquear el arranque. **Al aceptar el primer revisor con Write o superior, cambiar required_approving_review_count a 1 en los tres rulesets de ambos repositorios**, antes de integrar trabajo de equipo. Esa transición es manual y pendiente; no se afirma que una revisión independiente esté automatizada ahora. Para entregas, designar además responsable humano; no se firman specs automáticamente.
- Mantener main como default; cada nuevo PR se dirige explícitamente a develop salvo promoción/estabilización/reintegración. Dependabot se configura con target-branch develop.
- Merge commits habilitados; squash disponible solo para trabajo a develop; promociones/correcciones a release requieren merge por ruleset. Rebase merge deshabilitado. Borrado automático deshabilitado para conservar develop/release hasta reintegrar; limpieza manual tras comprobar destino.

## CI y CD futuros

La rama no es un entorno. Hoy solo hay CI documental y validación del flujo. Cuando se autorice producto/infraestructura: develop podrá validar integración, release un candidato en pruebas y main la promoción del mismo artefacto aprobado. Cuenta/región/presupuesto, roles/OIDC, firma y canal Play siguen pendientes; no se crean secretos ni despliegues ficticios. Una promoción documental no acredita DevSecOps de producto completo.

## Trazabilidad con Jira y Confluence

Aplicar [convencion y plantilla](trazabilidad-jira-confluence.md): rama por tarea con clave Jira, titulo convencional con la misma clave y campos Jira principal/Jira/Confluence obligatorios en cada PR. Los ejemplos NEXO son ilustrativos, no un proyecto confirmado. El check PR / traceability se encuentra preparado, pendiente de datos reales y activacion remota; no sustituye la revision de la pagina ni el SDD versionado. Las ramas antiguas sin clave son historicas y no la convencion para nuevo trabajo.
