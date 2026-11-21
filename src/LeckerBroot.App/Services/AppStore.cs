using System.Text.Json;
using System.Text.Json.Serialization;
using LeckerBroot.Core;
using Microsoft.JSInterop;

namespace LeckerBroot.App.Services;

/// <summary>
/// Hält alle Daten der App und speichert sie im Browser (localStorage).
/// Daten bleiben auf dem jeweiligen Gerät – Übertragung und Sicherung per Export-Datei.
/// </summary>
public class AppStore
{
    private const string Key = "leckerbroot.data.v1";
    private const string BrokenKey = "leckerbroot.data.unreadable";

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly IJSRuntime _js;
    private bool _loaded;

    public AppStore(IJSRuntime js) => _js = js;

    public AppData Data { get; private set; } = new();

    /// <summary>Hinweis, falls gespeicherte Daten nicht gelesen werden konnten.</summary>
    public string? LoadProblem { get; private set; }

    public AppSettings Settings => Data.Settings;

    public async Task EnsureLoadedAsync()
    {
        if (_loaded) return;
        var json = await _js.InvokeAsync<string?>("localStorage.getItem", Key);
        if (string.IsNullOrWhiteSpace(json))
        {
            Data = SeedData.Create();
            await SaveAsync();
        }
        else
        {
            try
            {
                Data = JsonSerializer.Deserialize<AppData>(json, JsonOptions) ?? SeedData.Create();
            }
            catch (JsonException ex)
            {
                // Unlesbare Daten nicht überschreiben, sondern beiseitelegen.
                await _js.InvokeVoidAsync("localStorage.setItem", BrokenKey, json);
                LoadProblem = "Die gespeicherten Daten konnten nicht gelesen werden und wurden beiseitegelegt: " + ex.Message;
                Data = SeedData.Create();
            }
        }
        _loaded = true;
    }

    public async Task SaveAsync()
    {
        var json = JsonSerializer.Serialize(Data, JsonOptions);
        await _js.InvokeVoidAsync("localStorage.setItem", Key, json);
    }

    // ---------- Rezepte ----------

    public Recipe? GetRecipe(Guid id) => Data.Recipes.FirstOrDefault(r => r.Id == id);

    public async Task SaveRecipeAsync(Recipe recipe)
    {
        recipe.Modified = DateTime.Now;
        var index = Data.Recipes.FindIndex(r => r.Id == recipe.Id);
        if (index >= 0) Data.Recipes[index] = recipe;
        else Data.Recipes.Add(recipe);
        await SaveAsync();
    }

    public async Task DeleteRecipeAsync(Guid id)
    {
        Data.Recipes.RemoveAll(r => r.Id == id);
        await SaveAsync();
    }

    /// <summary>Tiefe Kopie über JSON (für Duplizieren und zum Bearbeiten ohne direktes Ändern).</summary>
    public static T DeepCopy<T>(T value) =>
        JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, JsonOptions), JsonOptions)!;

    // ---------- Zutaten ----------

    public IEnumerable<Ingredient> Flours => Data.Ingredients.Where(i => i.Kind == IngredientKind.Mehl).OrderBy(Names.Display);

    public IEnumerable<Ingredient> OfKind(IngredientKind kind) => Data.Ingredients.Where(i => i.Kind == kind).OrderBy(i => i.Name);

    public Ingredient? GetIngredient(Guid id) => Data.Ingredients.FirstOrDefault(i => i.Id == id);

    /// <summary>Erste passende Zutat einer Art (für Wasser, Salz, Hefe, Anstellgut).</summary>
    public Ingredient Default(IngredientKind kind, Guid preferredId, Func<Ingredient> fallback) =>
        (GetIngredient(preferredId) ?? Data.Ingredients.FirstOrDefault(i => i.Kind == kind) ?? fallback()).Clone();

    // ---------- Sicherung ----------

    public string ExportJson() => JsonSerializer.Serialize(Data, JsonOptions);

    /// <summary>Liest eine Sicherungsdatei. Ersetzt alles oder führt zusammen (gleiche Ids werden überschrieben).</summary>
    public async Task<string> ImportJsonAsync(string json, bool replace)
    {
        var incoming = JsonSerializer.Deserialize<AppData>(json, JsonOptions)
                       ?? throw new InvalidOperationException("Die Datei enthält keine LeckerBroot-Daten.");
        if (replace)
        {
            Data = incoming;
        }
        else
        {
            foreach (var ing in incoming.Ingredients)
            {
                Data.Ingredients.RemoveAll(i => i.Id == ing.Id);
                Data.Ingredients.Add(ing);
            }
            foreach (var rec in incoming.Recipes)
            {
                Data.Recipes.RemoveAll(r => r.Id == rec.Id);
                Data.Recipes.Add(rec);
            }
        }
        await SaveAsync();
        return $"{incoming.Recipes.Count} Rezepte und {incoming.Ingredients.Count} Zutaten übernommen.";
    }
}
