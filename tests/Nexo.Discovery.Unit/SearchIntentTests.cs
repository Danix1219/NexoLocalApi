using Nexo.Discovery.Application.Directory;
using Nexo.Discovery.Infrastructure.Search;

namespace Nexo.Discovery.Unit;

public sealed class SearchIntentTests
{
    private static readonly MlNetSearchIntentModel Model = new();
    private static readonly string[] Slugs = DirectoryCatalog.Current.Categories.Select(c => c.Slug).Where(s => s != "otros").ToArray();

    // Phrases NOT in the training set: the model must generalize, not memorize.
    [Theory]
    [InlineData("se tapó la taza del baño", "plomeria")]
    [InlineData("tengo una fuga debajo del lavabo", "plomeria")]
    [InlineData("necesito un plomero urgente", "plomeria")]
    [InlineData("no prende la luz de la recámara", "electricidad")]
    [InlineData("el contacto de la cocina echa chispas", "electricidad")]
    [InlineData("quiero que me corten el pasto", "jardineria")]
    [InlineData("perdí mis llaves y no puedo entrar", "cerrajeria")]
    [InlineData("hay goteras cuando llueve", "pintura")]
    [InlineData("necesito un closet nuevo de madera", "carpinteria")]
    public void Entiende_el_problema_sin_el_nombre_del_oficio(string text, string expected) => Assert.Equal(expected, Model.Classify(text, Slugs));

    [Theory]
    [InlineData("pizza")]
    [InlineData("hola buenas tardes")]
    public void Texto_sin_oficio_no_se_adivina(string text) => Assert.Null(Model.Classify(text, Slugs));
}
