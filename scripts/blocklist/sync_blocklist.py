"""Crea o actualiza la lista de groserías en Azure AI Content Safety.

Lee ContentSafety__Endpoint, ContentSafety__ApiKey y ContentSafety__Blocklist del entorno o del .env
de la API, y sube groserias-es.txt en lotes de 100 (límite del servicio). Es idempotente: volver a
correrlo solo agrega o actualiza. Uso: python scripts/blocklist/sync_blocklist.py [--check "texto"]
"""
import json
import os
import sys
import urllib.request
from pathlib import Path

API = "2024-09-01"
HERE = Path(__file__).resolve().parent


def settings():
    values = dict(os.environ)
    env = HERE.parent.parent / ".env"
    if env.exists():
        for line in env.read_text(encoding="utf-8").splitlines():
            if "=" in line and not line.lstrip().startswith("#"):
                key, value = line.split("=", 1)
                values.setdefault(key.strip(), value.strip())
    endpoint = values.get("ContentSafety__Endpoint", "").rstrip("/")
    key = values.get("ContentSafety__ApiKey", "")
    name = values.get("ContentSafety__Blocklist", "") or "nexo-groserias-es"
    if not endpoint.startswith("https://") or not key:
        sys.exit("Faltan ContentSafety__Endpoint (https) o ContentSafety__ApiKey en el entorno o el .env")
    return endpoint, key, name


def call(method, url, key, body):
    request = urllib.request.Request(url, method=method, data=json.dumps(body).encode("utf-8"),
                                     headers={"Ocp-Apim-Subscription-Key": key,
                                              # The create/update blocklist call is a JSON merge patch.
                                              "Content-Type": "application/merge-patch+json" if method == "PATCH" else "application/json"})
    with urllib.request.urlopen(request, timeout=30) as response:
        return json.loads(response.read() or b"{}")


def terms():
    seen, result = set(), []
    for raw in (HERE / "groserias-es.txt").read_text(encoding="utf-8").splitlines():
        term = raw.strip()
        if not term or term.startswith("#") or term.lower() in seen:
            continue
        assert len(term) <= 128, f"Entrada demasiado larga: {term}"
        seen.add(term.lower())
        result.append(term)
    return result


def main():
    endpoint, key, name = settings()
    base = f"{endpoint}/contentsafety/text/blocklists/{name}"
    if len(sys.argv) == 3 and sys.argv[1] == "--check":
        result = call("POST", f"{endpoint}/contentsafety/text:analyze?api-version={API}", key,
                      {"text": sys.argv[2], "blocklistNames": [name], "haltOnBlocklistHit": True})
        print("bloqueado" if result.get("blocklistsMatch") else "permitido")
        return
    call("PATCH", f"{base}?api-version={API}", key, {"description": "Groserías e insultos en español (Nexo Local)"})
    items = terms()
    for start in range(0, len(items), 100):
        batch = [{"text": t, "description": "nexo"} for t in items[start:start + 100]]
        call("POST", f"{base}:addOrUpdateBlocklistItems?api-version={API}", key, {"blocklistItems": batch})
    print(f"Lista '{name}' con {len(items)} entradas. Configura ContentSafety__Blocklist={name} en la API.")


if __name__ == "__main__":
    main()
