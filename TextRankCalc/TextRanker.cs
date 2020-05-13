using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TextRankCalc
{
    public class TextRanker
    {
        public static void CalculateRank(string id, string text)
        {
            List<char> vowelChars = new List<char> { 'a', 'e', 'i', 'o', 'u'};
            List<char> consonantChars = new List<char> { 'b', 'c', 'd', 'f', 'g', 'h', 'j', 'k', 'l', 'm', 'n', 'p', 'q', 'r', 's', 't', 'v', 'w', 'x', 'y', 'z' };
            int vowelCounter = 0;
            int consonantCounter = 0;

            foreach (char ch in text)
            {
                if (Char.IsLetter(ch))
                {
                    char lower = Char.ToLower(ch);

                    if (vowelChars.Contains(lower))
                    {
                        vowelCounter++; 
                    }
                    else if (consonantChars.Contains(lower))
                    {
                        consonantCounter++;
                    }
                }
            }

            string result = String.Empty;

            if (consonantCounter == 0)
            {
                result = "infinity";
            }
            else
            {
                float num = (float)vowelCounter / (float)consonantCounter;
                result = num.ToString();
            }

            SaveToDb("rank_" + id, result);
        }

        private static void SaveToDb(string id, string rank)
        {
            string currentDirectoryPath = Directory.GetCurrentDirectory();
            string jsonPath = Directory.GetParent(currentDirectoryPath) + "/config/config.json";
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(jsonPath, optional: false)
                .Build();

            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect($"{config.GetValue<string>("RedisHost")}:{config.GetValue<int>("RedisPort")}");
            IDatabase db = redis.GetDatabase();
            db.StringSet(id, rank);
        }
    }
}
