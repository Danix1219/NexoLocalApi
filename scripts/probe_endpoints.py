# -*- coding: utf-8 -*-
"""Comprueba los endpoints de identidad (ep-02 a ep-07) contra un host en marcha.

Uso: levantar NexoLocal.Identity.Api en Development y ejecutar
    python -B scripts/probe_endpoints.py <ruta del log del host>

Lee el codigo de verificacion del registro porque en Development no hay proveedor de
correo (D04 sigue abierta). Crea una cuenta desechable prueba.ep.<marca>@nexolocal.test:
borrarla despues de usar un entorno que no sea de desarrollo.
No es aceptacion de producto: no sustituye las suites de contrato ni los gates de nv-03.
"""
import json
import re
import sys
import time
import urllib.error
import urllib.request

# Host de S01 segun launchSettings; se puede sobreescribir con el segundo argumento.
BASE = (sys.argv[2] if len(sys.argv) > 2 else "http://127.0.0.1:5188") + "/api/v1"
LOG = sys.argv[1]
EMAIL = f"prueba.ep.{int(time.time())}@nexolocal.test"
PASSWORD = "Clave-Segura-9"
NEW_PASSWORD = "Otra-Clave-Segura-9"

failures = []
checks = 0


def call(method, path, body=None, token=None):
    data = json.dumps(body).encode() if body is not None else None
    request = urllib.request.Request(BASE + path, data=data, method=method)
    if data is not None:
        request.add_header("Content-Type", "application/json")
    if token:
        request.add_header("Authorization", "Bearer " + token)
    try:
        with urllib.request.urlopen(request, timeout=30) as response:
            raw = response.read()
            return response.status, (json.loads(raw) if raw else None)
    except urllib.error.HTTPError as error:
        raw = error.read()
        try:
            return error.code, json.loads(raw) if raw else None
        except json.JSONDecodeError:
            return error.code, {"raw": raw.decode("utf-8", "replace")}


def expect(label, actual, wanted):
    global checks
    checks += 1
    ok = actual == wanted
    if not ok:
        failures.append(f"{label}: esperaba {wanted}, obtuvo {actual}")
    print(f"  [{'ok ' if ok else 'FALLA'}] {label}: {actual}")


def code_for(email):
    """Lee el codigo del registro de desarrollo; en produccion lo enviaria el proveedor."""
    with open(LOG, encoding="utf-8", errors="replace") as handle:
        text = handle.read()
    found = re.findall(re.escape(email) + r": (\d{6})", text)
    return found[-1] if found else None


def verified_proof(purpose):
    status, body = call("POST", "/auth/otp", {"email": EMAIL, "purpose": purpose})
    expect(f"EP-001 solicitar codigo ({purpose})", status, 202)
    challenge = body["challengeId"]
    time.sleep(1.5)
    code = code_for(EMAIL)
    if not code:
        failures.append("no se encontro el codigo en el registro")
        return None
    status, body = call("POST", "/auth/otp/verify", {"challengeId": challenge, "code": code})
    expect(f"EP-002 verificar codigo ({purpose})", status, 200)
    return body["proofToken"] if status == 200 else None


print(f"correo de prueba: {EMAIL}\n")

print("ep-03 / EP-001 y EP-002 — verificacion de correo")
expect("correo invalido rechazado", call("POST", "/auth/otp", {"email": "sin-arroba", "purpose": "registration"})[0], 400)
expect("purpose invalido rechazado", call("POST", "/auth/otp", {"email": EMAIL, "purpose": "otro"})[0], 400)
expect("cuerpo JSON invalido da 400", call("POST", "/auth/otp", "no-es-objeto")[0], 400)

status, first = call("POST", "/auth/otp", {"email": EMAIL, "purpose": "registration"})
expect("solicitar codigo", status, 202)
status, again = call("POST", "/auth/otp", {"email": EMAIL, "purpose": "registration"})
expect("reenvio inmediato reutiliza el desafio", again["challengeId"], first["challengeId"])
expect("codigo incorrecto rechazado", call("POST", "/auth/otp/verify", {"challengeId": first["challengeId"], "code": "000000"})[0], 400)

time.sleep(1.5)
code = code_for(EMAIL)
status, proof = call("POST", "/auth/otp/verify", {"challengeId": first["challengeId"], "code": code})
expect("codigo correcto emite prueba", status, 200)
expect("el mismo desafio no se verifica dos veces",
       call("POST", "/auth/otp/verify", {"challengeId": first["challengeId"], "code": code})[0], 400)
