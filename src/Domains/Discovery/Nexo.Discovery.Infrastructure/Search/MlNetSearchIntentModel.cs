using Microsoft.ML;
using Microsoft.ML.Data;
using Nexo.Discovery.Application.Directory;

namespace Nexo.Discovery.Infrastructure.Search;

/// <summary>
/// ML.NET text classifier (n-gram features + SDCA maximum entropy) trained once in memory from <see cref="SearchIntentTraining"/>.
/// Answers only above <see cref="MinConfidence"/>; "otros" means "not a trade we know" and is never returned.
/// </summary>
public sealed class MlNetSearchIntentModel : ISearchIntentModel
{
    public const float MinConfidence = 0.45f;
    private readonly Lazy<(PredictionEngine<Phrase, Prediction> Engine, string[] Labels)> model = new(Train);
    private readonly Lock gate = new();

    public string Source => "ml";

    public Task<string?> ClassifyAsync(string text, IReadOnlyList<string> slugs, CancellationToken ct) => Task.FromResult(Classify(text, slugs));

    public string? Classify(string text, IReadOnlyList<string> slugs)
    {
        var (engine, labels) = model.Value;
        Prediction p;
        lock (gate) p = engine.Predict(new Phrase { Text = DirectoryCatalog.Normalize(text) }); // PredictionEngine is not thread-safe.
        var best = Array.IndexOf(p.Score, p.Score.Max());
        var slug = labels[best];
        return p.Score[best] >= MinConfidence && slug != "otros" && slugs.Contains(slug) ? slug : null;
    }

    private static (PredictionEngine<Phrase, Prediction>, string[]) Train()
    {
        var ml = new MLContext(seed: 7);
        var data = ml.Data.LoadFromEnumerable(SearchIntentTraining.Examples
            .Select(e => new Phrase { Text = DirectoryCatalog.Normalize(e.Text), Label = e.Category }));
        var pipeline = ml.Transforms.Conversion.MapValueToKey("Label")
            .Append(ml.Transforms.Text.FeaturizeText("Features", new Microsoft.ML.Transforms.Text.TextFeaturizingEstimator.Options
            {
                WordFeatureExtractor = new() { NgramLength = 2, UseAllLengths = true },
                CharFeatureExtractor = new() { NgramLength = 4, UseAllLengths = false } // char 4-grams catch "plomero"/"plomería", typos
            }, nameof(Phrase.Text)))
            .Append(ml.MulticlassClassification.Trainers.SdcaMaximumEntropy())
            .Append(ml.Transforms.Conversion.MapKeyToValue("PredictedLabel"));
        var trained = pipeline.Fit(data);
        var engine = ml.Model.CreatePredictionEngine<Phrase, Prediction>(trained);
        // Score[i] follows the key order of the label column.
        VBuffer<ReadOnlyMemory<char>> keys = default;
        engine.OutputSchema["Score"].GetSlotNames(ref keys);
        return (engine, keys.DenseValues().Select(k => k.ToString()).ToArray());
    }

    public sealed class Phrase
    {
        public string Text { get; set; } = "";
        public string Label { get; set; } = "";
    }

    public sealed class Prediction
    {
        public string PredictedLabel { get; set; } = "";
        public float[] Score { get; set; } = [];
    }
}

/// <summary>Training phrases: how people describe the problem, not the trade's name. Add real zero-result searches here.</summary>
public static class SearchIntentTraining
{
    public static readonly (string Text, string Category)[] Examples = Build(new()
    {
        ["plomeria"] = [
            "plomero", "fontanero", "se me tapó el baño", "el excusado no baja", "el wc está tapado", "gotea la llave del lavabo",
            "fuga de agua", "hay una fuga en la cocina", "se rompió un tubo", "tubería rota", "destapar el drenaje", "la coladera huele mal",
            "no sale agua caliente", "instalar boiler", "el calentador no prende", "limpiar el tinaco", "la cisterna no se llena",
            "cambiar la mezcladora", "la regadera gotea", "el fregadero está tapado", "instalar un baño", "no sube el agua al tinaco",
            "la bomba de agua no funciona", "se sale el agua del inodoro", "cambiar el flotador del tanque"],
        ["electricidad"] = [
            "electricista", "se fue la luz en mi casa", "no hay luz en un cuarto", "el contacto no sirve", "cambiar un enchufe",
            "instalar lámparas", "se fundió el foco y no prende", "hizo corto circuito", "se bota la pastilla", "se baja el breaker",
            "revisar el centro de carga", "cambiar el tablero eléctrico", "sale chispa del contacto", "instalar ventilador de techo",
            "el apagador no funciona", "cableado nuevo", "huele a quemado el cable", "variaciones de voltaje", "instalar luces en el jardín",
            "poner más contactos", "no prende el foco del baño", "no enciende la luz de la sala", "la luz parpadea", "no hay corriente en la cocina"],
        ["carpinteria"] = [
            "carpintero", "hacer un closet", "arreglar un mueble", "la puerta de madera no cierra", "cocina integral a la medida",
            "reparar cajones", "barnizar una mesa", "hacer un librero", "repisas de madera", "restaurar sillas", "mueble roto",
            "ropero a la medida", "la puerta se atora y raspa el piso", "cambiar bisagras del closet", "muebles de madera"],
        ["pintura"] = [
            "pintor", "pintar mi casa", "pintar la fachada", "pintar un cuarto", "impermeabilizar el techo", "hay goteras en el techo",
            "humedad en la pared", "la pared tiene salitre", "resanar paredes", "se está cayendo la pintura", "aplicar tirol",
            "pintar rejas con esmalte", "cambiar el color de la sala", "manchas de humedad", "se filtra el agua por la azotea"],
        ["jardineria"] = [
            "jardinero", "cortar el pasto", "podar un árbol", "quitar la maleza", "arreglar el jardín", "sembrar plantas",
            "instalar riego por aspersión", "poner pasto en rollo", "tirar un árbol", "mantenimiento de áreas verdes", "podar setos",
            "las plantas se están secando", "diseño de jardín", "limpiar el terreno de hierba"],
        ["cerrajeria"] = [
            "cerrajero", "me quedé afuera de mi casa", "perdí las llaves", "abrir una puerta", "cambiar la chapa", "la cerradura no abre",
            "sacar copia de llaves", "duplicado de llaves", "la llave se rompió dentro de la chapa", "poner chapa de seguridad",
            "abrir mi carro", "se trabó la cerradura", "cambiar combinación del candado", "chapa multipunto"],
        ["otros"] = [
            "hola", "buenos días", "lo más barato", "cerca de mí", "abierto ahora", "comida", "pizza", "taxi", "doctor", "farmacia",
            "clases de inglés", "renta de departamento", "trabajo", "precio", "opiniones", "mejor calificado"]
    });

    private static (string, string)[] Build(Dictionary<string, string[]> map) => map.SelectMany(kv => kv.Value.Select(t => (t, kv.Key))).ToArray();
}
