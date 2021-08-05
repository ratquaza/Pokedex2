using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Pokedex2;

namespace Pokedex2
{
    public class Pokemon
    {
        public readonly string Name;

        public readonly bool IsDefault;
        public readonly bool IsBaby;
        public readonly Arctype Arctype;

        public readonly Type[] Types = { Type.Unknown, Type.Unknown };

        public readonly int ID;
        public readonly int Generation;

        public readonly string FrontDefault;
        public readonly string FrontShiny;

        private Dictionary<string, Pokemon> Forms = new Dictionary<string, Pokemon>();

        internal Pokemon(JObject species, JObject pokemon)
        {
            JArray langArray = (JArray)species["names"];
            foreach (JObject i in langArray)
            {
                if (i["language"]["name"].ToString() == "en")
                {
                    this.Name = i["name"].ToString();
                    break;
                }
            }

            ID = (int)species["id"];

            IsDefault = (bool)pokemon["is_default"];
            IsBaby = (bool)species["is_baby"];

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

            string generationString = (species["generation"]["url"].ToString());
            this.Generation = int.Parse(generationString[generationString.Length - 2].ToString());

            JArray typeArray = (JArray)pokemon["types"];
            Type type;
            Enum.TryParse<Type>(typeArray[0]["type"]["name"].ToString(), true, out type);
            this.Types[0] = type;
            if (typeArray.Count == 2)
            {
                Enum.TryParse<Type>(typeArray[1]["type"]["name"].ToString(), true, out type);
            }
            this.Types[1] = type;

            this.FrontDefault = pokemon["sprites"]["front_default"].ToString();
            this.FrontShiny = pokemon["sprites"]["front_shiny"].ToString();

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

        public override string ToString()
        {
            return "Pokemon{" + Name + "," + Types[0].ToString() + (Types[1] == Types[0] ? "" : " " + Types[1].ToString()) + "}";
        }
    }
}
