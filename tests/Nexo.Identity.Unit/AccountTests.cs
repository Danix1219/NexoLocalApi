using Nexo.Identity.Domain.Accounts;

namespace Nexo.Identity.Unit;

/// <summary>nv-00 AC-008: el dominio Identity conserva su comportamiento tras el traslado a src/Domains.</summary>
[Trait("Category", "Unit")]
public sealed class AccountTests
{
    [Theory]
    [InlineData("  Ana@Ejemplo.MX ", "ana@ejemplo.mx")]
    [InlineData("YA@minusculas.com", "ya@minusculas.com")]
    public void NormalizeEmail_quita_espacios_y_baja_a_minusculas(string entrada, string esperado) =>
        Assert.Equal(esperado, Account.NormalizeEmail(entrada));

    [Fact]
    public void NormalizeEmail_hace_colisionar_las_variantes_de_un_mismo_correo()
    {
        // La llave unica de cuentas depende de esto: con mayusculas se crearian dos cuentas.
        Assert.Equal(Account.NormalizeEmail("Ana@Ejemplo.mx"), Account.NormalizeEmail("ana@EJEMPLO.MX"));
    }

    [Theory]
    [InlineData("ana@ejemplo.mx")]
    [InlineData("a.b+c@sub.dominio.com")]
    public void IsEmailShaped_acepta_correos_bien_formados(string email) =>
        Assert.True(Account.IsEmailShaped(email));

    [Theory]
    [InlineData(null)]            // nulo
    [InlineData("")]              // vacio
    [InlineData("   ")]           // solo espacios
    [InlineData("sinarroba.com")] // sin @
    [InlineData("@ejemplo.mx")]   // sin parte local
    [InlineData("ana@")]          // sin dominio
    [InlineData("ana@@ejemplo.mx")] // dos @
    [InlineData("ana@ejemplo")]   // dominio sin punto
    [InlineData("ana@.ejemplo.mx")] // dominio empieza en punto
    [InlineData("ana@ejemplo.mx.")] // dominio termina en punto
    [InlineData("an a@ejemplo.mx")] // espacio interior
    public void IsEmailShaped_rechaza_correos_mal_formados(string? email) =>
        Assert.False(Account.IsEmailShaped(email));

    [Fact]
    public void IsEmailShaped_rechaza_al_pasar_de_254_caracteres()
    {
        // Limite de RFC 5321 y de la columna: 254 pasa, 255 no.
        var dominio = "@ejemplo.mx";
        var justo = new string('a', 254 - dominio.Length) + dominio;
        var pasado = new string('a', 255 - dominio.Length) + dominio;

        Assert.Equal(254, justo.Length);
        Assert.True(Account.IsEmailShaped(justo));
        Assert.False(Account.IsEmailShaped(pasado));
    }
}
