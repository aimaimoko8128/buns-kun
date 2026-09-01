using System.Collections.Generic;
using UnityEngine;

namespace BunsKun.Buns
{
    public static class BunDatabase
    {
        private static List<BunData> all;
        private static BunData starter;

        public static IReadOnlyList<BunData> All
        {
            get { if (all == null) Build(); return all; }
        }

        /// <summary>The bun every new run begins equipped with.</summary>
        public static BunData Starter
        {
            get { if (starter == null) Build(); return starter; }
        }

        public static BunData GetRandomNonStarter(System.Random rng)
        {
            var list = All;
            BunData pick;
            int guard = 0;
            do
            {
                pick = list[rng.Next(list.Count)];
                guard++;
            } while (pick == starter && guard < 10);
            return pick;
        }

        private static void Build()
        {
            all = new List<BunData>();

            starter = BunData.Create("normal", "Normal Bun", "A balanced, everyday bun. Good all-round performance.",
                new Color(0.87f, 0.68f, 0.4f), 5, 100f, 10f, 1f, 1f);
            all.Add(starter);

            all.Add(BunData.Create("small", "Small Bun", "Light and nimble, but can only hold a few ingredients.",
                new Color(0.95f, 0.82f, 0.55f), 3, 60f, 12f, 1.15f, 1f));

            all.Add(BunData.Create("large", "Large Bun", "Holds many ingredients, but its bulk slows you down and eats into mana.",
                new Color(0.7f, 0.5f, 0.28f), 7, 80f, 7f, 0.85f, 1.1f));

            all.Add(BunData.Create("sesame", "Sesame Royale Bun", "A big mana pool for aggressive builds, at the cost of a fragile crust.",
                new Color(0.9f, 0.78f, 0.4f), 6, 130f, 9f, 0.95f, 1.2f));

            all.Add(BunData.Create("wholewheat", "Whole Wheat Bun", "Dense and tough. Takes noticeably less damage, but is slower and holds less mana.",
                new Color(0.55f, 0.38f, 0.22f), 4, 90f, 8f, 0.9f, 0.75f));
        }
    }
}
