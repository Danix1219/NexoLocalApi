# Redis: timeout de revocacion (2026-11-19)

RedisTimeoutException en StackExchange.Redis 3.4.31 deriva de TimeoutException, no de RedisException. El catch anterior no lo interceptaba y Visual Studio lo mostraba como no controlado por usuario.

Se reconoce RedisTimeoutException explicitamente. La lectura conserva su politica alternativa existente y la escritura de revocacion sigue propagando un fallo: no se informa exito falso. No se cambia la validacion de sesiones ni se aumenta arbitrariamente el timeout. BacklogPolicy.FailFast evita acumular operaciones durante desconexion, pero no elimina timeouts con servidor conectado o pausas de depurador.

Evidencia: contenedor local healthy, PONG, latencia muestreada 0-1 ms. Suite Common Unit: 42/42; cinco casos de regresion con dobles cubren timeout de lectura, resultado true/false, fallo de escritura y excepcion ajena. No acredita fallo de Redis en produccion. Recompilar y reiniciar Host para cargar el cambio. No se modificaron reglas de excepciones de Visual Studio.
