# Versionamiento

La versión viva está en **`VERSION`**, en la raíz del repositorio. Una sola línea, SemVer, sin
prefijo `v`:

```
0.2.0
```

Ese archivo es la única fuente. La rama de entrega, la etiqueta de Git y la versión que publica la
API salen todas de ahí, de modo que no pueden contradecirse.

## Matriz

| Parte | Cuándo sube | Ejemplo en este proyecto |
|---|---|---|
| **Mayor** `X` | Rompe el contrato con la app: se quita o cambia una operación, un campo deja de devolverse, cambia el significado de un código de error | `POST /v1/identity/accounts` dejó de devolver tokens. Con la app ya publicada en la tienda, eso sería un cambio mayor |
| **Menor** `Y` | Se añade capacidad sin romper lo anterior: operaciones nuevas, campos opcionales nuevos | Implementar `specs/endpoints/02-cuenta` |
| **Parche** `Z` | Corrección sin cambio de contrato: un fallo, una mejora interna, cualquier cosa de CI o infraestructura | Los tres arreglos del despliegue de noviembre |

Antes del `1.0.0` la regla formal de SemVer permite romper en la menor. Aquí no se usa esa
licencia: la app móvil consume esta API y un cambio incompatible sube la mayor igual.

### Candidatos de entrega

Un candidato lleva sufijo `-rc.N`, en minúsculas:

```
0.3.0-rc.1
```

**Si el contenido del candidato cambia, sube `N`.** Un `rc` que se corrige y se vuelve a promover
deja de ser el mismo candidato, y reutilizar el número hace imposible saber qué se evaluó.

## Correspondencia

| | Valor |
|---|---|
| Archivo | `VERSION` → `0.3.0` |
| Rama | `release/0.3.0-rc.1` |
| Etiqueta | `api-v0.3.0`, creada por el despliegue |
| OpenAPI | `info.version` del documento |
| Ensamblado | `InformationalVersion` = `0.3.0+<sha corto>` |

`VERSION` lleva la **versión que se entrega**; el sufijo `-rc.N` vive solo en el nombre de la
rama, porque cuenta las vueltas de la estabilización y no forma parte de lo que se publica. Lo
que **tiene que coincidir** es el núcleo `X.Y.Z` de los dos. Lo comprueba
[`.ci/check-version.py`](../.ci/check-version.py) en la etapa `2 · Compilación y estilo` de la
canalización, y solo lo exige en ramas `release/*`: en una rama de trabajo la versión de entrega
todavía no está decidida.

## Qué pasa en cada despliegue

1. `dotnet publish` sella el ensamblado con la versión y el SHA corto del commit.
2. La API publica esa misma cadena en `info.version` de `/openapi/v1.json`, así que se puede
   comprobar **qué versión está viva** sin entrar al portal ni a los registros.
3. Al terminar, el despliegue crea la etiqueta `api-vX.Y.Z` si no existe, y escribe versión,
   etiqueta y URL en el resumen de la ejecución.

El SHA va en el `InformationalVersion` y no en la versión del paquete a propósito: dos despliegues
de la misma versión desde commits distintos se distinguen, y el número que lee una persona sigue
siendo limpio.

## Cómo subir de versión

1. En una rama de trabajo, editar `VERSION`.
2. Anotar el cambio en `Memoria.md` con su motivo.
3. Integrar a `develop`.
4. Crear `release/X.Y.Z-rc.N` desde `develop`, con el mismo `X.Y.Z` que `VERSION`.
5. Promover a `main`. El despliegue pone la etiqueta.
6. Reintegrar `main` a `develop`.

Si durante la estabilización entra un `fix/*` en la rama de release, **sube el `rc`** creando
`release/X.Y.Z-rc.(N+1)`. `VERSION` no cambia: la versión que se entrega es la misma, lo que
cambia es cuántas vueltas costó llegar a ella.
