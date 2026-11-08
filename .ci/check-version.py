"""Comprueba el archivo VERSION y su coherencia con la rama. Sin red ni secretos."""
import json
import re
import sys

SEMVER = re.compile(
    r"(?P<nucleo>(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*))"
    r"(?:-(?P<prerelease>[0-9a-z]+(?:[.-][0-9a-z]+)*))?$"
)

RAMA_RELEASE = re.compile(r"release/(?P<version>.+)$")


def validate(texto, rama):
    """Devuelve el mensaje de exito o lanza ValueError con el motivo.

    `texto` es el contenido crudo de VERSION y `rama` la rama de trabajo, o None cuando no
    se conoce. Solo se exige coincidencia en ramas `release/*`: en una rama de trabajo la
    version todavia no esta decidida.
    """
    version = (texto or "").strip()
    if not version:
        raise ValueError("VERSION esta vacio")
    if "\n" in version:
        raise ValueError("VERSION debe tener una sola linea")

    encontrado = SEMVER.fullmatch(version)
    if encontrado is None:
        raise ValueError(f"VERSION no es SemVer: {version}")

    if rama:
        release = RAMA_RELEASE.fullmatch(rama)
        if release and release.group("version") != version:
            # El nombre de la rama es lo que lee el equipo y lo que acaba en la etiqueta;
            # si no coincide con VERSION, se despliega una version distinta de la anunciada.
            raise ValueError(
                f"La rama dice {release.group('version')} y VERSION dice {version}: deben coincidir"
            )

    return f"VERSION-OK: {version} (formato y rama; no acredita despliegue)"


def rama_del_evento(ruta):
    """Saca la rama de trabajo del evento de GitHub, sea un push o un pull request."""
    if not ruta:
        return None
    try:
        with open(ruta, encoding="utf-8") as stream:
            evento = json.load(stream)
    except (OSError, ValueError):
        return None

    pr = evento.get("pull_request")
    if isinstance(pr, dict):
        return (pr.get("head") or {}).get("ref")

    referencia = evento.get("ref") or ""
    return referencia[len("refs/heads/"):] if referencia.startswith("refs/heads/") else None


if __name__ == "__main__":
    with open("VERSION", encoding="utf-8") as stream:
        contenido = stream.read()
    try:
        print(validate(contenido, rama_del_evento(sys.argv[1] if len(sys.argv) > 1 else None)))
    except ValueError as error:
        print(f"VERSION-FAIL: {error}", file=sys.stderr)
        sys.exit(1)
