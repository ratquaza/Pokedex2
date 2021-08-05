using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Ratquaza.Pokedex2;

namespace Ratquaza.Pokedex2
{
    public class Pokemon
    {
        private static readonly string SPRITE_URL = "https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/";

        public readonly string Name;
        public readonly string InternalName;
        public readonly int ID;
        public readonly int Generation;

        public readonly bool IsDefault;
        public readonly bool IsBaby;
        public readonly Arctype Arctype;

        public readonly Type[] Types = { Type.Unknown, Type.Unknown };

        private readonly string[] MaleSprites = new string[4];
        private readonly string[] FemaleSprites = new string[4];

        private Dictionary<string, Pokemon> Forms = new Dictionary<string, Pokemon>();
        private readonly string[] EvolutionNames = new string[0];

        internal Pokemon(JObject species, JObject pokemon)
        {
            JArray nameArray = (JArray) species["names"];
            foreach (JObject names in nameArray)
            {
                if (names["language"]["name"].ToString() == "en")
                {
                    this.Name = names["name"].ToString();
                    break;
                }
            }

            InternalName = pokemon["name"].ToString();
            ID = (int) species["id"];
            string generationString = (species["generation"]["url"].ToString());
            this.Generation = int.Parse(generationString[generationString.Length - 2].ToString());

            IsDefault = (bool) pokemon["is_default"];
            IsBaby = (bool) species["is_baby"];

            if ((bool)species["is_legendary"])
            {
                Arctype = Arctype.Legendary;
            } else if ((bool)species["is_mythical"])
            {
                Arctype = Arctype.Mythical;
            } else if ((ID >= 793 && ID <= 799) || (ID >= 803 && ID <= 806))
            {
                Arctype = Arctype.Ultrabeast;
            } else
            {
                Arctype = Arctype.Normal;
            }

            JArray typeArray = (JArray)pokemon["types"];
            Type type;
            Enum.TryParse(typeArray[0]["type"]["name"].ToString(), true, out type);
            this.Types[0] = type;
            if (typeArray.Count == 2)
            {
                Enum.TryParse(typeArray[1]["type"]["name"].ToString(), true, out type);
            }
            this.Types[1] = type;

            MaleSprites = new string[]
            {
                SPRITE_URL + ID + ".png",
                SPRITE_URL + "shiny/" + ID + ".png",
                SPRITE_URL + "back/" + ID + ".png",
                SPRITE_URL + "back/shiny/" + ID + ".png"
            };
            if (pokemon["sprites"]["front_female"].Type == JTokenType.Null)
            {
                FemaleSprites = MaleSprites;
            } else
            {
                FemaleSprites = new string[]
                {
                    SPRITE_URL + "female/" + ID + ".png",
                    SPRITE_URL + "shiny/female/"  + ID + ".png",
                    SPRITE_URL + "back/female/" + ID + ".png",
                    SPRITE_URL + "back/shiny/female/" + ID + ".png"
                };
            }

            if (IsDefault)
            {
                JArray varietyArray = (JArray) species["varieties"];
                if (varietyArray.Count > 1)
                {
                    List<Task> tasks = new List<Task>();
                    HttpClient client = new HttpClient();

                    for (int i = 1; i < varietyArray.Count; i++)
                    {
                        int copy = i;
                        tasks.Add(new Task(() => {
                            JObject pk = (JObject) varietyArray[copy];
                            string formName = pk["pokemon"]["name"].ToString().Substring(species["name"].ToString().Length + 1);

                            Pokemon form = new Pokemon(species, Pokedex.GetRequest(pk["pokemon"]["url"].ToString(), client).Result);
                            Forms.Add(formName, form);
                        }));
                    }
                    tasks.ForEach((t) => t.Start());
                    Task.WaitAll(tasks.ToArray());
                }

                JObject evolutionData = GetEvolutionData((JObject) Pokedex.GetRequest(species["evolution_chain"]["url"].ToString()).Result["chain"]);
                JArray chains = (JArray) evolutionData["evolves_to"];
                if (chains.Count > 0)
                {
                    EvolutionNames = new string[chains.Count];
                    for (int i = 0; i < EvolutionNames.Length; i++)
                    {
                        EvolutionNames[i] = ((JObject)chains[i])["species"]["name"].ToString();
                    }
                }
            }
        }

        private JObject GetEvolutionData(JObject baseChain)
        {

            if (baseChain["species"]["name"].ToString() == InternalName)
            {
                return baseChain;
            } else
            {
                JArray chain = (JArray) baseChain["evolves_to"];
                if (chain.Count > 0)
                {
                    foreach (JObject c in chain)
                    {
                        JObject data = GetEvolutionData(c);
                        if (data != null) return data;
                    }
                }
                return null;
            }
        }

        public KeyValuePair<string, Pokemon>[] GetForms()
        {
            KeyValuePair<string, Pokemon>[] content = new KeyValuePair<string, Pokemon>[Forms.Keys.Count];
            int index = 0;
            foreach (string formname in Forms.Keys)
            {
                Pokemon form;
                Forms.TryGetValue(formname, out form);
                content[index] = new KeyValuePair<string, Pokemon>(formname, form);
                index++;
            }
            return content;
        }

        public Pokemon GetForm(string name)
        {
            Pokemon form;
            Forms.TryGetValue(name.ToLower(), out form);
            return form;
        }

        public string GetSprite(bool front = true, bool female = false, bool shiny = false)
        {
            int index = (front ? 0 : 2) + (shiny ? 1 : 0);
            return female ? FemaleSprites[index] : MaleSprites[index];
        }

        public string[] GetEvolutions()
        {
            return EvolutionNames;
        }

        public override string ToString()
        {
            return "Pokemon{" + Name + "," + Types[0].ToString() + (Types[1] == Types[0] ? "" : " " + Types[1].ToString()) + "}";
        }
    }
}