proof_token = proof["proofToken"]

print("\nep-02 / EP-003 — registro")
expect("contrasena corta rechazada",
       call("POST", "/auth/register", {"proofToken": proof_token, "password": "corta", "displayName": "Johann",
                                       "consentVersion": "2026-11", "deviceId": "dev-1"})[0], 400)
status, registered = call("POST", "/auth/register", {"proofToken": proof_token, "password": PASSWORD,
                                                     "displayName": "Johann Prueba", "consentVersion": "2026-11",
                                                     "deviceId": "dev-1"})
expect("registro crea cuenta y sesion", status, 201)
expect("la prueba no se reutiliza",
       call("POST", "/auth/register", {"proofToken": proof_token, "password": PASSWORD, "displayName": "Duplicado",
                                       "consentVersion": "2026-11", "deviceId": "dev-1"})[0], 401)
account_id = registered["accountId"]

print("\nep-04 / EP-004 — inicio de sesion")
expect("contrasena incorrecta da 401",
       call("POST", "/auth/login", {"email": EMAIL, "password": "equivocada", "deviceId": "dev-1"})[0], 401)
expect("correo inexistente da el mismo 401",
       call("POST", "/auth/login", {"email": "nadie@nexolocal.test", "password": PASSWORD, "deviceId": "dev-1"})[0], 401)
status, session = call("POST", "/auth/login", {"email": EMAIL, "password": PASSWORD, "deviceId": "dev-1"})
expect("login correcto", status, 200)

print("\nep-06 / EP-008 — cuenta propia")
expect("sin token da 401", call("GET", "/me")[0], 401)
expect("token invalido da 401", call("GET", "/me", token="inventado")[0], 401)
status, me = call("GET", "/me", token=session["accessToken"])
expect("/me con token valido", status, 200)
expect("/me devuelve la cuenta del titular", me["accountId"], account_id)
expect("/me no expone hashes", [k for k in me if "hash" in k.lower() or "password" in k.lower()], [])

print("\nep-05 / EP-005 y EP-007 — continuidad y cierre")
status, rotated = call("POST", "/auth/refresh", {"refreshToken": session["refreshToken"], "deviceId": "dev-1"})
expect("refresh rota la sesion", status, 200)
expect("el refresh anterior ya no sirve",
       call("POST", "/auth/refresh", {"refreshToken": session["refreshToken"], "deviceId": "dev-1"})[0], 401)
expect("el replay revoco la familia: el nuevo refresh tampoco sirve",
       call("POST", "/auth/refresh", {"refreshToken": rotated["refreshToken"], "deviceId": "dev-1"})[0], 401)

status, session = call("POST", "/auth/login", {"email": EMAIL, "password": PASSWORD, "deviceId": "dev-2"})
expect("sesion nueva tras la revocacion", status, 200)
expect("logout", call("POST", "/auth/logout", token=session["accessToken"])[0], 204)
expect("el token revocado ya no autentica", call("POST", "/auth/logout", token=session["accessToken"])[0], 401)
expect("tras el logout el refresh no sirve",
       call("POST", "/auth/refresh", {"refreshToken": session["refreshToken"], "deviceId": "dev-2"})[0], 401)

print("\nep-07 / EP-006 — recuperacion de contrasena")
expect("prueba de registro no sirve para recuperar",
       call("POST", "/auth/password-reset", {"proofToken": verified_proof("registration"), "newPassword": NEW_PASSWORD})[0], 401)
recovery = verified_proof("recovery")
expect("recuperacion con prueba de recovery",
       call("POST", "/auth/password-reset", {"proofToken": recovery, "newPassword": NEW_PASSWORD})[0], 204)
expect("correo desconocido responde igual",
       call("POST", "/auth/password-reset", {"proofToken": recovery, "newPassword": NEW_PASSWORD})[0], 401)
expect("la contrasena anterior ya no entra",
       call("POST", "/auth/login", {"email": EMAIL, "password": PASSWORD, "deviceId": "dev-3"})[0], 401)
expect("la contrasena nueva entra",
       call("POST", "/auth/login", {"email": EMAIL, "password": NEW_PASSWORD, "deviceId": "dev-3"})[0], 200)

print(f"\n{checks - len(failures)}/{checks} comprobaciones correctas")
for failure in failures:
    print("FALLA: " + failure)
print("CUENTA_DE_PRUEBA=" + EMAIL)
sys.exit(1 if failures else 0)
