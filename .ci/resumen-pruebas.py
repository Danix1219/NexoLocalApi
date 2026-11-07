"""Convierte los `.trx` de `dotnet test` en el resumen que GitHub muestra en la ejecucion.

Sin esto, la CI corre las pruebas y solo deja un numero en el log: hay que abrir el job y leerlo.
Aqui cada clase aparece con su marca, y una prueba que falla trae su mensaje a la vista.

Se escribe en Markdown a la salida estandar; el workflow la redirige a `$GITHUB_STEP_SUMMARY`.
No usa ninguna accion de terceros: es un lector de XML y nada mas.
"""
import sys
import xml.etree.ElementTree as ET
from collections import defaultdict
from pathlib import Path

NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}

# La consola de Windows usa cp1252 por omision y revienta con las marcas. El runner es UTF-8,
# pero esto tiene que poder ejecutarse igual en la maquina de quien lo depura.
if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")


def leer(archivo):
    """Devuelve (clase, nombre, resultado, mensaje) por cada prueba del `.trx`."""
    raiz = ET.parse(archivo).getroot()

    # El resultado trae el id de la definicion, y solo la definicion trae la clase.
    clases = {}
    for definicion in raiz.findall(".//t:UnitTest", NS):
        metodo = definicion.find("t:TestMethod", NS)
        if metodo is not None:
            clases[definicion.get("id")] = metodo.get("className") or "sin clase"

    for resultado in raiz.findall(".//t:UnitTestResult", NS):
        salida = resultado.find("t:Output/t:ErrorInfo/t:Message", NS)
        yield (
            clases.get(resultado.get("testId"), "sin clase"),
            resultado.get("testName") or "sin nombre",
            resultado.get("outcome") or "Unknown",
            (salida.text or "").strip() if salida is not None else "",
        )


def main(directorio):
    archivos = sorted(Path(directorio).rglob("*.trx"))
    if not archivos:
        print("No se encontro ningun `.trx`: las pruebas no llegaron a ejecutarse.")
        return 1

    por_clase = defaultdict(lambda: {"ok": 0, "mal": 0, "saltadas": 0})
    fallos = []
    for archivo in archivos:
        for clase, nombre, resultado, mensaje in leer(archivo):
            corta = clase.rsplit(".", 1)[-1]
            if resultado == "Passed":
                por_clase[corta]["ok"] += 1
            elif resultado in ("NotExecuted", "Inconclusive"):
                por_clase[corta]["saltadas"] += 1
            else:
                por_clase[corta]["mal"] += 1
                fallos.append((nombre, mensaje))

    total_ok = sum(c["ok"] for c in por_clase.values())
    total_mal = sum(c["mal"] for c in por_clase.values())
    total_saltadas = sum(c["saltadas"] for c in por_clase.values())

    print("## Pruebas\n")
    if total_mal:
        print(f"❌ **{total_mal} con error** de {total_ok + total_mal + total_saltadas}\n")
    else:
        print(f"✅ **{total_ok} correctas** de {total_ok + total_saltadas}\n")

    print("| | Grupo | Correctas | Con error | Omitidas |")
    print("|---|---|---:|---:|---:|")
    for clase in sorted(por_clase):
        cuenta = por_clase[clase]
        marca = "❌" if cuenta["mal"] else "✅"
        print(f"| {marca} | {clase} | {cuenta['ok']} | {cuenta['mal']} | {cuenta['saltadas']} |")

    if fallos:
        print("\n### Qué falló\n")
        for nombre, mensaje in fallos:
            print(f"**{nombre}**\n")
            # El bloque evita que un mensaje con `|` o `#` rompa la tabla de arriba.
            print("```")
            print(mensaje[:1500] if mensaje else "Sin mensaje en el .trx.")
            print("```\n")

    return 1 if total_mal else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1] if len(sys.argv) > 1 else "resultados"))
