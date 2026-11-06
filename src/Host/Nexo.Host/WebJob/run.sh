#!/bin/sh
# WebJob continuo: mismo binario, rol worker, sin puerto HTTP.
export NEXO_ROLE=worker
exec dotnet Nexo.Host.dll
