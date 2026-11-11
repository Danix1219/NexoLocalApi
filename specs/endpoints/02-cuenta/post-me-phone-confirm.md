# Confirmación de celular retirada · antigua `POST /v1/identity/me/phone/confirm`

Estado: retirada del diseño vigente el 2026-11-08; nunca registrada en el host actual.
Se conserva esta ficha para que los enlaces históricos resuelvan.

## 1. Decisión

La verificación se realiza por correo mediante SMTP/MailKit, según el
[registro de decisiones](../../../docs/decisions/registro.md). El móvil es contacto declarado,
capturado mediante [POST /me/phone](post-me-phone.md), implementado como contacto declarado.

## 2. Contrato vigente

No implementar ni consumir esta ruta. No hay OTP SMS, `phonePending`, ticket de confirmación,
`phoneVerifiedAt`, `badgePhoneVerified` ni evento `account.phone_verified` en el flujo vigente.
El host no registra esta operación; no se promete un `410` que no existe.

## 3. Confianza

La insignia vigente es `emailVerified`, "Correo confirmado". No acredita control del móvil,
identidad legal ni competencia profesional. El canal de llamada exige correo confirmado,
móvil declarado y consentimiento explícito.

## 4. Evidencia

Esta ficha corrige diseño; no acredita implementación de cuenta ni entrega real de correo.
No se modificaron ni borraron datos.
