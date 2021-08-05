using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Pokedex2
{
    public class Pokedex
    {
        private static readonly string URL_PREFIX = "https://pokeapi.co/api/v2/";
        private static Dictionary<string, Pokemon> NamedData = new Dictionary<string, Pokemon>();

        public static Pokemon ByName(string name)
        {
            name = name.ToLower().Replace(" ", "-");
            Regex rgx = new Regex("[^a-zA-Z0-9 -]");
            name = rgx.Replace(name, "");
            if (!NamedData.ContainsKey(name))
            {
                HttpClient client = new HttpClient();
                JObject speciesData = GetRequest(URL_PREFIX + "pokemon-species/" + name.ToLower(), client).Result;
                if (speciesData == null)
                {
                    throw new ArgumentNullException("Pokemon " + name + " was not found on the server.");
                }
                JObject pokemonData = GetRequest(speciesData["varieties"][0]["pokemon"]["url"].ToString(), client).Result;

                Pokemon data = new Pokemon(speciesData, pokemonData);
                NamedData.Add(name, data);
            }
            Pokemon value;
            NamedData.TryGetValue(name, out value);
            return value;
        }

        internal static async Task<JObject> GetRequest(string URL, HttpClient existing = null)
        {
            existing = existing == null ? new HttpClient() : existing;
            HttpResponseMessage response = await existing.GetAsync(URL);
            try
            {
                response.EnsureSuccessStatusCode();
                return JObject.Parse(await response.Content.ReadAsStringAsync());
            } catch (HttpRequestException)
            {
                return null;
            }
        }

    }
}
